using System;
using System.IO;
using System.Xml.Serialization;

namespace ThreeFingersDragOnWindows; 

public class UserPreferences {

    public float MouseSpeed{ get; set; } = 1;

    public float MouseAcceleration{ get; set; } = 1;

    public static UserPreferences load(){
        XmlSerializer mySerializer = new XmlSerializer(typeof(UserPreferences));
        FileStream myFileStream = new FileStream(getPath(true), FileMode.Open);
        
        return (UserPreferences) mySerializer.Deserialize(myFileStream);
    }
    
    public static void save(UserPreferences up){
        XmlSerializer mySerializer = new XmlSerializer(typeof(UserPreferences));
        StreamWriter myWriter = new StreamWriter(getPath(false));
        mySerializer.Serialize(myWriter, up);
        myWriter.Close();
    }
    
    private static string getPath(bool testCreate){
        var dirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "ThreeFingersDragOnWindows");
        var filePath = Path.Combine(dirPath, "userpreferences.xml");
        
        if(!Directory.Exists(dirPath) || !File.Exists(filePath)){
            Directory.CreateDirectory(dirPath);
            if(testCreate) save(new UserPreferences());
        }
            
        return filePath;
    }
}