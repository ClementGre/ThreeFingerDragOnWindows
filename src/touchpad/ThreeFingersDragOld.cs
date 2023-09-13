using System;
using System.Diagnostics;
using System.Timers;
using ThreeFingersDragOnWindows.utils;

namespace ThreeFingersDragOnWindows.touchpad;

public class ThreeFingersDragOld {
    /*private readonly Timer _dragEndTimer = new(50);
    private readonly Timer _oneFingerTimer = new(20);
    private long _firstOneFingerContact;

    private ThreeFingersPoints _firstOneFingerPoints = ThreeFingersPoints.Empty;
    private long _firstThreeFingersContact;

    private bool _isDragging;
    private bool _isFirstOneFingerInput;
    private bool _isFirstThreeFingersInput;
    private IntMousePoint _lastDragMousePoint = new(0, 0);
    private long _lastOneFingerContact;
    private long _lastThreeFingersContact;


    private ThreeFingersPoints _lastThreeFingersPoints = ThreeFingersPoints.Empty;

    public ThreeFingersDragOld(){
        // Setup timer
        _dragEndTimer.Elapsed += (_, _) => CheckDragEnd();
        _oneFingerTimer.Elapsed += (_, _) => {
            if(Ctms() - _lastThreeFingersContact > 250) stopDrag();
        };
        _dragEndTimer.AutoReset = false;
        _oneFingerTimer.AutoReset = false;
    }

    public void OnTouchpadContact(TouchpadContact[] contacts){
        ThreeFingersPoints points = new(contacts);

        if(contacts.Length == 3){
            _isFirstOneFingerInput = true;
            if(_isFirstThreeFingersInput){
                _firstThreeFingersContact = Ctms();
                _isFirstThreeFingersInput = false;
            }

            if(!_isDragging){
                if(Ctms() - _firstThreeFingersContact > 100){
                    //r fin When placing fougers, a three fingers input can be detected for less than 100ms
                    //Debug.WriteLine("ThreeFingersDrag Start drag");
                    _isDragging = true;
                    MouseOperations.MouseClick(MouseOperations.MOUSEEVENTF_LEFTDOWN);
                }
            } else{
                var dist2d = points.GetLongestDist2D(_lastThreeFingersPoints);
                //var dist2d = new MousePoint(_lastThreeFingersPoints.x1 - points.x1, _lastThreeFingersPoints.y1 - points.y1);


                if(App.SettingsData.ThreeFingersMove // Three Fingers drag enabled in preferences
                   && _lastThreeFingersPoints !=
                   ThreeFingersPoints.Empty) // Last contact is a three fingers drag contact)
                {
                    float elapsed = _lastThreeFingersContact == 0 ? 0 : Ctms() - _lastThreeFingersContact;

                    if(elapsed < 100){
                        // Fingers can be released and replaced without catching any one/two finger contact. This makes sure the fingers haven't been replaced on the touchpad
                        // Apply the Mouse Speed preference
                        dist2d.Multiply(App.SettingsData.MouseSpeed / 60);

                        // Calculate the mouse velocity
                        var mouseVelocity = (float) Math.Max(0.2, Math.Min(dist2d.Length() / elapsed, 20));
                        if(float.IsNaN(mouseVelocity) || float.IsInfinity(mouseVelocity)) mouseVelocity = 1;

                        // Calculate the pointer velocity in function of the mouse velocity and the preference
                        var pointerVelocity =
                            (float) (App.SettingsData.MouseAcceleration / 10 * Math.Pow(mouseVelocity, 2) +
                                     0.4 * mouseVelocity);
                        pointerVelocity = (float) Math.Max(0.4, Math.Min(pointerVelocity, 1.6)); // Clamp
                        if(App.SettingsData.MouseAcceleration == 0) pointerVelocity = 1; // Disable acceleration
                        pointerVelocity = 1;

                        // Apply acceleration
                        // dist2d.Multiply(pointerVelocity);

                        //Debug.WriteLine("Elapsed: " + elapsed);
                        //Debug.WriteLine("Shift cursor position: " + dist2d.x + " " + dist2d.y);
                        MouseOperations.ShiftCursorPosition(dist2d.x, dist2d.y);
                    }
                }

                _lastDragMousePoint = MouseOperations.GetCursorPosition();
                _dragEndTimer.Stop();
                _dragEndTimer.Interval = GetReleaseDelay();
                _dragEndTimer.Start();
            }

            _lastThreeFingersContact = Ctms();
            _lastThreeFingersPoints = points;
        } else{
            //Debug.WriteLine("ThreeFingersDrag Not three fingers");
            _isFirstThreeFingersInput = true;
            if(!_isDragging){
                _isFirstOneFingerInput = true;
                return;
            }

            if(_isFirstOneFingerInput || points.Length != _firstOneFingerPoints.Length ||
               Ctms() - _lastOneFingerContact > 50){
                _isFirstOneFingerInput = false;
                _firstOneFingerContact = Ctms();
                _firstOneFingerPoints = points;
            }

            if(!App.SettingsData.AllowReleaseAndRestart) stopDrag();
            else if(_firstOneFingerPoints.GetLongestDist(points) > 30)
                // When RELEASING and then REPLACING the fingers, one finger or two can be detected and send some events.
                stopDrag();
            else
                MouseOperations.SetCursorPosition(_lastDragMousePoint.x, _lastDragMousePoint.y);

            _lastOneFingerContact = Ctms();
            _lastThreeFingersPoints = ThreeFingersPoints.Empty;
        }
    }

    private void CheckDragEnd(){
        // minus 15 to avoid bugs when the timer ends before the time elapsed
        if(_isDragging && Ctms() - _lastThreeFingersContact >= GetReleaseDelay() - 15) stopDrag();
    }

    private void stopDrag(){
        Debug.WriteLine("ThreeFingersDrag Stop drag");
        _isDragging = false;
        MouseOperations.MouseClick(MouseOperations.MOUSEEVENTF_LEFTUP);
    }

    private int GetReleaseDelay(){
        return App.SettingsData.AllowReleaseAndRestart ? Math.Max(App.SettingsData.ReleaseDelay, 50) : 50;
    }

    private long Ctms(){
        return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
    }

    private float MouseSpeedFactor(){
        return App.SettingsData.MouseSpeed / 30;
    }*/
}