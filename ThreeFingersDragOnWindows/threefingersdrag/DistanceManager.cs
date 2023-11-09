using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ThreeFingersDragEngine.utils;
using ThreeFingersDragOnWindows.utils;

namespace ThreeFingersDragOnWindows.threefingersdrag;

public class DistanceManager {

    // List of contacts that exists but that can't be used for the distance calculation yet.
    // There is a delay of RELEASE_FINGERS_THRESHOLD_MS before they can affect the distance.
    private Dictionary<int, long> _quarantineContacts = new();
    private List<int> _trustedContacts = new();

    /// <summary>
    /// Find the longest distance between two TouchpadContact of same ID.
    /// When new contacts are registered, there is a delay (quarantine) before they can affect the distance.
    /// Returned distance is 0 if the delay between this call and the last one is higher than RELEASE_FINGERS_THRESHOLD_MS (if hasFingersReleased is true).
    /// </summary>
    /// <param name="oldContacts">First contacts list</param>
    /// <param name="newContacts">Second contacts list</param>
    /// <param name="hasFingersReleased">Wether if fingers has been released and replaced on the touchpad</param>
    /// <returns>(ID of the TouchpadContact, Point distance (in x and y), 2D distance)</returns>
    public (int, Point, float) GetLongestDist2D(TouchpadContact[] oldContacts, TouchpadContact[] newContacts, bool hasFingersReleased){
        if(hasFingersReleased){
            _quarantineContacts.Clear();
            _trustedContacts.Clear();
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


    public static Point ApplySpeedAndAcc(Point delta, int elapsed){

        // Apply Speed
        delta.Multiply(App.SettingsData.ThreeFingersDragCursorSpeed / 120);

        // Calculate the mouse velocity : sort of a relative speed between 0 and 4, 1 being the average speed.
        var mouseVelocity = Math.Min(delta.Length() / elapsed, 4);
        if(float.IsNaN(mouseVelocity) || float.IsInfinity(mouseVelocity)) mouseVelocity = 1;


        float pointerVelocity = 1;
        if(App.SettingsData.ThreeFingersDragCursorAcceleration != 0){
            // Apply acceleration : function that transform the mouseVelocity into a pointerVelocity : 0.7+zs\left(2.6a\left(x-1+\frac{3-\ln\left(\frac{z}{0.3}-1\right)}{2.6a}\right)-3\right)
            // See https://www.desmos.com/calculator/khtj85jopn
            float a = App.SettingsData.ThreeFingersDragCursorAcceleration;
            pointerVelocity = (float) (0.7 + 0.8 * Sigmoid(2.6 * a * (mouseVelocity - 1 + (3 - Math.Log2(0.8/0.3 - 1)) / (2.6 * a)) - 3));
            // No need to clamp, the function gives values between 0.7 and 1.5.
            Debug.WriteLine("    pointerVelocity: " + pointerVelocity + " (mouseVelocity: " + mouseVelocity + ")");
        }

        // Apply acceleration
        delta.Multiply(pointerVelocity);

        return delta;
    }

    public static float ApplySpeed(float distance){
        distance *= App.SettingsData.ThreeFingersDragCursorSpeed / 60;
        return distance;
    }

    private long Ctms(){
        return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
    }
    
    private static double Sigmoid(double value) {
        double k = Math.Exp(value);
        return k / (1.0d + k);
    }

}