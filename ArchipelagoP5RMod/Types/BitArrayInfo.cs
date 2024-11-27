using System.Runtime.InteropServices;

namespace ArchipelagoP5RMod.Types;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct BitFlagArrayInfo
{
    [FieldOffset(0x0)] public uint* bitArrayAdr;
    [FieldOffset(0x8)] public long bitArrayLength;
}
