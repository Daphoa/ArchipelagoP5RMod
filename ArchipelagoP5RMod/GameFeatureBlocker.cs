using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;

namespace ArchipelagoP5RMod;

public class GameFeatureBlocker
{
    [Function(CallingConventions.Fastcall)]
    private delegate long CallTutorialFlow();
    [Function(CallingConventions.Fastcall)]
    private delegate void NetSetAction(byte time, short day, byte activity);

    private static IHook<CallTutorialFlow>? _callTutorialFlowHook;
    private static IHook<NetSetAction>? _netSetActionHook;

    public static void BlockGameFeatures(IReloadedHooks hooks)
    {
        _callTutorialFlowHook =
            hooks.CreateHook<CallTutorialFlow>(CallTutorialImpl, AddressScanner.CallTutorialFlowFuncAddress)
                .Activate();
        _netSetActionHook =
            hooks.CreateHook<NetSetAction>(NetSetActionImpl, AddressScanner.NetSetActionFuncAddress)
                .Activate();
    }

    private static long CallTutorialImpl()
    {
        // Don't call tutorials
        // return 0;
        return _callTutorialFlowHook!.OriginalFunction();
    }
    
    private static void NetSetActionImpl(byte time, short day, byte activity)
    {
        // Don't set net activity
        // return;
        _netSetActionHook!.OriginalFunction(time, day, activity);
    }
    
}