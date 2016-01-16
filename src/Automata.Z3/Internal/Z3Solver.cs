using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Z3;

using Microsoft.Automata;

using Microsoft.Automata.Z3.Internal;

namespace Microsoft.Automata.Z3
{
    /// <summary>
    /// Wrapper of a Z3 solver
    /// </summary>
    internal class Z3Solver : ISolver<Expr>, IDisposable
    {
        Solver solver;
        Z3Provider z3p;

        /// <summary>
        /// Underlying Z3 solver.
        /// </summary>
        public Solver Solver
        {
            get { return solver; }
        }

        internal Z3Solver(Solver solver, Z3Provider z3p)
        {
            this.solver = solver;
            this.z3p = z3p;
        }

        /// <summary>
        /// Push a new local context.
        /// </summary>
        public void Push()
        {
            solver.Push();
        }

        /// <summary>
        /// Pop the current local context.
        /// </summary>
        public void Pop()
        {
            solver.Pop();
        }

        /// <summary>
        /// Constraints asserted in the solver.
        /// </summary>
        public Expr[] Assertions
        {
            get { return solver.Assertions; }
        }

        /// <summary>
        /// Number of constraints asserted in the solver.
        /// </summary>
        public uint NumAssertions
        {
            get { return solver.NumAssertions; }
        }

        /// <summary>
        /// Assert a constraint in the solver.
        /// </summary>
        /// <param name="constraint">given constraint to be asserted</param>
        public void Assert(Expr constraint)
        {
            solver.Assert((BoolExpr)constraint);
        }

        /// <summary>
        /// Check and return true if the solver is not unsatisfiable.
        /// </summary>
        public bool Check()
        {
            var s = solver.Check();
            return s != Status.UNSATISFIABLE;
        }

        /// <summary>
        /// Dispose the solver.
        /// </summary>
        public void Dispose()
        {
            solver.Dispose();
        }

        /// <summary>
        /// If the assertion is satisfiable, returns an interpretation of the listed terms.
        /// The returned dictionary is empty if no terms are listed for evaluation.
        /// Returns null iff the assertion is unsatisfiable.
        /// </summary>
        public IDictionary<Expr, IValue<Expr>> GetModel(Expr assertion, params Expr[] termsToEvaluate)
        {
            if (!z3p.GetSort(assertion).Equals(z3p.BoolSort))
                throw new ArgumentException("must be Boolean", "assertion");

            if (termsToEvaluate.Length == 0)
                if (IsSatisfiable(assertion))
                    return new Dictionary<Expr, IValue<Expr>>();
                else
                    return null;

            Dictionary<Expr, IValue<Expr>> witness = null;
            Push();
            Assert(assertion);

            Model model;
            if (CheckAndGetModel(out model) != Status.UNSATISFIABLE)
            {
                witness = new Dictionary<Expr, IValue<Expr>>();
                foreach (var t in termsToEvaluate)
                {
                    Expr tval = model.Eval(t, true);
                    witness[t] = new ValueExpr(z3p, tval);
                }
            }
            if (model != null)
                model.Dispose();
            Pop();
            return witness;
        }

        /// <summary>
        /// Finds a model of the asserted constraints if they are satisfiable
        /// </summary>
        /// <param name="model">generated model</param>
        /// <returns>Status.True if satisfiable, Status.UNSATISFIABLE if unsatisfiable, Status.Undef upon a timeout or if quantifiers are used</returns>
        internal Status CheckAndGetModel(out Model model)
        {
            var v = solver.Check();
            if (v != Status.UNSATISFIABLE)
                model = solver.Model;
            else
                model = null;
            return v;
        }

        Dictionary<Expr, bool> IsSatisfiableCache = new Dictionary<Expr, bool>();

        /// <summary>
        /// Check satisfiability of the constraint in the solver without changing the assertions.
        /// </summary>
        public bool IsSatisfiable(Expr constraint)
        {
            if (constraint.Equals(z3p.False))
                return false;
            else if (constraint.Equals(z3p.True))
                return true;
            else
            {
                bool res;
                if (!IsSatisfiableCache.TryGetValue(constraint, out res))
                {
                    Push();
                    Assert(constraint);
                    res = Check();
                    Pop();
                    IsSatisfiableCache[constraint] = res;
                }
                return res;
            }
        }

        /// <summary>
        /// Enumerate all solutions for an open formula containing a single free variable (input or output)
        /// Is meant to be used for lightweight constraint solving during transducer construction.
        /// Assumes that the formula contains a single free variable only.
        /// </summary>
        public IEnumerable<IValue<Expr>> FindAllMembers(Expr openFormula)
        {
            var vars = new List<Expr>(z3p.GetVars(openFormula));
            if (vars.Count != 1)
                throw new ArgumentException("must contain exactly one free variable", "openFormula");

            var v0 = vars[0];

            Push();
            var model = GetModel(openFormula, v0);
            IValue<Expr> valueExpr = (model == null ? null : model[v0]);
            Pop();

            List<Expr> oldValues = new List<Expr>();

            while (valueExpr != null)
            {
                oldValues.Add(valueExpr.Value);

                yield return valueExpr;

                Push();
                Expr negatedSolutions = z3p.True;
                foreach (var v in oldValues)
                    negatedSolutions = z3p.MkAnd(negatedSolutions, z3p.MkNeq(v0, v));

                model = GetModel(z3p.MkAnd(openFormula, negatedSolutions), v0);
                valueExpr = (model == null ? null : model[v0]);
                Pop();
            }
        }


        /// <summary>
        /// Returns a single solution to the formula containing a single variable or null if no solution exists
        /// </summary>
        public IValue<Expr> FindOneMember(Expr openFormula)
        {
            foreach (var v in FindAllMembers(openFormula))
                return v;
            return null;
        }

    }


}
