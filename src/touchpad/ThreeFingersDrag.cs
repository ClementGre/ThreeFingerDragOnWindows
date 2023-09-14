using System;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using Microsoft.Win32;
using ThreeFingersDragOnWindows.utils;

namespace ThreeFingersDragOnWindows.touchpad;

public class ThreeFingersDrag {

    public const int RELEASE_FINGERS_THRESHOLD_MS = 30; // Windows Precision Touchpad sends contacts about every 10ms

    private readonly FingersCounter _fingersCounter = new();
    private readonly Timer _dragEndTimer = new();
    private bool _isDragging;
    private long _dragStartCtms = 0;


    public ThreeFingersDrag(){
        _dragEndTimer.AutoReset = false;
        _dragEndTimer.Elapsed += OnTimerElapsed;
    }

    public void OnTouchpadContact(TouchpadContact[] oldContacts, TouchpadContact[] contacts, long elapsed){
        Debug.WriteLine("\nTFD: " + string.Join(", ", oldContacts.Select(c => c.ToString())) + " | " + string.Join(", ", contacts.Select(c => c.ToString())) + " | " + elapsed);
        bool areContactsIdsCommons = FingersCounter.AreContactsIdsCommons(oldContacts, contacts);

        (int longestDistId, Point longestDistDelta, float longestDist2D) = DistanceManager.GetLongestDist2D(oldContacts, contacts, areContactsIdsCommons, elapsed);
        (int fingersCount, int movingFingersCount) = _fingersCounter.CountMovingFingers(oldContacts, contacts, areContactsIdsCommons, longestDist2D);

        Debug.WriteLine("    fingers: " + fingersCount + ", moving: " + movingFingersCount + ", dist: " + longestDist2D);

        if(movingFingersCount == 3 && !_isDragging){
            // Start dragging
            _isDragging = true;
            _dragStartCtms = Ctms();
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
            _isDragging = false;
            Debug.WriteLine("    STOP DRAG, Left click up");
            MouseOperations.MouseClick(MouseOperations.MOUSEEVENTF_LEFTUP);
        }

    }

    private void OnTimerElapsed(object source, ElapsedEventArgs e){
        if(_isDragging){
            _isDragging = false;
            Debug.WriteLine("    STOP DRAG FROM TIMER, Left click up");
            MouseOperations.MouseClick(MouseOperations.MOUSEEVENTF_LEFTUP);
        }
    }

    private int GetReleaseDelay(){
        // Delay after which the click is released if no input is detected
        return App.SettingsData.AllowReleaseAndRestart ? Math.Max(App.SettingsData.ReleaseDelay, RELEASE_FINGERS_THRESHOLD_MS) : RELEASE_FINGERS_THRESHOLD_MS;
    }

    private long Ctms(){
        return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
    }

}