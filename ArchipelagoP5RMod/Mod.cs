#define DEBUG

using System.Timers;
using ArchipelagoP5RMod.Configuration;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using ArchipelagoP5RMod.Template;
using ArchipelagoP5RMod.Types;

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
    /// Provides access to the Reloaded logger.
    /// </summary>
    private readonly ILogger _logger;

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
    private event EventHandler OnGameLoaded;

    private readonly ApConnector _apConnector;
    private readonly DateManipulator _dateManipulator;
    private readonly FlagManipulator _flagManipulator;
    private readonly ItemManipulator _itemManipulator;
    private readonly FlagManager _flagManager;
    private readonly ChestRewardDirector _chestRewardDirector;

    private readonly System.Timers.Timer _checkGameLoaded;
    private readonly DebugTools _debugTools;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks ?? throw new ArgumentNullException(nameof(context), "context.hooks cannot be null");
        _logger = context.Logger;
        _owner = context.Owner;
        _configuration = context.Configuration;
        _modConfig = context.ModConfig;


        // For more information about this template, please see
        // https://reloaded-project.github.io/Reloaded-II/ModTemplate/

        // If you want to implement e.g. unload support in your mod,
        // and some other neat features, override the methods in ModBase.

        AddressScanner.Scan(_logger);
        
        FlowFunctionWrapper.SetLogger(_logger);
        FlowFunctionWrapper.Setup(_hooks);

        _dateManipulator = new DateManipulator(_hooks, _logger);
        _flagManipulator = new FlagManipulator(_hooks, _logger);
        _itemManipulator = new ItemManipulator(_hooks, _logger);
        _apConnector = new ApConnector();
        _flagManager = new FlagManager();
        _chestRewardDirector = new ChestRewardDirector();
        
        _debugTools = new DebugTools();
        
        _apConnector.Init(config: _configuration, logger: _logger);

        OnGameLoaded += (_, _) =>
        {
            _apConnector.RegisterForCollection(0, itemId =>
            {
                _itemManipulator.RewardItem((ushort)itemId, 1, true);
                return true;
            });
        };

        // OnGameLoaded += TestFlowFuncWrapper;
        // OnGameLoaded += TestBitManipulator;
        OnGameLoaded += (_, _) => _flagManager.Setup(_flagManipulator);

        _chestRewardDirector.Setup(_apConnector, _itemManipulator);
        
        var logTimer = new System.Timers.Timer(10000);
        logTimer.Elapsed += LogStuff;
        logTimer.AutoReset = true;

        OnGameLoaded += (_, _) => logTimer.Start();

        _checkGameLoaded = new System.Timers.Timer(1000);
        _checkGameLoaded.Elapsed += CheckGameLoaded;
        _checkGameLoaded.AutoReset = false;
        _checkGameLoaded.Enabled = true;

        _itemManipulator.OnChestOpened += id =>
        {
            _logger.WriteLine($"StartOpenChest got flag: 0x{id:X}");
        };
        
        _itemManipulator.OnChestOpened += id =>
        {
            _apConnector.ReportLocationCheck(id);
        };
        _logger.WriteLine("End Mod Constructor");
    }

    private void CheckGameLoaded(object? sender, ElapsedEventArgs elapsedEventArgs)
    {
        _logger.WriteLine("Checking if game loaded");

        unsafe
        {
            if (AddressScanner.DateInfoAddress is null || AddressScanner.DateInfoAddress->currTotalDays == 0)
            {
                _checkGameLoaded.Start();
                return;
            }
        }
        
        _logger.WriteLine("Game seemingly loaded, waiting 3 seconds");
        
        var logTimer = new System.Timers.Timer(3000);
        logTimer.AutoReset = false;
        logTimer.Elapsed += (_, _) =>
        {
            _logger.WriteLine("Game loaded, calling onGameLoaded");
            OnGameLoaded?.Invoke(this, EventArgs.Empty);
        };
        logTimer.Start();

        _checkGameLoaded.Stop();
        _checkGameLoaded.Close();
    }

    private unsafe void LogStuff(object? sender, ElapsedEventArgs elapsedEventArgs)
    {
        // _logger.WriteLine($"DateInfo Adr - {(int)AddressScanner.DateInfoAddress:X8}");
        // _logger.WriteLine($"DateInfo - {AddressScanner.DateInfoAddress->ToString()}");
        if (_debugTools.HasFlagBackup)
        {
            _debugTools.FindChangedFlags(_logger);
        }
        
        _debugTools.BackupCurrentFlags();
    }

    private void TestBitManipulator(object? sender, EventArgs eventArgs)
    {
        uint[] TEST_VALS = [1244, 0x20000000 + 54, 0x30000000 + 1, 0x40000000 + 54];
        _logger.WriteLine("Testing bit manipulator");

        foreach (uint testVal in TEST_VALS)
        {
            bool preVal = _flagManipulator.CheckBit(testVal);
            _logger.WriteLine($"Pretest {testVal:X} bit value: {preVal}");
            _flagManipulator.SetBit(testVal, true);
            bool val = _flagManipulator.CheckBit(testVal);
            _logger.WriteLine(val ? $"TestBitManipulator test{testVal:X} on passed" : $"TestBitManipulator test{testVal:X} on failed!!!!!!!!!!!!!");

            _flagManipulator.SetBit(testVal, false);
            val = _flagManipulator.CheckBit(testVal);
            _logger.WriteLine(!val ? $"TestBitManipulator test{testVal:X} off passed" : $"TestBitManipulator test{testVal:X} off failed!!!!!!!!!!!!!");

            _flagManipulator.SetBit(testVal, preVal);
        }
        _logger.WriteLine("Ended TestBitManipulator");
    }

    private void TestFlowFuncWrapper(object? sender, EventArgs eventArgs)
    {
        _logger.WriteLine("Starting flow func wrapper test");
        var success = FlowFunctionWrapper.TestFlowscriptWrapper(5);
        _logger.WriteLine(success ? "FlowFuncWrapper test Success" : "FlowFuncWrapper test Failed!!!!!!!!!!!!!!!!");
    }
    
    #region Standard Overrides

    public override void ConfigurationUpdated(Config configuration)
    {
        // Apply settings from configuration.
        // ... your code here.
        _configuration = configuration;
        _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
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