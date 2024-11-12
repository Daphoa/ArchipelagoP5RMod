using System.Diagnostics;
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
    
    public static unsafe DateInfo* DateInfoAddress => *_dateInfoRefAddress;

    private static unsafe DateInfo** _dateInfoRefAddress;

    private static IntPtr _baseAddress;
    private static int _exeSize;

    public static void Scan(ILogger? logger = null)
    {
        var thisProcess = Process.GetCurrentProcess();
        
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

            // var dateInfoPointer = (DateInfo***)(GetTimeFunAddress + 3);

            // TODO get this from sig searching instead
            _dateInfoRefAddress = (DateInfo**)(_baseAddress + 0x286c188);
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