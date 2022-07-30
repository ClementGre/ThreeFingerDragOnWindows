using System;
using System.Collections.Generic;
using System.Timers;

namespace ThreeFingersDragOnWindows;

public class ThreeFingersDrag {
    private const int ReleaseDelay = 20; // milliseconds
    private readonly Timer _dragEndTimer = new(ReleaseDelay);

    private bool _isDragging;
    private MousePoint _lastLocation = new(0, 0);
    private long _lastThreeFingersContact;

    public ThreeFingersDrag(){

        // Setup timer
        _dragEndTimer.Elapsed += (_, _) => CheckDragEnd();
        _dragEndTimer.AutoReset = false;
    }

    public void OnTouchpadContact(TouchpadContact[] contacts){
        var point = Utils.AverageCoordinate(contacts);

        if(contacts.Length == 3){
            _lastThreeFingersContact = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

            if(!_isDragging){
                _isDragging = true;
                MouseOperations.MouseEvent(MouseOperations.MouseEventFlags.LeftDown);
            }else{
                // Mouse do not move automatically on three fingers drag
                MouseOperations.ShiftCursorPosition((point.x - _lastLocation.x) / App.Prefs.MouseSpeed,
                    (point.y - _lastLocation.y) / App.Prefs.MouseSpeed);

                _dragEndTimer.Stop();
                _dragEndTimer.Start();
            }
        }
        else{
            if(_isDragging){
                _isDragging = false;
                MouseOperations.MouseEvent(MouseOperations.MouseEventFlags.LeftUp);
            }
        }

        _lastLocation = point;
    }

    private void CheckDragEnd(){
        if(_isDragging && new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds() - _lastThreeFingersContact >
           ReleaseDelay){
            _isDragging = false;
            MouseOperations.MouseEvent(MouseOperations.MouseEventFlags.LeftUp);
        }
    }
}

