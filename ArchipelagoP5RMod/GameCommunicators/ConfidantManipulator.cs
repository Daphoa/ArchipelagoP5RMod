using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Mod.Interfaces;

namespace ArchipelagoP5RMod;

public class ConfidantManipulator
{

    [Function(CallingConventions.Fastcall)]
    private delegate long CmmCheckEnableFunc(uint funcId);
    [Function(CallingConventions.Fastcall)]
    private delegate IntPtr CmmSetLv(ushort cmmId, short cmmLv);
    
    private readonly IHook<CmmCheckEnableFunc> _cmmCheckEnableFuncHook;
    private readonly IHook<CmmSetLv> _cmmSetLvHook;
    private static ILogger _logger;

    private static readonly HashSet<uint> allCmmFuncIds = [
        11,  // Infiltration Tools ?
        14,  // Advanced  Infiltration Tools ?
        21,  // Shadow Calculus (Enemy Scan Extra Details)
        52,  // Make Coffee (Coffee 1)
        54,  // Coffee Mastery (Coffee 2)
        55,  // LeBlanc Curry (Curry 1)
        57,  // Curry Tips (Curry 2)
        59,  // Curry Master (Curry 3)
        66,  // "Come now, hot stuff. Can't you be a little more gentle with me?"
        91,  // Futaba: Moral Support
        96,  // Futaba: Active Support
        147, // Kawakami: Super Housekeeping
        150, // Kawakami: Massage
        200, // Charismatic Speech 
    ];
    
    // TODO get this from AP settings eventually.
    private HashSet<uint> _controlledCmmFuncIds = [..allCmmFuncIds];
    private HashSet<uint> _acquiredCmmFuncIds = [];
    
    public event OnCmmSetLvEvent OnCmmSetLv;
    
    public delegate void OnCmmSetLvEvent(ushort cmmId, short cmmLv);

    
    public ConfidantManipulator(IReloadedHooks hooks, ILogger logger)
    {
        _logger = logger;
        unsafe
        {
            _cmmCheckEnableFuncHook = hooks
                .CreateHook<CmmCheckEnableFunc>(CmmCheckEnableFuncImpl, AddressScanner.CmmCheckEnableFuncAddress)
                .Activate();
            _cmmSetLvHook = hooks
                .CreateHook<CmmSetLv>(CmmSetLvImpl, AddressScanner.CmmSetLvFuncAddress)
                .Activate();
        }

        logger.WriteLine("Created ItemManipulator Hooks");
    }

    public void EnableCmmFeature(uint feature)
    {
        if (!allCmmFuncIds.Contains(feature))
        {
            _logger.WriteLine($"{nameof(EnableCmmFeature)} called with {nameof(feature)}:{feature} but it's not supported.");
            return; 
        }

        _logger.WriteLine($"{nameof(EnableCmmFeature)} called. {nameof(feature)}:{feature} enabled.");
        _acquiredCmmFuncIds.Add(feature);
    }

    public void ResetEnabledCmmFeatures()
    {
        _acquiredCmmFuncIds.Clear();
    }

    private long CmmCheckEnableFuncImpl(uint funcId)
    {
        // _logger.WriteLine($"{nameof(CmmCheckEnableFuncImpl)} called with {nameof(funcId)}: {funcId}");
        if (!_controlledCmmFuncIds.Contains(funcId))
        {
            // Fallback on original if the value isn't controlled by us.
            return _cmmCheckEnableFuncHook.OriginalFunction(funcId);
        }
        
        return _acquiredCmmFuncIds.Contains(funcId) ? 1 : 0;
    }
    
    private IntPtr CmmSetLvImpl(ushort cmmId, short cmmLv) {
        IntPtr val = _cmmSetLvHook.OriginalFunction(cmmId, cmmLv);
        
        _logger.WriteLine($"CmmSetLv called with id: {cmmId} | lv: {cmmLv}");
        
        OnCmmSetLv?.Invoke(cmmId, cmmLv);

        return val;
    }
}