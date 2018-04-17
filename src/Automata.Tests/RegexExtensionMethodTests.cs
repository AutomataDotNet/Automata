using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Automata;
using Microsoft.Automata.Rex;

namespace Automata.Tests
{
    [TestClass]
    public class RegexExtensionMethodTests
    {
        static string regexesFile = "../../../regexes.txt";

        [TestMethod]
        public void TestRegexCompile()
        {
            Regex[] regexes = Array.ConvertAll(File.ReadAllLines(regexesFile), x => new Regex(x));
            for (int i = 0; i < regexes.Length && i < 50; i++)
            {
                var regex = regexes[i];
                var rex = new RexEngine(BitWidth.BV16);
                var aut = rex.CreateFromRegexes(regex.ToString());
                var comp = regex.Compile(100000);
                if (comp == null)
                    continue;
                foreach (var s in rex.GenerateMembers(aut, 10))
                {
                    Assert.IsTrue(regex.IsMatch(s));
                    Assert.IsTrue(comp.IsMatch(s));
                }
            }
        }

        [TestMethod]
        public void TestRegexGenerateCpp()
        {
            Regex[] regexes = Array.ConvertAll(File.ReadAllLines(regexesFile), x => new Regex(x));
            var brexman = new BREXManager("Matcher", "FString", 0, 0);
            for (int i = 0; i < regexes.Length && i < 50; i++)
            {
                var regex = regexes[i];
                var cpp = regex.GenerateCpp();
                Assert.IsNotNull(cpp);
            }
        }

        [TestMethod]
        public void TestRegexGenerateRandomDataset()
        { 
            var regex1 = new Regex(@"^bcd$");
            var set1 = regex1.GenerateRandomDataSet(100);
            Assert.IsTrue(set1.Count == 1, "Incorrect Dataset");
            var regex1a = new Regex(@"^b(c|C)d$");
            var set1a = regex1a.GenerateRandomDataSet(100);
            Assert.IsTrue(set1a.Count == 2, "Incorrect Dataset");
            var regex1b = new Regex(@"(?i:^bcd$)");
            var set1b = regex1b.GenerateRandomDataSet(100);
            Assert.IsTrue(set1b.Count == 8, "Incorrect Dataset");
            var regex1c = new Regex(@"(?i:^bcd$)");
            var set1c = regex1c.GenerateRandomDataSet(100,"[bcCdD]");
            Assert.IsTrue(set1c.Count == 4, "Incorrect Dataset");
            //----
            var regex2 = new Regex(@"^(\d[\w-[\d]])+$");
            var set2 = regex2.GenerateRandomDataSet(100,"[012a-d]", 20);
            Assert.IsTrue(set2.Count == 100);
            foreach (string s in set2)
                Assert.IsTrue(Regex.IsMatch(s, "^([0-2][a-d])+$"), "Incorrect Dataset");
        }

        [TestMethod]
        public void TestRegexGenerateRandomDatasetFromComplement()
        {
            var regex1 = new Regex(@"^bcd$").Complement();
            var set1 = regex1.GenerateRandomDataSet(100);
            Assert.IsTrue(set1.Count == 100, "Incorrect Dataset");
            //----
            var regex2 = new Regex(@"^(\d[\w-[\d]])+$").Complement();
            var set2 = regex2.GenerateRandomDataSet(100);
            Assert.IsTrue(set2.Count == 100);
            foreach (string s in set2)
                Assert.IsTrue(!Regex.IsMatch(s, @"^(\d[\w-[\d]])+$"), "Incorrect Dataset");
        }

        [TestMethod]
        public void TestRegexGenerateRandomMember()
        {
            var regex = new Regex(@"^(\d[\w-[\d]])+$");
            for (int i = 0; i < 100; i++)
            {
                var s = regex.GenerateRandomMember("[01ab]");
                Assert.IsTrue(Regex.IsMatch(s, "^([01][ab])+$"));
                s = regex.GenerateRandomMember("[9a-z]");
                Assert.IsTrue(Regex.IsMatch(s, "^(9[a-z])+$"));
            }
        }
    }
}
