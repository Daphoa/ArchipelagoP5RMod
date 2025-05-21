using ArchipelagoP5RMod.GameCommunicators;
using ArchipelagoP5RMod.Types;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;

namespace ArchipelagoP5RMod;

using idType = uint;

public class ConfidantManipulator
{
    [Function(CallingConventions.Fastcall)]
    private delegate long CmmCheckEnableFunc(uint funcId);

    [Function(CallingConventions.Fastcall)]
    private delegate IntPtr CmmSetLv(ushort cmmId, short cmmLv);

    [Function(CallingConventions.Fastcall)]
    public delegate void CmmOpenFunc(ushort cmmId);

    private IntPtr _getCmmOpenPtr;

    public CmmOpenFunc _cmmOpen { get; private set; }

    private readonly FlagManipulator _flagManipulator;

    private IHook<CmmCheckEnableFunc> _cmmCheckEnableFuncHook;
    private IHook<CmmSetLv> _cmmSetLvHook;


    private static readonly HashSet<idType> allCmmFuncIds =
    [
        0x01, // Fool: Wild Talk 
        0x02, // Fool: Third Eye 
        0x03, // Fool: Arcana Burst 
        0x04, // Fool: Power Stock (8) 
        0x05, // Fool: Super Stock (10) 
        0x06, // Fool: High Arcana Burst  
        0x08, // Fool: Ultra Stock (12)  
        0x0A, // Fool: Max Arcana Burst
        0x0B, // Magician: Infiltration Tools
        0x0C, // Magician: Follow Up
        0x0F, // Magician: Kitty Talk
        0x4E, // Magician: Pickpocket
        0x0E, // Magician: Ace Tools
        0x12, // Magician: Harisen Recovery
        0x11, // Magician: Endure
        0x13, // Magician: Protect
        0xE7, // Magician: Second Awakening
        0xF1, // Magician: Second Awakening | Bit: 0x100000E1 (3rd)
        0x15, // Priestess: Shadow Calculus
        0x17, // Priestess: Black Belt Talk
        0x16, // Priestess: Follow Up
        0x1A, // Priestess: Harisen Recovery
        0x1E, // Priestess: Shadow Factorization
        0x1B, // Priestess: Endure
        0x1D, // Priestess: Protect
        0xE8, // Priestess: Second Awakening
        0xF2, // Priestess: Second Awakening | Bit: 0x100000E2 (3rd)
        0xDD, // Empress: Cultivation
        0x20, // Empress: Follow Up
        0x21, // Empress: Celeb Talk
        0x22, // Empress: Bumper Crop
        0x1F, // Empress: Harisen Recovery
        0x25, // Empress: Soil Improvement
        0x26, // Empress: Endure
        0x27, // Empress: Protect
        0xE9, // Empress: Second Awakening
        0xF4, // Empress: Second Awakening | Bit: 0x100000E3 (3rd)
        0x29, // Emperor: Card Duplication
        0x2A, // Emperor: Follow Up
        0x2B, // Emperor: Art Talk
        0x112, // Emperor: Card Creation
        0x2E, // Emperor: Harisen Recovery
        0x113, // Emperor: Live Painting
        0x2F, // Emperor: Endure
        0x31, // Emperor: Protect
        0xEA, // Emperor: Second Awakening
        0xF5, // Emperor: Second Awakening | Bit: 0x100000E4 (3rd)
        0x34, // Hierophant: Coffee Basics (Coffee 1)
        0x36, // Hierophant: Coffee Mastery (Coffee 2)
        0x37, // Hierophant: LeBlanc Curry (Curry 1)
        0x39, // Hierophant: Curry Tips (Curry 2)
        0x3B, // Hierophant: Curry Master (Curry 3)
        0x3D, // Lovers: Harisen Recovery
        0x3E, // Lovers: Girl Talk
        0x3F, // Lovers: Follow Up
        0x40, // Lovers: Crocodile Tears
        0x43, // Lovers: Endure
        0x45, // Lovers: Protect
        0x119, // Lovers: Sexy Technique♥
        0xEC, // Lovers: Second Awakening
        0xF6, // Lovers: Second Awakening | Bit: 0x100000E6 (3rd)
        0x4B, // Chariot: Punk Talk
        0x49, // Chariot: Follow Up
        0x11B, // Chariot: Stealth Dash
        0x4A, // Chariot: Harisen Recovery
        0x50, // Chariot: Insta-kill
        0x4D, // Chariot: Endure
        0x4F, // Chariot: Protect
        0xED, // Chariot: Second Awakening
        0xF9, // Chariot: Second Awakening | Bit: 0x100000E7 (3rd)
        // TODO note Justice is weird and needs more investigation
        0xF0, // Justice: Sleuthing Instinct
        0x57, // Justice: Rank 3 | Bit: 0x10000706
        0xF3, // Justice: Sleuthing Mastery
        0x56, // Justice: Rank 6 | Bit: 0x10000706
        0x55, // Justice: Harisen Recovery
        0xF7, // Justice: Rank 10 
        0xF8, // Justice: Rank 10 Bit: 0xA4
        0x100, // Justice: Rank 10 Bit: 0x100000E8 (3rd)
        0x5B, // Hermit: Moral Support
        0x5C, // Hermit: Mementos Scan
        0x5E, // Hermit: Position Hack
        0x60, // Hermit: Active Support
        0x61, // Hermit: Treasure Reboot
        0x63, // Hermit: Emergency Shift 
        0x64, // Hermit: Final Guard 
        0xEF, // Hermit: Second Awakening 
        0x109, // Hermit: Second Awakening | Bit: 0x100000E9 (3rd)
        0x69, // Fortune: Luck Reading
        0x6A, // Fortune: Money Reading
        0x6C, // Fortune: Affinity Reading
        0x6E, // Fortune: Special Fate Reading
        0x10F, // Fortune: Celestial Reading
        0x11D, // Fortune: True Affinity Reading
        0x7D, // Hanged Man: Discount 
        0x114, // Hanged Man: Starter Customization 
        0x115, // Hanged Man: Camo Customization 
        0x116, // Hanged Man: Expert Customization  
        0x117, // Hanged Man: On The House 
        0x83, // Death: Rejuvenation
        0x85, // Death: Sterilization
        0x89, // Death: Immunization
        0x87, // Death: Discount
        0x8B, // Death: Resuscitation
        0x8D, // Temperance: Slack Off
        0x8F, // Temperance: Housekeeping
        0x91, // Temperance: Free Time
        0x93, // Temperance: Super Housekeeping
        0x96, // Temperance: Special Massage
        0xBE, // Moon: Mishima's Support
        0xB5, // Moon: Mishima's Enthusiasm
        0x24, // Moon: Mishima's Desperation
        0xB7, // Moon: Phanboy
        0x28, // Moon: Salvation Wish
        0xC0, // Sun: Diplomacy
        0xC1, // Sun: Fundraising
        0xC3, // Sun: Manipulation
        0xC6, // Sun: Mind Control
        0xC8, // Sun: Charismatic Speech
        0xFA, // Faith: Tumbling
        0xFE, // Faith: Chaînés Hook
        0xFC, // Faith: Follow Up
        0xFD, // Faith: Fitness Talk
        0xFF, // Faith: Harisen Recovery
        0x101, // Faith: Endure
        0x102, // Faith: Protect
        0x103, // Faith: Second Awakening
        0x10C, // Faith: Second Awakening | Bit: 0x100000EA (3rd)
        0x105, // Councillor: Detox X
        0x106, // Councillor: Detox DX
        0x107, // Councillor: Mindfulness
        0x108, // Councillor: Wakefulness
        0x10A, // Councillor: Flow
        0x10B, // Councillor: Flow Boost
    ];

    // TODO get this from AP settings eventually.
    private readonly HashSet<idType> _controlledCmmFuncIds = [..allCmmFuncIds];

    // TODO move this to flag manipulator so they are saved.
    private readonly HashSet<idType> _acquiredCmmFuncIds =
    [
        0x01, // Fool: Wild Talk 
        0x02, // Fool: Third Eye 
        0x0B, // Magician: Infiltration Tools
    ];

    public event OnCmmSetLvEvent OnCmmSetLv;

    public delegate void OnCmmSetLvEvent(ushort cmmId, short cmmLv);


    public ConfidantManipulator(FlagManipulator flagManipulator, IReloadedHooks hooks)
    {
        _flagManipulator = flagManipulator;
        AddressScanner.DelayedScanPattern(
            "40 53 55 56 41 54 41 56 48 83 EC 20",
            address => _cmmCheckEnableFuncHook =
                hooks.CreateHook<CmmCheckEnableFunc>(CmmCheckEnableFuncImpl, address).Activate());
        AddressScanner.DelayedScanPattern(
            "66 85 C9 0F 84 ?? ?? ?? ?? 57",
            address => _cmmSetLvHook = hooks.CreateHook<CmmSetLv>(CmmSetLvImpl, address).Activate());
        AddressScanner.DelayedScanPattern(
            "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B 35 ?? ?? ?? ?? 33 FF 0F B7 D9",
            address => _cmmOpen =
                hooks.CreateWrapper<CmmOpenFunc>(address, out _getCmmOpenPtr));

        MyLogger.DebugLog("Created ItemManipulator Hooks");
    }

    public void CmmOpen(Confidant confidant)
    {
        _cmmOpen((ushort)confidant);
    }

    public bool EnableCmmFeature(uint feature)
    {
        if (!allCmmFuncIds.Contains(feature))
        {
            MyLogger.DebugLog(
                $"{nameof(EnableCmmFeature)} called with {nameof(feature)}:{feature:X} but it's not supported.");
            return false;
        }

        MyLogger.DebugLog($"{nameof(EnableCmmFeature)} called. {nameof(feature)}:{feature:X} enabled.");
        return _acquiredCmmFuncIds.Add(feature);
    }

    public void ResetEnabledCmmFeatures()
    {
        _acquiredCmmFuncIds.Clear();
    }

    private long CmmCheckEnableFuncImpl(idType funcId)
    {
        // _logger.WriteLine($"{nameof(CmmCheckEnableFuncImpl)} called with {nameof(funcId)}: {funcId}");
        if (!_controlledCmmFuncIds.Contains(funcId))
        {
            // Fallback on original if the value isn't controlled by us.
            return _cmmCheckEnableFuncHook.OriginalFunction(funcId);
        }

        return _acquiredCmmFuncIds.Contains(funcId) ? 1 : 0;
    }

    private IntPtr CmmSetLvImpl(ushort cmmId, short cmmLv)
    {
        IntPtr val = _cmmSetLvHook.OriginalFunction(cmmId, cmmLv);

        MyLogger.DebugLog($"CmmSetLv called with id: {cmmId} | lv: {cmmLv}");

        OnCmmSetLv?.Invoke(cmmId, cmmLv);

        return val;
    }

    public void HandleApItem(object? sender, ApConnector.ApItemReceivedEvent e)
    {
        if (e.Handled || e.ApItem.Type != ItemType.CmmAbility ||
            _flagManipulator.CheckBit(FlagManipulator.SHOWING_MESSAGE)
            || _flagManipulator.CheckBit(FlagManipulator.SHOWING_GAME_MSG) || !SequenceMonitor.SequenceCanShowMessage)

            return;

        bool cmmFeatureEnabled = EnableCmmFeature(e.ApItem.Id);
        if (cmmFeatureEnabled)
        {
            // Only show notification for cmm that were actually newly enabled.
            _flagManipulator.SetBit(FlagManipulator.SHOWING_MESSAGE, true);
            _flagManipulator.SetCount(FlagManipulator.AP_CURR_REWARD_CMM_ABILITY, e.ApItem.Id);
            FlowFunctionWrapper.CallCustomFlowFunction(CustomApMethodsIndexes.NotifyConfidantAbilityReward);
        }

        e.Handled = true;
    }

    #region Save/Load

    public byte[] SaveEnabledCmmData()
    {
        MemoryStream stream = new();

        foreach (idType id in _acquiredCmmFuncIds)
        {
            stream.Write(BitConverter.GetBytes(id));
        }

        // EOL
        stream.WriteByte(0x0);

        return stream.ToArray();
    }

    public void LoadEnabledCmmData(MemoryStream data)
    {
        _acquiredCmmFuncIds.Clear();

        while (true)
        {
            byte[] buffer = new byte[sizeof(idType)];
            int readBytes = data.Read(buffer, 0, sizeof(idType));

            if (readBytes < sizeof(idType))
            {
                if (readBytes < 1 || buffer[0] != 0x0)
                {
                    MyLogger.DebugLog("WARNING: Read unusual data while loading CMM data.");
                }

                return;
            }

            idType cmmAbility = BitConverter.ToUInt32(buffer);
            _acquiredCmmFuncIds.Add(cmmAbility);
        }
    }

    #endregion
}