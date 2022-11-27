using System;
using System.Windows.Documents;

namespace ThreeFingersDragOnWindows.src.utils;

public static class Utils {
    public static MousePoint AverageCoordinate(TouchpadContact[] contacts){
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
    
    public static float Dist(float x1, float y1, float x2, float y2){
        return (float) Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
    }
}

public struct MousePoint {
    public float x;
    public float y;

    public MousePoint(float x, float y){
        this.x = x;
        this.y = y;
    }
    public MousePoint(IntMousePoint point){
        this.x = point.x;
        this.y = point.y;
    }

    public void Multiply(float multiplicator){
        x = x * multiplicator;
        y = y * multiplicator;
    }

    public float Length(){
        return (float) Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));
    }

    public float DistTo(MousePoint point){
        return (float) Math.Sqrt(Math.Pow(x - point.x, 2) + Math.Pow(y - point.y, 2));
    }
    public float DistTo(IntMousePoint point){
        return (float) Math.Sqrt(Math.Pow(x - point.x, 2) + Math.Pow(y - point.y, 2));
    }

    public static MousePoint Empty = new(0, 0);
    
    public static bool operator ==(MousePoint a, MousePoint b){
        return a.x == b.x && a.y == b.y;
    }
    public static bool operator !=(MousePoint a, MousePoint b){
        return a.x != b.x || a.y != b.y;
    }
}

public struct IntMousePoint {
    public int x;
    public int y;

    public IntMousePoint(int x, int y){
        this.x = x;
        this.y = y;
    }
}

public struct ThreeFingersPoints {
    public int Length;
    public float x1;
    public float y1;
    public float x2;
    public float y2;
    public float x3;
    public float y3;
    public ThreeFingersPoints(){
        this.Length = 0;
        this.x1 = 0;
        this.y1 = 0;
        this.x2 = 0;
        this.y2 = 0;
        this.x3 = 0;
        this.y3 = 0;
    }
    public ThreeFingersPoints(float x1, float y1, float x2, float y2, float x3, float y3){
        this.x1 = x1;
        this.y1 = y1;
        this.x2 = x2;
        this.y2 = y2;
        this.x3 = x3;
        this.y3 = y3;
        if (x1 == 0 && y1 == 0 && x2 == 0 && y2 == 0 && x3 == 0 && y3 == 0){
            this.Length = 0;
        }else if (x2 == 0 && y2 == 0 && x3 == 0 && y3 == 0){
            this.Length = 1;
        }else if (x3 == 0 && y3 == 0){
            this.Length = 2;
        }else this.Length = 3;
    }
    public ThreeFingersPoints(TouchpadContact[] contacts){
        if(contacts.Length >= 1){
            this.x1 = contacts[0].X;
            this.y1 = contacts[0].Y;
        }else{
            this.x1 = 0;
            this.y1 = 0;
        }
        if(contacts.Length >= 2){
            this.x2 = contacts[1].X;
            this.y2 = contacts[1].Y;
        }else{
            this.x2 = contacts[0].X;
            this.y2 = contacts[0].Y;
        }
        if(contacts.Length >= 3){
            this.x3 = contacts[2].X;
            this.y3 = contacts[2].Y;
        }else{
            this.x3 = contacts[0].X;
            this.y3 = contacts[0].Y;
        }
        this.Length = contacts.Length;
    }
    public static ThreeFingersPoints Empty = new();

    public static bool operator ==(ThreeFingersPoints a, ThreeFingersPoints b){
        return a.x1 == b.x1 && a.y1 == b.y1 && a.x2 == b.x2 && a.y2 == b.y2 && a.x3 == b.x3 && a.y3 == b.y3;
    }
    public static bool operator !=(ThreeFingersPoints a, ThreeFingersPoints b){
        return a.x1 != b.x1 || a.y1 != b.y1 || a.x2 != b.x2 || a.y2 != b.y2 || a.x3 != b.x3 || a.y3 != b.y3;
    }

    public MousePoint GetLongestDistPoint(ThreeFingersPoints points){
        var d1 = Utils.Dist(x1, y1, points.x1, points.y1);
        var d2 = Utils.Dist(x2, y2, points.x2, points.y2);
        var d3 = Utils.Dist(x3, y3, points.x3, points.y3);
        if(d1 > d2 && d1 > d3) return new MousePoint(x1, y1);
        if(d2 > d1 && d2 > d3) return new MousePoint(x2, y2);
        return new MousePoint(x3, y3);
    }
    public MousePoint GetLongestDist2D(ThreeFingersPoints points){
        return GetLongestDistNumber(points) switch{
            1 => new MousePoint(x1 - points.x1, y1 - points.y1),
            2 => new MousePoint(x2 - points.x2, y2 - points.y2),
            3 => new MousePoint(x3 - points.x3, y3 - points.y3),
            _ => new MousePoint(0, 0)
        };
    }
    public int GetLongestDistNumber(ThreeFingersPoints points){
        var d1 = Utils.Dist(x1, y1, points.x1, points.y1);
        var d2 = Utils.Dist(x2, y2, points.x2, points.y2);
        var d3 = Utils.Dist(x3, y3, points.x3, points.y3);
        if(d1 > d2 && d1 > d3) return 1;
        if(d2 > d1 && d2 > d3) return 2;
        return 3;
    }

    public float GetLongestDist(ThreeFingersPoints points){
        var d1 = Utils.Dist(x1, y1, points.x1, points.y1);
        var d2 = Utils.Dist(x2, y2, points.x2, points.y2);
        var d3 = Utils.Dist(x3, y3, points.x3, points.y3);
        if(d1 > d2 && d1 > d3) return d1;
        if(d2 > d1 && d2 > d3) return d2;
        return d3;
    }

    public override string ToString(){
        return "{x1: " + x1 + " y1: " + y1 + " x2: " + x2 + " y2: " + y2 + " x3: " + x3 + " y3: " + y3 + "}";
    }
}