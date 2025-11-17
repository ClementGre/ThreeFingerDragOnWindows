using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using ThreeFingerDragOnWindows.touchpad;
using ThreeFingerDragOnWindows.utils;

namespace ThreeFingerDragOnWindows.settings;

public sealed partial class ThreeFingerDragSettings : INotifyPropertyChanged{
    public ThreeFingerDragSettings(){
        InitializeComponent();
    }

    private void OpenSettings(object sender, RoutedEventArgs e){
        _ = Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:devices-touchpad"));
    }


    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged(string propertyName){
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


    public bool EnabledProperty {
        get{ return App.SettingsData.ThreeFingerDrag; }
        set{ App.SettingsData.ThreeFingerDrag = value; }
    }

    public int ButtonTypeProperty {
        get{ return (int) App.SettingsData.ThreeFingerDragButton; }
        set{ App.SettingsData.ThreeFingerDragButton = (SettingsData.ThreeFingerDragButtonType) value; }
    }

    public bool AllowReleaseAndRestartProperty {
        get{ return App.SettingsData.ThreeFingerDragAllowReleaseAndRestart; }
        set{ App.SettingsData.ThreeFingerDragAllowReleaseAndRestart = value; }
    }

    public int ReleaseDelayProperty {
        get{ return App.SettingsData.ThreeFingerDragReleaseDelay; }
        set{ App.SettingsData.ThreeFingerDragReleaseDelay = value; }
    }

    public bool CursorMoveProperty {
        get{ return App.SettingsData.ThreeFingerDragCursorMove; }
        set{ App.SettingsData.ThreeFingerDragCursorMove = value; }
    }

    public ObservableCollection<MouseSpeedSettings> MouseSpeedSettingItems
    {
        get
        {
            ObservableCollection<MouseSpeedSettings> settings = new ObservableCollection<MouseSpeedSettings>(new Collection<MouseSpeedSettings>());
            List<TouchpadDeviceInfo> allDeviceInfos = TouchpadHelper.GetAllDeivceInfos();
            foreach (TouchpadDeviceInfo device in allDeviceInfos)
            {
                settings.Add(new MouseSpeedSettings(device));
            }
            return settings;
        }
    }

    public float CursorAccelerationProperty {
        get{ return App.SettingsData.ThreeFingerDragCursorAcceleration; }
        set{
            if(App.SettingsData.ThreeFingerDragCursorAcceleration != value){
                App.SettingsData.ThreeFingerDragCursorAcceleration = value;
                OnPropertyChanged(nameof(CursorAccelerationProperty));
            }
        }
    }

    public int CursorAveragingProperty {
        get{ return App.SettingsData.ThreeFingerDragCursorAveraging; }
        set{
            if(App.SettingsData.ThreeFingerDragCursorAveraging != value){
                App.SettingsData.ThreeFingerDragCursorAveraging = value;
            }
        }
    }

    public int StartDragThresholdProperty {
        get{ return App.SettingsData.ThreeFingerDragStartThreshold; }
        set{
            if(App.SettingsData.ThreeFingerDragStartThreshold != value){
                App.SettingsData.ThreeFingerDragStartThreshold = value;
                OnPropertyChanged(nameof(StartDragThresholdProperty));

                if(value < App.SettingsData.ThreeFingerDragStopThreshold){
                    App.SettingsData.ThreeFingerDragStopThreshold = value;
                    OnPropertyChanged(nameof(StopDragThresholdProperty));
                }
            }
        }
    }
    public int StopDragThresholdProperty {
        get{ return App.SettingsData.ThreeFingerDragStopThreshold; }
        set{
            if(App.SettingsData.ThreeFingerDragStopThreshold != value){
                App.SettingsData.ThreeFingerDragStopThreshold = value;
                OnPropertyChanged(nameof(StopDragThresholdProperty));

                if(value > App.SettingsData.ThreeFingerDragStartThreshold){
                    App.SettingsData.ThreeFingerDragStartThreshold = value;
                    OnPropertyChanged(nameof(StartDragThresholdProperty));
                }
            }
        }
    }
    public int MaxFingerMoveDistanceProperty {
        get{ return App.SettingsData.ThreeFingerDragMaxFingerMoveDistance; }
        set{
            if(App.SettingsData.ThreeFingerDragMaxFingerMoveDistance != value){
                App.SettingsData.ThreeFingerDragMaxFingerMoveDistance = value;
                OnPropertyChanged(nameof(MaxFingerMoveDistanceProperty));
            }
        }
    }
}
