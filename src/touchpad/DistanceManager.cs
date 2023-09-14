using System;
using ThreeFingersDragOnWindows.utils;

namespace ThreeFingersDragOnWindows.touchpad;

public class DistanceManager {
    
    public static Point ApplySpeedAndAcc(Point delta, float dist2d, int elapsed){

        // Apply Speed
        delta.Multiply(App.SettingsData.MouseSpeed / 60);

        // Calculate the mouse velocity
        var mouseVelocity = (float) Math.Max(0.2, Math.Min(dist2d / elapsed, 20));
        if(float.IsNaN(mouseVelocity) || float.IsInfinity(mouseVelocity)) mouseVelocity = 1;

        
        float pointerVelocity = 1;
        if(App.SettingsData.MouseAcceleration != 0){
            // Apply acceleration
            pointerVelocity = (float) (App.SettingsData.MouseAcceleration / 10 * Math.Pow(mouseVelocity, 2) +
                                       0.4 * mouseVelocity);
            // Clamp
            pointerVelocity = (float) Math.Max(0.4, Math.Min(pointerVelocity, 1.6)); 
        }

        // Apply acceleration
        delta.Multiply(pointerVelocity);

        return delta;
    }
    
    public static float ApplySpeed(float distance){
        distance *= App.SettingsData.MouseSpeed / 60;
        return distance;
    }

    /// <summary>
    /// Find the longest distance between two TouchpadContact of same ID. If the two contacts lists does not contains the same contacts Ids, returns 0
    /// </summary>
    /// <param name="oldContacts">First contacts list</param>
    /// <param name="newContacts">Second contacts list</param>
    /// <param name="areContactsIdsCommons">Does the two contacts lists have the same contacts Ids</param>
    /// <param name="elapsed">Elapsed ms between two contacts inputs</param>
    /// <returns>(ID of the TouchpadContact, Point distance (in x and y), 2D distance)</returns>
    public static (int, Point, float) GetLongestDist2D(TouchpadContact[] oldContacts, TouchpadContact[] newContacts, bool areContactsIdsCommons, long elapsed){
        if(elapsed > ThreeFingersDrag.RELEASE_FINGERS_THRESHOLD_MS || !areContactsIdsCommons){
            return (0, new Point(0, 0), 0);
        }

        float longestDist2D = 0;
        int longestDistId = 0;
        Point longestDistPoint = Point.Empty;

        foreach(var newContact in newContacts){
            foreach(var oldContact in oldContacts){
                if(newContact.ContactId != oldContact.ContactId) continue;

                float dist2D = newContact.GetDist2D(oldContact);
                if(dist2D > longestDist2D){
                    longestDist2D = dist2D;
                    longestDistId = newContact.ContactId;
                    longestDistPoint = new Point(newContact.X - oldContact.X, newContact.Y - oldContact.Y);
                }

                break;
            }
        }

        return (longestDistId, longestDistPoint, longestDist2D);
    }

}