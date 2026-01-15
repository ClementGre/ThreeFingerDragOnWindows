using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using ThreeFingerDragEngine.utils;
using ThreeFingerDragOnWindows.touchpad;

namespace ThreeFingerDragOnWindows.utils;

// Code from emoacht/RawInput.Touchpad, edited by Clément Grennerat
internal static class TouchpadHelper {
    public const int WM_INPUT = 0x00FF;
    public const int WM_INPUT_DEVICE_CHANGE = 0x00FE;
    public const int RIM_INPUT = 0;
    public const int RIM_INPUTSINK = 1;
    
    private static Dictionary<IntPtr, TouchpadDeviceInfo> availableDeviceInfos = new Dictionary<IntPtr, TouchpadDeviceInfo>(2);
    private static Dictionary<IntPtr, DateTime> deviceLastSeenTime = new Dictionary<IntPtr, DateTime>(2);
    private static Dictionary<IntPtr, bool> deviceHasSentInput = new Dictionary<IntPtr, bool>(2);
    private static readonly TimeSpan DeviceRemovalGracePeriod = TimeSpan.FromSeconds(1);

    private static TouchpadDeviceInfo GetDeviceInfoFromHid(IntPtr hwnd)
    {
        TouchpadDeviceInfo touchpadDevice = new TouchpadDeviceInfo();
        touchpadDevice.hid = hwnd;
        
        uint nameSize = 0;
        if (GetRawInputDeviceInfo(hwnd, RIDI_DEVICENAME, IntPtr.Zero, ref nameSize) == unchecked((uint)-1))
        {
            touchpadDevice.deviceId = "default";
            return touchpadDevice;
        }
        
        var ptr = Marshal.AllocHGlobal((int)nameSize);
        try
        {
            if (GetRawInputDeviceInfo(hwnd, RIDI_DEVICENAME, ptr, ref nameSize) != unchecked((uint)-1))
            {
                var deviceName = Marshal.PtrToStringAnsi(ptr);
                touchpadDevice.deviceId = ComputeMD5(deviceName);
                
                // Store the raw device name for connection type detection
                // Device name format: \\?\HID#VID_xxxx&PID_yyyy&...
                // Bluetooth devices typically have different patterns in their device paths
                touchpadDevice.deviceName = deviceName;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }

        return touchpadDevice;
    }
    
    public static bool Exists(IntPtr hwnd)
    {
        uint deviceInfoSize = 0;

        if (GetRawInputDeviceInfo(
                hwnd,
                RIDI_DEVICEINFO,
                IntPtr.Zero,
                ref deviceInfoSize) != 0)
            return false;

        var deviceInfo = new RID_DEVICE_INFO { cbSize = deviceInfoSize };

        if (GetRawInputDeviceInfo(
                hwnd,
                RIDI_DEVICEINFO,
                ref deviceInfo,
                ref deviceInfoSize) == unchecked((uint)-1))
            return false;

        if (deviceInfo.hid.usUsagePage == 0x000D &&
            deviceInfo.hid.usUsage == 0x0005)
        {
            bool isNewDevice = !availableDeviceInfos.ContainsKey(hwnd);
            
            if (isNewDevice)
            {
                var newDeviceInfo = GetDeviceInfoFromHid(hwnd);
                newDeviceInfo.vendorId = deviceInfo.hid.dwVendorId.ToString();
                newDeviceInfo.productId = deviceInfo.hid.dwProductId.ToString();
                
                availableDeviceInfos[hwnd] = newDeviceInfo;
                deviceLastSeenTime[hwnd] = DateTime.Now; // Only set timestamp for NEW devices
                deviceHasSentInput[hwnd] = false; // New devices haven't sent input yet
                Logger.Log($"[TouchpadHelper] Added device: {hwnd}, deviceId: {newDeviceInfo.deviceId}, VID/PID: {newDeviceInfo.vendorId}/{newDeviceInfo.productId}");
            }
            // Note: We don't update timestamp for existing devices during enumeration
            // Timestamps should only be updated when devices actually send input
            
            return true;
        }
        return false;
    }
    
    public static bool Exists() {
        uint deviceListCount = 0;
        var rawInputDeviceListSize = (uint) Marshal.SizeOf<RAWINPUTDEVICELIST>();
        
        
        if(GetRawInputDeviceList(
               null,
               ref deviceListCount,
               rawInputDeviceListSize) != 0)
            return false;

        var devices = new RAWINPUTDEVICELIST[deviceListCount];

        if(GetRawInputDeviceList(
               devices,
               ref deviceListCount,
               rawInputDeviceListSize) != deviceListCount)
            return false;

        foreach (var device in devices.Where(x => x.dwType == RIM_TYPEHID))
        {
            if (Exists(device.hDevice))
            {
                return true;
            }
        }
        return false;
    }
    
    public static string ComputeMD5(string input)
    {
        if (string.IsNullOrEmpty(input))
            return null;

        using (MD5 md5 = MD5.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            // 转换为十六进制字符串
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                sb.Append(b.ToString("x2")); // 小写十六进制，如 "a1b2c3"
                // 或使用 sb.Append(b.ToString("X2")); // 大写，如 "A1B2C3"
            }
            return sb.ToString();
        }
    }

    public static TouchpadDeviceInfo GetDeivceInfo(IntPtr currentDevice)
    {
        if (availableDeviceInfos.ContainsKey(currentDevice))
        {
            return availableDeviceInfos[currentDevice];
        }
        return null;
    }

    /// <summary>
    /// Validates that a device handle is still valid by checking if it still exists in the system
    /// </summary>
    private static bool IsDeviceStillValid(IntPtr hDevice)
    {
        uint deviceInfoSize = 0;
        // Try to get device info - if it fails, the device is no longer valid
        return GetRawInputDeviceInfo(hDevice, RIDI_DEVICEINFO, IntPtr.Zero, ref deviceInfoSize) == 0;
    }

    /// <summary>
    /// Removes disconnected devices from the cache, but only after a grace period
    /// to handle device reconnections during mode switches (e.g., Bluetooth to wired)
    /// </summary>
    public static void CleanupDisconnectedDevices()
    {
        var now = DateTime.Now;
        var devicesToRemove = new List<IntPtr>();

        foreach (var kvp in availableDeviceInfos)
        {
            var device = kvp.Key;
            
            // Check if device is still valid
            if (!IsDeviceStillValid(device))
            {
                // If we haven't tracked this device's last seen time, set it now
                if (!deviceLastSeenTime.ContainsKey(device))
                {
                    deviceLastSeenTime[device] = now;
                }
                // Only remove if grace period has elapsed
                else if (now - deviceLastSeenTime[device] > DeviceRemovalGracePeriod)
                {
                    devicesToRemove.Add(device);
                }
            }
            else
            {
                // Device is valid, update last seen time
                deviceLastSeenTime[device] = now;
            }
        }

        foreach (var device in devicesToRemove)
        {
            var deviceInfo = availableDeviceInfos[device];
            availableDeviceInfos.Remove(device);
            deviceLastSeenTime.Remove(device);
            deviceHasSentInput.Remove(device);
            Logger.Log($"[TouchpadHelper] Removed disconnected device after grace period: {device}, deviceId: {deviceInfo.deviceId}");
        }
    }

    public static List<TouchpadDeviceInfo> GetAllDeivceInfos()
    {
        // Clean up stale entries before returning
        CleanupDisconnectedDevices();
        return availableDeviceInfos.Values.ToList();
    }

    /// <summary>
    /// Extracts a normalized hardware identifier from the device name to match Bluetooth and wired connections
    /// of the same physical device. Returns the VID and PID extracted from the device path.
    /// </summary>
    private static string GetNormalizedHardwareId(string deviceName, string vendorId, string productId)
    {
        if (string.IsNullOrEmpty(deviceName))
            return $"{vendorId}_{productId}";
        
        // Try to extract VID and PID from device name path
        // Format examples:
        // \\?\HID#VID_046D&PID_B036&MI_01&Col02#...  (USB/wired)
        // \\?\HID#VID_046D&PID_B036&...  (Bluetooth)
        // We want to extract the VID_xxxx&PID_yyyy part as the hardware identifier
        
        var vidIndex = deviceName.IndexOf("VID_", StringComparison.OrdinalIgnoreCase);
        var pidIndex = deviceName.IndexOf("PID_", StringComparison.OrdinalIgnoreCase);
        
        if (vidIndex >= 0 && pidIndex >= 0)
        {
            // Extract VID (4 hex digits after VID_)
            var vid = deviceName.Substring(vidIndex + 4, 4);
            // Extract PID (4 hex digits after PID_)
            var pid = deviceName.Substring(pidIndex + 4, 4);
            return $"VID_{vid}_PID_{pid}";
        }
        
        // Fallback to vendorId + productId
        return $"{vendorId}_{productId}";
    }
    
    /// <summary>
    /// Re-enumerates all touchpad devices. Should be called when WM_INPUT_DEVICE_CHANGE is received
    /// to handle device reconnections (e.g., switching between Bluetooth and wired mode)
    /// </summary>
    public static void RefreshDevices()
    {
        Logger.Log("[TouchpadHelper] Refreshing device list...");
        
        uint deviceListCount = 0;
        var rawInputDeviceListSize = (uint)Marshal.SizeOf<RAWINPUTDEVICELIST>();
        
        if (GetRawInputDeviceList(null, ref deviceListCount, rawInputDeviceListSize) != 0)
        {
            Logger.Log("[TouchpadHelper] Failed to get device count");
            return;
        }

        var devices = new RAWINPUTDEVICELIST[deviceListCount];

        if (GetRawInputDeviceList(devices, ref deviceListCount, rawInputDeviceListSize) != deviceListCount)
        {
            Logger.Log("[TouchpadHelper] Failed to enumerate devices");
            return;
        }

        // Track which handles are currently valid in the system and which are new
        var currentHandles = new HashSet<IntPtr>();
        var newlyAddedHandles = new HashSet<IntPtr>();
        
        // Check each HID device to see if it's a touchpad
        foreach (var device in devices.Where(x => x.dwType == RIM_TYPEHID))
        {
            bool wasNew = !availableDeviceInfos.ContainsKey(device.hDevice);
            if (Exists(device.hDevice)) // This will add/update the device in our cache
            {
                currentHandles.Add(device.hDevice);
                if (wasNew)
                {
                    newlyAddedHandles.Add(device.hDevice);
                }
            }
        }
        
        // Remove any devices from our cache that are not in the current enumeration
        // This ensures we don't keep stale devices when hardware disconnects
        var devicesToRemove = availableDeviceInfos.Keys.Where(handle => !currentHandles.Contains(handle)).ToList();
        
        foreach (var handle in devicesToRemove)
        {
            var deviceInfo = availableDeviceInfos[handle];
            availableDeviceInfos.Remove(handle);
            deviceLastSeenTime.Remove(handle);
            deviceHasSentInput.Remove(handle);
            Logger.Log($"[TouchpadHelper] Removed device not found in enumeration: {handle}, deviceId: {deviceInfo.deviceId}, VID/PID: {deviceInfo.vendorId}/{deviceInfo.productId}");
        }
        
        // Deduplicate devices with the same hardware ID (extracted from device name)
        // This handles Bluetooth vs wired connections which may have different VID/PID reported by Windows
        // but share the same hardware identifiers in their device path
        var devicesByHardwareId = availableDeviceInfos
            .GroupBy(kvp => GetNormalizedHardwareId(kvp.Value.deviceName, kvp.Value.vendorId, kvp.Value.productId))
            .Where(g => g.Count() > 1)
            .ToList();
        
        foreach (var group in devicesByHardwareId)
        {
            // Three-tier sorting priority:
            // 1. Devices that have sent actual input (active devices)
            // 2. Newly added devices (likely the new connection)
            // 3. Most recent timestamp (fallback)
            var sortedDevices = group.OrderByDescending(kvp => deviceHasSentInput.ContainsKey(kvp.Key) && deviceHasSentInput[kvp.Key] ? 1 : 0)
                                     .ThenByDescending(kvp => newlyAddedHandles.Contains(kvp.Key) ? 1 : 0)
                                     .ThenByDescending(kvp => deviceLastSeenTime.ContainsKey(kvp.Key) ? deviceLastSeenTime[kvp.Key] : DateTime.MinValue)
                                     .ToList();
            
            var keepDevice = sortedDevices.First();
            var hasSentInput = deviceHasSentInput.ContainsKey(keepDevice.Key) && deviceHasSentInput[keepDevice.Key];
            var wasNewDevice = newlyAddedHandles.Contains(keepDevice.Key);
            
            string reason = hasSentInput ? "has sent input" : (wasNewDevice ? "newly added" : "most recent");
            Logger.Log($"[TouchpadHelper] Found {group.Count()} devices with hardware ID: {group.Key}, keeping ({reason}): {keepDevice.Key}");
            
            // Remove all except the first one
            foreach (var device in sortedDevices.Skip(1))
            {
                var deviceHadSentInput = deviceHasSentInput.ContainsKey(device.Key) && deviceHasSentInput[device.Key];
                Logger.Log($"[TouchpadHelper] Removing duplicate device: {device.Key}, deviceId: {device.Value.deviceId}, VID/PID: {device.Value.vendorId}/{device.Value.productId}, hadSentInput: {deviceHadSentInput}");
                availableDeviceInfos.Remove(device.Key);
                deviceLastSeenTime.Remove(device.Key);
                deviceHasSentInput.Remove(device.Key);
            }
        }
        
        Logger.Log($"[TouchpadHelper] Refresh complete. Active devices: {availableDeviceInfos.Count}");
    }

    public static bool RegisterInput(IntPtr hwndTarget){
        // Precision Touchpad (PTP) in HID Clients Supported in Windows
        // https://docs.microsoft.com/en-us/windows-hardware/drivers/hid/hid-architecture#hid-clients-supported-in-windows
        var device = new RAWINPUTDEVICE{
            usUsagePage = 0x000D,
            usUsage = 0x0005,
            dwFlags = 0x00002100, // Messages come even if the window is in the background/foreground.
            hwndTarget = hwndTarget
        };

        return RegisterRawInputDevices(new[]{ device }, 1, (uint) Marshal.SizeOf<RAWINPUTDEVICE>());
    }

    public static (IntPtr, List<TouchpadContact>, uint) ParseInput(IntPtr lParam){
        // Get RAWINPUT.
        uint rawInputSize = 0;
        var rawInputHeaderSize = (uint) Marshal.SizeOf<RAWINPUTHEADER>();
        
        IntPtr currentDevice = IntPtr.Zero;
        if(GetRawInputData(
               lParam,
               RID_INPUT,
               IntPtr.Zero,
               ref rawInputSize,
               rawInputHeaderSize) != 0)
            return (currentDevice, null, 0);

        RAWINPUT rawInput;
        byte[] rawHidRawData;

        var rawInputPointer = IntPtr.Zero;
        try{
            rawInputPointer = Marshal.AllocHGlobal((int) rawInputSize);

            if(GetRawInputData(
                   lParam,
                   RID_INPUT,
                   rawInputPointer,
                   ref rawInputSize,
                   rawInputHeaderSize) != rawInputSize)
                return (currentDevice, null, 0);

            rawInput = Marshal.PtrToStructure<RAWINPUT>(rawInputPointer);

            currentDevice = rawInput.Header.hDevice;
            
            var rawInputData = new byte[rawInputSize];
            Marshal.Copy(rawInputPointer, rawInputData, 0, rawInputData.Length);

            rawHidRawData = new byte[rawInput.Hid.dwSizeHid * rawInput.Hid.dwCount];
            var rawInputOffset = (int) rawInputSize - rawHidRawData.Length;
            Buffer.BlockCopy(rawInputData, rawInputOffset, rawHidRawData, 0, rawHidRawData.Length);
        } finally{
            Marshal.FreeHGlobal(rawInputPointer);
        }

        // Parse RAWINPUT.
        var rawHidRawDataPointer = Marshal.AllocHGlobal(rawHidRawData.Length);
        Marshal.Copy(rawHidRawData, 0, rawHidRawDataPointer, rawHidRawData.Length);

        var preparsedDataPointer = IntPtr.Zero;
        try{
            uint preparsedDataSize = 0;

            if(GetRawInputDeviceInfo(
                   rawInput.Header.hDevice,
                   RIDI_PREPARSEDDATA,
                   IntPtr.Zero,
                   ref preparsedDataSize) != 0)
                return (currentDevice, null, 0);
            
            
            preparsedDataPointer = Marshal.AllocHGlobal((int) preparsedDataSize);

            if(GetRawInputDeviceInfo(
                   rawInput.Header.hDevice,
                   RIDI_PREPARSEDDATA,
                   preparsedDataPointer,
                   ref preparsedDataSize) != preparsedDataSize)
                return (currentDevice,null, 0);

            if(HidP_GetCaps(
                   preparsedDataPointer,
                   out var caps) != HIDP_STATUS_SUCCESS)
                return (currentDevice,null, 0);

            var valueCapsLength = caps.NumberInputValueCaps;
            var valueCaps = new HIDP_VALUE_CAPS[valueCapsLength];

            if(HidP_GetValueCaps(
                   HIDP_REPORT_TYPE.HidP_Input,
                   valueCaps,
                   ref valueCapsLength,
                   preparsedDataPointer) != HIDP_STATUS_SUCCESS)
                return (currentDevice,null, 0);

            uint scanTime = 0;
            uint contactCount = 99;

            List<TouchpadContactCreator> creators = new();
            List<TouchpadContact> contacts = new();

            // Iterating though each value (scanTime, contactCount, contactId, x, y)
            // Sometimes, iterates also through contacts by looping these values for each contact
            String toLog = "Parsing RawInput: ";
            foreach(var valueCap in valueCaps.OrderBy(x => x.LinkCollection)){
                toLog += "| ";
                // In case this valueCap contains multiple contacts at a time (rawInput.Hid.dwCount), iterates over each contact
                for(int contactIndex = 0; contactIndex < rawInput.Hid.dwCount; contactIndex++){
                    toLog += contactIndex + ": ";
                    IntPtr rawHidRawDataPointerAdjusted = IntPtr.Add(rawHidRawDataPointer,
                        (int) (rawInput.Hid.dwSizeHid * contactIndex));

                    if(HidP_GetUsageValue(
                           HIDP_REPORT_TYPE.HidP_Input,
                           valueCap.UsagePage,
                           valueCap.LinkCollection,
                           valueCap.Usage,
                           out var value,
                           preparsedDataPointer,
                           rawHidRawDataPointerAdjusted,
                           (uint) rawHidRawData.Length) != HIDP_STATUS_SUCCESS)
                        continue;

                    // Usage Page and ID in Windows Precision Touchpad input reports
                    // https://docs.microsoft.com/en-us/windows-hardware/design/component-guidelines/windows-precision-touchpad-required-hid-top-level-collections#windows-precision-touchpad-input-reports
                    switch(valueCap.LinkCollection){
                        case 0:
                            switch (valueCap.UsagePage, valueCap.Usage){
                                case (0x0D, 0x56): // Scan Time
                                    toLog += $"sT{value} ";
                                    scanTime = value;
                                    break;

                                case (0x0D, 0x54): // Contact Count
                                    toLog += $"cC{value} ";
                                    contactCount = value;
                                    break;
                                default:
                                    toLog += $"0U{valueCap.UsagePage}/{valueCap.Usage} ";
                                    break;
                            }

                            break;

                        default:
                            while(creators.Count <= contactIndex){
                                creators.Add(new TouchpadContactCreator());
                            }

                            switch (valueCap.UsagePage, valueCap.Usage){
                                case (0x0D, 0x51): // Contact ID
                                    toLog += $"ID{(int) value} ";
                                    creators[contactIndex].ContactId = (int) value;
                                    break;

                                case (0x01, 0x30): // X
                                    toLog += "X ";
                                    creators[contactIndex].X = (int) value;
                                    break;

                                case (0x01, 0x31): // Y
                                    toLog += "Y ";
                                    creators[contactIndex].Y = (int) value;
                                    break;
                                default:
                                    toLog += $"U{valueCap.UsagePage}/{valueCap.Usage} ";
                                    break;
                            }

                            break;
                    }
                }

                creators.ForEach(creator => {
                    if((contactCount == 0 || contacts.Count < contactCount) && creator.TryCreate(out var contact)){
                        contacts.Add(contact);
                        creator.Clear();
                    }
                });
                if(contactCount != 0 && contacts.Count >= contactCount){
                    break;
                }
            }

            Logger.Log(toLog);

            // Update the timestamp for this device since it just sent input
            if (availableDeviceInfos.ContainsKey(currentDevice))
            {
                deviceLastSeenTime[currentDevice] = DateTime.Now;
                deviceHasSentInput[currentDevice] = true; // Mark that this device has sent actual input
            }

            return (currentDevice, contacts, contactCount);
        } finally{
            Marshal.FreeHGlobal(rawHidRawDataPointer);
            Marshal.FreeHGlobal(preparsedDataPointer);
        }
    }

    #region Win32

    [DllImport("User32", SetLastError = true)]
    private static extern uint GetRawInputDeviceList(
        [Out] RAWINPUTDEVICELIST[] pRawInputDeviceList,
        ref uint puiNumDevices,
        uint cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTDEVICELIST {
        public readonly IntPtr hDevice;
        public readonly uint dwType; // RIM_TYPEMOUSE or RIM_TYPEKEYBOARD or RIM_TYPEHID
    }

    private const uint RIM_TYPEMOUSE = 0;
    private const uint RIM_TYPEKEYBOARD = 1;
    private const uint RIM_TYPEHID = 2;

    [DllImport("User32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterRawInputDevices(
        RAWINPUTDEVICE[] pRawInputDevices,
        uint uiNumDevices,
        uint cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTDEVICE {
        public ushort usUsagePage;
        public ushort usUsage;
        public uint dwFlags; // RIDEV_INPUTSINK
        public IntPtr hwndTarget;
    }

    private const uint RIDEV_INPUTSINK = 0x00000100;

    [DllImport("User32.dll", SetLastError = true)]
    private static extern uint GetRawInputData(
        IntPtr hRawInput, // lParam in WM_INPUT
        uint uiCommand, // RID_HEADER
        IntPtr pData,
        ref uint pcbSize,
        uint cbSizeHeader);

    private const uint RID_INPUT = 0x10000003;

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUT {
        public readonly RAWINPUTHEADER Header;
        public readonly RAWHID Hid;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTHEADER {
        public readonly uint dwType; // RIM_TYPEMOUSE or RIM_TYPEKEYBOARD or RIM_TYPEHID
        public readonly uint dwSize;
        public readonly IntPtr hDevice;
        public readonly IntPtr wParam; // wParam in WM_INPUT
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWHID {
        public readonly uint dwSizeHid;
        public readonly uint dwCount;
        public readonly IntPtr bRawData; // This is not for use.
    }

    [DllImport("User32.dll", SetLastError = true)]
    private static extern uint GetRawInputDeviceInfo(
        IntPtr hDevice, // hDevice by RAWINPUTHEADER
        uint uiCommand, // RIDI_PREPARSEDDATA
        IntPtr pData,
        ref uint pcbSize);

    [DllImport("User32.dll", SetLastError = true)]
    private static extern uint GetRawInputDeviceInfo(
        IntPtr hDevice, // hDevice by RAWINPUTDEVICELIST
        uint uiCommand, // RIDI_DEVICEINFO
        ref RID_DEVICE_INFO pData,
        ref uint pcbSize);

    private const uint RIDI_PREPARSEDDATA = 0x20000005;
    private const uint RIDI_DEVICEINFO = 0x2000000b;
    private const uint RIDI_DEVICENAME = 0x20000007;

    [StructLayout(LayoutKind.Sequential)]
    internal struct RID_DEVICE_INFO {
        public uint cbSize; // This is determined to accommodate RID_DEVICE_INFO_KEYBOARD.
        public readonly uint dwType;
        public readonly RID_DEVICE_INFO_HID hid;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RID_DEVICE_INFO_HID {
        public readonly uint dwVendorId;
        public readonly uint dwProductId;
        public readonly uint dwVersionNumber;
        public readonly ushort usUsagePage;
        public readonly ushort usUsage;
    }

    [DllImport("Hid.dll", SetLastError = true)]
    private static extern uint HidP_GetCaps(
        IntPtr PreparsedData,
        out HIDP_CAPS Capabilities);


    [DllImport("hid.dll", SetLastError = true)]
    private static extern uint HidP_GetButtonCaps(
        HIDP_REPORT_TYPE reportType,
        [Out] HIDP_BUTTON_CAPS[] buttonCaps,
        ref ushort buttonCapsLength,
        IntPtr preparsedData
    );

    [DllImport("hid.dll", SetLastError = true)]
    private static extern uint HidP_GetUsages(
        HIDP_REPORT_TYPE reportType,
        ushort usagePage,
        ushort linkCollection,
        [Out] uint[] usageList,
        ref uint usageLength,
        IntPtr preparsedData,
        byte[] report,
        uint reportLength
    );


    private const uint HIDP_STATUS_SUCCESS = 0x00110000;

    [StructLayout(LayoutKind.Sequential)]
    private struct HIDP_CAPS {
        public readonly ushort Usage;
        public readonly ushort UsagePage;
        public readonly ushort InputReportByteLength;
        public readonly ushort OutputReportByteLength;
        public readonly ushort FeatureReportByteLength;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        public readonly ushort[] Reserved;

        public readonly ushort NumberLinkCollectionNodes;
        public readonly ushort NumberInputButtonCaps;
        public readonly ushort NumberInputValueCaps;
        public readonly ushort NumberInputDataIndices;
        public readonly ushort NumberOutputButtonCaps;
        public readonly ushort NumberOutputValueCaps;
        public readonly ushort NumberOutputDataIndices;
        public readonly ushort NumberFeatureButtonCaps;
        public readonly ushort NumberFeatureValueCaps;
        public readonly ushort NumberFeatureDataIndices;
    }

    [DllImport("Hid.dll", CharSet = CharSet.Auto)]
    private static extern uint HidP_GetValueCaps(
        HIDP_REPORT_TYPE ReportType,
        [Out] HIDP_VALUE_CAPS[] ValueCaps,
        ref ushort ValueCapsLength,
        IntPtr PreparsedData);

    private enum HIDP_REPORT_TYPE {
        HidP_Input,
        HidP_Output,
        HidP_Feature
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HIDP_VALUE_CAPS {
        public readonly ushort UsagePage;
        public readonly byte ReportID;

        [MarshalAs(UnmanagedType.U1)] public readonly bool IsAlias;

        public readonly ushort BitField;
        public readonly ushort LinkCollection;
        public readonly ushort LinkUsage;
        public readonly ushort LinkUsagePage;

        [MarshalAs(UnmanagedType.U1)] public readonly bool IsRange;
        [MarshalAs(UnmanagedType.U1)] public readonly bool IsStringRange;
        [MarshalAs(UnmanagedType.U1)] public readonly bool IsDesignatorRange;
        [MarshalAs(UnmanagedType.U1)] public readonly bool IsAbsolute;
        [MarshalAs(UnmanagedType.U1)] public readonly bool HasNull;

        public readonly byte Reserved;
        public readonly ushort BitSize;
        public readonly ushort ReportCount;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public readonly ushort[] Reserved2;

        public readonly uint UnitsExp;
        public readonly uint Units;
        public readonly int LogicalMin;
        public readonly int LogicalMax;
        public readonly int PhysicalMin;
        public readonly int PhysicalMax;

        // Range
        public readonly ushort UsageMin;
        public readonly ushort UsageMax;
        public readonly ushort StringMin;
        public readonly ushort StringMax;
        public readonly ushort DesignatorMin;
        public readonly ushort DesignatorMax;
        public readonly ushort DataIndexMin;
        public readonly ushort DataIndexMax;

        // NotRange
        public ushort Usage => UsageMin;

        // ushort Reserved1;
        public ushort StringIndex => StringMin;

        // ushort Reserved2;
        public ushort DesignatorIndex => DesignatorMin;

        // ushort Reserved3;
        public ushort DataIndex => DataIndexMin;
        // ushort Reserved4;
    }

    [DllImport("Hid.dll", CharSet = CharSet.Auto)]
    private static extern uint HidP_GetUsageValue(
        HIDP_REPORT_TYPE ReportType,
        ushort UsagePage,
        ushort LinkCollection,
        ushort Usage,
        out uint UsageValue,
        IntPtr PreparsedData,
        IntPtr Report,
        uint ReportLength);

    #endregion
}

[StructLayout(LayoutKind.Sequential)]
public struct HIDP_BUTTON_CAPS
{
    public ushort UsagePage;
    public byte ReportID;
    public bool IsAlias;

    public ushort BitField;
    public ushort LinkCollection;
    public ushort LinkUsage;
    public ushort LinkUsagePage;

    public bool IsRange;
    public bool IsStringRange;
    public bool IsDesignatorRange;
    public bool IsAbsolute;

    [StructLayout(LayoutKind.Explicit)]
    public struct RangeOrNotRange
    {
        [FieldOffset(0)]
        public ushort UsageMin;
        [FieldOffset(2)]
        public ushort UsageMax;
        [FieldOffset(4)]
        public ushort StringMin;
        [FieldOffset(6)]
        public ushort StringMax;
        [FieldOffset(8)]
        public ushort DesignatorMin;
        [FieldOffset(10)]
        public ushort DesignatorMax;
        [FieldOffset(12)]
        public ushort DataIndexMin;
        [FieldOffset(14)]
        public ushort DataIndexMax;

        [FieldOffset(0)]
        public ushort Usage;
        [FieldOffset(2)]
        public ushort Reserved1;
        [FieldOffset(4)]
        public ushort StringIndex;
        [FieldOffset(6)]
        public ushort Reserved2;
        [FieldOffset(8)]
        public ushort DesignatorIndex;
        [FieldOffset(10)]
        public ushort Reserved3;
        [FieldOffset(12)]
        public ushort DataIndex;
        [FieldOffset(14)]
        public ushort Reserved4;
    }

    public RangeOrNotRange Range;
}
