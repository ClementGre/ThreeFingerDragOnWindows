using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ThreeFingersDragOnWindows.touchpad;

// From https://www.travelneil.com/wndproc-in-uwp.html
internal class Interop {
    public delegate IntPtr WndProcDelegate(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

    private const int GWLP_WNDPROC = -4;
    private static WndProcDelegate _currDelegate;

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")] //32-bit
    public static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")] // 64-bit
    public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hwnd, uint msg, IntPtr wParam,
        IntPtr lParam);

    // Returns a pointer to the previous WndProc function.
    public static IntPtr SetWndProc(IntPtr hwnd, WndProcDelegate newProc){
        // Assign the delegate to a static variable, so that garbage collector won't 
        // wipe it out from underneath us
        _currDelegate = newProc;

        var functionPointer = Marshal.GetFunctionPointerForDelegate(newProc);
        if(IntPtr.Size == 8)
            return SetWindowLongPtr(hwnd, GWLP_WNDPROC, functionPointer);
        return SetWindowLong(hwnd, GWLP_WNDPROC, functionPointer);
    }
}