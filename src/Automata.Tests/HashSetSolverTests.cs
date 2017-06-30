//using System;
//using System.Text;
//using System.Collections.Generic;
//using Microsoft.VisualStudio.TestTools.UnitTesting;

//using Microsoft.Automata;
//using Microsoft.Automata;

//namespace Microsoft.Automata.Tests
//{
//    [TestClass]
//    public class HashSetSolverTests
//    {

//        [TestMethod]
//        public void GenerateMintermsTest1()
//        {
//            HashSetSolver bddb = new HashSetSolver(CharacterEncoding.ASCII);

//            HashSet<char>  a = bddb.MkRangeConstraint(false, 'a', 'a');
//            HashSet<char> b = bddb.MkRangeConstraint(false, 'b', 'b');
//            HashSet<char> c = bddb.MkRangeConstraint(false, 'c', 'c');
//            var combinations = new List<Tuple<bool[], HashSet<char>>>(bddb.GenerateMinterms(new HashSet<char>[] { a, b, c }));
//            Assert.AreEqual<int>(4, combinations.Count);
//            for (int i = 0; i < 4; i++)
//            {
//                var comb = combinations[i];
//                int nrOfTrues = 0;
//                foreach (var x in comb.First)
//                    nrOfTrues += (x ? 1 : 0);
//                if (i == 0)
//                    Assert.AreEqual<int>(0, nrOfTrues, "all are negated");
//                else
//                    Assert.AreEqual<int>(1, nrOfTrues, "only one can be true at a time");
//            }
//        }

//        [TestMethod]
//        public void GenerateMintermsTest2()
//        {
//            HashSetSolver bddb = new HashSetSolver(CharacterEncoding.ASCII);

//            HashSet<char> a = bddb.MkRangeConstraint(false, 'b', 'c');
//            HashSet<char> b = bddb.MkRangeConstraint(false, 'b', 'b');
//            HashSet<char> b2 = bddb.MkRangeConstraint(false, 'b', 'b');
//            HashSet<char> c = bddb.MkRangeConstraint(false, 'c', 'c');
//            HashSet<char> b3 = bddb.MkRangeConstraint(false, 'b', 'b');

//            var combinations = new List<Tuple<bool[], HashSet<char>>>(bddb.GenerateMinterms(new HashSet<char>[] { a, b, b2, c, b3 }));
//            Assert.AreEqual<int>(3, combinations.Count, "only three combinations are possible");
//        }

//        [TestMethod]
//        public void GenerateMintermsTest3()
//        {
//            HashSetSolver bddb = new HashSetSolver(CharacterEncoding.ASCII);

//            HashSet<char> A = bddb.MkRangeConstraint(false, '1', '4');
//            HashSet<char> B = bddb.MkRangesConstraint(false, new char[][] { new char[] { '2', '3' }, new char[] { '5', '6' }, new char[] { '8', '8' } });
//            HashSet<char> C = bddb.MkRangesConstraint(false, new char[][] { new char[] { '3', '4' }, new char[] { '6', '7' }, new char[] { '9', '9' } });
//            HashSet<char> D = bddb.MkRangesConstraint(false, new char[][] { new char[] { '0', '0' }, new char[] { '8', '9' } });

//            var combinations = new List<Tuple<bool[], HashSet<char>>>(bddb.GenerateMinterms(new HashSet<char>[] { A, B, C, D }));
//            Assert.AreEqual<int>(11, combinations.Count, "exactly 11 combinations must be possible");
//        }
//    }
//}
