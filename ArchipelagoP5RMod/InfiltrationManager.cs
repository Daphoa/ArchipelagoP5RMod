﻿using ArchipelagoP5RMod.GameCommunicators;

namespace ArchipelagoP5RMod;

public class InfiltrationManager
{
    private readonly FlagManipulator _flagManipulator;
    private readonly ItemManipulator _itemManipulator;

    private const ushort RED_LUST_SEED = 0x40E1;
    private const ushort GREEN_LUST_SEED = 0x40E2;
    private const ushort BLUE_LUST_SEED = 0x40E3;

    public InfiltrationManager(FlagManipulator flagManipulator, ItemManipulator itemManipulator)
    {
        _flagManipulator = flagManipulator;
        _itemManipulator = itemManipulator;

        itemManipulator.OnItemCountChanged += (item, _) => InfiltrationCheck(item);
    }
    
    public void OnDateChangedHandler(short currTotalDays, byte currTime)
    {
        var palace = ConquestManager.TotalDaysToPalace(currTotalDays);
        bool canInfiltrate = CanInfiltrate(palace);
        MyLogger.DebugLog($"Date changed: can infiltrate: {canInfiltrate}");
        SetupInfiltration(palace, canInfiltrate);
    }

    // Not FULLY sure what these flags do, but copied them from the flow script. 
    public static bool IsCurrentlyInfiltrating(FlagManipulator flagManipulator, Palaces palace)
    {
        return !flagManipulator.CheckBit(1040) && flagManipulator.CheckBit(6393);
    }
    
    public bool CanInfiltrate(Palaces palace)
    {
        switch (palace)
        {
            case Palaces.KAMOSHIDA:
                return _itemManipulator.HasItem(RED_LUST_SEED) && _itemManipulator.HasItem(GREEN_LUST_SEED) &&
                       _itemManipulator.HasItem(BLUE_LUST_SEED);
            default:
                return false;
        }
    }

    private void InfiltrationCheck(ushort itemId)
    {
        if (itemId != RED_LUST_SEED && itemId != GREEN_LUST_SEED && itemId != BLUE_LUST_SEED)
        {
            return;
        }

        // ReSharper disable once InvertIf
        if (CanInfiltrate(Palaces.KAMOSHIDA))
        {
            NotifyInfiltration(Palaces.KAMOSHIDA);
            if (ConquestManager.TotalDaysToPalace(DateManipulator.CurrTotalDays) == Palaces.KAMOSHIDA)
            {
                SetupInfiltration(Palaces.KAMOSHIDA, true);
            }
        }
    }

    private async void NotifyInfiltration(Palaces palace)
    {
        while (_flagManipulator.CheckBit(FlagManipulator.SHOWING_MESSAGE)
               || _flagManipulator.CheckBit(FlagManipulator.SHOWING_GAME_MSG) ||
               !SequenceMonitor.SequenceCanShowMessage)
        {
            await Task.Delay(500);
        }

        _flagManipulator.SetCount(FlagManipulator.AP_CURR_NOTIFY_PALACE, (uint)palace);

        FlowFunctionWrapper.CallCustomFlowFunction(CustomApMethodsIndexes.NotifyInfiltrationRoute);
    }

    private void SetupInfiltration(Palaces palace, bool canInfiltrate)
    {
        switch (palace)
        {
            case Palaces.KAMOSHIDA:
                _flagManipulator.SetBit(0x20000000 + 209, canInfiltrate);
                _flagManipulator.SetBit(0x20000000 + 200, false); // This should be false even before infiltration is ready - probably represents if dungeon is cleared.
                // _flagManipulator.SetBit(0x20000000 + 281, true); // This is a test - it might be for the giant door in front of the treasure.
                break;
            case Palaces.MADARAME:
                _flagManipulator.SetBit(0x20000000 + 609, canInfiltrate);
                _flagManipulator.SetBit(0x20000000 + 600, false);
                break;
            case Palaces.KANESHIRO:
                _flagManipulator.SetBit(0x20000000 + 1010, canInfiltrate);
                _flagManipulator.SetBit(0x20000000 + 1000, false);
                break;
            case Palaces.FUTABA:
                _flagManipulator.SetBit(0x20000000 + 1410, canInfiltrate);
                _flagManipulator.SetBit(0x20000000 + 1400, false);
                break;
            case Palaces.OKUMURA:
                _flagManipulator.SetBit(0x20000000 + 1806, canInfiltrate);
                _flagManipulator.SetBit(0x20000000 + 1800, false);
                break;
            case Palaces.SAE:
                _flagManipulator.SetBit(0x20000000 + 2214, canInfiltrate);
                _flagManipulator.SetBit(0x20000000 + 2200, false);
                break;
            case Palaces.SHIDO:
                _flagManipulator.SetBit(0x20000000 + 2703, canInfiltrate);
                _flagManipulator.SetBit(0x20000000 + 2700, false);
                break;
            case Palaces.MARUKI:
                _flagManipulator.SetBit(0x20000000 + 4105, canInfiltrate);
                _flagManipulator.SetBit(0x20000000 + 0x1000, false);
                break;
        }
    }
}