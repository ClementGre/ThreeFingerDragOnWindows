using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using ThreeFingersDragOnWindows.settings;
using ThreeFingersDragOnWindows.utils;

namespace ThreeFingersDragOnWindows;

public partial class App : Application {

    public static DispatcherQueue DispatcherQueue;

    public static SettingsData SettingsData;
    private SettingsWindow _settingsWindow;

    public TrayWindow TrayWindow;

    public App(){
        Debug.WriteLine("Starting ThreeFingersDragOnWindows...");
        InitializeComponent();
        DispatcherQueue = DispatcherQueue.GetForCurrentThread();
        SettingsData = SettingsData.load();

        if(SettingsData.IsFirstRun){
            OpenSettingsWindow();
            Utils.runOnMainThreadAfter(3000, () => TrayWindow = new TrayWindow(this));
        } else{
            TrayWindow = new TrayWindow(this);
        }

        if(!Environment.GetCommandLineArgs().Contains("FromWPFEngine")){
            Debug.WriteLine("Not started from WPF Engine -> starting the WPF Engine.");
            StartWpfEngine(SettingsData.RunElevated);
        }
    }

    private void SendPipeMessageForDispose(){
        new Thread(() => {
            Debug.WriteLine("Sending Dispose message to WPF Engine...");
            
            NamedPipeClientStream client = new NamedPipeClientStream(".", "ThreeFingersDragOnWindows-DisposeEngine", PipeDirection.Out);
            
            client.Connect();
            
            Debug.WriteLine("Connected, sending message...");

            StreamWriter writer = new StreamWriter(client);
            writer.AutoFlush = true;

            writer.WriteLine("Dispose");
            writer.Flush();
            client.Close();

        }).Start();
    }


    public void OpenSettingsWindow(){
        Debug.WriteLine("Opening SettingsWindow...");
        _settingsWindow ??= new SettingsWindow(this);
        _settingsWindow.Activate();
        Utils.FocusWindow(_settingsWindow);

    }

    public void OnClosePrefsWindow(){
        _settingsWindow = null;
    }

    public void Quit(){
        SendPipeMessageForDispose();
        TrayWindow?.Close();
        _settingsWindow?.Close();
    }

    public static void StartWpfEngine(bool elevated){
        var path = GetEnginePath();
        Debug.WriteLine("Runnig the WPF Engine app at " + path);
        ProcessStartInfo processInfo = new ProcessStartInfo{
            UseShellExecute = true,
            FileName = path,
            Arguments = "FromWinUIApp"
        };
        if(elevated){
            processInfo.Verb = "runas";
        }

        try{
            Process.Start(processInfo);
        } catch(Win32Exception ex){
            // Probably the user canceled the UAC window, 
            Debug.WriteLine(ex);
            if(elevated){
                Debug.WriteLine("The user canceled the UAC window, running the engine app in non elevated mode.");
                StartWpfEngine(false);
            }
        }
    }


    public static string GetEnginePath(){
        var dir = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;
        if(dir == null) throw new Exception("Could not get the directory of the current assembly.");
        return Path.Combine(dir.FullName, "ThreeFingersDragEngine.exe");
    }
}