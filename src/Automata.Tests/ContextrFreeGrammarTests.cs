using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Automata;
using Microsoft.Automata.Grammars;
using System.Collections.Generic;


namespace Automata.Tests
{
    [TestClass]
    public class ContextFreeGrammarTests
    {
        [TestMethod]
        public void TestCFGParser()
        {
            var input = @"
START -> AS BS 
AS -> AS aa | aa | @ 
BS -> bbb BS | bbb";
            TestCFGParser_validate(input);
            var input2 = GrammarParser<int>.Parse(MapTerminalToMyToken, input).ToString();
            TestCFGParser_validate(input2);
        }

        void TestCFGParser_validate(string input)
        {
            ContextFreeGrammar cfg = GrammarParser<int>.Parse(MapTerminalToMyToken, input);
            Assert.AreEqual("START", cfg.StartSymbol.Name);
            Assert.AreEqual(3, cfg.Variables.Count);
            var terminals = new List<GrammarSymbol>(cfg.GetNonVariableSymbols());
            Assert.AreEqual(3, terminals.Count);
            var a = new Terminal<int>(1, "a");
            var b = new Terminal<int>(2, "b");
            var at = new Terminal<int>(0, "@");
            Assert.IsTrue(terminals.Contains(a));
            Assert.IsTrue(terminals.Contains(b));
            Assert.IsTrue(terminals.Contains(at));
            var productions = new List<Production>(cfg.GetProductions());
            Assert.AreEqual(6, productions.Count);
        }

        int MapTerminalToMyToken(char c)
        {
            if (c == 'a')
                return 1;
            else if (c == 'b')
                return 2;
            else
                return 0;
        }
    }
}
