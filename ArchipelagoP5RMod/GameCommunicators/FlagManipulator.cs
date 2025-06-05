using System.Diagnostics;
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

    private static readonly uint[] neverTouchFlags =
    [
        // Disallows add/remove members from current party in Stats menu 
        11779, 0x40000000 + 0003,
        11780, 0x40000000 + 0004,
        11784, 0x40000000 + 0008,
        11785, 0x40000000 + 0009,
        11786, 0x40000000 + 0010,
        // Party Members
        11824, 0x40000000 + 48, // Ryuji
        11825, 0x40000000 + 49, // Morgana
        11826, 0x40000000 + 50, // Ann
        11827, 0x40000000 + 51, // Yosuke
        11828, 0x40000000 + 52, // Makoto
        11829, 0x40000000 + 53, // Haru
        11830, 0x40000000 + 54, // Futaba
        11831, 0x40000000 + 55, // Aketchi
        11832, 0x40000000 + 56, // Kasumi
        1168, //  Ryuji Group Chat
        1169, // Ann Group Chat
        1170, // Yosuke Group Chat
        1171, // Makoto Group Chat
        1173, // Haru Group Chat
        1172, // Futaba Group Chat
        1174, // Aketchi Group Chat
        527, // Kasumi Group Chat
    ];

    // This will have consequences if changed. Should stay at this value ideally.
    private const uint ExternalCountSection = 1;

    const uint NumExternalCounts = 5;
    private static uint[] externalCounts = [0, 0, 0, 0, 0];
    private const int CountTypeSize = sizeof(uint);

#if DEVELOP
    private static readonly uint[] FlagsOfInterest = [];
    private static readonly uint[] CountsOfInterest = [];
#endif

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

    public byte[] SaveData()
    {
        MemoryStream stream = new();

        stream.Write(ByteTools.CollectionToByteArray(externalCounts, BitConverter.GetBytes));

        return stream.ToArray();
    }

    public void LoadData(MemoryStream data)
    {
        var results = ByteTools.ByteArrayToCollection<List<uint>, uint>(data, sizeof(uint),
            b => BitConverter.ToUInt32(b));

        if (results.Count != NumExternalCounts)
        {
            MyLogger.DebugLog($"Invalid number of external counts got while loading data: {results.Count}");
            return;
        }
        
        externalCounts = results.ToArray();
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
        uint countId = (uint)FlowFunctionWrapper.GetFlowscriptInt4Arg(0);
        uint value = (uint)FlowFunctionWrapper.GetFlowscriptInt4Arg(1);

        PrintCountOfInterest(countId, value);

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

        PrintFlagOfInterest(bitIndex, true);

        if (neverTouchFlags.Contains(bitIndex))
        {
            MyLogger.DebugLog($"Tried to turn on bit {bitIndex:X2} but we shouldn't touch it.");
            return 1;
        }

        if (bitIndex is < ExternalBitSection * SectionMask or >= ExternalBitSection * SectionMask + NumExternalBitFlags)
            return _bitOnHook.OriginalFunction();

        uint bitOffset = bitIndex % ExternalBitSection;

        externalBitFlags[bitOffset] = true;

        return 1;
    }

    private uint BitOffImpl()
    {
        var bitIndex = (uint)FlowFunctionWrapper.GetFlowscriptInt4Arg(0);

        PrintFlagOfInterest(bitIndex, false);

        if (neverTouchFlags.Contains(bitIndex))
        {
            MyLogger.DebugLog($"Tried to turn on bit {bitIndex:X2} but we shouldn't touch it.");
            return 1;
        }

        if (bitIndex is < ExternalBitSection * SectionMask or >= ExternalBitSection * SectionMask + NumExternalBitFlags)
            return _bitOffHook.OriginalFunction();

        uint bitOffset = bitIndex % ExternalBitSection;

        externalBitFlags[bitOffset] = false;

        return 1;
    }

    [Conditional("DEVELOP")]
    private void PrintFlagOfInterest(uint bitIndex, bool value)
    {
        if (!FlagsOfInterest.Contains(bitIndex))
        {
            return;
        }

        MyLogger.DebugLog($"[DEV] BitIndex: 0x{bitIndex:X} set to {value}");
    }

    [Conditional("DEVELOP")]
    private void PrintCountOfInterest(uint countIndex, long value)
    {
        if (!CountsOfInterest.Contains(countIndex))
        {
            return;
        }

        MyLogger.DebugLog($"[DEV] BitIndex: {countIndex} set to {value}");
    }

    public void SetBit(short section, uint bitIndex, bool value)
    {
        uint bit = (uint)section * SectionMask + bitIndex;
        SetBit(bit, value);
    }
}