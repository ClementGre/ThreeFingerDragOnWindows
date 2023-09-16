using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Windows;
using Microsoft.Win32.TaskScheduler;

namespace ThreeFingersDragEngine;

public partial class App : Application {
    
    public App(){
        Debug.WriteLine("Starting ThreeFingersDragEngine...");
        InitializeComponent();
        RegisterStartupTask();
    }

    public void RegisterStartupTask(){

        if(!IsAppRunningAsAdministrator()){
            Debug.WriteLine("The app is not running as administrator, can't register startup task.");
            return;
        }

        Debug.WriteLine("Registering Startup Task...");
        using TaskService taskService = new TaskService();
        TaskFolder folder = taskService.RootFolder.CreateFolder("ThreeFingersDragOnWindows", null, false);

        taskService.RootFolder.DeleteTask("ThreeFingersDragOnWindows on startup", false);
        if(folder.Tasks.Count != 0) return;

        // Create a new task definition
        TaskDefinition taskDefinition = taskService.NewTask();

        // Set task properties (description, etc.)
        taskDefinition.RegistrationInfo.Description = "Starting ThreeFingersDragOnWindows on system startup with elevated privileges.";
        taskDefinition.RegistrationInfo.Author = "Clément Grennerat";
        taskDefinition.Principal.RunLevel = TaskRunLevel.Highest;

        // Create a trigger (when to start the task)
        LogonTrigger logonTrigger = new LogonTrigger();
        taskDefinition.Triggers.Add(logonTrigger);

        // Set an action (what the task does)
        ExecAction execAction = new ExecAction(GetEnginePath(), "");
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

    public static bool IsAppRunningAsAdministrator(){
        var identity = new WindowsPrincipal(WindowsIdentity.GetCurrent());
        return identity.IsInRole(WindowsBuiltInRole.Administrator);
    }
    
    
    public static string GetEnginePath(){
        // It is necessary to use the path of the parent directory to use the .exe file instead of the .dll file.
        var dir = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;
        if (dir == null) throw new Exception("Could not get the directory of the current assembly.");
        return Path.Combine(dir.FullName, "ThreeFingersDragEngine.exe");
    }
    
}