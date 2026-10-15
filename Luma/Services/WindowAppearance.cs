using System.Runtime.InteropServices;

namespace Luma.Services;

internal static class WindowAppearance
{
    public static void ApplyDarkTitle(IntPtr handle, bool dark)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763)) return;
        int enabled = dark ? 1 : 0;
        // Earlier Windows 10 releases use attribute 19; newer builds use 20.
        if (DwmSetWindowAttribute(handle, 20, ref enabled, sizeof(int)) < 0)
            DwmSetWindowAttribute(handle, 19, ref enabled, sizeof(int));
    }

    public static void ApplyCorners(IntPtr handle)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) return;
        int rounded = 2;
        DwmSetWindowAttribute(handle, 33, ref rounded, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr handle, int attribute, ref int value, int size);
}
