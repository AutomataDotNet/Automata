using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Z3;

namespace Microsoft.Automata.Z3
{
    public class BooleanAlgebraZ3 : IBoolAlgMinterm<BoolExpr>
    {
        Context context;
        Solver solver;
        Sort elementSort;
        Expr elemVar;
        MintermGenerator<BoolExpr> mtg;
        BoolExpr _False;
        BoolExpr _True;
        public BooleanAlgebraZ3(Context z3context, Sort elementSort)
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


        public IEnumerable<Pair<bool[], BoolExpr>> GenerateMinterms(params BoolExpr[] constraints)
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
            var predicatesArr = new List<BoolExpr>(predicates).ToArray();
            return context.MkOr(predicatesArr);
        }

        public BoolExpr MkAnd(IEnumerable<BoolExpr> predicates)
        {
            var predicatesArr = new List<BoolExpr>(predicates).ToArray();
            return context.MkAnd(predicatesArr);
        }

        public BoolExpr MkAnd(params BoolExpr[] predicates)
        {
            return context.MkAnd(predicates);
        }

        public BoolExpr MkNot(BoolExpr predicate)
        {
            if (predicate.Equals(_False))
                return _True;
            if (predicate.Equals(_True))
                return _False;
            return context.MkNot(predicate);
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
                return context.MkFalse();
            }
        }

        public BoolExpr MkAnd(BoolExpr predicate1, BoolExpr predicate2)
        {
            return context.MkAnd(predicate1, predicate2);
        }

        public BoolExpr MkOr(BoolExpr predicate1, BoolExpr predicate2)
        {
            return context.MkOr(predicate1, predicate2);
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
    }
}
