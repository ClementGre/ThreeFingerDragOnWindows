using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;

namespace ThreeFingersDragOnWindows.settings;

public sealed partial class ThreeFingersDragSettings {

    public ThreeFingersDragSettings(){
        InitializeComponent();
    }

    private void OpenSettings(object sender, RoutedEventArgs e){
        _ = Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:devices-touchpad"));
    }
    
    public bool EnabledProperty
    {
        get { return App.SettingsData.ThreeFingersDrag; }
        set { App.SettingsData.ThreeFingersDrag = value; }
    }
    
    public bool AllowReleaseAndRestartProperty
    {
        get { return App.SettingsData.ThreeFingersDragAllowReleaseAndRestart; }
        set { App.SettingsData.ThreeFingersDragAllowReleaseAndRestart = value; }
    }
    
    public int ReleaseDelayProperty
    {
        get { return App.SettingsData.ThreeFingersDragReleaseDelay; }
        set { App.SettingsData.ThreeFingersDragReleaseDelay = value; }
    }
    
    public bool CursorMoveProperty
    {
        get { return App.SettingsData.ThreeFingersDragCursorMove; }
        set { App.SettingsData.ThreeFingersDragCursorMove = value; }
    }
    
    public float CursorSpeedProperty
    {
        get { return App.SettingsData.ThreeFingersDragCursorSpeed; }
        set { App.SettingsData.ThreeFingersDragCursorSpeed = value; }
    }
    
    public float CursorAccelerationProperty
    {
        get { return App.SettingsData.ThreeFingersDragCursorAcceleration; }
        set { App.SettingsData.ThreeFingersDragCursorAcceleration = value; }
    }
}