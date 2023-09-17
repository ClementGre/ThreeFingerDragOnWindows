using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using ThreeFingersDragEngine.utils;
using ThreeFingersDragOnWindows.settings;
using ThreeFingersDragOnWindows.touchpad;
using ThreeFingersDragOnWindows.utils;

namespace ThreeFingersDragOnWindows;




public partial class App {

    public readonly DispatcherQueue DispatcherQueue;

    public static SettingsData SettingsData;
    private SettingsWindow _settingsWindow;

    public HandlerWindow HandlerWindow;

    public App(){
        if(!Utils.IsAppRunningAsAdministrator()){
            if(RestartElevated()) return;
        }
        
        Debug.WriteLine("Starting ThreeFingersDragOnWindows...");
        InitializeComponent();
        
        DispatcherQueue = DispatcherQueue.GetForCurrentThread();
        SettingsData = SettingsData.load();

        if(SettingsData.IsFirstRun){
            OpenSettingsWindow();
            Utils.runOnMainThreadAfter(3000, () => HandlerWindow = new HandlerWindow(this));
        } else{
            HandlerWindow = new HandlerWindow(this);
        }
    }


    public void OpenSettingsWindow(){
        Debug.WriteLine("Opening SettingsWindow...");
        _settingsWindow ??= new SettingsWindow(this);
        _settingsWindow.Activate();
        Utils.FocusWindow(_settingsWindow);
    }

    public void OnClosePrefsWindow(){
        _settingsWindow = null;
    }

    public void Quit(){
        HandlerWindow?.Close();
        _settingsWindow?.Close();
    }

    public bool RestartElevated(){
        var path = Utils.GetElevatorPath();
        Debug.WriteLine("Running the Elevator app at " + path);
        ProcessStartInfo processInfo = new ProcessStartInfo{
            Verb = "runas",
            UseShellExecute = true,
            FileName = path
        };

        try{
            Process.Start(processInfo);
        } catch(Win32Exception ex){
            // Probably the user canceled the UAC window, 
            Debug.WriteLine(ex);
            return false;
        }
        // Close app
        Quit();
        return true;
    }
    
    public void OnTouchpadContact(TouchpadContact[] contacts){
        _settingsWindow?.OnTouchpadContact(contacts);
    }

    public bool DoTouchpadExist(){
        return HandlerWindow.TouchpadExists;
    }

    public bool DoTouchpadRegistered(){
        return HandlerWindow.TouchpadRegistered;
    }
}