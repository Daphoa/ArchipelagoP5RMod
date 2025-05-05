namespace ArchipelagoP5RMod;

/**
 * A class to manage some unique situations that aren't easily handled in other ways
 */
public static class CustomLogic
{
    private const ushort RANDY_RIGHT_EYE_ITEM_ID = 0x4000 + 116;
    private const ushort LUSTFUL_LEFT_EYE_ITEM_ID = 0x4000 + 117;

    private static ItemManipulator _itemManipulator;
    private static FlagManipulator _flagManipulator;

    public static void Setup(ItemManipulator itemManipulator, FlagManipulator flagManipulator)
    {
        _itemManipulator = itemManipulator;
        _flagManipulator = flagManipulator;

        _itemManipulator.OnItemCountChanged += CheckForRightLeftEyeGate;
    }

    public static void CheckForRightLeftEyeGate(ushort itemId, byte itemNum)
    {
        if (itemId != RANDY_RIGHT_EYE_ITEM_ID && itemId != LUSTFUL_LEFT_EYE_ITEM_ID)
            return;

        if (!_itemManipulator.HasItem(RANDY_RIGHT_EYE_ITEM_ID) ||
            !_itemManipulator.HasItem(LUSTFUL_LEFT_EYE_ITEM_ID))
        {
            return;
        }

        MyLogger.DebugLog("We have both eyes, so setting flags to true.");
        // Eye statue setup
        _flagManipulator.SetBit(9647, true);
        _flagManipulator.SetBit(6410, true);
        _flagManipulator.SetBit(6412, true);
        _flagManipulator.SetBit(6413, true);
        _flagManipulator.SetBit(6462, true);
        _flagManipulator.SetBit(6492, false);
        _flagManipulator.SetBit(6683, true);

        // Found Key 1
        _flagManipulator.SetBit(1908, true);
        _flagManipulator.SetBit(6390, true);
        _flagManipulator.SetBit(6439, true);
        _flagManipulator.SetBit(6463, true);

        // Found Key 2
        _flagManipulator.SetBit(1909, true);
        _flagManipulator.SetBit(6440, true);
        _flagManipulator.SetBit(6464, true);
        _flagManipulator.SetBit(6492, true);
        _flagManipulator.SetBit(11492, true);
        _flagManipulator.SetBit(11560, true);

        _flagManipulator.SetBit(11492, true); // This is probably the most important one.
    }
}