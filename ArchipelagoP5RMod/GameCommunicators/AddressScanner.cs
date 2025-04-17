using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ArchipelagoP5RMod.Types;
using Reloaded.Memory.Sigscan;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;

namespace ArchipelagoP5RMod;

public static class AddressScanner
{
    public enum AddressName
    {
        NextTimeFunAddress,
        GetTimeFunAddress,
        UpdateCurrentTotalDaysAddress,
        AdvanceToNextDayAddress,
        UnknownTimeAdvanceFuncAddress,
        ChkBitFuncAddress,
        BitOnFlowFuncAddress,
        BitOffFlowFuncAddress,
        GetCountFlowFuncAddress,
        SetCountFlowFuncAddress,
        OpenChestOnUpdateFuncAddress,
        StartOpenChestFuncAddress,
        OnCompleteOpenChestFuncAddress,
        GetItemNameFuncAddress,
        GetTboxFlagFlowFuncAddress,
        SetItemNumFuncAddress,
        GetItemNumFuncAddress,
        GetItemWindowFuncAddress,
        GetItemWindowFlowFuncAddress,
        RunFlowFuncFromFileAddress,
        ExecuteFlowFuncOnUpdate,
        CallTutorialFlowFuncAddress,
        NetSetActionFuncAddress,
        CmmCheckEnableFuncAddress,
        CmmSetLvFuncAddress,
        AppStorageReadFuncAddress,
        AppStorageWriteFuncAddress,
        BitFlagSectionMap,
        SequenceObjAddress,

        //Debug
        GetFlowscriptInt4ArgAddress,

        // Manual
    }

    private class AddressInfo(AddressName name, string pattern)
    {
        public AddressName Name { get; set; } = name;
        public string Pattern { get; set; } = pattern;
    }

    private static readonly List<AddressInfo> _addressInfos =
    [
        new(AddressName.NextTimeFunAddress,
            "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B 05 ?? ?? ?? ?? 33 F6 8B DE"),
        new(AddressName.GetTimeFunAddress, "48 8B 05 ?? ?? ?? ?? 48 8B 15 ?? ?? ?? ?? 0F B6 48 ??"),
        new(AddressName.UpdateCurrentTotalDaysAddress,
            "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 0F B7 D9 33 C9"),
        new(AddressName.AdvanceToNextDayAddress,
            "40 53 48 83 EC 20 80 3D ?? ?? ?? ?? 00 48 8B D9 48 8B 15 ?? ?? ?? ??"),
        new(AddressName.UnknownTimeAdvanceFuncAddress, "40 53 48 83 EC 60 48 8B 59 ?? 48 63 03"),
        new(AddressName.ChkBitFuncAddress,
            "4C 8D 05 ?? ?? ?? ?? 33 C0 49 8B D0 0F 1F 40 00 39 0A 74 ?? FF C0 48 83 C2 08 83 F8 10 72 ?? 8B D1"),
        new(AddressName.BitOnFlowFuncAddress, "40 53 48 83 EC 20 31 C9 E8 ?? ?? ?? ?? B9 01 00 00 00"),
        new(AddressName.BitOffFlowFuncAddress, "40 53 48 83 EC 20 31 C9 E8 ?? ?? ?? ?? 31 C9 89 C3"),
        new(AddressName.GetCountFlowFuncAddress, "48 83 EC 28 31 C9 E8 ?? ?? ?? ?? 4C 8D 05 ?? ?? ?? ??"),
        new(AddressName.SetCountFlowFuncAddress, "48 83 EC 28 31 C9 E8 ?? ?? ?? ?? B9 01 00 00 00 4C 63 C8"),
        new(AddressName.OpenChestOnUpdateFuncAddress,
            "48 8B C4 48 89 58 ?? 48 89 48 ?? 55 56 57 41 54 41 55 41 56 41 57 48 8D A8 ?? ?? ?? ?? 48 81 EC 70 08 00 00"),
        new(AddressName.StartOpenChestFuncAddress,
            "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 41 56 41 57 48 83 EC 60 48 8B FA 4C 8B F9"),
        new(AddressName.GetTboxFlagFlowFuncAddress,
            "48 83 EC 28 E8 ?? ?? ?? ?? 48 85 C0 74 ?? 4C 8B 48 ?? 4D 85 C9 74 ?? 49 8B 91 ?? ?? ?? ??"),
        new(AddressName.SetItemNumFuncAddress, "4C 8B DC 49 89 5B ?? 57 48 83 EC 70 48 8D 05 ?? ?? ?? ??"),
        new(AddressName.GetItemWindowFlowFuncAddress,
            "48 83 EC 28 33 C9 E8 ?? ?? ?? ?? B9 01 00 00 00 44 8B C8 E8 ?? ?? ?? ?? B9 02 00 00 00 44 8B D0 E8 ?? ?? ?? ?? 48 8B 0D ?? ?? ?? ?? 48 85 C9 74 ?? 83 B9 ?? ?? ?? ?? 00 74 ?? 33 C0"),
        new(AddressName.GetItemWindowFuncAddress,
            "48 8B C4 48 81 EC B8 00 00 00 48 89 58 ??"),
        new(AddressName.CallTutorialFlowFuncAddress,
            "48 83 EC 28 48 8B 05 ?? ?? ?? ?? 48 85 C0 74 ?? 83 B8 ?? ?? ?? ?? 00 74 ?? 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 85 C0 75 ?? B8 01 00 00 00 48 83 C4 28 C3 B9 01 00 00 00 E8 ?? ?? ?? ?? 33 C9 44 8B C8 E8 ?? ?? ?? ?? 8B D0"),
        new(AddressName.RunFlowFuncFromFileAddress,
            "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 41 56 48 83 EC 60 41 89 CE"),
        new(AddressName.ExecuteFlowFuncOnUpdate, "48 83 EC 28 48 8B 49 ?? E8 ?? ?? ?? ?? 83 E0 FD"),
        new(AddressName.NetSetActionFuncAddress, "B8 6D 01 00 00 66 3B D0 7D ?? 0F B6 C9"),
        new(AddressName.CmmCheckEnableFuncAddress, "40 53 55 56 41 54 41 56 48 83 EC 20"),
        new(AddressName.CmmSetLvFuncAddress, "66 85 C9 0F 84 ?? ?? ?? ?? 57"),
        new(AddressName.AppStorageReadFuncAddress, "48 89 5C 24 ?? 57 48 83 EC 60 89 CB"),
        new(AddressName.AppStorageWriteFuncAddress, "48 89 5C 24 ?? 57 48 83 EC 60 8B D9 0F B7 FA"),
        new(AddressName.GetFlowscriptInt4ArgAddress, "4C 8B 05 ?? ?? ?? ?? 41 8B 50 ?? 29 CA")
    ];

    public static FrozenDictionary<AddressName, IntPtr> Addresses { get; private set; } =
        FrozenDictionary<AddressName, IntPtr>.Empty;

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
            var foundAddresses = new Dictionary<AddressName, IntPtr>();

            // Do the search for all results in parallel.
            var results = scanner.FindPatterns(_addressInfos.ConvertAll(x => x.Pattern).ToArray());

            for (var i = 0; i < results.Length; i++)
            {
                var name = _addressInfos[i].Name;
                var result = results[i];
                if (!result.Found)
                    throw new Exception($"Signature for function {name} not found.");

                foundAddresses.Add(name, _baseAddress + result.Offset);
            }

            // var dateInfoPointer = (DateInfo***)(GetTimeFunAddress + 3);

            // TODO get these from sig searching instead
            _dateInfoRefAddress = (DateInfo**)(_baseAddress + 0x286c188);
            _flowCommanderDataRefAddress = (FlowCommandData**)(_baseAddress + 0x293d008);
            foundAddresses.Add(AddressName.BitFlagSectionMap, _baseAddress + 0x2511310);
            foundAddresses.Add(AddressName.GetItemNameFuncAddress, _baseAddress + 0xd68530);
            foundAddresses.Add(AddressName.OnCompleteOpenChestFuncAddress, _baseAddress + 0x102cdd0);
            foundAddresses.Add(AddressName.GetItemNumFuncAddress, _baseAddress + 0xd68720);
            foundAddresses.Add(AddressName.SequenceObjAddress, _baseAddress + 0x2902558);

            Addresses = foundAddresses.ToFrozenDictionary();
        }
    }
}