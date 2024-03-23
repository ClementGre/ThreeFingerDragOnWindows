using System.ComponentModel;
using System.Diagnostics;
using Microsoft.UI.Dispatching;
using ThreeFingerDragEngine.utils;
using ThreeFingerDragOnWindows.settings;
using ThreeFingerDragOnWindows.touchpad;
using ThreeFingerDragOnWindows.utils;

namespace ThreeFingerDragOnWindows;

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
        Debug.WriteLine("Starting ThreeFingerDragOnWindows...");
        InitializeComponent();
        
        bool openOtherSettings = false;
        if(SettingsData.StartupAction != SettingsData.StartupActionType.NONE){
            if(Utils.IsAppRunningAsAdministrator()){
                openOtherSettings = true;
                switch(SettingsData.StartupAction){
                    case SettingsData.StartupActionType.ENABLE_ELEVATED_RUN_WITH_STARTUP :
                        StartupManager.EnableElevatedStartup();
                        SettingsData.RunElevated = true;
                        break;
                    case SettingsData.StartupActionType.DISABLE_ELEVATED_RUN_WITH_STARTUP :
                        StartupManager.DisableElevatedStartup();
                        _ = StartupManager.EnableUnelevatedStartup();
                        SettingsData.RunElevated = false;
                        break;
                    case SettingsData.StartupActionType.ENABLE_ELEVATED_STARTUP :
                        StartupManager.EnableElevatedStartup();
                        break;
                    case SettingsData.StartupActionType.DISABLE_ELEVATED_STARTUP :
                        StartupManager.DisableElevatedStartup();
                        _ = StartupManager.EnableUnelevatedStartup();
                        break;
                }
            }
            SettingsData.StartupAction = SettingsData.StartupActionType.NONE;
            SettingsData.save();
        }
        
        if(SettingsData.IsFirstRun || openOtherSettings){
            OpenSettingsWindow(openOtherSettings);
            Utils.runOnMainThreadAfter(3000, () => HandlerWindow = new HandlerWindow(this));
        } else{
            HandlerWindow = new HandlerWindow(this);
        }
    }


    public void OpenSettingsWindow(bool openOtherSettings = false){
        Debug.WriteLine("Opening SettingsWindow...");
        _settingsWindow ??= new SettingsWindow(this, openOtherSettings);
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

    public static bool RestartElevated(SettingsData.StartupActionType startupActionType = SettingsData.StartupActionType.NONE){
        SettingsData.StartupAction = startupActionType;
        SettingsData.save();
        
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
            SettingsData.StartupAction = SettingsData.StartupActionType.NONE;
            SettingsData.save();
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