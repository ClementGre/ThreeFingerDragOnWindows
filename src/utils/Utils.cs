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
}

public struct MousePoint {
    public int x;
    public int y;

    public MousePoint(int x, int y){
        this.x = x;
        this.y = y;
    }
}