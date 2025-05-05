using ArchipelagoP5RMod.GameCommunicators;
using Reloaded.Mod.Interfaces;

namespace ArchipelagoP5RMod;

public class InfiltrationManager
{
    private readonly FlagManipulator _flagManipulator;
    private readonly ItemManipulator _itemManipulator;
    private readonly ILogger _logger;
    
    private const ushort RED_LUST_SEED = 0x40E1;
    private const ushort GREEN_LUST_SEED = 0x40E2;
    private const ushort BLUE_LUST_SEED = 0x40E3;

    public InfiltrationManager(FlagManipulator flagManipulator, ItemManipulator itemManipulator, ILogger logger)
    {
        _flagManipulator = flagManipulator;
        _itemManipulator = itemManipulator;
        _logger = logger;

        itemManipulator.OnItemCountChanged += (item, _) => InfiltrationCheck(item);
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
        
        if (CanInfiltrate(Palaces.KAMOSHIDA))
        {
            NotifyInfiltration(Palaces.KAMOSHIDA);
            SetupInfiltration(Palaces.KAMOSHIDA);
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
        
        FlowFunctionWrapper.CallCustomFlowFunction(CustomApMethodsIndexes.NotifyInfiltrationRoute);
    }

    private void SetupInfiltration(Palaces palace)
    {
        _flagManipulator.SetBit(6230, false);
        switch (palace)
        {
            case Palaces.KAMOSHIDA:
                _flagManipulator.SetBit(0x20000000 + 209, true);
                _flagManipulator.SetBit(0x20000000 + 200, false);
                break;
            case Palaces.MADARAME:
                _flagManipulator.SetBit(0x20000000 + 609, true);
                _flagManipulator.SetBit(0x20000000 + 600, false);
                break;
            case Palaces.KANESHIRO:
                _flagManipulator.SetBit(0x20000000 + 1010, true);
                _flagManipulator.SetBit(0x20000000 + 1000, false);
                break;
            case Palaces.FUTABA:
                _flagManipulator.SetBit(0x20000000 + 1410, true);
                _flagManipulator.SetBit(0x20000000 + 1400, false);
                break;
            case Palaces.OKUMURA:
                _flagManipulator.SetBit(0x20000000 + 1806, true);
                _flagManipulator.SetBit(0x20000000 + 1800, false);
                break;
            case Palaces.SAE:
                _flagManipulator.SetBit(0x20000000 + 2214, true);
                _flagManipulator.SetBit(0x20000000 + 2200, false);
                break;
            case Palaces.SHIDO:
                _flagManipulator.SetBit(0x20000000 + 2703, true);
                _flagManipulator.SetBit(0x20000000 + 2700, false);
                break;
            case Palaces.MARUKI:
                _flagManipulator.SetBit(0x20000000 + 4105, true);
                _flagManipulator.SetBit(0x20000000 + 0x1000, false);
                break;
        }
    } 
}