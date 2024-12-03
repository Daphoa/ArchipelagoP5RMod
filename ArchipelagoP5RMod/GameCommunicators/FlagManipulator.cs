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

    private IntPtr _bitChkWrapperAdr;
    private IntPtr _bitOnWrapperAdr;
    private IntPtr _bitOffWrapperAdr;

    private BitChkType BitChk { get; set; }
    private BitToggleType BitOn { get; set; }
    private BitToggleType BitOff { get; set; }

    public FlagManipulator(IReloadedHooks hooks, ILogger logger)
    {
        _logger = logger;
        BitChk = hooks.CreateWrapper<BitChkType>(AddressScanner.ChkBitFuncAddress, out _bitChkWrapperAdr);
        BitOn = hooks.CreateWrapper<BitToggleType>(AddressScanner.BitOnFlowFuncAddress, out _bitOnWrapperAdr);
        BitOff = hooks.CreateWrapper<BitToggleType>(AddressScanner.BitOffFlowFuncAddress, out _bitOffWrapperAdr);

        logger.WriteLine("Created FlagManipulator Hooks");
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public bool CheckBit(uint bitIndex)
    {
        return BitChk(bitIndex) != 0;
    }

    public bool CheckBit(short section, uint bitIndex)
    {
        uint bit = (uint)section * 0x10000000 + bitIndex;
        return CheckBit(bit);
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public void SetBit(uint bitIndex, bool value)
    {
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
        uint bit = (uint)section * 0x10000000 + bitIndex;
        SetBit(bit, value);
    }

}