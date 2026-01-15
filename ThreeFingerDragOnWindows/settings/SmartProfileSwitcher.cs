using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using ThreeFingerDragOnWindows.settings.profiles;
using ThreeFingerDragOnWindows.touchpad;
using ThreeFingerDragOnWindows.utils;

namespace ThreeFingerDragOnWindows.settings;

public class SmartProfileSwitcher{
[DllImport("user32.dll")]
private static extern IntPtr GetForegroundWindow();

[DllImport("user32.dll")]
private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

[DllImport("user32.dll")]
private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, 
    WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

[DllImport("user32.dll")]
private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

private const uint EVENT_SYSTEM_FOREGROUND = 3;
private const uint WINEVENT_OUTOFCONTEXT = 0;

private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, 
    int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

private readonly Timer _checkTimer;
private WinEventDelegate _hookDelegate;
private IntPtr _hookHandle;
private string _lastProcessPath = "";
private bool _isEnabled = false;
private int _checkCount = 0;
private string _currentDeviceId = "";

public void SetCurrentDevice(string deviceId)
{
    if (_currentDeviceId != deviceId)
    {
        Logger.Log($"[SmartSwitcher] ►►► DEVICE CHANGED: '{_currentDeviceId}' -> '{deviceId}'");
        _currentDeviceId = deviceId;
        
        // Re-check foreground window with new device
        if (_isEnabled)
        {
            _lastProcessPath = ""; // Force re-check
            CheckForegroundWindow();
        }
    }
}

    public SmartProfileSwitcher(){
        _checkTimer = new Timer();
        _checkTimer.Elapsed += OnTimerElapsed;
        _checkTimer.AutoReset = true;
        
        // Keep delegate alive to prevent garbage collection
        _hookDelegate = new WinEventDelegate(WinEventProc);
    }

    public void Start(){
        if(!_isEnabled){
            _isEnabled = true;
            
            var mode = App.SettingsData.DetectionMode;
            Logger.Log($"★★★ Smart Profile Switcher STARTED - mode: {mode} ★★★");
            
            if(mode == SettingsData.ProfileSwitchingDetectionMode.WindowsHook){
                StartWindowsHook();
            } else {
                StartIntervalCheck();
            }
            
            // Do an immediate check
            CheckForegroundWindow();
        }
    }

    private void StartWindowsHook(){
        _hookHandle = SetWinEventHook(
            EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero, _hookDelegate,
            0, 0, WINEVENT_OUTOFCONTEXT);
        
        if(_hookHandle == IntPtr.Zero){
            Logger.Log("[SmartSwitcher] ERROR: Failed to set Windows hook, falling back to interval mode");
            StartIntervalCheck();
        } else {
            Logger.Log("[SmartSwitcher] Windows hook installed successfully");
        }
    }

    private void StartIntervalCheck(){
        var interval = App.SettingsData.DetectionInterval;
        _checkTimer.Interval = interval;
        _checkTimer.Start();
        Logger.Log($"[SmartSwitcher] Interval-based checking started ({interval}ms)");
    }

    public void Stop(){
        if(_isEnabled){
            _isEnabled = false;
            
            if(_hookHandle != IntPtr.Zero){
                UnhookWinEvent(_hookHandle);
                _hookHandle = IntPtr.Zero;
                Logger.Log("[SmartSwitcher] Windows hook uninstalled");
            }
            
            _checkTimer.Stop();
            Logger.Log("Smart Profile Switcher stopped");
        }
    }

    public void Restart(){
        Logger.Log("[SmartSwitcher] Restarting with new settings...");
        Stop();
        Start();
    }

    private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, 
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime){
        if(eventType == EVENT_SYSTEM_FOREGROUND){
            CheckForegroundWindow();
        }
    }

    private void OnTimerElapsed(object sender, ElapsedEventArgs e){
        CheckForegroundWindow();
    }

    private void CheckForegroundWindow(){
        if(!_isEnabled) return;
        
        try{
            _checkCount++;
            
            var foregroundWindow = GetForegroundWindow();
            if(foregroundWindow == IntPtr.Zero){
                if(_checkCount % 10 == 1) Logger.Log("[SmartSwitcher] No foreground window detected");
                return;
            }

            GetWindowThreadProcessId(foregroundWindow, out uint processId);
            if(processId == 0){
                if(_checkCount % 10 == 1) Logger.Log("[SmartSwitcher] No process ID found");
                return;
            }

            var process = Process.GetProcessById((int)processId);
            var processPath = GetProcessPath(process);
            var processName = process.ProcessName;

            if(string.IsNullOrEmpty(processPath)){
                if(_checkCount % 10 == 1) Logger.Log($"[SmartSwitcher] Could not get path for {processName}");
                return;
            }

            if(processPath == _lastProcessPath){
                return; // Same as before, no change
            }

            _lastProcessPath = processPath;
            Logger.Log($"[SmartSwitcher] ►►► Foreground changed to {processName}");
            Logger.Log($"[SmartSwitcher]     Path: {processPath}");

            // Check if any profile is associated with this process
            CheckAndSwitchProfile(processPath);

        } catch(Exception ex){
            Logger.Log($"[SmartSwitcher] ERROR: {ex.Message}");
        }
    }

    private string GetProcessPath(Process process){
        try{
            return process.MainModule?.FileName;
        } catch{
            return null;
        }
    }

    private void CheckAndSwitchProfile(string processPath){
        Logger.Log($"[SmartSwitcher] === CheckAndSwitchProfile called ===");
        Logger.Log($"[SmartSwitcher]   Process: {processPath}");
        Logger.Log($"[SmartSwitcher]   Current device: '{_currentDeviceId}'");
        
        if (string.IsNullOrEmpty(_currentDeviceId))
        {
            // Try to auto-detect device
            var devices = TouchpadHelper.GetAllDeivceInfos();
            if (devices.Count > 0)
            {
                _currentDeviceId = devices[0].deviceId;
                Logger.Log($"[SmartSwitcher] ✓ Auto-detected device: {_currentDeviceId}");
            }
            else
            {
                Logger.Log("[SmartSwitcher] ⚠️ No current device set and no devices detected, skipping profile check");
                return;
            }
        }

        var profilesDir = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "profiles");
        if(!Directory.Exists(profilesDir)){
            Logger.Log("[SmartSwitcher] ERROR: Profiles directory not found!");
            return;
        }

        var profileFiles = Directory.GetFiles(profilesDir, "*.xml");
        var currentProfilePath = App.SettingsData.ActiveProfilePath;
        
        Logger.Log($"[SmartSwitcher] Checking {profileFiles.Length} profiles for device: {_currentDeviceId}...");
        Logger.Log($"[SmartSwitcher] Current active: {Path.GetFileName(currentProfilePath)}");

        int profilesWithSmart = 0;
        int totalPrograms = 0;
        ThreeFingerDragProfile matchedProfile = null;
        string matchedProfileFile = null;

        // First pass: find matching profile for this device
        foreach(var profileFile in profileFiles){
            try{
                var profile = ThreeFingerDragProfile.Load(profileFile);
                var isActive = profileFile == currentProfilePath;
                
                // Get device-specific smart switching config
                var deviceConfig = profile.GetDeviceSmartSwitchingConfig(_currentDeviceId);
                
                Logger.Log($"[SmartSwitcher] Profile: '{profile.ProfileName}' {(isActive ? "(ACTIVE)" : "")}");
                Logger.Log($"[SmartSwitcher]   Device smart switching: {(deviceConfig.SmartSwitchingEnabled ? "YES" : "NO")}");
                
                if(deviceConfig.SmartSwitchingEnabled){
                    profilesWithSmart++;
                    Logger.Log($"[SmartSwitcher]   Associated programs for device ({deviceConfig.AssociatedPrograms.Count}):");
                    
                    foreach(var prog in deviceConfig.AssociatedPrograms){
                        totalPrograms++;
                        var matches = string.Equals(prog, processPath, StringComparison.OrdinalIgnoreCase);
                        Logger.Log($"[SmartSwitcher]     {(matches ? "✓ MATCH" : "-")} {prog}");
                        
                        if(matches && !isActive){
                            matchedProfile = profile;
                            matchedProfileFile = profileFile;
                            Logger.Log($"[SmartSwitcher]   >>> Found match in '{profile.ProfileName}' for device!");
                        }
                    }
                }
            } catch(Exception ex){
                Logger.Log($"[SmartSwitcher] ERROR loading {Path.GetFileName(profileFile)}: {ex.Message}");
            }
        }
        
        // If we found a match, switch to it
        if(matchedProfile != null && matchedProfileFile != null){
            Logger.Log($"[SmartSwitcher] ►►► SWITCHING to '{matchedProfile.ProfileName}' for device {_currentDeviceId}!");
            App.SettingsData.SwitchToProfile(matchedProfileFile);
            
            // Refresh settings window if open
            App.Instance.DispatcherQueue.TryEnqueue(() => {
                if(App.SettingsWindow is SettingsWindow settingsWindow){
                    settingsWindow.LoadProfiles();
                }
            });
        } else {
            Logger.Log($"[SmartSwitcher] Summary: {profilesWithSmart} profiles with smart switching for device, {totalPrograms} total programs");
            Logger.Log($"[SmartSwitcher] No matching profile found for current process on device {_currentDeviceId}");
        }
    }
}
