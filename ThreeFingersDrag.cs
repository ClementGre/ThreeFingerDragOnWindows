using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;

namespace ThreeFingersDragOnWindows;

public class ThreeFingersDrag {
    private const int ReleaseDelay = 20; // milliseconds

    // When not null, the calibration is working
    private TouchpadCalibrator _calibrator;
    private readonly Timer _dragEndTimer = new(ReleaseDelay);

    private bool _isDragging;
    private readonly List<TouchpadContact> _lastContacts = new();
    private MousePoint _lastLocation = new(0, 0);
    private long _lastThreeFingersContact;
    private float _ratio = 1; // touchpad dist / screen dist

    public ThreeFingersDrag(){
        // Setup timer
        _dragEndTimer.Elapsed += (_, _) => CheckDragEnd();
        _dragEndTimer.AutoReset = false;
    }


    public void RegisterTouchpadContacts(TouchpadContact[] contacts){
        foreach(var contact in contacts) RegisterTouchpadContact(contact);
    }

    private void RegisterTouchpadContact(TouchpadContact contact){
        if(_calibrator != null){
            _calibrator.OnCalibratingContact(contact);
            return;
        }

        foreach(var lastContact in _lastContacts)
            if(lastContact.ContactId == contact.ContactId){
                // A contact is registered twice: send the event with the list of all contacts
                OnTouchpadContact(_lastContacts.ToArray());
                _lastContacts.Clear();
                break;
            }

        // Add the contact to the list
        _lastContacts.Add(contact);
    }

    private void OnTouchpadContact(TouchpadContact[] contacts){
        var point = AverageCoordinate(contacts);

        if(contacts.Length == 3){
            _lastThreeFingersContact = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

            if(!_isDragging){
                _isDragging = true;
                MouseOperations.MouseEvent(MouseOperations.MouseEventFlags.LeftDown);
            }
            else{
                // Mouse do not move automatically on three fingers drag
                MouseOperations.ShiftCursorPosition((int) ((point.x - _lastLocation.x) / _ratio),
                    (int) ((point.y - _lastLocation.y) / _ratio));

                _dragEndTimer.Stop();
                _dragEndTimer.Start();
            }
        }
        else{
            if(_isDragging){
                _isDragging = false;
                MouseOperations.MouseEvent(MouseOperations.MouseEventFlags.LeftUp);
            }
        }

        _lastLocation = point;
    }

    private void CheckDragEnd(){
        if(_isDragging && new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds() - _lastThreeFingersContact >
           ReleaseDelay){
            _isDragging = false;
            MouseOperations.MouseEvent(MouseOperations.MouseEventFlags.LeftUp);
        }
    }

    public void Calibrate(){
        _calibrator = new TouchpadCalibrator();
        _calibrator.Calibrate(5, (ratio) => {
            Console.WriteLine("Calibrated with ratio: " + ratio);
            _ratio = ratio;
            _calibrator = null;
        });
    }


    private MousePoint AverageCoordinate(TouchpadContact[] contacts){
        var totalX = 0;
        var totalY = 0;
        var count = 0;
        foreach(var contact in contacts){
            totalX += contact.X;
            totalY += contact.Y;
            count++;
        }

        if(count == 0) return new MousePoint(0, 0);
        return new MousePoint(totalX / count, totalY / count);
    }
}

public struct MousePoint {
    public int x;
    public int y;

    public MousePoint(int x, int y){
        this.x = x;
        this.y = y;
    }
}