using System;
using System.Windows;
using ThreeFingersDragOnWindows.src.touchpad;
using ThreeFingersDragOnWindows.src.utils;

namespace ThreeFingersDragOnWindows.src;

public sealed partial class HandlerWindow : IContactsManager {
    private readonly App _app;
    private readonly ContactsManager<HandlerWindow> _contactsManager;
    private readonly ThreeFingersDrag _threeFingersDrag;

    public bool TouchpadExists;
    public bool TouchpadRegistered;

    public HandlerWindow(App app){
        Console.WriteLine("Starting HandlerWindow...");

        _app = app;
        _contactsManager = new ContactsManager<HandlerWindow>(this);
        InitializeComponent();

        _threeFingersDrag = new ThreeFingersDrag();

        Show();
        Hide();
        WindowState = WindowState.Minimized;
        ShowInTaskbar = false;
    }

    public void OnTouchpadInitialized(bool touchpadExists, bool touchpadRegistered){
        TouchpadExists = touchpadExists;
        TouchpadRegistered = touchpadRegistered;
        if(!touchpadExists) Console.WriteLine("Touchpad is not detected.");
        else if(!touchpadRegistered) Console.WriteLine("Touchpad is detected but not registered.");
        else Console.WriteLine("Touchpad is detected and registered.");
    }

    public void OnTouchpadContact(TouchpadContact[] contacts){
        _threeFingersDrag.OnTouchpadContact(contacts);
        _app.OnTouchpadContact(contacts);
    }

    protected override void OnSourceInitialized(EventArgs e){
        _contactsManager.InitializeSource();
        base.OnSourceInitialized(e);
    }
}