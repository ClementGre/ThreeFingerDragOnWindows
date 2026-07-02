using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using ThreeFingerDragEngine.utils;
using ThreeFingerDragOnWindows.settings;
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

    public void OnTouchpadContact(IntPtr currentDevice, TouchpadContact[] oldContacts, TouchpadContact[] contacts, long elapsed){
        var deviceInfo = TouchpadHelper.GetDeivceInfo(currentDevice);
        bool hasFingersReleased = elapsed > RELEASE_FINGERS_THRESHOLD_MS;
        Logger.Log("TFD: " + string.Join(", ", oldContacts.Select(c => c.ToString())) + " | " +
                   string.Join(", ", contacts.Select(c => c.ToString())) + " | " + elapsed);

        // Outlier rejection: when exactly 4 fingers are present and the max-distance threshold is
        // set, check if one finger is an accidental touch (e.g. a thumb while typing) far from a
        // compact trio. If so, drop the outlier so the remaining 3 fingers can drive the drag.
        // oldContacts and contacts are filtered with the same rule so AreContactsIdsCommons keeps
        // returning true (lengths stay equal and the same physical finger is dropped on both sides).
        if(oldContacts.Length == 4 && contacts.Length == 4 &&
           App.SettingsData.ThreeFingerDragMaxFingersDistance > 0){
            oldContacts = TryRejectOutlier(oldContacts, App.SettingsData.ThreeFingerDragMaxFingersDistance, out _);
            contacts = TryRejectOutlier(contacts, App.SettingsData.ThreeFingerDragMaxFingersDistance, out bool rejected);
            if(rejected){
                Logger.Log("    rejected 1 outlier finger (1+3 -> 3)");
            }
        }

        bool areContactsIdsCommons = FingerCounter.AreContactsIdsCommons(oldContacts, contacts);

        (_, Point longestDistDelta, float longestDist2D) =
            _distanceManager.GetLongestDist2D(oldContacts, contacts, hasFingersReleased);
        (int fingersCount, int shortDelayMovingFingersCount, int longDelayMovingFingersCount,
                int originalFingersCount) =
            _fingerCounter.CountMovingFingers(currentDevice, contacts, areContactsIdsCommons, longestDist2D, hasFingersReleased);

        // Max pairwise distance between fingers on the current frame. Used to reject drags started
        // by fingers spread far apart (e.g. a thumb resting on the left while typing + two fingers on the right).
        float maxPairDist = App.SettingsData.ThreeFingerDragMaxFingersDistance > 0
            ? GetMaxPairwiseDistance(contacts)
            : 0;
        bool areFingersTooFar = maxPairDist > 0 &&
                                maxPairDist > App.SettingsData.ThreeFingerDragMaxFingersDistance;

        Logger.Log("    fingers: " + fingersCount + ", original: " + originalFingersCount + ", moving: " +
                   shortDelayMovingFingersCount + "/" + longDelayMovingFingersCount + ", dist: " + longestDist2D +
                   ", maxPairDist: " + maxPairDist + (areFingersTooFar ? " (too far)" : ""));

        if(fingersCount >= 3 && areContactsIdsCommons && longDelayMovingFingersCount == 3 &&
           originalFingersCount == 3 && !areFingersTooFar && !_isDragging){
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
            if(App.SettingsData.ThreeFingerDeviceDragCursorConfigs.ContainsKey(deviceInfo.deviceId)
                && App.SettingsData.ThreeFingerDeviceDragCursorConfigs.GetValueOrDefault(deviceInfo.deviceId, new SettingsData.ThreeFingerDragConfig()).ThreeFingerDragCursorMove){
                if(App.SettingsData.ThreeFingerDragMaxFingerMoveDistance != 0 && longestDist2D > App.SettingsData.ThreeFingerDragMaxFingerMoveDistance){
                    Logger.Log("    DISCARDING MOVE, (x, y) = (" + longestDistDelta.x + ", " + longestDistDelta.y + ")");
                } else if(!longestDistDelta.IsNull()){
                    Point delta = DistanceManager.ApplySpeedAndAcc(currentDevice, longestDistDelta, (int)elapsed);
                    Logger.Log("    MOVING (avg), (x, y) = (" + longestDistDelta.x + ", " + longestDistDelta.y + ")");
                    if(App.SettingsData.ThreeFingerDragCursorAveraging > 1){
                        _averagingX += delta.x;
                        _averagingY += delta.y;
                        _averagingCount++;
                        if(_averagingCount >= App.SettingsData.ThreeFingerDragCursorAveraging){
                            Logger.Log("    MOVING (avg effectively), (x, y) = (" + longestDistDelta.x + ", " + longestDistDelta.y + ")");
                            MouseOperations.ShiftCursorPosition(_averagingX, _averagingY);
                            _averagingX = 0;
                            _averagingY = 0;
                            _averagingCount = 0;
                        }
                    } else{
                        Logger.Log("    MOVING, (x, y) = (" + longestDistDelta.x + ", " + longestDistDelta.y + ")");
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

    /// <summary>
    /// Returns the largest distance between any two contacts on the current frame.
    /// Used to detect fingers spread too far apart (e.g. an accidental thumb while typing).
    /// </summary>
    private static float GetMaxPairwiseDistance(TouchpadContact[] contacts){
        float max = 0;
        for(int i = 0; i < contacts.Length; i++){
            for(int j = i + 1; j < contacts.Length; j++){
                float d = contacts[i].GetDist2D(contacts[j]);
                if(d > max) max = d;
            }
        }
        return max;
    }

    /// <summary>
    /// When exactly 4 contacts are present, find the single outlier (the finger far from a
    /// compact trio) and return the remaining 3. Only rejects when the outlier is farther than
    /// <paramref name="threshold"/> from ALL of the other three, which themselves must all be
    /// within <paramref name="threshold"/> of each other (a true 1+3 split). A normal 4-finger
    /// gesture has no fully-compact trio, so it is left untouched.
    /// </summary>
    private static TouchpadContact[] TryRejectOutlier(TouchpadContact[] contacts, float threshold, out bool rejected){
        rejected = false;
        if(contacts.Length != 4) return contacts;

        for(int i = 0; i < 4; i++){
            // Build the trio of the other three contacts.
            var trio = new TouchpadContact[3];
            int t = 0;
            for(int j = 0; j < 4; j++) if(j != i) trio[t++] = contacts[j];

            // The trio must be compact (all pairwise distances <= threshold).
            if(GetMaxPairwiseDistance(trio) > threshold) continue;

            // i must be far from every member of the trio (> threshold).
            bool farFromAll = true;
            for(int j = 0; j < 4; j++){
                if(j == i) continue;
                if(contacts[i].GetDist2D(contacts[j]) <= threshold){
                    farFromAll = false;
                    break;
                }
            }
            if(farFromAll){
                rejected = true;
                return trio;
            }
        }
        return contacts;
    }
}
