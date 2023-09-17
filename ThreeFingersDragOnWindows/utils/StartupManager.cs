using System.Diagnostics;
using Microsoft.Win32.TaskScheduler;

namespace ThreeFingersDragOnWindows.utils; 

public static class StartupManager {


    public static void RegisterElevatedStartup(){
        Debug.WriteLine("Registering elevated startup task...");
        
        using TaskService taskService = new TaskService();
        TaskFolder folder = taskService.RootFolder.CreateFolder("ThreeFingersDragOnWindows", null, false);

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
        ExecAction execAction = new ExecAction(Utils.GetElevatorPath(), "");
        taskDefinition.Actions.Add(execAction);

        // Register the task
        folder.RegisterTaskDefinition(
            "Run on Startup", // Task name
            taskDefinition,
            TaskCreation.CreateOrUpdate,
            null, // User credentials (null for current user)
            null, // User account password
            TaskLogonType.InteractiveToken);

    }
    public static void UnregisterElevatedStartup(){
        using TaskService taskService = new TaskService();
        TaskFolder folder = taskService.RootFolder.CreateFolder("ThreeFingersDragOnWindows", null, false);
        folder.DeleteTask("Run on Startup", false);
        taskService.RootFolder.DeleteFolder("ThreeFingersDragOnWindows", false);
    }
}