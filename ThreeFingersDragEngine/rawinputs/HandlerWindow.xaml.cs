using System;
using System.Diagnostics;
using System.Windows;
using ThreeFingersDragEngine;
using ThreeFingersDragEngine.touchpad;
using ThreeFingersDragEngine.utils;

namespace ThreeFingersDragEngine.rawinputs;

public sealed partial class HandlerWindow {
    private readonly App _app;
    private readonly ContactsManager _contactsManager;
    private readonly ThreeFingersDrag _threeFingersDrag;

    public bool TouchpadExists;
    public bool TouchpadRegistered;

    public HandlerWindow(App app){
        _app = app;
        Console.WriteLine("Starting HandlerWindow...");
        InitializeComponent();
        
        _contactsManager = new ContactsManager(this);
        _threeFingersDrag = new ThreeFingersDrag();

        Show();
        Hide();
        WindowState = WindowState.Minimized;
        ShowInTaskbar = false;
        
        _contactsManager.InitializeSource();
    }

    public void OnTouchpadInitialized(bool touchpadExists, bool touchpadRegistered){
        TouchpadExists = touchpadExists;
        TouchpadRegistered = touchpadRegistered;
        if(!touchpadExists) Debug.WriteLine("Touchpad is not detected.");
        else if(!touchpadRegistered) Debug.WriteLine("Touchpad is detected but not registered.");
        else Debug.WriteLine("Touchpad is detected and registered.");
        
        // TODO: Send data to WinUI app
    }

    // Called when a new set of contacts has been registered
    
    private TouchpadContact[] _oldContacts = Array.Empty<TouchpadContact>();
    private long _lastContactCtms = Ctms();
    
    public void OnTouchpadContact(TouchpadContact[] contacts){
        _threeFingersDrag.OnTouchpadContact(_oldContacts, contacts, Ctms() - _lastContactCtms);
        // TODO: Send data to WinUI app
        // _app.OnTouchpadContact(contacts); // Transfer to App for displaying contacts in SettingsWindow
        
        _lastContactCtms = Ctms();
        _oldContacts = contacts;
    }
    
    private static long Ctms(){
        return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
    }
}