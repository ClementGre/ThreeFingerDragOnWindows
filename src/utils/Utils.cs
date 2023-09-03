using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreeFingersDragOnWindows.src.utils;

internal class Utils
{

   public static void runOnMainThreadAfter(int ms, Action action)
    {
    var queue = DispatcherQueue.GetForCurrentThread();
    var timer = new System.Timers.Timer(ms);
      timer.Elapsed += (_, _) => queue.TryEnqueue(() => action());
      timer.AutoReset = false;
      timer.Start();
   }

}
