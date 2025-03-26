using System.Runtime.InteropServices;
using System.Text;
using Archipelago.MultiClient.Net.Packets;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Mod.Interfaces;

namespace ArchipelagoP5RMod;

public class ItemManipulator
{
    private readonly ILogger _logger;

    private const long DUMMY_ITEM = 0;

    private readonly IHook<OpenChestOnUpdate> _openChestHook;
    private readonly IHook<StartOpenChest> _startOpenChestHook;
    private readonly IHook<OnCompleteOpenChest> _onCompleteOpenChestHook;
    private readonly IHook<GetItemName> _getItemNameHook;
    private readonly IHook<GetItemNum> _getItemNumHook;
    private readonly IHook<SetItemNum> _setItemNumHook;

    private readonly IntPtr _getTboxFlagFlowAdr;
    private readonly IntPtr _getItemWindowAdr;
    private readonly IntPtr _getItemWindowFlowAdr;

    private GCHandle _itemNameOverrideAdr;
    private long? _currChestFlag = null;

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
    private delegate byte GetItemNum(ushort itemId);

    [Function(CallingConventions.Fastcall)]
    private delegate void SetItemNum(ushort itemId, byte newItemCount, byte shouldUpdateRecentItem);

    [Function(CallingConventions.Fastcall)]
    private unsafe delegate IntPtr GetItemWindow(short* itemIds, int* itemNum, uint length, int flag);

    private delegate bool GetItemWindowFlow();

    private GetTboxFlagFlow? _getTboxFlag { get; set; }
    private GetItemWindow? _getItemWindow { get; set; }
    private GetItemWindowFlow? _getItemWindowFlow { get; set; }

    public event OnChestOpenedEvent OnChestOpened;
    public event OnChestOpenedEvent OnChestOpenedCompleted;

    public delegate void OnChestOpenedEvent(long chestId);

    public ItemManipulator(IReloadedHooks hooks, ILogger logger)
    {
        _logger = logger;
        unsafe
        {
            _openChestHook = hooks
                .CreateHook<OpenChestOnUpdate>(OpenChestOnUpdateImpl, AddressScanner.Addresses[AddressScanner.AddressName.OpenChestOnUpdateFuncAddress])
                .Activate();
            _startOpenChestHook =
                hooks.CreateHook<StartOpenChest>(StartOpenChestImpl, AddressScanner.Addresses[AddressScanner.AddressName.StartOpenChestFuncAddress])
                    .Activate();
            _onCompleteOpenChestHook =
                hooks.CreateHook<OnCompleteOpenChest>(OnCompleteOpenChestImpl,
                    AddressScanner.Addresses[AddressScanner.AddressName.OnCompleteOpenChestFuncAddress]).Activate();
            _getItemNameHook = hooks.CreateHook<GetItemName>(GetItemNameImpl, AddressScanner.Addresses[AddressScanner.AddressName.GetItemNameFuncAddress])
                .Activate();
            _getItemNumHook = hooks.CreateHook<GetItemNum>(GetItemNumImpl, AddressScanner.Addresses[AddressScanner.AddressName.GetItemNumFuncAddress])
                .Activate();
            _setItemNumHook = hooks.CreateHook<SetItemNum>(SetItemNumImpl, AddressScanner.Addresses[AddressScanner.AddressName.SetItemNumFuncAddress])
                .Activate();
            _getTboxFlag = hooks.CreateWrapper<GetTboxFlagFlow>(AddressScanner.Addresses[AddressScanner.AddressName.GetTboxFlagFlowFuncAddress],
                out _getTboxFlagFlowAdr);
            _getItemWindow = hooks.CreateWrapper<GetItemWindow>(AddressScanner.Addresses[AddressScanner.AddressName.GetItemWindowFuncAddress],
                out _getItemWindowAdr);
            _getItemWindowFlow = hooks.CreateWrapper<GetItemWindowFlow>(AddressScanner.Addresses[AddressScanner.AddressName.GetItemWindowFlowFuncAddress],
                out _getItemWindowFlowAdr);
        }

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

    private void SetItemNumImpl(ushort itemId, byte newItemCount, byte shouldUpdateRecentItem)
    {
        _setItemNumHook.OriginalFunction(itemId, newItemCount, shouldUpdateRecentItem);
    }

    private void OnCompleteOpenChestImpl(long param1, IntPtr param2, long param3, int param4)
    {
        _onCompleteOpenChestHook.OriginalFunction(param1, param2, param3, param4);

        if (_currChestFlag is not null)
        {
            OnChestOpenedCompleted.Invoke((long)_currChestFlag);
        }

        ClearItemNameOverride();
        _currChestFlag = null;
    }

    private unsafe char* GetItemNameImpl(ushort itemId)
    {
        if (_itemNameOverrideAdr.IsAllocated)
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

    private unsafe long CStrLen(char* str)
    {
        char *s;
        for (s = str; *s == (char)0; ++s) { }
        return s - str;
    } 

    public void RewardItem(ushort itemId, byte count, bool showWindow)
    {
        _logger.WriteLine($"Rewarding item {itemId:X} x{count}");
        byte newCount = GetItemNumImpl(itemId);
        newCount += count;
        SetItemNumImpl(itemId, newCount, 1);
        if (showWindow)
        {
            _logger.WriteLine($"Opening item window for item {itemId:X}");
            OpenItemWindow(itemId, count);
        }
    }

    public unsafe string GetOriginalItemName(ushort itemId)
    {

        char* str = _getItemNameHook.OriginalFunction(itemId);
        int len = (int)CStrLen(str);

        var managedArray = new byte[len];

        Marshal.Copy((IntPtr)str, managedArray, 0, len);
        return Encoding.UTF8.GetString(managedArray);
    }

    public unsafe char* GetOriginalItemNamePtr(ushort itemId)
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