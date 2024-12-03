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

    private readonly IntPtr _getTboxFlagFlowAdr;

    private GCHandle _itemNameOverrideAdr;

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

    private GetTboxFlagFlow? _getTboxFlag { get; set; }

    public ItemManipulator(IReloadedHooks hooks, ILogger logger)
    {
        _logger = logger;
        unsafe
        {
            _openChestHook = hooks
                .CreateHook<OpenChestOnUpdate>(OpenChestOnUpdateImpl, AddressScanner.OpenChestOnUpdateFuncAddress)
                .Activate();
            _startOpenChestHook =
                hooks.CreateHook<StartOpenChest>(StartOpenChestImpl, AddressScanner.StartOpenChestFuncAddress)
                    .Activate();
            _onCompleteOpenChestHook =
                hooks.CreateHook<OnCompleteOpenChest>(OnCompleteOpenChestImpl,
                    AddressScanner.OnCompleteOpenChestFuncAddress).Activate();
            _getItemNameHook = hooks.CreateHook<GetItemName>(GetItemNameImpl, AddressScanner.GetItemNameFuncAddress)
                .Activate();
            _getTboxFlag = hooks.CreateWrapper<GetTboxFlagFlow>(AddressScanner.GetTboxFlagFlowFuncAddress,
                out _getTboxFlagFlowAdr);
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
        SetItemNameOverride("Item2");

        var retVal = _startOpenChestHook.OriginalFunction(param1, param2, param3);
        
        long flag = GetCurrentTboxFlag();
        _logger.WriteLine($"StartOpenChest got flag: {flag:X}");

        return retVal;
    }

    private void OnCompleteOpenChestImpl(long param1, IntPtr param2, long param3, int param4)
    {
        _onCompleteOpenChestHook.OriginalFunction(param1, param2, param3, param4);

        ClearItemNameOverride();
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