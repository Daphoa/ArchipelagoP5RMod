namespace ArchipelagoP5RMod;

public readonly struct ApItem(long itemCode)
{
    public long ItemCode => itemCode;
    public ItemType Type { get; } = (ItemType)((itemCode >> (8 * 3)) & 0xFF);
    public byte Count { get; } = (byte)((itemCode >> (8 * 2)) & 0xFF);
    public ushort Id { get; } = (ushort)(itemCode & 0xFFFF);

    public override string ToString()
    {
        return $"{Type}: {Count} x 0x{Id:X}";
    }
}

public enum ItemType
{
    None = 0,
    Item = 1,
    CmmAbility = 2,
}