using System;
using System.IO;
using System.Xml.Serialization;

namespace ThreeFingersDragOnWindows.src.utils;

public class UserPreferences {
    public bool AllowReleaseAndRestart{ get; set; } = true;
    public int ReleaseDelay{ get; set; } = 500;

    public bool ThreeFingersMove{ get; set; } = true;
    public float MouseSpeed{ get; set; } = 30;
    public float MouseAcceleration{ get; set; } = 1;
    
    public static bool IsFirstRun{ get; set; }

    public static UserPreferences load(){
        var mySerializer = new XmlSerializer(typeof(UserPreferences));
        var myFileStream = new FileStream(getPath(true), FileMode.Open);
        var up = (UserPreferences) mySerializer.Deserialize(myFileStream);
        myFileStream.Close();
        return up;
    }

    public static void save(UserPreferences up){
        var mySerializer = new XmlSerializer(typeof(UserPreferences));
        var myWriter = new StreamWriter(getPath(false));
        mySerializer.Serialize(myWriter, up);
        myWriter.Close();
    }

    private static string getPath(bool testCreate){
        var dirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ThreeFingersDragOnWindows");
        var filePath = Path.Combine(dirPath, "preferences.xml");

        if(!Directory.Exists(dirPath) || !File.Exists(filePath)){
            Directory.CreateDirectory(dirPath);
            IsFirstRun = true;
            if(testCreate) save(new UserPreferences());
        }

        return filePath;
    }
}