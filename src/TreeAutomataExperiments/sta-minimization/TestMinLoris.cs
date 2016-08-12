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
    class TestMinLoris
    {
        static int startAt =2;
        static int maxSize = 3;

        public static void RunTest()
        {
            for (int i = startAt; i < maxSize; i++)
                RunTestForGivenSize(i);
        }

        private static void RunTestForGivenSize(int K)
        {
            Console.WriteLine(K);
            Z3Provider Z = new Z3Provider();
            var A = (Z.TT.MkRankedAlphabet("A", Z.IntSort, new string[] { "zero", "two" }, new int[] { 0, 2 }));

            Func<int, Expr> beta = (i => Z.MkEq(Z.MkInt(1), Z.MkMod(Z.MkDiv(A.AttrVar, Z.MkInt(1 << (i % 32))), Z.MkInt(2))));

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

            var T = Z.TT.MkTreeAutomaton(new int[] { 3 * K, 3 * K + 1 }, A, A, rules);
            //var comp = T.Complete();

            Console.WriteLine();
            Console.WriteLine(T.StateCount + " " + T.RuleCount);
            foreach (var r in T.GetRules())
                Console.WriteLine(r.ToString());

            var v = T.Minimize();
            Console.WriteLine(v.StateCount + " " + v.RuleCount);
            foreach (var r in v.GetRules())
                Console.WriteLine(r.ToString());


            var v1 = T.MinimizeViaSFA();
            Console.WriteLine();
            Console.WriteLine(v1.StateCount + " " + v1.RuleCount);
            foreach (var r in v1.GetRules())
                Console.WriteLine(r.ToString());
            Console.WriteLine();
        }
    }
}
