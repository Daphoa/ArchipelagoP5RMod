using ArchipelagoP5RMod.GameCommunicators;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Mod.Interfaces;

namespace ArchipelagoP5RMod;

using idType = uint;

public class ConfidantManipulator
{
    [Function(CallingConventions.Fastcall)]
    private delegate long CmmCheckEnableFunc(uint funcId);

    [Function(CallingConventions.Fastcall)]
    private delegate IntPtr CmmSetLv(ushort cmmId, short cmmLv);

    private readonly FlagManipulator _flagManipulator;

    private IHook<CmmCheckEnableFunc> _cmmCheckEnableFuncHook;
    private IHook<CmmSetLv> _cmmSetLvHook;
    private static ILogger _logger;


    private static readonly HashSet<idType> allCmmFuncIds =
    [
        0x34, // Hierophant: Coffee Basics (Coffee 1)
        0x36, // Hierophant: Coffee Mastery (Coffee 2)
        0x37, // Hierophant: LeBlanc Curry (Curry 1)
        0x39, // Hierophant: Curry Tips (Curry 2)
        0x3B, // Hierophant: Curry Master (Curry 3)
        0x4B, // Chariot: Punk Talk
        0x49, // Chariot: Follow Up
        0x11B, // Chariot: Stealth Dash
        0x4A, // Chariot: Harisen Recovery
        0x50, // Chariot: Insta-kill
        0x4D, // Chariot: Endure
        0x4F, // Chariot: Protect
        0xED, // Chariot: Second Awakening
        0xF9, // Chariot: Second Awakening | Bit: 0x100000E7
        0x83, // Death: Rejuvenation
        0x85, // Death: Sterilization
        0x89, // Death: Immunization
        0x87, // Death: Discount
        0x8B, // Death: Resuscitation
    ];

    // TODO get this from AP settings eventually.
    private readonly HashSet<idType> _controlledCmmFuncIds = [..allCmmFuncIds];

    // TODO move this to flag manipulator so they are saved.
    private readonly HashSet<idType> _acquiredCmmFuncIds = [];

    public event OnCmmSetLvEvent OnCmmSetLv;

    public delegate void OnCmmSetLvEvent(ushort cmmId, short cmmLv);


    public ConfidantManipulator(FlagManipulator flagManipulator, IReloadedHooks hooks, ILogger logger)
    {
        _flagManipulator = flagManipulator;
        _logger = logger;
        AddressScanner.DelayedScanPattern(
            "40 53 55 56 41 54 41 56 48 83 EC 20",
            address => _cmmCheckEnableFuncHook =
                hooks.CreateHook<CmmCheckEnableFunc>(CmmCheckEnableFuncImpl, address).Activate());
        AddressScanner.DelayedScanPattern(
            "66 85 C9 0F 84 ?? ?? ?? ?? 57",
            address => _cmmSetLvHook = hooks.CreateHook<CmmSetLv>(CmmSetLvImpl, address).Activate());

        logger.WriteLine("Created ItemManipulator Hooks");
    }

    public bool EnableCmmFeature(uint feature)
    {
        if (!allCmmFuncIds.Contains(feature))
        {
            _logger.WriteLine(
                $"{nameof(EnableCmmFeature)} called with {nameof(feature)}:{feature} but it's not supported.");
            return false;
        }

        _logger.WriteLine($"{nameof(EnableCmmFeature)} called. {nameof(feature)}:{feature} enabled.");
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

        _logger.WriteLine($"CmmSetLv called with id: {cmmId} | lv: {cmmLv}");

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
            FlowFunctionWrapper.CallCustomFlowFunction(ApMethodsIndexes.NotifyConfidantReward);
        }

        e.Handled = true;
    }

    #region Save/Load

    public byte[] SaveEnabledCmmData()
    {
        MemoryStream stream = new();

        foreach (var id in _acquiredCmmFuncIds)
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
                    _logger.WriteLine("WARNING: Read unusual data while loading CMM data.");
                }
                return;
            }

            idType cmmAbility = BitConverter.ToUInt32(buffer);
            _acquiredCmmFuncIds.Add(cmmAbility);
        }
    }

    #endregion
}