using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using Windows.System;

namespace ThreeFingersDragEngine.utils;

internal static class Utils {
    public static void RunOnMainThreadAfter(int ms, Action action){
        var queue = DispatcherQueue.GetForCurrentThread();
        var timer = new System.Timers.Timer(ms);
        timer.Elapsed += (_, _) => queue.TryEnqueue(() => action());
        timer.AutoReset = false;
        timer.Start();
    }
    
    
    
    public static void StartWinUIApp(){
        ProcessStartInfo processInfo = new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = "ThreeFingersDragOnWindows.exe",
            Arguments = "FromWPFEngine"
        };
        try{
            Process.Start(processInfo);
        }catch(Win32Exception ex){
            Debug.WriteLine(ex);
        }
    }

    public static bool IsAppRunningAsAdministrator(){
        var identity = new WindowsPrincipal(WindowsIdentity.GetCurrent());
        return identity.IsInRole(WindowsBuiltInRole.Administrator);
    }
    
    
    public static string GetEnginePath(){
        // It is necessary to use the path of the parent directory to use the .exe file instead of the .dll file.
        var dir = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;
        if (dir == null) throw new Exception("Could not get the directory of the current assembly.");
        return Path.Combine(dir.FullName, "ThreeFingersDragEngine.exe");
    }
}