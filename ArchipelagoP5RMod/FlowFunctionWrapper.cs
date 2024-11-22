using System.Diagnostics;
using ArchipelagoP5RMod.Types;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Mod.Interfaces;

namespace ArchipelagoP5RMod;

public static class FlowFunctionWrapper
{
    private static ILogger? _logger;

    public delegate void BasicFlowFunc();

    public delegate void BitToggleType();

    private static unsafe FlowCommandData* backupCommandData;
    private static FlowCommandData temporaryCommandData;
    // private static int addedStack = 0;

    [Function(CallingConventions.Fastcall)]
    private delegate int GetFlowscriptInt4ArgType(byte paramIndex);

    private static GetFlowscriptInt4ArgType? GetFlowscriptInt4Arg { get; set; }

    private static IntPtr _getFlowscriptInt4ArgPtr;

    public static void SetLogger(ILogger logger)
    {
        _logger = logger;

        logger.WriteLine("FlowFunctionWrapper created");
    }

    public static void Setup(IReloadedHooks hooks)
    {
        GetFlowscriptInt4Arg =
            hooks.CreateWrapper<GetFlowscriptInt4ArgType>(AddressScanner.GetFlowscriptInt4ArgAddress,
                out _getFlowscriptInt4ArgPtr);
    }

    // public static unsafe long CallFlowFunction<T>(Func<T>  func, params long[] args)
    // {
    //     var flowCommandDataAddress = CallFlowFunctionSetup(args, out var backupCommandData);
    //
    //     // Call function
    //     func.Invoke();
    //
    //     long retVal = flowCommandDataAddress->ReturnValue;
    //
    //     CallFlowFunctionCleanup(flowCommandDataAddress, backupCommandData);
    //
    //     return retVal;
    // }

    // public static unsafe long CallFlowFunction<T>(T func, params long[] args) where T: Delegate
    // {
    //     var flowCommandDataAddress = CallFlowFunctionSetup(args, out var backupCommandData);
    //
    //     // Call function
    //     func.Invoke();
    //
    //     long retVal = flowCommandDataAddress->ReturnValue;
    //
    //     CallFlowFunctionCleanup(flowCommandDataAddress, backupCommandData);
    //
    //     return retVal;
    // }


    public static unsafe void CallFlowFunctionSetup(params long[] args)
    {
        // _logger?.WriteLine($"Got flowCommandDataAddress {(int)AddressScanner.FlowCommandDataAddress}");

        backupCommandData = AddressScanner.FlowCommandDataAddress;
        // _logger?.WriteLine($"Backed up command data adr {(int)backupCommandData:X}");

        temporaryCommandData = new FlowCommandData();
        fixed (FlowCommandData* adr = &temporaryCommandData)
        {
            AddressScanner.FlowCommandDataAddress = adr;
        }

        AddressScanner.FlowCommandDataAddress->StackSize = 1;
        // _logger?.WriteLine($"Set stack size to {AddressScanner.FlowCommandDataAddress->StackSize}");

        int newStackSize = AddressScanner.FlowCommandDataAddress->StackSize + args.Length;
        if (newStackSize > 47)
        {
            _logger?.WriteLine(
                $"ERROR: trying to push flow command call data stack to {newStackSize} over maximum size 47");
            return;
        }

        AddressScanner.FlowCommandDataAddress->StackSize = newStackSize;
        for (var i = 0; i < args.Length; i++)
        {
            AddressScanner.FlowCommandDataAddress->ArgData[newStackSize - i - 1] = args[i];
            AddressScanner.FlowCommandDataAddress->ArgTypes[newStackSize - i - 1] = (byte)FlowParamType.Int;
        }
    }

    public static unsafe long CallFlowFunctionCleanup()
    {
        _logger?.WriteLine($"Beginning Flow Function cleanup");
        long retVal = AddressScanner.FlowCommandDataAddress->ReturnValue;

        AddressScanner.FlowCommandDataAddress = backupCommandData;
        // flowCommandDataAddress->StackSize -= addedStack;
        // addedStack = 0;

        return retVal;
    }

    public static bool TestFlowscriptWrapper(int totalTests = 10)
    {
        Debug.Assert(GetFlowscriptInt4Arg != null, nameof(GetFlowscriptInt4Arg) + " != null");
        var random = new Random();
        var success = true;
        for (var testNum = 0; testNum < totalTests; testNum++)
        {
            int paramNum = random.Next() % 3 + 1;
            var parameters = new long[paramNum];
            for (var i = 0; i < paramNum; i++)
            {
                parameters[i] = random.Next();
            }

            CallFlowFunctionSetup(parameters);

            for (byte i = 0; i < paramNum; i++)
            {
                bool thisTestSuccess = parameters[i] == GetFlowscriptInt4Arg(i);
                if (!thisTestSuccess)
                {
                    success = false;
                    _logger?.WriteLine(
                        $"Test {testNum} failed on {i}. {GetFlowscriptInt4Arg(i)} is not equal to parameters[{i}]: {parameters[i]}.");
                }
            }

            CallFlowFunctionCleanup();
        }

        return success;
    }
}