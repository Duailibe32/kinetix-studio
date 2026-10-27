using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace KinetixStudio
{
    public static class EmbeddedIcon
    {
        delegate bool ResourceName(IntPtr module, IntPtr type, IntPtr name, IntPtr data);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern bool EnumResourceNames(IntPtr module, IntPtr type, ResourceName callback, IntPtr data);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr LoadImage(IntPtr module, IntPtr name, uint type, int width, int height, uint flags);
        public static Icon Load()
        {
            IntPtr module = Native.GetModuleHandle(null), name = IntPtr.Zero;
            ResourceName callback = delegate (IntPtr m, IntPtr t, IntPtr n, IntPtr d)
            {
                name = n;
                return false;
            };
            EnumResourceNames(module, (IntPtr)14, callback, IntPtr.Zero);
            if (name != IntPtr.Zero)
            {
                IntPtr handle = LoadImage(module, name, 1, 32, 32, 0x8000);
                if (handle != IntPtr.Zero)
                    using (Icon shared = Icon.FromHandle(handle))
                        return (Icon)shared.Clone();
            }

            return (Icon)SystemIcons.Application.Clone();
        }
    }
}
