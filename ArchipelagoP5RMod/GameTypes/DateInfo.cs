using System.Runtime.InteropServices;

namespace ArchipelagoP5RMod.Types;

[StructLayout(LayoutKind.Explicit)]
public struct DateInfo
{
    [FieldOffset(0x3)] public byte unknown_flag;
    
    [FieldOffset(0x2)] public byte currTime;
    [FieldOffset(0x0)] public short currTotalDays;

    [FieldOffset(0x6)] public byte nextTime;
    [FieldOffset(0x4)] public short nextTotalDays;

    public override string ToString()
    {
        return
            $"current_time:{currTime}, curr_total_days:{currTotalDays}, " +
            $"next_time:{nextTime}, next_total_days:{nextTotalDays}, unknown_flag:{unknown_flag}";
    }
}