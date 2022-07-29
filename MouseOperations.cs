using System;
using System.Runtime.InteropServices;


// From https://stackoverflow.com/questions/2416748/how-do-you-simulate-mouse-click-in-c

namespace ThreeFingersDragOnWindows;

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
    private static extern bool GetCursorPos(out MousePoint lpMousePoint);

    [DllImport("user32.dll")]
    private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

    public static void SetCursorPosition(int x, int y){
        SetCursorPos(x, y);
    }

    public static void SetCursorPosition(MousePoint point){
        SetCursorPos(point.x, point.y);
    }

    public static void ShiftCursorPosition(int x, int y){
        var point = GetCursorPosition();
        SetCursorPos(point.x + x, point.y + y);
    }

    public static MousePoint GetCursorPosition(){
        MousePoint currentMousePoint;
        var gotPoint = GetCursorPos(out currentMousePoint);
        if(!gotPoint) currentMousePoint = new MousePoint(0, 0);
        return currentMousePoint;
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