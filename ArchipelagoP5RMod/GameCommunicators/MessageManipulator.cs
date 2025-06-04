using Reloaded.Hooks.Definitions;

namespace ArchipelagoP5RMod.GameCommunicators;

public class MessageManipulator
{
    private readonly FlagManipulator _flagManipulator;

    private static readonly object MessageDisplayingLock = new();

    private static bool showingMessage = false;

    private IHook<FlowFunctionWrapper.FlowFuncDelegate8> _msgWndDps;
    private IHook<FlowFunctionWrapper.FlowFuncDelegate8> _msgWndCls;

    public MessageManipulator(FlagManipulator flagManipulator, IReloadedHooks hooks)
    {
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
        // Only one message can be shown at once. 
        bool acquiredLock = false;
        try
        {
            Monitor.Enter(MessageDisplayingLock, ref acquiredLock);

            if (showingMessage)
            {
                MyLogger.DebugLog("Can't show message, it is already showing.");
                return 0;
            }
            showingMessage = true;
        }
        finally
        {
            if (acquiredLock)
            {
                Monitor.Exit(MessageDisplayingLock);
            }
        }

        _flagManipulator.SetBit(FlagManipulator.SHOWING_GAME_MSG, true);

        return _msgWndDps.OriginalFunction();
    }

    private ulong MsgWndClsImpl()
    {
        ulong retVal = _msgWndCls.OriginalFunction();

        showingMessage = false;
        _flagManipulator.SetBit(FlagManipulator.SHOWING_GAME_MSG, false);

        return retVal;
    }
}