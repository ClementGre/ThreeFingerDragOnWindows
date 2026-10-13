using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using ThreeFingerDragOnWindows.utils;

namespace ThreeFingerDragOnWindows.settings.profiles;

public class ThreeFingerDragProfile{
// Profile metadata
public string ProfileName { get; set; } = "Default";
    
// Smart Profile Switching - per device
public class DeviceSmartSwitchingConfig
{
    public string DeviceId { get; set; } = "";
    public bool SmartSwitchingEnabled { get; set; } = false;
    public List<string> AssociatedPrograms { get; set; } = new List<string>();
}

public List<DeviceSmartSwitchingConfig> DeviceSmartSwitchingConfigs { get; set; } = new List<DeviceSmartSwitchingConfig>();

/// <summary>
/// Get or create smart switching config for a specific device
/// </summary>
public DeviceSmartSwitchingConfig GetDeviceSmartSwitchingConfig(string deviceId)
{
    var config = DeviceSmartSwitchingConfigs.FirstOrDefault(c => c.DeviceId == deviceId);
    if (config == null)
    {
        config = new DeviceSmartSwitchingConfig { DeviceId = deviceId };
        DeviceSmartSwitchingConfigs.Add(config);
    }
    return config;
}
    
// Three finger drag Settings
public bool ThreeFingerDrag { get; set; } = true;

public enum ThreeFingerDragButtonType {
    NONE,
    LEFT,
    RIGHT,
    MIDDLE,
}
public ThreeFingerDragButtonType ThreeFingerDragButton { get; set; } = ThreeFingerDragButtonType.LEFT;

    public bool ThreeFingerDragAllowReleaseAndRestart { get; set; } = true;
    public int ThreeFingerDragReleaseDelay { get; set; } = 500;

    public bool ThreeFingerDragCursorMove { get; set; } = true;
    public float ThreeFingerDragCursorSpeed { get; set; } = 30;
    public float ThreeFingerDragCursorAcceleration { get; set; } = 10;
    public int ThreeFingerDragCursorAveraging { get; set; } = 1;
    public int ThreeFingerDragMaxFingerMoveDistance{ get; set; } = 0;

    public int ThreeFingerDragStartThreshold { get; set; } = 100;
    public int ThreeFingerDragStopThreshold { get; set; } = 10;

    public static ThreeFingerDragProfile Load(string filePath){
        Logger.Log($"Loading profile from {filePath}...");

        if(!File.Exists(filePath)){
            Logger.Log("Profile file not found, creating default profile");
            var defaultProfile = new ThreeFingerDragProfile();
            defaultProfile.Save(filePath);
            return defaultProfile;
        }

        var mySerializer = new XmlSerializer(typeof(ThreeFingerDragProfile));
        ThreeFingerDragProfile profile;

        try{
            using(var myFileStream = new FileStream(filePath, FileMode.Open)){
                profile = (ThreeFingerDragProfile)mySerializer.Deserialize(myFileStream);
            }
            Logger.Log($"Profile loaded: {profile.ProfileName}");
        } catch(Exception e){
            Logger.Log($"Error loading profile: {e.Message}");
            Console.WriteLine(e);
            profile = new ThreeFingerDragProfile();
            profile.Save(filePath);
        }

        return profile;
    }

    public void Save(string filePath){
        Logger.Log($"Saving profile to {filePath}...");
        
        var directory = Path.GetDirectoryName(filePath);
        if(!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)){
            Directory.CreateDirectory(directory);
        }

        var mySerializer = new XmlSerializer(typeof(ThreeFingerDragProfile));
        using(var myWriter = new StreamWriter(filePath)){
            mySerializer.Serialize(myWriter, this);
        }
        
        
        Logger.Log("Profile saved");
    }

    /// <summary>
    /// Sanitizes a profile name to make it safe for use as a filename
    /// </summary>
    public static string SanitizeProfileName(string profileName){
        if(string.IsNullOrWhiteSpace(profileName)){
            return "Default";
        }

        // Remove invalid filename characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", profileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        
        // Trim whitespace and limit length
        sanitized = sanitized.Trim();
        if(sanitized.Length > 100){
            sanitized = sanitized.Substring(0, 100);
        }
        
        // Ensure we have a valid name
        if(string.IsNullOrWhiteSpace(sanitized)){
            return "Default";
        }
        
        return sanitized;
    }
}

