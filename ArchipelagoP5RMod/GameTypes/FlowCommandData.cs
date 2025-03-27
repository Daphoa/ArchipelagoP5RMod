using System.Runtime.InteropServices;

namespace ArchipelagoP5RMod.Types;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct FlowCommandData
{
    [FieldOffset(0x0)] public fixed char CurrFuncName[40];
    [FieldOffset(0x28)] public int CurrInstructionIndex;

    [FieldOffset(0x2c)] public int StackSize;
    [FieldOffset(0x30)] public fixed byte ArgTypes[0x2f];
    [FieldOffset(0x5f)] public FlowReturnType ReturnType;
    [FieldOffset(0x60)] public fixed long ArgData[0x2f];
    [FieldOffset(0x1d8)] public long ReturnValue;

    [FieldOffset(0x1e0)] public IntPtr FileHeader;
    [FieldOffset(0x1e8)] public IntPtr FileLabels;
    [FieldOffset(0x1f0)] public IntPtr ProcedureEntries;
    [FieldOffset(0x1f8)] public IntPtr LabelEntries;
    [FieldOffset(0x200)] public IntPtr InstructionData;
    [FieldOffset(0x208)] public IntPtr MessageScriptData;
    [FieldOffset(0x210)] public IntPtr StringData;
    
    [FieldOffset(0x218)] public IntPtr CurrFuncIndex;

    public override string ToString()
    {
        return $"Stack size: {StackSize}, Return value: {ReturnValue}, Arg0: {ArgData[0]}";
    }
}

public enum FlowReturnType : byte
{
    Int = 0,
    Float = 1,
}

public enum FlowParamType : byte
{
    Int = 4,
    Float = 1,
    IntPtr = 2,
    FloatPtr = 3
}