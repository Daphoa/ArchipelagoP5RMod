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

    private readonly IHook<OpenChest> _openChestHook;
    private readonly IHook<GetItemName> _getItemNameHook;

    private GCHandle _itemNameOverrideAdr;

    [Function(CallingConventions.Fastcall)]
    private unsafe delegate long OpenChest(int* param1, float param2, long param3, float param4);

    [Function(CallingConventions.Fastcall)]
    private unsafe delegate char* GetItemName(ushort itemId);

    public ItemManipulator(IReloadedHooks hooks, ILogger logger)
    {
        _logger = logger;
        unsafe
        {
            _openChestHook = hooks.CreateHook<OpenChest>(OpenChestImpl, AddressScanner.OpenChestFuncAddress).Activate();
            _getItemNameHook = hooks.CreateHook<GetItemName>(GetItemNameImpl, AddressScanner.GetItemNameFuncAddress)
                .Activate();
        }

        logger.WriteLine("Created ItemManipulator Hooks");
    }

    private unsafe long OpenChestImpl(int* param1, float param2, long param3, float param4)
    {
        SetItemNameOverride("Item2");
        var debugTools = new DebugTools();
        debugTools.BackupCurrentFlags();

        long retVal = _openChestHook.OriginalFunction(param1, param2, param3, param4);

        debugTools.FindChangedFlags(_logger);
        return retVal;
    }

    private unsafe char* GetItemNameImpl(ushort itemId)
    {
        if (_itemNameOverrideAdr.IsAllocated)
        {
            return (char*)_itemNameOverrideAdr.AddrOfPinnedObject();
        }

        return _getItemNameHook.OriginalFunction(itemId);
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