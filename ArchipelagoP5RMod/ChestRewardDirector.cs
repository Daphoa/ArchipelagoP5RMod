using Archipelago.MultiClient.Net.Models;

namespace ArchipelagoP5RMod;

public class ChestRewardDirector
{
    private ApConnector _apConnector;
    private ItemManipulator _itemManipulator;

    private readonly Dictionary<long, string> _rewardName = new Dictionary<long, string>();
    
    private readonly long[] chestFlags = [
        0x200001D4,
        0x200001CA,
        0x200001C9,
    ];

    public void Setup(ApConnector apConnector, ItemManipulator itemManipulator)
    {
        _apConnector = apConnector;
        _itemManipulator = itemManipulator;

        apConnector.ScoutLocations(chestFlags, ProcessScoutedInfo);
        
        itemManipulator.OnChestOpened += chestId =>
        {
            if (_rewardName.TryGetValue(chestId, out string? value))
            {
                itemManipulator.SetItemNameOverride(value);
            }
        };
    }

    private void ProcessScoutedInfo(Dictionary<long, ScoutedItemInfo> scoutedInfo)
    {
        foreach (var scoutedItemInfo in scoutedInfo)
        {
            long chestId = scoutedItemInfo.Key;
            string shortName;
            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (scoutedItemInfo.Value.IsReceiverRelatedToActivePlayer)
            {
                 shortName = scoutedItemInfo.Value.ItemName;
            }
            else
            {
                 shortName = CreateShortItemName(scoutedItemInfo.Value.Player.Alias, scoutedItemInfo.Value.ItemName);
            }

            _rewardName.Add(chestId, shortName);
        }
    }

    private string CreateShortItemName(string player, string itemName)
    {
        return $"{player}'s {itemName}";
    }
}