﻿using System.Text;

namespace ArchipelagoP5RMod;

public class ModSaveLoadManager(string saveDirectory)
{
    private const string Filename = "{0}/AP_Mod_Save_Data_{1:d2}";

    private readonly Dictionary<byte, Func<byte[]>> _registeredSaveMethods = new();
    private readonly Dictionary<byte, Action<MemoryStream>> _registeredLoadMethods = new();

    private static readonly byte[] header = [0xAA, 0xAA, 0x0, 0xBD];
    private static readonly byte[] sectionHeader = [0x4A, 0x5, 0x34];

    public delegate void LoadCompleteEventHandler(uint index, bool success);

    public event LoadCompleteEventHandler OnLoadComplete;

    private bool lastSaveIndexHadFile = false;

    public void RegisterSaveLoad(Func<byte[]> saveMethod, Action<MemoryStream> loadMethod, int section = -1)
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
    
    private string GetFilename(uint fileIndex)
    {
        return String.Format(Filename, saveDirectory, fileIndex);
    }

    public void Save(uint fileIndex)
    {
        if (fileIndex == 0)
        {
            return;
        }
        
        if (string.IsNullOrEmpty(saveDirectory))
        {
            MyLogger.DebugLog("Got empty save directory.");
            return;
        }

        string fileName = GetFilename(fileIndex);
        string backupFileName = fileName + "_bak";

        // Delete the backup file if it exists.
        if (File.Exists(backupFileName))
        {
            File.Delete(backupFileName);
        }

        // Move the original file to the backup file if it exists
        if (File.Exists(fileName))
        {
            File.Move(fileName, backupFileName);
        }

        try
        {
            // Create the file.
            using FileStream fs = File.Create(fileName);
            SaveToStream(fs);
            // D:\Documents\AP Mod Save Directory
        }
        catch (Exception ex)
        {
            MyLogger.DebugLog(ex.Message);
            MyLogger.DebugLog(ex.StackTrace ?? "");
        }
    }

    public void Load(uint fileIndex)
    {
        if (fileIndex == 0)
        {
            return;
        }

        string fileName = GetFilename(fileIndex);

        if (!File.Exists(fileName))
        {
            // Fail gracefully.
            MyLogger.DebugLog($"No file found for index {fileIndex}");

            OnLoadComplete?.Invoke(fileIndex, false);
            return;
        }

        using FileStream fs = File.OpenRead(fileName);
        LoadFromStream(fs);

        OnLoadComplete?.Invoke(fileIndex, true);
    }

    private void SaveToStream(Stream writeStream)
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

    private void LoadFromStream(Stream readStream)
    {
        byte[] buffer = new byte[header.Length];
        readStream.Read(buffer, 0, header.Length);

        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] == header[i]) continue;

            StringBuilder logMsg = new StringBuilder();
            
            logMsg.Append("Loaded header: ");
            foreach (byte t in buffer)
            {
                logMsg.Append($"{t:X2} ");
            }

            logMsg.Append('\n');

            logMsg.Append("Expected header: ");
            foreach (byte t in header)
            {
                logMsg.Append($"{t:X2} ");
            }

            MyLogger.DebugLog(logMsg.ToString());

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
                    sectionData.Position = 0;
                    _registeredLoadMethods[activeSection](sectionData);
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
                    sectionData.Position = 0;
                    _registeredLoadMethods[activeSection](sectionData);
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

    private ModSaveLoadManager() : this(null)
    {
    }

    private class TestSection
    {
        private byte[] data;

        public TestSection(Random rand, ModSaveLoadManager modSaveLoadManager)
        {
            var size = rand.Next(10, 500);
            data = new byte[size];
            rand.NextBytes(data);

            modSaveLoadManager.RegisterSaveLoad(() => data, CompareValues);
        }

        private void CompareValues(MemoryStream newDataStream)
        {
            var newData = newDataStream.ToArray();

            for (int i = 0; i < newData.Length; i++)
            {
                if (newData[i] == data[i]) continue;
                MyLogger.DebugLog(
                    $"Original data wasn't equal to new data at index {i}: {newData[i]:X2} expected: {data[i]:X2}");

                StringBuilder logMsg = new StringBuilder();
                logMsg.Append("New Data:      ");
                foreach (byte t in newData)
                {
                    logMsg.Append($"{t:X2} ");
                }

                logMsg.Append('\n');

                logMsg.Append("Expected data: ");
                foreach (byte t in data)
                {
                    logMsg.Append($"{t:X2} ");
                }

                logMsg.Append('\n');
                MyLogger.DebugLog(logMsg.ToString());
                return;
            }

            MyLogger.DebugLog("TestSection: Equal");
        }
    }

    public static void TestSaveLoad()
    {
        MyLogger.DebugLog("Testing Save/Load");

        var rand = new Random();

        const int numAttempts = 5;

        for (int i = 0; i < numAttempts; i++)
        {
            MyLogger.DebugLog($"Starting attempt {i}");

            var handler = new ModSaveLoadManager();
            MyLogger.DebugLog("Created handler");

            int numSection = rand.Next(2, 10);
            MyLogger.DebugLog($"Testing with {numSection} sections");
            List<TestSection> testSections = [];
            for (int j = 0; j < numSection; j++)
            {
                testSections.Add(new TestSection(rand, handler));
                MyLogger.DebugLog($"Created section {j}");
            }

            MemoryStream testSaveFile = new MemoryStream();
            MyLogger.DebugLog($"Trying to save file");
            handler.SaveToStream(testSaveFile);
            MyLogger.DebugLog($"File saved, moving \"file\" position to start for load");

            testSaveFile.Seek(0, SeekOrigin.Begin);

            MyLogger.DebugLog($"Trying to load file (position: {testSaveFile.Position})");
            handler.LoadFromStream(testSaveFile);
        }
    }

    #endregion
}