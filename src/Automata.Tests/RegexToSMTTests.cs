using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

using Microsoft.Automata;

using System.Text.RegularExpressions;

namespace Microsoft.Automata.Tests
{
    [TestClass]
    public class RegexToSMTTests
    {
        //static string medium = @"(^(?:\w\:)?(?:/|\\\\){1}[^/|\\]*(?:/|\\){1})";
        static string large = @"^(([A-Za-z]+[^0-9]*)([0-9]+[^\W]*)([\W]+[\W0-9A-Za-z]*))|(([A-Za-z]+[^\W]*)([\W]+[^0-9]*)([0-9]+[\W0-9A-Za-z]*))|(([\W]+[^A-Za-z]*)([A-Za-z]+[^0-9]*)([0-9]+[\W0-9A-Za-z]*))|(([\W]+[^0-9]*)([0-9]+[^A-Za-z]*)([A-Za-z]+[\W0-9A-Za-z]*))|(([0-9]+[^A-Za-z]*)([A-Za-z]+[^\W]*)([\W]+[\W0-9A-Za-z]*))|(([0-9]+[^\W]*)([\W]+[^A-Za-z]*)([A-Za-z]+[\W0-9A-Za-z]*))$";
        
        [TestMethod]
        public void TestRegexBV16toSMT()
        {
            var conv = new RegexToSMTConverter(BitWidth.BV16, "CHAR");
            var res = conv.ConvertRegex(large);
            Console.WriteLine(res);
        }
    }
}
