﻿using System;
using System.Linq;
using System.Timers;
using ThreeFingerDragEngine.utils;
using ThreeFingerDragOnWindows.utils;

namespace ThreeFingerDragOnWindows.threefingerdrag;

public class ThreeFingerDrag{
    public const int RELEASE_FINGERS_THRESHOLD_MS = 40; // Windows Precision Touchpad sends contacts about every 10ms

    private readonly DistanceManager _distanceManager = new();
    private readonly FingerCounter _fingerCounter = new();
    private readonly Timer _dragEndTimer = new();
    private bool _isDragging;

    public ThreeFingerDrag(){
        _dragEndTimer.AutoReset = false;
        _dragEndTimer.Elapsed += OnTimerElapsed;
    }

    private float _averagingX = 0;
    private float _averagingY = 0;
    private int _averagingCount = 0;

    public void OnTouchpadContact(TouchpadContact[] oldContacts, TouchpadContact[] contacts, long elapsed){
        bool hasFingersReleased = elapsed > RELEASE_FINGERS_THRESHOLD_MS;
        Logger.Log("TFD: " + string.Join(", ", oldContacts.Select(c => c.ToString())) + " | " +
                   string.Join(", ", contacts.Select(c => c.ToString())) + " | " + elapsed);
        bool areContactsIdsCommons = FingerCounter.AreContactsIdsCommons(oldContacts, contacts);

        (_, Point longestDistDelta, float longestDist2D) =
            _distanceManager.GetLongestDist2D(oldContacts, contacts, hasFingersReleased);
        (int fingersCount, int shortDelayMovingFingersCount, int longDelayMovingFingersCount,
                int originalFingersCount) =
            _fingerCounter.CountMovingFingers(contacts, areContactsIdsCommons, longestDist2D, hasFingersReleased);

        Logger.Log("    fingers: " + fingersCount + ", original: " + originalFingersCount + ", moving: " +
                   shortDelayMovingFingersCount + "/" + longDelayMovingFingersCount + ", dist: " + longestDist2D);

        if(fingersCount >= 3 && areContactsIdsCommons && longDelayMovingFingersCount == 3 &&
           originalFingersCount == 3 && !_isDragging){
            // Start dragging
            _isDragging = true;
            Logger.Log("    START DRAG, click down");
            MouseOperations.ThreeFingersDragMouseDown();
        } else if(_isDragging &&
                  (shortDelayMovingFingersCount < 2 || (originalFingersCount != 3 && originalFingersCount >= 2))){
            // Stop dragging
            // Condition over originalFingersCount to catch cases where the drag has continued with only two or four fingers
            Logger.Log("    STOP DRAG, click up");
            StopDrag();
        } else if(fingersCount >= 2 && originalFingersCount == 3 && areContactsIdsCommons && _isDragging){
            // Dragging
            if(App.SettingsData.ThreeFingerDragCursorMove){
                Logger.Log("    MOVING, (x, y) = (" + longestDistDelta.x + ", " + longestDistDelta.y + ")");

                if(!longestDistDelta.IsNull()){
                    Point delta = DistanceManager.ApplySpeedAndAcc(longestDistDelta, (int)elapsed);
                    if(App.SettingsData.ThreeFingerDragCursorAveraging > 1){
                        _averagingX += delta.x;
                        _averagingY += delta.y;
                        _averagingCount++;
                        if(_averagingCount >= App.SettingsData.ThreeFingerDragCursorAveraging){
                            MouseOperations.ShiftCursorPosition(_averagingX, _averagingY);
                            _averagingX = 0;
                            _averagingY = 0;
                            _averagingCount = 0;
                        }
                    } else{
                        MouseOperations.ShiftCursorPosition(delta.x, delta.y);
                    }
                }

                _dragEndTimer.Stop();
                _dragEndTimer.Interval = GetReleaseDelay();
                _dragEndTimer.Start();
            }
        }
        Logger.Log("");
    }

    private void OnTimerElapsed(object source, ElapsedEventArgs e){
        if(_isDragging){
            Logger.Log("    STOP DRAG FROM TIMER, Left click up");
            Logger.Log("");
            StopDrag();
        }
    }

    private void StopDrag(){
        _isDragging = false;
        MouseOperations.ThreeFingersDragMouseUp();
    }

    private int GetReleaseDelay(){
        // Delay after which the click is released if no input is detected
        return App.SettingsData.ThreeFingerDragAllowReleaseAndRestart
            ? Math.Max(App.SettingsData.ThreeFingerDragReleaseDelay, RELEASE_FINGERS_THRESHOLD_MS)
            : RELEASE_FINGERS_THRESHOLD_MS;
    }
}
