using ArchipelagoP5RMod.Types;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Mod.Interfaces;

namespace ArchipelagoP5RMod;

public class DateManipulator
{
    /* Fields */
    private readonly ILogger _logger;

    public static unsafe DateInfo* DateInfoAddress => *_dateInfoRefAddress;
    private static unsafe DateInfo** _dateInfoRefAddress;

    private IHook<NextTime> _nextTimeHook;

    // private readonly IHook<Update> _updateCurrentTotalDaysHook;
    private IHook<AdvanceToNextDay> _advanceToNextDayHook;
    private IHook<UnknownTimeAdvanceFunc> _unknownTimeAdvanceFunc;

    /**
     * Delegates *
     */
    [Function(CallingConventions.Fastcall)]
    private delegate short NextTime();

    [Function(CallingConventions.Fastcall)]
    private delegate long GetTime();

    [Function(CallingConventions.Fastcall)]
    private delegate void AdvanceToNextDay(IntPtr totalDays);

    [Function(CallingConventions.Fastcall)]
    private delegate long UnknownTimeAdvanceFunc(long param1, float param2, long param3, long param4);

    private readonly short[] _loopDates = [22];

    public DateManipulator(IReloadedHooks hooks, ILogger logger)
    {
        _logger = logger;
        AddressScanner.DelayedScanPattern(
            "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B 05 ?? ?? ?? ?? 33 F6 8B DE",
            address => _nextTimeHook = hooks.CreateHook<NextTime>(NextTimeImpl, address).Activate());

        AddressScanner.DelayedScanPattern(
            "40 53 48 83 EC 20 80 3D ?? ?? ?? ?? 00 48 8B D9 48 8B 15 ?? ?? ?? ??",
            address => _advanceToNextDayHook =
                hooks.CreateHook<AdvanceToNextDay>(AdvanceToNextDayImpl, address).Activate());

        AddressScanner.DelayedScanPattern(
            "40 53 48 83 EC 60 48 8B 59 ?? 48 63 03",
            address =>
                _unknownTimeAdvanceFunc =
                    hooks.CreateHook<UnknownTimeAdvanceFunc>(UnknownTimeAdvanceImpl, address).Activate());

        unsafe
        {
            AddressScanner.DelayedAddressHack(0x286c188, address => _dateInfoRefAddress = (DateInfo**)address);
        }

        logger.WriteLine("Created DateManipulator Hooks");
    }


    private short NextDay(short currentDay)
    {
        foreach (short date in _loopDates)
        {
            if (currentDay < date)
            {
                return date;
            }
        }

        return _loopDates[0];
    }

    private unsafe void ManipulateInGameDate()
    {
        var dateInfo = DateInfoAddress;

        if (dateInfo->currTime < 6) return; // Only mess with dates at the end of the day.

        dateInfo->nextTime = 0;
        dateInfo->nextTotalDays = NextDay(dateInfo->currTotalDays);
        if (dateInfo->nextTotalDays > dateInfo->currTotalDays)
        {
            dateInfo->currTotalDays = (short)(dateInfo->nextTotalDays - 1);
        }
    }

    private void AdvanceToNextDayImpl(IntPtr param1)
    {
        _logger.WriteLine("DateManipulator::AdvanceToNextDayImpl called");

        // manipulateGameDate();

        _advanceToNextDayHook.OriginalFunction(param1);
    }

    private long UnknownTimeAdvanceImpl(long param1, float param2, long param3, long param4)
    {
        _logger.WriteLine("DateManipulator::UnknownTimeAdvanceImpl called");

        uint state;

        unsafe
        {
            var stateAdr = (uint**)(param1 + 0x48);
            state = **stateAdr;
        }

        _logger.WriteLine($"State: {state}");

        // Only mess with the date on the early entries to this function. 
        if (state is >= 11 or < 2)
        {
            ManipulateInGameDate();
        }

        return _unknownTimeAdvanceFunc.OriginalFunction(param1, param2, param3, param4);
    }


    private short NextTimeImpl()
    {
        _logger.WriteLine("DateManipulator::NextTimeImpl called");

        short retVal;
        unsafe
        {
            ManipulateInGameDate();

            retVal = _nextTimeHook.OriginalFunction();

            ManipulateInGameDate();
        }

        return retVal;
    }
}