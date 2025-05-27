using System.Runtime.InteropServices;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Reloaded.Memory.Extensions;

namespace ArchipelagoP5RMod;

public class ApConnector
{
    private readonly FlagManipulator _flagManipulator;
    
    private HashSet<long> _queuedFoundLocations = [];

    public class ApItemReceivedEvent(ApItem apItem, string Sender, bool IsSelf) : EventArgs
    {
        public bool Handled { get; set; } = false;
        public bool IsSenderSelf { get; private set; } = IsSelf;
        public string Sender { get; set; } = Sender;
        public ApItem ApItem { get; private set; } = apItem;
    }

    private readonly ArchipelagoSession _session;
    public event EventHandler<ApItemReceivedEvent> OnItemReceivedEvent;
    private bool _isTryingToConnect = false;
    private bool _closeConnection = false;
    private bool _isProcessingItems = false;
    private bool _readyToCollect = false;

    public uint LastRewardIndex
    {
        get => _flagManipulator.GetCount(FlagManipulator.AP_LAST_REWARD_INDEX);
        private set => _flagManipulator.SetCount(FlagManipulator.AP_LAST_REWARD_INDEX, value);
    }

    private string ServerAddress { get; set; }
    private string ServerPassword { get; set; }
    private string SlotName { get; set; }

    public ApConnector(string serverAddress, string serverPassword, string slotName,
        FlagManipulator flagManipulator)
    {
        _session = ArchipelagoSessionFactory.CreateSession(serverAddress);
        this.ServerPassword = serverPassword;
        this.SlotName = slotName;
        this.ServerAddress = serverAddress;

        this._flagManipulator = flagManipulator;

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

            MyLogger.Log($"Trying to connect to {ServerAddress} as {SlotName}...");

            try
            {
                Task<RoomInfoPacket> connectTask = _session.ConnectAsync();

                await connectTask;
            }
            catch (Exception ex)
            {
                MyLogger.Log("Failed to connect to server");
                MyLogger.DebugLog(ex.Message);
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
                MyLogger.DebugLog(ex.Message);
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

            MyLogger.DebugLog(errorMessage);
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

    private async Task WaitForReportedLocations()
    {
        while (_queuedFoundLocations.Count > 0)
        {
            await Task.Delay(1000);
        }
    }

    #endregion

    public void ReadyToCollect()
    {
        this._readyToCollect = true;
    }

    private void OnMessageReceived(LogMessage message)
    {
        MyLogger.Log("[AP]" + message);
    }

    private void OnItemReceived(ReceivedItemsHelper receivedItemsHelper)
    {
        receivedItemsHelper.DequeueItem();

        // Just let the master loop handle it.
        ProcessAllItems();
    }

    public async Task StartCollectionAsync()
    {
        await WaitForConnection();

        ProcessAllItems();
    }

    private async void ProcessAllItems()
    {
        if (!CheckConnection() || _isProcessingItems)
            return;
        _isProcessingItems = true;

        while (!_readyToCollect)
        {
            await Task.Delay(1000);
        }

        MyLogger.DebugLog("Collecting items from archipelago");
        MyLogger.DebugLog($"LastRewardIndex: {LastRewardIndex}");
        MyLogger.DebugLog($"session: {LastRewardIndex}");
        while (LastRewardIndex < _session.Items.AllItemsReceived.Count)
        {
            var itemInfo = _session.Items.AllItemsReceived[(int)LastRewardIndex];

            var item = new ApItem(itemInfo.ItemId);

            MyLogger.DebugLog(string.IsNullOrEmpty(itemInfo.ItemName)
                ? $"Collecting item {LastRewardIndex}: {item.ToString()}"
                : $"Collecting item {LastRewardIndex}: {itemInfo.ItemName}");

            var e = new ApItemReceivedEvent(item, itemInfo.Player.Alias,
                itemInfo.Player.IsRelatedTo(_session.Players.ActivePlayer));
            OnItemReceivedEvent.Invoke(this, e);
            if (!e.Handled)
            {
                // Wait for a second then try again.
                await Task.Delay(1000);
                continue;
            }

            MyLogger.DebugLog($"Processed index {LastRewardIndex} for item {item.ToString()}");

            LastRewardIndex++;
        }

        _isProcessingItems = false;
    }

    public async Task ReportLocationCheckAsync(params long[] locationIds)
    {
        _queuedFoundLocations.UnionWith(locationIds);

        await WaitForConnection();

        await _session.Locations.CompleteLocationChecksAsync(locationIds);

        _queuedFoundLocations.ExceptWith(locationIds);
    }

    public async void ScoutLocations(long[]? locationIds,
        Action<Dictionary<long, ScoutedItemInfo>> scoutLocationsCallback)
    {
        await WaitForConnection();

        MyLogger.DebugLog("Connection made - scouting locations");

        var results = _session.Locations.ScoutLocationsAsync(locationIds);
        await results.WaitAsync(new TimeSpan(0, 0, 0, 10));

        scoutLocationsCallback.Invoke(results.Result);
    }

    public async Task<IEnumerable<long>> GetUnfoundLocations(IEnumerable<long>? locationIds = null)
    {
        await WaitForConnection();

        await WaitForReportedLocations();

        await WaitForConnection();

        IEnumerable<long> retVal = _session.Locations.AllMissingLocations;
        if (locationIds is not null)
        {
            retVal = retVal.Intersect(locationIds);
        }

        return retVal;
    }

    public async Task<IEnumerable<long>> GetFoundLocations(IEnumerable<long>? locationIds = null)
    {
        await WaitForConnection();

        IEnumerable<long> retVal = _session.Locations.AllLocationsChecked;
        if (locationIds is not null)
        {
            retVal = retVal.Intersect(locationIds);
        }

        return retVal;
    }

    public async void ReportGoalComplete()
    {
        await WaitForConnection();

        _session.SetGoalAchieved();
    }
    
    #region Save/Load

    private const ulong QUEUED_FOUND_LOCATIONS_HEADER = 0xFFFFFFFF;
    private const ulong LIST_END = 0x0;

    public byte[] SaveConnectionData()
    {
        MemoryStream stream = new();
        
        stream.Write(QUEUED_FOUND_LOCATIONS_HEADER);

        foreach (long location in _queuedFoundLocations)
        {
            stream.Write(location);
        }
        
        stream.Write(LIST_END);
        
        return stream.ToArray();
    }

    public void LoadConnectionData(MemoryStream memStream)
    {
        byte loadType = 0;
        
        while (true)
        {
            byte[] buffer = new byte[sizeof(long)];
            int readBytes = memStream.Read(buffer, 0, sizeof(long));
            
            if (readBytes < sizeof(long))
            {
                if (readBytes < 1 || buffer[0] != 0x0)
                {
                    MyLogger.DebugLog("WARNING: Read unusual data while loading AP Connection data.");
                }

                break;
            }

            ulong longItem = BitConverter.ToUInt64(buffer);

            switch (longItem)
            {
                case QUEUED_FOUND_LOCATIONS_HEADER:
                    loadType = 0;
                    continue;
                case LIST_END:
                    continue;
            }

            if (loadType == 0)
            {
                _queuedFoundLocations.Add((long)longItem);
            }
        }

        ReportLocationCheckAsync();
    }
    
    #endregion
}