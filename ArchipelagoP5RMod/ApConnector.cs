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

    private string serverPassword { get; set; }
    private string slotName { get; set; }

    public void Init(string serverAddress, string serverPassword, string slotName, ILogger logger)
    {
        _session = ArchipelagoSessionFactory.CreateSession(serverAddress);
        this._logger = logger;
        this.serverPassword = serverPassword;
        this.slotName = slotName;

        _session.MessageLog.OnMessageReceived += OnMessageReceived;

        _session.Items.ItemReceived += receivedItemsHelper =>
        {
            ApItem item = new ApItem(receivedItemsHelper.PeekItem().ItemId);
            bool success = rewardHandler.Invoke(item);

            logger.WriteLine($"Got item {item.ToString()} from 0x{item.ItemCode}");

            if (success)
            {
                receivedItemsHelper.DequeueItem();
            }
        };

        MaintainConnection();
    }

    private async Task ConnectToServerAsync()
    {
        byte failureCount = 0;
        _isTryingToConnect = true;

        while (true)
        {
            int waitTime = 250 * (1 << failureCount); // 250 * (2 to the power of failureCount)
            if (waitTime > 30000) waitTime = 30000;

            await Task.Delay(waitTime);

            _logger.WriteLine($"Connecting as {slotName}...");

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
                var loginTask = _session.LoginAsync("Persona 5 Royal", slotName, ItemsHandlingFlags.AllItems,
                    version: null, tags: null, uuid: null, password: serverPassword, requestSlotData: true);

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

            var failure = (LoginFailure)loginResult;
            var errorMessage = $"Failed to Connect as {slotName}:";
            foreach (string error in failure.Errors)
            {
                errorMessage += $"\n    {error}";
            }
            foreach (ConnectionRefusedError error in failure.ErrorCodes)
            {
                errorMessage += $"\n    {error}";
            }

            _logger.WriteLine(errorMessage);

            failureCount++;
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

    private void OnMessageReceived(LogMessage message)
    {
        _logger.WriteLine(message.ToString());
    }

    public void CloseConnection()
    {
        _closeConnection = true;
    }
    
    public void SetItemRewarder(Func<ApItem, bool> rewardHandler)
    {
        this.rewardHandler = rewardHandler;
    }

    public int RegisterForCollection(int processedNum, Func<ApItem, bool> rewardHandler)
    {
        this.rewardHandler = rewardHandler;

        if (CheckConnection())
        {
            processedNum = ProcessAllItems(processedNum);
        }

        return processedNum;
    }
    
    public async Task<int> RegisterForCollectionAsync(int processedNum, Func<ApItem, bool> rewardHandler)
    {
        this.rewardHandler = rewardHandler;

        await WaitForConnection();

        processedNum = ProcessAllItems(processedNum);

        return processedNum;
    }

    private int ProcessAllItems(int processedNum)
    {
        if (!CheckConnection())
        {
            return processedNum;
        }
        
        _logger.WriteLine("Collecting items from archipelago");
        while (_session.Items.AllItemsReceived.Count > processedNum)
        {
            _logger.WriteLine(
                $"Collecting item {processedNum}: {_session.Items.AllItemsReceived[processedNum].ItemName}");
            var item = new ApItem(_session.Items.AllItemsReceived[processedNum].ItemId);
            rewardHandler(item);
            processedNum++;

            _logger.WriteLine($"Processed index {processedNum} for item {item.ToString()}");
        }

        return processedNum;
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