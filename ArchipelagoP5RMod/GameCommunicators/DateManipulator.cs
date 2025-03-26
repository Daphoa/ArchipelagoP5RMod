using ArchipelagoP5RMod.Types;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Mod.Interfaces;

namespace ArchipelagoP5RMod;

public class DateManipulator
{
    /* Fields */
    private readonly ILogger _logger;

    private readonly IHook<NextTime> _nextTimeHook;

    // private readonly IHook<Update> _updateCurrentTotalDaysHook;
    private readonly IHook<AdvanceToNextDay> _advanceToNextDayHook;
    private readonly IHook<UnknownTimeAdvanceFunc> _unknownTimeAdvanceFunc;

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
        _nextTimeHook = hooks.CreateHook<NextTime>(NextTimeImpl, AddressScanner.Addresses[AddressScanner.AddressName.NextTimeFunAddress]).Activate();

        _advanceToNextDayHook =
            hooks.CreateHook<AdvanceToNextDay>(AdvanceToNextDayImpl, AddressScanner.Addresses[AddressScanner.AddressName.AdvanceToNextDayAddress]).Activate();

        _unknownTimeAdvanceFunc =
            hooks.CreateHook<UnknownTimeAdvanceFunc>(UnknownTimeAdvanceImpl,
                AddressScanner.Addresses[AddressScanner.AddressName.UnknownTimeAdvanceFuncAddress]).Activate();

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
        var dateInfo = AddressScanner.DateInfoAddress;
        
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