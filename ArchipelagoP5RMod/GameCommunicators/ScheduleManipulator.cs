using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Mod.Interfaces;

namespace ArchipelagoP5RMod.GameCommunicators;

public class ScheduleManipulator
{
    readonly FlagManipulator _flagManipulator;

    [Function(CallingConventions.Fastcall)]
    private delegate IntPtr RunScheduleForDay(uint month, uint day, byte time);

    private IHook<RunScheduleForDay> _runScheduleForDayHook;

    public ScheduleManipulator(FlagManipulator flagManipulator, IReloadedHooks hooks)
    {
        _flagManipulator = flagManipulator;

        AddressScanner.DelayedScanPattern(
            "40 55 48 8D 6C 24 ?? 48 81 EC B0 00 00 00 8B 05 ?? ?? ?? ??",
            address => _runScheduleForDayHook =
                hooks.CreateHook<RunScheduleForDay>(RunScheduleForDayImpl, address).Activate());
    }

    private IntPtr RunScheduleForDayImpl(uint month, uint day, byte time)
    {
        uint newMonth;
        uint newDay;

        // Not FULLY sure what these flags do, but copied them from the flow script. 
        if (!_flagManipulator.CheckBit(1040) && _flagManipulator.CheckBit(6393))
        {
            (newMonth, newDay) = GetInfiltrationDay(month, time);
        }
        else
        {
            (newMonth, newDay) = GetBoringDay(time);
        }

        return _runScheduleForDayHook.OriginalFunction(newMonth, newDay, time);
    }

    private (uint month, uint day) GetBoringDay(byte time)
    {
        return (4, 1);
    }

    private (uint month, uint day) GetInfiltrationDay(uint month, byte time)
    {
        return (4, 28);
    }
}