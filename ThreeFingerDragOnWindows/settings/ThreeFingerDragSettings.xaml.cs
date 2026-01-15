using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Microsoft.UI.Xaml;
using ThreeFingerDragOnWindows.settings.profiles;
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
        get{ return App.SettingsData.ActiveProfile.ThreeFingerDrag; }
        set{ 
            App.SettingsData.ActiveProfile.ThreeFingerDrag = value;
            App.SettingsData.SaveActiveProfile();
        }
    }

    public int ButtonTypeProperty {
        get{ return (int) App.SettingsData.ActiveProfile.ThreeFingerDragButton; }
        set{ 
            App.SettingsData.ActiveProfile.ThreeFingerDragButton = (ThreeFingerDragProfile.ThreeFingerDragButtonType) value;
            App.SettingsData.SaveActiveProfile();
        }
    }

    public bool AllowReleaseAndRestartProperty {
        get{ return App.SettingsData.ActiveProfile.ThreeFingerDragAllowReleaseAndRestart; }
        set{ 
            App.SettingsData.ActiveProfile.ThreeFingerDragAllowReleaseAndRestart = value;
            App.SettingsData.SaveActiveProfile();
        }
    }

    public int ReleaseDelayProperty {
        get{ return App.SettingsData.ActiveProfile.ThreeFingerDragReleaseDelay; }
        set{ 
            App.SettingsData.ActiveProfile.ThreeFingerDragReleaseDelay = value;
            App.SettingsData.SaveActiveProfile();
        }
    }

    public bool CursorMoveProperty {
        get{ return App.SettingsData.ActiveProfile.ThreeFingerDragCursorMove; }
        set{ 
            App.SettingsData.ActiveProfile.ThreeFingerDragCursorMove = value;
            App.SettingsData.SaveActiveProfile();
        }
    }

    public float CursorSpeedProperty {
        get{ return App.SettingsData.ActiveProfile.ThreeFingerDragCursorSpeed; }
        set{
            if(App.SettingsData.ActiveProfile.ThreeFingerDragCursorSpeed != value){
                App.SettingsData.ActiveProfile.ThreeFingerDragCursorSpeed = value;
                App.SettingsData.SaveActiveProfile();
                OnPropertyChanged(nameof(CursorSpeedProperty));
            }
        }
    }

    public ObservableCollection<MouseSpeedSettings> MouseSpeedSettingItems
    {
        get
        {
            var settings = new ObservableCollection<MouseSpeedSettings>();
            var allDeviceInfos = TouchpadHelper.GetAllDeivceInfos();

            foreach (var device in allDeviceInfos)
            {
                var config = App.SettingsData.GetDeviceDragConfig(device.deviceId);
                settings.Add(new MouseSpeedSettings(config, device));
            }

            settings.CollectionChanged += OnCollectionChanged;
            return settings;
        }
    }

    public float CursorAccelerationProperty {
        get{ return App.SettingsData.ActiveProfile.ThreeFingerDragCursorAcceleration; }
        set{
            if(App.SettingsData.ActiveProfile.ThreeFingerDragCursorAcceleration != value){
                App.SettingsData.ActiveProfile.ThreeFingerDragCursorAcceleration = value;
                App.SettingsData.SaveActiveProfile();
                OnPropertyChanged(nameof(CursorAccelerationProperty));
            }
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(MouseSpeedSettingItems));
    }

    public int CursorAveragingProperty {
        get{ return App.SettingsData.ActiveProfile.ThreeFingerDragCursorAveraging; }
        set{
            if(App.SettingsData.ActiveProfile.ThreeFingerDragCursorAveraging != value){
                App.SettingsData.ActiveProfile.ThreeFingerDragCursorAveraging = value;
                App.SettingsData.SaveActiveProfile();
            }
        }
    }

    public int StartDragThresholdProperty {
        get{ return App.SettingsData.ActiveProfile.ThreeFingerDragStartThreshold; }
        set{
            if(App.SettingsData.ActiveProfile.ThreeFingerDragStartThreshold != value){
                App.SettingsData.ActiveProfile.ThreeFingerDragStartThreshold = value;
                OnPropertyChanged(nameof(StartDragThresholdProperty));

                if(value < App.SettingsData.ActiveProfile.ThreeFingerDragStopThreshold){
                    App.SettingsData.ActiveProfile.ThreeFingerDragStopThreshold = value;
                    OnPropertyChanged(nameof(StopDragThresholdProperty));
                }
                App.SettingsData.SaveActiveProfile();
            }
        }
    }
    public int StopDragThresholdProperty {
        get{ return App.SettingsData.ActiveProfile.ThreeFingerDragStopThreshold; }
        set{
            if(App.SettingsData.ActiveProfile.ThreeFingerDragStopThreshold != value){
                App.SettingsData.ActiveProfile.ThreeFingerDragStopThreshold = value;
                OnPropertyChanged(nameof(StopDragThresholdProperty));

                if(value > App.SettingsData.ActiveProfile.ThreeFingerDragStartThreshold){
                    App.SettingsData.ActiveProfile.ThreeFingerDragStartThreshold = value;
                    OnPropertyChanged(nameof(StartDragThresholdProperty));
                }
                App.SettingsData.SaveActiveProfile();
            }
        }
    }
    public int MaxFingerMoveDistanceProperty {
        get{ return App.SettingsData.ActiveProfile.ThreeFingerDragMaxFingerMoveDistance; }
        set{
            if(App.SettingsData.ActiveProfile.ThreeFingerDragMaxFingerMoveDistance != value){
                App.SettingsData.ActiveProfile.ThreeFingerDragMaxFingerMoveDistance = value;
                App.SettingsData.SaveActiveProfile();
                OnPropertyChanged(nameof(MaxFingerMoveDistanceProperty));
            }
        }
    }
}
