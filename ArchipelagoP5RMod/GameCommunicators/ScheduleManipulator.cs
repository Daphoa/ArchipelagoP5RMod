using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;

namespace ArchipelagoP5RMod.GameCommunicators;

public class ScheduleManipulator
{
    readonly FlagManipulator _flagManipulator;

    [Function(CallingConventions.Fastcall)]
    private delegate IntPtr RunScheduleForDay(uint month, uint day, byte time);

    private IHook<RunScheduleForDay> _runScheduleForDayHook;
    private const byte SETUP_TIME = DateManipulator.SETUP_TIME;

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
        uint newMonth = month;
        uint newDay = day;

        var typeOfDay = DateManipulator.ToTypeOfDay(month, day);

        switch (typeOfDay)
        {
            case TypeOfDay.Setup:
                if (time == SETUP_TIME)
                {
                    MyLogger.DebugLog("Trying to call custom schedule for setup day.");
                    return FlowFunctionWrapper.CallCustomFlowFunction(CustomApMethodsIndexes.NewGameSetupSdl);
                }

                (newMonth, newDay) = GetBoringDay(month, day, time);
                break;
            case TypeOfDay.InfiltrationDay:
                (newMonth, newDay) = GetInfiltrationDay(month, day, time);
                break;
            case TypeOfDay.None:
            case TypeOfDay.LoopDay:
            default:
                (newMonth, newDay) = GetBoringDay(month, day, time);
                break;
        }

        return _runScheduleForDayHook.OriginalFunction(newMonth, newDay, time);
    }

    private (uint month, uint day) GetBoringDay(uint month, uint day, byte time)
    {
        if (time is < 4 or > 6)
        {
            return (4, 1);
        }

        switch (month)
        {
            case 4:
                return (4, 1);
            case 5:
                return (5, 20);
            default:
                return (month, day);
        }
    }

    private (uint month, uint day) GetInfiltrationDay(uint month, uint day, byte time)
    {
        return (4, 28);
    }
}