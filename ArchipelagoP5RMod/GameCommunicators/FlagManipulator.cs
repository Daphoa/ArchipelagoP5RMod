using ArchipelagoP5RMod.Types;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using MemoryStream = System.IO.MemoryStream;

namespace ArchipelagoP5RMod;

public class FlagManipulator
{
    public const uint AP_LAST_REWARD_INDEX = SectionMask * ExternalCountSection + 0;
    public const uint AP_CURR_REWARD_CMM_ABILITY = SectionMask * ExternalCountSection + 1;
    public const uint AP_CURR_REWARD_ITEM_ID = SectionMask * ExternalCountSection + 2;
    public const uint AP_CURR_REWARD_ITEM_NUM = SectionMask * ExternalCountSection + 3;
    public const uint AP_CURR_NOTIFY_PALACE = SectionMask * ExternalCountSection + 4;
    public const uint SHOWING_MESSAGE = SectionMask * ExternalBitSection + 1;
    public const uint SHOWING_GAME_MSG = SectionMask * ExternalBitSection + 2;
    public const uint OVERWRITE_ITEM_TEXT = SectionMask * ExternalBitSection + 3;

    [Function(CallingConventions.Fastcall)]
    private delegate byte BitChkType(uint bitIndex);

    [Function(CallingConventions.Fastcall)]
    private delegate uint BitToggleType();

    // private IntPtr _bitChkWrapperAdr;
    private IHook<BitChkType> _bitChkHook;
    private IHook<BitToggleType> _bitOnHook;
    private IHook<BitToggleType> _bitOffHook;
    private IHook<FlowFunctionWrapper.FlowFuncDelegate4> _getCountFlowHook;
    private IHook<FlowFunctionWrapper.FlowFuncDelegate4> _setCountFlowHook;

    private const uint SectionMask = 0x10000000;

    private const uint ExternalBitSection = 6; // This will have consequences if changed.
    private const uint NumExternalBitFlags = 4;
    private static bool[] externalBitFlags = new bool[NumExternalBitFlags];

    // This will have consequences if changed. Should stay at this value ideally.
    private const uint ExternalCountSection = 1;

    const uint NumExternalCounts = 5;
    private static uint[] externalCounts = [0, 0, 0, 0, 0];
    private const int CountTypeSize = sizeof(uint);

    public FlagManipulator(IReloadedHooks hooks)
    {
        AddressScanner.DelayedScanPattern(
            "4C 8D 05 ?? ?? ?? ?? 33 C0 49 8B D0 0F 1F 40 00 39 0A 74 ?? FF C0 48 83 C2 08 83 F8 10 72 ?? 8B D1",
            address => _bitChkHook = hooks.CreateHook<BitChkType>(BitChkImpl, address).Activate());
        AddressScanner.DelayedScanPattern(
            "40 53 48 83 EC 20 31 C9 E8 ?? ?? ?? ?? B9 01 00 00 00",
            address => _bitOnHook = hooks.CreateHook<BitToggleType>(BitOnImpl, address).Activate());
        AddressScanner.DelayedScanPattern(
            "40 53 48 83 EC 20 31 C9 E8 ?? ?? ?? ?? 31 C9 89 C3",
            address => _bitOffHook = hooks.CreateHook<BitToggleType>(BitOffImpl, address).Activate());
        AddressScanner.DelayedScanPattern(
            "48 83 EC 28 31 C9 E8 ?? ?? ?? ?? 4C 8D 05 ?? ?? ?? ??",
            address => _getCountFlowHook =
                hooks.CreateHook<FlowFunctionWrapper.FlowFuncDelegate4>(GetCountImpl, address).Activate());
        AddressScanner.DelayedScanPattern(
            "48 83 EC 28 31 C9 E8 ?? ?? ?? ?? B9 01 00 00 00 4C 63 C8",
            address => _setCountFlowHook =
                hooks.CreateHook<FlowFunctionWrapper.FlowFuncDelegate4>(SetCountImpl, address).Activate());

        MyLogger.DebugLog("Created FlagManipulator Hooks");

        // Note: this is playing a little bit with fire. If it needed to call the in game function, it'd get a null ref.
        SetBit(SHOWING_MESSAGE, false);
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public bool CheckBit(uint bitIndex)
    {
        if (bitIndex is >= SectionMask * ExternalBitSection
            and < SectionMask * ExternalBitSection + NumExternalCounts)
        {
            return externalBitFlags[bitIndex % SectionMask];
        }

        return _bitChkHook.OriginalFunction(bitIndex) != 0;
    }

    public bool CheckBit(short section, uint bitIndex)
    {
        uint bit = (uint)section * SectionMask + bitIndex;
        return CheckBit(bit);
    }

    #region Save/Load

    public byte[] SaveCountData()
    {
        MemoryStream stream = new();
        for (int i = 0; i < NumExternalCounts; i++)
        {
            stream.Write(BitConverter.GetBytes(externalCounts[i]), 0, CountTypeSize);
        }

        return stream.ToArray();
    }

    public void LoadCountData(MemoryStream data)
    {
        for (int i = 0; i < NumExternalCounts; i++)
        {
            byte[] buffer = new byte[CountTypeSize];
            int readBytes = data.Read(buffer, 0, CountTypeSize);
            if (readBytes < CountTypeSize)
            {
                throw new InvalidDataException("Tried to read from count data, but got fewer bytes than expected " +
                                               $"(expected {CountTypeSize * NumExternalCounts}, received {i * NumExternalCounts + readBytes}).");
            }

            externalCounts[i] = BitConverter.ToUInt16(buffer);
        }
    }

    #endregion

    private uint GetCountImpl()
    {
        int countId = FlowFunctionWrapper.GetFlowscriptInt4Arg(0);

        if (countId < SectionMask * ExternalCountSection ||
            countId >= SectionMask * ExternalCountSection + NumExternalCounts)
            return _getCountFlowHook.OriginalFunction();

        unsafe
        {
            FlowFunctionWrapper.FlowCommandDataAddress->ReturnType = FlowReturnType.Int;
            FlowFunctionWrapper.FlowCommandDataAddress->ReturnValue = externalCounts[countId % SectionMask];
        }

        return 1;
    }

    public void SetCount(uint countId, uint value)
    {
        if (countId is >= SectionMask * ExternalCountSection
            and < SectionMask * ExternalCountSection + NumExternalCounts)
        {
            externalCounts[countId % SectionMask] = value;
            return;
        }

        FlowFunctionWrapper.CallFlowFunctionSetup(countId, value);

        _setCountFlowHook.OriginalFunction();

        FlowFunctionWrapper.CallFlowFunctionCleanup();
    }

    public uint GetCount(uint countId)
    {
        if (countId is >= SectionMask * ExternalCountSection
            and < SectionMask * ExternalCountSection + NumExternalCounts)
        {
            return externalCounts[countId % SectionMask];
        }

        FlowFunctionWrapper.CallFlowFunctionSetup(countId);

        _getCountFlowHook.OriginalFunction();

        return (uint)FlowFunctionWrapper.CallFlowFunctionCleanup();
    }

    private uint SetCountImpl()
    {
        int countId = FlowFunctionWrapper.GetFlowscriptInt4Arg(0);
        uint value = (uint)FlowFunctionWrapper.GetFlowscriptInt4Arg(1);

        if (countId == 56 && value == 15)
        {
            // Doing a sneaky swap of 15 to 13 so Ryuji will hang out with us even if we can send a calling card. 
            FlowFunctionWrapper.ReplaceArgInt4(1, 13);
        }
        
        if (countId < SectionMask * ExternalCountSection ||
            countId >= SectionMask * ExternalCountSection + NumExternalCounts)
            return _setCountFlowHook.OriginalFunction();

        externalCounts[countId % SectionMask] = value;

        return 1;
    }

    private byte BitChkImpl(uint bitIndex)
    {
        if (bitIndex is < ExternalBitSection * SectionMask or >= ExternalBitSection * SectionMask + NumExternalBitFlags)
            return _bitChkHook.OriginalFunction(bitIndex);

        uint bit = bitIndex - ExternalBitSection * SectionMask;
        return externalBitFlags[bit] ? (byte)1 : (byte)0;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public void SetBit(uint bitIndex, bool value)
    {
        if (bitIndex is >= ExternalBitSection * SectionMask
            and < ExternalBitSection * SectionMask + NumExternalBitFlags)
        {
            uint bit = bitIndex - ExternalBitSection * SectionMask;
            externalBitFlags[bit] = value;
            return;
        }

        FlowFunctionWrapper.CallFlowFunctionSetup((int)bitIndex);

        if (value)
        {
            _bitOnHook.OriginalFunction();
        }
        else
        {
            _bitOffHook.OriginalFunction();
        }

        FlowFunctionWrapper.CallFlowFunctionCleanup();
    }
    
    public void ToggleBit(uint bitIndex)
    {
        if (bitIndex is >= ExternalBitSection * SectionMask
            and < ExternalBitSection * SectionMask + NumExternalBitFlags)
        {
            uint bit = bitIndex - ExternalBitSection * SectionMask;
            externalBitFlags[bit] = !externalBitFlags[bit];
            return;
        }
        
        bool originalValue = _bitChkHook.OriginalFunction(bitIndex) != 0;
        SetBit(bitIndex, !originalValue);
    }

    private uint BitOnImpl()
    {
        var bitIndex = (uint)FlowFunctionWrapper.GetFlowscriptInt4Arg(0);

        if (bitIndex is < ExternalBitSection * SectionMask or >= ExternalBitSection * SectionMask + NumExternalBitFlags)
            return _bitOnHook.OriginalFunction();

        uint bitOffset = bitIndex % ExternalBitSection;

        externalBitFlags[bitOffset] = true;

        return 1;
    }

    private uint BitOffImpl()
    {
        var bitIndex = (uint)FlowFunctionWrapper.GetFlowscriptInt4Arg(0);

        if (bitIndex is < ExternalBitSection * SectionMask or >= ExternalBitSection * SectionMask + NumExternalBitFlags)
            return _bitOffHook.OriginalFunction();

        uint bitOffset = bitIndex % ExternalBitSection;

        externalBitFlags[bitOffset] = false;

        return 1;
    }

    public void SetBit(short section, uint bitIndex, bool value)
    {
        uint bit = (uint)section * SectionMask + bitIndex;
        SetBit(bit, value);
    }
}