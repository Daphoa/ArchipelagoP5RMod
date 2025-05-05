using System.Runtime.InteropServices;
using System.Text;
using ArchipelagoP5RMod.GameCommunicators;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Mod.Interfaces;

namespace ArchipelagoP5RMod;

public class ItemManipulator
{
    private readonly ILogger _logger;
    private readonly FlagManipulator _flagManipulator;

    private const long DUMMY_ITEM = 0;

    private IHook<OpenChestOnUpdate> _openChestHook;
    private IHook<StartOpenChest> _startOpenChestHook;
    private IHook<OnCompleteOpenChest> _onCompleteOpenChestHook;
    private IHook<GetItemName> _getItemNameHook;
    private IHook<GetItemNumFunc> _getItemNumHook;
    private IHook<SetItemNumFunc> _setItemNumHook;

    private IntPtr _getTboxFlagFlowAdr;
    private IntPtr _getItemWindowAdr;
    private IntPtr _getItemWindowFlowAdr;

    private GCHandle _itemNameOverrideAdr;
    private long? _currChestFlag = null;

    private readonly uint[] BLANK_ITEMS = [0x0000, 0x1000, 0x2000, 0x3000, 0x4000, 0x5000, 0x6000, 0x7000, 0x8000];

    [Function(CallingConventions.Fastcall)]
    private unsafe delegate long OpenChestOnUpdate(int* param1, float param2, long param3, float param4);

    [Function(CallingConventions.Fastcall)]
    private delegate IntPtr StartOpenChest(IntPtr param1, long param2, ushort param3);

    [Function(CallingConventions.Fastcall)]
    private delegate void OnCompleteOpenChest(long param1, IntPtr param2, long param3, int param4);

    [Function(CallingConventions.Fastcall)]
    private unsafe delegate char* GetItemName(ushort itemId);

    [Function(CallingConventions.Fastcall)]
    private delegate long GetTboxFlagFlow();

    [Function(CallingConventions.Fastcall)]
    private delegate byte GetItemNumFunc(ushort itemId);

    [Function(CallingConventions.Fastcall)]
    private delegate void SetItemNumFunc(ushort itemId, byte newItemCount, byte shouldUpdateRecentItem);

    [Function(CallingConventions.Fastcall)]
    private unsafe delegate IntPtr GetItemWindowFunc(short* itemIds, int* itemNum, uint length, int flag);

    public delegate void OnItemCountChangedEvent(ushort itemId, byte itemNum);

    public event OnItemCountChangedEvent OnItemCountChanged = (_, _) => { };

    private delegate bool GetItemWindowFlow();

    private GetTboxFlagFlow? _getTboxFlag { get; set; }
    private GetItemWindowFunc? _getItemWindow { get; set; }
    private GetItemWindowFlow? _getItemWindowFlow { get; set; }

    public event OnChestOpenedEvent OnChestOpened;
    public event OnChestOpenedEvent OnChestOpenedCompleted;

    public delegate void OnChestOpenedEvent(long chestId);

    public unsafe ItemManipulator(FlagManipulator flagManipulator, IReloadedHooks hooks, ILogger logger)
    {
        _flagManipulator = flagManipulator;
        _logger = logger;
        AddressScanner.DelayedScanPattern(
            "48 8B C4 48 89 58 ?? 48 89 48 ?? 55 56 57 41 54 41 55 41 56 41 57 48 8D A8 ?? ?? ?? ?? 48 81 EC 70 08 00 00",
            address => _openChestHook =
                hooks.CreateHook<OpenChestOnUpdate>(OpenChestOnUpdateImpl, address).Activate());
        AddressScanner.DelayedScanPattern(
            "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 41 56 41 57 48 83 EC 60 48 8B FA 4C 8B F9",
            address => _startOpenChestHook =
                hooks.CreateHook<StartOpenChest>(StartOpenChestImpl, address).Activate());
        AddressScanner.DelayedAddressHack(
            0x102cdd0,
            address => _onCompleteOpenChestHook =
                hooks.CreateHook<OnCompleteOpenChest>(OnCompleteOpenChestImpl, address).Activate());
        AddressScanner.DelayedAddressHack(
            0xd68530,
            address => _getItemNameHook = hooks.CreateHook<GetItemName>(GetItemNameImpl, address).Activate());
        AddressScanner.DelayedAddressHack(
            0xd68720,
            address => _getItemNumHook = hooks.CreateHook<GetItemNumFunc>(GetItemNumImpl, address).Activate());
        AddressScanner.DelayedScanPattern(
            "4C 8B DC 49 89 5B ?? 57 48 83 EC 70 48 8D 05 ?? ?? ?? ??",
            address => _setItemNumHook = hooks.CreateHook<SetItemNumFunc>(SetItemNumImpl, address).Activate());
        AddressScanner.DelayedScanPattern(
            "48 83 EC 28 E8 ?? ?? ?? ?? 48 85 C0 74 ?? 4C 8B 48 ?? 4D 85 C9 74 ?? 49 8B 91 ?? ?? ?? ??",
            address => _getTboxFlag = hooks.CreateWrapper<GetTboxFlagFlow>(address, out _getTboxFlagFlowAdr));
        AddressScanner.DelayedScanPattern(
            "48 8B C4 48 81 EC B8 00 00 00 48 89 58 ??",
            address => _getItemWindow = hooks.CreateWrapper<GetItemWindowFunc>(address, out _getItemWindowAdr));
        AddressScanner.DelayedScanPattern(
            "48 83 EC 28 33 C9 E8 ?? ?? ?? ?? B9 01 00 00 00 44 8B C8 E8 ?? ?? ?? ?? B9 02 00 00 00 44 8B D0 E8 ?? " +
            "?? ?? ?? 48 8B 0D ?? ?? ?? ?? 48 85 C9 74 ?? 83 B9 ?? ?? ?? ?? 00 74 ?? 33 C0",
            address => _getItemWindowFlow = hooks.CreateWrapper<GetItemWindowFlow>(address, out _getItemWindowFlowAdr));

        logger.WriteLine("Created ItemManipulator Hooks");
    }

    private unsafe long OpenChestOnUpdateImpl(int* param1, float param2, long param3, float param4)
    {
        long retVal = _openChestHook.OriginalFunction(param1, param2, param3, param4);

        return retVal;
    }

    private IntPtr StartOpenChestImpl(IntPtr param1, long param2, ushort param3)
    {
        IntPtr retVal = _startOpenChestHook.OriginalFunction(param1, param2, param3);

        long flag = GetCurrentTboxFlag();

        OnChestOpened?.Invoke(flag);
        _currChestFlag = flag;

        return retVal;
    }

    private byte GetItemNumImpl(ushort itemId)
    {
        return _getItemNumHook.OriginalFunction(itemId);
    }

    public void SetItemNumImpl(ushort itemId, byte newItemCount, byte shouldUpdateRecentItem)
    {
        if (BLANK_ITEMS.Contains(itemId))
        {
            // Never actually add blank items to the player's inventory.
            return;
        }

        _setItemNumHook.OriginalFunction(itemId, newItemCount, shouldUpdateRecentItem);

        OnItemCountChanged.Invoke(itemId, newItemCount);
    }

    private void OnCompleteOpenChestImpl(long param1, IntPtr param2, long param3, int param4)
    {
        _onCompleteOpenChestHook.OriginalFunction(param1, param2, param3, param4);

        if (_currChestFlag is not null)
        {
            OnChestOpenedCompleted.Invoke((long)_currChestFlag);
        }

        _flagManipulator.SetBit(FlagManipulator.OVERWRITE_ITEM_TEXT, false);
        ClearItemNameOverride();
        _currChestFlag = null;
    }

    private unsafe char* GetItemNameImpl(ushort itemId)
    {
        if (_flagManipulator.CheckBit(FlagManipulator.OVERWRITE_ITEM_TEXT) && _itemNameOverrideAdr.IsAllocated)
        {
            return (char*)_itemNameOverrideAdr.AddrOfPinnedObject();
        }
        else
        {
            return _getItemNameHook.OriginalFunction(itemId);
        }
    }

    public void SetItemNameOverride(string itemName)
    {
        if (_itemNameOverrideAdr.IsAllocated)
        {
            _itemNameOverrideAdr.Free();
        }

        byte[] utf8Str = Encoding.Convert(Encoding.Unicode, Encoding.UTF8, Encoding.Unicode.GetBytes(itemName));

        _itemNameOverrideAdr = GCHandle.Alloc(utf8Str, GCHandleType.Pinned);
    }

    public void ClearItemNameOverride()
    {
        if (_itemNameOverrideAdr.IsAllocated)
        {
            _itemNameOverrideAdr.Free();
        }
    }

    private long GetCurrentTboxFlag()
    {
        if (_getTboxFlag is null) return 0;

        FlowFunctionWrapper.CallFlowFunctionSetup();

        _getTboxFlag();

        return FlowFunctionWrapper.CallFlowFunctionCleanup();
    }

    public byte GetItemNum(ushort itemId)
    {
        return _getItemNumHook.OriginalFunction(itemId);
    }

    public bool HasItem(ushort itemId)
    {
        return _getItemNumHook.OriginalFunction(itemId) > 0;
    }

    public void RewardItem(ushort itemId, byte count)
    {
        _logger.WriteLine($"Rewarding item {itemId:X} x{count}");
        byte newCount = GetItemNumImpl(itemId);
        newCount += count;
        SetItemNumImpl(itemId, newCount, 1);
    }

    public void HandleApItem(object? sender, ApConnector.ApItemReceivedEvent? e)
    {
        if (e.Handled || e.ApItem.Type != ItemType.Item || _flagManipulator.CheckBit(FlagManipulator.SHOWING_MESSAGE)
            || _flagManipulator.CheckBit(FlagManipulator.SHOWING_GAME_MSG) || !SequenceMonitor.SequenceCanShowMessage)
            return;

        var apItem = e.ApItem;

        RewardItem(apItem.Id, apItem.Count);

        if (!e.IsSenderSelf)
        {
            // Only display a notification if we got this from someone else - otherwise we are assuming we've seen the
            // item from the location.
            _flagManipulator.SetBit(FlagManipulator.SHOWING_MESSAGE, true);
            _flagManipulator.SetCount(FlagManipulator.AP_CURR_REWARD_ITEM_ID, e.ApItem.Id);
            _flagManipulator.SetCount(FlagManipulator.AP_CURR_REWARD_ITEM_NUM, e.ApItem.Count);

            FlowFunctionWrapper.CallCustomFlowFunction(CustomApMethodsIndexes.RewardItemsFunc);

            _logger.WriteLine($"Opening item window for item {e.ApItem.Id:X}");
        }

        e.Handled = true;
    }

    public unsafe string GetOriginalItemName(ushort itemId)
    {
        char* str = _getItemNameHook.OriginalFunction(itemId);
        return StrTools.CStrToString(str);
    }

    public unsafe char* GetOriginalItemNameCStr(ushort itemId)
    {
        return _getItemNameHook.OriginalFunction(itemId);
    }

    private void OpenItemWindow(ushort itemId, byte count)
    {
        FlowFunctionWrapper.CallFlowFunctionSetup(itemId, count, 0);
        _getItemWindowFlow?.Invoke();
        FlowFunctionWrapper.CallFlowFunctionCleanup();
    }

    private unsafe ItemRewardPackage[]* findItemRewardPackage(int* param1)
    {
        throw new NotImplementedException();
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct ItemRewardPackage
    {
        // [FieldOffset(0x0)]
        // public uint itemId;
    }
}