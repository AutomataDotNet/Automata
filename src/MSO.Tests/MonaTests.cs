using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Automata.MSO.Mona;

namespace MSO.Tests
{
    [TestClass]
    public class MonaTests
    {
        [TestMethod]
        public void MonaTest1()
        {
            string input = @"
m2l-str;

# fo vars p, q and r
var1 p, q, r; 
var2 $;
p = q + 1; # p is the successor of q
p < r; /* r is after p */
p in $ & q notin $;
";
            Program pgm = MonaParser.Parse(input);
            Assert.AreEqual<int>(5, pgm.declarations.Count);
        }
    }
}
