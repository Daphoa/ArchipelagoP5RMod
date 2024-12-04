namespace ArchipelagoP5RMod;

public class FlagManager
{
    private readonly uint[] _onBits =
    [
        0x20000000 + 493,
        0x20000000 + 482
    ];

    public void Setup(FlagManipulator flagManipulator)
    {
        foreach (uint adr in _onBits)
        {
            flagManipulator.SetBit(adr, true);
        }
    }
}