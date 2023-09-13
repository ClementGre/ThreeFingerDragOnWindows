using ThreeFingersDragOnWindows.utils;

namespace ThreeFingersDragOnWindows.touchpad; 

public class DistanceManager {
    
    
    public static float ApplySpeedAndAccOnLongestDist(TouchpadContact[] oldContacts, TouchpadContact[] contacts, int elapsed){
        int longestDistIndex = getLongestDistIndex(oldContacts, contacts);
        return ApplySpeedAndAcc(contacts[longestDistIndex].X, contacts[longestDistIndex].Y, elapsed);
    }
    
    public static float ApplySpeedAndAcc(float dx, float dy, int elapsed){
        return 0;
    }
    
    // Relative to new contacts parameter
    public static int getLongestDistIndex(TouchpadContact[] oldContacts, TouchpadContact[] contacts){
        float longestDist2D = 0;
        int longestDistIndex = 0;
        
        
        return longestDistIndex;
    }
    
}