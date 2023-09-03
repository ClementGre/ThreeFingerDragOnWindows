using H.NotifyIcon;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System;
using System.Diagnostics;
using System.Windows.Input;
using ThreeFingersDragOnWindows.src.Utils;

namespace ThreeFingersDragOnWindows.src.touchpad;

public sealed partial class HandlerWindow : Window//, IContactsManager
{
    private readonly App _app;
    //private readonly ContactsManager<HandlerWindow> _contactsManager;
    //private readonly ThreeFingersDrag _threeFingersDrag;

    public bool TouchpadExists;
    public bool TouchpadRegistered;

    public HandlerWindow(App app)
    {
        Debug.WriteLine("Starting HandlerWindow...");
        this.InitializeComponent();

        _app = app;
        //_contactsManager = new ContactsManager<HandlerWindow>(this);

        //_threeFingersDrag = new ThreeFingersDrag();



        //WindowExtensions.Hide(this, false);
        //Show();
        //Hide();
        //WindowState = WindowState.Minimized;
        //ShowInTaskbar = false;
    }
    private void OpenSettingsWindow(object sender, ExecuteRequestedEventArgs e)
    {
        Debug.WriteLine("Opening SettingsWindow from HandlerWindow TaskbarIcon");
        _app.OpenSettingsWindow();
    }
    private void QuitApp(object sender, ExecuteRequestedEventArgs e)
    {
        Debug.WriteLine("Quitting App from HandlerWindow TaskbarIcon");
        _app.Quit();
    }



    /* public void OnTouchpadInitialized(bool touchpadExists, bool touchpadRegistered)
     {
         TouchpadExists = touchpadExists;
         TouchpadRegistered = touchpadRegistered;
         if (!touchpadExists) Debug.WriteLine("Touchpad is not detected.");
         else if (!touchpadRegistered) Debug.WriteLine("Touchpad is detected but not registered.");
         else Debug.WriteLine("Touchpad is detected and registered.");
     }


     public void OnTouchpadContact(TouchpadContact[] contacts)
     {
         _threeFingersDrag.OnTouchpadContact(contacts);
         _app.OnTouchpadContact(contacts);
     }

     protected void OnSourceInitialized(EventArgs e)
     {
         _contactsManager.InitializeSource();
     }*/
}