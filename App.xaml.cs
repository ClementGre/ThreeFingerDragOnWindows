using H.NotifyIcon;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System.Diagnostics;
using System.Timers;
using ThreeFingersDragOnWindows.src.settings;
using ThreeFingersDragOnWindows.src.touchpad;
using ThreeFingersDragOnWindows.src.utils;

namespace ThreeFingersDragOnWindows;

public partial class App : Application
{

    public static SettingsData SettingsData;
    private SettingsWindow _settingsWindow;


    public HandlerWindow HandlerWindow;
    

    public App()
    {
        Debug.WriteLine("Starting ThreeFingersDragOnWindows...");
        this.InitializeComponent();

        SettingsData = SettingsData.load();

        if (SettingsData.IsFirstRun) {
            OpenSettingsWindow();
            Utils.runOnMainThreadAfter(3000, () => HandlerWindow = new HandlerWindow(this));
        }
        else {
            HandlerWindow = new HandlerWindow(this);
        }

    }



    public void OpenSettingsWindow()
    {
         _settingsWindow ??= new SettingsWindow(this);
        _settingsWindow.Activate();
    }

    public void OnClosePrefsWindow()
    {
        _settingsWindow = null;
    }

    public void Quit()
    {
        HandlerWindow?.Close();
        _settingsWindow?.Close();
    }

    public void OnTouchpadContact(TouchpadContact[] contacts)
    {
        _settingsWindow?.OnTouchpadContact(contacts);
    }

    public bool DoTouchpadExist()
    {
        return HandlerWindow.TouchpadExists;
    }

    public bool DoTouchpadRegistered()
    {
        return HandlerWindow.TouchpadRegistered;
    }


}

