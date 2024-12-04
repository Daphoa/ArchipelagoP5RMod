using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using ArchipelagoP5RMod.Configuration;
using Reloaded.Mod.Interfaces;

namespace ArchipelagoP5RMod;

public class ApConnector
{
    private ArchipelagoSession session = null!;
    private LoginSuccessful loginSuccessful = null!;
    private ILogger logger = null!;

    public void Init(Config config, ILogger logger)
    {
        session = ArchipelagoSessionFactory.CreateSession(config.ServerAddress);
        this.logger = logger;

        LoginResult result;
        
        try
        {
            result = session.TryConnectAndLogin(
                "Persona 5 Royal", config.SlotName, ItemsHandlingFlags.AllItems, 
                version: null, tags: null, uuid: null, password: config.ServerPassword, requestSlotData: true);
        }
        catch (Exception e)
        {
            result = new LoginFailure(e.GetBaseException().Message);
        }
        
        if (!result.Successful)
        {
            LoginFailure failure = (LoginFailure)result;
            string errorMessage = $"Failed to Connect to {config.ServerAddress} as {config.ConfigName}:";
            foreach (string error in failure.Errors)
            {
                errorMessage += $"\n    {error}";
            }
            foreach (ConnectionRefusedError error in failure.ErrorCodes)
            {
                errorMessage += $"\n    {error}";
            }

            logger.WriteAsync(errorMessage);
            
            return;
        }
        
        loginSuccessful = (LoginSuccessful)result;
    }

    public void ReportLocationCheck(params long[] locationIds)
    {
        session.Locations.CompleteLocationChecksAsync(locationIds);
    }

    public async void ScoutLocations(long[] locationIds, Action<Dictionary<long, ScoutedItemInfo>> scoutLocationsCallback)
    {
        var results = session.Locations.ScoutLocationsAsync(locationIds);
        await results.WaitAsync(new TimeSpan(0, 0, 0, 30));

        scoutLocationsCallback.Invoke(results.Result);
    }
}