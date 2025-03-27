using System.Runtime.InteropServices.ComTypes;
using Reloaded.Memory.Extensions;
using Reloaded.Mod.Interfaces;

namespace ArchipelagoP5RMod;

public class ModSaveLoadHandler
{
    private readonly ILogger _logger;
    private readonly Dictionary<byte, Func<byte[]>> _registeredSaveMethods = new();
    private readonly Dictionary<byte, Action<byte[]>> _registeredLoadMethods = new();

    private static readonly byte[] header = [0xAA, 0xAA, 0x0, 0xBD];
    private static readonly byte[] sectionHeader = [0x4A, 0x0, 0x34];

    public ModSaveLoadHandler(ILogger logger, SaveLoadConnector saveLoadConnector)
    {
        _logger = logger;
    }

    public void RegisterSaveLoad(Func<byte[]> saveMethod, Action<byte[]> loadMethod, int section = -1)
    {
        byte sectionNum = (byte)section;
        if (section == -1)
        {
            sectionNum = 0;
            while (_registeredSaveMethods.ContainsKey(sectionNum)) sectionNum++;
        }

        _registeredSaveMethods.Add(sectionNum, saveMethod);
        _registeredLoadMethods.Add(sectionNum, loadMethod);
    }


    private void Save(Stream writeStream)
    {
        writeStream.Write(header, 0, header.Length);

        foreach (var section in _registeredSaveMethods)
        {
            writeStream.Write(sectionHeader, 0, sectionHeader.Length);
            writeStream.WriteByte(section.Key);
            var sectionBytes = section.Value();
            writeStream.Write(sectionBytes, 0, sectionBytes.Length);
        }
    }

    private void Load(Stream readStream)
    {
        byte[] buffer = new byte[header.Length];
        readStream.Read(buffer, 0, header.Length);

        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] == header[i]) continue;
            
            _logger.Write("Loaded header: ");
            foreach (byte t in buffer)
            {
                _logger.Write($"{t:X2} ");
            }
            _logger.WriteLine("");
            
            _logger.Write("Expected header: ");
            foreach (byte t in header)
            {
                _logger.Write($"{t:X2} ");
            }
            _logger.WriteLine("");

            
            throw new InvalidDataException("Tried to read file, but no header was present.");
        }

        MemoryStream sectionData = new MemoryStream();
        int matchedSectionHeader = 0;
        byte activeSection = Byte.MaxValue;
        while (true)
        {
            int nextByte = readStream.ReadByte();

            if (nextByte == -1)
            {
                // We're done
                if (activeSection != Byte.MaxValue)
                {
                    _registeredLoadMethods[activeSection](sectionData.ToArray());
                }

                break;
            }

            if (nextByte == sectionHeader[matchedSectionHeader])
            {
                matchedSectionHeader++;
                if (matchedSectionHeader != sectionHeader.Length)
                    continue;

                if (activeSection != Byte.MaxValue)
                {
                    _registeredLoadMethods[activeSection](sectionData.ToArray());
                }

                activeSection = (byte)readStream.ReadByte();
                sectionData = new MemoryStream();
                matchedSectionHeader = 0;

                continue;
            }

            // Since we don't write to stream when we see a potential match for the header, we need to backfill the data.
            for (int i = 0; i < matchedSectionHeader; i++)
            {
                sectionData.WriteByte(sectionHeader[i]);
            }

            matchedSectionHeader = 0;

            sectionData.WriteByte((byte)nextByte);
        }
    }

    #region TestingCode

    private ModSaveLoadHandler(ILogger logger)
    {
        _logger = logger;
    }

    private class TestSection
    {
        private byte[] data;
        private ILogger logger;

        public TestSection(Random rand, ModSaveLoadHandler modSaveLoadHandler, ILogger logger)
        {
            this.logger = logger;

            var size = rand.Next(10, 500);
            data = new byte[size];
            rand.NextBytes(data);

            modSaveLoadHandler.RegisterSaveLoad(() => data, CompareValues);
        }

        private void CompareValues(byte[] newData)
        {
            for (int i = 0; i < newData.Length; i++)
            {
                if (newData[i] == data[i]) continue;
                logger.WriteLine($"Original data wasn't equal to new data at index {i}: {newData[i]:X2} expected: {data[i]:X2}");
                    
                logger.Write("New Data:      ");
                foreach (byte t in newData)
                {
                    logger.Write($"{t:X2} ");
                }
                logger.WriteLine("");
            
                logger.Write("Expected data: ");
                foreach (byte t in data)
                {
                    logger.Write($"{t:X2} ");
                }
                logger.WriteLine("");
                return;
            }

            logger.WriteLine("TestSection: Equal");
        }
    }

    public static void TestSaveLoad(ILogger logger)
    {
        logger.WriteLine("Testing Save/Load");
        
        var rand = new Random();

        const int numAttempts = 5;

        for (int i = 0; i < numAttempts; i++)
        {
            logger.WriteLine($"Starting attempt {i}");

            var handler = new ModSaveLoadHandler(logger);
            logger.WriteLine("Created handler");

            int numSection = rand.Next(2, 10);
            logger.WriteLine($"Testing with {numSection} sections");
            List<TestSection> testSections = new List<TestSection>();
            for (int j = 0; j < numSection; j++)
            {
                testSections.Add(new TestSection(rand, handler, logger));
                logger.WriteLine($"Created section {j}");
            }
            
            MemoryStream testSaveFile = new MemoryStream();
            logger.WriteLine($"Trying to save file");
            handler.Save(testSaveFile);
            logger.WriteLine($"File saved, moving \"file\" position to start for load");
      
            testSaveFile.Seek(0, SeekOrigin.Begin);
            
            logger.WriteLine($"Trying to load file (position: {testSaveFile.Position})");
            handler.Load(testSaveFile);
        }
    }

    #endregion
}