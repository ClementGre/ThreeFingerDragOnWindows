using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ThreeFingerDragOnWindows.touchpad;

public class MouseSpeedSettings
{
    public TouchpadDeviceInfo TouchpadDevice { get; set; }
    public string Header { get;  } = "Mouse Speed";
    public string Description { get; }
    public double MinValue { get; } = 0;
    public double MaxValue { get; } = 200;
    public double StepFrequency { get; } = 1;

    private float _currentValue;

    public MouseSpeedSettings(TouchpadDeviceInfo device)
    {
        TouchpadDevice = device;
        Description = TouchpadDevice.ToString();
    }

    public float CurrentValue
    {
        get => App.SettingsData.ThreeFingerDeviceDragCursorSpeed.GetValueOrDefault(TouchpadDevice.deviceId, 30);
        set
        {
            if (!(Math.Abs(_currentValue - value) > 0.1)) return;
            _currentValue = value;
            App.SettingsData.ThreeFingerDeviceDragCursorSpeed[TouchpadDevice.deviceId] = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}