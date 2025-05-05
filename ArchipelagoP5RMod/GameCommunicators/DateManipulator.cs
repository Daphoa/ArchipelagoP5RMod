using ArchipelagoP5RMod.GameCommunicators;
using ArchipelagoP5RMod.Types;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Mod.Interfaces;

namespace ArchipelagoP5RMod;

public class DateManipulator
{
    public const uint CALENDAR_ANIM_TOGGLE = 0x50000000 + 17;

    /* Fields */
    private readonly FlagManipulator _flagManipulator;

    public static unsafe DateInfo* DateInfoAddress => *_dateInfoRefAddress;
    private static unsafe DateInfo** _dateInfoRefAddress;

    
    private readonly SortedSet<short> _loopDates = [21];

    private bool disablingCalendarAnimation = false;

    public DateManipulator(GameTaskListener gameTaskListener, FlagManipulator flagManipulator, IReloadedHooks hooks)
    {
        _flagManipulator = flagManipulator;

        AddressScanner.DelayedScanPattern(
            "40 53 48 83 EC 60 48 8B 59 ?? 48 63 03",
            address =>
            {
                gameTaskListener.ListenForTaskCreate(address, OnTimeUpdateCreated);
                gameTaskListener.ListenForTaskDestroy(address, OnTimeUpdateDestroyed);
            });

        unsafe
        {
            AddressScanner.DelayedAddressHack(0x286c188, address => _dateInfoRefAddress = (DateInfo**)address);
        }

        MyLogger.DebugLog("Created DateManipulator Hooks");
    }

    private void OnTimeUpdateCreated()
    {
        ManipulateInGameDate();
    }

    private void OnTimeUpdateDestroyed()
    {
        if (disablingCalendarAnimation)
        {
            _flagManipulator.ToggleBit(CALENDAR_ANIM_TOGGLE);
            disablingCalendarAnimation = false;
        }
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

        return (short)(_loopDates.First() - 1);
    }

    private unsafe void ManipulateInGameDate()
    {
        var dateInfo = DateInfoAddress;

        // Only mess with dates at the end of the day unless we are outside of AP days.
        if (dateInfo->currTime < 6 && _loopDates.Contains(dateInfo->currTotalDays)) return; 

        dateInfo->nextTime = 0;
        dateInfo->nextTotalDays = NextDay(dateInfo->currTotalDays);
        if (dateInfo->nextTotalDays < dateInfo->currTotalDays)
        {
            dateInfo->nextTime = 7;
            _flagManipulator.ToggleBit(CALENDAR_ANIM_TOGGLE);
            disablingCalendarAnimation = true;
        }
    }
}