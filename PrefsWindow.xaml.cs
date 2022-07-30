using System;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace ThreeFingersDragOnWindows;

public sealed partial class PrefsWindow {
    
    
    private readonly App _app;
    public PrefsWindow(App app){
        _app = app;
        Console.WriteLine("Starting PrefsWindow...");
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e){
        base.OnSourceInitialized(e);
        
        TouchpadExists.Text = _app.DoTouchpadExist() ? "Yes" : "No";
        TouchpadRegistered.Text = _app.DoTouchpadRegistered() ? "Yes" : "No";

        AllowReleaseAndRestart.IsChecked = App.Prefs.AllowReleaseAndRestart;
        AllowReleaseAndRestart.Checked += (_, _) => App.Prefs.AllowReleaseAndRestart = true;
        AllowReleaseAndRestart.Unchecked += (_, _) => App.Prefs.AllowReleaseAndRestart = false;
        ReleaseDelay.Text = App.Prefs.ReleaseDelay.ToString();
        ReleaseDelay.TextChanged += (_, _) => {
            if(!int.TryParse(ReleaseDelay.Text, out var delay)){
                ReleaseDelay.Text = App.Prefs.ReleaseDelay.ToString();
                return;
            }
            App.Prefs.ReleaseDelay = delay;
        };
        
        ThreeFingersMove.IsChecked = App.Prefs.ThreeFingersMove;
        ThreeFingersMove.Checked += (_, _) => App.Prefs.ThreeFingersMove = true;
        ThreeFingersMove.Unchecked += (_, _) => App.Prefs.ThreeFingersMove = false;
        MouseSpeed.Value = App.Prefs.MouseSpeed;
        MouseSpeed.ValueChanged += (_, _) => App.Prefs.MouseSpeed = (float) MouseSpeed.Value;
        MouseAcceleration.Value = App.Prefs.MouseAcceleration;
        MouseAcceleration.ValueChanged += (_, _) => App.Prefs.MouseAcceleration = (float) MouseAcceleration.Value;
    }

    ////////// Close & quit //////////

    private void CloseButton_Click(object sender, RoutedEventArgs e){
        Close();
    }
    private void QuitButton_Click(object sender, RoutedEventArgs e){
        _app.Quit();
    }

    protected override void OnClosing(CancelEventArgs e){
        Console.WriteLine("Hiding PrefsWindow, saving data...");
        UserPreferences.save(App.Prefs);
        _app.OnClosePrefsWindow();
        base.OnClosing(e);
    }
    
    ////////// UI Tools //////////
    
    private void NumberValidationTextBox(object sender, TextCompositionEventArgs e){
        Regex regex = new Regex("[^0-9]+");
        e.Handled = regex.IsMatch(e.Text);
    }

    ////////// Touchpad registration and data //////////
    
    public static readonly DependencyProperty TouchpadContactsProperty =
        DependencyProperty.Register("TouchpadContacts", typeof(string), typeof(PrefsWindow), new PropertyMetadata(null));
    public string TouchpadContacts{ get => (string) GetValue(TouchpadContactsProperty); private set => SetValue(TouchpadContactsProperty, value); }

    public void OnTouchpadContact(TouchpadContact[] contacts){
        TouchpadContacts = String.Join(" | ", contacts.Select(c => c.ToString()));
    }
}