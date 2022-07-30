using System;
using System.Timers;
using System.Windows.Input;
using ThreeFingersDragOnWindows.src.utils;

namespace ThreeFingersDragOnWindows.src.touchpad;

public class ThreeFingersDrag {
    private const int ReleaseDelay = 20; // milliseconds
    private readonly Timer _dragEndTimer = new(ReleaseDelay);

    private bool _isDragging;
    private ThreeFingersPoints _lastPoints = new(0, 0, 0, 0, 0, 0);
    private long _lastThreeFingersContact = 0;

    public ThreeFingersDrag(){
        // Setup timer
        _dragEndTimer.Elapsed += (_, _) => CheckDragEnd();
        _dragEndTimer.AutoReset = false;
    }

    public void OnTouchpadContact(TouchpadContact[] contacts){
        ThreeFingersPoints points = new(contacts);

        if(contacts.Length == 3){

            if(!_isDragging){
                _isDragging = true;
                MouseOperations.MouseEvent(MouseOperations.MouseEventFlags.LeftDown);
            }else{
                if(App.Prefs.ThreeFingersMove){
                    var dist2d = points.GetLongestDist2D(_lastPoints);
                    float elapsed = _lastThreeFingersContact == 0 ? 0 : new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds() - _lastThreeFingersContact;

                    dist2d.Multiply(App.Prefs.MouseSpeed / 60);
                    
                    var mouseVelocity = (float) Math.Max(0.2, Math.Min(dist2d.Length() / elapsed, 20));
                    if(float.IsNaN(mouseVelocity) || float.IsInfinity(mouseVelocity)) mouseVelocity = 1;

                    var pointerVelocity = (float) (App.Prefs.MouseAcceleration/10 * Math.Pow(mouseVelocity, 2) + 0.4 * mouseVelocity);
                    pointerVelocity = (float) Math.Max(0.4, Math.Min(pointerVelocity, 1.6));
                    if(App.Prefs.MouseAcceleration == 0) pointerVelocity = 1;

                    dist2d.Multiply(pointerVelocity);

                    MouseOperations.ShiftCursorPosition(dist2d.x, dist2d.y);
                }
                
                _dragEndTimer.Stop();
                _dragEndTimer.Start();
            }
            _lastThreeFingersContact = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
        }
        else{
            if(_isDragging){
                _isDragging = false;
                MouseOperations.MouseEvent(MouseOperations.MouseEventFlags.LeftUp);
            }
        }

        _lastPoints = points;
    }

    private void CheckDragEnd(){
        if(_isDragging && new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds() - _lastThreeFingersContact >
           ReleaseDelay){
            _isDragging = false;
            MouseOperations.MouseEvent(MouseOperations.MouseEventFlags.LeftUp);
        }
    }
}