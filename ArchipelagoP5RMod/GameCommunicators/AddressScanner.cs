using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ArchipelagoP5RMod.Types;
using Reloaded.Memory.Sigscan;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;

namespace ArchipelagoP5RMod;

public static class AddressScanner
{
    private static Dictionary<string, Action<IntPtr>> scanRequests = new();
    private static Dictionary<nint, Action<IntPtr>> offsetRequests = new();

    public static bool HasScanned { get; private set; }

    private static ILogger _logger;

    public static unsafe BitFlagArrayInfo* BitFlagSectionMap { get; private set; }

    private static IntPtr _baseAddress;
    private static int _exeSize;

    /**
     * WARNING: This is hacky and only intended for development or for situations where sig patterns are impossible.
     * Sig Patterns are strongly preferred.
     */
    public static void DelayedAddressHack(nint offset, Action<IntPtr> onResponse)
    {
        if (HasScanned)
        {
            // I could spit out an error... but it's a hack anyway so whatever.
            onResponse.Invoke(_baseAddress + offset);
        }
        else
        {
            offsetRequests.Add(offset, onResponse);
        }
    }

    public static void DelayedScanPattern(string pattern, Action<IntPtr> onResponse, bool suppressWarning = false)
    {
        if (HasScanned)
        {
            if (!suppressWarning)
            {
                _logger.WriteLine("[WARNING] Registered address after scan was completed. " +
                                   "This has significant performance impact.");
                _logger.WriteLine(Environment.StackTrace);
            }

            unsafe
            {
                var scanner = new Scanner((byte*)_baseAddress, _exeSize);
                var result = scanner.FindPattern(pattern);

                onResponse(_baseAddress + result.Offset);
            }

            return;
        }

        if (scanRequests.ContainsKey(pattern))
        {
            scanRequests[pattern] += onResponse;
        }
        else
        {
            scanRequests.Add(pattern, onResponse);
        }
    }

    public static void Scan(ILogger logger)
    {
        _logger = logger;

        var thisProcess = Process.GetCurrentProcess();

        if (thisProcess.MainModule == null)
        {
            throw new InvalidOperationException("The process cannot be found.");
        }

        _baseAddress = thisProcess.MainModule.BaseAddress;
        _exeSize = thisProcess.MainModule.ModuleMemorySize;

        unsafe
        {
            var scanner = new Scanner((byte*)_baseAddress, _exeSize);

            var patterns = scanRequests.Keys.ToArray();
            // Do the search for all results in parallel.
            var results = scanner.FindPatterns(patterns);

            for (int i = 0; i < results.Length; i++)
            {
                var pattern = patterns[i];
                var result = results[i];
                if (!result.Found)
                    throw new Exception($"Signature for function {pattern} not found.");

                scanRequests[pattern].Invoke(_baseAddress + result.Offset);
            }

            foreach (var offsetRequest in offsetRequests)
            {
                offsetRequest.Value.Invoke(_baseAddress + offsetRequest.Key);
            }
        }

        offsetRequests.Clear();
        scanRequests.Clear();
        HasScanned = true;
    }
}