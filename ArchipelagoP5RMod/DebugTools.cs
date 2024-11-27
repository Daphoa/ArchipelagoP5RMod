using System.Runtime.InteropServices;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;

namespace ArchipelagoP5RMod;

public class DebugTools
{
    public bool HasFlagBackup => _flagBackup is not null;

    private uint[][]? _flagBackup;
    private readonly uint[] _flagLengths = [3072, 2442, 5120, 512, 512, 512]; 
    // private readonly int[] _flagLengths = [512]; 
    
    public unsafe void BackupCurrentFlags()
    {
        var bitFlagSectionMap = AddressScanner.BitFlagSectionMap;

        _flagBackup = null;
        _flagBackup = new uint[_flagLengths.Length][];

        for (var section = 0; section < _flagLengths.Length; section++)
        {
            var currBitArray = bitFlagSectionMap[section].bitArrayAdr;

            var flagGroupLen = _flagLengths[section] / (sizeof(uint) * 8);
            
            _flagBackup[section] = new uint[flagGroupLen];
            for (var flagGroup = 0; flagGroup < flagGroupLen; flagGroup++)
            {
                _flagBackup[section][flagGroup] = currBitArray[flagGroup];
            }
        }
    }

    public unsafe List<BitAddress> FindChangedFlags(ILogger? logger = null)
    {
        if (_flagBackup == null)
        {
            throw new NullReferenceException("_flagBackup was null: Tried to find changed flags before backup");
        }
        
        List<BitAddress> changedFlags = [];
        var bitFlagSectionMap = AddressScanner.BitFlagSectionMap;

        for (var section = 0; section < _flagLengths.Length; section++)
        {
            var currBitArray = bitFlagSectionMap[section].bitArrayAdr;
            
            var flagGroupLen = _flagLengths[section] / (sizeof(uint) * 8);
            for (var flagGroup = 0; flagGroup < flagGroupLen; flagGroup++)
            {
                if (_flagBackup[section][flagGroup] == currBitArray[flagGroup]) 
                    continue;

                var changedBits = _flagBackup[section][flagGroup] ^ currBitArray[flagGroup];

                for (var i = 0; i < sizeof(uint) * 8; i++)
                {
                    if (((1 << i) & changedBits) == 0) 
                        continue;

                    var value = ((1 << 1) & currBitArray[flagGroup]) == 0;
                    var flag = flagGroup * sizeof(uint) * 8 + i;

                    var changedFlag = new BitAddress
                    {
                        Section = section,
                        ID = flag
                    };
                    changedFlags.Add(changedFlag);
                    logger?.WriteLine(
                        $"Found changed bit: 0x{section:X}0000000 + {flag} | value: {value} | adr: {(IntPtr)(&currBitArray[flagGroup]):X}");
                }
               
            }
        }

        if (logger != null && changedFlags.Count == 0)
        {
            logger.WriteLine("FindChangedFlags: No changes found");
        }

        return changedFlags;
    }

    public struct BitAddress
    {
        public int Section;
        public int ID;
    }
}