using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using ThreeFingersDragOnWindows.utils;

namespace ThreeFingersDragOnWindows.settings;

public sealed partial class OtherSettings {

    public OtherSettings(){
        InitializeComponent();
        
        ElevatedStatus.Title = Utils.IsAppRunningAsAdministrator() ? "The app is currently elevated (running with administrator privileges)." : "The app is currently not elevated (not running with administrator privileges).";
        if(Utils.IsAppRunningAsAdministrator()) ElevatedStatus.Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success;
    }
    
    private void RunAtStartup_Toggled(object sender, RoutedEventArgs e){
        // TODO
    }
    
    private void RunElevated_Toggled(object sender, RoutedEventArgs e){
        if(!App.SettingsData.RunElevated && RunElevated.IsOn && !Utils.IsAppRunningAsAdministrator()){
            App.SettingsData.RunElevated = true; // The binding does not have the time to update the settings if the app is restarted.
            App.RestartElevated();
        }
    }
    
    public bool RunElevatedProperty
    {
        get { return App.SettingsData.RunElevated; }
        set { App.SettingsData.RunElevated = value; }
    }

}