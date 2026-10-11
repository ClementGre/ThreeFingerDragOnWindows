using System;
using System.Runtime.InteropServices;
using ThreeFingerDragOnWindows.settings;

namespace ThreeFingerDragOnWindows.utils;

public class MouseOperations {
    private const int INPUT_MOUSE = 0;
    private const int SM_SWAPBUTTON = 23;
    private const int MOUSEEVENTF_MOVE = 0x0001;
    public const int MOUSEEVENTF_LEFTDOWN = 0x0002;
    public const int MOUSEEVENTF_LEFTUP = 0x0004;
    public const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
    public const int MOUSEEVENTF_RIGHTUP = 0x0010;
    public const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    public const int MOUSEEVENTF_MIDDLEUP = 0x0040;

    // moving the cursor does not work with floating point values
    // decimal parts are kept and then added to be taken in account
    private static float _decimalX;
    private static float _decimalY;
    private static int? _threeFingersDragMouseUpEventFlag;


    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out IntMousePoint lpFIntMousePoint);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);


    public static IntMousePoint GetCursorPosition(){
        IntMousePoint currentIntMousePoint;
        var gotPoint = GetCursorPos(out currentIntMousePoint);
        if(!gotPoint) currentIntMousePoint = new IntMousePoint(0, 0);
        return currentIntMousePoint;
    }

    public static void ShiftCursorPosition(float x, float y){
        var intX = (int) (x + _decimalX);
        var intY = (int) (y + _decimalY);
        _decimalX = x + _decimalX - intX;
        _decimalY = y + _decimalY - intY;

        MoveMouse(intX, intY);
    }

    public static void SetCursorPosition(float x, float y){
        var intX = (int) (x + _decimalX);
        var intY = (int) (y + _decimalY);
        _decimalX = x + _decimalX - intX;
        _decimalY = y + _decimalY - intY;
        var point = GetCursorPosition();

        MoveMouse(intX - point.x, intY - point.y);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);

    private static void MoveMouse(int dx, int dy){
        var input = new Input[1];
        input[0].type = INPUT_MOUSE;
        input[0].mi.dx = dx;
        input[0].mi.dy = dy;
        input[0].mi.dwFlags = MOUSEEVENTF_MOVE;

        var result = SendInput(1, input, Marshal.SizeOf(typeof(Input)));
        if(result == 0) Console.WriteLine("Failed to move the mouse. Error code: " + Marshal.GetLastWin32Error());
    }

    public static void MouseClick(int mouseEventFlag){
        var input = new Input[1];
        input[0].type = INPUT_MOUSE;
        input[0].mi.dwFlags = mouseEventFlag;

        var result = SendInput(1, input, Marshal.SizeOf(typeof(Input)));

        if(result == 0) Console.WriteLine("Failed to send mouse click. Error code: " + Marshal.GetLastWin32Error());
    }

    public static void ThreeFingersDragMouseDown(){
        (int? mouseDownEventFlag, int? mouseUpEventFlag) = GetThreeFingersDragMouseEventFlags(
            App.SettingsData.ThreeFingerDragButton, GetSystemMetrics(SM_SWAPBUTTON) != 0);
        _threeFingersDragMouseUpEventFlag = mouseUpEventFlag;

        if(mouseDownEventFlag.HasValue) MouseClick(mouseDownEventFlag.Value);
    }

    public static void ThreeFingersDragMouseUp(){
        int? mouseUpEventFlag = _threeFingersDragMouseUpEventFlag;
        _threeFingersDragMouseUpEventFlag = null;

        if(mouseUpEventFlag.HasValue) MouseClick(mouseUpEventFlag.Value);
    }

    internal static (int? mouseDownEventFlag, int? mouseUpEventFlag) GetThreeFingersDragMouseEventFlags(
        SettingsData.ThreeFingerDragButtonType buttonType, bool areButtonsSwapped){
        switch(buttonType){
            case SettingsData.ThreeFingerDragButtonType.PRIMARY:
                return areButtonsSwapped
                    ? (MOUSEEVENTF_RIGHTDOWN, MOUSEEVENTF_RIGHTUP)
                    : (MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP);
            case SettingsData.ThreeFingerDragButtonType.RIGHT:
                return (MOUSEEVENTF_RIGHTDOWN, MOUSEEVENTF_RIGHTUP);
            case SettingsData.ThreeFingerDragButtonType.MIDDLE:
                return (MOUSEEVENTF_MIDDLEDOWN, MOUSEEVENTF_MIDDLEUP);
            default:
                return (null, null);
        }
    }


    [StructLayout(LayoutKind.Sequential)]
    private struct Input {
        public int type;
        public MouseInput mi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput {
        public int dx;
        public int dy;
        public int mouseData;
        public int dwFlags;
        public int time;
        public IntPtr dwExtraInfo;
    }
}

public struct IntMousePoint {
    public int x;
    public int y;

    public IntMousePoint(int x, int y){
        this.x = x;
        this.y = y;
    }
}
