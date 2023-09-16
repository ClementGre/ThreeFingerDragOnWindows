using System;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using ThreeFingersDragEngine.utils;

namespace ThreeFingersDragEngine.touchpad;

public class ThreeFingersDrag {

    public const int RELEASE_FINGERS_THRESHOLD_MS = 30; // Windows Precision Touchpad sends contacts about every 10ms

    private readonly DistanceManager _distanceManager = new();
    private readonly FingersCounter _fingersCounter = new();
    private readonly System.Timers.Timer _dragEndTimer = new();
    private bool _isDragging;
    
    public ThreeFingersDrag(){
        _dragEndTimer.AutoReset = false;
        _dragEndTimer.Elapsed += OnTimerElapsed;
    }

    public void OnTouchpadContact(TouchpadContact[] oldContacts, TouchpadContact[] contacts, long elapsed){
        Debug.WriteLine("\nTFD: " + string.Join(", ", oldContacts.Select(c => c.ToString())) + " | " + string.Join(", ", contacts.Select(c => c.ToString())) + " | " + elapsed);
        bool areContactsIdsCommons = FingersCounter.AreContactsIdsCommons(oldContacts, contacts);

        (_, Point longestDistDelta, float longestDist2D) = _distanceManager.GetLongestDist2D(oldContacts, contacts, elapsed);
        (int fingersCount, int movingFingersCount) = _fingersCounter.CountMovingFingers(contacts, areContactsIdsCommons, longestDist2D);

        Debug.WriteLine("    fingers: " + fingersCount + ", moving: " + movingFingersCount + ", dist: " + longestDist2D);

        if(movingFingersCount == 3 && !_isDragging){
            // Start dragging
            _isDragging = true;
            Debug.WriteLine("    START DRAG, Left click down");
            MouseOperations.MouseClick(MouseOperations.MOUSEEVENTF_LEFTDOWN);

        } else if(fingersCount >= 3 && areContactsIdsCommons && _isDragging){
            // Dragging
            if(App.SettingsData.ThreeFingersMove){
                Debug.WriteLine("    MOVING, (x, y) = (" + longestDistDelta.x + ", " + longestDistDelta.y + ")");

                Point delta = DistanceManager.ApplySpeedAndAcc(longestDistDelta, longestDist2D, (int) elapsed);
                MouseOperations.ShiftCursorPosition(delta.x, delta.y);

                _dragEndTimer.Stop();
                _dragEndTimer.Interval = GetReleaseDelay();
                _dragEndTimer.Start();
            }

        } else if(movingFingersCount < 3 && _isDragging){
            // Stop dragging
            Debug.WriteLine("    STOP DRAG, Left click up");
            StopDrag();
        }
    }

    private void OnTimerElapsed(object source, ElapsedEventArgs e){
        if(_isDragging){
            Debug.WriteLine("    STOP DRAG FROM TIMER, Left click up");
            StopDrag();
        }
    }

    private void StopDrag(){
        _isDragging = false;
        MouseOperations.MouseClick(MouseOperations.MOUSEEVENTF_LEFTUP);
    }

    private int GetReleaseDelay(){
        // Delay after which the click is released if no input is detected
        return App.SettingsData.AllowReleaseAndRestart ? Math.Max(App.SettingsData.ReleaseDelay, RELEASE_FINGERS_THRESHOLD_MS) : RELEASE_FINGERS_THRESHOLD_MS;
    }

}