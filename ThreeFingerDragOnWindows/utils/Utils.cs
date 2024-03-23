using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace ThreeFingerDragOnWindows.utils;

class Utils {
    public static void runOnMainThreadAfter(int ms, Action action){
        runOnMainThreadAfter(ms, DispatcherQueue.GetForCurrentThread(), action);
    }

    public static void runOnMainThreadAfter(int ms, DispatcherQueue queue, Action action){
        var timer = new System.Timers.Timer(ms);
        timer.Elapsed += (_, _) => queue.TryEnqueue(() => action());
        timer.AutoReset = false;
        timer.Start();
    }
    
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    public static void FocusWindow(Window window){
        IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        SetForegroundWindow(hWnd);
    }
    
    public static bool IsAppRunningAsAdministrator(){
        var identity = new WindowsPrincipal(WindowsIdentity.GetCurrent());
        return identity.IsInRole(WindowsBuiltInRole.Administrator);
    }
    
    
    public static string GetElevatorPath(){
        // It is necessary to use the path of the parent directory to use the .exe file instead of the .dll file.
        var dir = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;
        if (dir == null) throw new Exception("Could not get the directory of the current assembly.");
        return Path.Combine(dir.FullName, "ThreeFingerDragElevator.exe");
    }
    public static string GetAppPath()
    {
        // It is necessary to use the path of the parent directory to use the .exe file instead of the .dll file.
        var dir = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;
        if (dir == null) throw new Exception("Could not get the directory of the current assembly.");
        return Path.Combine(dir.FullName, "ThreeFingerDragOnWindows.exe");
    }
}