using ThreeFingersDragOnWindows.utils;

namespace ThreeFingersDragOnWindows.touchpad;

public class FingersCounter {


    private static readonly float FINGERS_MOVE_THRESHOLD = DistanceManager.ApplySpeed(50);

    private int _fingersCount;
    private float _lastFingersMove;

    public (int, int) CountMovingFingers(TouchpadContact[] oldContacts, TouchpadContact[] newContacts, bool areContactsIdsCommons, float longestDist2D){

        if(!areContactsIdsCommons){
            _lastFingersMove = 0;
            return (0, _fingersCount);
        }

        _lastFingersMove += longestDist2D;
        if(_lastFingersMove > FINGERS_MOVE_THRESHOLD){
            _fingersCount = newContacts.Length;
            _lastFingersMove = 0;
        }

        return (newContacts.Length, _fingersCount);
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