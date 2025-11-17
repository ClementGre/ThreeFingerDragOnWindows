using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ThreeFingerDragOnWindows.settings;

namespace ThreeFingerDragOnWindows.touchpad;

public class MouseSpeedSettings : INotifyPropertyChanged
{
    private bool _cursorMoveProperty;
    private float _cursorSpeedProperty;
    private float _cursorAccelerationProperty;
    public TouchpadDeviceInfo TouchpadDevice { get; set; }
    public string Header { get; }
    
    public MouseSpeedSettings(SettingsData.ThreeFingerDragConfig dragConfig, TouchpadDeviceInfo device)
    {
        TouchpadDevice = device;
        Header = "Enable three finger mouse move (" + TouchpadDevice + ")";

        CursorMoveProperty = dragConfig.ThreeFingerDragCursorMove;
        CursorSpeedProperty = dragConfig.ThreeFingerDragCursorSpeed;
        CursorAccelerationProperty = dragConfig.ThreeFingerDragCursorAcceleration;
    }
    
    private void UpdateDragConfig()
    {
        App.SettingsData.ThreeFingerDeviceDragCursorConfigs[TouchpadDevice.deviceId] = new SettingsData.ThreeFingerDragConfig(_cursorMoveProperty, _cursorSpeedProperty, _cursorAccelerationProperty);
    }

    public bool CursorMoveProperty
    {
        get => _cursorMoveProperty;
        set
        {
            if (_cursorMoveProperty != value)
            {
                _cursorMoveProperty = value;
                
                OnPropertyChanged(nameof(CursorMoveProperty));
            }
            
        }
    }

    public float CursorSpeedProperty
    {
        get => _cursorSpeedProperty;
        set
        {
            if (_cursorSpeedProperty != value)
            {
                _cursorSpeedProperty = value;
                OnPropertyChanged(nameof(CursorSpeedProperty));
            }
        }
    }

    public float CursorAccelerationProperty
    {
        get => _cursorAccelerationProperty;
        set
        {
            if (_cursorAccelerationProperty != value)
            {
                _cursorAccelerationProperty = value; 
                OnPropertyChanged(nameof(CursorAccelerationProperty));
            }
            
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    
    private void OnPropertyChanged(string propertyName){
        UpdateDragConfig();
        
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}