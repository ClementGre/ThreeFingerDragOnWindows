using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using Application = Microsoft.UI.Xaml.Application;
using Utils = ThreeFingersDragOnWindows.utils.Utils;

namespace ThreeFingersDragOnWindows;

public class Program {
    [STAThread]
    static async Task<int> Main(string[] args){
        WinRT.ComWrappersSupport.InitializeComWrappers();

        (AppInstance existingInstance, bool existingInstanceIsAdmin) = FindExistingInstance();

        if(existingInstance != null){
            if(Utils.IsAppRunningAsAdministrator() && !existingInstanceIsAdmin && TerminateOldInstance(existingInstance.ProcessId)){
                Debug.WriteLine("Unelevated instance found and killed. Starting the app");
                StartApp();
            } else{
                Debug.WriteLine("Instance found, redirecting activation.");
                RedirectActivation(existingInstance);
            }
        } else{
            Debug.WriteLine("No instance found, starting the app.");
            StartApp();
        }
        return 0;
    }

    private static void RedirectActivation(AppInstance instance){
        AppActivationArguments args = AppInstance.GetCurrent().GetActivatedEventArgs();
        instance.RedirectActivationToAsync(args);
    }

    private static void StartApp(){
        AppInstance.FindOrRegisterForKey("ThreeFingersDragOnWindows-SingleInstance-" + (Utils.IsAppRunningAsAdministrator() ? "Admin" : "User"));
        AppInstance.GetCurrent().Activated += OnActivated;

        Application.Start((p) => {
            var context = new DispatcherQueueSynchronizationContext(
                DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }

    private static (AppInstance, bool) FindExistingInstance(){
        foreach(AppInstance appInstance in AppInstance.GetInstances()){
            if(appInstance.IsCurrent) continue;
            if(appInstance.Key.Equals("ThreeFingersDragOnWindows-SingleInstance-User")){
                return (appInstance, false);
            }

            if(appInstance.Key.Equals("ThreeFingersDragOnWindows-SingleInstance-Admin")){
                return (appInstance, true);
            }
        }

        return (null, false);
    }

    private static void OnActivated(object sender, AppActivationArguments args){
        (Application.Current as App)?.DispatcherQueue.TryEnqueue(() => { (Application.Current as App)?.OpenSettingsWindow(); });
    }

    private static bool TerminateOldInstance(uint processId){
        try{
            Process oldInstance = Process.GetProcessById((int) processId);
            oldInstance.Kill();
        } catch(Exception ex){
            Debug.WriteLine(ex);
            return false;
        }

        return true;
    }
}