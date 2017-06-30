using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Automata;

namespace Microsoft.Automata.Tests
{
    [TestClass]
    public class LikePatternTests
    {
        [TestMethod]
        public void TestSampleLikeExpressions()
        {
            TestCorrectnessOfLike("a","^a$");
            TestCorrectnessOfLike("%a", "a$");
            TestCorrectnessOfLike("a%", "^a");
            TestCorrectnessOfLike("a%b", "^a.*b$");
            TestCorrectnessOfLike("%", ".*");
            TestCorrectnessOfLike("%", "");
            TestCorrectnessOfLike("[a-z]", "^[a-z]$");
            TestCorrectnessOfLike("____", "^.{4}$");
            TestCorrectnessOfLike("a_b_c", "^a.b.c$");
        }

        private static void TestCorrectnessOfLike(string like, string regex)
        {
            BREXManager bm = new BREXManager();
            var a = bm.MkLike(like);
            var A = a.Optimize();
            var b = bm.MkRegex(regex, System.Text.RegularExpressions.RegexOptions.Singleline);
            var B = b.Optimize();
            var eq = A.IsEquivalentWith(B);
            Assert.IsTrue(eq);
        }
    }
}
