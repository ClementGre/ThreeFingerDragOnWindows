using System;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using ThreeFingersDragOnWindows.src.utils;

namespace ThreeFingersDragOnWindows.src;

public sealed partial class PrefsWindow {
    ////////// Touchpad registration and data //////////

    public static readonly DependencyProperty TouchpadContactsProperty =
        DependencyProperty.Register("TouchpadContacts", typeof(string), typeof(PrefsWindow),
            new PropertyMetadata(null));


    private readonly App _app;

    public PrefsWindow(App app){
        _app = app;
        Console.WriteLine("Starting PrefsWindow...");
        InitializeComponent();
    }

    public string TouchpadContacts{
        get => (string) GetValue(TouchpadContactsProperty);
        private set => SetValue(TouchpadContactsProperty, value);
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
        var regex = new Regex("[^0-9]+");
        e.Handled = regex.IsMatch(e.Text);
    }

    private long _lastContact = 0;
    private int _inputCount = 0;
    private long _lastEventSpeed = 0;
    public void OnTouchpadContact(TouchpadContact[] contacts){
        _inputCount++;

        if(_inputCount >= 20){
            _inputCount = 0;
            _lastEventSpeed = (Ctms() - _lastContact) / 20;
            _lastContact = Ctms();
        }
        TouchpadContacts = string.Join(" | ", contacts.Select(c => c.ToString())) + " | Event speed: " + _lastEventSpeed + "ms";
    }
    
    private long Ctms(){
        return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
    }
}