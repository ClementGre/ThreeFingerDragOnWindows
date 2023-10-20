using System.ComponentModel;
using System.Diagnostics;
using Microsoft.UI.Dispatching;
using ThreeFingersDragEngine.utils;
using ThreeFingersDragOnWindows.settings;
using ThreeFingersDragOnWindows.touchpad;
using ThreeFingersDragOnWindows.utils;

namespace ThreeFingersDragOnWindows;

public partial class App {

    public readonly DispatcherQueue DispatcherQueue;

    public static App Instance;
    public static SettingsData SettingsData;
    private SettingsWindow _settingsWindow;

    public HandlerWindow HandlerWindow;

    public App(){
        Instance = this;
        DispatcherQueue = DispatcherQueue.GetForCurrentThread();
        SettingsData = SettingsData.load();
        
        if(SettingsData.RunElevated && !Utils.IsAppRunningAsAdministrator()){
            if(RestartElevated()) return;
        }
        
        Debug.WriteLine("Starting ThreeFingersDragOnWindows...");
        InitializeComponent();
        
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

    public static bool RestartElevated(){
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
        Instance.Quit();
        return true;
    }
    
    public void OnTouchpadContact(TouchpadContact[] contacts){
        _settingsWindow?.OnTouchpadContact(contacts);
    }
    public void OnTouchpadInitialized(){
        _settingsWindow?.OnTouchpadInitialized();
    }

    public bool DoTouchpadExist(){
        return HandlerWindow.TouchpadExists;
    }

    public bool DoTouchpadRegistered(){
        return HandlerWindow.TouchpadRegistered;
    }
}