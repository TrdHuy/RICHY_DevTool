using RICHY_DevTool;
using RICHYEngine;
using System.Collections.ObjectModel;

namespace RICHY_DevToolTest
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var spcRandom = new Random();
            var fluctuationRate = (double)(spcRandom.Next(1000) / 100d);

            var t = new StockPriceController(10f);
            var testStock = new MutableStock("TST",10000f)
            {
                Name = "TST",
                News = new Collection<StockNews> {
                    new StockNews()
                    {
                        AffectedStockPrice_DecreasePercent = -30f,
                        AffectedStockPrice_SteadyPercent = -30f,
                        AffectedStockPrice_IncreasePercent = 60f,
                    },
                },
            };

            StockPriceController.SelectFromPercentages(33.3f, 33.3f, 33.3f);

            t.CalculateNewPriceForNextDay(testStock);
            t.CalculateNewPriceForNextDay(testStock);
            t.CalculateNewPriceForNextDay(testStock);
            t.CalculateNewPriceForNextDay(testStock);
            t.CalculateNewPriceForNextDay(testStock);
            t.CalculateNewPriceForNextDay(testStock);
            t.CalculateNewPriceForNextDay(testStock);
            t.CalculateNewPriceForNextDay(testStock);
            t.CalculateNewPriceForNextDay(testStock);

            Assert.Pass();
        }


        [Test]
        public void TestReadOnlyCollection()
        {
            Collection<int> testCollection = new Collection<int> { 1, 2, 3 };
            var x  = testCollection.IndexOf(2);
            ReadOnlyCollection<int> testReadOnly = new ReadOnlyCollection<int>(testCollection);
            testCollection.Add(4);
            Assert.IsTrue(testReadOnly[3] == 4);
        }

        class Animal { }
        class Dog : Animal { }

        [Test]
        public void TestCovariance()
        {
            Collection<Dog> collectionDogs = new Collection<Dog>();

        }
    }
}