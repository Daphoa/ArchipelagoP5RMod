using System.Globalization;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using ArchipelagoP5RMod.Configuration;
using Reloaded.Mod.Interfaces;

namespace ArchipelagoP5RMod;

public class ApConnector
{
    private FlagManipulator flagManipulator;

    public class ApItemReceivedEvent(ApItem apItem) : EventArgs
    {
        public bool Handled { get; set; } = false;
        public ApItem ApItem { get; private set; } = apItem;
    }
    
    private readonly ArchipelagoSession _session;
    private readonly ILogger _logger;
    public event EventHandler<ApItemReceivedEvent> OnItemReceivedEvent; 
    private bool _isTryingToConnect = false;
    private bool _closeConnection = false;

    public uint LastRewardIndex {
        get => flagManipulator.GetCount(FlagManipulator.AP_LAST_REWARD_INDEX);
        private set => flagManipulator.SetCount(FlagManipulator.AP_LAST_REWARD_INDEX, value);
    }
    
    private string ServerPassword { get; set; }
    private string SlotName { get; set; }

    public ApConnector(string serverAddress, string serverPassword, string slotName, 
        FlagManipulator flagManipulator, ILogger logger)
    {
        _session = ArchipelagoSessionFactory.CreateSession(serverAddress);
        this._logger = logger;
        this.ServerPassword = serverPassword;
        this.SlotName = slotName;
        
        this.flagManipulator = flagManipulator;

        _session.MessageLog.OnMessageReceived += OnMessageReceived;

        _session.Items.ItemReceived += OnItemReceived;

        MaintainConnection();
    }
    
    #region Connection Management
    
    private async Task ConnectToServerAsync()
    {
        ushort failureCount = 0;
        _isTryingToConnect = true;

        while (true)
        {
            int waitTime;
            if (failureCount < 8)
            {
                waitTime = 250 * (1 << failureCount); // 250 * (2 to the power of failureCount)
            }
            else
            {
                waitTime = 60000;
            }

            await Task.Delay(waitTime);

            _logger.WriteLine($"Connecting as {SlotName}...");

            try
            {
                Task<RoomInfoPacket> connectTask = _session.ConnectAsync();

                await connectTask;
            }
            catch (Exception ex)
            {
                _logger.WriteLine("Failed to connect to server");
                _logger.WriteLine(ex.Message);
                failureCount++;
                continue;
            }

            LoginResult? loginResult;
            try
            {
                var loginTask = _session.LoginAsync("Persona 5 Royal", SlotName, ItemsHandlingFlags.AllItems,
                    version: null, tags: null, uuid: null, password: ServerPassword, requestSlotData: true);

                loginResult = await loginTask;
            }
            catch (Exception ex)
            {
                _logger.WriteLine(ex.Message);
                failureCount++;
                continue;
            }

            if (loginResult.Successful)
            {
                break;
            }

            failureCount++;

            var failure = (LoginFailure)loginResult;
            var errorMessage = $"Failed to Connect as {SlotName} ({failureCount} failures):";
            foreach (string error in failure.Errors)
            {
                errorMessage += $"\n    {error}";
            }

            foreach (ConnectionRefusedError error in failure.ErrorCodes)
            {
                errorMessage += $"\n    {error}";
            }

            _logger.WriteLine(errorMessage);
        }

        _isTryingToConnect = false;
    }

    private async void MaintainConnection()
    {
        while (true)
        {
            await Task.Delay(1000);

            if (_closeConnection)
            {
                break;
            }

            if (_session.Socket.Connected)
            {
                continue;
            }

            if (_isTryingToConnect)
            {
                await WaitForConnection();
            }
            else
            {
                await ConnectToServerAsync();
            }
        }

        if (_session.Socket.Connected)
        {
            await _session.Socket.DisconnectAsync();
        }
    }
    
    public void CloseConnection()
    {
        _closeConnection = true;
    }

    private bool CheckConnection()
    {
        if (_session.Socket.Connected)
            return true;

        _logger.WriteLine("No connection to server.");

        if (!_isTryingToConnect)
        {
            ConnectToServerAsync();
        }

        return false;
    }

    private async Task WaitForConnection()
    {
        while (!_session.Socket.Connected)
        {
            await Task.Delay(1000);
        }
    }
    
    #endregion

    private void OnMessageReceived(LogMessage message)
    {
        _logger.WriteLine(message.ToString());
    }
    
    private void OnItemReceived(ReceivedItemsHelper receivedItemsHelper)
    {
        var item = new ApItem(receivedItemsHelper.PeekItem().ItemId);

        var e = new ApItemReceivedEvent(item);
        OnItemReceivedEvent.Invoke(this, e);
        
        // bool success = _rewardHandler.Invoke(item);

        _logger.WriteLine($"Got item {item.ToString()} from 0x{item.ItemCode:X}");

        if (e.Handled)
        {
            receivedItemsHelper.DequeueItem();
        }
    }

    public async Task StartCollectionAsync()
    {
        await WaitForConnection();

        ProcessAllItems();
    }

    private void ProcessAllItems()
    {
        if (!CheckConnection())
        {
            return;
        }

        _logger.WriteLine("Collecting items from archipelago");
        while (_session.Items.AllItemsReceived.Count > LastRewardIndex)
        {
            _logger.WriteLine(
                $"Collecting item {LastRewardIndex}: {_session.Items.AllItemsReceived[(int)LastRewardIndex].ItemName}");
            
            var item = new ApItem(_session.Items.AllItemsReceived[(int)LastRewardIndex].ItemId);

            var e = new ApItemReceivedEvent(item);
            OnItemReceivedEvent.Invoke(this, e);
            if (!e.Handled)
            {
                // We can't keep going or the last reward index will be messed up.
                return;
            }

            LastRewardIndex++;

            _logger.WriteLine($"Processed index {LastRewardIndex} for item {item.ToString()}");
        }
    }

    public async void ReportLocationCheckAsync(params long[] locationIds)
    {
        await WaitForConnection();

        await _session.Locations.CompleteLocationChecksAsync(locationIds);
    }

    public async void ScoutLocations(long[] locationIds,
        Action<Dictionary<long, ScoutedItemInfo>> scoutLocationsCallback)
    {
        await WaitForConnection();

        var results = _session.Locations.ScoutLocationsAsync(locationIds);
        await results.WaitAsync(new TimeSpan(0, 0, 0, 6));

        scoutLocationsCallback.Invoke(results.Result);
    }
}