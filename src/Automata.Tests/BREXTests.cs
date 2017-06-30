using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Automata;
using System.Text.RegularExpressions;

namespace Automata.Tests
{
    [TestClass]
    public class BREXTests
    {
        [TestMethod]
        public void TestBREXLikeExpressionSmall()
        {
            var man = new BREXManager();  
            var like1 = man.MkLike(@"%[ab]_____");
            var like2 = man.MkLike(@"%[bc]_____");
            var and = man.MkAnd(like1, like2);
            var dfa = and.Optimize();
            var like = man.MkLike(@"%b_____");
            var dfa2 = like.Optimize();
            var equiv = dfa.IsEquivalentWith(dfa2);
            Assert.IsTrue(equiv);
        }

        //fails due to timeout
        [TestMethod]
        public void TestBREXLikeExpressionMedium()
        {
            try
            {
                var man = new BREXManager();
                var like1 = man.MkLike(@"%[ab]________");
                var like2 = man.MkLike(@"%[bc]________");
                var and = man.MkAnd(like1, like2);
                var dfa = and.Optimize();
            }
            catch (AutomataException e)
            {
                Assert.IsTrue(e.kind == AutomataExceptionKind.AutomataConversionFailed);
            }
        }

        //fails due to too many states
        [TestMethod]
        public void TestBREXLikeExpressionTooManyStates()
        {
            try
            {
                var man = new BREXManager();
                var like = man.MkLike(@"%[ab]_________");
                var dfa = like.Optimize();
            }
            catch (AutomataException e)
            {
                Assert.IsTrue(e.kind == AutomataExceptionKind.AutomataConversionFailed);
            }
        }

        //fails due to too many states
        [TestMethod]
        public void TestBREXLikeExpressionOK()
        {

            var man = new BREXManager("Matcher", "FString", 2000);
            var like = man.MkLike(@"%[ab]_________");
            var dfa = like.Optimize();
            Assert.IsTrue(dfa.StateCount > 1000 && dfa.StateCount <= 2000);
        }

        [TestMethod]
        public void TestBREXLikeExpressionLargeTimeout()
        {
            var man = new BREXManager("Matcher","FString",int.MaxValue);
            var like1 = man.MkLike(@"%[ab]_____________");
            var like2 = man.MkLike(@"%[bc]_____________");
            var and = man.MkAnd(like1, like2);
            try
            {
                var dfa = and.Optimize();
            }
            catch (AutomataException e)
            {
                Assert.IsTrue(e.kind == AutomataExceptionKind.AutomataConversionFailed);
            }
        }

        [TestMethod]
        public void TestBREXLikeExpressionLargeTooManyStates()
        {
            var man = new BREXManager();
            var like = man.MkLike(@"%[ab]_____________");
            try
            {
                var dfa = like.Optimize();
            }
            catch (AutomataException e)
            {
                Assert.IsTrue(e.kind == AutomataExceptionKind.AutomataConversionFailed);
            }
        }


    }
}
