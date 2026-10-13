using System;

namespace ThreeFingerDragOnWindows.touchpad;

public class TouchpadDeviceInfo
{
    public String deviceId { get; set; }
    
    public String deviceName { get; set; }  // Raw device path for connection type detection
    
    public String vendorId { get; set; }
    
    public String productId { get; set; }
    
    public IntPtr hid { get; set; }

    public override string ToString()
    {
        return deviceId + "(" + productId + ":" + vendorId + ")";
    }
}