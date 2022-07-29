using System.Timers;
using System.Collections.Generic;
using System;


namespace RawInput.Touchpad{
    public class ThreeFingersDrag {

        private const int releaseDelay = 300; // miliseconds
        private Timer dragEndTimer = new Timer(releaseDelay);

        private bool isDragging = false;
        private long lastThreeFingersContact = 0;
        private MousePoint lastLocation = new MousePoint(0, 0);
        private List<TouchpadContact> lastContacts = new List<TouchpadContact>();

        // When not null, the calibration is working
        private TouchpadCalibrator calibrator;
        private float ratio = 1; // touchpad dist / screen dist

        public ThreeFingersDrag(){
            // Setup timer
            dragEndTimer.Elapsed += (Object source, ElapsedEventArgs e) => checkDragEnd();
            dragEndTimer.AutoReset = false;
        }


        public void registerTouchpadContacts(TouchpadContact[] contacts){
            foreach(TouchpadContact contact in contacts) registerTouchpadContact(contact);
        }
        private void registerTouchpadContact(TouchpadContact contact){
            if(this.calibrator != null){
                calibrator.onCalibratingContact(contact);
                return;
            }

            foreach(TouchpadContact lastContact in lastContacts){
                if(lastContact.ContactId == contact.ContactId){
                    // A contact is registered twice: send the event with the list of all contacts
                    onTouchpadContact(lastContacts.ToArray());
                    lastContacts.Clear();
                    break;
                }
            }
            // Add the contact to the list
            lastContacts.Add(contact);
        }

        private void onTouchpadContact(TouchpadContact[] contacts){
            MousePoint point = averageCoordinate(contacts);

            if(contacts.Length == 3){
                lastThreeFingersContact = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

                if(!isDragging){
                    isDragging = true;
                    MouseOperations.MouseEvent(MouseOperations.MouseEventFlags.LeftDown);

                }else{ // Mouse do not move automatically on three fingers drag
                    MouseOperations.ShiftCursorPosition((int) ((point.x - lastLocation.x) / ratio), (int) ((point.y - lastLocation.y) / ratio));

                    dragEndTimer.Stop();
                    dragEndTimer.Start();
                }
            }else{
                if(isDragging){
                    isDragging = false;
                    MouseOperations.MouseEvent(MouseOperations.MouseEventFlags.LeftUp);
                }
            }
            lastLocation = point;
        }
        private void checkDragEnd(){
            if(isDragging && new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds() - lastThreeFingersContact > releaseDelay){
                isDragging = false;
                MouseOperations.MouseEvent(MouseOperations.MouseEventFlags.LeftUp);
            }
        }

        public void calibrate(){
            calibrator = new TouchpadCalibrator();
            calibrator.calibrate(5, (ratio) => {
                Console.WriteLine("Calibrated with ratio: " + ratio);
                this.ratio = ratio;
                this.calibrator = null;
            });
        }


        private MousePoint averageCoordinate(TouchpadContact[] contacts){
            int totalX = 0;
            int totalY = 0;
            int count = 0;
            foreach(TouchpadContact contact in contacts){
                totalX += contact.X;
                totalY += contact.Y;
                count++;
            }
            return new MousePoint(totalX/count, totalY/count);
        }
    }

    public struct MousePoint{
        public int x;
        public int y;
        public MousePoint(int x, int y){
            this.x = x;
            this.y = y;
        }
    }
    public struct Dimensions{
        public int w;
        public int h;
        public Dimensions(int w, int h){
            this.w = w;
            this.h = h;
        }
    }
}