namespace ThreeFingerDragOnWindows.touchpad;

public static class TouchpadTiming{
    // Windows Precision Touchpads normally report contacts about every 10 ms.
    // A longer gap means that the previous contact sequence has ended.
    public const int ContactReleaseThresholdMs = 40;
}
