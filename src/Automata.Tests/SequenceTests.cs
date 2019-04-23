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

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Formatters.Soap;
using System.IO;
using System.Web;

using System.Linq;

namespace Automata.Tests
{
    [TestClass]
    public class SequenceTests
    {
        [TestMethod]
        public void TestSequence_Reverse()
        {
            var s = new Sequence<char>('a', 'b', 'c');
            var r = new Sequence<char>('c', 'b', 'a');
            Assert.AreEqual<string>("[a,b,c]", s.ToString());
            Assert.AreEqual<string>("[c,b,a]", r.ToString());
            Assert.AreEqual<Sequence<char>>(s, r.Reverse());
            Assert.AreEqual<Sequence<char>>(s, s.Reverse().Reverse());
        }

        [TestMethod]
        public void TestSequence_Rest()
        {
            var s = new Sequence<char>('a', 'b', 'c', 'd');
            var r2 = new Sequence<char>('c', 'd');
            var s2 = s.Rest().Rest();
            Assert.AreEqual<Sequence<char>>(s2, r2);
            Assert.AreEqual<Sequence<char>>(s2.Reverse(), r2.Reverse());
        }

        [TestMethod]
        public void TestSequence_MaximalCommonPrefix()
        {
            var s = new Sequence<char>('a', 'b', 'c', 'd', 'g');
            var r = new Sequence<char>('e', 'c', 'd', 'h', 'x', 'x');
            var s2 = s.Rest().Rest();
            var r2 = r.Rest();
            var p1 = s2.MaximalCommonPrefix(r2);
            var p2 = r2.MaximalCommonPrefix(s2);
            Assert.AreEqual<Sequence<char>>(p1, p2);
            Assert.AreEqual<string>("[c,d]", p1.ToString());
        }

        [TestMethod]
        public void TestSequence_Index()
        {
            var s = new Sequence<int>(2, 3, 0, 1, 2);
            var s2 = s.Rest().Rest();
            Assert.AreEqual<int>(0, s2[0]);
            Assert.AreEqual<int>(1, s2[1]);
        }

        [TestMethod]
        public void TestSequence_Append()
        {
            var s = new Sequence<int>(2, 3, 0, 1, 2);
            var s2 = s.Rest().Rest();
            var s3 = s.Rest().Rest().Rest();
            var app = s2.Append(s3);
            Assert.AreEqual<Sequence<int>>(new Sequence<int>(0, 1, 2, 1, 2), app);
        }

        [TestMethod]
        public void TestSequence_Suffix()
        {
            var s1 = new Sequence<int>(2, 3, 0, 1, 2);
            var s2 = new Sequence<int>(7, 8, 9, 0, 1, 2);
            var s1suff2 = s1.Suffix(2);
            var s2suff3 = s2.Suffix(3);
            Assert.IsTrue(s1suff2.Equals(s2suff3));
            Assert.IsFalse(s1suff2.Equals(s2.Suffix(2)));
            Assert.IsTrue(s1.Suffix(100).IsEmpty);
        }

        [TestMethod]
        public void TestSequence_EqAllButOne()
        {
            var s1 = new Sequence<int>(0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            var s2 = new Sequence<int>(0, 1, 2, 3, 4, 0, 6, 7, 8, 9);
            var s3 = new Sequence<int>(0, 1, 2, 3, 4, 0, 6, 7, 0, 9);
            Assert.AreEqual<int>(5, s1.EqAllButOne(s2));
            Assert.AreEqual<int>(5, s2.EqAllButOne(s1));
            Assert.AreEqual<int>(8, s2.EqAllButOne(s3));
            Assert.AreEqual<int>(8, s3.EqAllButOne(s2));  //symmetric
            Assert.AreEqual<int>(-1, s1.EqAllButOne(s3)); //not transitive
            Assert.AreEqual<int>(-1, s1.EqAllButOne(s1)); //irreflexive
        }

        [TestMethod]
        public void TestSequence_TrueForAll()
        {
            var s = new Sequence<int>(0, 2, 44, 6, 8, 22);
            Assert.IsTrue(s.TrueForAll(x => x >= 0));
            Assert.IsFalse(s.TrueForAll(x => x <= 7));
            Assert.IsTrue(s.TrueForAll(x => (x % 2 == 0)));
        }

        [TestMethod]
        public void TestSequence_Exists()
        {
            var s = new Sequence<int>(0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            Assert.IsTrue(s.Exists(x => x <= 1));
            Assert.IsFalse(s.Suffix(2).Exists(x => x <= 1));
        }

        [TestMethod]
        public void TestSequence_First()
        {
            var s = new Sequence<int>(0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            Assert.AreEqual<int>(4, s.Suffix(4).First);
        }
    }
}
