using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using Application = Microsoft.UI.Xaml.Application;

namespace ThreeFingersDragOnWindows;

public class Program {
    [STAThread]
    static async Task<int> Main(string[] args){
        WinRT.ComWrappersSupport.InitializeComWrappers();
        bool isRedirect = await DecideRedirection();
        if(!isRedirect){
            Microsoft.UI.Xaml.Application.Start((p) => {
                var context = new DispatcherQueueSynchronizationContext(
                    DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                new App();
            });
        }

        return 0;
    }

    private static async Task<bool> DecideRedirection(){
        bool isRedirect = false;
        AppActivationArguments args = AppInstance.GetCurrent().GetActivatedEventArgs();
        AppInstance keyInstance = AppInstance.FindOrRegisterForKey("randomKey");

        if(keyInstance.IsCurrent){
            keyInstance.Activated += OnActivated;
        } else{
            isRedirect = true;
            await keyInstance.RedirectActivationToAsync(args);
        }

        return isRedirect;
    }

    private static void OnActivated(object sender, AppActivationArguments args){
        App.DispatcherQueue.TryEnqueue(() => {
            (Application.Current as App)?.OpenSettingsWindow();
        });
    }
}