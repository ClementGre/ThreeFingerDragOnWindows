using System;
using System.Drawing;
using System.Windows;
using Forms = System.Windows.Forms;

namespace ThreeFingersDragOnWindows;

public partial class App : Application {
    protected override void OnStartup(StartupEventArgs e){
        
        MainWindow = new MainWindow();
        MainWindow.Show();
        
        base.OnStartup(e);
    }
    
}