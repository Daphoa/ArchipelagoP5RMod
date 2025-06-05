using System.ComponentModel.DataAnnotations;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace ArchipelagoP5RMod;

public static class ByteTools
{
    public static unsafe long CStrLen(char* str)
    {
        char* s;
        for (s = str; *s != (char)0; ++s)
        {
        }

        return s - str;
    }

    public static unsafe string CStrToString(char* str)
    {
        int len = (int)CStrLen(str);

        var managedArray = new byte[len];

        Marshal.Copy((IntPtr)str, managedArray, 0, len);
        return Encoding.UTF8.GetString(managedArray);
    }

    public static byte[] StringToCStr(string str)
    {
        return Encoding.UTF8.GetBytes(str);
    }

    public static byte[] CollectionToByteArray<T>(IEnumerable<T> collection, Func<T, byte[]> toByteFunc)
        where T : IMinMaxValue<T>
    {
        return CollectionToByteArray(collection, toByteFunc, T.MaxValue);
    }

    public static byte[] CollectionToByteArray<T>(IEnumerable<T> collection, Func<T, byte[]> toByteFunc, T footerVal)
    {
        MemoryStream stream = new MemoryStream();

        foreach (var item in collection)
        {
            stream.Write(toByteFunc(item));
        }

        stream.Write(toByteFunc(footerVal));

        return stream.ToArray();
    }

    public static TC ByteArrayToCollection<TC, T>(MemoryStream data, int size, Func<byte[], T> fromByteFunc)
        where TC : ICollection<T>, new() where T : IMinMaxValue<T>
    {
        return ByteArrayToCollection<TC, T>(data, size, fromByteFunc, T.MaxValue);
    }
    
    public static TC ByteArrayToCollection<TC, T>(MemoryStream data, int size, Func<byte[], T> fromByteFunc, T footerVal)
        where TC : ICollection<T>, new()
    {
        var collection = new TC();

        while (true)
        {
            byte[] buffer = new byte[size];
            int readBytes = data.Read(buffer, 0, size);

            if (readBytes < size)
            {
                MyLogger.DebugLog("WARNING: Read unusual data while loading CMM data.");

                break;
            }

            var value = fromByteFunc(buffer);
            if (value == null || value.Equals(footerVal))
            {
                // Max value is the footer of the collection.
                break;
            }

            collection.Add(value);
        }

        return collection;
    }
}