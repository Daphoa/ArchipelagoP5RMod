using System.Runtime.InteropServices;

namespace ArchipelagoP5RMod.Types;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct FlowCommandData
{
    [FieldOffset(0x0)]
    public fixed uint unknownHeader[11];
    
    [FieldOffset(0x2c)]
    public int StackSize;

    [FieldOffset(0x30)]
    public fixed byte ArgTypes[0x2f];

    [FieldOffset(0x5f)]
    public FlowReturnType ReturnType;

    [FieldOffset(0x60)]
    public fixed long ArgData[0x2f];
    
    // [FieldOffset(0x11c)]
    // private fixed uint unknownData[0x2f];

    [FieldOffset(0x1d8)]
    public long ReturnValue;

    public override string ToString()
    {
        return $"Stack size: {StackSize}, Return value: {ReturnValue}, Arg0: {ArgData[0]}";
    }
}

public enum FlowReturnType : byte
{
    Boolean=0,
    Int=1,
    Float=2
}

public enum FlowParamType : byte
{
    Int = 4,
    Float = 1,
    IntPtr = 2,
    FloatPtr = 3
}