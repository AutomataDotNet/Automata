using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

using Microsoft.Automata.Z3;
using Microsoft.Automata.Z3.Internal;
using Microsoft.Automata;
using Microsoft.Z3;
using System.Diagnostics;

namespace RunExperiments
{
    class LargeAlphabetExperiment
    {

        static int startAt = 1;
        static int maxSize = 100;

        public static void RunTest()
        {
            if (startAt == 1)
            {
                using (System.IO.StreamWriter outfile = new System.IO.StreamWriter(Program.path + Program.largeAlphabetFile, false))
                {
                    outfile.WriteLine("ID, StateCount, RuleCount, CompleteRuleCount, MinStateCount, MinRuleCount, Algo1, Algo2, SFABased");
                }
            }

            for (int i = startAt; i < maxSize; i++)
                RunTestForGivenSize(i);
        }

        private static void RunTestForGivenSize(int K)
        {
            Console.WriteLine(K);
            Z3Provider Z = new Z3Provider();
            var A = (Z.TT.MkRankedAlphabet("A", Z.IntSort, new string[] { "zero", "two" }, new int[] { 0, 2 }));

            Func<int, Expr> beta = (i => Z.MkEq(Z.MkInt(1), Z.MkMod(Z.MkDiv(A.AttrVar, Z.MkInt(1 << (i%32))), Z.MkInt(2))));

            Expr e1 = Z.MkEq(Z.MkInt(1), A.AttrVar);
            Expr e2 = Z.MkEq(Z.MkInt(2), A.AttrVar);
            Expr e3 = Z.MkEq(Z.MkInt(3), A.AttrVar);

            var r1 = Z.TT.MkTreeAcceptorRule(A, 0, "zero", e1);
            var r2 = Z.TT.MkTreeAcceptorRule(A, 1, "zero", e2);
            var r3 = Z.TT.MkTreeAcceptorRule(A, 2, "zero", e3);
            var rules = new List<TreeRule>();
            
            rules.Add(r1);
            rules.Add(r2);
            rules.Add(r3);

            for (int i = 0; i < K; i++)
            {
                rules.Add(Z.TT.MkTreeAcceptorRule(A, 3 * i + 3, "two", beta(i), 3 * i, 3 * i + 2));
                rules.Add(Z.TT.MkTreeAcceptorRule(A, 3 * i + 4, "two", beta(i), 3 * i + 1, 3 * i + 2));
                rules.Add(Z.TT.MkTreeAcceptorRule(A, 3 * i + 5, "two", beta(i), 3 * i + 2, 3 * i + 2));
            }

            var T = Z.TT.MkTreeAutomaton(new int[] { 3 * K , 3 * K +1 }, A, A, rules);             
            var comp = T.Complete();

            Util.RunAllAlgorithms(T, comp, K.ToString(), Program.largeAlphabetFile);
        }

    }
}
