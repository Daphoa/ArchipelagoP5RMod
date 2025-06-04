using ArchipelagoP5RMod.Types;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;

namespace ArchipelagoP5RMod.GameCommunicators;

public class BattleManipulator
{
    [Function(CallingConventions.Fastcall)]
    private delegate IntPtr CallBattleFlowType(IntPtr param1, IntPtr param2, IntPtr param3, int param4);

    [Function(CallingConventions.Fastcall)]
    private unsafe delegate ulong EndBattleType(BattleResult* battleResult);

    private IHook<CallBattleFlowType> _callBattleFlowHook;
    private IHook<CallBattleFlowType> _callEventBattleFlowHook;
    private IHook<CallBattleFlowType> _callFieldBattleFlowHook;
    private IHook<EndBattleType> _endBattleHook;

    private int _currBattleId = -1;
    private unsafe ushort* _CALL_EVENTBATTLE_STATE;

    public delegate void OnBattleCompleteEvent(int battleId, BattleResult result);

    public event OnBattleCompleteEvent OnBattleComplete;


    public BattleManipulator(IReloadedHooks hooks)
    {
        AddressScanner.DelayedScanPattern(
            "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 50 E8 ?? ?? ?? ??",
            address => _callBattleFlowHook =
                hooks.CreateHook<CallBattleFlowType>(CallBattleFlowImpl, address).Activate());
        AddressScanner.DelayedScanPattern(
            "48 8B C4 55 53 41 55 48 8D A8 ?? ?? ?? ?? 48 81 EC E0 04 00 00",
            address => _callEventBattleFlowHook =
                hooks.CreateHook<CallBattleFlowType>(CallEventBattleFlowImpl, address).Activate());
        // AddressScanner.DelayedScanPattern(
        //     "48 8B C4 55 57 48 8D 68 ?? 48 81 EC B8 00 00 00",
        //     address => _callFieldBattleFlowHook =
        //         hooks.CreateHook<CallBattleFlowType>(CallFieldBattleFlowImpl, address).Activate());
        unsafe
        {
            AddressScanner.DelayedAddressHack(0x2959798, address =>
                _CALL_EVENTBATTLE_STATE = (ushort*)address
            );

            AddressScanner.DelayedScanPattern(
                "40 53 55 41 57 48 81 EC 80 00 00 00",
                address => _endBattleHook = hooks.CreateHook<EndBattleType>(EndBattleImpl, address).Activate());
        }
    }

    private IntPtr CallBattleFlowImpl(IntPtr param1, IntPtr param2, IntPtr param3, int param4)
    {
        _currBattleId = FlowFunctionWrapper.GetFlowscriptInt4Arg.Invoke(0) % 1000;
        MyLogger.DebugLog($"Call battle: {_currBattleId}");

        return _callBattleFlowHook.OriginalFunction(param1, param2, param3, param4);
    }

    private unsafe IntPtr CallEventBattleFlowImpl(IntPtr param1, IntPtr param2, IntPtr param3, int param4)
    {
        if (*_CALL_EVENTBATTLE_STATE == 0)
        {
            _currBattleId = FlowFunctionWrapper.GetFlowscriptInt4Arg.Invoke(2) % 1000;
            MyLogger.DebugLog($"Call Event Battle: {_currBattleId}");
        }

        return _callEventBattleFlowHook.OriginalFunction(param1, param2, param3, param4);
    }

    // private long CallFieldBattleFlowImpl(long param1, long param2, long param3, int param4)
    // {
    //     return _callFieldBattleFlowHook.OriginalFunction(param1, param2, param3, param4);
    // }
    //
    private unsafe ulong EndBattleImpl(BattleResult* battleResult)
    {
        MyLogger.DebugLog($"End battle: {_currBattleId} | result: {*battleResult}");
        OnBattleComplete.Invoke(_currBattleId, *battleResult);

        _currBattleId = -1;
        return _endBattleHook.OriginalFunction(battleResult);
    }
}