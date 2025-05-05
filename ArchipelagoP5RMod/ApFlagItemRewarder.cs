using System.Diagnostics.CodeAnalysis;
using Reloaded.Mod.Interfaces;

namespace ArchipelagoP5RMod;

public readonly struct ApBitFlagItem(string itemName, uint bitFlag, short? itemId = null)
{
    public readonly uint BitFlag = bitFlag;
    public readonly short? ItemId = itemId;
    public readonly string ItemName = itemName;

    public static ApBitFlagItem Empty => new ApBitFlagItem(itemName: null, bitFlag: 0, itemId: null);

    public override int GetHashCode()
    {
        // This is the unique identifier.
        return BitFlag.GetHashCode();
    }

    public bool IsEmpty => BitFlag == 0;
}

public class ApFlagItemRewarder
{
    private HashSet<ApBitFlagItem> flagItems =
    [
        new("Grappling Hook", bitFlag: 0x2A3B, itemId: 0x4000 + 154),
        new("Randy Right Eye", bitFlag: 6464, itemId: 0x4000 + 116),
        new("Lustful Left Eye", bitFlag: 6464, itemId: 0x4000 + 117),
    ];

    private readonly ILogger _logger;
    private readonly ItemManipulator _itemManipulator;
    private readonly FlagManipulator _flagManipulator;

    public ApFlagItemRewarder(ItemManipulator itemManipulator, FlagManipulator flagManipulator, ILogger logger)
    {
        _logger = logger;
        _itemManipulator = itemManipulator;
        _flagManipulator = flagManipulator;
    }

    public void HandleApItem(object? sender, ApConnector.ApItemReceivedEvent? e)
    {
        if (e.Handled || e.ApItem.Type != ItemType.FlagItem ||
            _flagManipulator.CheckBit(FlagManipulator.SHOWING_MESSAGE))
            return;

        var fItem = flagItems.FirstOrDefault(fItem => fItem.BitFlag == e.ApItem.Id % 0x1000000, ApBitFlagItem.Empty);

        if (fItem.IsEmpty)
            return;

        _flagManipulator.SetBit(fItem.BitFlag, true);
        if (fItem.ItemId.HasValue)
        {
            _itemManipulator.RewardItem((ushort)fItem.ItemId, 1);

            _flagManipulator.SetCount(FlagManipulator.AP_CURR_REWARD_ITEM_ID, (ushort)fItem.ItemId);
            _flagManipulator.SetCount(FlagManipulator.AP_CURR_REWARD_ITEM_NUM, 1);
        }
        else
        {
            throw new NotImplementedException("Processing flag items without key items isn't implemented yet.");
        }

        _flagManipulator.SetBit(FlagManipulator.SHOWING_MESSAGE, true);

        FlowFunctionWrapper.CallCustomFlowFunction(CustomApMethodsIndexes.RewardItemsFunc);

        e.Handled = true;
    }
}