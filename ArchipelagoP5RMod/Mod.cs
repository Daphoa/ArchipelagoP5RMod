#define DEBUG

using System.Security.Cryptography;
using System.Text;
using System.Timers;
using ArchipelagoP5RMod.Configuration;
using ArchipelagoP5RMod.GameCommunicators;
using ArchipelagoP5RMod.Template;
using ArchipelagoP5RMod.Types;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;

namespace ArchipelagoP5RMod;

/// <summary>
/// Your mod logic goes here.
/// </summary>
public class Mod : ModBase // <= Do not Remove.
{
    /// <summary>
    /// Provides access to the mod loader API.
    /// </summary>
    private readonly IModLoader _modLoader;

    /// <summary>
    /// Provides access to the Reloaded.Hooks API.
    /// </summary>
    /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
    private readonly IReloadedHooks _hooks;

    /// <summary>
    /// Entry point into the mod, instance that created this class.
    /// </summary>
    private readonly IMod _owner;

    /// <summary>
    /// Provides access to this mod's configuration.
    /// </summary>
    private Config _configuration;

    /// <summary>
    /// The configuration of the currently executing mod.
    /// </summary>
    private readonly IModConfig _modConfig;

    /// <summary>
    /// 
    /// </summary>
    private event EventHandler OnGameContextLoadedFirstTime;

    private readonly GameTaskListener _gameTaskListener;
    private readonly ApConnector _apConnector;
    private readonly DateManipulator _dateManipulator;
    private readonly FlagManipulator _flagManipulator;
    private readonly ItemManipulator _itemManipulator;
    private readonly ConfidantManipulator _confidantManipulator;
    private readonly GameSaveLoadConnector _gameSaveLoadConnector;
    private readonly ModSaveLoadManager _modSaveLoadManager;
    private readonly FirstTimeSetup _firstTimeSetup;
    private readonly ChestRewardDirector _chestRewardDirector;
    private readonly ApFlagItemRewarder _apFlagItemRewarder;
    private readonly MessageManipulator _messageManipulator;
    private readonly ScheduleManipulator _scheduleManipulator;
    private readonly InfiltrationManager _infiltrationManager;
    private readonly ConquestManager _conquestManager;
    private readonly PersonaManipulator _personaManipulator;
    private readonly BattleManipulator _battleManipulator;
    private readonly SocialStatManipulator _socialStatManipulator;

    private readonly DebugTools _debugTools;
    // Used to detect if the game was started as a new game.
    private bool loadedSuccess = false;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks ?? throw new ArgumentNullException(nameof(context), "context.hooks cannot be null");
        _owner = context.Owner;
        _configuration = context.Configuration;
        _modConfig = context.ModConfig;

        MyLogger.Setup(context.Logger, _configuration);
        FlowFunctionWrapper.Setup(_hooks);

        string apConnectionHash = GenerateHash(_configuration.ServerAddress + _configuration.SlotName);

        _gameTaskListener = new GameTaskListener(_hooks);
        _flagManipulator = new FlagManipulator(_hooks);
        _dateManipulator = new DateManipulator(_gameTaskListener, _flagManipulator, _hooks);
        _itemManipulator = new ItemManipulator(_flagManipulator, _hooks);
        _gameSaveLoadConnector = new GameSaveLoadConnector(_hooks);
        _modSaveLoadManager = new ModSaveLoadManager(_configuration.SaveDirectory, apConnectionHash);
        _confidantManipulator = new ConfidantManipulator(_flagManipulator, _hooks);
        _apConnector = new ApConnector(serverAddress: _configuration.ServerAddress,
            serverPassword: _configuration.ServerPassword,
            slotName: _configuration.SlotName,
            flagManipulator: _flagManipulator);
        _firstTimeSetup = new FirstTimeSetup();
        _chestRewardDirector = new ChestRewardDirector();
        _apFlagItemRewarder = new ApFlagItemRewarder(_itemManipulator, _flagManipulator);
        _messageManipulator = new MessageManipulator(_flagManipulator, _hooks);
        _scheduleManipulator = new ScheduleManipulator(_flagManipulator, _hooks);
        _infiltrationManager = new InfiltrationManager(_flagManipulator, _itemManipulator);
        _conquestManager = new ConquestManager(_flagManipulator);
        _personaManipulator = new PersonaManipulator(_hooks);
        _battleManipulator = new BattleManipulator(_hooks);
        _socialStatManipulator = new SocialStatManipulator(_hooks);
        BfLoader.Setup();
        SequenceMonitor.Setup();
        CustomLogic.Setup(_itemManipulator, _flagManipulator);

        AddressScanner.Scan();
        _gameTaskListener.FreezeListeners();

        _debugTools = new DebugTools();

        OnGameContextLoadedFirstTime += (_, _) =>
        {
            _apConnector.OnItemReceivedEvent += _itemManipulator.HandleApItem;
            _apConnector.OnItemReceivedEvent += _confidantManipulator.HandleApItem;
            _apConnector.OnItemReceivedEvent += _apFlagItemRewarder.HandleApItem;
        };

        _modSaveLoadManager.OnLoadComplete += (_, success) => OnGameFileLoaded(!success);

        // OnGameLoaded += TestFlowFuncWrapper;
        // OnGameLoaded += TestBitManipulator;

        OnGameContextLoadedFirstTime += (_, _) => _apConnector.ReadyToCollect();

        _chestRewardDirector.Setup(_apConnector, _itemManipulator, _flagManipulator);

        _modSaveLoadManager.RegisterSaveLoad(_flagManipulator.SaveCountData, _flagManipulator.LoadCountData);
        _modSaveLoadManager.RegisterSaveLoad(_confidantManipulator.SaveEnabledCmmData,
            _confidantManipulator.LoadEnabledCmmData);

        _dateManipulator.OnDateChanged += _infiltrationManager.OnDateChangedHandler;
        _dateManipulator.OnDateChanged += _conquestManager.OnDateChangedHandler;

        _battleManipulator.OnBattleComplete += (battleId, result) =>
        {
            if (result == BattleResult.Victory1 && battleId is 0839 or 0769)
            {
                // Kamoshida boss fight won.
                _apConnector.ReportGoalComplete();
            }
        };

        // New game detection
        OnGameContextLoadedFirstTime += (_, _) =>
        {
            if (loadedSuccess) return;

            OnGameFileLoaded(true);
        };

        _itemManipulator.OnChestOpenedCompleted += id => { _apConnector.ReportLocationCheckAsync(id); };
        _confidantManipulator.OnCmmSetLv += ReportCmmLvl;

        // Register save/load events
        _gameSaveLoadConnector.OnGameFileSaved += _modSaveLoadManager.Save;
        _gameSaveLoadConnector.OnGameFileLoaded += _modSaveLoadManager.Load;
        _gameSaveLoadConnector.OnGameFileLoaded += _ => { loadedSuccess = true; };

    AsyncStartCheckingForGameLoaded();
    }

    private string GenerateHash(string input)
    {
        using HashAlgorithm algorithm = SHA256.Create();

        StringBuilder sb = new StringBuilder();
        var hashArray = algorithm.ComputeHash(
            Encoding.UTF8.GetBytes(input));

        foreach (byte b in hashArray)
        {
            sb.Append(b.ToString("X2"));
        }

        return sb.ToString();
    }

    private async void OnGameFileLoaded(bool firstTimeLoad)
    {
        MyLogger.DebugLog("OnGameFileLoaded... waiting for sequence to be ready to show message.");
        
        while (!SequenceMonitor.SequenceCanShowMessage)
        {
            await Task.Delay(1000);
        }
        
        if (string.IsNullOrEmpty(_configuration.SaveDirectory))
        {
            MyLogger.DebugLog("Empty save directory message displayed.");
            FlowFunctionWrapper.CallCustomFlowFunction(CustomApMethodsIndexes.NotifyMissingSaveDirectoryError);
            return;
        }
        
        MyLogger.DebugLog("Calling setup after loading a file.");

        _apConnector.StartCollectionAsync();
        
        if (firstTimeLoad)
        {
            // Only setup if this is the first time we are loading with a new AP file.
            _firstTimeSetup.Setup(_flagManipulator, _personaManipulator, _confidantManipulator, _socialStatManipulator, 
                _dateManipulator);
        }

        _itemManipulator.SetItemNumImpl(0x3065, 99, 0); // Goho-M
        _itemManipulator.SetItemNumImpl(0x306D, 99, 0); // Silk Yarn (Lockpicks)
        _itemManipulator.SetItemNumImpl(0x306F, 99, 0); // Tin Clasp (Lockpicks)

        _apFlagItemRewarder.SyncWithInventory(); // This will try to ensure flags match if they are in inventory or not.
        
        _chestRewardDirector.MatchChestStateToAp();
    }

    // TODO figure out somewhere better for this to go.
    private async void ReportCmmLvl(ushort cmmId, short rank)
    {
        if (cmmId != 0x6 && cmmId != 0x8 && cmmId != 0xE)
        {
            // Only report handled cmms.
            MyLogger.DebugLog($"Not reporting cmm to AP, as it isn't handled. CmmId: {cmmId}, Rank: {rank}");
            return;
        }
        
        // TODO convert friend zone cmmId to default ids.
        long locId = 0x60000000L + cmmId * 0x10L + rank;

        while (_flagManipulator.CheckBit(FlagManipulator.SHOWING_MESSAGE) ||
               _flagManipulator.CheckBit(FlagManipulator.SHOWING_GAME_MSG) || !SequenceMonitor.SequenceCanShowMessage)
        {
            await Task.Delay(500);
        }

        await _apConnector.ReportLocationCheckAsync(locId);

        while (_flagManipulator.CheckBit(FlagManipulator.SHOWING_MESSAGE) ||
               _flagManipulator.CheckBit(FlagManipulator.SHOWING_GAME_MSG) || !SequenceMonitor.SequenceCanShowMessage)
        {
            await Task.Delay(500);
        }

        _itemManipulator.SetItemNameOverride("AP Item");
        _flagManipulator.SetBit(FlagManipulator.OVERWRITE_ITEM_TEXT, true);
        FlowFunctionWrapper.CallCustomFlowFunction(CustomApMethodsIndexes.NotifyConfidantLocation);
    }

    private async void AsyncStartCheckingForGameLoaded()
    {
        MyLogger.DebugLog("Checking if game loaded");

        while (!SequenceMonitor.SequenceCanShowMessage)
        {
            await Task.Delay(1000);
        }

        // _logger.WriteLine("Game seemingly loaded, waiting 5 seconds");
        // await Task.Delay(5000);

        MyLogger.DebugLog("Game loaded, calling onGameLoaded");
        OnGameContextLoadedFirstTime.Invoke(this, EventArgs.Empty);
    }

    private void LogStuff(object? sender, ElapsedEventArgs elapsedEventArgs)
    {
        return;
        // _logger.WriteLine($"DateInfo Adr - {(int)AddressScanner.DateInfoAddress:X8}");
        // _logger.WriteLine($"DateInfo - {AddressScanner.DateInfoAddress->ToString()}");
        if (_debugTools.HasFlagBackup)
        {
            _debugTools.FindChangedFlags();
        }

        _debugTools.BackupCurrentFlags();
    }

    private void TestBitManipulator(object? sender, EventArgs eventArgs)
    {
        uint[] TEST_VALS = [1244, 0x20000000 + 54, 0x30000000 + 1, 0x40000000 + 54];
        MyLogger.DebugLog("Testing bit manipulator");

        foreach (uint testVal in TEST_VALS)
        {
            bool preVal = _flagManipulator.CheckBit(testVal);
            MyLogger.DebugLog($"Pretest {testVal:X} bit value: {preVal}");
            _flagManipulator.SetBit(testVal, true);
            bool val = _flagManipulator.CheckBit(testVal);
            MyLogger.DebugLog(val
                ? $"TestBitManipulator test{testVal:X} on passed"
                : $"TestBitManipulator test{testVal:X} on failed!!!!!!!!!!!!!");

            _flagManipulator.SetBit(testVal, false);
            val = _flagManipulator.CheckBit(testVal);
            MyLogger.DebugLog(!val
                ? $"TestBitManipulator test{testVal:X} off passed"
                : $"TestBitManipulator test{testVal:X} off failed!!!!!!!!!!!!!");

            _flagManipulator.SetBit(testVal, preVal);
        }

        MyLogger.DebugLog("Ended TestBitManipulator");
    }

    private void TestFlowFuncWrapper(object? sender, EventArgs eventArgs)
    {
        MyLogger.DebugLog("Starting flow func wrapper test");
        var success = FlowFunctionWrapper.TestFlowscriptWrapper(5);
        MyLogger.DebugLog(success ? "FlowFuncWrapper test Success" : "FlowFuncWrapper test Failed!!!!!!!!!!!!!!!!");
    }

    #region Standard Overrides

    public override void ConfigurationUpdated(Config configuration)
    {
        // Apply settings from configuration.
        // ... your code here.
        _configuration = configuration;
        MyLogger.DebugLog($"[{_modConfig.ModId}] Config Updated: Applying");
    }

    #endregion

    #region For Exports, Serialization etc.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod()
    {
    }
#pragma warning restore CS8618

    #endregion
}