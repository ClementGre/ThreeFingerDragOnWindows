using System;
using System.Timers;
using Microsoft.UI.Dispatching;

namespace ThreeFingersDragOnWindows.utils;

internal class Utils {
    public static void runOnMainThreadAfter(int ms, Action action){
        var queue = DispatcherQueue.GetForCurrentThread();
        var timer = new Timer(ms);
        timer.Elapsed += (_, _) => queue.TryEnqueue(() => action());
        timer.AutoReset = false;
        timer.Start();
    }
}