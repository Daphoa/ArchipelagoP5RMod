using System.Diagnostics;
using System.Runtime.InteropServices;
using ArchipelagoP5RMod.Types;
using Reloaded.Memory.Sigscan;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;

namespace ArchipelagoP5RMod;

public static class AddressScanner
{
    public static IntPtr NextTimeFunAddress { get; private set; }
    public static IntPtr GetTimeFunAddress { get; private set; }
    public static IntPtr UpdateCurrentTotalDaysAddress { get; private set; }
    public static IntPtr AdvanceToNextDayAddress { get; private set; }
    public static IntPtr UnknownTimeAdvanceFuncAddress { get; private set; }
    public static IntPtr ChkBitFuncAddress { get; private set; }
    public static IntPtr BitOnFlowFuncAddress { get; private set; }
    public static IntPtr BitOffFlowFuncAddress { get; private set; }

    //Debug
    public static IntPtr GetFlowscriptInt4ArgAddress { get; private set; }

    public static unsafe DateInfo* DateInfoAddress => *_dateInfoRefAddress;

    public static unsafe FlowCommandData* FlowCommandDataAddress
    {
        get => *_flowCommanderDataRefAddress;
        set => *_flowCommanderDataRefAddress = value;
    }

    public static unsafe bool IsAdrNullPointer => (IntPtr)_flowCommanderDataRefAddress == IntPtr.Zero;

    private static unsafe DateInfo** _dateInfoRefAddress;
    private static unsafe FlowCommandData** _flowCommanderDataRefAddress;

    private static IntPtr _baseAddress;
    private static int _exeSize;

    public static void Scan(ILogger? logger = null)
    {
        var thisProcess = Process.GetCurrentProcess();

        if (thisProcess.MainModule == null)
        {
            throw new InvalidOperationException("The process cannot be found.");
        }

        _baseAddress = thisProcess.MainModule.BaseAddress;
        _exeSize = thisProcess.MainModule.ModuleMemorySize;

        logger?.WriteLine($"Program range: {_baseAddress:X} - {_baseAddress + _exeSize:X}");
        unsafe
        {
            var scanner = new Scanner((byte*)_baseAddress, _exeSize);

            NextTimeFunAddress = FindAsmMethod(scanner, "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B 05 " +
                                                        "?? ?? ?? ?? 33 F6 8B DE");

            //
            // var dateInfoPointer = (DateInfo**)(NextTimeFunAddress + 18);
            //
            // _dateInfoRefAddress = **dateInfoPointer;

            GetTimeFunAddress = FindAsmMethod(scanner, "48 8B 05 ?? ?? ?? ?? 48 8B 15 ?? ?? ?? ?? 0F B6 48 ??");

            UpdateCurrentTotalDaysAddress = FindAsmMethod(scanner, "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC " +
                                                                   "20 0F B7 D9 33 C9");

            AdvanceToNextDayAddress = FindAsmMethod(scanner, "40 53 48 83 EC 20 80 3D ?? ?? ?? ?? 00 48 8B D9 " +
                                                             "48 8B 15 ?? ?? ?? ??");

            UnknownTimeAdvanceFuncAddress = FindAsmMethod(scanner, "40 53 48 83 EC 60 48 8B 59 ?? 48 63 03");

            ChkBitFuncAddress = FindAsmMethod(scanner, "4C 8D 05 ?? ?? ?? ?? 33 C0 49 8B D0 0F 1F 40 00 39 0A " +
                                                       "74 ?? FF C0 48 83 C2 08 83 F8 10 72 ?? 8B D1");

            BitOnFlowFuncAddress = FindAsmMethod(scanner, "40 53 48 83 EC 20 31 C9 E8 ?? ?? ?? ?? B9 01 00 00 00");

            BitOffFlowFuncAddress = FindAsmMethod(scanner, "40 53 48 83 EC 20 31 C9 E8 ?? ?? ?? ?? 31 C9 89 C3");

            // DEBUG
            GetFlowscriptInt4ArgAddress = FindAsmMethod(scanner, "4C 8B 05 ?? ?? ?? ?? 41 8B 50 ?? 29 CA");

            // var dateInfoPointer = (DateInfo***)(GetTimeFunAddress + 3);

            // TODO get these from sig searching instead
            _dateInfoRefAddress = (DateInfo**)(_baseAddress + 0x286c188);
            _flowCommanderDataRefAddress = (FlowCommandData**)(_baseAddress + 0x293d008);
        }
    }

    private static IntPtr FindAsmMethod(Scanner scanner, string pattern)
    {
        var result = scanner.FindPattern(pattern);
        if (!result.Found)
            throw new Exception("Signature for function not found.");

        return _baseAddress + result.Offset;
    }
}