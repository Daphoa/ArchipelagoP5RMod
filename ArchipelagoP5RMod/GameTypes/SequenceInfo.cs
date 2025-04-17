using System.Runtime.InteropServices;

namespace ArchipelagoP5RMod.Types;

[StructLayout(LayoutKind.Explicit)]
public struct SequenceObj
{
    [FieldOffset(0x48)] public unsafe SequenceInfo* args;
}

[StructLayout(LayoutKind.Explicit)]
public struct SequenceInfo
{
    [FieldOffset(0x4)] public SequenceType CurrentSequence;
    [FieldOffset(0x8)] public SequenceType LastSequence;
}

public enum SequenceType : int
{
    None = -1,
    Title = 0x0,
    TitleRapid = 0x1,
    Load = 0x2,
    Field = 0x3,
    Battle = 0x4, // This seems to be used quite a bit, including field. Needs more testing, but probably should check
                  // if we are in this state before showing anything.
    FieldViewer = 0x5,
    Event = 0x6,
    EventViewer = 0x7,
    Movie = 0x8,
    MovieViewer = 0x9,
    InitRead = 0xa,
    Calendar = 0xb,
    CalendarReset = 0xc,
    DungeonResult = 0xd,
}