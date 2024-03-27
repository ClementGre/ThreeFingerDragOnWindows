using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Microsoft.Win32.TaskScheduler;

namespace ThreeFingerDragOnWindows.utils; 

public static class StartupManager {


    public static void EnableElevatedStartup(){
        _ = DisableUnelevatedStartup();
        
        Logger.Log("Enabling elevated startup task...");
        
        using TaskService taskService = new TaskService();
        TaskFolder folder = taskService.RootFolder.CreateFolder("ThreeFingerDragOnWindows", null, false);
        folder.AllTasks.ToList().ForEach(task => folder.DeleteTask(task.Name, false));

        TaskDefinition taskDefinition = taskService.NewTask();
        taskDefinition.RegistrationInfo.Description = "Starting ThreeFingerDragOnWindows on system startup with elevated privileges.";
        taskDefinition.RegistrationInfo.Author = "Clément Grennerat";
        taskDefinition.Principal.RunLevel = TaskRunLevel.Highest;
        taskDefinition.Settings.DisallowStartIfOnBatteries = false;
        taskDefinition.Settings.StopIfGoingOnBatteries = false;
        taskDefinition.Settings.IdleSettings.StopOnIdleEnd = false;
        taskDefinition.Settings.ExecutionTimeLimit = TimeSpan.Zero;
        taskDefinition.Settings.AllowHardTerminate = false;
        taskDefinition.Settings.Priority = ProcessPriorityClass.High;

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
        Logger.Log("Disabling elevated startup task...");
        
        using TaskService taskService = new TaskService();
        TaskFolder folder = taskService.RootFolder.CreateFolder("ThreeFingerDragOnWindows", null, false);
        folder.AllTasks.ToList().ForEach(task => folder.DeleteTask(task.Name, false));
        taskService.RootFolder.DeleteFolder("ThreeFingerDragOnWindows", false);
    }
    public static bool IsElevatedStartupOn(){
        using TaskService taskService = new TaskService();
        return taskService.RootFolder.SubFolders.Any(folder => folder.Name == "ThreeFingerDragOnWindows" && folder.Tasks.Count != 0);
    }
    
    
    public static async Task<bool> EnableUnelevatedStartup(){
        Logger.Log("Enabling unelevated startup task...");
        
        StartupTask startupTask = await StartupTask.GetAsync("ThreeFingerDragOnWindows");
        startupTask.Disable();
        
        
        switch (startupTask.State)
        {
            case StartupTaskState.Disabled:
                StartupTaskState newState = await startupTask.RequestEnableAsync();
                Logger.Log($"Request to enable startup, result = {newState}");
                break;
        }
        return startupTask.State is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
    }
    public static async Task<bool> DisableUnelevatedStartup(){
        Logger.Log("Disabling unelevated startup task...");
        StartupTask startupTask = await StartupTask.GetAsync("ThreeFingerDragOnWindows");
        startupTask.Disable();
        return startupTask.State is StartupTaskState.Disabled or StartupTaskState.DisabledByUser or StartupTaskState.DisabledByPolicy;
    }
    public static async Task<StartupTaskState> GetUnelevatedStartupStatus(){
        StartupTask startupTask = await StartupTask.GetAsync("ThreeFingerDragOnWindows");
        return startupTask.State;
    }
}