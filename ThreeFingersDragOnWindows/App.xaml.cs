using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.UI.Xaml;
using ThreeFingersDragOnWindows.settings;
using ThreeFingersDragOnWindows.touchpad;
using ThreeFingersDragOnWindows.utils;

namespace ThreeFingersDragOnWindows;

public partial class App : Application {
    public static SettingsData SettingsData;
    private SettingsWindow _settingsWindow;


    public HandlerWindow HandlerWindow;


    public App(){
        Debug.WriteLine("Starting ThreeFingersDragOnWindows...");
        InitializeComponent();

        SettingsData = SettingsData.load();

        if(SettingsData.IsFirstRun){
            OpenSettingsWindow();
            Utils.runOnMainThreadAfter(3000, () => HandlerWindow = new HandlerWindow(this));
        } else{
            HandlerWindow = new HandlerWindow(this);
        }
    }


    public void OpenSettingsWindow(){
        _settingsWindow ??= new SettingsWindow(this);
        _settingsWindow.Activate();
    }

    public void OnClosePrefsWindow(){
        _settingsWindow = null;
    }

    public void Quit(){
        HandlerWindow?.Close();
        _settingsWindow?.Close();
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
        if (dir == null) throw new Exception("Could not get the directory of the current assembly.");
        return Path.Combine(dir.FullName, "ThreeFingersDragEngine.exe");
    }
}