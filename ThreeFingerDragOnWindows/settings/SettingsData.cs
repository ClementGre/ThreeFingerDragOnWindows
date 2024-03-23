using System;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using Windows.Storage;

namespace ThreeFingerDragOnWindows.settings;

public class SettingsData {
    
    private static int CURRENT_SETTINGS_VERSION = 1;

    // Other
    public static bool IsFirstRun{ get; set; } = false;
    public int SettingsVersion{ get; set; } = 0;

    // Three fingers drag Settings
    public bool RegularTouchpadCheck{ get; set; } = true;
    public int RegularTouchpadCheckInterval{ get; set; } = 5;
    public bool RegularTouchpadCheckEvenAlreadyRegistered{ get; set; } = false;
    
    // Three fingers drag Settings
    public bool ThreeFingerDrag{ get; set; } = true;
    
    public bool ThreeFingerDragAllowReleaseAndRestart{ get; set; } = true;
    public int ThreeFingerDragReleaseDelay{ get; set; } = 500;

    public bool ThreeFingerDragCursorMove{ get; set; } = true;
    public float ThreeFingerDragCursorSpeed{ get; set; } = 30;
    public float ThreeFingerDragCursorAcceleration{ get; set; } = 10;
    
    
    // Other settings
    
    public enum StartupActionType {
        NONE,
        ENABLE_ELEVATED_RUN_WITH_STARTUP,
        DISABLE_ELEVATED_RUN_WITH_STARTUP,
        ENABLE_ELEVATED_STARTUP,
        DISABLE_ELEVATED_STARTUP,
    }

    public StartupActionType StartupAction{ get; set; } = StartupActionType.NONE;
    
    public bool RunElevated{ get; set; } = false;

    
    public static SettingsData load(){
        var mySerializer = new XmlSerializer(typeof(SettingsData));
        var myFileStream = new FileStream(getPath(true), FileMode.Open);
        SettingsData up;
        
        try{
            up = (SettingsData) mySerializer.Deserialize(myFileStream);
            myFileStream.Close();
        } catch(Exception e){
            Console.WriteLine(e);
            myFileStream.Close();
            up = new SettingsData();
            up.save();
        }

        if (up.SettingsVersion < 1)
        {
            Debug.WriteLine("Updating settings to version 1");
            up.ThreeFingerDragCursorAcceleration *= 10;
            up.save();
        }
        
        return up;
    }

    public void save(){
        SettingsVersion = CURRENT_SETTINGS_VERSION;
        var mySerializer = new XmlSerializer(typeof(SettingsData));
        var myWriter = new StreamWriter(getPath(false));
        mySerializer.Serialize(myWriter, this);
        myWriter.Close();
    }

    private static string getPath(bool createIfEmpty){
        var dirPath = ApplicationData.Current.LocalFolder.Path;
        var filePath = Path.Combine(dirPath, "preferences.xml");

        if(!Directory.Exists(dirPath) || !File.Exists(filePath)){
            Debug.WriteLine("First run: creating settings file");
            Directory.CreateDirectory(dirPath);
            IsFirstRun = true;
            if(createIfEmpty) new SettingsData().save();
        }

        return filePath;
    }
}