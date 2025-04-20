using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Mod.Interfaces;

namespace ArchipelagoP5RMod.GameCommunicators;

public class MessageManipulator
{
    private readonly ILogger _logger;
    private readonly FlagManipulator _flagManipulator;

    private IHook<FlowFunctionWrapper.FlowFuncDelegate8> _msgWndDps;
    private IHook<FlowFunctionWrapper.FlowFuncDelegate8> _msgWndCls;

    public MessageManipulator(FlagManipulator flagManipulator, IReloadedHooks hooks, ILogger logger)
    {
        _logger = logger;
        _flagManipulator = flagManipulator;

        AddressScanner.DelayedScanPattern(
            "40 53 48 83 EC 20 48 8B 05 ?? ?? ?? ?? 48 85 C0 75 ?? 31 DB",
            address => _msgWndDps =
                hooks.CreateHook<FlowFunctionWrapper.FlowFuncDelegate8>(MsgWndDpsImpl, address).Activate());
        AddressScanner.DelayedScanPattern(
            "40 53 48 83 EC 20 48 8B 05 ?? ?? ?? ?? 31 DB",
            address => _msgWndCls =
                hooks.CreateHook<FlowFunctionWrapper.FlowFuncDelegate8>(MsgWndClsImpl, address).Activate());
    }

    private ulong MsgWndDpsImpl()
    {
        _flagManipulator.SetBit(FlagManipulator.SHOWING_GAME_MSG, true);

        return _msgWndDps.OriginalFunction();
    }

    private ulong MsgWndClsImpl()
    {
        _flagManipulator.SetBit(FlagManipulator.SHOWING_GAME_MSG, false);

        return _msgWndCls.OriginalFunction();
    }
}