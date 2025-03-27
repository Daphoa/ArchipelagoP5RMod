using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Mod.Interfaces;

namespace ArchipelagoP5RMod;

public class GameSaveLoadConnector
{
    public event GameSavedLoaded OnGameFileLoaded;
    public event GameSavedLoaded OnGameFileSaved;

    public delegate void GameSavedLoaded(uint fileIndex);

    [Function(CallingConventions.Fastcall)]
    private delegate IntPtr AppStorageReadWrite(int param1, uint fileIndex);

    private readonly IHook<AppStorageReadWrite> _appStorageReadHook;
    private readonly IHook<AppStorageReadWrite> _appStorageWriteHook;

    private readonly ILogger _logger;

    public GameSaveLoadConnector(IReloadedHooks hooks, ILogger logger)
    {
        _logger = logger;

        _appStorageReadHook = hooks
            .CreateHook<AppStorageReadWrite>(AppStorageReadImpl,
                AddressScanner.Addresses[AddressScanner.AddressName.AppStorageReadFuncAddress])
            .Activate();
        _appStorageWriteHook = hooks
            .CreateHook<AppStorageReadWrite>(AppStorageWriteImpl,
                AddressScanner.Addresses[AddressScanner.AddressName.AppStorageWriteFuncAddress])
            .Activate();
    }

    private IntPtr AppStorageReadImpl(int param1, uint fileIndex)
    {
        IntPtr val = _appStorageReadHook.OriginalFunction(param1, fileIndex);
        _logger.WriteLine($"AppStorageRead called with {nameof(fileIndex)} {fileIndex}");
        if (fileIndex > 0)
        {
            OnGameFileLoaded?.Invoke(fileIndex);
            // TODO add a separate event for save/load system file. 
        }

        return val;
    }

    private IntPtr AppStorageWriteImpl(int param1, uint fileIndex)
    {
        IntPtr val = _appStorageWriteHook.OriginalFunction(param1, fileIndex);
        _logger.WriteLine($"AppStorageWrite called with {nameof(fileIndex)} {fileIndex}");
        if (fileIndex > 0)
        {
            OnGameFileSaved?.Invoke(fileIndex);
            // TODO add a separate event for save/load system file. 
        }

        return val;
    }
}