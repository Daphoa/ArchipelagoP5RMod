using System.Diagnostics;
using System.Runtime.InteropServices;
using ArchipelagoP5RMod.Types;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;

namespace ArchipelagoP5RMod;

public static class FlowFunctionWrapper
{
    [Function(CallingConventions.Fastcall)]
    public delegate uint FlowFuncDelegate4();
    [Function(CallingConventions.Fastcall)]
    public delegate ulong FlowFuncDelegate8();
    
    [Function(CallingConventions.Fastcall)]
    public unsafe delegate long OnUpdateDelegate(GameTask* eventInfo);


    public delegate void BasicFlowFunc();

    public delegate void BitToggleType();

    private static unsafe FlowCommandData* backupCommandData;
    private static GCHandle temporaryCommandDataHandle;
    // private static int addedStack = 0;

    [Function(CallingConventions.Fastcall)]
    public delegate int GetFlowscriptInt4ArgType(byte paramIndex);

    [Function(CallingConventions.Fastcall)]
    private delegate IntPtr RunFlowFuncFromFileType(int param1, IntPtr file, uint fileSize, uint funcIndex);

    public static GetFlowscriptInt4ArgType? GetFlowscriptInt4Arg { get; set; }
    private static RunFlowFuncFromFileType? RunFlowFuncFromFile { get; set; }

    private static IntPtr _getFlowscriptInt4ArgPtr;
    private static IntPtr _runFlowFuncFromFilePtr;

    private static IHook<OnUpdateDelegate> _onFlowUpdateDelegate;
    
    public static unsafe FlowCommandData* FlowCommandDataAddress
    {
        get => *_flowCommanderDataRefAddress;
        set => *_flowCommanderDataRefAddress = value;
    }

    public static unsafe bool IsAdrNullPointer => (IntPtr)_flowCommanderDataRefAddress == IntPtr.Zero;

    private static unsafe FlowCommandData** _flowCommanderDataRefAddress;

    public static void Setup(IReloadedHooks hooks)
    {
        AddressScanner.DelayedScanPattern(
            "4C 8B 05 ?? ?? ?? ?? 41 8B 50 ?? 29 CA",
            address => GetFlowscriptInt4Arg =
                hooks.CreateWrapper<GetFlowscriptInt4ArgType>(address, out _getFlowscriptInt4ArgPtr));
        AddressScanner.DelayedScanPattern(
            "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 41 56 48 83 EC 60 41 89 CE",
            address => RunFlowFuncFromFile =
                hooks.CreateWrapper<RunFlowFuncFromFileType>(address, out _runFlowFuncFromFilePtr));

        unsafe
        {
            // AddressScanner.DelayedScanPattern(
            //     "48 83 EC 28 48 8B 49 ?? E8 ?? ?? ?? ?? 83 E0 FD",
            //     address => _onFlowUpdateDelegate =
            //         hooks.CreateHook<OnUpdateDelegate>(FlowOnUpdateImpl, address).Activate());

            AddressScanner.DelayedAddressHack(0x293d008,
                address => _flowCommanderDataRefAddress = (FlowCommandData**)address);
        }
    }

    private static unsafe long FlowOnUpdateImpl(GameTask* eventInfo)
    {
        // _logger.WriteLine("Called Flow onUpdate");
        var flowCommandData = (FlowCommandData*)eventInfo->args;
        // _logger.WriteLine($"Got the args from the event {(IntPtr)flowCommandData}");
        // _logger.WriteLine($"Func name => {(IntPtr)flowCommandData->CurrFuncName}");
        // _logger?.Write($"FlowFunctionWrapper flow function called with method \"");
        // for (int i = 0; i < 40; i++)
        // {
        //     _logger.Write(flowCommandData->CurrFuncName[i].ToString());
        // }
        // _logger?.WriteLine("\"");

        // var funcName = new string(flowCommandData->CurrFuncName);
        // _logger?.WriteLine($"FlowFunctionWrapper flow function called with method {funcName}");

        return _onFlowUpdateDelegate.OriginalFunction(eventInfo);
    }

    public static unsafe void ReplaceArgInt4(int index, int newValue)
    {
        var stackIndex = FlowCommandDataAddress->StackSize - index - 1;
        FlowCommandDataAddress->ArgData[stackIndex] = newValue;
    }

    public static unsafe void CallFlowFunctionSetup(params long[] args)
    {
        backupCommandData = FlowCommandDataAddress;

        temporaryCommandDataHandle = GCHandle.Alloc(new FlowCommandData(), GCHandleType.Pinned);
        FlowCommandDataAddress = (FlowCommandData*)temporaryCommandDataHandle.AddrOfPinnedObject();

        FlowCommandDataAddress->StackSize = 1;

        int newStackSize = FlowCommandDataAddress->StackSize + args.Length;
        if (newStackSize > 47)
        {
            MyLogger.DebugLog(
                $"ERROR: trying to push flow command call data stack to {newStackSize} over maximum size 47");
            return;
        }

        FlowCommandDataAddress->StackSize = newStackSize;
        for (int i = 0; i < args.Length; i++)
        {
            FlowCommandDataAddress->ArgData[newStackSize - i - 1] = args[i];
            FlowCommandDataAddress->ArgTypes[newStackSize - i - 1] = (byte)FlowParamType.Int;
        }
    }

    public static unsafe long CallFlowFunctionCleanup()
    {
        long retVal = FlowCommandDataAddress->ReturnValue;

        FlowCommandDataAddress = backupCommandData;
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
                    MyLogger.DebugLog(
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
                MyLogger.DebugLog(
                    $"Test {testNum} failed on retVal. Expected value ({testRetVal:X}) is not equal to actual value ({actualRetVal:X}).");
            }
        }

        return success;
    }

    public static unsafe IntPtr CallCustomFlowFunction(CustomApMethodsIndexes func)
    {
        if (RunFlowFuncFromFile is null)
        {
            throw new NullReferenceException("RunFlowFuncFromFile is null");
        }

        return RunFlowFuncFromFile(8, (IntPtr)BfLoader.ApMethodsBfFilePointer, BfLoader.ApMethodsBfFileLength, (uint)func);
    }
}