using H.NotifyIcon;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;
using ThreeFingersDragOnWindows.src.settings;
using ThreeFingersDragOnWindows.src.touchpad;
using ThreeFingersDragOnWindows.src.Utils;

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


        HandlerWindow = new HandlerWindow(this);
        //if (SettingsData.IsFirstRun) ShowSettingsWindow();


        //_notifyIcon.Icon = new Icon("Resources/icon.ico");
        //_notifyIcon.Text = "ThreeFingersDragOnWindows";
        //_notifyIcon.Click += (_, _) => ShowSettingsWindow();

        //var button = new ToolStripButton("Quit");
        //button.Click += (_, _) => Quit();
        //_notifyIcon.ContextMenuStrip = new ContextMenuStrip();
        //_notifyIcon.ContextMenuStrip.Items.Add(button);
        //_notifyIcon.Visible = true;
    }


    public void OpenSettingsWindow()
    {
        _settingsWindow = new SettingsWindow(this);
        _settingsWindow.Activate();
    }

    public void OnClosePrefsWindow()
    {
        SettingsData = null;
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

