using ArchipelagoP5RMod.GameCommunicators;
using ArchipelagoP5RMod.Types;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;

namespace ArchipelagoP5RMod;

public class DateManipulator
{
    public const uint CALENDAR_ANIM_TOGGLE = 0x50000000 + 17;

    [Function(CallingConventions.Fastcall)]
    private delegate void DateDisplayToggle(long shouldDisplay, uint param2);

    private DateDisplayToggle _dateDisplay;

    private FlowFunctionWrapper.BasicFlowFunc _dateDisplayFlow;

    /* Fields */
    private readonly FlagManipulator _flagManipulator;

    public static unsafe short CurrTotalDays => DateInfoAddress->currTotalDays;
    public static unsafe DateInfo* DateInfoAddress => *_dateInfoRefAddress;
    private static unsafe DateInfo** _dateInfoRefAddress;

    private IntPtr _dateDisplayAdr;
    private IntPtr _dateDisplayFlowAdr;

    private static int[] DAYS_IN_MONTH = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

    public const short SETUP_TOTAL_DAY = 6;
    public const byte SETUP_TIME = 4;
    private readonly SortedSet<short> _loopDates = [21];

    public delegate void OnDateChangedHandler(short currTotalDays, byte currTime);

    public event OnDateChangedHandler OnDateChanged = delegate { };

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

        AddressScanner.DelayedScanPattern(
            "40 57 48 83 EC 70 48 8B 05 ?? ?? ?? ?? 48 8B 78 ??",
            address => _dateDisplay = hooks.CreateWrapper<DateDisplayToggle>(address, out _dateDisplayAdr));
        //
        // AddressScanner.DelayedScanPattern(
        //     "48 83 EC 28 31 C9 E8 ?? ?? ?? ?? 89 C1 E8 ?? ?? ?? ?? B8 01 00 00 00",
        //     address => _dateDisplayFlow =
        //         hooks.CreateWrapper<FlowFunctionWrapper.BasicFlowFunc>(address, out _dateDisplayFlowAdr));

        unsafe
        {
            AddressScanner.DelayedAddressHack(0x286c188, address => _dateInfoRefAddress = (DateInfo**)address);
        }

        MyLogger.DebugLog("Created DateManipulator Hooks");
    }

    public void SetDateDisplay(bool display)
    {
        MyLogger.DebugLog($"SetDateDisplay called with display: {display}");
        _dateDisplay(display ? 100L : 0L, 1);
        
        // FlowFunctionWrapper.CallFlowFunctionSetup(display ? 1L : 0L);
        //
        // _dateDisplayFlow();
        //
        // FlowFunctionWrapper.CallFlowFunctionCleanup();
    }

    private Month GetMonthFromTotalDays(int totalGameDays)
    {
        int month = 3;
        totalGameDays %= 365;
        // Looks a little odd because it mimics the decompiled C version of this method in the game.
        int i = 0;
        do
        {
            if (totalGameDays < DAYS_IN_MONTH[month % 12])
                break;
            totalGameDays -= DAYS_IN_MONTH[month % 12];
            i += 1;
            month += 1 % 12;
        } while (i < 12);

        return (Month)month;
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

        unsafe
        {
            OnDateChanged.Invoke(DateInfoAddress->currTotalDays, DateInfoAddress->currTime);
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
        if ((_loopDates.Contains(dateInfo->currTotalDays) || SETUP_TOTAL_DAY == dateInfo->currTotalDays) 
            && dateInfo->currTime < 6) return;

        if (dateInfo->currTotalDays < SETUP_TOTAL_DAY)
        {
            MyLogger.DebugLog("Going to setup day.");
            dateInfo->nextTime = SETUP_TIME;
            dateInfo->nextTotalDays = SETUP_TOTAL_DAY;
            return;
        }

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