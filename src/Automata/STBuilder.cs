using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Automata
{
    /// <summary>
    /// Builder for Symbolic Transducers that uses a given solver.
    /// The builder provides some convenience functions.
    /// </summary>
    public class STBuilder<FUNC, TERM, SORT>
    {
        IContext<FUNC, TERM, SORT> solver;

        /// <summary>
        /// Gets the underlying solver of the ST builder.
        /// </summary>
        public IContext<FUNC, TERM, SORT> Solver
        {
            get { return solver; }
        }

        /// <summary>
        /// Create a new ST builder with the given solver.
        /// </summary>
        public STBuilder(IContext<FUNC, TERM, SORT> solver)
        {
            this.solver = solver;
        }

        /// <summary>
        /// Create an ST with the given solver. Calls ST.Create(solver,...)
        /// </summary>
        public ST<FUNC, TERM, SORT> MkST(string name, TERM initialRegister, SORT inSort, SORT outSort, SORT regSort,
          int initialState,
          IEnumerable<Move<Rule<TERM>>> rulesAndFinalOutputs)
        {
            return ST<FUNC, TERM, SORT>.Create(solver, name, initialRegister, inSort, outSort, regSort, initialState, rulesAndFinalOutputs);
        }

        /// <summary>
        /// Make the ST that is the identity function from input sequences to output sequences.
        /// </summary>
        public ST<FUNC, TERM, SORT> MkIdentityST(SORT charsort)
        {
            List<Move<Rule<TERM>>> rules = new List<Move<Rule<TERM>>>();
            rules.Add(this.MkRule(0, 0, solver.True, this.MkRegister(solver.UnitSort), this.MkInputVariable(charsort)));
            rules.Add(this.MkFinalOutput(0, solver.True));
            return MkST("ID", solver.UnitConst, charsort, charsort, solver.UnitSort, 0, rules);
        }

        /// <summary>
        /// Make a move that corresponds to a nonfinal rule. This is indicated by IsFinal=false of the label of the move.
        /// </summary>
        /// <param name="source">source state of the move</param>
        /// <param name="target">target state of the move</param>
        /// <param name="guard">guard of the rule</param>
        /// <param name="update">update of the rule</param>
        /// <param name="yields">sequence of output yields of the rule</param>
        /// <returns>nonfinal move from source to target</returns>
        public Move<Rule<TERM>> MkRule(int source, int target, TERM guard, TERM update, params TERM[] yields)
        {
            return Move<Rule<TERM>>.Create(source, target, Rule<TERM>.Mk(guard, update, yields));
        }

        /// <summary>
        ///  Make a move that corresponds to a final rule. This is indicated by IsFinal=true of the condition of the move.
        /// </summary>
        /// <param name="finalState">final state</param>
        /// <param name="finalCondition">guard of the final outputs</param>
        /// <param name="finalYields">final outputs</param>
        /// <returns>a move from final state to final state that represents the final outputs yielded from the final state</returns>
        public Move<Rule<TERM>> MkFinalOutput(int finalState, TERM finalCondition, params TERM[] finalYields)
        {
            return Move<Rule<TERM>>.Create(finalState, finalState, Rule<TERM>.MkFinal(finalCondition, finalYields));
        }

        /// <summary>
        /// The input variable for the given sort. The variable has index 0.
        /// </summary>
        public TERM MkInputVariable(SORT sort)
        {
            return solver.MkVar(0, sort);
        }

        /// <summary>
        /// The register variable for the given sort. The variable has index 1.
        /// </summary>
        public TERM MkRegister(SORT sort)
        {
            return solver.MkVar(1, sort);
        }

        /// <summary>
        /// Creates an SFA from the given regex, with the character sort solver.CharSort.
        /// </summary>
        /// <param name="name">name of the SFA</param>
        /// <param name="regex">the regex from which the SFA is created</param>
        public SFA<FUNC, TERM, SORT> CreateFromRegex(string name, string regex)
        {
            return new SFA<FUNC, TERM, SORT>(solver, solver.CharSort, name, solver.RegexConverter.Convert(regex));
        }

        /// <summary>
        /// Negates the predicate, provides trivial optimizations: Not(False)= True and Not(True) = False.
        /// </summary>
        public TERM Not(TERM pred)
        {
            if (pred.Equals(solver.True))
                return solver.False;
            else if (pred.Equals(solver.False))
                return solver.True;
            else
                return solver.MkNot(pred);
        }

        /// <summary>
        /// Conjunction of the predicates, provides trivial optimization such as And(True, P) = P, etc.
        /// </summary>
        public TERM And(TERM pred1, TERM pred2)
        {
            if (pred1.Equals(solver.False) || pred2.Equals(solver.False))
                return solver.False;
            else if (pred1.Equals(solver.True))
                return pred2;
            else if (pred2.Equals(solver.True))
                return pred1;
            else if (pred1.Equals(pred2))
                return pred1;
            else if (pred1.Equals(Not(pred2)))
                return solver.False;
            else
                return solver.MkAnd(pred1, pred2);
        }

        /// <summary>
        /// Disjunction of the predicates, provides trivial optimization such as Or(False, P) = P, etc.
        /// </summary>
        public TERM Or(TERM pred1, TERM pred2)
        {
            if (pred1.Equals(solver.True) || pred2.Equals(solver.True))
                return solver.True;
            else if (pred1.Equals(solver.False))
                return pred2;
            else if (pred2.Equals(solver.False))
                return pred1;
            else if (pred1.Equals(pred2))
                return pred1;
            else if (pred1.Equals(Not(pred2)))
                return solver.True;
            else
                return solver.MkOr(pred1, pred2);
        }

        /// <summary>
        /// For example, given reg: (bool x int x bool x int), b : (bool x bool), newreg : (int x int).
        /// Returns T(b.0, newreg.0, b.1, newreg.1)
        /// </summary> 
        internal TERM MkBPInstance(TERM reg, TERM b, TERM newreg)
        {
            var subst = new Dictionary<TERM, TERM>();
            var bp = new List<TERM>(BP1(reg));
            if (bp.Count == 1)
                subst[bp[0]] = b;
            else
            {
                for (int i = 0; i < bp.Count; i++)
                    subst[bp[i]] = Solver.MkProj(i, b);
            }
            var nbp = new List<TERM>(NBP1(reg));
            if (nbp.Count == 1)
                subst[nbp[0]] = newreg;
            else
            {
                for (int i = 0; i < nbp.Count; i++)
                    subst[nbp[i]] = Solver.MkProj(i, newreg);
            }
            TERM skel = MkTupleSkeleton(reg);
            TERM res = Solver.ApplySubstitution(skel, subst);
            return res;
        }

        /// <summary>
        /// For example if r is a variable of sort s0 x (s10 x s11) makes the term T(r.0, T(r.1.0, r.1.1))
        /// </summary>
        internal TERM MkTupleSkeleton(TERM r)
        {
            var sort = Solver.GetSort(r);
            if (Solver.IsTupleSort(sort))
            {
                int k = Solver.GetTupleLength(sort);
                TERM[] args = new TERM[k];
                for (int i = 0; i < k; i++)
                {
                    args[i] = MkTupleSkeleton(Solver.MkProj(i, r));
                }
                FUNC constr = Solver.GetTupleConstructor(sort);
                var res = Solver.MkApp(constr, args);
                return res;
            }
            else
            {
                return r;
            }
        }

        /// <summary>
        /// For example if s is a sort (s0 x (s10 x s11)) makes the term T(#2, T(#3, #4))
        /// and the output dictionary  #2 -> r.0, #3 -> r.1.0, #4 -> r.1.1
        /// </summary>
        internal TERM MkTupleVarSkeleton(TERM r, out List<KeyValuePair<TERM, TERM>> subst)
        {
            subst = new List<KeyValuePair<TERM, TERM>>();
            var res = MkTupleVarSkeleton1(r, subst);
            return res;
        }

        TERM MkTupleVarSkeleton1(TERM r, List<KeyValuePair<TERM, TERM>> subst)
        {
            var sort = solver.GetSort(r);
            if (Solver.IsTupleSort(sort))
            {
                int k = Solver.GetTupleLength(sort);
                TERM[] args = new TERM[k];
                for (int i = 0; i < k; i++)
                {
                    args[i] = MkTupleVarSkeleton1(Solver.MkProj(i, r), subst);
                }
                FUNC constr = Solver.GetTupleConstructor(sort);
                var res = Solver.MkApp(constr, args);
                return res;
            }
            else
            {
                var v = solver.MkVar((uint)(subst.Count + 2), sort);
                subst.Add(new KeyValuePair<TERM, TERM>(v, r));
                return v;
            }
        }

        /// <summary>
        /// Get the projection pair for A, the control-state projection, and the register-state projection.
        /// Gets also the function combine that combines the projections back to the original register format 
        /// combine(control_state,register_state).
        /// </summary>
        internal void GetProjectionPair(IRegisterInfo<TERM> A, out TERM control_proj, out TERM register_proj, out Func<TERM, TERM, TERM> combine) 
        {
            var v = MkRegister(solver.GetSort(A.InitialRegister));
            var projs = new List<Tuple<TERM, bool>>(GetRegisterProjections(v, v, A)).ToArray();
            var first = Array.ConvertAll(Array.FindAll(projs, p => p.Item2), pair => pair.Item1);
            var second = Array.ConvertAll(Array.FindAll(projs, p => !p.Item2), pair => pair.Item1);
            if (first.Length == 0)        //no control projection
            {
                control_proj = solver.UnitConst;
                register_proj = v;
                combine = ((x1, x2) => x2);
            }
            else if (second.Length == 0)  //no register projection
            {
                control_proj = v;
                register_proj = solver.UnitConst;
                combine = ((x1, x2) => x1);
            }
            else
            {

                control_proj = (first.Length == 1 ? first[0] : solver.MkTuple(first));
                register_proj = (second.Length == 1 ? second[0] : solver.MkTuple(second));
                var control_sort = solver.GetSort(control_proj);
                var tmp_control_var = solver.MkVar(2, control_sort);
                var register_sort = solver.GetSort(register_proj);
                var tmp_register_var = solver.MkVar(3, register_sort);
                List<KeyValuePair<TERM,TERM>> subst_list;
                var skel = MkTupleVarSkeleton(v, out subst_list);
                var subst = new Dictionary<TERM,TERM>();
                int m = 0;
                int n = 0;
                for (int i = 0; i < subst_list.Count; i++)  //subst_list has the same length as projs
                    if (projs[i].Item2)
                        subst[subst_list[i].Key] = (first.Length == 1 ? tmp_control_var : solver.MkProj(m++, tmp_control_var));
                    else
                        subst[subst_list[i].Key] = (second.Length == 1 ? tmp_register_var : solver.MkProj(n++, tmp_register_var));
                var combined_reg = solver.Simplify(solver.ApplySubstitution(skel, subst));
                combine = (x1, x2) => solver.ApplySubstitution(combined_reg, tmp_control_var, x1, tmp_register_var, x2);
            }
        }

        IEnumerable<Tuple<TERM, bool>> GetRegisterProjections(TERM x, TERM proj, IRegisterInfo<TERM> A)
        {
            var sort = solver.GetSort(proj);
            if (sort.Equals(solver.BoolSort))
                yield return new Tuple<TERM, bool>(proj, true);
            else if (!solver.IsTupleSort(sort))
            {
                Predicate<TERM> pred = r =>
                    {
                        var t = solver.Simplify(solver.ApplySubstitution(proj, x, r));
                        var res = t.Equals(proj) || solver.IsGround(t);
                        return res;
                    };

                if (A.IsTrueForAllRegisterUpdates(pred))
                    yield return new Tuple<TERM, bool>(proj, true);
                else
                    yield return new Tuple<TERM, bool>(proj, false);
            }
            else
            {
                for (int i = 0; i < solver.GetTupleLength(sort); i++)
                    foreach (var pair in GetRegisterProjections(x, solver.MkProj(i, proj), A))
                        yield return pair;
            }
        }

              
        /// <summary>
        /// Boolean Projection
        /// </summary>
        internal TERM BP(TERM t)
        {
            var booleanProjections = new List<TERM>(BP1(t));
            if (booleanProjections.Count == 1)
                return booleanProjections[0];
            else
                return Solver.MkTuple(booleanProjections.ToArray());
        }

        private IEnumerable<TERM> BP1(TERM v)
        {
            var sort = Solver.GetSort(v);
            if (sort.Equals(Solver.BoolSort))
                yield return v;
            else if (Solver.IsTupleSort(sort))
            {
                int k = Solver.GetTupleLength(sort);
                for (int i = 0; i < k; i++)
                {
                    SORT elemSort = Solver.GetTupleElementSort(sort, i);
                    foreach (var bp in BP1(Solver.MkProj(i, v)))
                        yield return bp;
                }
            }
        }

        /// <summary>
        /// Non-Boolean Projection
        /// </summary>
        internal TERM NBP(TERM t)
        {
            var nonBooleanProjections = new List<TERM>(NBP1(t));
            if (nonBooleanProjections.Count == 1)
                return nonBooleanProjections[0];
            else
                return Solver.MkTuple(nonBooleanProjections.ToArray());
        }

        private IEnumerable<TERM> NBP1(TERM v)
        {
            var sort = Solver.GetSort(v);
            if (!sort.Equals(Solver.BoolSort) && !Solver.IsTupleSort(sort))
                yield return v;
            else if (Solver.IsTupleSort(sort))
            {
                int k = Solver.GetTupleLength(sort);
                for (int i = 0; i < k; i++)
                {
                    SORT elemSort = Solver.GetTupleElementSort(sort, i);
                    foreach (var nbp in NBP1(Solver.MkProj(i, v)))
                        yield return nbp;
                }
            }
        }

        internal bool ContainsSomeBooleans(SORT registerSort)
        {
            if (registerSort.Equals(solver.BoolSort))
                return true;
            else if (solver.IsTupleSort(registerSort))
            {
                int k = solver.GetTupleLength(registerSort);
                for (int i = 0; i < k; i++)
                    if (ContainsSomeBooleans(solver.GetTupleElementSort(registerSort, i)))
                        return true;
                return false;
            }
            else
                return false;
        }

        internal bool ContainsOnlyBooleans(SORT registerSort)
        {
            if (registerSort.Equals(solver.BoolSort))
                return true;
            else if (solver.IsTupleSort(registerSort))
            {
                int k = solver.GetTupleLength(registerSort);
                for (int i = 0; i < k; i++)
                    if (!ContainsOnlyBooleans(solver.GetTupleElementSort(registerSort, i)))
                        return false;
                return true;
            }
            else
                return false;
        }
    }
}
