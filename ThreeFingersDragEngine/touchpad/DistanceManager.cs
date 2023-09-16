using System;
using System.Collections.Generic;
using System.Linq;
using ThreeFingersDragEngine.utils;

namespace ThreeFingersDragEngine.touchpad;

public class DistanceManager {

    // List of contacts that exists but that can't be used for the distance calculation yet.
    // There is a delay of RELEASE_FINGERS_THRESHOLD_MS before they can affect the distance.
    private Dictionary<int, long> _quarantineContacts = new();
    private List<int> _trustedContacts = new();

    /// <summary>
    /// Find the longest distance between two TouchpadContact of same ID. When new contacts are registered, there is a delay of RELEASE_FINGERS_THRESHOLD_MS before they can affect the distance.
    /// </summary>
    /// <param name="oldContacts">First contacts list</param>
    /// <param name="newContacts">Second contacts list</param>
    /// <param name="elapsed">Elapsed ms between two contacts inputs</param>
    /// <returns>(ID of the TouchpadContact, Point distance (in x and y), 2D distance)</returns>
    public (int, Point, float) GetLongestDist2D(TouchpadContact[] oldContacts, TouchpadContact[] newContacts, long elapsed){
        if(elapsed > ThreeFingersDrag.RELEASE_FINGERS_THRESHOLD_MS){
            return (0, new Point(0, 0), 0);
        }

        float longestDist2D = 0;
        int longestDistId = 0;
        Point longestDistPoint = Point.Empty;

        // Quarantine system

        // Remove contacts that don't exist anymore
        _trustedContacts.RemoveAll(c => newContacts.All(nc => nc.ContactId != c));
        _quarantineContacts = _quarantineContacts.Where(c => newContacts.Any(nc => nc.ContactId == c.Key))
            .ToDictionary(c => c.Key, c => c.Value);

        // Add new contacts
        foreach(var newContact in newContacts){
            long contactCtms;
            if(_quarantineContacts.TryGetValue(newContact.ContactId, out contactCtms)){
                // Contact exist in quarantine
                if(Ctms() - contactCtms > ThreeFingersDrag.RELEASE_FINGERS_THRESHOLD_MS){
                    // Contact is now trusted
                    _trustedContacts.Add(newContact.ContactId);
                    _quarantineContacts.Remove(newContact.ContactId);
                }
            } else{
                // Contact is new
                _quarantineContacts.Add(newContact.ContactId, Ctms());
            }
        }

        // Checking the longest distance

        foreach(var newContact in newContacts){
            // If the contact is not trusted, skip it
            if(!_trustedContacts.Contains(newContact.ContactId)) continue;
            
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

    private long Ctms(){
        return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
    }

}