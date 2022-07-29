using System;
using System.Timers;

namespace ThreeFingersDragOnWindows{
    public class TouchpadCalibrator {


        public delegate void TaskCompletedCallBack(float ratio);

        public TouchpadCalibrator(){

        }


        public void calibrate(int seconds, TaskCompletedCallBack callback){

            Timer timer = new Timer(seconds * 1000);
            timer.AutoReset = false;
            timer.Enabled = true;
            timer.Elapsed += (Object source, ElapsedEventArgs e) => {
                
                calculateRatio();
                Console.WriteLine("Returning ratio: " + ratio);
                callback(ratio);
            };
        }

        private MousePoint touchpadStartPoint = new MousePoint(0, 0);
        private MousePoint touchpadLastPoint = new MousePoint(0, 0);
        private MousePoint startPoint = new MousePoint(0, 0);
        private MousePoint lastPoint = new MousePoint(0, 0);
        private float longestDist = 0; // Longest distance of last move

        private float globalLongestDist = 0; // Longest distance of all moves
        private float ratio = 1; // Represents the numbers of touchpad coordinates that corresponds to one pixel.

        public void onCalibratingContact(TouchpadContact contact){
            if(contact.ContactId != 0) return;

            MousePoint currentPoint = MouseOperations.GetCursorPosition();

        
            float dist = pointDist(currentPoint, startPoint);

            if((startPoint.x == 0 && startPoint.y == 0) || dist < longestDist){
                calculateRatio();
                touchpadStartPoint = contact.getMousePoint();
                startPoint = currentPoint;

            }else{
                longestDist = dist;
            }

            lastPoint = currentPoint;
            touchpadLastPoint = contact.getMousePoint();
        }

        private float pointDist(MousePoint a, MousePoint b){
            return (float) Math.Sqrt(Math.Pow(a.x - b.x, 2) + Math.Pow(a.y - b.y, 2));
        }
        private void calculateRatio(){
            if(longestDist <= globalLongestDist) return;

            globalLongestDist = pointDist(startPoint, lastPoint);
            float touchPadDist = pointDist(touchpadStartPoint, touchpadLastPoint);
            ratio = touchPadDist / globalLongestDist;
            Console.WriteLine("Calculated ratio: " + globalLongestDist + "/" + touchPadDist + " = " + ratio);
        }

    }
}