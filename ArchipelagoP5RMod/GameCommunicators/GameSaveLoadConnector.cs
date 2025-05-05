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

    private IHook<AppStorageReadWrite> _appStorageReadHook;
    private IHook<AppStorageReadWrite> _appStorageWriteHook;

    public GameSaveLoadConnector(IReloadedHooks hooks)
    {
        AddressScanner.DelayedScanPattern(
            "48 89 5C 24 ?? 57 48 83 EC 60 89 CB",
            address => _appStorageReadHook =
                hooks.CreateHook<AppStorageReadWrite>(AppStorageReadImpl, address).Activate());

        AddressScanner.DelayedScanPattern(
            "48 89 5C 24 ?? 57 48 83 EC 60 8B D9 0F B7 FA",
            address => _appStorageWriteHook = 
                hooks.CreateHook<AppStorageReadWrite>(AppStorageWriteImpl, address).Activate());
    }

    private IntPtr AppStorageReadImpl(int param1, uint fileIndex)
    {
        IntPtr val = _appStorageReadHook.OriginalFunction(param1, fileIndex);
        MyLogger.DebugLog($"AppStorageRead called with {nameof(fileIndex)} {fileIndex}");
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
        MyLogger.DebugLog($"AppStorageWrite called with {nameof(fileIndex)} {fileIndex}");
        if (fileIndex > 0)
        {
            OnGameFileSaved?.Invoke(fileIndex);
            // TODO add a separate event for save/load system file. 
        }

        return val;
    }
}