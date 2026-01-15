using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Graphics;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ThreeFingerDragOnWindows.settings.profiles;
using ThreeFingerDragOnWindows.utils;
using Windows.ApplicationModel;
using Windows.Foundation.Metadata;
using System.Reflection;
using ThreeFingerDragEngine.utils;
using WinUICommunity;

namespace ThreeFingerDragOnWindows.settings;

public sealed partial class SettingsWindow {
    private readonly App _app;

    /*private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
        var regex = new Regex("[^0-9]+");
        e.Handled = regex.IsMatch(e.Text);
    }*/

    public SettingsWindow(App app, bool openOtherSettings){
        _app = app;
        Logger.Log("Starting SettingsWindow...");


        InitializeComponent();
        AppWindow.Resize(new SizeInt32(1700, 1000));

        ExtendsContentIntoTitleBar = true; // enable custom titlebar
        SetTitleBar(TitleBar); // set TitleBar element as titlebar
        
        // Set non-client regions after controls are fully loaded
        ProfileComboBox.Loaded += (s, e) => SetupNonClientRegions();

        LoadProfiles();
        NavigationView.SelectedItem = openOtherSettings ? OtherSettings : Profiles;
    }

    private void SetupNonClientRegions(){
        try{
            // Ensure controls are loaded
            if(TitleBar?.XamlRoot == null || ProfileComboBox?.ActualWidth == 0){
                // Retry after a short delay if controls aren't ready
                Utils.runOnMainThreadAfter(200, SetupNonClientRegions);
                return;
            }
            
            // Get the scale factor for the display
            var scaleFactor = TitleBar.XamlRoot.RasterizationScale;
            
            // Create non-client regions for interactive controls
            var nonClientRegions = new List<Windows.Graphics.RectInt32>();
            
            // Add ComboBox region
            var comboTransform = ProfileComboBox.TransformToVisual(null);
            var comboPoint = comboTransform.TransformPoint(new Windows.Foundation.Point(0, 0));
            nonClientRegions.Add(new Windows.Graphics.RectInt32(
                (int)(comboPoint.X * scaleFactor),
                (int)(comboPoint.Y * scaleFactor),
                (int)(ProfileComboBox.ActualWidth * scaleFactor),
                (int)(ProfileComboBox.ActualHeight * scaleFactor)
            ));
            
            // Add button regions
            foreach(var button in new[] { CreateProfileButton, RenameProfileButton, DuplicateProfileButton }){
                if(button?.ActualWidth > 0){
                    var transform = button.TransformToVisual(null);
                    var point = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
                    nonClientRegions.Add(new Windows.Graphics.RectInt32(
                        (int)(point.X * scaleFactor),
                        (int)(point.Y * scaleFactor),
                        (int)(button.ActualWidth * scaleFactor),
                        (int)(button.ActualHeight * scaleFactor)
                    ));
                }
            }
            
            // Apply non-client regions
            if(AppWindow != null && AppWindow.TitleBar != null && nonClientRegions.Count > 0){
                var nonClientInputSrc = InputNonClientPointerSource.GetForWindowId(AppWindow.Id);
                nonClientInputSrc.SetRegionRects(NonClientRegionKind.Passthrough, nonClientRegions.ToArray());
                Logger.Log($"Non-client regions set successfully - {nonClientRegions.Count} regions");
            }
        } catch(Exception ex){
            Logger.Log($"Error setting non-client regions: {ex.Message}");
        }
    }


    private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs e){
        if(e.SelectedItem.Equals(Profiles)){
            sender.Header = "Profiles";
            ContentFrame.Navigate(typeof(ProfilesSettings));

        } else if(e.SelectedItem.Equals(ThreeFingerDrag)){
            sender.Header = "Three Finger Drag";
            ContentFrame.Navigate(typeof(ThreeFingerDragSettings));

        } else if(e.SelectedItem.Equals(Touchpad)){
            sender.Header = "Touchpad";
            ContentFrame.Navigate(typeof(TouchpadSettings));

        } else if(e.SelectedItem.Equals(OtherSettings)){
            sender.Header = "Other Settings";
            ContentFrame.Navigate(typeof(OtherSettings));
        }
    }


    ////////// Profile Management //////////

    private bool _isLoadingProfiles = false;

    public void LoadProfiles(){
        _isLoadingProfiles = true;
        
        ProfileComboBox.Items.Clear();
        
        // Load all profiles from the profiles directory
        var profilesDir = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "profiles");
        if(Directory.Exists(profilesDir)){
            var profileFiles = Directory.GetFiles(profilesDir, "*.xml");
            foreach(var file in profileFiles){
                try{
                    var profile = ThreeFingerDragProfile.Load(file);
                    ProfileComboBox.Items.Add(profile.ProfileName);
                } catch(Exception e){
                    Logger.Log($"Error loading profile {file}: {e.Message}");
                }
            }
        }
        
        // Select the active profile
        try{
            var activeProfileName = App.SettingsData.ActiveProfile.ProfileName;
            ProfileComboBox.SelectedItem = activeProfileName;
        } catch(Exception e){
            Logger.Log($"Error selecting active profile: {e.Message}");
            // If there are any profiles loaded, select the first one
            if(ProfileComboBox.Items.Count > 0){
                ProfileComboBox.SelectedIndex = 0;
            }
        }
        
        _isLoadingProfiles = false;
    }

    private void ProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e){
        if(_isLoadingProfiles) return;
        
        var selectedProfileName = ProfileComboBox.SelectedItem as string;
        if(string.IsNullOrEmpty(selectedProfileName)) return;
        
        SwitchProfile(selectedProfileName);
    }

    private void SwitchProfile(string profileName){
        var profilePath = SettingsData.GetProfileFilePath(profileName);
        
        if(!File.Exists(profilePath)){
            Logger.Log($"Profile file not found: {profilePath}");
            return;
        }
        
        // Switch to the new profile
        App.SettingsData.SwitchToProfile(profilePath);
        
        // Refresh the current page to show new profile settings
        RefreshCurrentPage();
    }

    private async void CreateProfileButton_Click(object sender, RoutedEventArgs e){
        var dialog = new ContentDialog{
            XamlRoot = Content.XamlRoot,
            Title = "Create New Profile",
            PrimaryButtonText = "Create",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary
        };
        
        var textBox = new TextBox{
            PlaceholderText = "Enter profile name",
            Text = "New Profile"
        };
        dialog.Content = textBox;
        
        var result = await dialog.ShowAsyncDraggable();
        
        if(result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(textBox.Text)){
            var newProfileName = textBox.Text.Trim();
            var newProfilePath = SettingsData.GetProfileFilePath(newProfileName);
            
            if(File.Exists(newProfilePath)){
                await ShowErrorDialog("A profile with this name already exists.");
                return;
            }
            
            // Create new profile with default settings
            var newProfile = new ThreeFingerDragProfile{
                ProfileName = newProfileName
            };
            newProfile.Save(newProfilePath);
            
            // Add to profiles list
            App.SettingsData.Profiles.Add(new SettingsData.ProfileInfo{
                Name = newProfileName,
                FilePath = newProfilePath
            });
            App.SettingsData.save();
            
            LoadProfiles();
            ProfileComboBox.SelectedItem = newProfileName;
            
            Logger.Log($"Created new profile: {newProfileName}");
        }
    }

    private async void RenameProfileButton_Click(object sender, RoutedEventArgs e){
        var currentProfileName = App.SettingsData.ActiveProfile.ProfileName;
        
        var dialog = new ContentDialog{
            XamlRoot = Content.XamlRoot,
            Title = "Rename Profile",
            PrimaryButtonText = "Rename",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary
        };
        
        var textBox = new TextBox{
            PlaceholderText = "Enter new profile name",
            Text = currentProfileName
        };
        dialog.Content = textBox;
        
        var result = await dialog.ShowAsyncDraggable();
        
        if(result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(textBox.Text)){
            var newProfileName = textBox.Text.Trim();
            
            if(newProfileName == currentProfileName){
                return; // No change
            }
            
            var newProfilePath = SettingsData.GetProfileFilePath(newProfileName);
            if(File.Exists(newProfilePath)){
                await ShowErrorDialog("A profile with this name already exists.");
                return;
            }
            
            App.SettingsData.RenameActiveProfile(newProfileName);
            LoadProfiles();
            
            Logger.Log($"Renamed profile from {currentProfileName} to {newProfileName}");
        }
    }

    private async void DuplicateProfileButton_Click(object sender, RoutedEventArgs e){
        var currentProfile = App.SettingsData.ActiveProfile;
        var currentProfileName = currentProfile.ProfileName;
        
        var dialog = new ContentDialog{
            XamlRoot = Content.XamlRoot,
            Title = "Duplicate Profile",
            PrimaryButtonText = "Duplicate",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary
        };
        
        var textBox = new TextBox{
            PlaceholderText = "Enter name for duplicated profile",
            Text = currentProfileName + " Copy"
        };
        dialog.Content = textBox;
        
        var result = await dialog.ShowAsyncDraggable();
        
        if(result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(textBox.Text)){
            var newProfileName = textBox.Text.Trim();
            var newProfilePath = SettingsData.GetProfileFilePath(newProfileName);
            
            if(File.Exists(newProfilePath)){
                await ShowErrorDialog("A profile with this name already exists.");
                return;
            }
            
            // Create a copy of the current profile
            var duplicatedProfile = new ThreeFingerDragProfile{
                ProfileName = newProfileName,
                ThreeFingerDrag = currentProfile.ThreeFingerDrag,
                ThreeFingerDragButton = currentProfile.ThreeFingerDragButton,
                ThreeFingerDragAllowReleaseAndRestart = currentProfile.ThreeFingerDragAllowReleaseAndRestart,
                ThreeFingerDragReleaseDelay = currentProfile.ThreeFingerDragReleaseDelay,
                ThreeFingerDragCursorMove = currentProfile.ThreeFingerDragCursorMove,
                ThreeFingerDragCursorSpeed = currentProfile.ThreeFingerDragCursorSpeed,
                ThreeFingerDragCursorAcceleration = currentProfile.ThreeFingerDragCursorAcceleration,
                ThreeFingerDragCursorAveraging = currentProfile.ThreeFingerDragCursorAveraging,
                ThreeFingerDragMaxFingerMoveDistance = currentProfile.ThreeFingerDragMaxFingerMoveDistance,
                ThreeFingerDragStartThreshold = currentProfile.ThreeFingerDragStartThreshold,
                ThreeFingerDragStopThreshold = currentProfile.ThreeFingerDragStopThreshold
            };
            duplicatedProfile.Save(newProfilePath);
            
            // Add to profiles list
            App.SettingsData.Profiles.Add(new SettingsData.ProfileInfo{
                Name = newProfileName,
                FilePath = newProfilePath
            });
            App.SettingsData.save();
            
            LoadProfiles();
            ProfileComboBox.SelectedItem = newProfileName;
            
            Logger.Log($"Duplicated profile {currentProfileName} to {newProfileName}");
        }
    }

    private async System.Threading.Tasks.Task ShowErrorDialog(string message){
        var errorDialog = new ContentDialog{
            XamlRoot = Content.XamlRoot,
            Title = "Error",
            Content = message,
            CloseButtonText = "OK"
        };
        await errorDialog.ShowAsyncDraggable();
    }

    private void RefreshCurrentPage(){
        // Re-navigate to current page to refresh bindings
        if(NavigationView.SelectedItem.Equals(Profiles)){
            ContentFrame.Navigate(typeof(ProfilesSettings));
        } else if(NavigationView.SelectedItem.Equals(ThreeFingerDrag)){
            ContentFrame.Navigate(typeof(ThreeFingerDragSettings));
        } else if(NavigationView.SelectedItem.Equals(Touchpad)){
            ContentFrame.Navigate(typeof(TouchpadSettings));
        } else if(NavigationView.SelectedItem.Equals(OtherSettings)){
            ContentFrame.Navigate(typeof(OtherSettings));
        }
    }

    ////////// Close & quit //////////

    private void CloseButton_Click(object sender, RoutedEventArgs e){
        Close();
    }

    private void QuitButton_Click(object sender, RoutedEventArgs e){
        Logger.Log("Quitting SettingsWindow...");
        _app.Quit();
    }

    private void Window_Closed(object sender, WindowEventArgs e){
        Logger.Log("Hiding SettingsWindow, saving data...");
        App.SettingsData.save();

        // Navigate to another page, so the "OnNavigatedFrom()" of OtherSettings gets called (for the timer).
        ContentFrame.Navigate(typeof(TouchpadSettings));

        _app.OnClosePrefsWindow();
    }

    private int _inputCount;
    private long _lastContact;
    private long _lastEventSpeed;
    public void OnTouchpadContact(IntPtr currentDevice, TouchpadContact[] contacts){
        _inputCount++;

        // Event speed is an average over 20 inputs calls (usually about 200 ms)
        if(_inputCount >= 20){
            _inputCount = 0;
            _lastEventSpeed = (Ctms() - _lastContact) / 20;
            _lastContact = Ctms();
        }
        Page currentPage = ContentFrame.Content as Page;
        if(currentPage is TouchpadSettings touchpadSettings){
            touchpadSettings.UpdateContactsText("TouchpadDevice: " + TouchpadHelper.GetDeivceInfo(currentDevice).ToString() + "\n" + string.Join('\n', contacts.Select(c => c.ToString())) + "\nEvent speed: " + _lastEventSpeed + "ms");
        }
    }
    public void OnTouchpadInitialized(){
        Page currentPage = ContentFrame.Content as Page;
        if(currentPage is TouchpadSettings touchpadSettings){
            touchpadSettings.OnTouchpadInitialized();
        }
    }

    private long Ctms(){
        return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
    }
}
