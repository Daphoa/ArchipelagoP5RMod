using System.Runtime.InteropServices;
using System.Text;

namespace ArchipelagoP5RMod;

public static class StrTools
{
    public static unsafe long CStrLen(char* str)
    {
        char* s;
        for (s = str; *s != (char)0; ++s) { }

        return s - str;
    }

    public static unsafe string CStrToString(char* str)
    {
        int len = (int)CStrLen(str);

        var managedArray = new byte[len];

        Marshal.Copy((IntPtr)str, managedArray, 0, len);
        return Encoding.UTF8.GetString(managedArray);
    }
}