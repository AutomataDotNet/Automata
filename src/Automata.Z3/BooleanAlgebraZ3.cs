using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Z3;

namespace Microsoft.Automata.Z3
{
    public class Z3BoolAlg : IBooleanAlgebra<BoolExpr>
    {
        Context context;
        Solver solver;
        Sort elementSort;
        Expr elemVar;
        MintermGenerator<BoolExpr> mtg;
        BoolExpr _False;
        BoolExpr _True;

        public Z3BoolAlg(Context z3context, Sort elementSort, long timeout)
        {
            this.context = z3context;
            this.context.UpdateParamValue("MODEL", "true");
            this.context.UpdateParamValue("timeout", timeout.ToString());
            this.elementSort = elementSort;
            this.solver = z3context.MkSolver();
            this.elemVar = z3context.MkConst("x", elementSort);
            this.mtg = new MintermGenerator<BoolExpr>(this);
            this._False = z3context.MkFalse();
            this._True = z3context.MkTrue();
        }

        public Z3BoolAlg(Context z3context, Sort elementSort)
        {
            this.context = z3context;
            this.context.UpdateParamValue("MODEL", "true");
            this.elementSort = elementSort;
            this.solver = z3context.MkSolver();
            this.elemVar = z3context.MkConst("x", elementSort);
            this.mtg = new MintermGenerator<BoolExpr>(this);
            this._False = z3context.MkFalse();
            this._True = z3context.MkTrue();
        }

        /// <summary>
        /// x is the only allowed uninterpreted constant in Boolean expressions that are created within this Boolean Algebra.
        /// </summary>
        public Expr x
        {
            get { return elemVar; }
        }

        Context Context
        {
            get { return context; }
        }


        public IEnumerable<Tuple<bool[], BoolExpr>> GenerateMinterms(params BoolExpr[] constraints)
        {
            return mtg.GenerateMinterms(constraints);
        }

        public BoolExpr True
        {
            get { return _True; }
        }

        public BoolExpr False
        {
            get { return _False; }
        }

        public BoolExpr MkOr(IEnumerable<BoolExpr> predicates)
        {
            var res = False;
            foreach (var pred in predicates)
            {
                if (pred.Equals(True))
                    return True;
                else
                    res = MkOr(res, pred);
            }
            return res;
        }

        public BoolExpr MkAnd(IEnumerable<BoolExpr> predicates)
        {
            var res = True;
            foreach (var pred in predicates)
            {
                if (pred.Equals(False))
                    return False;
                else
                {
                    if (res.Equals(True))
                        res = pred;
                    else if (!pred.Equals(True) && !pred.Equals(res))
                        res = context.MkAnd(res, pred);
                }
            }
            return res;
        }

        public BoolExpr MkAnd(params BoolExpr[] predicates)
        {
            var res = True;
            for (int i = 0; i < predicates.Length; i++ )
            {
                var pred = predicates[i];
                if (pred.Equals(False))
                    return False;
                else
                {
                    if (res.Equals(True))
                        res = pred;
                    else if (!pred.Equals(True) && !pred.Equals(res))
                        res = context.MkAnd(res, pred);
                }
            }
            return res;
        }

        public BoolExpr MkNot(BoolExpr predicate)
        {
            if (predicate.Equals(_False))
                return _True;
            else if (predicate.Equals(_True))
                return _False;
            else
            {
                if (predicate.IsNot)
                    return (BoolExpr)predicate.Args[0];
                else
                    return context.MkNot(predicate);
            }
        }

        public bool AreEquivalent(BoolExpr predicate1, BoolExpr predicate2)
        {
            solver.Push();
            var psi = context.MkNot(context.MkEq(predicate1, predicate2));
            solver.Assert(psi);
            var sat = solver.Check();
            solver.Pop();
            if (sat == Status.UNSATISFIABLE)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public BoolExpr MkSymmetricDifference(BoolExpr p1, BoolExpr p2)
        {
            return context.MkOr(context.MkAnd(context.MkNot(p1),p2),context.MkAnd(p1,context.MkNot(p2)));
        }

        public bool CheckImplication(BoolExpr predicate1, BoolExpr predicate2)
        { 
            solver.Push();
            var psi = context.MkNot(context.MkImplies(predicate1, predicate2)); 
            solver.Assert(psi);
            var sat = solver.Check();
            solver.Pop();
            if (sat == Status.UNSATISFIABLE)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsExtensional
        {
            get { return false; }
        }

        public BoolExpr Simplify(BoolExpr predicate)
        {
            return (BoolExpr)predicate.Simplify();
        }

        public bool IsAtomic
        {
            get { return true; }
        }

        public BoolExpr GetAtom(BoolExpr psi)
        {
            solver.Push();
            solver.Assert(psi);
            var sat = solver.Check();
            if (sat != Status.UNSATISFIABLE)
            {
                Model model = solver.Model;
                var val = model.Evaluate(elemVar, true);
                solver.Pop();
                var atom = context.MkEq(elemVar, val);
                return atom;
            }
            else
            {
                solver.Pop();
                return _False;
            }
        }

        public BoolExpr MkAnd(BoolExpr predicate1, BoolExpr predicate2)
        {
            if (predicate1.Equals(True))
                return predicate2;
            else if (predicate2.Equals(True))
                return predicate1;
            else if (predicate1.Equals(False) || predicate1.Equals(False))
                return False;
            else if (predicate1.Equals(predicate2))
                return predicate2;
            else
                return context.MkAnd(predicate1, predicate2);
        }

        public BoolExpr MkOr(BoolExpr predicate1, BoolExpr predicate2)
        {
            if (predicate1.Equals(False))
                return predicate2;
            else if (predicate2.Equals(False))
                return predicate1;
            else if (predicate1.Equals(True) || predicate1.Equals(True))
                return True;
            else if (predicate1.Equals(predicate2))
                return predicate2;
            else
            {
                if ((predicate1.IsNot && predicate1.Args[0].Equals(predicate2)) ||
                    (predicate2.IsNot && predicate2.Args[0].Equals(predicate1)))
                    return True;
                else
                    return context.MkOr(predicate1, predicate2);
            }
        }

        public bool IsSatisfiable(BoolExpr psi)
        {
            if (psi.Equals(_False))
                return false;
            if (psi.Equals(_True))
                return true;
            solver.Push();
            solver.Assert(psi);

            var sat = solver.Check();
            solver.Pop();
            if (sat != Status.UNSATISFIABLE)
            {
                if (sat == Status.UNKNOWN)
                    throw new Z3Exception("timeout");
                return true;
            }
            else
            {
                return false;
            }
        }


        public bool EvaluateAtom(BoolExpr atom, BoolExpr psi)
        {
            if (atom.IsEq && atom.Args[0].Equals(elemVar))
            {
                Expr v = atom.Args[1];
                BoolExpr res = (BoolExpr)psi.Substitute(elemVar, v).Simplify();
                if (res.Equals(_True))
                    return true;
                else if (res.Equals(_False))
                    return false;
                else
                    throw new AutomataException(AutomataExceptionKind.PredicateIsNotSingleton);
            }
            else
                throw new AutomataException(AutomataExceptionKind.PredicateIsNotSingleton);
        }


        public BoolExpr MkDiff(BoolExpr predicate1, BoolExpr predicate2)
        {
            return MkAnd(predicate1, MkNot(predicate2));
        }
    }
}
