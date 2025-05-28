using Reloaded.Hooks.Definitions;

namespace ArchipelagoP5RMod.GameCommunicators;

public class MiscManipulator
{
    private IntPtr _fldVltFilterVisibleFlowAdr;

    FlowFunctionWrapper.BasicFlowFunc _fldVltFilterVisibleFlow;

    public MiscManipulator(IReloadedHooks hooks)
    {
        AddressScanner.DelayedScanPattern(
            "48 83 EC 68 33 C9 E8 ?? ?? ?? ?? 85 C0 0F 84 ?? ?? ?? ??",
            address => _fldVltFilterVisibleFlow =
                hooks.CreateWrapper<FlowFunctionWrapper.BasicFlowFunc>(address, out _fldVltFilterVisibleFlowAdr));
    }

    public void VltFilterVisibleFlow(bool value)
    {
        FlowFunctionWrapper.CallFlowFunctionSetup(value ? 1 : 0);

        _fldVltFilterVisibleFlow.Invoke();
        
        FlowFunctionWrapper.CallFlowFunctionCleanup();
    }
}