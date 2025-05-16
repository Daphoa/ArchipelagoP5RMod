namespace ArchipelagoP5RMod;

public class ConquestManager(FlagManipulator flagManipulator)
{
    private readonly FlagManipulator _flagManipulator = flagManipulator;

    public static Palaces TotalDaysToPalace(short totalDays)
    {
        switch (totalDays)
        {
            case >= 17 and <= 31:
                return Palaces.KAMOSHIDA;
            case >= 47 and <= 65:
                return Palaces.MADARAME;
            case >= 81 and <= 99:
                return Palaces.KANESHIRO;
            case >= 116 and <= 142:
                return Palaces.FUTABA;
            case >= 171 and <= 193:
                return Palaces.OKUMURA;
            case >= 212 and <= 233:
                return Palaces.SAE;
            case >= 238 and <= 261:
                return Palaces.SHIDO;
            case 267:
                return Palaces.MEMENTOS_DEPTHS;
            case >= 284 and <= 308:
                return Palaces.MARUKI;
            default:
                return Palaces.NONE;
        }
    }

    public void OnDateChangedHandler(short currTotalDays, byte currTime)
    {
        Palaces palace = TotalDaysToPalace(currTotalDays);
        SetupConquest(palace);
    }

    public void SetupConquest(Palaces palace)
    {
        _flagManipulator.SetBit(6230, false);

        switch (palace)
        {
            case Palaces.KAMOSHIDA:
                _flagManipulator.SetBit(0x20000000 + 201, true);
                _flagManipulator.SetBit(0x20000000 + 200, false);
                _flagManipulator.SetBit(0x20000000 + 249, false);
                break;
            case Palaces.MADARAME:
                _flagManipulator.SetBit(0x20000000 + 600, false);

                // 
                flagManipulator.SetBit(1040, true);
                flagManipulator.SetBit(6393, true);
                break;
        }
    }

}