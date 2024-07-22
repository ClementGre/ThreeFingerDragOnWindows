using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using ThreeFingerDragEngine.utils;

namespace ThreeFingerDragOnWindows.utils;

// From  emoacht/RawInput.Touchpad
internal static class TouchpadHelper{
    public const int WM_INPUT = 0x00FF;
    public const int WM_INPUT_DEVICE_CHANGE = 0x00FE;
    public const int RIM_INPUT = 0;
    public const int RIM_INPUTSINK = 1;

    public static bool Exists(){
        uint deviceListCount = 0;
        var rawInputDeviceListSize = (uint)Marshal.SizeOf<RAWINPUTDEVICELIST>();

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

        foreach(var device in devices.Where(x => x.dwType == RIM_TYPEHID)){
            uint deviceInfoSize = 0;

            if(GetRawInputDeviceInfo(
                   device.hDevice,
                   RIDI_DEVICEINFO,
                   IntPtr.Zero,
                   ref deviceInfoSize) != 0)
                continue;

            var deviceInfo = new RID_DEVICE_INFO{ cbSize = deviceInfoSize };

            if(GetRawInputDeviceInfo(
                   device.hDevice,
                   RIDI_DEVICEINFO,
                   ref deviceInfo,
                   ref deviceInfoSize) == unchecked((uint)-1))
                continue;

            if(deviceInfo.hid.usUsagePage == 0x000D &&
               deviceInfo.hid.usUsage == 0x0005)
                return true;
        }

        return false;
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

        return RegisterRawInputDevices(new[]{ device }, 1, (uint)Marshal.SizeOf<RAWINPUTDEVICE>());
    }

    public static TouchpadContact[] ParseInput(IntPtr lParam){
        // Get RAWINPUT.
        uint rawInputSize = 0;
        var rawInputHeaderSize = (uint) Marshal.SizeOf<RAWINPUTHEADER>();

        if(GetRawInputData(
               lParam,
               RID_INPUT,
               IntPtr.Zero,
               ref rawInputSize,
               rawInputHeaderSize) != 0)
            return null;

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
                return null;

            rawInput = Marshal.PtrToStructure<RAWINPUT>(rawInputPointer);

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
                return null;

            preparsedDataPointer = Marshal.AllocHGlobal((int)preparsedDataSize);

            if(GetRawInputDeviceInfo(
                   rawInput.Header.hDevice,
                   RIDI_PREPARSEDDATA,
                   preparsedDataPointer,
                   ref preparsedDataSize) != preparsedDataSize)
                return null;

            if(HidP_GetCaps(
                   preparsedDataPointer,
                   out var caps) != HIDP_STATUS_SUCCESS)
                return null;

            var valueCapsLength = caps.NumberInputValueCaps;
            var valueCaps = new HIDP_VALUE_CAPS[valueCapsLength];

            if(HidP_GetValueCaps(
                   HIDP_REPORT_TYPE.HidP_Input,
                   valueCaps,
                   ref valueCapsLength,
                   preparsedDataPointer) != HIDP_STATUS_SUCCESS)
                return null;

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
                    IntPtr rawHidRawDataPointerAdjusted = IntPtr.Add(rawHidRawDataPointer, (int) (rawInput.Hid.dwSizeHid * contactIndex));

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
                    switch (valueCap.LinkCollection){
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
                            }
                            break;
                    }
                }
                creators.ForEach(creator => {
                    if(contacts.Count < contactCount && creator.TryCreate(out var contact)){
                        contacts.Add(contact);
                        creator.Clear();
                    }
                });
                if(contacts.Count >= contactCount){
                    break;
                }
            }
            Logger.Log(toLog);

            return contacts.ToArray();
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
    private struct RAWINPUTDEVICELIST{
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
    private struct RAWINPUTDEVICE{
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
    private struct RAWINPUT{
        public readonly RAWINPUTHEADER Header;
        public readonly RAWHID Hid;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTHEADER{
        public readonly uint dwType; // RIM_TYPEMOUSE or RIM_TYPEKEYBOARD or RIM_TYPEHID
        public readonly uint dwSize;
        public readonly IntPtr hDevice;
        public readonly IntPtr wParam; // wParam in WM_INPUT
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWHID{
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

    [StructLayout(LayoutKind.Sequential)]
    private struct RID_DEVICE_INFO{
        public uint cbSize; // This is determined to accommodate RID_DEVICE_INFO_KEYBOARD.
        public readonly uint dwType;
        public readonly RID_DEVICE_INFO_HID hid;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RID_DEVICE_INFO_HID{
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

    private const uint HIDP_STATUS_SUCCESS = 0x00110000;

    [StructLayout(LayoutKind.Sequential)]
    private struct HIDP_CAPS{
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

    private enum HIDP_REPORT_TYPE{
        HidP_Input,
        HidP_Output,
        HidP_Feature
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HIDP_VALUE_CAPS{
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
