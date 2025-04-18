using System.Runtime.InteropServices;

namespace ArchipelagoP5RMod.Types;

[StructLayout(LayoutKind.Explicit)]
public struct GameTask
{
    [FieldOffset(0x0)] public uint eventType;
    [FieldOffset(0x30)] public IntPtr runtimeFunc;
    [FieldOffset(0x40)] public IntPtr onCompleteFunc;
    [FieldOffset(0x48)] public IntPtr args;
}