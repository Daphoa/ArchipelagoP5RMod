using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Mod.Interfaces;

namespace ArchipelagoP5RMod.GameCommunicators;

public class ScheduleManipulator
{
    ILogger _logger;

    [Function(CallingConventions.Fastcall)]
    private delegate IntPtr RunScheduleForDay(uint month, uint day, byte time);

    private IHook<RunScheduleForDay> _runScheduleForDayHook;

    public ScheduleManipulator(IReloadedHooks hooks, ILogger logger)
    {
        _logger = logger;

        AddressScanner.DelayedScanPattern(
            "40 55 48 8D 6C 24 ?? 48 81 EC B0 00 00 00 8B 05 ?? ?? ?? ??",
            address => _runScheduleForDayHook =
                hooks.CreateHook<RunScheduleForDay>(RunScheduleForDayImpl, address).Activate());
    }

    private IntPtr RunScheduleForDayImpl(uint month, uint day, byte time)
    {
        (uint newMonth, uint newDay) = GetBoringDay(time);
        return _runScheduleForDayHook.OriginalFunction(newMonth, newDay, time);
    }

    private (uint month, uint day) GetBoringDay(byte time)
    {
        return (4, 1);
    }
}