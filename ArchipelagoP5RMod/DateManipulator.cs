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

    private readonly IHook<GetTime> _getTimeHook;

    // private readonly IHook<UpdateCurrentTotalDays> _updateCurrentTotalDaysHook;
    private readonly IHook<AdvanceToNextDay> _advanceToNextDayHook;
    private readonly IHook<UnknownTimeAdvanceFunc> _unknownTimeAdvanceFunc;


    [Function(CallingConventions.Fastcall)]
    private delegate short NextTime();

    [Function(CallingConventions.Fastcall)]
    private delegate long GetTime();

    [Function(CallingConventions.Fastcall)]
    private delegate void UpdateCurrentTotalDays(short totalDays);

    [Function(CallingConventions.Fastcall)]
    private delegate void AdvanceToNextDay(IntPtr totalDays);

    [Function(CallingConventions.Fastcall)]
    private delegate long UnknownTimeAdvanceFunc(long param1, float param2, long param3, long param4);


    public DateManipulator(IReloadedHooks hooks, ILogger logger)
    {
        _logger = logger;
        _nextTimeHook = hooks.CreateHook<NextTime>(NextTimeImpl, AddressScanner.NextTimeFunAddress).Activate();

        _getTimeHook = hooks.CreateHook<GetTime>(GetTimeImpl, AddressScanner.GetTimeFunAddress).Activate();

        _advanceToNextDayHook =
            hooks.CreateHook<AdvanceToNextDay>(AdvanceToNextDayImpl, AddressScanner.AdvanceToNextDayAddress).Activate();

        _unknownTimeAdvanceFunc =
            hooks.CreateHook<UnknownTimeAdvanceFunc>(UnknownTimeAdvanceImpl,
                AddressScanner.UnknownTimeAdvanceFuncAddress).Activate();

        // _updateCurrentTotalDaysHook = hooks.CreateHook<UpdateCurrentTotalDays>(UpdateCurrentTotalDaysImpl, 
        // AddressScanner.UpdateCurrentTotalDaysAddress).Activate();

        logger.WriteLine("Created DateManipulator Hooks");
    }

    private const short LOOP_DAY = 26;

    private long GetTimeImpl()
    {
        // _logger.WriteLine("DateManipulator::GetTimeImpl called");

        return _getTimeHook.OriginalFunction();
    }

    private void AdvanceToNextDayImpl(IntPtr param1)
    {
        _logger.WriteLine("DateManipulator::AdvanceToNextDayImpl called");

        unsafe
        {
            var dateInfo = AddressScanner.DateInfoAddress;

            if (dateInfo->nextTime == 0)
            {
                dateInfo->nextTotalDays = LOOP_DAY;
            }
        }

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
            unsafe
            {
                var dateInfo = AddressScanner.DateInfoAddress;

                if (dateInfo->currTime >= 6)
                {
                    // dateInfo->cur
                    dateInfo->nextTime = 0;
                    dateInfo->nextTotalDays = LOOP_DAY;
                }
                // if (dateInfo->nextTime == 0)
                // {
                //     dateInfo->nextTotalDays = LOOP_DAY;
                // }
            }
        }

        return _unknownTimeAdvanceFunc.OriginalFunction(param1, param2, param3, param4);
    }


    private short NextTimeImpl()
    {
        _logger.WriteLine("DateManipulator::NextTimeImpl called");

        short retVal;
        unsafe
        {
            var dateInfo = AddressScanner.DateInfoAddress;

            if (dateInfo->currTime == 7)
            {
                dateInfo->currTotalDays = LOOP_DAY - 1;
            }

            retVal = _nextTimeHook.OriginalFunction();

            // ReSharper disable once InvertIf
            if (dateInfo->currTime == 7)
            {
                dateInfo->nextTime = 0;
                dateInfo->nextTotalDays = LOOP_DAY;
            }
        }

        return retVal;
    }

    // private void UpdateCurrentTotalDaysImpl(short totalDays)
    // {
    //     _logger.WriteLine("DateManipulator::UpdateCurrentTotalDaysImpl called");
    //     _logger.WriteLine(Environment.StackTrace);
    //
    //     _updateCurrentTotalDaysHook.OriginalFunction(totalDays);
    // }
}