using System;
using Windows.System;

namespace ThreeFingersDragEngine.utils;

internal class Utils {
    public static void runOnMainThreadAfter(int ms, Action action){
        var queue = DispatcherQueue.GetForCurrentThread();
        var timer = new System.Timers.Timer(ms);
        timer.Elapsed += (_, _) => queue.TryEnqueue(() => action());
        timer.AutoReset = false;
        timer.Start();
    }
}