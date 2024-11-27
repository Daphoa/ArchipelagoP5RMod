using System.Runtime.InteropServices;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Mod.Interfaces;

namespace ArchipelagoP5RMod;

public class ItemManipulator
{
    private readonly ILogger _logger;

    private readonly IHook<OpenChest> _openChestHook;
    
    [Function(CallingConventions.Fastcall)]
    private unsafe delegate long OpenChest(int* param1, float param2, long param3, float param4);


    public ItemManipulator(IReloadedHooks hooks, ILogger logger)
    {
        _logger = logger;
        unsafe
        {
            _openChestHook = hooks.CreateHook<OpenChest>(OpenChestImpl, AddressScanner.NextTimeFunAddress).Activate();
        }

        logger.WriteLine("Created ItemManipulator Hooks");
    }

    private unsafe long OpenChestImpl(int* param1, float param2, long param3, float param4)
    {
        var debugTools = new DebugTools();
        debugTools.BackupCurrentFlags();
        
        long retVal = _openChestHook.OriginalFunction(param1, param2, param3, param4);

        debugTools.FindChangedFlags(_logger);
        return retVal;
    }

    private unsafe ItemRewardPackage[]* findItemRewardPackage(int* param1)
    {
        throw new NotImplementedException();
    }
    
    [StructLayout(LayoutKind.Explicit)]
    public struct ItemRewardPackage
    {
        // [FieldOffset(0x0)]
        // public uint itemId;
        

    }
}