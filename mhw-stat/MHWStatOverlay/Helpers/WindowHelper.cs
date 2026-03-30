using System.Windows;
using System.Windows.Interop;

namespace MHWStatOverlay.Helpers;

public static class WindowHelper
{
    public static void SetClickThrough(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        int extStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
        NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE,
            extStyle | NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_LAYERED);
    }

    public static void RemoveClickThrough(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        int extStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
        NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE,
            extStyle & ~NativeMethods.WS_EX_TRANSPARENT);
    }

    public static void SetTopmost(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        NativeMethods.SetWindowPos(hwnd, (IntPtr)NativeMethods.HWND_TOPMOST,
            0, 0, 0, 0,
            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
    }

    public static void HideFromTaskbar(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        int extStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
        NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE,
            extStyle | NativeMethods.WS_EX_TOOLWINDOW);
    }
}
