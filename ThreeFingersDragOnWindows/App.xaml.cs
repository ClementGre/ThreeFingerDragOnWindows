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
using ThreeFingersDragOnWindows.dialogs;
using ThreeFingersDragOnWindows.settings;
using ThreeFingersDragOnWindows.touchpad;
using ThreeFingersDragOnWindows.utils;
using Application = Microsoft.UI.Xaml.Application;
using Style = Microsoft.UI.Xaml.Style;

namespace ThreeFingersDragOnWindows;




public partial class App : Application {

    public DispatcherQueue DispatcherQueue;

    public static SettingsData SettingsData;
    private SettingsWindow _settingsWindow;

    public HandlerWindow HandlerWindow;

    public App(){
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
        
        
        
        if(!Utils.IsAppRunningAsAdministrator()){
             RestartElevated();
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
        var path = GetEnginePath();
        Debug.WriteLine("Runnig the Elevator app at " + path);
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


    public static string GetEnginePath(){
        var dir = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;
        if(dir == null) throw new Exception("Could not get the directory of the current assembly.");
        return Path.Combine(dir.FullName, "ThreeFingersDragEngine.exe");
    }
}