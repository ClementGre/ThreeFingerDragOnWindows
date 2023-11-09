using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Microsoft.Win32.TaskScheduler;

namespace ThreeFingersDragOnWindows.utils; 

public static class StartupManager {


    public static void EnableElevatedStartup(){
        DisableUnelevatedStartup();
        
        Debug.WriteLine("Enabling elevated startup task...");
        
        using TaskService taskService = new TaskService();
        TaskFolder folder = taskService.RootFolder.CreateFolder("ThreeFingersDragOnWindows", null, false);
        folder.AllTasks.ToList().ForEach(task => folder.DeleteTask(task.Name, false));

        TaskDefinition taskDefinition = taskService.NewTask();
        taskDefinition.RegistrationInfo.Description = "Starting ThreeFingersDragOnWindows on system startup with elevated privileges.";
        taskDefinition.RegistrationInfo.Author = "Clément Grennerat";
        taskDefinition.Principal.RunLevel = TaskRunLevel.Highest;

        LogonTrigger logonTrigger = new LogonTrigger();
        taskDefinition.Triggers.Add(logonTrigger);

        ExecAction execAction = new ExecAction(Utils.GetAppPath(), "");
        taskDefinition.Actions.Add(execAction);

        folder.RegisterTaskDefinition(
            "Run on Startup", // Task name
            taskDefinition,
            TaskCreation.CreateOrUpdate,
            null, // User credentials (null for current user)
            null, // User account password
            TaskLogonType.InteractiveToken);
    }
    public static void DisableElevatedStartup(){
        Debug.WriteLine("Disabling elevated startup task...");
        
        using TaskService taskService = new TaskService();
        TaskFolder folder = taskService.RootFolder.CreateFolder("ThreeFingersDragOnWindows", null, false);
        folder.AllTasks.ToList().ForEach(task => folder.DeleteTask(task.Name, false));
        taskService.RootFolder.DeleteFolder("ThreeFingersDragOnWindows", false);
    }
    public static bool IsElevatedStartupOn(){
        using TaskService taskService = new TaskService();
        return taskService.RootFolder.SubFolders.Any(folder => folder.Name == "ThreeFingersDragOnWindows" && folder.Tasks.Count != 0);
    }
    
    
    public static async Task<bool> EnableUnelevatedStartup(){
        Debug.WriteLine("Enabling unelevated startup task...");
        
        StartupTask startupTask = await StartupTask.GetAsync("ThreeFingersDragOnWindows");
        startupTask.Disable();
        
        
        switch (startupTask.State)
        {
            case StartupTaskState.Disabled:
                StartupTaskState newState = await startupTask.RequestEnableAsync();
                Debug.WriteLine("Request to enable startup, result = {0}", newState);
                break;
        }
        return startupTask.State is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
    }
    public static async Task<bool> DisableUnelevatedStartup(){
        Debug.WriteLine("Disabling unelevated startup task...");
        StartupTask startupTask = await StartupTask.GetAsync("ThreeFingersDragOnWindows");
        startupTask.Disable();
        return startupTask.State is StartupTaskState.Disabled or StartupTaskState.DisabledByUser or StartupTaskState.DisabledByPolicy;
    }
    public static async Task<StartupTaskState> GetUnelevatedStartupStatus(){
        StartupTask startupTask = await StartupTask.GetAsync("ThreeFingersDragOnWindows");
        return startupTask.State;
    }
}