using Reloaded.Mod.Interfaces;

namespace ArchipelagoP5RMod;

public static class MyLogger
{
    private static ILogger _logger;
    
    public static void Setup(ILogger logger)
    {
        _logger = logger;
    }

    public static void Log(string message)
    {
        _logger.WriteLine(message);
    }
    
    public static void DebugLog(string message)
    {
        _logger.WriteLine(message);
    }
}