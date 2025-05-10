using Reloaded.Hooks.Definitions;

namespace ArchipelagoP5RMod.GameCommunicators;

public class SocialStatManipulator
{
    private FlowFunctionWrapper.BasicFlowFunc _addPcAllParamFlowWrapper;
    
    private IntPtr _addPcAllParamFlowAdr;

    public SocialStatManipulator(IReloadedHooks hooks)
    {
        AddressScanner.DelayedScanPattern(
            "40 53 55 41 57 48 81 EC D0 00 00 00",
            address => _addPcAllParamFlowWrapper = 
                hooks.CreateWrapper<FlowFunctionWrapper.BasicFlowFunc>(address, out _addPcAllParamFlowAdr));
    }

    public void AddPcAllParam(int knowledge, int charm, int kindness, int guts, int proficiency)
    {
        FlowFunctionWrapper.CallFlowFunctionSetup(knowledge, charm, proficiency, guts, kindness);

        _addPcAllParamFlowWrapper();
        
        FlowFunctionWrapper.CallFlowFunctionCleanup();
    }
}