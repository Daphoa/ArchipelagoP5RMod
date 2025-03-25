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
    public static IntPtr OpenChestOnUpdateFuncAddress { get; private set; }
    public static IntPtr StartOpenChestFuncAddress { get; private set; }
    public static IntPtr OnCompleteOpenChestFuncAddress { get; private set; }
    public static IntPtr GetItemNameFuncAddress { get; private set; }
    public static IntPtr GetTboxFlagFlowFuncAddress { get; private set; }
    public static IntPtr SetItemNumFuncAddress { get; private set; }
    public static IntPtr GetItemNumFuncAddress { get; private set; }
    public static IntPtr GetItemWindowFuncAddress { get; private set; }
    public static IntPtr GetItemWindowFlowFuncAddress { get; private set; }
    public static IntPtr RunFlowFuncFromFileAddress { get; private set; }
    public static IntPtr CallTutorialFlowFuncAddress { get; private set; }
    public static IntPtr NetSetActionFuncAddress { get; private set; }
    public static IntPtr CmmCheckEnableFuncAddress { get; private set; }
    public static IntPtr CmmSetLvFuncAddress { get; private set; }
    public static IntPtr AppStorageReadFuncAddress { get; private set; }
    public static IntPtr AppStorageWriteFuncAddress { get; private set; }

    //Debug
    public static IntPtr GetFlowscriptInt4ArgAddress { get; private set; }

    public static unsafe DateInfo* DateInfoAddress => *_dateInfoRefAddress;
    public static unsafe BitFlagArrayInfo* BitFlagSectionMap { get; private set; }

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

            OpenChestOnUpdateFuncAddress = FindAsmMethod(scanner, "48 8B C4 48 89 58 ?? 48 89 48 ?? 55 56 57 41 54 41 " +
                                                          "55 41 56 41 57 48 8D A8 ?? ?? ?? ?? 48 81 EC 70 08 00 00");
            
            StartOpenChestFuncAddress = FindAsmMethod(scanner, "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 " +
                                                               "41 56 41 57 48 83 EC 60 48 8B FA 4C 8B F9");
            
            GetTboxFlagFlowFuncAddress = FindAsmMethod(scanner, "48 83 EC 28 E8 ?? ?? ?? ?? 48 85 C0 74 ?? 4C 8B " +
                                                                "48 ?? 4D 85 C9 74 ?? 49 8B 91 ?? ?? ?? ??");

            SetItemNumFuncAddress = FindAsmMethod(scanner, "4C 8B DC 49 89 5B ?? 57 48 83 EC 70 48 8D 05 ?? ?? ?? ??");
            
            GetItemWindowFuncAddress = FindAsmMethod(scanner, "48 8B C4 48 81 EC B8 00 00 00 48 89 58 ??");
        
            GetItemWindowFlowFuncAddress = FindAsmMethod(scanner, "48 83 EC 28 33 C9 E8 ?? ?? ?? ?? B9 01 00 00 " +
                                                                  "00 44 8B C8 E8 ?? ?? ?? ?? B9 02 00 00 00 44 8B D0 " +
                                                                  "E8 ?? ?? ?? ?? 48 8B 0D ?? ?? ?? ?? 48 85 C9 74 ?? " +
                                                                  "83 B9 ?? ?? ?? ?? 00 74 ?? 33 C0");
            
            CallTutorialFlowFuncAddress = FindAsmMethod(scanner, "48 83 EC 28 48 8B 05 ?? ?? ?? ?? 48 85 C0 74 " +
                                                                 "?? 83 B8 ?? ?? ?? ?? 00 74 ?? 48 8D 0D ?? ?? ?? ?? E8 " +
                                                                 "?? ?? ?? ?? 48 85 C0 75 ?? B8 01 00 00 00 48 83 C4 28 " +
                                                                 "C3 B9 01 00 00 00 E8 ?? ?? ?? ?? 33 C9 44 8B C8 E8 ?? ?? ?? ?? 8B D0");

            RunFlowFuncFromFileAddress = FindAsmMethod(scanner, "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? " +
                                                                "48 89 7C 24 ?? 41 56 48 83 EC 60 41 89 CE");

            NetSetActionFuncAddress = FindAsmMethod(scanner, "B8 6D 01 00 00 66 3B D0 7D ?? 0F B6 C9");

            CmmCheckEnableFuncAddress = FindAsmMethod(scanner, "40 53 55 56 41 54 41 56 48 83 EC 20");
            
            CmmSetLvFuncAddress = FindAsmMethod(scanner, "66 85 C9 0F 84 ?? ?? ?? ?? 57");
            
            AppStorageReadFuncAddress = FindAsmMethod(scanner, "48 89 5C 24 ?? 57 48 83 EC 60 89 CB");
            
            AppStorageWriteFuncAddress = FindAsmMethod(scanner, "48 89 5C 24 ?? 57 48 83 EC 60 8B D9 0F B7 FA");

            // DEBUG
            GetFlowscriptInt4ArgAddress = FindAsmMethod(scanner, "4C 8B 05 ?? ?? ?? ?? 41 8B 50 ?? 29 CA");

            // var dateInfoPointer = (DateInfo***)(GetTimeFunAddress + 3);

            // TODO get these from sig searching instead
            _dateInfoRefAddress = (DateInfo**)(_baseAddress + 0x286c188);
            _flowCommanderDataRefAddress = (FlowCommandData**)(_baseAddress + 0x293d008);
            BitFlagSectionMap = (BitFlagArrayInfo*)(_baseAddress + 0x2511310);
            GetItemNameFuncAddress = _baseAddress + 0xd68530;
            OnCompleteOpenChestFuncAddress = _baseAddress + 0x102cdd0;
            GetItemNumFuncAddress = _baseAddress + 0xd68720;
        }
    }
    
    // DEBUG
    private static void findOffsets()
    {
        
    }

    private static IntPtr FindAsmMethod(Scanner scanner, string pattern)
    {
        var result = scanner.FindPattern(pattern);
        if (!result.Found)
            throw new Exception("Signature for function not found.");

        return _baseAddress + result.Offset;
    }
}