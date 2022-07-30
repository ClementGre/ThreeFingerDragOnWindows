using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using ThreeFingersDragOnWindows.src.utils;

namespace ThreeFingersDragOnWindows.src;

public partial class App {
    public static UserPreferences Prefs;


    private readonly NotifyIcon _notifyIcon;
    private PrefsWindow _prefsWindow;

    public App(){
        _notifyIcon = new NotifyIcon();
        Prefs = UserPreferences.load();
    }

    private HandlerWindow GetHandlerWindow(){
        return (HandlerWindow) MainWindow;
    }

    protected override void OnStartup(StartupEventArgs e){
        _notifyIcon.Icon = new Icon("Resources/icon.ico");
        _notifyIcon.Text = "ThreeFingersDragOnWindows";
        _notifyIcon.Click += (_, _) => ShowPrefsWindow();

        var button = new ToolStripButton("Quit");
        button.Click += (_, _) => Quit();
        _notifyIcon.ContextMenuStrip = new ContextMenuStrip();
        _notifyIcon.ContextMenuStrip.Items.Add(button);

        _notifyIcon.Visible = true;

        MainWindow = new HandlerWindow(this);
        if(UserPreferences.IsFirstRun) ShowPrefsWindow();
        
        base.OnStartup(e);
    }

    private void ShowPrefsWindow(){
        _prefsWindow ??= new PrefsWindow(this);
        _prefsWindow.Show();
    }

    public void OnClosePrefsWindow(){
        _prefsWindow = null;
    }

    public void Quit(){
        _notifyIcon.Dispose();
        Shutdown();
    }

    public void OnTouchpadContact(TouchpadContact[] contacts){
        _prefsWindow?.OnTouchpadContact(contacts);
    }

    public bool DoTouchpadExist(){
        return GetHandlerWindow().TouchpadExists;
    }

    public bool DoTouchpadRegistered(){
        return GetHandlerWindow().TouchpadRegistered;
    }
}