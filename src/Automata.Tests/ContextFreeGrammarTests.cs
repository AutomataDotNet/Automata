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
        public void TestCFG_Parser()
        {
            var input = @"
START -> AS BS 
AS -> AS a a | a a | @ 
BS -> b b b BS | b b b";
            TestCFGParser_validate(input);
            var input2 = GrammarParser<string>.Parse(MapTerminalToDummyAutomaton, input).ToString();
            TestCFGParser_validate(input2);
        }

        void TestCFGParser_validate(string input)
        {
            ContextFreeGrammar cfg = GrammarParser<string>.Parse(MapTerminalToDummyAutomaton, input);
            Assert.AreEqual("START", cfg.StartSymbol.Name);
            Assert.AreEqual(3, cfg.Variables.Count);
            var terminals = new List<GrammarSymbol>(cfg.GetNonVariableSymbols());
            Assert.AreEqual(3, terminals.Count);
            var a = new Terminal<string>("a", "a");
            var b = new Terminal<string>("b", "b");
            var at = new Terminal<string>("@", "@");
            Assert.IsTrue(terminals.Contains(a));
            Assert.IsTrue(terminals.Contains(b));
            Assert.IsTrue(terminals.Contains(at));
            var productions = new List<Production>(cfg.GetProductions());
            Assert.AreEqual(6, productions.Count);
        }

        [TestMethod]
        public void TestCFG_Overlaps()
        {
            var input = 
@"S -> \( S \) | [abx] [bxd] [xde] 
";
            ContextFreeGrammar cfg = GrammarParser<BDD>.Parse(ContextFreeGrammar.GetContext().CharSetProvider.Convert, input);
            Assert.AreEqual("S", cfg.StartSymbol.Name);
            Assert.AreEqual(1, cfg.Variables.Count);
            var productions = new List<Production>(cfg.GetProductions());
            Assert.AreEqual(2, productions.Count);
            Assert.IsFalse(cfg.IsInGNF());

            var aut = ContextFreeGrammar.GetContext().Convert(@"^\((xx)+\)$").RemoveEpsilons();
            BDD[] witness;
            var no = cfg.Overlaps<BDD>(aut, out witness);
            Assert.IsFalse(no);

            var aut2 = ContextFreeGrammar.GetContext().Convert(@"^\(x+\)$").RemoveEpsilons();
            BDD[] witness2 = null;
            var yes = cfg.Overlaps<BDD>(aut2, out witness2);
            Assert.IsTrue(yes);
            string concrete_witness = new string(Array.ConvertAll(witness2, GetChar));
            Assert.AreEqual<string>("(xxx)", concrete_witness);
        }

        [TestMethod]
        public void TestCFG_Intersect()
        {
            //same as above but with a regex as a single terminal because there are no spaces between the sub-terminals
            var input =
@"S -> \( S \) | [abx][bxd][xde] 
";
            ContextFreeGrammar cfg = GrammarParser<BDD>.Parse(ContextFreeGrammar.GetContext().CharSetProvider.Convert, input);
            Assert.AreEqual("S", cfg.StartSymbol.Name);
            //automaton has 4 states, so there are 4 extra nonterminals
            Assert.AreEqual(5, cfg.Variables.Count);
            var productions = new List<Production>(cfg.GetProductions());
            Assert.AreEqual(6, productions.Count);

            var w = cfg.Intersect(@"^\((xx)+\)$");
            Assert.IsNull(w);

            var w2 = cfg.Intersect(@"^\(x+\)$");
            Assert.AreEqual<string>("(xxx)",w2);
        }

        [TestMethod]
        public void TestCFG_Intersect_Nontrivial()
        {
            var input =@"S -> \( S \) | \w{3} | \d{2,}";
            ContextFreeGrammar cfg = ContextFreeGrammar.Parse(input);

            var w = cfg.Intersect(@"^\((xy)+\)$");
            Assert.IsNull(w);

            var w2 = cfg.Intersect(@"^\({4}[0-9]+\)+$");
            Assert.AreEqual<string>("((((000))))", w2);
        }

        BDD MkPred(string c)
        {
            return ContextFreeGrammar.GetContext().CharSetProvider.MkCharSetFromRegexCharClass(c);
        }

        Automaton<BDD> MkAutomaton(string regex)
        {
            return ContextFreeGrammar.GetContext().CharSetProvider.Convert(regex);
        }

        char GetChar(BDD pred)
        {
            return (char)ContextFreeGrammar.GetContext().CharSetProvider.GetMin(pred);
        }

        Automaton<string> MapTerminalToDummyAutomaton(string s)
        {
            //eliminate dummy anchors
            var c = s.Substring(2, 1);
            var aut = Automaton<string>.Create(null, 0, new int[] { 1 }, new Move<string>[] { Move<string>.Create(0, 1, c) });
            return aut;
        }
    }
}
