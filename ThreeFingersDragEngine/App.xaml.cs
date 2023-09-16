using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using ThreeFingersDragEngine.rawinputs;
using ThreeFingersDragEngine.touchpad;
using ThreeFingersDragEngine.utils;

namespace ThreeFingersDragEngine;

public partial class App {
    
    public static SettingsData SettingsData;
    private HandlerWindow _handlerWindow;
    
    // Kill: taskkill /IM ThreeFingersDragEngine.exe /F
    protected override void OnStartup(StartupEventArgs e){
        Debug.WriteLine("Starting ThreeFingersDragEngine...");

        if(IsInstanceAlreadyRunning()){
            Debug.WriteLine("Already an instance is running, shutting down...");
            Current.Shutdown();
        }
        
        if(!e.Args.Contains("FromWinUIApp")){
            Debug.WriteLine("Not started from WinUI app -> starting the WinUI app.");
            Utils.StartWinUIApp();
        }

        SettingsData = new SettingsData();
        _handlerWindow = new HandlerWindow(this);
        
        base.OnStartup(e);
    }

    private bool IsInstanceAlreadyRunning(){
        Process proc = Process.GetCurrentProcess();
        int count = Process.GetProcesses().Count(p => p.ProcessName == proc.ProcessName);
        return count > 1;
    }
    
    

    
    
}