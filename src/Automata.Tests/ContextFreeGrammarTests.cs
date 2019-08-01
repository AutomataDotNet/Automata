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
        public void TestCFG_Parser_WithDummyAutomaton()
        {
            var input = @"
START -> AS BS 
AS -> AS (a) (a) | (a) (a) | (@)
BS -> (b) (b) (b) BS | (b) (b) (b)";
            TestCFGParser_validate(input);
            var input2 = GrammarParser<string>.Parse(MapTerminalToDummyAutomaton, input).ToString();
            TestCFGParser_validate(input2);
        }

        void TestCFGParser_validate(string input)
        {
            ContextFreeGrammar cfg = GrammarParser<string>.Parse(MapTerminalToDummyAutomaton, input);
            Assert.IsNull(cfg.BuiltinTerminalAlgebra);
            Assert.AreEqual("START", cfg.StartSymbol.Name);
            Assert.AreEqual(3, cfg.Nonterminals.Count);
            var terminals = new List<GrammarSymbol>(cfg.GetTerminals());
            Assert.AreEqual(3, terminals.Count);
            var a = new Terminal<string>("(a)");
            var b = new Terminal<string>("(b)");
            var at = new Terminal<string>("(@)");
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
            ContextFreeGrammar cfg = GrammarParser<BDD>.Parse(MkAutomaton, input);
            Assert.IsNotNull(cfg.BuiltinTerminalAlgebra);
            Assert.AreEqual("S", cfg.StartSymbol.Name);
            Assert.AreEqual(1, cfg.NonterminalCount);
            Assert.AreEqual(2, cfg.ProductionCount);
            Assert.IsFalse(cfg.IsInGNF());

            var aut = MkAutomaton(@"\((xx)+\)");
            BDD[] witness;
            var no = cfg.Overlaps<BDD>(aut, out witness);
            Assert.IsFalse(no);

            var aut2 = MkAutomaton(@"\(x+\)");
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
            //but outcome must be equivalent
            var input =
@"S -> \( S \) | [abx][bxd][xde] 
";
            ContextFreeGrammar cfg = GrammarParser<BDD>.Parse(MkAutomaton, input);
            Assert.AreEqual("S", cfg.StartSymbol.Name);
            Assert.AreEqual(1, cfg.NonterminalCount);
            Assert.AreEqual(2, cfg.ProductionCount);

            string w;
            Assert.IsFalse(cfg.IntersectsWith(@"^\((xx)+\)$", out w));
            Assert.IsTrue(cfg.IntersectsWith(@"^\(x+\)$", out w));
            Assert.AreEqual<string>("(xxx)", w);
        }

        [TestMethod]
        public void TestCFG_EmptyRHS()
        {
            var input = "S -> (x) S (y) | () | (a)";
            ContextFreeGrammar cfg = ContextFreeGrammar.Parse(input);
            TestCFG_EmptyRHS_check(cfg);
            ContextFreeGrammar cfg_ = ContextFreeGrammar.Parse(cfg.ToString());
            TestCFG_EmptyRHS_check(cfg_);
        }

        [TestMethod]
        public void TestCFG_CapitalLetterNonterminalHandling()
        {
            //enclose upper case letters in paranthesis when printed in production rules
            //otherwise they will be considered as nonterminals
            //paranthesis themselves must be escaped to be considered as terminals
            var input = @"S -> \( S \) | (A)";
            var cfg = ContextFreeGrammar.Parse(input);
            Assert.AreEqual<int>(2, cfg.ProductionCount);
            Assert.AreEqual<int>(1, cfg.NonterminalCount);
            Assert.AreEqual<int>(3, cfg.TerminalCount);
            var terminal_preds = Array.ConvertAll(cfg.Terminals.ToArray(), t => ((Terminal<BDD>)t).term);
            var terminals = new List<char>(Array.ConvertAll(terminal_preds, bdd => (char)bdd.GetMin()));
            Assert.AreEqual<int>(3, terminals.Count);
            Assert.IsTrue(terminals.Contains('A'));
            Assert.IsTrue(terminals.Contains('('));
            Assert.IsTrue(terminals.Contains(')'));
            //check also nonemptiness of the pda
            var pda = cfg.ToPDA<BDD>();
            BDD[] w;
            Assert.IsTrue(pda.IsNonempty(out w));
            string s = cfg.BuiltinTerminalAlgebra.ChooseString(w);
            if (s.Length == 1)
                Assert.AreEqual<string>("A", s);
            else if (s.Length == 2)
                Assert.AreEqual<string>("()", s);
            else if (s.Length == 3)
                Assert.AreEqual<string>("(A)", s);
            Assert.IsTrue(pda.Intersect(cfg.BuiltinTerminalAlgebra.Convert("^...$")).IsNonempty(out w));
            Assert.AreEqual<string>("(A)", cfg.BuiltinTerminalAlgebra.ChooseString(w));
        }


        public void TestCFG_EmptyRHS_check(ContextFreeGrammar cfg)
        {
            Assert.AreEqual("S", cfg.StartSymbol.Name);
            Assert.AreEqual(1, cfg.NonterminalCount);
            Assert.AreEqual(3, cfg.ProductionCount);

            string w;
            Assert.IsFalse(cfg.IntersectsWith(@"^x+a$", out w));
            Assert.IsTrue(cfg.IntersectsWith(@"^x+ay+$", out w));
            Assert.AreEqual<string>("xay", w);
        }

        [TestMethod]
        public void TestCFG_Intersect_Nontrivial()
        {
            var input = @"S -> \( S \) | [\w-[\d]]{3} | 0{2,}";
            ContextFreeGrammar cfg = ContextFreeGrammar.Parse(input);

            string w;
            Assert.IsFalse(cfg.IntersectsWith(@"^\((xy)+\)$", out w));
            Assert.IsTrue(cfg.IntersectsWith(@"^\({4}x+\)+$", out w));
            Assert.AreEqual<string>("((((xxx))))", w);
            Assert.IsTrue(cfg.IntersectsWith(@"^\(+0+\){7}$", out w));
            Assert.AreEqual<string>("(((((((00)))))))", w);
        }

        [TestMethod]
        public void TestCFG_MultipleRegexesAsTerminals()
        {
            var input = @"S -> \( S \) | \x20 | (cd) | (a+b+)";
            ContextFreeGrammar cfg = ContextFreeGrammar.Parse(input);
            Assert.IsTrue(cfg.NonterminalCount == 5);
            string w;
            Assert.IsTrue(cfg.IntersectsWith(@"\)", out w));
            Assert.AreEqual<string>("( )", w);
        }

        [TestMethod]
        public void TestCFG_IntersectionWithRegexOptions()
        {
            var input = @"S -> \( S \) | (a)";
            ContextFreeGrammar cfg = ContextFreeGrammar.Parse(input);

            int k = 5;
            string w;
            Assert.IsTrue(cfg.IntersectsWith(@"a.{" + k + "}", out w));

            var s = "";
            for (int i = 0; i < k; i++)
                s += "(";
            s += "a";
            for (int i = 0; i < k; i++)
                s += ")";

            Assert.AreEqual<string>(s, w);

            //determinization causes blowup of states in this case
            //will not terminate if determinization of regex is set to true, for large k
            Assert.IsTrue(cfg.IntersectsWith(@"a.{" + k + "}", out w, true));

            Assert.AreEqual<string>(s, w);
        }

        BDD MkPred(string c)
        {
            return ContextFreeGrammar.GetContext().CharSetProvider.MkCharSetFromRegexCharClass(c);
        }

        Automaton<BDD> MkAutomaton(string regex)
        {
            var regex_with_anchors = "^(" + regex + ")$";
            return ContextFreeGrammar.GetContext().CharSetProvider.Convert(regex_with_anchors).RemoveEpsilons();
        }

        char GetChar(BDD pred)
        {
            return (char)ContextFreeGrammar.GetContext().CharSetProvider.GetMin(pred);
        }

        Automaton<string> MapTerminalToDummyAutomaton(string s)
        {
            //eliminate dummy anchors
            var aut = Automaton<string>.Create(null, 0, new int[] { 1 }, new Move<string>[] { Move<string>.Create(0, 1, s) });
            return aut;
        }

        [TestMethod]
        public void TestCFG_PDAConstruction()
        {
            var input = "S -> (x) (y)";
            var cfg = ContextFreeGrammar.Parse(input);
            var pda = cfg.ToPDA<BDD>();
            var aut = ContextFreeGrammar.GetContext().Convert("xy");
            var pda2 = pda.Intersect(aut);
            BDD[] w;
            Assert.IsTrue(pda2.IsNonempty(out w));
            string s = cfg.BuiltinTerminalAlgebra.ChooseString(w);
            Assert.AreEqual<string>("xy", s);
        }

        [TestMethod]
        public void TestCFG_PDAConstruction_IntersectMultipleRegexes()
        {
            var input = @"S -> \( S \) | (A) | (BC)+ ";
            var cfg = ContextFreeGrammar.Parse(input);
            var pda = cfg.ToPDA<BDD>();
            var aut_contains_2LPs = cfg.BuiltinTerminalAlgebra.Convert(@"\({2}").RemoveEpsilons();
            var aut_contains_two_Bs = cfg.BuiltinTerminalAlgebra.Convert("B.*B").RemoveEpsilons();
            var aut_contains_A = cfg.BuiltinTerminalAlgebra.Convert("A").RemoveEpsilons();
            var pda2 = pda.Intersect(aut_contains_2LPs, aut_contains_two_Bs);
            BDD[] w;
            Assert.IsTrue(pda2.IsNonempty(out w));
            string s = cfg.BuiltinTerminalAlgebra.ChooseString(w);
            Assert.AreEqual<string>("((BCBC))", s);
            //---
            var pda3 = pda.Intersect(aut_contains_2LPs, aut_contains_two_Bs, aut_contains_A);
            Assert.IsFalse(pda3.IsNonempty(out w));
            //---
            var pda4 = pda.Intersect(aut_contains_2LPs, aut_contains_A);
            Assert.IsTrue(pda4.IsNonempty(out w));
            s = cfg.BuiltinTerminalAlgebra.ChooseString(w);
            Assert.AreEqual<string>("((A))", s);
        }

        [TestMethod]
        public void TestCFG_PDAConstruction_IntersectMultipleRegexes_2()
        {
            var input = @"S -> \( S \) | (A) | (BC)+ ";
            var cfg = ContextFreeGrammar.Parse(input);
            string w;
            //---
            Assert.IsTrue(cfg.IntersectsWith(new string[] { @"\({2}", "B.*B" }, out w));
            Assert.AreEqual<string>("((BCBC))", w);
            //---
            Assert.IsFalse(cfg.IntersectsWith(new string[] { @"\({2}", "A", "B" }, out w));
            //---
            Assert.IsTrue(cfg.IntersectsWith(new string[] { @"\){3}", "A" }, out w));
            Assert.AreEqual<string>("(((A)))", w);
        }

        [TestMethod]
        public void TestCFG_PDAConstruction_GenerateWitnesses()
        {
            var input = @"S -> \( S \) | [0-9]+ ";
            var cfg = ContextFreeGrammar.Parse(input);
            //---
            var pda1 = cfg.ToPDA<BDD>();
            var aut1 = cfg.BuiltinTerminalAlgebra.Convert(@"\({2,5}").RemoveEpsilons();
            var pda2 = pda1.Intersect(aut1);

            var witnesses = new List<string>();
            foreach (var w in pda2.GenerateWitnesses(5))
            {
                witnesses.Add(cfg.BuiltinTerminalAlgebra.ChooseString(w));
            }
            Assert.IsTrue(witnesses.Count == 5);
            foreach (var w in witnesses)
                Assert.IsTrue(w.EndsWith("))") && w.StartsWith("(("));
        }

        [TestMethod]
        public void TestCFG_Parse_Medium()
        {
            var input = @"
S -> NAME MAIN
MAIN -> SEARCH_CONDITION 
SEARCH_CONDITION -> (OR) PREDICATE | (AND) PREDICATE 
PREDICATE -> COMPARISON_PREDICATE | BETWEEN_PREDICATE | LIKE_PREDICATE | TEST_FOR_NULL | IN_PREDICATE | ALL_OR_ANY_PREDICATE | EXISTENCE_TEST
COMPARISON_PREDICATE -> SCALAR_EXP COMPARISON_OP SCALAR_EXP | SCALAR_EXP COMPARISON SUBQUERY
BETWEEN_PREDICATE -> SCALAR_EXP BETWEEN SCALAR_EXP (AND) SCALAR_EXP
LIKE_PREDICATE ->  SCALAR_EXP (LIKE) ATOM 
TEST_FOR_NULL -> COLUMN_REF (IS) (NULL)
IN_PREDICATE -> SCALAR_EXP (IN) \( SUBQUERY \) | SCALAR_EXP (IN) \( ATOM \) 
ALL_OR_ANY_PREDICATE -> SCALAR_EXP COMPARISON_OP ALL_ANY_SOME SUBQUERY
EXISTENCE_TEST -> (EXISTS) SUBQUERY
SCALAR_EXP ->  SCALAR_EXP OP SCALAR_EXP | ATOM | COLUMN_REF  | \( SCALAR_EXP \) 
ATOM -> PARAMETER | INTNUM 
SUBQUERY -> SELECT_EXP
SELECT_EXP -> (SELECT) NAME
ALL_ANY_SOME -> (ANY) | (ALL) | (SOME)
COLUMN_REF -> NAME
PARAMETER -> NAME
INTNUM -> (\d*)
OP -> (\+)  | (-) | (\*) | (\/) 
COMPARISON_OP -> (=) | (<) | (>)  
NAME -> (\w*)
";
            var cfg = ContextFreeGrammar.Parse(input);
            Assert.IsTrue(cfg.ProductionCount == 46);
        }

        [TestMethod]
        public void TestCFG_Parse_SmallNFA()
        {
            var input = @"
S -> R0_q0 
R0_q0 -> (a) R0_q1 
R0_q1 -> (s) R0_q2 
R0_q2 ->
";
            var cfg = ContextFreeGrammar.Parse(input);
            Assert.IsTrue(cfg.ProductionCount == 4);
            var pda = cfg.ToPDA<BDD>();
            BDD[] witness;
            Assert.IsTrue(pda.IsNonempty(out witness));
            var s = ChooseString(witness);
            Assert.IsTrue(s == "as");
            string s2;
            cfg.IntersectsWith("", out s2);
            Assert.IsTrue(s2 == "as");
        }

        string ChooseString(BDD[] witness)
        {
            if (witness.Length == 0)
                return "";
            else
            {
                var solver = (ICharAlgebra<BDD>)witness[0].algebra;
                var chars = Array.ConvertAll(witness, x => (char)solver.Choose(x));
                var str = new String(chars);
                return str;
            }
        }

        [TestMethod]
        public void TestCFG_PDAConstruction_Explore_Small()
        {
            var input = @"S -> \( S \) | (A) ";
            var cfg = ContextFreeGrammar.Parse(input);
            var pda = cfg.ToPDA<BDD>();
            var aut = cfg.BuiltinTerminalAlgebra.Convert(@"^.{0,10}$", System.Text.RegularExpressions.RegexOptions.Singleline).RemoveEpsilons();
            var pda2 = pda.Intersect(aut);
            //pda2.ShowGraph();
            var aut2 = pda2.Explore(4);
            //aut2.ShowGraph("aut2");
            var chooser = new Chooser();
            for (int i = 0; i < 10; i++)
            {
                BDD[] w = new List<BDD>(aut2.ChoosePathToSomeFinalState(chooser)).ToArray();
                string s = cfg.BuiltinTerminalAlgebra.ChooseString(w);
                Assert.IsTrue(cfg.BuiltinTerminalAlgebra.Accepts(aut, s));
                Assert.IsTrue(cfg.BuiltinTerminalAlgebra.Accepts(aut2, s));
            }
        }

        [TestMethod]
        public void TestCFG_PDAConstruction_Explore_Medium()
        {
            var input = @"
S -> NAME MAIN
MAIN -> SEARCH_CONDITION 
SEARCH_CONDITION -> (OR) PREDICATE | (AND) PREDICATE 
PREDICATE -> COMPARISON_PREDICATE | BETWEEN_PREDICATE | LIKE_PREDICATE | TEST_FOR_NULL | IN_PREDICATE | ALL_OR_ANY_PREDICATE | EXISTENCE_TEST
COMPARISON_PREDICATE -> SCALAR_EXP COMPARISON_OP SCALAR_EXP | SCALAR_EXP COMPARISON SUBQUERY
BETWEEN_PREDICATE -> SCALAR_EXP BETWEEN SCALAR_EXP (AND) SCALAR_EXP
LIKE_PREDICATE ->  SCALAR_EXP (LIKE) ATOM 
TEST_FOR_NULL -> COLUMN_REF (IS) (NULL)
IN_PREDICATE -> SCALAR_EXP (IN) \( SUBQUERY \) | SCALAR_EXP (IN) \( ATOM \) 
ALL_OR_ANY_PREDICATE -> SCALAR_EXP COMPARISON_OP ALL_ANY_SOME SUBQUERY
EXISTENCE_TEST -> (EXISTS) SUBQUERY
SCALAR_EXP ->  SCALAR_EXP OP SCALAR_EXP | ATOM | COLUMN_REF  | \( SCALAR_EXP \) 
ATOM -> PARAMETER | INTNUM 
SUBQUERY -> SELECT_EXP
SELECT_EXP -> (SELECT) NAME
ALL_ANY_SOME -> (ANY) | (ALL) | (SOME)
COLUMN_REF -> NAME
PARAMETER -> NAME
INTNUM -> ([0-9]+)
OP -> (\+)  | (-) | (\*) | (\/) 
COMPARISON_OP -> (=) | (<) | (>)  
NAME -> ([a-zA-Z][a-zA-Z0-9]*)
";
            var cfg = ContextFreeGrammar.Parse(input);
            var pda = cfg.ToPDA<BDD>();
            int t = System.Environment.TickCount;
            var aut = pda.Explore(15);
            t = System.Environment.TickCount - t;
            aut = aut.RemoveEpsilons().Determinize();
            var autm = aut.Minimize();
            //aut.ShowGraph("pda_explored_stack_depth_5");
            var chooser = new Chooser();
            for (int i = 0; i < 10; i++)
            {
                BDD[] w = new List<BDD>(aut.ChoosePathToSomeFinalState(chooser)).ToArray();
                string s = cfg.BuiltinTerminalAlgebra.ChooseString(w);
                Assert.IsTrue(s.Length > 1);
                //Assert.IsTrue(cfg.BuiltinTerminalAlgebra.Accepts(aut, s));
                //Assert.IsTrue(cfg.BuiltinTerminalAlgebra.Accepts(aut, s));
            }
        }

        [TestMethod]
        public void TestCFG_PDAConstruction_Large()
        {
            var sqlgrammar = System.IO.File.ReadAllText("../../../../Automata.Tests/Samples/grammar-sql.txt");
            var cfg = ContextFreeGrammar.Parse(sqlgrammar);
            var cfg2 = cfg.RemoveEpsilonsAndUselessSymbols().RemoveUnitProductions();
            var pda = cfg2.ToPDA<BDD>();
            //var pda2 = pda.Intersect(cfg.BuiltinTerminalAlgebra.Convert("^(SELECT.*)$").RemoveEpsilons());
            //BDD[] w;
            //bool ne = pda2.IsNonempty(out w);
            //var s = cfg.BuiltinTerminalAlgebra.ChooseString(w);
            //Assert.IsTrue(s.StartsWith("SELECT"));
            Assert.IsTrue(cfg.StartSymbol.Name == "sql_clauses");
        }
    }
}
