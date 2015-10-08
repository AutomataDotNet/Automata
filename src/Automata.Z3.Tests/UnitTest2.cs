using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.Automata;
using Microsoft.Z3;


namespace Automata.Z3.Tests
{
    [TestClass]
    public class Z3_GenericSTTests
    {

        //[TestMethod]
        public void STExploreTest1()
        {
            IContext<FuncDecl, Expr, Sort> solver = new Microsoft.Automata.Z3.Z3Provider(BitWidth.BV7);
            var st = MkDecodeST(solver, solver.CharSort).ExploreBools();
            var stst = st.Compose(st).ExploreBools();

            //st.ShowGraph();

            //stst.ShowGraph();

            var stst_simpl = stst.ToSTb().Simplify();

            //var sfa = st1.ToSFA().Minimize().NormalizeLabels();

            //sfa.ShowGraph(20);
            var w = stst.Diff(stst_simpl.ToST());
            Assert.IsNotNull(w);
        }

        [TestMethod]
        public void STExploreTest2()
        {
            IContext<FuncDecl, Expr, Sort> solver = new Microsoft.Automata.Z3.Z3Provider(BitWidth.BV7);

            var st = MkDecodeWithFinalOutputs(solver, solver.CharSort);

           //st.ShowGraph();


            var st1 = st.Explore();
            //st.AddEOI().ShowGraph();

            //var stst = st1,

            var w = st.Diff(st1, 5);
            Assert.IsNull(w);
        }


        private static ST<FuncDecl, Expr, Sort> MkDecodeST(IContext<FuncDecl, Expr, Sort> z3p, Sort charSort)
        {
            Expr tt = z3p.True; 
            Expr[] eps = new Expr[] { };
            List<Move<Rule<Expr>>> rules = new List<Move<Rule<Expr>>>();
            Expr x = z3p.MkVar(0,charSort);
            Sort regSort = z3p.MkTupleSort(charSort, charSort);
            //the compound register
            Expr yz = z3p.MkVar(1,regSort);
            //the individual registers
            Expr y = z3p.MkProj(0, yz);
            Expr z = z3p.MkProj(1, yz);
            //constant characer values
            Expr amp = z3p.MkNumeral((int)'&', charSort);
            Expr sharp = z3p.MkNumeral((int)'#', charSort);
            Expr semi = z3p.MkNumeral((int)';', charSort);
            Expr zero = z3p.MkNumeral((int)'0', charSort);
            Expr nine = z3p.MkNumeral((int)'9', charSort);
            Expr _1 = z3p.MkNumeral(1, charSort);
            Expr _0 = z3p.MkNumeral(0, charSort);
            Expr _10 = z3p.MkNumeral(10, charSort);
            Expr _48 = z3p.MkNumeral(48, charSort);
            //initial register value
            Expr _11 = z3p.MkTuple(_1, _1);
            //various terms
            Expr xNEQ0 = z3p.MkNeq(x, _0);
            Expr xEQ0 = z3p.MkEq(x, _0);
            Expr xNEQamp = z3p.MkNeq(x, amp);
            Expr xEQamp = z3p.MkEq(x, amp);
            Expr xNEQsharp = z3p.MkNeq(x, sharp);
            Expr xEQsharp = z3p.MkEq(x, sharp);
            Expr xNEQsemi = z3p.MkNeq(x, semi);
            Expr xEQsemi = z3p.MkEq(x, semi);
            Expr xIsDigit = z3p.MkAnd(z3p.MkCharLe(zero, x), z3p.MkCharLe(x, nine));
            Expr yIsDigit = z3p.MkAnd(z3p.MkCharLe(zero, y), z3p.MkCharLe(y, nine));
            Expr zIsDigit = z3p.MkAnd(z3p.MkCharLe(zero, z), z3p.MkCharLe(z, nine));
            Expr yzAreDigits = z3p.MkAnd(yIsDigit, zIsDigit);
            Expr xIsNotDigit = z3p.MkNot(xIsDigit);
            Expr decode = z3p.MkCharAdd(z3p.MkCharMul(_10, z3p.MkCharSub(y, _48)), z3p.MkCharSub(z, _48));
            //final state 
            rules.Add(Move<Rule<Expr>>.Create(5,5,Rule<Expr>.MkFinal(tt)));
            //terminating rules
            rules.Add(Move<Rule<Expr>>.Create(0, 5, Rule<Expr>.Mk(xEQ0, _11, _0)));
            rules.Add(Move<Rule<Expr>>.Create(1, 5, Rule<Expr>.Mk(xEQ0, _11, amp, _0)));
            rules.Add(Move<Rule<Expr>>.Create(2, 5, Rule<Expr>.Mk(xEQ0, _11, amp, sharp, _0)));
            rules.Add(Move<Rule<Expr>>.Create(3, 5, Rule<Expr>.Mk(xEQ0, _11, amp, sharp, y, _0)));
            rules.Add(Move<Rule<Expr>>.Create(4, 5, Rule<Expr>.Mk(xEQ0, _11, amp, sharp, y, z, _0)));
            //main rules 
            //rules from state q0
            rules.Add(Move<Rule<Expr>>.Create(0, 0, Rule<Expr>.Mk(z3p.MkAnd(xNEQ0, xNEQamp), yz, x)));
            rules.Add(Move<Rule<Expr>>.Create(0, 1, Rule<Expr>.Mk(z3p.MkAnd(xNEQ0, xEQamp), yz)));
            //rules from state q1
            rules.Add(Move<Rule<Expr>>.Create(1, 0, Rule<Expr>.Mk(z3p.MkAnd(xNEQ0, z3p.MkAnd(xNEQamp, xNEQsharp)), yz, amp, x)));
            rules.Add(Move<Rule<Expr>>.Create(1, 1, Rule<Expr>.Mk(z3p.MkAnd(xNEQ0, xEQamp), yz, amp)));
            rules.Add(Move<Rule<Expr>>.Create(1, 2, Rule<Expr>.Mk(z3p.MkAnd(xNEQ0, xEQsharp), yz)));
            //rules from state q2
            rules.Add(Move<Rule<Expr>>.Create(2, 0, Rule<Expr>.Mk(z3p.MkAnd(xNEQ0, z3p.MkAnd(xNEQamp, xIsNotDigit)), yz, amp, sharp, x)));
            rules.Add(Move<Rule<Expr>>.Create(2, 1, Rule<Expr>.Mk(z3p.MkAnd(xNEQ0, xEQamp), yz, amp, sharp)));
            rules.Add(Move<Rule<Expr>>.Create(2, 3, Rule<Expr>.Mk(z3p.MkAnd(xNEQ0, xIsDigit), z3p.MkTuple(x, z))));
            //rules from state q3
            rules.Add(Move<Rule<Expr>>.Create(3, 0, Rule<Expr>.Mk(z3p.MkAnd(xNEQ0, z3p.MkAnd(xNEQamp, xIsNotDigit)), _11, amp, sharp, y, x)));
            rules.Add(Move<Rule<Expr>>.Create(3, 1, Rule<Expr>.Mk(z3p.MkAnd(xNEQ0, xEQamp), _11, amp, sharp, y)));
            rules.Add(Move<Rule<Expr>>.Create(3, 4, Rule<Expr>.Mk(z3p.MkAnd(xNEQ0, xIsDigit), z3p.MkTuple(y, x))));
            //rules from state q4
            rules.Add(Move<Rule<Expr>>.Create(4, 0, Rule<Expr>.Mk(z3p.MkAnd(xNEQ0, xEQsemi), _11, decode)));
            rules.Add(Move<Rule<Expr>>.Create(4, 0, Rule<Expr>.Mk(z3p.MkAnd(xNEQ0, z3p.MkAnd(xNEQsemi, xNEQamp)), _11, amp, sharp, y, z, x)));
            rules.Add(Move<Rule<Expr>>.Create(4, 1, Rule<Expr>.Mk(z3p.MkAnd(xNEQ0, xEQamp), _11, amp, sharp, y, z)));


            ST<FuncDecl, Expr, Sort> st = ST<FuncDecl, Expr, Sort>.Create(z3p, "Decode", _11, charSort, charSort, regSort, 0, rules);
            return st;
        }

        private static ST<FuncDecl, Expr, Sort> MkDecodeWithFinalOutputs(IContext<FuncDecl, Expr, Sort> z3p, Sort charSort)
        {
            Expr tt = z3p.True;
            Expr[] eps = new Expr[] { };
            List<Move<Rule<Expr>>> rules = new List<Move<Rule<Expr>>>();
            Expr x = z3p.MkVar(0,charSort);
            Sort regSort = z3p.MkTupleSort(charSort, charSort);
            //the compound register
            Expr yz = z3p.MkVar(1, regSort);
            //the individual registers
            Expr y = z3p.MkProj(0, yz);
            Expr z = z3p.MkProj(1, yz);
            //constant characer values
            Expr amp = z3p.MkNumeral((int)'&', charSort);
            Expr sharp = z3p.MkNumeral((int)'#', charSort);
            Expr semi = z3p.MkNumeral((int)';', charSort);
            Expr zero = z3p.MkNumeral((int)'0', charSort);
            Expr nine = z3p.MkNumeral((int)'9', charSort);
            Expr _1 = z3p.MkNumeral(1, charSort);
            Expr _0 = z3p.MkNumeral(0, charSort);
            Expr _10 = z3p.MkNumeral(10, charSort);
            Expr _48 = z3p.MkNumeral(48, charSort);
            //initial register value
            Expr _11 = z3p.MkTuple(_1, _1);
            //various terms
            Expr xNEQamp = z3p.MkNeq(x, amp);
            Expr xEQamp = z3p.MkEq(x, amp);
            Expr xNEQsharp = z3p.MkNeq(x, sharp);
            Expr xEQsharp = z3p.MkEq(x, sharp);
            Expr xNEQsemi = z3p.MkNeq(x, semi);
            Expr xEQsemi = z3p.MkEq(x, semi);
            Expr xIsDigit = z3p.MkAnd(z3p.MkCharLe(zero, x), z3p.MkCharLe(x, nine));
            Expr yIsDigit = z3p.MkAnd(z3p.MkCharLe(zero, y), z3p.MkCharLe(y, nine));
            Expr zIsDigit = z3p.MkAnd(z3p.MkCharLe(zero, z), z3p.MkCharLe(z, nine)) ;
            Expr yzAreDigits = z3p.MkAnd(yIsDigit, zIsDigit);
            Expr xIsNotDigit = z3p.MkNot(xIsDigit);
            Expr decode = z3p.MkCharAdd(z3p.MkCharMul(_10, z3p.MkCharSub(y, _48)), z3p.MkCharSub(z, _48));
            //final outputs 
            rules.Add(Move<Rule<Expr>>.Create(0, 0, Rule<Expr>.MkFinal(tt)));
            rules.Add(Move<Rule<Expr>>.Create(1, 1, Rule<Expr>.MkFinal(tt, amp)));
            rules.Add(Move<Rule<Expr>>.Create(2, 2, Rule<Expr>.MkFinal(tt, amp, sharp)));
            rules.Add(Move<Rule<Expr>>.Create(3, 3, Rule<Expr>.MkFinal(tt, amp, sharp, y)));
            rules.Add(Move<Rule<Expr>>.Create(4, 4, Rule<Expr>.MkFinal(tt, amp, sharp, y, z)));
            //main rules 
            //rules from state q0
            rules.Add(Move<Rule<Expr>>.Create(0, 0, Rule<Expr>.Mk(xNEQamp, yz, x)));
            rules.Add(Move<Rule<Expr>>.Create(0, 1, Rule<Expr>.Mk(xEQamp, yz)));
            //rules from state q1
            rules.Add(Move<Rule<Expr>>.Create(1, 0, Rule<Expr>.Mk(z3p.MkAnd(xNEQamp, xNEQsharp), yz, amp, x)));
            rules.Add(Move<Rule<Expr>>.Create(1, 1, Rule<Expr>.Mk(xEQamp, yz, amp)));
            rules.Add(Move<Rule<Expr>>.Create(1, 2, Rule<Expr>.Mk( xEQsharp, yz)));
            //rules from state q2
            rules.Add(Move<Rule<Expr>>.Create(2, 0, Rule<Expr>.Mk(z3p.MkAnd(xNEQamp, xIsNotDigit), yz, amp, sharp, x)));
            rules.Add(Move<Rule<Expr>>.Create(2, 1, Rule<Expr>.Mk(xEQamp, yz, amp, sharp)));
            rules.Add(Move<Rule<Expr>>.Create(2, 3, Rule<Expr>.Mk(xIsDigit, z3p.MkTuple(x, z))));
            //rules from state q3
            rules.Add(Move<Rule<Expr>>.Create(3, 0, Rule<Expr>.Mk(z3p.MkAnd(xNEQamp, xIsNotDigit), _11, amp, sharp, y, x)));
            rules.Add(Move<Rule<Expr>>.Create(3, 1, Rule<Expr>.Mk(xEQamp, _11, amp, sharp, y)));
            rules.Add(Move<Rule<Expr>>.Create(3, 4, Rule<Expr>.Mk(xIsDigit, z3p.MkTuple(y, x))));
            //rules from state q4
            rules.Add(Move<Rule<Expr>>.Create(4, 0, Rule<Expr>.Mk(xEQsemi, _11, decode)));
            rules.Add(Move<Rule<Expr>>.Create(4, 0, Rule<Expr>.Mk(z3p.MkAnd(xNEQsemi, xNEQamp), _11, amp, sharp, y, z, x)));
            rules.Add(Move<Rule<Expr>>.Create(4, 1, Rule<Expr>.Mk(xEQamp, _11, amp, sharp, y, z)));

            ST<FuncDecl, Expr, Sort> st = ST<FuncDecl, Expr, Sort>.Create(z3p, "Decode", _11, charSort, charSort, regSort, 0, rules);
            return st;
        }


        //[TestMethod]
        //public void DotTest()
        //{
        //    string filename = @"C:\risebox\web\riselive\RiSELive\Tools\Agl\ft.dot";
        //    ISMTSolver<FuncDecl, Expr, Sort> solver = new Microsoft.Automata.Z3.Internal.Z3Provider(CharacterEncoding.ASCII);

        //    var st = MkDecodeWithFinalOutputs(solver, solver.CharSort);
        //    //st.SaveAsDot(filename);
        //    st.ShowGraph(0);
        //}

        //[TestMethod]
        public void STDecodeTest()
        {
            IContext<FuncDecl, Expr, Sort> solver = new Microsoft.Automata.Z3.Z3Provider(BitWidth.BV7);

            var st = MkDecodeWithFinalOutputs(solver, solver.CharSort);

            var stst = st.Compose(st);

            var w = st.Neq1(stst);

            Assert.IsNotNull(w);
        }
    }
}
