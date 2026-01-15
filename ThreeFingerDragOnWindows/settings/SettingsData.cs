using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ThreeFingerDragOnWindows.settings.profiles;
using ThreeFingerDragOnWindows.utils;
using WinUICommunity;

namespace ThreeFingerDragOnWindows.settings;

public class SettingsData
{
    private const int CURRENT_SETTINGS_VERSION = 5;

    // Other
    public static bool DidVersionChanged { get; set; } = false;
    public int SettingsVersion { get; set; } = 0;

    // Profile management
    public class ProfileInfo
    {
        public string Name { get; set; } = "Default";
        public string FilePath { get; set; } = string.Empty;
    }

    public List<ProfileInfo> Profiles { get; set; } = new();
    public string ActiveProfilePath { get; set; } = string.Empty;

    [JsonIgnore]
    private ThreeFingerDragProfile _activeProfile;

    [JsonIgnore]
    public ThreeFingerDragProfile ActiveProfile
    {
        get
        {
            if (_activeProfile == null)
            {
                LoadActiveProfile();
            }
            return _activeProfile;
        }
    }

    private void LoadActiveProfile()
    {
        Profiles ??= new List<ProfileInfo>();

        bool needsSave = false;

        if (string.IsNullOrEmpty(ActiveProfilePath) || !File.Exists(ActiveProfilePath))
        {
            var defaultProfilePath = GetProfileFilePath("Default");
            _activeProfile = ThreeFingerDragProfile.Load(defaultProfilePath);
            _activeProfile.ProfileName = "Default";
            _activeProfile.Save(defaultProfilePath);

            ActiveProfilePath = defaultProfilePath;
            needsSave = true;
        }
        else
        {
            _activeProfile = ThreeFingerDragProfile.Load(ActiveProfilePath);
        }

        if (_activeProfile != null)
        {
            var profileEntry = Profiles.FirstOrDefault(p => p.FilePath == ActiveProfilePath);
            if (profileEntry == null)
            {
                Profiles.Add(new ProfileInfo { Name = _activeProfile.ProfileName, FilePath = ActiveProfilePath });
                needsSave = true;
            }
            else if (profileEntry.Name != _activeProfile.ProfileName)
            {
                profileEntry.Name = _activeProfile.ProfileName;
                needsSave = true;
            }
        }

        if (needsSave)
        {
            save();
        }
    }

    public void SaveActiveProfile()
    {
        if (_activeProfile != null && !string.IsNullOrEmpty(ActiveProfilePath))
        {
            _activeProfile.Save(ActiveProfilePath);
        }
    }

    /// <summary>
    /// Switches to a different profile
    /// </summary>
    public void SwitchToProfile(string profilePath)
    {
        // Save current profile before switching
        SaveActiveProfile();

        // Load new profile
        ActiveProfilePath = profilePath;
        _activeProfile = ThreeFingerDragProfile.Load(profilePath);

        if (!Profiles.Any(p => p.FilePath == profilePath))
        {
            Profiles.Add(new ProfileInfo { Name = _activeProfile.ProfileName, FilePath = profilePath });
        }

        save();
        Logger.Log($"Switched to profile: {_activeProfile.ProfileName}");
    }

    /// <summary>
    /// Renames the active profile and updates the filename to match
    /// </summary>
    public void RenameActiveProfile(string newName)
    {
        if (_activeProfile == null || string.IsNullOrEmpty(ActiveProfilePath))
        {
            return;
        }

        var oldPath = ActiveProfilePath;
        var newPath = GetProfileFilePath(newName);

        // Update the profile name
        _activeProfile.ProfileName = newName;

        // If the path would be different, rename the file
        if (oldPath != newPath)
        {
            // Save to new location
            _activeProfile.Save(newPath);

            if (File.Exists(oldPath))
            {
                try
                {
                    File.Delete(oldPath);
                    Logger.Log($"Renamed profile file from {oldPath} to {newPath}");
                }
                catch (Exception e)
                {
                    Logger.Log($"Error deleting old profile file: {e.Message}");
                }
            }

            ActiveProfilePath = newPath;

            var profileInfo = Profiles.FirstOrDefault(p => p.FilePath == oldPath);
            if (profileInfo != null)
            {
                profileInfo.Name = newName;
                profileInfo.FilePath = newPath;
            }

            save();
        }
        else
        {
            // Just save the profile with the new name
            _activeProfile.Save(ActiveProfilePath);
        }
    }

    public static string GetProfileFilePath(string profileName)
    {
        var sanitizedName = ThreeFingerDragProfile.SanitizeProfileName(profileName);
        return Path.Combine(GetProfilesDirectory(), $"{sanitizedName}.xml");
    }

    private static string GetProfilesDirectory()
    {
        var dirPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "profiles");
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }
        return dirPath;
    }

    public enum StartupActionType
    {
        NONE,
        ENABLE_ELEVATED_RUN_WITH_STARTUP,
        DISABLE_ELEVATED_RUN_WITH_STARTUP,
        ENABLE_ELEVATED_STARTUP,
        DISABLE_ELEVATED_STARTUP,
    }

    public StartupActionType StartupAction { get; set; } = StartupActionType.NONE;

    public bool RunElevated { get; set; } = false;

    public bool RecordLogs { get; set; } = false;

    public enum ProfileSwitchingDetectionMode
    {
        WindowsHook,
        IntervalBased
    }

    public ProfileSwitchingDetectionMode DetectionMode { get; set; } = ProfileSwitchingDetectionMode.WindowsHook;
    public int DetectionInterval { get; set; } = 2000;

    public static SettingsData load()
    {
        Logger.Log("Loading settings...");

        var filePath = getPath(true);
        SettingsData up;

        try
        {
            var jsonString = File.ReadAllText(filePath);
            up = JsonSerializer.Deserialize<SettingsData>(jsonString) ?? new SettingsData();
            Logger.Log($"Settings loaded, version = {up.SettingsVersion}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            up = new SettingsData();
            up.save();
        }

        up.Profiles ??= new List<ProfileInfo>();

        if (up.SettingsVersion < 1)
        {
            Logger.Log("Updating settings to version 1");
            up.save();
        }
        if (up.SettingsVersion < 2)
        {
            Logger.Log("Updating settings to version 2");
            if (up.RunElevated && StartupManager.IsElevatedStartupOn())
            {
                if (Utils.IsAppRunningAsAdministrator())
                {
                    StartupManager.DisableElevatedStartup();
                    StartupManager.EnableElevatedStartup();
                }
                else
                {
                    Utils.runOnMainThreadAfter(2000, () =>
                    {
                        if (App.SettingsWindow?.Content?.XamlRoot == null)
                        {
                            Logger.Log("SettingsWindow not ready, skipping v2.0.3 upgrade dialog");
                            return;
                        }

                        ContentDialog dialog = new ContentDialog
                        {
                            XamlRoot = App.SettingsWindow.Content.XamlRoot,
                            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                            Title = "Fixing startup task issue",
                            Content = "The v2.0.3 fixed a bug in the app startup task with elevated privileges. Please disable and re-enable the \"Run at startup\" option in the Other Settings tab to fix this bug.",
                            CloseButtonText = "Ok"
                        };
                        dialog.ShowAsyncDraggable();
                    });
                }
            }
        }

        if (up.SettingsVersion < 5)
        {
            Logger.Log("Updating settings to version 5 - migrating to profiles");
        }

        if (up.SettingsVersion != CURRENT_SETTINGS_VERSION)
        {
            DidVersionChanged = true;
            up.save();
        }

        up.LoadActiveProfile();

        return up;
    }

    public void save()
    {
        SettingsVersion = CURRENT_SETTINGS_VERSION;

        var options = new JsonSerializerOptions { WriteIndented = true };
        var jsonString = JsonSerializer.Serialize(this, options);

        var filePath = getPath(false);
        File.WriteAllText(filePath, jsonString);
    }

    private static string getPath(bool createIfEmpty)
    {
        var dirPath = ApplicationData.Current.LocalFolder.Path;
        var filePath = Path.Combine(dirPath, "preferences.json");



        Logger.Log("filepath: " + filePath);



        if (!Directory.Exists(dirPath) || !File.Exists(filePath))
        {

            Logger.Log("First run: creating settings file");

            Directory.CreateDirectory(dirPath);

            DidVersionChanged = true;

            if (createIfEmpty) new SettingsData().save();  // Wait for the async save to complete

        }



        return filePath;

    }
}
