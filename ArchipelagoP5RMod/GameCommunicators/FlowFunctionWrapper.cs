using System.Diagnostics;
using System.Runtime.InteropServices;
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
    private static GCHandle temporaryCommandDataHandle;
    // private static int addedStack = 0;

    [Function(CallingConventions.Fastcall)]
    private delegate int GetFlowscriptInt4ArgType(byte paramIndex);
    
    [Function(CallingConventions.Fastcall)]
    private delegate IntPtr RunFlowFuncFromFileType(int param1, IntPtr file, uint fileSize, uint funcIndex);

    private static GetFlowscriptInt4ArgType? GetFlowscriptInt4Arg { get; set; }
    private static RunFlowFuncFromFileType? RunFlowFuncFromFile { get; set; }

    private static IntPtr _getFlowscriptInt4ArgPtr;
    private static IntPtr _runFlowFuncFromFilePtr;

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

        RunFlowFuncFromFile =
            hooks.CreateWrapper<RunFlowFuncFromFileType>(AddressScanner.RunFlowFuncFromFileAddress,
                out _runFlowFuncFromFilePtr);
    }

    public static unsafe void CallFlowFunctionSetup(params long[] args)
    {
        backupCommandData = AddressScanner.FlowCommandDataAddress;

        temporaryCommandDataHandle = GCHandle.Alloc(new FlowCommandData(), GCHandleType.Pinned);
        AddressScanner.FlowCommandDataAddress = (FlowCommandData*)temporaryCommandDataHandle.AddrOfPinnedObject();

        AddressScanner.FlowCommandDataAddress->StackSize = 1;

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
        long retVal = AddressScanner.FlowCommandDataAddress->ReturnValue;

        AddressScanner.FlowCommandDataAddress = backupCommandData;
        temporaryCommandDataHandle.Free();

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

            long testRetVal = random.NextInt64();
            unsafe
            {
                ((FlowCommandData*)temporaryCommandDataHandle.AddrOfPinnedObject())->ReturnValue = testRetVal;
            }

            long actualRetVal = CallFlowFunctionCleanup();

            if (testRetVal != actualRetVal)
            {
                success = false;
                _logger?.WriteLine(
                    $"Test {testNum} failed on retVal. Expected value ({testRetVal:X}) is not equal to actual value ({actualRetVal:X}).");
            }
        }

        return success;
    }

    public static unsafe void CallCustomFlowFunction(ApMethodsIndexes func)
    {
        if (RunFlowFuncFromFile is null)
        {
            throw new NullReferenceException("RunFlowFuncFromFile is null");
        }
        RunFlowFuncFromFile(8, (IntPtr)BfLoader.ApMethodsBfFilePointer, BfLoader.ApMethodsBfFileLength, (uint)func);
    }
}