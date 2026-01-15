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
                touchpadDevice.deviceId = ComputeMD5(Marshal.PtrToStringAnsi(ptr));
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
            if (!availableDeviceInfos.ContainsKey(hwnd))
            {
                availableDeviceInfos[hwnd] = GetDeviceInfoFromHid(hwnd);
                availableDeviceInfos[hwnd].vendorId = deviceInfo.hid.dwVendorId.ToString();
                availableDeviceInfos[hwnd].productId = deviceInfo.hid.dwProductId.ToString();
            }
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

    public static List<TouchpadDeviceInfo> GetAllDeivceInfos()
    {
        return availableDeviceInfos.Values.ToList();
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
