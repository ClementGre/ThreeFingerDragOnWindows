using System;

namespace ThreeFingersDragOnWindows.src.utils;

public struct TouchpadContact : IEquatable<TouchpadContact> {
    public int ContactId{ get; }
    public int X{ get; }
    public int Y{ get; }

    public TouchpadContact(int contactId, int x, int y){
        (ContactId, X, Y) = (contactId, x, y);
    }

    public override bool Equals(object obj){
        return obj is TouchpadContact other && Equals(other);
    }

    public bool Equals(TouchpadContact other){
        return ContactId == other.ContactId && X == other.X && Y == other.Y;
    }

    public static bool operator ==(TouchpadContact a, TouchpadContact b){
        return a.Equals(b);
    }

    public static bool operator !=(TouchpadContact a, TouchpadContact b){
        return !(a == b);
    }

    public override int GetHashCode(){
        return (ContactId, X, Y).GetHashCode();
    }

    public override string ToString(){
        return $"ID: {ContactId} x,y: {X},{Y}";
    }

    public MousePoint getMousePoint(){
        return new MousePoint(X, Y);
    }
}

internal class TouchpadContactCreator {
    public int? ContactId{ get; set; }
    public int? X{ get; set; }
    public int? Y{ get; set; }

    public bool TryCreate(out TouchpadContact contact){
        if(ContactId.HasValue && X.HasValue && Y.HasValue){
            contact = new TouchpadContact(ContactId.Value, X.Value, Y.Value);
            return true;
        }

        contact = default;
        return false;
    }

    public void Clear(){
        ContactId = null;
        X = null;
        Y = null;
    }
}