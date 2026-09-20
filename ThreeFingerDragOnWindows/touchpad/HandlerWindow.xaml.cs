using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using ThreeFingerDragOnWindows.drag;
using ThreeFingerDragOnWindows.doubletapdrag;
using ThreeFingerDragEngine.utils;
using ThreeFingerDragOnWindows.threefingerdrag;
using ThreeFingerDragOnWindows.utils;

namespace ThreeFingerDragOnWindows.touchpad;

public sealed partial class HandlerWindow : Window {
    private readonly App _app;
    private readonly ContactsManager _contactsManager;
    private readonly DragButtonCoordinator _dragButtonCoordinator;
    private readonly DoubleTapDragLock _doubleTapDragLock;
    private readonly ThreeFingerDrag _threeFingersDrag;

    public bool TouchpadInitialized; // Became true when the touchpad check is done, but does not confirm that the touchpad has been registered
    public bool TouchpadExists;
    public bool InputReceiverInstalled;

    public HandlerWindow(App app){
        Logger.Log("Starting HandlerWindow...");
        InitializeComponent();

        _app = app;
        _contactsManager = new ContactsManager(this);
        _dragButtonCoordinator = new DragButtonCoordinator();
        _doubleTapDragLock = new DoubleTapDragLock(
            _dragButtonCoordinator,
            App.SettingsData.DoubleTapDragLockEnabled);
        _threeFingersDrag = new ThreeFingerDrag(_dragButtonCoordinator);
        Closed += (_, _) => ShutdownInputEngines();

        // Let the _handlerWindow to be defined in App.xaml.cs before initializing the source
        Utils.runOnMainThreadAfter(100, () => {
            _contactsManager.InitializeSource();
        });

    }

    // TaskbarIcon Actions
    private void OpenSettingsWindow(object sender, ExecuteRequestedEventArgs e){
        Logger.Log("Opening SettingsWindow from HandlerWindow TaskbarIcon");
        _app.OpenSettingsWindow();
    }

    private void QuitApp(object sender, ExecuteRequestedEventArgs e){
        Logger.Log("Quitting App from HandlerWindow TaskbarIcon");
        _app.Quit();
    }

    public void OnTouchpadReleased(IntPtr currentDevice){
        _doubleTapDragLock.OnTouchpadReleased(currentDevice);
    }

    public void SetDoubleTapDragLockEnabled(bool enabled){
        _doubleTapDragLock.SetEnabled(enabled);
    }

    public void SetThreeFingerDragEnabled(bool enabled){
        if(!enabled) _threeFingersDrag.Stop("feature-disabled");
    }

    private void ShutdownInputEngines(){
        _doubleTapDragLock.Dispose();
        _threeFingersDrag.Dispose();
        _dragButtonCoordinator.ForceRelease("handler-window-closed");
    }


    // Touchpad
    // Called when the touchpad is detected and the events handlers are registered (or not)
    public void OnTouchpadInitialized(bool touchpadExists, bool inputReceiverInstalled){
        TouchpadExists = touchpadExists;
        InputReceiverInstalled = inputReceiverInstalled;
        if(!touchpadExists) Logger.Log("Touchpad is not detected.");
        else if(!inputReceiverInstalled) Logger.Log("Touchpad is detected but the input receiver couldn't be installed.");
        else Logger.Log("Touchpad is detected and registered.");

        if(!touchpadExists || !inputReceiverInstalled){
            _doubleTapDragLock.Cancel("touchpad-unavailable");
            _threeFingersDrag.Stop("touchpad-unavailable");
            _dragButtonCoordinator.ForceRelease("touchpad-unavailable");
        }

        TouchpadInitialized = true;
        _app.OnTouchpadInitialized();
    }

    // Called when a new set of contacts has been registered

    private TouchpadContact[] _oldContacts = Array.Empty<TouchpadContact>();
    private long _lastContactCtms = Ctms();

    public void OnTouchpadContact(IntPtr currentDevice, List<TouchpadContact> contacts){
        _doubleTapDragLock.OnTouchpadContact(currentDevice, contacts.ToArray());

        if(App.SettingsData.ThreeFingerDrag){
            _threeFingersDrag.OnTouchpadContact(currentDevice, _oldContacts, contacts.ToArray(), Ctms() - _lastContactCtms);
        }

        _app.OnTouchpadContact(currentDevice, contacts.ToArray()); // Transfer to App for displaying contacts in SettingsWindow
        _lastContactCtms = Ctms();
        _oldContacts = contacts.ToArray();
    }

    private static long Ctms(){
        return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
    }
}
