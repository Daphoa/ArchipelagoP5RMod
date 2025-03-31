using System.Runtime.InteropServices.ComTypes;
using ArchipelagoP5RMod.Types;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Mod.Interfaces;
using MemoryStream = System.IO.MemoryStream;

namespace ArchipelagoP5RMod;

public class FlagManipulator
{
    private readonly ILogger _logger;

    public const uint AP_LAST_REWARD_INDEX = SectionMask * ExternalCountSection + 0;
    public const uint AP_CURR_REWARD_CMM_ABILITY = SectionMask * ExternalCountSection + 1;
    public const uint AP_CURR_REWARD_ITEM_ID = SectionMask * ExternalCountSection + 2;
    public const uint AP_CURR_REWARD_ITEM_NUM = SectionMask * ExternalCountSection + 3;

    public const uint SHOWING_MESSAGE = SectionMask * ExternalBitSection + 1;

    [Function(CallingConventions.Fastcall)]
    private delegate long BitChkType(uint bitIndex);

    [Function(CallingConventions.Fastcall)]
    private delegate uint BitToggleType();

    // private IntPtr _bitChkWrapperAdr;
    private readonly IHook<BitChkType> _bitChkTypeHook;
    private readonly IHook<BitToggleType> _bitOnHook;
    private readonly IHook<BitToggleType> _bitOffHook;
    private readonly IHook<FlowFunctionWrapper.FlowFuncDelegate> _getCountFlowHook;
    private readonly IHook<FlowFunctionWrapper.FlowFuncDelegate> _setCountFlowHook;

    private const uint SectionMask = 0x10000000;

    private const uint ExternalBitSection = 6; // This will have consequences if changed. Should stay hardcoded ideally.
    private const uint NumExternalBitFlags = 4;
    private static bool[] externalBitFlags = new bool[NumExternalBitFlags];

    // This will have consequences if changed. Should stay at this value ideally.
    private const uint ExternalCountSection = 1;

    const uint NumExternalCounts = 4;
    private static uint[] externalCounts = new uint[4] { 0, 0, 0, 0 };
    private const int CountTypeSize = sizeof(uint);

    public FlagManipulator(IReloadedHooks hooks, ILogger logger)
    {
        _logger = logger;
        _bitChkTypeHook = hooks
            .CreateHook<BitChkType>(BitChkImpl, AddressScanner.Addresses[AddressScanner.AddressName.ChkBitFuncAddress])
            .Activate();
        _bitOnHook = hooks.CreateHook<BitToggleType>(BitOnImpl,
            AddressScanner.Addresses[AddressScanner.AddressName.BitOnFlowFuncAddress]).Activate();
        _bitOffHook = hooks.CreateHook<BitToggleType>(BitOffImpl,
            AddressScanner.Addresses[AddressScanner.AddressName.BitOffFlowFuncAddress]).Activate();
        _getCountFlowHook = hooks.CreateHook<FlowFunctionWrapper.FlowFuncDelegate>(GetCountImpl,
            AddressScanner.Addresses[AddressScanner.AddressName.GetCountFlowFuncAddress]).Activate();
        _setCountFlowHook = hooks.CreateHook<FlowFunctionWrapper.FlowFuncDelegate>(SetCountImpl,
            AddressScanner.Addresses[AddressScanner.AddressName.SetCountFlowFuncAddress]).Activate();

        logger.WriteLine("Created FlagManipulator Hooks");
        
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
        
        return _bitChkTypeHook.OriginalFunction(bitIndex) != 0;
    }

    public bool CheckBit(short section, uint bitIndex)
    {
        uint bit = (uint)section * SectionMask + bitIndex;
        return CheckBit(bit);
    }

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

    private uint GetCountImpl()
    {
        int countId = FlowFunctionWrapper.GetFlowscriptInt4Arg(0);

        if (countId < SectionMask * ExternalCountSection ||
            countId >= SectionMask * ExternalCountSection + NumExternalCounts)
            return _getCountFlowHook.OriginalFunction();

        unsafe
        {
            AddressScanner.FlowCommandDataAddress->ReturnType = FlowReturnType.Int;
            AddressScanner.FlowCommandDataAddress->ReturnValue = externalCounts[countId % SectionMask];
        }

        _logger.WriteLine($"GetCountImpl found result {externalCounts[countId % SectionMask]} from input {countId:X}.");

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
        if (countId is >= SectionMask * ExternalCountSection and < SectionMask * ExternalCountSection + NumExternalCounts)
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

        if (countId < SectionMask * ExternalCountSection ||
            countId >= SectionMask * ExternalCountSection + NumExternalCounts)
            return _setCountFlowHook.OriginalFunction();

        uint value = (uint)FlowFunctionWrapper.GetFlowscriptInt4Arg(1);
        externalCounts[countId % SectionMask] = value;

        return 1;
    }

    private long BitChkImpl(uint bitIndex)
    {
        if (bitIndex is < ExternalBitSection * SectionMask or >= ExternalBitSection * SectionMask + NumExternalBitFlags)
            return _bitChkTypeHook.OriginalFunction(bitIndex);

        uint bit = bitIndex - ExternalBitSection * SectionMask;
        return externalBitFlags[bit] ? 1 : 0;
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

    private uint BitOnImpl()
    {
        var bitIndex = (uint)FlowFunctionWrapper.GetFlowscriptInt4Arg(0);

        _logger.WriteLine($"Turning {bitIndex:X} bit on.");

        if (bitIndex is < ExternalBitSection * SectionMask or >= ExternalBitSection * SectionMask + NumExternalBitFlags)
            return _bitOnHook.OriginalFunction();

        uint bitOffset = bitIndex % ExternalBitSection;

        externalBitFlags[bitOffset] = true;

        return 1;
    }

    private uint BitOffImpl()
    {
        var bitIndex = (uint)FlowFunctionWrapper.GetFlowscriptInt4Arg(0);
        
        _logger.WriteLine($"Turning {bitIndex:X} bit off.");

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