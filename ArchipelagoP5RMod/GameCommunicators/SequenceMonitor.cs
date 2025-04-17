using ArchipelagoP5RMod.Types;

namespace ArchipelagoP5RMod.GameCommunicators;

public class SequenceMonitor
{
    // ReSharper disable once FieldCanBeMadeReadOnly.Local
    private unsafe SequenceObj** _sequence;

    public unsafe SequenceType CurrentSequenceType
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

    public unsafe SequenceType LastSequenceType
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

    public unsafe SequenceMonitor()
    {
        AddressScanner.DelayedAddressHack(0x2902558, address => _sequence = (SequenceObj**)address);
    }
}