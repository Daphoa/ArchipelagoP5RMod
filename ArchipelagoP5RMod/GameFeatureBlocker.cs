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
        AddressScanner.DelayedScanPattern(
            "48 83 EC 28 48 8B 05 ?? ?? ?? ?? 48 85 C0 74 ?? 83 B8 ?? ?? ?? ?? 00 74 ?? 48 8D 0D ?? ?? ?? ?? E8 ?? " +
            "?? ?? ?? 48 85 C0 75 ?? B8 01 00 00 00 48 83 C4 28 C3 B9 01 00 00 00 E8 ?? ?? ?? ?? 33 C9 44 8B C8 E8 " +
            "?? ?? ?? ?? 8B D0",
            address => _callTutorialFlowHook =
                hooks.CreateHook<CallTutorialFlow>(CallTutorialImpl, address).Activate());
        AddressScanner.DelayedScanPattern(
            "B8 6D 01 00 00 66 3B D0 7D ?? 0F B6 C9",
            address => _netSetActionHook = hooks.CreateHook<NetSetAction>(NetSetActionImpl, address).Activate());
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