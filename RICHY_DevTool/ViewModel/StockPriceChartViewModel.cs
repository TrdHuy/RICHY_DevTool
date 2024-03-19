using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace RICHY_DevTool.ViewModel
{
    public class StockPriceChartViewModel : BaseViewModel, IStockPriceChangedListener
    {
        private Stock Stock { get; set; }
        public SeriesCollection SeriesCollection { get; set; } = new SeriesCollection();
        public long MinDate
        {
            get
            {
                return Stock.PriceRecords.First().Date.Ticks;
            }
        }

        public long MaxDate
        {
            get
            {
                if (Stock.PriceRecords.Count == 1)
                {
                    return Stock.PriceRecords.Last().Date.AddDays(1).Ticks;
                }
                return Stock.PriceRecords.Last().Date.Ticks;
            }
        }

        public StockPriceChartViewModel(Stock stock)
        {
            Stock = stock;
            SeriesCollection.Add(new LineSeries
            {
                Title = "Series 4",
                Values = GetData(),
                LineSmoothness = 0, //0: straight lines, 1: really smooth lines
                PointGeometrySize = 5,
                PointForeground = Brushes.Gray
            });
        }

        public void OnNewRecordAdded(PriceRecord newRecord)
        {
            SeriesCollection[0].Values.Add(new DateTimePoint(newRecord.Date, newRecord.Price));
        }

        private ChartValues<DateTimePoint> GetData()
        {
            var values = new ChartValues<DateTimePoint>();
            values.Add(new DateTimePoint(new DateTime(MinDate).AddDays(-1), 0));
            foreach (var record in Stock.PriceRecords)
            {
                values.Add(new DateTimePoint(record.Date, record.Price));
            }
            return values;
        }


    }
}
