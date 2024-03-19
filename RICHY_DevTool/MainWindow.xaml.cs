using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace RICHY_DevTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }


    public class StockPriceController
    {
        private static Random spcRandom = new Random();

        private const float INCREASE_PERCENTAGE = 33.33f;
        private const float DECREASE_PERCENTAGE = 33.33f;
        private const float STEADY_PERCENTAGE = 33.33f;

        private float mFluctuationRateMaximum = 10f;

        public StockPriceController(float fluctuationRateMaximum)
        {
            mFluctuationRateMaximum = fluctuationRateMaximum;
        }

        public void CalculateNewPriceForNextDay(Stock targetStock)
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
            switch (SelectFromPercentages(increasePer, decreasePer, steadyPer))
            {
                // increase case
                case 0:
                    {
                        targetStock.Price = (float)(targetStock.Price * (1 + fluctuationRate));
                        break;
                    }
                // decrease case
                case 1:
                    {
                        targetStock.Price = (float)(targetStock.Price * (1 - fluctuationRate));
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
    }

    public class Stock
    {
        public string Name { get; set; } = "";
        public float Price { get; set; } = 10000;
        public Collection<StockNews> News { get; set; } = new Collection<StockNews>();

    }

    public class StockNews
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public float AffectedStockPrice_IncreasePercent { get; set; }
        public float AffectedStockPrice_DecreasePercent { get; set; }
        public float AffectedStockPrice_SteadyPercent { get; set; }
    }
}
