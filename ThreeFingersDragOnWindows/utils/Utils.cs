using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using ThreeFingersDragOnWindows.settings;

namespace ThreeFingersDragOnWindows.utils;

class Utils {
    public static void runOnMainThreadAfter(int ms, Action action){
        var queue = DispatcherQueue.GetForCurrentThread();
        var timer = new System.Timers.Timer(ms);
        timer.Elapsed += (_, _) => queue.TryEnqueue(() => action());
        timer.AutoReset = false;
        timer.Start();
    }
    public static void runOnMainThread(Action action){
        DispatcherQueue.GetForCurrentThread().TryEnqueue(() => action());
    }
    
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    public static void FocusWindow(Window window){
        IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        SetForegroundWindow(hWnd);
    }
}