using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Xml;
using Microsoft.VisualBasic.Devices;


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
        Absolute = 0x8000,
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

    public static IntMousePoint GetCursorPosition(){
        IntMousePoint currentIntMousePoint;
        var gotPoint = GetCursorPos(out currentIntMousePoint);
        if(!gotPoint) currentIntMousePoint = new IntMousePoint(0, 0);
        return currentIntMousePoint;
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
        
        Move(intX, intY);
    }
    public static void SetCursorPosition(float x, float y){
        var intX = (int) (x + _decimalX);
        var intY = (int) (y + _decimalY);
        _decimalX = (x + _decimalX) - intX;
        _decimalY = (y + _decimalY) - intY;
        var point = GetCursorPosition();
        
        Move(intX - point.x, intY - point.y);
    }

    public static void Move(int dx, int dy) {
        MouseEvent(MouseOperations.MouseEventFlags.Move, dx, dy);
    }

    public static void LeftClick(bool down) {
        var position = GetCursorPosition();
        MouseEvent(down ? MouseOperations.MouseEventFlags.LeftDown : MouseOperations.MouseEventFlags.LeftUp, position.x, position.y);
    }
    public static void RightClick(bool down) {
        var position = GetCursorPosition();
        MouseEvent(down ? MouseOperations.MouseEventFlags.RightDown : MouseOperations.MouseEventFlags.RightUp, position.x, position.y);
    }
    
    
    
    public static void MouseEvent(MouseEventFlags value, int dx, int dy){
        mouse_event
            ((int) value,
                dx,
                dy,
                0,
                0)
            ;
    }
}