using System.Reflection;
using System.Runtime.InteropServices;
using Reloaded.Mod.Interfaces;

namespace ArchipelagoP5RMod;

public static class BfLoader
{
    private static ILogger _logger;
    private static GCHandle _apMethodsBfFile;

    public static unsafe byte* ApMethodsBfFilePointer => (byte*)_apMethodsBfFile.AddrOfPinnedObject();
    public static uint ApMethodsBfFileLength { get; private set; }

    public static void Setup(ILogger logger)
    {
        _logger = logger;
        if (!_apMethodsBfFile.IsAllocated)
        {
            LoadFileIntoMemory();
        }
    }

    private static void LoadFileIntoMemory()
    {
        foreach (string name in Assembly.GetExecutingAssembly().GetManifestResourceNames())
        {
            if (name.EndsWith("AP_Methods.bf", StringComparison.InvariantCultureIgnoreCase))
            {
                using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
                using MemoryStream memStream = new MemoryStream();
                stream?.CopyTo(memStream);
                _apMethodsBfFile = GCHandle.Alloc(memStream.ToArray(), GCHandleType.Pinned);
                ApMethodsBfFileLength = (uint)memStream.Length;
                break;
            }
        }
    }
}