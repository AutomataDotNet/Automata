using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Globalization;

namespace Automata.Tests
{
    [TestClass]
    public class MiscStringOperationTests
    {
        [TestMethod]
        public void TestCultureVariance()
        {
            Assert.IsTrue("ss".StartsWith("ß", false, CultureInfo.InvariantCulture));
            Assert.IsFalse("ss".StartsWith("ß", StringComparison.Ordinal));
            Assert.IsTrue("ss".StartsWith("ß", false, CultureInfo.CreateSpecificCulture("se")));
            Assert.IsFalse("æ".EndsWith("ae", false, CultureInfo.CreateSpecificCulture("se")));
            Assert.IsTrue("æ".EndsWith("ae", false, CultureInfo.InvariantCulture));

            var ci = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("se");
            Assert.IsFalse("æ".EndsWith("ae"));
            Assert.AreEqual<int>(-1, "æ".IndexOf("ae"));
            Thread.CurrentThread.CurrentCulture = ci;
            Assert.IsTrue("æ".EndsWith("ae"));
            Assert.AreEqual<int>(0, "æ".IndexOf("ae"));

            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("tr");
            string indigo_tr = "indigo".ToUpper();
            string indigo_tr1 = "indigo".ToUpper(CultureInfo.InvariantCulture);
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            string indigo = "indigo".ToUpper();
            Assert.IsFalse(indigo_tr == indigo);
            Assert.IsTrue(indigo_tr1 == indigo);
            var s = "ß";
            var ss = "ss";
            var SS = "SS";
            Assert.IsTrue(char.IsLower('ß'));
            Assert.IsFalse(char.IsUpper('ß'));
            Assert.IsTrue(s.StartsWith(ss));
            Assert.IsFalse(s.ToUpper().StartsWith(ss.ToUpper()));
            Assert.IsTrue(ss.StartsWith(s));
            Assert.IsFalse(ss.ToUpper().StartsWith(s.ToUpper()));
            Assert.IsTrue(ss.ToUpper().StartsWith(SS));
            Assert.IsTrue(s.ToUpper().StartsWith(s));
            var ae = "ae";
            var AE = "AE";
            var A = "Æ";
            var a = "æ";
            Assert.IsTrue(a.ToUpper().StartsWith(A));
            Assert.IsTrue(A.ToLower().StartsWith(a));
            Assert.IsTrue(ae.ToUpper().StartsWith(AE));
            Assert.IsTrue(AE.ToLower().StartsWith(ae));
            Assert.IsTrue(A.ToLower().StartsWith(ae));
            Assert.IsTrue(a.ToUpper().StartsWith(AE));
            Assert.IsFalse(a.ToUpper().StartsWith(AE, StringComparison.Ordinal));

            Thread.CurrentThread.CurrentCulture = ci;
        }
    }
}
