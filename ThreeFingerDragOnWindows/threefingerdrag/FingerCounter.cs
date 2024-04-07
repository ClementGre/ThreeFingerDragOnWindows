using System.Diagnostics;
using ThreeFingerDragEngine.utils;
using ThreeFingerDragOnWindows.utils;

namespace ThreeFingerDragOnWindows.threefingerdrag;

public class FingerCounter {


    private static readonly float FINGERS_MOVE_THRESHOLD_SHORT = 10;
    private static readonly float FINGERS_MOVE_THRESHOLD_LONG = 100;
    
    private int _originalFingersCount; // Number of original fingers on the touchpad after the short delay. This is updated only when contacts list length is <= 1. 

    private int _shortDelayFingersCount;
    private float _shortDelayFingersMove;
    
    private int _longDelayFingersCount;
    private float _longDelayFingersMove;
    
    /// <summary>
    /// Count the number of fingers that are moving.
    /// </summary>
    /// <param name="newContacts">new contacts list</param>
    /// <param name="areContactsIdsCommons">weather if contacts have changed since last call</param>
    /// <param name="longestDist2D">longest distance calculated by DistanceManager</param>
    /// <param name="hasFingersReleased">Weather if fingers has been released and replaced on the touchpad</param>
    /// <returns>
    /// fingersCount : real number of fingers on the touchpad, or 0 if contacts changed
    /// shortDelayMovingFingersCount : number of fingers that are on the touchpad and that have led to a moving distance higher than FINGERS_MOVE_THRESHOLD_SHORT
    ///     Used to determine what is the real number of fingers on the touchpad when contacts changed
    /// longDelayMovingFingersCount : number of fingers that are on the touchpad and that have led to a moving distance higher than FINGERS_MOVE_THRESHOLD_LONG
    ///     Used to determine if the user has really started to drag
    /// originalFingersCount : number of original fingers on the touchpad after the short delay.
    ///     This is updated only when contacts list length is &lt;= 1 or when contacts have been released for more than RELEASE_FINGERS_THRESHOLD_MS ms.
    ///     Used to determine if the user has originally started to scroll, drag, or desktop swipe. When moving with a single finger, this variable is reset.
    /// </returns>
    public (int, int, int, int) CountMovingFingers(TouchpadContact[] newContacts, bool areContactsIdsCommons, float longestDist2D, bool hasFingersReleased){

        if(!areContactsIdsCommons && (newContacts.Length <= 1 || hasFingersReleased)){
            _originalFingersCount = 0;
        }
        if(!areContactsIdsCommons || hasFingersReleased){
            _shortDelayFingersMove = 0;
            _longDelayFingersMove = 0;
            return (0, _shortDelayFingersCount, _longDelayFingersCount, _originalFingersCount);
        }

        longestDist2D = DistanceManager.ApplySpeed(longestDist2D);
        if(longestDist2D >= 1){ // Do not take in account shorter distances
            _shortDelayFingersMove += longestDist2D;
            _longDelayFingersMove += longestDist2D;
        }
        
        if(_shortDelayFingersMove >= FINGERS_MOVE_THRESHOLD_SHORT){
            _shortDelayFingersCount = newContacts.Length;
            _shortDelayFingersMove = 0;
            
        }
        if(_longDelayFingersMove > FINGERS_MOVE_THRESHOLD_LONG){
            _longDelayFingersCount = newContacts.Length;
            _longDelayFingersMove = 0;
            if(_originalFingersCount <= 1){
                _originalFingersCount = newContacts.Length;
            }
        }

        return (newContacts.Length, _shortDelayFingersCount, _longDelayFingersCount, _originalFingersCount);
    }

    public static bool AreContactsIdsCommons(TouchpadContact[] oldContacts, TouchpadContact[] newContacts){
        if(oldContacts.Length != newContacts.Length) return false;

        // Counting common contacts Ids
        int count = 0;
        foreach(var newContact in newContacts){
            foreach(var oldContact in oldContacts){
                if(newContact.ContactId != oldContact.ContactId) continue;
                count++;
                break;
            }
        }

        return count == newContacts.Length;
    }

}