using ArchipelagoP5RMod.Configuration;
using Reloaded.Mod.Interfaces;

namespace ArchipelagoP5RMod;

public static class MyLogger
{
    private static ILogger _logger;
    private static bool _logDebug;

    public static void Setup(ILogger logger, Config configuration)
    {
        _logger = logger;
        _logDebug = configuration.LogDebug;
    }

    public static void Log(string message)
    {
        _logger.Write("[AP] ");
        _logger.WriteLine(message);
    }

    public static void DebugLog(string message)
    {
        if (!_logDebug)
            return;
        _logger.Write("[AP] [DEBUG] ");
        _logger.WriteLine(message);
    }
}