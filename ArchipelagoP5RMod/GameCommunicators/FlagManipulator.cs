using ArchipelagoP5RMod.Types;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Mod.Interfaces;

namespace ArchipelagoP5RMod;

public class FlagManipulator
{
    private readonly ILogger _logger;

    [Function(CallingConventions.Fastcall)]
    private delegate long BitChkType(uint bitIndex);

    [Function(CallingConventions.Fastcall)]
    private delegate uint BitToggleType();

    // private IntPtr _bitChkWrapperAdr;
    private readonly IHook<BitChkType> _bitChkTypeHook;
    private readonly IntPtr _bitOnWrapperAdr;
    private readonly IntPtr _bitOffWrapperAdr;
    private readonly IHook<FlowFunctionWrapper.FlowFuncDelegate> _getCountFlowHook;
    private readonly IHook<FlowFunctionWrapper.FlowFuncDelegate> _setCountFlowHook;

    private const uint SectionMask = 0x10000000;

    private const uint ExternalBitSection = 6; // This will have consequences if changed. Should stay hardcoded ideally.
    private const uint NumExternalBitFlags = 4;
    private static bool[] externalBitFlags = new bool[NumExternalBitFlags];

    private const uint
        ExternalCountSection = 1; // This will have consequences if changed. Should stay hardcoded ideally.

    const uint NumExternalCounts = 4;
    private static uint[] externalCounts = new uint[4]{0x4000 + 72,0x4000 + 72,0x4000 + 72,0x4000 + 72};

    private BitToggleType BitOn { get; set; }
    private BitToggleType BitOff { get; set; }

    public FlagManipulator(IReloadedHooks hooks, ILogger logger)
    {
        _logger = logger;
        _bitChkTypeHook = hooks
            .CreateHook<BitChkType>(BitChkImpl, AddressScanner.Addresses[AddressScanner.AddressName.ChkBitFuncAddress])
            .Activate();
        BitOn = hooks.CreateWrapper<BitToggleType>(
            AddressScanner.Addresses[AddressScanner.AddressName.BitOnFlowFuncAddress], out _bitOnWrapperAdr);
        BitOff = hooks.CreateWrapper<BitToggleType>(
            AddressScanner.Addresses[AddressScanner.AddressName.BitOffFlowFuncAddress], out _bitOffWrapperAdr);
        _getCountFlowHook = hooks.CreateHook<FlowFunctionWrapper.FlowFuncDelegate>(GetCountImpl,
            AddressScanner.Addresses[AddressScanner.AddressName.GetCountFlowFuncAddress]).Activate();

        logger.WriteLine("Created FlagManipulator Hooks");
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public bool CheckBit(uint bitIndex)
    {
        return _bitChkTypeHook.OriginalFunction(bitIndex) != 0;
    }

    public bool CheckBit(short section, uint bitIndex)
    {
        uint bit = (uint)section * SectionMask + bitIndex;
        return CheckBit(bit);
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
            _logger.WriteLine("Calling BIT_ON");
            BitOn();
        }
        else
        {
            _logger.WriteLine("Calling BIT_OFF");
            BitOff();
        }

        FlowFunctionWrapper.CallFlowFunctionCleanup();
    }

    public void SetBit(short section, uint bitIndex, bool value)
    {
        uint bit = (uint)section * SectionMask + bitIndex;
        SetBit(bit, value);
    }
}