using System;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using Windows.Storage;

namespace ThreeFingersDragOnWindows.settings;

public class SettingsData {
    
    // Three fingers drag Settings
    public bool RegularTouchpadCheck{ get; set; } = true;
    public int RegularTouchpadCheckInterval{ get; set; } = 5;
    public bool RegularTouchpadCheckEvenAlreadyRegistered{ get; set; } = false;
    
    // Three fingers drag Settings
    public bool ThreeFingersDrag{ get; set; } = true;
    
    public bool ThreeFingersDragAllowReleaseAndRestart{ get; set; } = true;
    public int ThreeFingersDragReleaseDelay{ get; set; } = 500;

    public bool ThreeFingersDragCursorMove{ get; set; } = true;
    public float ThreeFingersDragCursorSpeed{ get; set; } = 25;
    public float ThreeFingersDragCursorAcceleration{ get; set; } = 1;
    
    
    // Other settings
    
    public enum StartupActioType {
        NONE,
        ENABLE_ELEVATED_RUN_WITH_STARTUP,
        DISABLE_ELEVATED_RUN_WITH_STARTUP,
        ENABLE_ELEVATED_STARTUP,
        DISABLE_ELEVATED_STARTUP,
    }

    public StartupActioType StartupAction{ get; set; } = StartupActioType.NONE;
    
    public bool RunElevated{ get; set; } = false;

    // Other
    public static bool IsFirstRun{ get; set; } = false;

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
        return up;
    }

    public void save(){
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