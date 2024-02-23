using LiveCharts.Wpf;
using LiveCharts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using LiveCharts.Defaults;

namespace RICHY_DevTool.View.Widgets
{
    /// <summary>
    /// Interaction logic for PriceChart.xaml
    /// </summary>
    public partial class PriceChart : UserControl, INotifyPropertyChanged
    {
        private ZoomingOptions _zoomingMode = ZoomingOptions.Xy;

        public PriceChart()
        {
            InitializeComponent();

            Labels = new[] { "Jan", "Feb", "Mar", "Apr", "May" };

            //modifying the series collection will animate and update the chart
            SeriesCollection.Add(new LineSeries
            {
                Title = "Series 4",
                Values = GetData(),
                LineSmoothness = 0, //0: straight lines, 1: really smooth lines
                PointGeometrySize = 5,
                PointForeground = Brushes.Gray
            });

        }

        public SeriesCollection SeriesCollection { get; set; } = new SeriesCollection();
        public string[] Labels { get; set; }
        public Func<double, string> YFormatter { get; set; } = value => value.ToString("C");
        public Func<double, string> XFormatter { get; set; } = val => new DateTime((long)val).ToString("dd MMM");
        public ZoomingOptions ZoomingMode
        {
            get { return _zoomingMode; }
            set
            {
                _zoomingMode = value;
                OnPropertyChanged();
            }
        }

        private ChartValues<DateTimePoint> GetData()
        {
            var r = new Random();
            var trend = 100;
            var values = new ChartValues<DateTimePoint>();

            for (var i = 0; i < 100; i++)
            {
                var seed = r.NextDouble();
                if (seed > .8) trend += seed > .9 ? 50 : -50;
                values.Add(new DateTimePoint(DateTime.Now.AddDays(i), trend + r.Next(0, 10)));
            }

            return values;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null) PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.LeftCtrl && ZoomingMode != ZoomingOptions.X)
            {
                ZoomingMode = ZoomingOptions.X;
            }
            else if (e.Key == Key.LeftShift && ZoomingMode != ZoomingOptions.Y)
            {
                ZoomingMode = ZoomingOptions.Y;
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.Key == Key.LeftCtrl && ZoomingMode != ZoomingOptions.Xy)
            {
                ZoomingMode = ZoomingOptions.Xy;
            }
            else if (e.Key == Key.LeftShift && ZoomingMode != ZoomingOptions.Xy)
            {
                ZoomingMode = ZoomingOptions.Xy;
            }
        }

        
        private void container_MouseEnter(object sender, MouseEventArgs e)
        {
            Focus();
        }

        private void container_MouseLeave(object sender, MouseEventArgs e)
        {
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), null);
        }

    }
}
