using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;

namespace ThreeFingerDragOnWindows.settings;

public sealed partial class ThreeFingerDragSettings {

    public ThreeFingerDragSettings(){
        InitializeComponent();
    }

    private void OpenSettings(object sender, RoutedEventArgs e){
        _ = Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:devices-touchpad"));
    }
    
    public bool EnabledProperty
    {
        get { return App.SettingsData.ThreeFingerDrag; }
        set { App.SettingsData.ThreeFingerDrag = value; }
    }
    
    public bool AllowReleaseAndRestartProperty
    {
        get { return App.SettingsData.ThreeFingerDragAllowReleaseAndRestart; }
        set { App.SettingsData.ThreeFingerDragAllowReleaseAndRestart = value; }
    }
    
    public int ReleaseDelayProperty
    {
        get { return App.SettingsData.ThreeFingerDragReleaseDelay; }
        set { App.SettingsData.ThreeFingerDragReleaseDelay = value; }
    }
    
    public bool CursorMoveProperty
    {
        get { return App.SettingsData.ThreeFingerDragCursorMove; }
        set { App.SettingsData.ThreeFingerDragCursorMove = value; }
    }
    
    public float CursorSpeedProperty
    {
        get { return App.SettingsData.ThreeFingerDragCursorSpeed; }
        set { App.SettingsData.ThreeFingerDragCursorSpeed = value; }
    }
    
    public float CursorAccelerationProperty
    {
        get { return App.SettingsData.ThreeFingerDragCursorAcceleration; }
        set { App.SettingsData.ThreeFingerDragCursorAcceleration = value; }
    }
}