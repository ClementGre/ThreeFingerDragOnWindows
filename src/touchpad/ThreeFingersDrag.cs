using System.Diagnostics;
using System.Linq;
using ThreeFingersDragOnWindows.utils;

namespace ThreeFingersDragOnWindows.touchpad;

public class ThreeFingersDrag {

    private readonly FingersCounter _fingersCounter = new ();
    
    private DragState _dragState = DragState.Off;
    private enum DragState {
        Off,
        Suspended,
        On
    }
    
    public const float RELEASE_FINGERS_THRESHOLD_MS = 30; // Windows Precision Touchpad sends contacts about every 10ms

    public void OnTouchpadContact(TouchpadContact[] oldContacts, TouchpadContact[] contacts, long elapsed){
        Debug.WriteLine("\nTFD: " + string.Join(", ", oldContacts.Select(c => c.ToString())) + " | " +
                        string.Join(", ", contacts.Select(c => c.ToString())) + " | " + elapsed);
        bool areContactsIdsCommons = FingersCounter.AreContactsIdsCommons(oldContacts, contacts);

        (int longestDistId, Point longestDistDelta, float longestDist2D) =
            DistanceManager.GetLongestDist2D(oldContacts, contacts, areContactsIdsCommons, elapsed);
        (int fingersCount, int movingFingersCount) =
            _fingersCounter.CountMovingFingers(oldContacts, contacts, areContactsIdsCommons, longestDist2D);

        Debug.WriteLine("    fingers: " + fingersCount + ", moving: " + movingFingersCount + ", dist: " +
                        longestDist2D);

        if(movingFingersCount == 3 && _dragState == DragState.Off){
            // Start dragging
            _dragState = DragState.On;
            Debug.WriteLine("    START DRAG, LEFT CLICK DOWN");
            MouseOperations.MouseClick(MouseOperations.MOUSEEVENTF_LEFTDOWN);

        } else if(fingersCount >= 3 && areContactsIdsCommons && _dragState == DragState.On){
            // Dragging
            if(App.SettingsData.ThreeFingersMove){
                Debug.WriteLine("    MOVING, (x, y) = (" + longestDistDelta.x + ", " + longestDistDelta.y + ")");

                Point delta = DistanceManager.ApplySpeedAndAcc(longestDistDelta, longestDist2D, (int) elapsed);
                MouseOperations.ShiftCursorPosition(delta.x, delta.y);
            }

        } else if(movingFingersCount < 3 && _dragState == DragState.On){
            // Stop dragging
            _dragState = DragState.Off;
            Debug.WriteLine("    STOP DRAG, LEFT CLICK UP");
            MouseOperations.MouseClick(MouseOperations.MOUSEEVENTF_LEFTUP);
        }

    }
    
}