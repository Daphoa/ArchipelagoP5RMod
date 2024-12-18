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
    private ArchipelagoSession _session = null!;
    private ILogger _logger = null!;
    private int _lastRewardIndex = 0;
    private Func<ApItem, bool> rewardHandler;
    private bool _isTryingToConnect = false;
    private bool _closeConnection = false;

    private string ServerPassword { get; set; }
    private string SlotName { get; set; }

    public ApConnector(string serverAddress, string serverPassword, string slotName, ILogger logger)
    {
        _session = ArchipelagoSessionFactory.CreateSession(serverAddress);
        this._logger = logger;
        this.ServerPassword = serverPassword;
        this.SlotName = slotName;

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
        ApItem item = new ApItem(receivedItemsHelper.PeekItem().ItemId);
        bool success = rewardHandler.Invoke(item);

        _logger.WriteLine($"Got item {item.ToString()} from 0x{item.ItemCode:X}");

        if (success)
        {
            receivedItemsHelper.DequeueItem();
        }
    }


    public void SetItemRewarder(Func<ApItem, bool> rewardHandler)
    {
        this.rewardHandler = rewardHandler;
    }

    public int RegisterForCollection(Func<ApItem, bool> rewardHandler)
    {
        this.rewardHandler = rewardHandler;

        if (CheckConnection())
        {
            ProcessAllItems();
        }

        return _lastRewardIndex;
    }

    public async Task RegisterForCollectionAsync(Func<ApItem, bool> rewardHandler)
    {
        this.rewardHandler = rewardHandler;

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
        while (_session.Items.AllItemsReceived.Count > _lastRewardIndex)
        {
            _logger.WriteLine(
                $"Collecting item {_lastRewardIndex}: {_session.Items.AllItemsReceived[_lastRewardIndex].ItemName}");
            var item = new ApItem(_session.Items.AllItemsReceived[_lastRewardIndex].ItemId);
            bool success = rewardHandler(item);
            if (!success)
            {
                // We can't keep going or the last reward index will be messed up.
                return;
            }

            _lastRewardIndex++;

            _logger.WriteLine($"Processed index {_lastRewardIndex} for item {item.ToString()}");
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