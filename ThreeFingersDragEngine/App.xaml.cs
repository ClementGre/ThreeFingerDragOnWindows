using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Windows.System;
using ThreeFingersDragEngine.rawinputs;
using ThreeFingersDragEngine.touchpad;
using ThreeFingersDragEngine.utils;

namespace ThreeFingersDragEngine;

public partial class App {

    public static SettingsData SettingsData;
    private HandlerWindow _handlerWindow;
   

    // Kill: taskkill /IM ThreeFingersDragEngine.exe /F
    protected override void OnStartup(StartupEventArgs e){
        Debug.WriteLine("Starting ThreeFingersDragEngine...");
            
        if(IsInstanceAlreadyRunning()){
            Debug.WriteLine("Already an instance is running, shutting down...");
            Current.Shutdown();
        }

        if(!e.Args.Contains("FromWinUIApp")){
            Debug.WriteLine("Not started from WinUI app -> starting the WinUI app.");
            Utils.StartWinUIApp();
        }

        SettingsData = new SettingsData();
        _handlerWindow = new HandlerWindow(this);

        base.OnStartup(e);
        SetupISSForDispose();
    }

    private bool IsInstanceAlreadyRunning(){
        Process proc = Process.GetCurrentProcess();
        int count = Process.GetProcesses().Count(p => p.ProcessName == proc.ProcessName);
        return count > 1;
    }

    private void SetupISSForDispose(){
        new Thread(() => {
            Debug.WriteLine("Setuping Dispose server pipe...");
            
            
            
            NamedPipeServerStream server = new NamedPipeServerStream("ThreeFingersDragOnWindows-DisposeEngine",
                PipeDirection.In, 
                1,
                PipeTransmissionMode.Message,
                PipeOptions.None);
            
            if(Utils.IsAppRunningAsAdministrator()){
                var ps = new PipeSecurity();
                ps.AddAccessRule(new PipeAccessRule(Environment.UserName, PipeAccessRights.ReadWrite, AccessControlType.Allow));
                server.SetAccessControl(ps);
            }
        
            server.WaitForConnection();
            
            StreamReader reader = new StreamReader(server);
            string? message;
            while((message = reader.ReadLine()) != null){
                Debug.WriteLine($"Received message from WinUI app: {message}");
                if(message == "Dispose"){
                    Debug.WriteLine("Received Dispose message from WinUI app, shutting down...");
                    Current.Dispatcher.Invoke(() => {
                        Current.Shutdown();
                    });
                    return;
                }
            }

        }).Start();
    }

}