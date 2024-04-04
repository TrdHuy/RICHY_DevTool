using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RICHY_DevTool
{
    /// <summary>
    /// Interaction logic for TestAnimationWindow.xaml
    /// </summary>
    public partial class TestAnimationWindow : Window
    {
        private SemaphoreSlim sml = new SemaphoreSlim(1, 1);
        private double currentFps;
        public TestAnimationWindow()
        {
            InitializeComponent();
            StartCountingFPS();
        }

        private async void StartCountingFPS()
        {
            await CalculateFps();
        }
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await sml.WaitAsync();
                var curentPosX = Canvas.GetLeft(TestEllipse);
                var isMoveLeft = curentPosX >= 100;
                while ((!isMoveLeft && curentPosX <= 100) ||
                    (isMoveLeft && curentPosX >= 0))
                {
                    if (!isMoveLeft)
                    {
                        curentPosX += 1;
                    }
                    else
                    {
                        curentPosX -= 1;
                    }
                    Canvas.SetLeft(TestEllipse, curentPosX);
                    await Task.Delay(10);
                }
            }
            finally
            {
                sml.Release();
            }
        }

        Boolean isMoveLeft = false;
        float velocity = 20; // pixel per sec
        private void Update()
        {
            var curentPosX = Canvas.GetLeft(TestEllipse);
            if ((!isMoveLeft && curentPosX <= 100) ||
                    (isMoveLeft && curentPosX >= 0))
            {
                if (!isMoveLeft)
                {
                    curentPosX += velocity / currentFps;
                }
                else
                {
                    curentPosX -= velocity / currentFps;
                }
                Canvas.SetLeft(TestEllipse, curentPosX);

                if (curentPosX >= 100)
                {
                    isMoveLeft = true;
                }
                else if (curentPosX <= 0)
                {
                    isMoveLeft = false;
                }
            }
        }


        private async Task CalculateFps()
        {
            var sw = Stopwatch.StartNew();
            long previousTick = sw.ElapsedTicks;
            while (true)
            {
                long currentTick = sw.ElapsedTicks;
                long deltaTick = currentTick - previousTick;
                previousTick = currentTick;
                double deltaSecond = (double)deltaTick / Stopwatch.Frequency;
                currentFps = 1.0 / deltaSecond;
                Dispatcher.Invoke(() =>
                {
                    this.Title = $"FPS: {(int)currentFps}, Time: {DateTime.Now.ToString("HH:mm:ss")}";
                    Update();
                }, priority: System.Windows.Threading.DispatcherPriority.Normal);

                await Task.Delay(30);
            }
        }



    }
}
