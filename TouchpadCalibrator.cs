using System;
using System.Diagnostics;
using System.Timers;

namespace ThreeFingersDragOnWindows;

public class TouchpadCalibrator {
    public delegate void TaskCompletedCallBack(float ratio);

    private float _globalLongestDist; // Longest distance of all moves
    private float _longestDist; // Longest distance of last move
    private float _ratio = 1; // Represents the numbers of touchpad coordinates that corresponds to one pixel.
    
    private MousePoint _startPoint = new(0, 0);
    private MousePoint _lastPoint = new(0, 0);
    private MousePoint _touchpadStartPoint = new(0, 0);
    private MousePoint _touchpadLastPoint = new(0, 0);

    public void Calibrate(int seconds, TaskCompletedCallBack callback){
        var timer = new Timer(seconds * 1000);
        timer.AutoReset = false;
        timer.Enabled = true;
        timer.Elapsed += (_, _) => {
            CalculateRatio();
            Console.WriteLine("Returning ratio: " + _ratio);
            callback(_ratio);
        };
    }

    public void OnCalibratingContact(TouchpadContact contact){
        if(contact.ContactId != 0) return;

        var currentPoint = MouseOperations.GetCursorPosition();
        var dist = PointDist(currentPoint, _startPoint);

        if((_startPoint.x == 0 && _startPoint.y == 0) || dist < _longestDist){
            CalculateRatio();
            _touchpadStartPoint = contact.getMousePoint();
            _startPoint = currentPoint;
        }
        else{
            _longestDist = dist;
        }

        _lastPoint = currentPoint;
        _touchpadLastPoint = contact.getMousePoint();
    }

    private static float PointDist(MousePoint a, MousePoint b){
        return (float) Math.Sqrt(Math.Pow(a.x - b.x, 2) + Math.Pow(a.y - b.y, 2));
    }

    private void CalculateRatio(){
        if(_longestDist <= _globalLongestDist) return;

        _globalLongestDist = PointDist(_startPoint, _lastPoint);
        var touchPadDist = PointDist(_touchpadStartPoint, _touchpadLastPoint);
        _ratio = touchPadDist / _globalLongestDist;
        Console.WriteLine("Calculated ratio: " + _globalLongestDist + "/" + touchPadDist + " = " + _ratio);
    }
}