using System;

using System.Collections.Generic;

using System.Diagnostics;

using System.IO;

using System.Text.Json;

using System.Threading.Tasks;

using Windows.Storage;

using Microsoft.UI.Xaml;

using Microsoft.UI.Xaml.Controls;

using ThreeFingerDragOnWindows.utils;

using WinUICommunity;

namespace ThreeFingerDragOnWindows.settings;

public class SettingsData{
    private static int CURRENT_SETTINGS_VERSION = 5;

    // Other
    public static bool DidVersionChanged { get; set; } = false;
    public int SettingsVersion { get; set; } = 0;

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

    
    public class ThreeFingerDragConfig
    {
        public ThreeFingerDragConfig()
        {
            
        }
        public ThreeFingerDragConfig(bool cursorMoveProperty, float cursorSpeedProperty, float cursorAccelerationProperty)
        {
            ThreeFingerDragCursorMove = cursorMoveProperty;
            ThreeFingerDragCursorSpeed = cursorSpeedProperty;
            ThreeFingerDragCursorAcceleration = cursorAccelerationProperty;
        }

        public bool ThreeFingerDragCursorMove { get; set; } = true;
        public float ThreeFingerDragCursorSpeed { get; set; } = 30;
        public float ThreeFingerDragCursorAcceleration { get; set; } = 10;
    }

    public Dictionary<string, ThreeFingerDragConfig> ThreeFingerDeviceDragCursorConfigs { get; set; }
    
    public int ThreeFingerDragCursorAveraging { get; set; } = 1;
    public int ThreeFingerDragMaxFingerMoveDistance{ get; set; } = 0;

    public int ThreeFingerDragStartThreshold { get; set; } = 100;
    public int ThreeFingerDragStopThreshold { get; set; } = 10;
   
    // Other settings

    public enum StartupActionType{
        NONE,
        ENABLE_ELEVATED_RUN_WITH_STARTUP,
        DISABLE_ELEVATED_RUN_WITH_STARTUP,
        ENABLE_ELEVATED_STARTUP,
        DISABLE_ELEVATED_STARTUP,
    }

    public StartupActionType StartupAction { get; set; } = StartupActionType.NONE;

    public bool RunElevated { get; set; } = false;

    public bool RecordLogs { get; set; } = false;


    public static SettingsData load(){
        Logger.Log("Loading settings...");

        var filePath = getPath(true);
        SettingsData up;

        try{
            var jsonString =  File.ReadAllText(filePath);
            up = JsonSerializer.Deserialize<SettingsData>(jsonString);
            Logger.Log($"Settings loaded, version = {up.SettingsVersion}");
        } catch(Exception e){
            Console.WriteLine(e);
            up = new SettingsData();
            up.save();
        }

        if (up.ThreeFingerDeviceDragCursorConfigs == null)
        {
            up.ThreeFingerDeviceDragCursorConfigs = new Dictionary<string, ThreeFingerDragConfig>(2);
            up.save();
        }

        if(up.SettingsVersion < 1){
            Logger.Log("Updating settings to version 1");
            up.save();
        }
        if(up.SettingsVersion < 2){
            Logger.Log("Updating settings to version 2");
            if(up.RunElevated && StartupManager.IsElevatedStartupOn()){

                if(Utils.IsAppRunningAsAdministrator()){
                    StartupManager.DisableElevatedStartup();
                    StartupManager.EnableElevatedStartup();
                } else{
                    Utils.runOnMainThreadAfter(2000, () => {
                        if(App.SettingsWindow?.Content?.XamlRoot == null){
                            Logger.Log("SettingsWindow not ready, skipping v2.0.3 upgrade dialog");
                            return;
                        }

                        ContentDialog dialog = new ContentDialog{
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

        if(up.SettingsVersion != CURRENT_SETTINGS_VERSION){
            DidVersionChanged = true;
            up.save();
        }

        return up;
    }

    public void save(){

        SettingsVersion = CURRENT_SETTINGS_VERSION;

        var options = new JsonSerializerOptions { WriteIndented = true };

        var jsonString = JsonSerializer.Serialize(this, options);

        var filePath = getPath(false);

        File.WriteAllText(filePath, jsonString);

    }

    private static string getPath(bool createIfEmpty){

        var dirPath = ApplicationData.Current.LocalFolder.Path;

        var filePath = Path.Combine(dirPath, "preferences.json");

        

        Logger.Log("filepath: " + filePath);



        if(!Directory.Exists(dirPath) || !File.Exists(filePath)){

            Logger.Log("First run: creating settings file");

            Directory.CreateDirectory(dirPath);

            DidVersionChanged = true;

            if(createIfEmpty) new SettingsData().save();  // Wait for the async save to complete

        }



        return filePath;

    }
}
