using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.Automata;

namespace Microsoft.Automata.Tests
{
    [TestClass]
    public class CharSetSolverTests
    {
        //[TestMethod]
        //public void TestBREXLikeExpression()
        //{
        //    var man = new BREXManager();
        //    var like1 = man.MkLike(@"%[ab]_____");
        //    var like2 = man.MkLike(@"%[bc]_____");
        //    var and = man.MkAnd(like1, like2);
        //    var dfa = and.Optimize();
        //    var like = man.MkLike(@"%b_____");
        //    var dfa2 = like.Optimize();
        //    var equiv = dfa.IsEquivalentWith(dfa2);
        //    Assert.IsTrue(equiv);
        //}


        [TestMethod]
        public void GenerateMembersTest()  
        {
            foreach (var encoding in new BitWidth[]{
                BitWidth.BV7, BitWidth.BV8, BitWidth.BV16})
            {
                CharSetSolver solver = new CharSetSolver(encoding);
                var ranges = new char[][] { 
                new char[] { 'a', 'c' }, 
                new char[] {'0', '5'},
                new char[] { 'e', 'h' }, 
                new char[] {'6', '9'},};
                BDD s = solver.MkRangesConstraint(false, ranges); 

                var members = new List<char>(solver.GenerateAllCharacters(s, false));

                Assert.AreEqual<int>(17, members.Count, "wrong number of members in the range [a-ce-h0-9]");
                Assert.AreEqual<char>('0', members[0], "the smallest character in the range must be '0'");
                Assert.AreEqual<char>('h', members[16], "the largest character in the range must be 'h'");

                var membersInReverse = new List<char>(solver.GenerateAllCharacters(s, true));

                Assert.AreEqual<int>(17, membersInReverse.Count, "wrong number of members in the range [a-ce-h0-9]");
                Assert.AreEqual<char>('h', membersInReverse[0], "the first character in the reverse enumeration must be 'h'");
                Assert.AreEqual<char>('0', membersInReverse[16], "the last character in the reverse enumeration must be '0'");
            }
        }

        [TestMethod]
        public void GenerateMembersTest2()
        {
            CharSetSolver solver = new CharSetSolver(BitWidth.BV16);
            var ranges = new char[][] { 
                new char[] { 'a', 'c' }, 
                new char[] {'\u5555', '\u55A5'},
                new char[] { 'e', 'h' }, 
                new char[] {'\u55A0', '\u55AA'},
            };
            BDD s = solver.MkRangesConstraint(false, ranges);
            s.ToDot("bug.dot");

            var r = solver.ToRanges(s);
            var s2 = solver.MkCharSetFromRanges(r);

            var members = new List<char>(solver.GenerateAllCharacters(s2, false));
            var smallest = (char)solver.GetMin(s2);

            Assert.AreEqual<int>(93, members.Count, "wrong number of members in the range [a-ce-h\\u5555-\\u55AA]");
            Assert.AreEqual<char>('a', members[0], "the smallest character in the range must be 'a'");
            Assert.AreEqual<char>('\u55AA', members[members.Count - 1], "the largest character in the range must be '\\u55AA'");

            var membersInReverse = new List<char>(solver.GenerateAllCharacters(s, true));

            Assert.AreEqual<int>(93, membersInReverse.Count, "wrong number of members in the range [a-ce-h\\u5555-\\u55AA]");
            Assert.AreEqual<char>('\u55AA', membersInReverse[0], "the first character in the reverse enumeration must be '\\u55AA'");
            Assert.AreEqual<char>('a', membersInReverse[membersInReverse.Count-1], "the last character in the reverse enumeration must be 'a'");
        }

        [TestMethod]
        public void ChooseTest()
        {
            for (int i = 0; i < 10; i++)
            {
                CharSetSolver solver = new CharSetSolver(BitWidth.BV16); 

                BDD set1 = solver.MkRangeConstraint('a', 'c', true);
                string set1str = solver.PrettyPrint(set1);
                BDD set2 = solver.MkRangeConstraint('a', 'c');
                string set2str = solver.PrettyPrint(set2);
                BDD set3 = solver.MkRangeConstraint( 'A', 'C');
                string set3str = solver.PrettyPrint(set3);

                BDD set1a = solver.MkOr(set2, set3);

                Assert.AreEqual<string>("[A-Ca-c]",set1str);
                Assert.AreEqual<string>("[a-c]", set2str);
                Assert.AreEqual<string>("[A-C]", set3str);



                int h1 = set1.GetHashCode();
                int h2 = set1a.GetHashCode();
                bool same = (h1 == h2);
                Assert.AreSame(set1, set1a);
                Assert.IsTrue(same);
                Assert.IsTrue(solver.AreEquivalent(set1, set1a));

                //int seed = solver.Chooser.RandomSeed;
                char choice1 = (char)solver.Choose(set1);
                char choice2 = (char)solver.Choose(set1);
                char choice3 = (char)solver.Choose(set1);
                char choice4 = (char)solver.Choose(set1);

                //solver.Chooser.RandomSeed = seed;
                //char choice1a = solver.Choose(set1a);
                //char choice2a = solver.Choose(set1a);
                //char choice3a = solver.Choose(set1a);
                //char choice4a = solver.Choose(set1a);

                //string s = new String(new char[] { choice1, choice2, choice3, choice4 });
                //string sa = new String(new char[] { choice1a, choice2a, choice3a, choice4a });

                //Assert.AreEqual<string>(s, sa);
            }

        }

        [TestMethod]
        public void ChooseTest2()
        {
            CharSetSolver solver = new CharSetSolver(BitWidth.BV16);

            BDD set1 = solver.MkRangeConstraint('a', 'a', true);
            string set1str = solver.PrettyPrint(set1);
            BDD set2 = solver.MkRangeConstraint('a', 'a');
            string set2str = solver.PrettyPrint(set2);
            BDD set3 = solver.MkRangeConstraint('A', 'A');
            string set3str = solver.PrettyPrint(set3);

            BDD set1a = solver.MkOr(set2, set3);

            Assert.AreEqual<string>("[Aa]", set1str);
            Assert.AreEqual<string>("a", set2str);
            Assert.AreEqual<string>("A", set3str);
        }

        [TestMethod]
        public void ChooseUnifromlyTest()
        {
            CharSetSolver solver = new CharSetSolver(BitWidth.BV16);

            BDD set1 = solver.MkRangeConstraint('\0', '\x01', true);
            BDD set2 = solver.MkRangeConstraint( '\u0FFF', '\u0FFF');
            string set2str = solver.PrettyPrint(set2);
            BDD set3 = solver.MkRangeConstraint( '\u00FF', '\u00FF');
            BDD set4 = solver.MkRangeConstraint( '\u000F', '\u000F');

            BDD set = solver.MkOr(new BDD[]{set2, set3, set4, set1});

            string setstr = solver.PrettyPrint(set);

            set.ToDot(@"foo.dot");

            var map = new Dictionary<char, int>();
            map['\0'] = 0;
            map['\x01'] = 0;
            map['\u0FFF'] = 0;
            map['\u00FF'] = 0;
            map['\u000F'] = 0;

            for (int i = 0; i < 50000; i++)
            {
                var c = solver.ChooseUniformly(set);
                map[c] += 1;
            }
            foreach (var kv in map)
                Assert.IsTrue(kv.Value > 9700);
        }

        [TestMethod]
        public void MinMaxTest()
        {
            CharSetSolver solver = new CharSetSolver(BitWidth.BV16);
            var ranges = new char[][] { 
                new char[] {'\u55A0', '\u55AA'},
                new char[] { 'a', 'c' }, 
                new char[] {'\u5555', '\u55A5'},
                new char[] { 'e', 'h' }
            };
            BDD s = solver.MkRangesConstraint(false, ranges);
            char c = solver.GetMax(s);
            Assert.AreEqual<char>('\u55AA', c, "not the maximum character");
            c = (char)solver.GetMin(s);
            Assert.AreEqual<char>('a', c, "not the minimum character"); 
        }

        [TestMethod]
        public void GenerateMintermsTest1()
        {
            CharSetSolver bddb = new CharSetSolver(BitWidth.BV16);

            BDD a = bddb.MkRangeConstraint( 'a', 'a');
            BDD b = bddb.MkRangeConstraint( 'b', 'b');
            BDD c = bddb.MkRangeConstraint( 'c', 'c');

            var combinations = new List<Tuple<bool[], BDD>>(bddb.GenerateMinterms(new BDD[] { a, b, c }));
            Assert.AreEqual<int>(4, combinations.Count);
        }

        [TestMethod]
        public void GenerateMintermsTest2()
        {
            CharSetSolver bddb = new CharSetSolver(BitWidth.BV16);

            BDD a = bddb.MkRangeConstraint( 'b', 'c');
            BDD b = bddb.MkRangeConstraint( 'b', 'b');
            BDD b2 = bddb.MkRangeConstraint( 'b', 'b');
            BDD c = bddb.MkRangeConstraint( 'c', 'c');
            BDD b3 = bddb.MkRangeConstraint( 'b', 'b');

            var combinations = new List<Tuple<bool[], BDD>>(bddb.GenerateMinterms(new BDD[] { a, b, b2, c, b3 }));
            Assert.AreEqual<int>(3, combinations.Count, "only three combinations are possible");
        }

        [TestMethod]
        public void GenerateMintermsTest3()
        {
            CharSetSolver bddb = new CharSetSolver(BitWidth.BV16);

            BDD A = bddb.MkRangeConstraint( '1', '4');
            BDD A1 = bddb.MkRangesConstraint(false, new char[][] { new char[] { '1', '3' }, new char[] { '3', '4' }});
            BDD B = bddb.MkRangesConstraint(false, new char[][] { new char[] { '2', '3' }, new char[] { '5', '6' }, new char[] { '8', '8' } });
            BDD C = bddb.MkRangesConstraint(false, new char[][] { new char[] { '3', '4' }, new char[] { '6', '7' }, new char[] { '9', '9' } });
            BDD D = bddb.MkRangesConstraint(false, new char[][] { new char[] { '0', '0' }, new char[] { '8', '9' } });

            var combinations = new List<Tuple<bool[], BDD>>(bddb.GenerateMinterms(new BDD[] { A, B, C, A1, D }));
            Assert.AreEqual<int>(11, combinations.Count, "exactly 11 combinations must be possible");
        }

        [TestMethod]
        public void GenerateMintermsTest4()
        {
            CharSetSolver solver = new CharSetSolver(BitWidth.BV7);

            BDD a = solver.MkRangeConstraint( '\0', '\x7E');
            BDD b = solver.MkRangeConstraint( '1', '1');
            BDD c = solver.MkRangeConstraint( '1', '3');

            var Z = new List<Tuple<bool[], BDD>>(solver.GenerateMinterms(new BDD[] { a, b, c })).ToArray();
            var Y = Array.ConvertAll(Z, x => x.Item2);
            var X = new HashSet<BDD>(Y);
            Assert.AreEqual<int>(4, X.Count);
            
            Assert.IsTrue(X.Contains(solver.MkRangeConstraint( '1', '1')));
            Assert.IsTrue(X.Contains(solver.MkRangeConstraint( '2', '3')));
            Assert.IsTrue(X.Contains(solver.MkRangesConstraint(false, new char[][] {new char[] { '\x7F', '\x7F' } })));
            Assert.IsTrue(X.Contains(solver.MkRangesConstraint(false, new char[][] { new char[] { '4', '\x7E' }, new char[] { '\0', '0' } })));
        }
    }
}
