using ArchipelagoP5RMod.Types;

namespace ArchipelagoP5RMod.GameCommunicators;

public static class SequenceMonitor
{
    // ReSharper disable once FieldCanBeMadeReadOnly.Local
    private static unsafe SequenceObj** _sequence;

    public static unsafe SequenceType CurrentSequenceType
    {
        get
        {
            if (_sequence == null || _sequence == (SequenceObj*)0x0 || *_sequence == (SequenceObj*)0x0 ||
                (*_sequence)->args == (SequenceInfo*)0x0)
            {
                return SequenceType.None;
            }

            return (*_sequence)->args->CurrentSequence;
        }
    }

    public static unsafe SequenceType LastSequenceType
    {
        get
        {
            if (_sequence == null || _sequence == (SequenceObj*)0x0 || *_sequence == (SequenceObj*)0x0 ||
                (*_sequence)->args == (SequenceInfo*)0x0)
            {
                return SequenceType.None;
            }

            return (*_sequence)->args->LastSequence;
        }
    }

    public static bool SequenceCanShowMessage => CurrentSequenceType is SequenceType.Battle or SequenceType.Field;

    public static unsafe void Setup()
    {
        AddressScanner.DelayedAddressHack(0x2902558, address => _sequence = (SequenceObj**)address);
    }
}