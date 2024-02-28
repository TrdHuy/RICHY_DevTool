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
            var now = DateTime.Now.Date;
            Assert.Pass();
        }


        [Test]
        public void TestReadOnlyCollection()
        {
            Collection<int> testCollection = new Collection<int> { 1, 2, 3 };
            var x = testCollection.IndexOf(2);
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

        [Test]
        public void TestClassTest()
        {
            Class1 a = new Class1();
            a.calCulate();
        }
    }
}