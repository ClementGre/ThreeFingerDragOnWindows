using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using ThreeFingerDragOnWindows.settings.profiles;
using ThreeFingerDragOnWindows.utils;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinUICommunity;
using WinRT.Interop;

namespace ThreeFingerDragOnWindows.settings;

public class ProfileViewModel : INotifyPropertyChanged
{
	private string _name;
	private Visibility _isActive = Visibility.Collapsed;
	private Visibility _hasSmartSwitching = Visibility.Collapsed;

	public string FilePath { get; set; }

	public string Name
	{
		get => _name;
		set
		{
			if (_name != value)
			{
				_name = value;
				OnPropertyChanged();
			}
		}
	}

	public Visibility IsActive
	{
		get => _isActive;
		set
		{
			if (_isActive != value)
			{
				_isActive = value;
				OnPropertyChanged();
			}
		}
	}

	public Visibility HasSmartSwitching
	{
		get => _hasSmartSwitching;
		set
		{
			if (_hasSmartSwitching != value)
			{
				_hasSmartSwitching = value;
				OnPropertyChanged();
			}
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}

public sealed partial class ProfilesSettings : Page
{
	private ObservableCollection<ProfileViewModel> _profiles = new ObservableCollection<ProfileViewModel>();
	private ProfileViewModel _selectedProfile;
	private ProfileViewModel _rightClickedProfile;
	private ProfileViewModel _previousSelection;

	public ProfilesSettings()
	{
		InitializeComponent();
		LoadProfiles();
		LoadDetectionSettings();
	}

	private void LoadProfiles()
	{
		_profiles.Clear();

		var profilesDir = Path.Combine(ApplicationData.Current.LocalFolder.Path, "profiles");
		if (!Directory.Exists(profilesDir))
		{
			return;
		}

		var activeProfilePath = App.SettingsData.ActiveProfilePath;
		var profileFiles = Directory.GetFiles(profilesDir, "*.xml");

		foreach (var file in profileFiles)
		{
			try
			{
				var profile = ThreeFingerDragProfile.Load(file);
				_profiles.Add(new ProfileViewModel
				{
					Name = profile.ProfileName,
					FilePath = file,
					IsActive = file == activeProfilePath ? Visibility.Visible : Visibility.Collapsed,
					HasSmartSwitching = profile.SmartSwitchingEnabled ? Visibility.Visible : Visibility.Collapsed
				});
			}
			catch (Exception ex)
			{
				Logger.Log($"Error loading profile {file}: {ex.Message}");
			}
		}

		ProfilesListView.ItemsSource = _profiles;

		// Select active profile
		var activeProfile = _profiles.FirstOrDefault(p => p.IsActive == Visibility.Visible);
		if (activeProfile != null)
		{
			ProfilesListView.SelectedItem = activeProfile;
		}

		UpdateButtonStates();
	}

	private void ProfilesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		_selectedProfile = ProfilesListView.SelectedItem as ProfileViewModel;

		if (_selectedProfile != null)
		{
			SmartSwitchingSection.Visibility = Visibility.Visible;
			LoadProfileSettings(_selectedProfile);
		}
		else
		{
			SmartSwitchingSection.Visibility = Visibility.Collapsed;
		}

		UpdateButtonStates();
	}

	private void UpdateButtonStates()
	{
		var hasSelection = _selectedProfile != null;
		DuplicateButton.IsEnabled = hasSelection;
		DeleteButton.IsEnabled = hasSelection;
	}

	private void LoadProfileSettings(ProfileViewModel profileVm)
	{
		try
		{
			var profile = ThreeFingerDragProfile.Load(profileVm.FilePath);

			ProfileNameTextBox.Text = profile.ProfileName;
			SmartSwitchingToggle.IsOn = profile.SmartSwitchingEnabled;
			SmartSwitchingOptions.Visibility = profile.SmartSwitchingEnabled ? Visibility.Visible : Visibility.Collapsed;

			// Auto-expand Smart Switching section if enabled
			SmartSwitchingExpander.IsExpanded = profile.SmartSwitchingEnabled;

			AssociatedProgramsListView.ItemsSource = new ObservableCollection<string>(profile.AssociatedPrograms);
		}
		catch (Exception ex)
		{
			Logger.Log($"Error loading profile settings: {ex.Message}");
		}
	}

	private void ProfileNameTextBox_LostFocus(object sender, RoutedEventArgs e)
	{
		if (_selectedProfile != null && !string.IsNullOrWhiteSpace(ProfileNameTextBox.Text))
		{
			var newName = ProfileNameTextBox.Text.Trim();
			if (newName != _selectedProfile.Name)
			{
				RenameProfile(_selectedProfile, newName);
			}
		}
	}

	private async void RenameProfile(ProfileViewModel profileVm, string newName)
	{
		try
		{
			var profile = ThreeFingerDragProfile.Load(profileVm.FilePath);
			var oldPath = profileVm.FilePath;
			var newPath = SettingsData.GetProfileFilePath(newName);

			if (File.Exists(newPath) && newPath != oldPath)
			{
				await ShowErrorDialog($"Profile with name '{newName}' already exists");
				profileVm.Name = profile.ProfileName; // Revert
				return;
			}

			profile.ProfileName = newName;
			profile.Save(newPath);

			if (oldPath != newPath && File.Exists(oldPath))
			{
				File.Delete(oldPath);
			}

			profileVm.Name = newName;
			profileVm.FilePath = newPath;

			// Update active profile path if this was the active profile
			if (profileVm.IsActive == Visibility.Visible)
			{
				App.SettingsData.ActiveProfilePath = newPath;
				App.SettingsData.save();
			}

			// Update profiles list in settings data
			var profileInfo = App.SettingsData.Profiles.FirstOrDefault(p => p.FilePath == oldPath);
			if (profileInfo != null)
			{
				profileInfo.Name = newName;
				profileInfo.FilePath = newPath;
				App.SettingsData.save();
			}

			// Refresh SettingsWindow profile selector
			if (App.SettingsWindow is SettingsWindow settingsWindow)
			{
				settingsWindow.LoadProfiles();
			}

			Logger.Log($"Profile renamed to {newName}");
		}
		catch (Exception ex)
		{
			Logger.Log($"Error renaming profile: {ex.Message}");
			await ShowErrorDialog($"Error renaming profile: {ex.Message}");
		}
	}

	private void ProfilesListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
	{
		var item = (e.OriginalSource as FrameworkElement)?.DataContext as ProfileViewModel;
		if (item != null)
		{
			// Store current selection and prevent selection change
			_previousSelection = _selectedProfile;
			_rightClickedProfile = item;
			// Don't change selection - just store the right-clicked item for context menu
		}
	}

	private async void NewProfile_Click(object sender, RoutedEventArgs e)
	{
		var dialog = new ContentDialog
		{
			XamlRoot = Content.XamlRoot,
			Title = "Create New Profile",
			PrimaryButtonText = "Create",
			CloseButtonText = "Cancel",
			DefaultButton = ContentDialogButton.Primary
		};

		var textBox = new TextBox
		{
			PlaceholderText = "Enter profile name",
			Text = "New Profile"
		};
		
		// Auto-select all text when the dialog opens
		textBox.Loaded += (s, e) => {
			textBox.SelectAll();
			textBox.Focus(FocusState.Programmatic);
		};
		
		dialog.Content = textBox;

		var result = await dialog.ShowAsyncDraggable();

		if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(textBox.Text))
		{
			var newProfileName = textBox.Text.Trim();
			var newProfilePath = SettingsData.GetProfileFilePath(newProfileName);

			if (File.Exists(newProfilePath))
			{
				await ShowErrorDialog("A profile with this name already exists.");
				return;
			}

			var newProfile = new ThreeFingerDragProfile
			{
				ProfileName = newProfileName
			};
			newProfile.Save(newProfilePath);

			App.SettingsData.Profiles.Add(new SettingsData.ProfileInfo
			{
				Name = newProfileName,
				FilePath = newProfilePath
			});
			App.SettingsData.save();

			LoadProfiles();
			ProfilesListView.SelectedItem = _profiles.FirstOrDefault(p => p.Name == newProfileName);

			// Refresh SettingsWindow profile selector
			if (App.SettingsWindow is SettingsWindow settingsWindow)
			{
				settingsWindow.LoadProfiles();
			}

			Logger.Log($"Created new profile: {newProfileName}");
		}
	}

	private void ActivateProfile_Click(object sender, RoutedEventArgs e)
	{
		var targetProfile = _rightClickedProfile ?? _selectedProfile;
		if (targetProfile != null)
		{
			App.SettingsData.SwitchToProfile(targetProfile.FilePath);

			// Update UI
			foreach (var profile in _profiles)
			{
				profile.IsActive = profile == targetProfile ? Visibility.Visible : Visibility.Collapsed;
			}

			// Refresh SettingsWindow profile selector to show the activated profile
			if (App.SettingsWindow is SettingsWindow settingsWindow)
			{
				settingsWindow.LoadProfiles();
			}

			Logger.Log($"Activated profile: {targetProfile.Name}");
		}
		_rightClickedProfile = null;
	}

	private async void DuplicateProfile_Click(object sender, RoutedEventArgs e)
	{
		if (_selectedProfile == null) return;

		var currentProfile = ThreeFingerDragProfile.Load(_selectedProfile.FilePath);

		var dialog = new ContentDialog
		{
			XamlRoot = Content.XamlRoot,
			Title = "Duplicate Profile",
			PrimaryButtonText = "Duplicate",
			CloseButtonText = "Cancel",
			DefaultButton = ContentDialogButton.Primary
		};

		var textBox = new TextBox
		{
			PlaceholderText = "Enter name for duplicated profile",
			Text = currentProfile.ProfileName + " Copy"
		};
		
		// Auto-select all text when the dialog opens
		textBox.Loaded += (s, e) => {
			textBox.SelectAll();
			textBox.Focus(FocusState.Programmatic);
		};
		
		dialog.Content = textBox;

		var result = await dialog.ShowAsyncDraggable();

		if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(textBox.Text))
		{
			var newProfileName = textBox.Text.Trim();
			var newProfilePath = SettingsData.GetProfileFilePath(newProfileName);

			if (File.Exists(newProfilePath))
			{
				await ShowErrorDialog("A profile with this name already exists.");
				return;
			}

			var duplicatedProfile = new ThreeFingerDragProfile
			{
				ProfileName = newProfileName,
				SmartSwitchingEnabled = currentProfile.SmartSwitchingEnabled,
				AssociatedPrograms = new List<string>(currentProfile.AssociatedPrograms),
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

			App.SettingsData.Profiles.Add(new SettingsData.ProfileInfo
			{
				Name = newProfileName,
				FilePath = newProfilePath
			});
			App.SettingsData.save();

			LoadProfiles();
			ProfilesListView.SelectedItem = _profiles.FirstOrDefault(p => p.Name == newProfileName);

			Logger.Log($"Duplicated profile to {newProfileName}");
		}
	}

	private async void DeleteProfile_Click(object sender, RoutedEventArgs e)
	{
		var targetProfile = _rightClickedProfile ?? _selectedProfile;
		if (targetProfile == null) return;

		if (targetProfile.IsActive == Visibility.Visible)
		{
			await ShowErrorDialog("Cannot delete the active profile. Please activate another profile first.");
			_rightClickedProfile = null;
			return;
		}

		var dialog = new ContentDialog
		{
			XamlRoot = Content.XamlRoot,
			Title = "Delete Profile",
			Content = $"Are you sure you want to delete the profile '{targetProfile.Name}'?",
			PrimaryButtonText = "Delete",
			CloseButtonText = "Cancel",
			DefaultButton = ContentDialogButton.Close
		};

		var result = await dialog.ShowAsyncDraggable();

		if (result == ContentDialogResult.Primary)
		{
			try
			{
				File.Delete(targetProfile.FilePath);

				var profileInfo = App.SettingsData.Profiles.FirstOrDefault(p => p.FilePath == targetProfile.FilePath);
				if (profileInfo != null)
				{
					App.SettingsData.Profiles.Remove(profileInfo);
					App.SettingsData.save();
				}

				LoadProfiles();
				
				// Refresh SettingsWindow profile selector
				if (App.SettingsWindow is SettingsWindow settingsWindow)
				{
					settingsWindow.LoadProfiles();
				}
				
				Logger.Log($"Deleted profile: {targetProfile.Name}");
			}
			catch (Exception ex)
			{
				await ShowErrorDialog($"Error deleting profile: {ex.Message}");
			}
		}
		_rightClickedProfile = null;
	}

	private void SmartSwitchingToggle_Toggled(object sender, RoutedEventArgs e)
	{
		if (_selectedProfile == null) return;

		try
		{
			var profile = ThreeFingerDragProfile.Load(_selectedProfile.FilePath);
			profile.SmartSwitchingEnabled = SmartSwitchingToggle.IsOn;
			profile.Save(_selectedProfile.FilePath);

			_selectedProfile.HasSmartSwitching = SmartSwitchingToggle.IsOn ? Visibility.Visible : Visibility.Collapsed;
			SmartSwitchingOptions.Visibility = SmartSwitchingToggle.IsOn ? Visibility.Visible : Visibility.Collapsed;

			Logger.Log($"Smart switching {(SmartSwitchingToggle.IsOn ? "enabled" : "disabled")} for {_selectedProfile.Name}");
		}
		catch (Exception ex)
		{
			Logger.Log($"Error toggling smart switching: {ex.Message}");
		}
	}

	private async void AddProgram_Click(object sender, RoutedEventArgs e)
	{
		var picker = new FileOpenPicker();
		picker.FileTypeFilter.Add(".exe");

		var hwnd = WindowNative.GetWindowHandle(App.SettingsWindow);
		InitializeWithWindow.Initialize(picker, hwnd);

		var file = await picker.PickSingleFileAsync();
		if (file != null)
		{
			AddProgramToProfile(file.Path);
		}
	}

	private async void AddFromRunning_Click(object sender, RoutedEventArgs e)
	{
		var runningProcesses = Process.GetProcesses()
			.Where(p => !string.IsNullOrEmpty(p.MainWindowTitle))
			.Select(p => {
				try
				{
					return new { p.ProcessName, Path = p.MainModule?.FileName };
				}
				catch
				{
					return null;
				}
			})
			.Where(p => p != null && !string.IsNullOrEmpty(p.Path))
			.DistinctBy(p => p.Path)
			.OrderBy(p => p.ProcessName)
			.ToList();

		var dialog = new ContentDialog
		{
			XamlRoot = Content.XamlRoot,
			Title = "Select Running Program",
			PrimaryButtonText = "Add",
			CloseButtonText = "Cancel",
			DefaultButton = ContentDialogButton.Primary
		};

		var listView = new ListView
		{
			ItemsSource = runningProcesses,
			SelectionMode = ListViewSelectionMode.Single,
			DisplayMemberPath = "ProcessName",
			Height = 400
		};

		dialog.Content = listView;

		var result = await dialog.ShowAsyncDraggable();

		if (result == ContentDialogResult.Primary && listView.SelectedItem != null)
		{
			dynamic selected = listView.SelectedItem;
			AddProgramToProfile(selected.Path);
		}
	}

	private void AddProgramToProfile(string exePath)
	{
		if (_selectedProfile == null || string.IsNullOrEmpty(exePath)) return;

		try
		{
			var profile = ThreeFingerDragProfile.Load(_selectedProfile.FilePath);

			if (!profile.AssociatedPrograms.Contains(exePath))
			{
				profile.AssociatedPrograms.Add(exePath);
				profile.Save(_selectedProfile.FilePath);

				// Refresh UI
				LoadProfileSettings(_selectedProfile);

				Logger.Log($"Added program {exePath} to profile {_selectedProfile.Name}");
			}
		}
		catch (Exception ex)
		{
			Logger.Log($"Error adding program: {ex.Message}");
		}
	}

	private void RemoveProgram_Click(object sender, RoutedEventArgs e)
	{
		var button = sender as Button;
		var exePath = button?.Tag as string;

		if (_selectedProfile == null || string.IsNullOrEmpty(exePath)) return;

		try
		{
			var profile = ThreeFingerDragProfile.Load(_selectedProfile.FilePath);
			profile.AssociatedPrograms.Remove(exePath);
			profile.Save(_selectedProfile.FilePath);

			// Refresh UI
			LoadProfileSettings(_selectedProfile);

			Logger.Log($"Removed program {exePath} from profile {_selectedProfile.Name}");
		}
		catch (Exception ex)
		{
			Logger.Log($"Error removing program: {ex.Message}");
		}
	}

	private async System.Threading.Tasks.Task ShowErrorDialog(string message)
	{
		var errorDialog = new ContentDialog
		{
			XamlRoot = Content.XamlRoot,
			Title = "Error",
			Content = message,
			CloseButtonText = "OK"
		};
		await errorDialog.ShowAsyncDraggable();
	}

	// Detection Mode Settings
	private void LoadDetectionSettings()
	{
		var mode = App.SettingsData.DetectionMode;
		DetectionModeComboBox.SelectedIndex = mode == SettingsData.ProfileSwitchingDetectionMode.WindowsHook ? 0 : 1;
		IntervalNumberBox.Value = App.SettingsData.DetectionInterval;
		IntervalSettingsPanel.Visibility = mode == SettingsData.ProfileSwitchingDetectionMode.IntervalBased
			? Visibility.Visible : Visibility.Collapsed;
	}

	private void DetectionMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (DetectionModeComboBox.SelectedItem is ComboBoxItem item)
		{
			var mode = item.Tag.ToString() == "WindowsHook"
				? SettingsData.ProfileSwitchingDetectionMode.WindowsHook
				: SettingsData.ProfileSwitchingDetectionMode.IntervalBased;

			App.SettingsData.DetectionMode = mode;
			App.SettingsData.save();

			IntervalSettingsPanel.Visibility = mode == SettingsData.ProfileSwitchingDetectionMode.IntervalBased
				? Visibility.Visible : Visibility.Collapsed;

			// Restart the switcher with new settings
			App.SmartProfileSwitcher?.Restart();

			Logger.Log($"Detection mode changed to: {mode}");
		}
	}

	private void IntervalNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
	{
		if (double.IsNaN(args.NewValue)) return;

		var interval = (int)args.NewValue;
		App.SettingsData.DetectionInterval = interval;
		App.SettingsData.save();

		// Restart the switcher with new interval
		if (App.SettingsData.DetectionMode == SettingsData.ProfileSwitchingDetectionMode.IntervalBased)
		{
			App.SmartProfileSwitcher?.Restart();
		}

		Logger.Log($"Detection interval changed to: {interval}ms");
	}
}



