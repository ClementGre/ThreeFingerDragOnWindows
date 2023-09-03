using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using ThreeFingersDragOnWindows.src.Utils;

namespace ThreeFingersDragOnWindows.src.settings;


public sealed partial class SettingsWindow : Window
{

    private readonly App _app;

    public SettingsWindow(App app)
    {
        _app = app;
        Debug.WriteLine("Starting SettingssWindow...");


        this.InitializeComponent();
        //ExtendsContentIntoTitleBar = true; // enable custom titlebar
        //SetTitleBar(TitleBar); // set TitleBar element as titlebar

        /* TouchpadExists.Text = _app.DoTouchpadExist() ? "Yes" : "No";
        TouchpadRegistered.Text = _app.DoTouchpadRegistered() ? "Yes" : "No";

        AllowReleaseAndRestart.IsChecked = App.Prefs.AllowReleaseAndRestart;
        AllowReleaseAndRestart.Checked += (_, _) => App.Prefs.AllowReleaseAndRestart = true;
        AllowReleaseAndRestart.Unchecked += (_, _) => App.Prefs.AllowReleaseAndRestart = false;
        ReleaseDelay.Text = App.Prefs.ReleaseDelay.ToString();
        ReleaseDelay.TextChanged += (_, _) => {
            if (!int.TryParse(ReleaseDelay.Text, out var delay))
            {
                ReleaseDelay.Text = App.Prefs.ReleaseDelay.ToString();
                return;
            }

            App.Prefs.ReleaseDelay = delay;
        };

        ThreeFingersMove.IsChecked = App.Prefs.ThreeFingersMove;
        ThreeFingersMove.Checked += (_, _) => App.Prefs.ThreeFingersMove = true;
        ThreeFingersMove.Unchecked += (_, _) => App.Prefs.ThreeFingersMove = false;
        MouseSpeed.Value = App.Prefs.MouseSpeed;
        MouseSpeed.ValueChanged += (_, _) => App.Prefs.MouseSpeed = (float)MouseSpeed.Value;
        MouseAcceleration.Value = App.Prefs.MouseAcceleration;
        MouseAcceleration.ValueChanged += (_, _) => App.Prefs.MouseAcceleration = (float)MouseAcceleration.Value; */
    }


    private void NavigationView_SelectionChanged(object sender, NavigationViewSelectionChangedEventArgs e)
    {
        Debug.WriteLine("Selection Changed");
    }


    ////////// Close & quit //////////

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void QuitButton_Click(object sender, RoutedEventArgs e)
    {
        _app.Quit();
    }

    private void Window_Closed(object sender, WindowEventArgs e)
    {
        Debug.WriteLine("Hiding PrefsWindow, saving data...");
        //App.SettingsData.save();
        //_app.OnClosePrefsWindow();
    }


    ////////// UI Tools //////////

    /*private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
        var regex = new Regex("[^0-9]+");
        e.Handled = regex.IsMatch(e.Text);
    }*/

    private long _lastContact = 0;
    private int _inputCount = 0;
    private long _lastEventSpeed = 0;
    public void OnTouchpadContact(TouchpadContact[] contacts)
    {
        _inputCount++;

        if (_inputCount >= 20)
        {
            _inputCount = 0;
            _lastEventSpeed = (Ctms() - _lastContact) / 20;
            _lastContact = Ctms();
        }
        //TouchpadContacts = string.Join(" | ", contacts.Select(c => c.ToString())) + " | Event speed: " + _lastEventSpeed + "ms";
    }

    private long Ctms()
    {
        return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
    }

}
