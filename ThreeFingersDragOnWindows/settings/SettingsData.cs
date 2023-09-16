using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using Windows.Storage;

namespace ThreeFingersDragOnWindows.settings;

public class SettingsData {
    public bool AllowReleaseAndRestart{ get; set; } = true;
    public int ReleaseDelay{ get; set; } = 500;

    public bool ThreeFingersMove{ get; set; } = true;
    public float MouseSpeed{ get; set; } = 30;
    public float MouseAcceleration{ get; set; } = 1;
    
    public bool RunElevated{ get; set; } = true;

    public static bool IsFirstRun{ get; set; } = true;

    public static SettingsData load(){
        var mySerializer = new XmlSerializer(typeof(SettingsData));
        var myFileStream = new FileStream(getPath(true), FileMode.Open);
        var up = (SettingsData) mySerializer.Deserialize(myFileStream);
        myFileStream.Close();
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