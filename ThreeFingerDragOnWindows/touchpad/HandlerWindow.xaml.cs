using System;
using System.Diagnostics;
using System.Timers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using ThreeFingerDragEngine.utils;
using ThreeFingerDragOnWindows.threefingerdrag;
using ThreeFingerDragOnWindows.utils;

namespace ThreeFingerDragOnWindows.touchpad;

public sealed partial class HandlerWindow : Window {
    private readonly App _app;
    private readonly ContactsManager _contactsManager;
    private readonly ThreeFingerDrag _threeFingersDrag;

    public bool TouchpadInitialized; // Became true when the touchpad check is done, but does not confirm that the touchpad has been registered, see TouchpadRegistered
    public bool TouchpadExists;
    public bool TouchpadRegistered;

    public HandlerWindow(App app){
        Logger.Log("Starting HandlerWindow...");
        InitializeComponent();

        _app = app;
        _contactsManager = new ContactsManager(this);
        _threeFingersDrag = new ThreeFingerDrag();

        // Let the _handlerWindow to be defined in App.xaml.cs before initializing the source
        var queue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        Utils.runOnMainThreadAfter(100, () => {
            _contactsManager.InitializeSource();
            
            Timer timer = new Timer();
            timer.Interval = 1000; 
            timer.AutoReset = true;
            timer.Elapsed += (sender, args) => queue.TryEnqueue(() => {
                if(!App.SettingsData.RegularTouchpadCheck){
                    timer.Interval = 5000;
                    return;
                }
                if(App.SettingsData.RegularTouchpadCheckInterval > 0){
                    timer.Interval = App.SettingsData.RegularTouchpadCheckInterval * 1000;
                }
                if(TouchpadInitialized && (!TouchpadRegistered || App.SettingsData.RegularTouchpadCheckEvenAlreadyRegistered)){
                    _contactsManager.InitializeSource();
                }
            });
            timer.Start();
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


    // Touchpad
    // Called when the touchpad is detected and the events handlers are registered (or not)
    public void OnTouchpadInitialized(bool touchpadExists, bool touchpadRegistered){
        TouchpadExists = touchpadExists;
        TouchpadRegistered = touchpadRegistered;
        if(!touchpadExists) Logger.Log("Touchpad is not detected.");
        else if(!touchpadRegistered) Logger.Log("Touchpad is detected but not registered.");
        else Logger.Log("Touchpad is detected and registered.");
        
        TouchpadInitialized = true;
        _app.OnTouchpadInitialized();
    }

    // Called when a new set of contacts has been registered
    
    private TouchpadContact[] _oldContacts = Array.Empty<TouchpadContact>();
    private long _lastContactCtms = Ctms();
    
    public void OnTouchpadContact(TouchpadContact[] contacts){
        if(App.SettingsData.ThreeFingerDrag){
            _threeFingersDrag.OnTouchpadContact(_oldContacts, contacts, Ctms() - _lastContactCtms);
        }
        
        _app.OnTouchpadContact(contacts, _contactsManager.isSingleContactMode); // Transfer to App for displaying contacts in SettingsWindow
        _lastContactCtms = Ctms();
        _oldContacts = contacts;
    }
    
    private static long Ctms(){
        return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
    }
}