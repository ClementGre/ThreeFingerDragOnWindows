using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace ThreeFingersDragOnWindows.utils;

public sealed partial class TrayWindow : Window {
    private readonly App _app;
    
    public TrayWindow(App app){
        Debug.WriteLine("Starting HandlerWindow...");
        InitializeComponent();
        _app = app;
    }

    // TaskbarIcon Actions
    private void OpenSettingsWindow(object sender, ExecuteRequestedEventArgs e){
        Debug.WriteLine("Opening SettingsWindow from HandlerWindow TaskbarIcon");
        _app.OpenSettingsWindow();
    }

    private void QuitApp(object sender, ExecuteRequestedEventArgs e){
        Debug.WriteLine("Quitting App from HandlerWindow TaskbarIcon");
        _app.Quit();
    }
}