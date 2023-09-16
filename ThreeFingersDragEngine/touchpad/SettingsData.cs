using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using Windows.Storage;

namespace ThreeFingersDragEngine.touchpad;

public class SettingsData {
    public bool AllowReleaseAndRestart{ get; set; } = true;
    public int ReleaseDelay{ get; set; } = 500;

    public bool ThreeFingersMove{ get; set; } = true;
    public float MouseSpeed{ get; set; } = 30;
    public float MouseAcceleration{ get; set; } = 1;

}