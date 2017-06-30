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
    }
}
