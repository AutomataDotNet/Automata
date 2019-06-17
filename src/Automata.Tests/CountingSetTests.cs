using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

using Microsoft.Automata;
using Microsoft.Automata.Generated;
using Microsoft.Automata.Rex;
using Microsoft.Automata.Utilities;

using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Security.Cryptography;

namespace Automata.Tests
{
    [TestClass]
    public class CountingSetTests
    {
        [TestMethod]
        public void TestBasicCountingSet_Empty()
        {
            var set = new BasicCountingSet(2);
            Assert.IsTrue(set.IsEmpty);
            Assert.IsTrue(!set.IsSingleton);
            Assert.IsTrue(set.UpperBound == 2);
            Assert.IsTrue(set.ToString() == "[]");
        }

        [TestMethod]
        public void TestBasicCountingSet_Set0()
        {
            var set = new BasicCountingSet(10);
            set.Set0();
            Assert.IsTrue(!set.IsEmpty);
            Assert.IsTrue(set.IsSingleton);
            Assert.IsTrue(set.UpperBound == 10);
            Assert.IsTrue(set.Max == 0);
            Assert.IsTrue(set.ToString() == "[0]");
        }

        [TestMethod]
        public void TestBasicCountingSet_Set1()
        {
            var set = new BasicCountingSet(100);
            set.Set1();
            Assert.IsTrue(!set.IsEmpty);
            Assert.IsTrue(set.IsSingleton);
            Assert.IsTrue(set.UpperBound == 100);
            Assert.IsTrue(set.Max == 1);
            Assert.IsTrue(set.ToString() == "[1]");
        }

        [TestMethod]
        public void TestBasicCountingSet_Inrc()
        {
            var set = new BasicCountingSet(3);
            set.Set0();
            Assert.IsTrue(!set.IsEmpty);
            Assert.IsTrue(set.IsSingleton);
            Assert.IsTrue(set.ToString() == "[0]");
            set.Incr();
            Assert.IsTrue(!set.IsEmpty);
            Assert.IsTrue(set.IsSingleton);
            Assert.IsTrue(set.ToString() == "[1]");
            set.Incr();
            Assert.IsTrue(!set.IsEmpty);
            Assert.IsTrue(set.IsSingleton);
            Assert.IsTrue(set.ToString() == "[2]");
            set.Incr();
            Assert.IsTrue(!set.IsEmpty);
            Assert.IsTrue(set.IsSingleton);
            Assert.IsTrue(set.ToString() == "[3]");
            set.Incr();
            Assert.IsTrue(set.IsEmpty);
            Assert.IsTrue(!set.IsSingleton);
            Assert.IsTrue(set.ToString() == "[]");
        }

        [TestMethod]
        public void TestBasicCountingSet_InrcPush()
        {
            var set = new BasicCountingSet(7);
            set.Set0();
            set.IncrPush0();
            Assert.IsTrue(!set.IsEmpty);
            Assert.IsTrue(!set.IsSingleton);
            Assert.IsTrue(set.ToString() == "[1,0]");
            set.Incr();
            set.Incr();
            Assert.IsTrue(set.ToString() == "[3,2]");
            Assert.IsTrue(set.Max == 3);
            set.IncrPush1();
            Assert.IsTrue(set.ToString() == "[4,3,1]");
            Assert.IsTrue(set.Max == 4);
            set.IncrPush01();
            Assert.IsTrue(set.ToString() == "[5,4,2,1,0]");
            Assert.IsTrue(set.Max == 5);
            set.IncrPush01();
            Assert.IsTrue(set.ToString() == "[6,5,3,2,1,0]");
            Assert.IsTrue(set.Max == 6);
            set.Incr();
            Assert.IsTrue(set.ToString() == "[7,6,4,3,2,1]");
            Assert.IsTrue(set.Max == 7);
            set.Incr();
            Assert.IsTrue(set.ToString() == "[7,5,4,3,2]");
            Assert.IsTrue(set.Max == 7);
            set.Incr();
            Assert.IsTrue(set.ToString() == "[6,5,4,3]");
            Assert.IsTrue(set.Max == 6);
            Assert.IsTrue(set.Min == 3);
            set.IncrPush0();
            Assert.IsTrue(set.ToString() == "[7,6,5,4,0]");
            Assert.IsTrue(set.Max == 7);
            set.Incr();
            Assert.IsTrue(set.ToString() == "[7,6,5,1]");
            Assert.IsTrue(set.Max == 7);
            set.Incr();
            Assert.IsTrue(set.ToString() == "[7,6,2]");
            Assert.IsTrue(set.Max == 7);
            Assert.IsTrue(set.Min == 2);
            set.Incr();
            Assert.IsTrue(set.ToString() == "[7,3]");
            Assert.IsTrue(set.Max == 7);
            set.Incr();
            Assert.IsTrue(set.ToString() == "[4]");
            Assert.IsTrue(set.Max == 4);
            Assert.IsTrue(set.IsSingleton);
            set.Incr();
            set.Incr();
            set.Incr();
            Assert.IsTrue(set.ToString() == "[7]");
            Assert.IsTrue(set.Max == 7);
            Assert.IsTrue(set.IsSingleton);
            set.IncrPush01();
            Assert.IsTrue(!set.IsEmpty);
            Assert.IsTrue(set.ToString() == "[1,0]");
            set.Incr();
            set.Incr();
            set.Incr();
            set.Incr();
            set.Incr();
            set.Incr();
            set.Incr();
            Assert.IsTrue(set.ToString() == "[7]");
            Assert.IsTrue(set.Max == 7);
            Assert.IsTrue(set.Min == 7);
            Assert.IsTrue(set.IsSingleton);
            set.Incr();
            Assert.IsTrue(set.IsEmpty);
        }
    }
}
