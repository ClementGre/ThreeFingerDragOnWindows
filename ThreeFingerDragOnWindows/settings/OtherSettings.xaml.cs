using System.Diagnostics;
using System.Timers;
using Windows.ApplicationModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using ThreeFingerDragOnWindows.utils;

namespace ThreeFingerDragOnWindows.settings;

public sealed partial class OtherSettings {

    private readonly Timer _timer = new(1000);
    
    public OtherSettings(){
        InitializeComponent();
        
        ElevatedStatus.Title = Utils.IsAppRunningAsAdministrator() ? "The app is currently elevated (running with administrator privileges)." : "The app is currently not elevated (not running with administrator privileges).";
        if(Utils.IsAppRunningAsAdministrator()) ElevatedStatus.Severity = InfoBarSeverity.Success;
        
        UpdateStartupStatus();
        
        // Checking if the user changed the startup settings in the task manager
        
        _timer.Elapsed += (_, _) => {
            if(!App.SettingsData.RunElevated){
                // Debug.WriteLine("Auto update startup status");
                DispatcherQueue.TryEnqueue(UpdateStartupStatus);
            }
        };
        _timer.AutoReset = true;
        _timer.Start();
    }
    
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _timer.Stop();
    }


    private bool _runAtStartupLastValue;
    private bool DoRunAtStartup {
        get => RunAtStartup.IsOn;
        set{
            Debug.WriteLine("Set DoRunAtStartup : " + value + " (last value : " + _runAtStartupLastValue + ")");
            _runAtStartupLastValue = value;
            if(RunAtStartup.IsOn != value) RunAtStartup.IsOn = value;
        }
    }

    private void RunAtStartup_Toggled(object sender, RoutedEventArgs e){
        // Returning if the event is called due to a programmatic change
        if(DoRunAtStartup == _runAtStartupLastValue) return;
        Debug.WriteLine("RunAtStartup_Toggled : " + DoRunAtStartup);
        
        if(App.SettingsData.RunElevated){
            if(DoRunAtStartup){
                if(!Utils.IsAppRunningAsAdministrator()){
                    DoRunAtStartup = false;
                    App.RestartElevated(SettingsData.StartupActionType.ENABLE_ELEVATED_STARTUP);
                    return;
                }
                StartupManager.EnableElevatedStartup();
            } else{
                if(!Utils.IsAppRunningAsAdministrator()){
                    DoRunAtStartup = true;
                    App.RestartElevated(SettingsData.StartupActionType.DISABLE_ELEVATED_STARTUP);
                    return;
                }
                StartupManager.DisableElevatedStartup();
            }
            UpdateStartupStatus();
        }else{
            if(DoRunAtStartup) StartupManager.EnableUnelevatedStartup().ContinueWith(_ => DispatcherQueue.TryEnqueue(UpdateStartupStatus));
            else StartupManager.DisableUnelevatedStartup().ContinueWith(_ => DispatcherQueue.TryEnqueue(UpdateStartupStatus));
        }
    }

    private void UpdateStartupStatus(){
        bool doTurnOnStartupToggle = true;
        bool doDisableStartupToggle = false;
        if(App.SettingsData.RunElevated){ // Elevated startup task
            ElevatedStartupInformation.Visibility = Visibility.Visible;
            if(StartupManager.IsElevatedStartupOn()){
                Debug.WriteLine("UpdateStartupStatus : Elevated startup is on");
                if(Utils.IsAppRunningAsAdministrator()){
                    StartupStatus.Title = "The app is currently set to run at startup with UAC skip configured.";
                } else{
                    StartupStatus.Title = "The app is currently set set to run at startup with UAC skip configured. Disabling this requires the app to restart with administrator privileges.";
                }
                StartupStatus.Severity = InfoBarSeverity.Success;
            } else{
                Debug.WriteLine("UpdateStartupStatus : Elevated startup is off");
                if(Utils.IsAppRunningAsAdministrator()){
                    StartupStatus.Title = "The app is currently not set to run at startup. You can enable it here with UAC skip configured.";
                    StartupStatus.Severity = InfoBarSeverity.Informational;
                } else{
                    StartupStatus.Title =
                        "The app is currently not set to run at startup. You can enable it here with UAC skip configured, but this requires the app to restart with administrator privileges.";
                    StartupStatus.Severity = InfoBarSeverity.Warning;
                }
                doTurnOnStartupToggle = false;
            }
            DoRunAtStartup = doTurnOnStartupToggle;
            if(RunAtStartup.IsEnabled != true) RunAtStartup.IsEnabled = true;
            return;
        }

        // Unelevated startup task
        ElevatedStartupInformation.Visibility = Visibility.Collapsed;
        StartupManager.GetUnelevatedStartupStatus().ContinueWith(task => {
            DispatcherQueue.TryEnqueue(() => {
                switch (task.Result)
                {
                    case StartupTaskState.Enabled:
                        StartupStatus.Title = "The app is currently set to run at startup.";
                        StartupStatus.Severity = InfoBarSeverity.Success;
                        break;
                    case StartupTaskState.Disabled:
                        StartupStatus.Title = "The app is currently not set to run at startup.";
                        StartupStatus.Severity = InfoBarSeverity.Informational;
                        doTurnOnStartupToggle = false;
                        break;
                    case StartupTaskState.DisabledByUser:
                        StartupStatus.Title = "The app can't run at startup because the startup task was disabled in the tasks manager. You can re-enable it there and then reload this page.";
                        StartupStatus.Severity = InfoBarSeverity.Warning;
                        doTurnOnStartupToggle = false;
                        doDisableStartupToggle = true;
                        break;
                    case StartupTaskState.EnabledByPolicy:
                        StartupStatus.Title = "The app startup task is enabled by a group policy. You can still setup an elevated startup task enabling the app to run as administrator.";
                        StartupStatus.Severity = InfoBarSeverity.Warning;
                        doTurnOnStartupToggle = false;
                        break;
                    case StartupTaskState.DisabledByPolicy:
                        StartupStatus.Title = "The app startup task is disabled by a group policy. You can still setup an elevated startup task enabling the app to run as administrator.";
                        StartupStatus.Severity = InfoBarSeverity.Error;
                        doTurnOnStartupToggle = false;
                        doDisableStartupToggle = true;
                        break;
                }
                DoRunAtStartup = doTurnOnStartupToggle;
                if(RunAtStartup.IsEnabled != !doDisableStartupToggle) RunAtStartup.IsEnabled = !doDisableStartupToggle;
            });
        });
    }
    
    
    
    private void RunElevated_Toggled(object sender, RoutedEventArgs e){
        // Returning if the event is called due to a programmatic change
        if(App.SettingsData.RunElevated == RunElevated.IsOn) return;
        Debug.WriteLine("RunElevated_Toggled : " + RunElevated.IsOn);

        // Updating the settings sooner because the function UpdateStartupStatus can be called, or the app can restart.
        App.SettingsData.RunElevated = RunElevated.IsOn;
        
        if(!Utils.IsAppRunningAsAdministrator()){
            Debug.WriteLine("RunElevated_Toggled : Restarting elevated");
            if(RunElevated.IsOn){
                if(DoRunAtStartup){
                    Debug.WriteLine("RunElevated_Toggled : Restarting elevated to enable startup");
                    setRunElevatedProperty(false); // The settings will be changed after the restart, with the settings StartupAction
                    
                    if(!App.RestartElevated(SettingsData.StartupActionType.ENABLE_ELEVATED_RUN_WITH_STARTUP)){
                        StartupManager.DisableUnelevatedStartup().ContinueWith(_ => DispatcherQueue.TryEnqueue(UpdateStartupStatus));
                        RunElevated.IsOn = true;
                    }
                } else{
                    App.SettingsData.save();
                    App.RestartElevated();
                }
            }else{
                if(DoRunAtStartup){
                    Debug.WriteLine("RunElevated_Toggled : Restarting elevated to disable startup");
                    setRunElevatedProperty(true); // The settings will be changed after the restart, with the settings StartupAction
                    RunElevated.IsOn = true;
                    App.RestartElevated(SettingsData.StartupActionType.DISABLE_ELEVATED_RUN_WITH_STARTUP);
                }
            }
        }else if(DoRunAtStartup){
            if(RunElevated.IsOn){
                StartupManager.DisableUnelevatedStartup().ContinueWith(_ => DispatcherQueue.TryEnqueue(UpdateStartupStatus));
                StartupManager.EnableElevatedStartup();
            } else{
                StartupManager.DisableElevatedStartup();
                StartupManager.EnableUnelevatedStartup().ContinueWith(_ => DispatcherQueue.TryEnqueue(UpdateStartupStatus));
            }
        }
        UpdateStartupStatus();
        
        // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
        // dialog.XamlRoot = this.XamlRoot;
        // dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
        // dialog.Title = "Save your work?";
        // dialog.PrimaryButtonText = "Save";
        // dialog.SecondaryButtonText = "Don't Save";
        // dialog.CloseButtonText = "Cancel";
        // dialog.DefaultButton = ContentDialogButton.Primary;
        // dialog.Content = new ContentDialogContent();
    }
    
    public bool RunElevatedProperty
    {
        get => App.SettingsData.RunElevated;
        set => App.SettingsData.RunElevated = value;
    }
    private void setRunElevatedProperty(bool value){
        App.SettingsData.RunElevated = value;
        RunElevatedProperty = value;
    }

}