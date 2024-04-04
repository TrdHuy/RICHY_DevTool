using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RICHYEngine.Views.Animation
{
    public static class AnimationController
    {
        public static double CurrentFps { get; private set; }
        public static event Action<double>? Animating;

        public static async void StartAnimationFPSCounter(Action<double, Action<double>?> uiUpdateCallback)
        {
            await Task.Run(async () =>
            {
                var sw = Stopwatch.StartNew();
                long previousTick = sw.ElapsedTicks;
                while (true)
                {
                    long currentTick = sw.ElapsedTicks;
                    long deltaTick = currentTick - previousTick;
                    previousTick = currentTick;
                    double deltaSecond = (double)deltaTick / Stopwatch.Frequency;
                    CurrentFps = 1.0 / deltaSecond;
                    uiUpdateCallback.Invoke(CurrentFps, Animating);

                    await Task.Delay(30);
                }
            });
            
        }
    }
}
