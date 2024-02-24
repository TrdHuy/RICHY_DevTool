using RICHY_DevTool.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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

namespace RICHY_DevTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MutableStock TTVStock = new MutableStock("TST", 10000f)
        {
            Name = "TST",
            News = new Collection<MutableStockNews> {
                    new MutableStockNews(
                        title: "",
                        description:"",
                        affectedStockPrice_DecreasePercent: -30f,
                        affectedStockPrice_IncreasePercent: 60f,
                        affectedStockPrice_SteadyPercent:-30f)
                },
        };
        private StockPriceController SPC = new StockPriceController(10f);


        public MainWindow()
        {
            InitializeComponent();
            var TTVStockVM = new ViewModel.StockPriceChartViewModel(TTVStock);
            SPC.RegisterPriceChanged(TTVStockVM);
            Chart.StockViewModel = TTVStockVM;
        }

        private void SkipDayClick(object sender, RoutedEventArgs e)
        {
            SPC.CalculateNewPriceForNextDay(TTVStock);
            //PriceSeries.Values.Add((double)TTVStock.Price);
        }
    }

    public interface IStockPriceChangedListener
    {
        void OnNewRecordAdded(PriceRecord newRecord);
    }

    public class StockPriceController
    {
        private static Random spcRandom = new Random();
        public static int SelectFromPercentages(params float[] percentage)
        {
            var sum = percentage.Sum(it => (int)(it * 100));
            var r = spcRandom.Next(0, sum);

            var recalcuPercent = new int[percentage.Length];
            for (int i = 0; i < percentage.Length; i++)
            {
                recalcuPercent[i] = (int)(percentage[i] * 100);
                if (i > 0)
                {
                    recalcuPercent[i] += recalcuPercent[i - 1];
                }

                if (r <= recalcuPercent[i])
                {
                    return i;
                }
            }
            return -1;
        }
        private enum PriceChangedEvent
        {
            RECORD_ADDED
        }

        private const float INCREASE_PERCENTAGE = 33.33f;
        private const float DECREASE_PERCENTAGE = 33.33f;
        private const float STEADY_PERCENTAGE = 33.33f;

        private float mFluctuationRateMaximum = 10f;
        private Collection<IStockPriceChangedListener> priceChangedCallback = new Collection<IStockPriceChangedListener>();
      

        public StockPriceController(float fluctuationRateMaximum)
        {
            mFluctuationRateMaximum = fluctuationRateMaximum;
        }

        public void RegisterPriceChanged(IStockPriceChangedListener callback)
        {
            if (!priceChangedCallback.Contains(callback))
            {
                priceChangedCallback.Add(callback);
            }
        }

        public void UnregisterPriceChanged(IStockPriceChangedListener callback)
        {
            priceChangedCallback.Remove(callback);
        }

        public void CalculateNewPriceForNextDay(MutableStock targetStock)
        {
            var increasePer = INCREASE_PERCENTAGE;
            var decreasePer = DECREASE_PERCENTAGE;
            var steadyPer = STEADY_PERCENTAGE;

            foreach (var news in targetStock.News)
            {
                increasePer += news.AffectedStockPrice_IncreasePercent;
                decreasePer += news.AffectedStockPrice_DecreasePercent;
                steadyPer += news.AffectedStockPrice_SteadyPercent;
            }
            increasePer = increasePer > 100f ? 100f : increasePer < 0f ? 0f : increasePer;
            decreasePer = decreasePer > 100f ? 100f : decreasePer < 0f ? 0f : decreasePer;
            steadyPer = steadyPer > 100f ? 100f : steadyPer < 0f ? 0f : steadyPer;

            var fluctuationRate = (double)(spcRandom.Next((int)(mFluctuationRateMaximum * 100)) / 10000d);
            var nextDay = targetStock.PriceRecords.Last().Date.AddDays(1);
            switch (SelectFromPercentages(increasePer, decreasePer, steadyPer))
            {
                // increase case
                case 0:
                    {
                        targetStock.Price = (float)(targetStock.Price * (1 + fluctuationRate));
                        var record = new MutablePriceRecord(nextDay, targetStock.Price, (float)fluctuationRate);
                        targetStock.PriceRecords.Add(record);
                        RaiseCallbackEvent(PriceChangedEvent.RECORD_ADDED, record);
                        break;
                    }
                // decrease case
                case 1:
                    {
                        targetStock.Price = (float)(targetStock.Price * (1 - fluctuationRate));
                        var record = new MutablePriceRecord(nextDay, targetStock.Price, (float)-fluctuationRate);
                        targetStock.PriceRecords.Add(record);
                        RaiseCallbackEvent(PriceChangedEvent.RECORD_ADDED, record);
                        break;
                    }
                //steady case
                case -1:
                case 2:
                    {
                        break;
                    }
            }
        }

        private void RaiseCallbackEvent(PriceChangedEvent priceEvent, PriceRecord? newRecord = null)
        {
            foreach (var callback in priceChangedCallback)
            {
                if (priceEvent.HasFlag(PriceChangedEvent.RECORD_ADDED))
                {
                    Debug.Assert(newRecord != null);
                    callback.OnNewRecordAdded(newRecord);
                }
            }
        }
    }

    public class MutableStock : Stock
    {
        public new string Name { get { return base.Name; } set { base.Name = value; } }
        public new float Price { get { return base.Price; } set { base.Price = value; } }

        public new Collection<MutableStockNews> News { get { return mNews; } set { mNews = value; } }
        public new Collection<MutablePriceRecord> PriceRecords { get { return mPriceRecords; } set { mPriceRecords = value; } }

        public MutableStock(string name, float price) : base(name, price)
        {
        }
    }

    public class Stock
    {
        protected Collection<MutableStockNews> mNews { get; set; } = new Collection<MutableStockNews>();
        protected Collection<MutablePriceRecord> mPriceRecords { get; set; } = new Collection<MutablePriceRecord>() {
            new MutablePriceRecord(DateTime.Now.Date, INIT_PRICE, 0f)
        };

        private const float INIT_PRICE = 10000f;
        public string Name { get; protected set; } = "";
        public float Price { get; protected set; } = INIT_PRICE;
        public ReadOnlyCollection2<StockNews> News { get; private set; }
        public ReadOnlyCollection2<PriceRecord> PriceRecords { get; private set; }

        public Stock(string name, float price)
        {
            Name = name;
            Price = price;
            News = new ReadOnlyCollection2<StockNews>(mNews);
            PriceRecords = new ReadOnlyCollection2<PriceRecord>(mPriceRecords);
        }
    }
    public class MutablePriceRecord : PriceRecord
    {
        public new DateTime Date { get { return base.Date; } set { base.Date = value; } }
        public new float Price { get { return base.Price; } set { base.Price = value; } }
        public new float FluctuationRate { get { return base.FluctuationRate; } set { base.FluctuationRate = value; } }

        public MutablePriceRecord(DateTime date, float price, float fluctuationRate) : base(date, price, fluctuationRate)
        {
            Date = date;
            Price = price;
            FluctuationRate = fluctuationRate;
        }
    }

    public class PriceRecord
    {
        public DateTime Date { get; protected set; }
        public float Price { get; protected set; }
        public float FluctuationRate { get; protected set; }

        public PriceRecord(DateTime date, float price, float fluctuationRate)
        {
            Date = date;
            Price = price;
            FluctuationRate = fluctuationRate;
        }
    }
    public class StockNews
    {
        public string Title { get; protected set; } = "";
        public string Description { get; protected set; } = "";
        public float AffectedStockPrice_IncreasePercent { get; protected set; }
        public float AffectedStockPrice_DecreasePercent { get; protected set; }
        public float AffectedStockPrice_SteadyPercent { get; protected set; }

        public StockNews(string title,
            string description,
            float affectedStockPrice_IncreasePercent,
            float affectedStockPrice_DecreasePercent,
            float affectedStockPrice_SteadyPercent)
        {
            Title = title;
            Description = description;
            AffectedStockPrice_IncreasePercent = affectedStockPrice_IncreasePercent;
            AffectedStockPrice_DecreasePercent = affectedStockPrice_DecreasePercent;
            AffectedStockPrice_SteadyPercent = affectedStockPrice_SteadyPercent;
        }
    }

    public class MutableStockNews : StockNews
    {
        public MutableStockNews(string title,
            string description,
            float affectedStockPrice_IncreasePercent,
            float affectedStockPrice_DecreasePercent,
            float affectedStockPrice_SteadyPercent) : base(title, description, affectedStockPrice_IncreasePercent, affectedStockPrice_DecreasePercent, affectedStockPrice_SteadyPercent)
        {
        }

        public new string Title { get { return base.Title; } set { base.Title = value; } }
        public new string Description { get { return base.Description; } set { base.Description = value; } }
        public new float AffectedStockPrice_IncreasePercent { get { return base.AffectedStockPrice_IncreasePercent; } set { base.AffectedStockPrice_IncreasePercent = value; } }
        public new float AffectedStockPrice_DecreasePercent { get { return base.AffectedStockPrice_DecreasePercent; } set { base.AffectedStockPrice_DecreasePercent = value; } }
        public new float AffectedStockPrice_SteadyPercent { get { return base.AffectedStockPrice_SteadyPercent; } set { base.AffectedStockPrice_SteadyPercent = value; } }
    }
}
