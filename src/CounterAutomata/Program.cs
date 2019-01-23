using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Automata;
using Microsoft.Automata.Z3;
using Microsoft.Z3;

namespace CounterAutomata
{
    class Program
    {
        static void Main(string[] args)
        {
            Test1();
            //Test2();
        }

        static void Test1()
        {
            var cab = new CABuilder_ST();
            var a = cab.InputGuard("[aA]");
            int q = 0;
            int r = 1;
            int s = 2;
            var zero = cab.z3p.MkInt(0);
            //var _22 = cab.z3p.MkInt(22);
            var k = cab.z3p.MkConst("k", cab.z3p.IntSort);
            var c = cab.Counter(0);
            var c1 = cab.Incr(c);
            //q -- T --> q
            var q_q = cab.MkTransition(q, q);
            //q -- x=a,c0:=0 --> r
            var q_r = cab.MkTransition(q, r, a, cab.MkUpdate(zero));
            //r -- T --> s 
            var r_s = cab.MkTransition(r, s);
            //s -- c0 := c0+1 --> r 
            var s_r = cab.MkTransition(s, r, cab.z3p.True, cab.MkUpdate(c1));
            //r -- c0 := c0+1 --> r 
            var r_r = cab.MkTransition(r, r, cab.z3p.True, cab.MkUpdate(c1));
            //r is final with condition c==k
            var r_final = cab.MkFinalizer(r, cab.z3p.MkEq(c, k));

            var moves = new Move<Rule<Expr>>[] { q_q, q_r, r_s, s_r, r_r, r_final };


            var A = cab.MkCntAut(0, new Expr[] { cab.z3p.MkInt(0) }, moves, "A");
            A.ShowGraph();

            //var AxA = cab.Intersect(A, A);
            //AxA.ShowGraph();
        }

        static void Test2()
        {
            var cab = new CABuilder();
            var a = cab.InputGuard('a');
            int q = 0;
            int r = 1;
            int s = 2;
            var zero = cab.z3p.MkInt(0);
            var k = cab.z3p.MkConst("k", cab.z3p.IntSort);
            var c = cab.Ctr(1);
            var c_ = cab.Ctr_(1);
            //q -- T --> q
            var q_q = cab.MkTransition(q, q, cab.z3p.True);
            //q -- x=a,c0:=0 --> r
            var q_r = cab.MkTransition(q, r, cab.z3p.MkAnd(cab.z3p.True, a,
                cab.z3p.MkAnd(cab.z3p.MkEq(c_, cab.z3p.MkInt(0)))));
            //r -- T --> s 
            var r_s = cab.MkTransition(r, s, cab.z3p.True);
            //s -- c0 := c0+1 --> r 
            var s_r = cab.MkTransition(s, r, cab.z3p.MkEq(c_, cab.Incr(c)));
            //r -- c0 := c0+1 --> r 
            var r_r = cab.MkTransition(r, r, cab.z3p.MkEq(c_, cab.Incr(c)));
            //r is final with condition c==k
            var r_final = new KeyValuePair<int,Expr>(r, cab.z3p.MkEq(c, k));

            var moves = new Move<Expr>[] { q_q, q_r, r_s, s_r, r_r};

            Expr x_guard;
            Expr c_guard;
            Expr[] updates;
            cab.SplitCase(q_r.Label, out x_guard, out c_guard, out updates);


            var A = cab.MkCA(0, 0, 1, cab.z3p.True, moves,
                new KeyValuePair<int, Expr>[] { r_final });
            A.ShowGraph();;
        }
    }
}
