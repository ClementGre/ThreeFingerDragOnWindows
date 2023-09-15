using System;
using System.Diagnostics;
using System.Security.Principal;
using Microsoft.UI.Xaml;
using ThreeFingersDragOnWindows.settings;
using ThreeFingersDragOnWindows.touchpad;
using ThreeFingersDragOnWindows.utils;
using Microsoft.Win32.TaskScheduler;

namespace ThreeFingersDragOnWindows;

public partial class App : Application {
    public static SettingsData SettingsData;
    private SettingsWindow _settingsWindow;


    public HandlerWindow HandlerWindow;


    public App(){
        Debug.WriteLine("Starting ThreeFingersDragOnWindows...");
        InitializeComponent();

        SettingsData = SettingsData.load();

        if(SettingsData.IsFirstRun){
            OpenSettingsWindow();
            Utils.runOnMainThreadAfter(3000, () => HandlerWindow = new HandlerWindow(this));
        } else{
            HandlerWindow = new HandlerWindow(this);
        }

        RegisterStartupTask();
    }


    public void OpenSettingsWindow(){
        _settingsWindow ??= new SettingsWindow(this);
        _settingsWindow.Activate();
    }

    public void OnClosePrefsWindow(){
        _settingsWindow = null;
    }

    public void Quit(){
        HandlerWindow?.Close();
        _settingsWindow?.Close();
    }

    public void OnTouchpadContact(TouchpadContact[] contacts){
        _settingsWindow?.OnTouchpadContact(contacts);
    }

    public bool DoTouchpadExist(){
        return HandlerWindow.TouchpadExists;
    }

    public bool DoTouchpadRegistered(){
        return HandlerWindow.TouchpadRegistered;
    }

    public void RegisterStartupTask()
    {

        if(!IsAppRunningAsAdministrator()){
            Debug.WriteLine("The app is not running as administrator.");
            return;
        }
        
        Debug.WriteLine("Registering Startup Task...");
        using TaskService taskService = new TaskService();
        TaskFolder folder = taskService.RootFolder.CreateFolder("ThreeFingersDragOnWindows", null, false);

        taskService.RootFolder.DeleteTask("ThreeFingersDragOnWindows on startup", false);
        if (folder.Tasks.Count != 0) return;

        // Create a new task definition
        TaskDefinition taskDefinition = taskService.NewTask();

        // Set task properties (description, etc.)
        taskDefinition.RegistrationInfo.Description = "Starting ThreeFingersDragOnWindows on system startup with elevated privileges.";
        taskDefinition.RegistrationInfo.Author = "Clément Grennerat";

        // Create a trigger (when to start the task)
        LogonTrigger logonTrigger = new LogonTrigger();
        taskDefinition.Triggers.Add(logonTrigger);

        // Set an action (what the task does)
        ExecAction execAction = new ExecAction("explorer.exe", "threefingersdragonwindows://", null);
        taskDefinition.Actions.Add(execAction);

        // Register the task
        folder.RegisterTaskDefinition(
            "ThreeFingersDragOnWindows on startup", // Task name
            taskDefinition,
            TaskCreation.CreateOrUpdate,
            null, // User credentials (null for current user)
            null, // User account password
            TaskLogonType.InteractiveToken);

        Debug.WriteLine("Task created successfully.");
    }
    
    public static bool IsAppRunningAsAdministrator()
    {
        var identity = new WindowsPrincipal(WindowsIdentity.GetCurrent());
        return identity.IsInRole(WindowsBuiltInRole.Administrator);
    }
}