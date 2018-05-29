using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.Automata;
using Microsoft.Automata.BooleanAlgebras;
using System.Text.RegularExpressions;

namespace Microsoft.Automata.Tests
{
    [TestClass]
    public class RegexAutomatonTests
    {
        //[TestMethod]
        //public void TestSimpleRegexAutomaton()
        //{
        //    var regex = new Regex("^a[bB]c$", RegexOptions.Singleline);
        //    var aut = regex.Compile2();
        //    Assert.IsTrue(aut.IsMatch("aBc"));
        //    Assert.IsTrue(aut.IsMatch("abc"));
        //    Assert.IsFalse(aut.IsMatch("abC"));
        //    Assert.IsTrue(aut.SymbolCount == 4);
        //    Assert.IsTrue(aut.StateCount == 5);
        //    Assert.IsTrue(aut.FinalSinkState == -1);
        //    Assert.IsTrue(aut.NonfinalSinkState != -1);
        //}
    }
}
