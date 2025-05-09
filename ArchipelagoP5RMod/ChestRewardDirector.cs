using Archipelago.MultiClient.Net.Models;

namespace ArchipelagoP5RMod;

public class ChestRewardDirector
{
    private ApConnector _apConnector;
    private ItemManipulator _itemManipulator;
    private FlagManipulator _flagManipulator;

    private readonly Dictionary<long, string> _rewardName = new();

    private readonly long[]? _chestFlags =
    [
        0x200001C2, 0x200001D6, 0x200001D5, 0x200001C4, 0x200001C5, 0x20000173, 0x200001D3, 0x200001D4, 0x200001CA,
        0x200001C9, 0x200001CB, 0x200001D8, 0x200001CC, 0x200001C6, 0x200001C3, 0x200001D9, 0x200001C7, 0x200001CD,
        0x200001D2, 0x200001CE, 0x200001C8, 0x200001D1, 0x200001CF, 0x200013FD, 0x200013FC, 0x200013FB
    ];


    public void Setup(ApConnector apConnector, ItemManipulator itemManipulator, FlagManipulator flagManipulator)
    {
        _apConnector = apConnector;
        _itemManipulator = itemManipulator;
        _flagManipulator = flagManipulator;

        apConnector.ScoutLocations(_chestFlags, ProcessScoutedInfo);

        itemManipulator.OnChestOpened += chestId =>
        {
            if (_rewardName.TryGetValue(chestId, out string? value) && !string.IsNullOrEmpty(value))
            {
                _flagManipulator.SetBit(FlagManipulator.OVERWRITE_ITEM_TEXT, true);
                itemManipulator.SetItemNameOverride(value);
            }
        };
    }

    private void ProcessScoutedInfo(Dictionary<long, ScoutedItemInfo> scoutedInfo)
    {
        MyLogger.DebugLog("Starting to process scouted chest info.");

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
        
        MyLogger.DebugLog("Done processing scouted chest info.");
    }

    public async void CloseUnopenedChests()
    {
        var unopenedChests = _apConnector.GetUnfoundLocations(_chestFlags);
        await unopenedChests;

        foreach (ulong id in unopenedChests.Result)
        {
            MyLogger.DebugLog($"Trying to close tbox ID: {id:X}");
            
            _flagManipulator.SetBit((uint)id, false);
        }
    }

    private string CreateShortItemName(string player, string itemName)
    {
        return $"{player}'s {itemName}";
    }
}