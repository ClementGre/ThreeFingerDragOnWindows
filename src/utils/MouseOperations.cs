using System;
using System.Drawing;
using System.Runtime.InteropServices;


// From https://stackoverflow.com/questions/2416748/how-do-you-simulate-mouse-click-in-c

namespace ThreeFingersDragOnWindows.src.utils;

public class MouseOperations {
    [Flags]
    public enum MouseEventFlags {
        LeftDown = 0x00000002,
        LeftUp = 0x00000004,
        MiddleDown = 0x00000020,
        MiddleUp = 0x00000040,
        Move = 0x00000001,
        Absolute = 0x00008000,
        RightDown = 0x00000008,
        RightUp = 0x00000010
    }

    [DllImport("user32.dll", EntryPoint = "SetCursorPos")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out IntMousePoint lpFIntMousePoint);

    [DllImport("user32.dll")]
    private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

    public static void SetCursorPosition(int x, int y){
        SetCursorPos(x, y);
    }

    public static void SetCursorPosition(MousePoint point){
        SetCursorPos((int) point.x, (int) point.y);
    }

    // moving the cursor does not work with floating point values
    // decimal parts are kept and then added to be taken in account
    private static float _decimalX;
    private static float _decimalY;
    
    public static void ShiftCursorPosition(float x, float y){
        var intX = (int) (x + _decimalX);
        var intY = (int) (y + _decimalY);
        _decimalX = (x + _decimalX) - intX;
        _decimalY = (y + _decimalY) - intY;
        var point = GetCursorPosition();

        SetCursorPos(point.x + intX, point.y + intY);
    }

    public static IntMousePoint GetCursorPosition(){
        IntMousePoint currentIntMousePoint;
        var gotPoint = GetCursorPos(out currentIntMousePoint);
        if(!gotPoint) currentIntMousePoint = new IntMousePoint(0, 0);
        return currentIntMousePoint;
    }

    public static void MouseEvent(MouseEventFlags value){
        var position = GetCursorPosition();

        mouse_event
            ((int) value,
                position.x,
                position.y,
                0,
                0)
            ;
    }
}