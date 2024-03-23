using System;

namespace ThreeFingerDragOnWindows.utils;

public struct PointUtils {
    
}

public struct Point
{
    public bool Equals(Point other){
        return x.Equals(other.x) && y.Equals(other.y);
    }

    public override bool Equals(object obj){
        return obj is Point other && Equals(other);
    }

    public override int GetHashCode(){
        return HashCode.Combine(x, y);
    }

    public float x;
    public float y;

    public Point(float x, float y)
    {
        this.x = x;
        this.y = y;
    }
    public Point(IntMousePoint point)
    {
        this.x = point.x;
        this.y = point.y;
    }

    public static Point Zero{ get; } = new(0, 0);

    public void Multiply(float multiplicator)
    {
        x = x * multiplicator;
        y = y * multiplicator;
    }

    public float Length()
    {
        return (float)Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));
    }

    public float DistTo(Point point)
    {
        return (float)Math.Sqrt(Math.Pow(x - point.x, 2) + Math.Pow(y - point.y, 2));
    }
    public float DistTo(IntMousePoint point)
    {
        return (float)Math.Sqrt(Math.Pow(x - point.x, 2) + Math.Pow(y - point.y, 2));
    }

    public static Point Empty = new(0, 0);

    public static bool operator ==(Point a, Point b)
    {
        return a.x == b.x && a.y == b.y;
    }
    public static bool operator !=(Point a, Point b)
    {
        return a.x != b.x || a.y != b.y;
    }
}