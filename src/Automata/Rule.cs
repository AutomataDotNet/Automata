using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Microsoft.Automata
{
    /// <summary>
    /// Represents the label of a move of a Symbolic Transducer (ST).
    /// STs have flat rules.
    /// </summary>
    public class Rule<TERM>
    {
        internal TERM guard;
        TERM[] yields;
        TERM[] yields2;

        Sequence<TERM> output;
        Sequence<TERM> output2;

        public Sequence<TERM> Output
        {
            get { return output; }
        }

        public Sequence<TERM> Output2
        {
            get { return output2; }
        }

        /// <summary>
        /// when nonfinal, indicates the number of inputs
        /// </summary>
        internal int k;
        internal TERM update;

        /// <summary>
        /// True iff the rule describes a final output.
        /// </summary>
        public bool IsFinal
        {
            get { return k == 0; }
        }

        /// <summary>
        /// Gets the guard of the rule.
        /// </summary>
        public TERM Guard
        {
            get { return guard; }
        }

        /// <summary>
        /// Gets the array of yielded outputs, that is the empty array if there are no outputs.
        /// </summary>
        public TERM[] Yields
        {
            get { return yields; }
        }

        internal TERM[] Yields2
        {
            get { return yields2; }
        }

        /// <summary>
        /// Gets the symbolic register update, that is default(TERM), if no registers are used or IsFinal is true.
        /// </summary>
        public TERM Update
        {
            get { return update; }
        }

        /// <summary>
        /// Must not be used.
        /// </summary>
        Rule() { }

        /// <summary>
        /// Creates a new rule
        /// </summary>
        /// <param name="final">true iff the rule represents a final output</param>
        /// <param name="guard">predicate over input and registers, or registers only when the rule is a final output</param>
        /// <param name="yields">output elements yielded by the rule</param>
        /// <param name="update">register update</param>
        Rule(bool final, TERM guard, TERM[] yields, TERM update)
        {
            this.guard = guard;
            this.yields = yields;
            this.output = new Sequence<TERM>(yields);
            this.update = update;
            this.k = (final ? 0 : 1);
        }

        internal Rule(int k, TERM guard, TERM[] yields, TERM[] yields2, TERM update)
        {
            this.guard = guard;
            this.yields = yields;
            this.output = new Sequence<TERM>(yields);
            this.yields2 = yields2;
            this.output2 = new Sequence<TERM>(yields2);
            this.update = update;
            this.k = k;
        }

        /// <summary>
        /// Creates a new rule representing a final output.
        /// </summary>
        /// <param name="guard">predicate over registers</param>
        /// <param name="yields">(possibly empty) array of output terms yielded by the rule</param>
        static public Rule<TERM> MkFinal(TERM guard, params TERM[] yields)
        {
            return new Rule<TERM>(true, guard, yields, default(TERM));
        }


        /// <summary>
        /// Creates a new rule representing a guarded register update.
        /// </summary>
        /// <param name="guard">predicate over input and registers</param>
        /// <param name="update">register update</param>
        /// <param name="yields">(possibly empty) array of output terms yielded by the rule</param>
        static public Rule<TERM> Mk(TERM guard, TERM update, params TERM[] yields)
        {
            return new Rule<TERM>(false, guard, yields, update);
        }

        public override string ToString()
        {
            string g = guard.ToString();
            string o1 = (Output == null ? "" : "/" + Output.ToString());
            string o2 = (Output2 == null ? "" : "/" + Output2.ToString());
            string u = (Update == null ? "" : "; r:=" + Update.ToString());
            string s = String.Format("{4}{0}{1}{2}{3}{5}{6}", g, o1, o2, u, (IsFinal ? "[" : ""), (IsFinal ? "]" : ""),(k > 1 ? "(" + k + ")" : ""));
            return s;
        }
    }

    /// <summary>
    /// There are four kinds of branching rules
    /// </summary>
    public enum BranchingRuleKind { Ite, Base, Undef, Switch};

    /// <summary>
    /// Abstract base class of branching rules.
    /// STbs have branching rules.
    /// </summary>
    /// <typeparam name="TERM">the type of terms</typeparam>
    public abstract class BranchingRule<TERM>
    {
        /// <summary>
        /// Enumerates the underlying branches as moves from the give source state and guard. 
        /// Excludes all raise rules.
        /// </summary>
        /// <param name="solver">solver for composing conditions</param>
        /// <param name="source">source states of the moves</param>
        /// <param name="guard">initial guard of the moves</param>
        /// <returns></returns>
        public abstract IEnumerable<Move<Rule<TERM>>> EnumerateMoves(IContextCore<TERM> solver, int source, TERM guard);


        /// <summary>
        /// Extracts the condition under which the rule raises an exception.
        /// </summary>
        /// <param name="solver">solver for composing conditions</param>
        /// <param name="guard">initial guard of the moves</param>
        /// <returns></returns>
        public abstract TERM ExctractRaiseCondition(IContextCore<TERM> solver, TERM guard);

        public bool TryGetGuardAndUpdate(IContextCore<TERM> solver, int targetState, out TERM guard, out TERM update)
        {
            switch (RuleKind)
            {
                case BranchingRuleKind.Base:
                    {
                        if (State == targetState)
                        {
                            guard = solver.True;
                            update = Register;
                            return true;
                        }
                        else
                        {
                            guard = default(TERM);
                            update = default(TERM);
                            return false;
                        }
                    }
                case BranchingRuleKind.Undef:
                    {
                        guard = default(TERM);
                        update = default(TERM);
                        return false;
                    }
                case BranchingRuleKind.Ite:
                    {
                        TERM t_guard;
                        TERM t;
                        bool t_ok = TrueCase.TryGetGuardAndUpdate(solver, targetState, out t_guard, out t);
                        TERM f_guard;
                        TERM f;
                        bool f_ok = FalseCase.TryGetGuardAndUpdate(solver, targetState, out f_guard, out f);
                        if (t_ok && f_ok)
                        {
                            if (t_guard.Equals(solver.True) && f_guard.Equals(solver.True))
                                guard = solver.True;
                            else
                                guard = solver.MkIte(Condition, t_guard, f_guard);
                            if (object.Equals(t,f))
                                update = t;
                            else
                                update = solver.MkIte(Condition, t, f);
                            return true;
                        }
                        else if (t_ok)
                        {
                            guard = And(solver, t_guard, Condition);
                            update = t;
                            return true;
                        }
                        else if (f_ok)
                        {
                            guard = And(solver, f_guard, Not(solver,Condition));
                            update = f;
                            return true;
                        }
                        else
                        {
                            guard = default(TERM);
                            update = default(TERM);
                            return false;
                        }
                    }
                default:
                    throw new NotImplementedException(BranchingRuleKind.Switch.ToString());
            }
        }

        public abstract BranchingRuleKind RuleKind { get; }

        public bool IsNotUndef
        {
            get
            {
                return !(this is UndefRule<TERM>);
            }
        }


        public virtual TERM Condition
        {
            get
            {
                throw new AutomataException(AutomataExceptionKind.InvalidSTbRuleOperation);
            }
        }
        public virtual BranchingRule<TERM> TrueCase
        {
            get
            {
                throw new AutomataException(AutomataExceptionKind.InvalidSTbRuleOperation);
            }
        }
        public virtual BranchingRule<TERM> FalseCase
        {
            get
            {
                throw new AutomataException(AutomataExceptionKind.InvalidSTbRuleOperation);
            }
        }

        /// <summary>
        /// Gets the sequence of output terms yielded by this rule.
        /// </summary>
        public virtual Sequence<TERM> Yields
        {
            get
            {
                throw new AutomataException(AutomataExceptionKind.InvalidSTbRuleOperation);
            }
        }

        /// <summary>
        /// Gets the register update produced by this rule.
        /// </summary>
        public virtual TERM Register
        {
            get
            {
                throw new AutomataException(AutomataExceptionKind.InvalidSTbRuleOperation);
            }
        }

        /// <summary>
        /// Gets the target state of this rule.
        /// </summary>
        public virtual int State
        {
            get
            {
                throw new AutomataException(AutomataExceptionKind.InvalidSTbRuleOperation);
            }
        }

        //public abstract TERM GetEnabledPredicate(ICoreSolver<TERM> solver, int targetState)
        //{

        //}

        /// <summary>
        /// Enumerates all the target states that occur in the rule.
        /// Yields a state as many times as it occurs in the rule.
        /// </summary>
        public abstract IEnumerable<int> EnumerateStates();

        /// <summary>
        /// Returns a list of all target states, without repetitions, that occur in the rule.
        /// </summary>
        public SimpleList<int> GetAllStates()
        {
            HashSet<int> seen = new HashSet<int>();
            SimpleList<int> states = SimpleList<int>.Empty;
            foreach (int q in EnumerateStates())
                if (seen.Add(q))
                    states = states.Append(q);
            return states;
        }

        /// <summary>
        /// Concretize the rule with respect to the given values.
        /// </summary>
        public BranchingRule<TERM> Concretize(IContextCore<TERM> solver, Func<TERM, TERM> fBP, 
            Func<TERM, TERM> fNBP, Func<int, TERM, int> stateMap, TERM newReg, TERM inputVar)
        {
            return Concretize1(solver.True, solver, fBP, fNBP, stateMap, newReg, inputVar);
        }

        /// <summary>
        /// Replace all states in the rule by q and all registers by r.
        /// </summary>
        /// <param name="q">given state</param>
        /// <param name="r">given register value</param>
        public BranchingRule<TERM> ReplaceAllStatesAndRegisters(int q, TERM r)
        {
            return ReplaceAllStates1(q, r);
        }

        internal abstract BranchingRule<TERM> ReplaceAllStates1(int q, TERM r);

        internal abstract BranchingRule<TERM> Concretize1(TERM path, IContextCore<TERM> solver, Func<TERM, TERM> fBP, 
            Func<TERM, TERM> fNBP, Func<int, TERM, int> stateMap, TERM newReg, TERM inputVar);

        /// <summary>
        /// Create an instance of the rule by applying the substitution to all the components
        /// </summary>
        public BranchingRule<TERM> Subst(IContextCore<TERM> solver, params TERM[] substitution)
        {
            if (substitution.Length % 2 != 0)
                throw new AutomataException(AutomataExceptionKind.InvalidSubstitution);
            var subst = new Dictionary<TERM, TERM>();
            for (int i = 0; i < substitution.Length; i += 2)
                subst[substitution[i]] = substitution[i + 1];
            return Subst1(solver.True, solver, subst);
        }

        /// <summary>
        /// Create an instance of the rule by applying the substitution to all the components
        /// </summary>
        public BranchingRule<TERM> Subst(TERM pathCondition, IContextCore<TERM> solver, params TERM[] substitution)
        {
            if (substitution.Length % 2 != 0)
                throw new AutomataException(AutomataExceptionKind.InvalidSubstitution);
            var subst = new Dictionary<TERM, TERM>();
            for (int i = 0; i < substitution.Length; i += 2)
                subst[substitution[i]] = subst[substitution[i + 1]];
            return Subst1(pathCondition, solver, subst);
        }

        /// <summary>
        /// Create an instance of the rule by replacing the input variable x with the new input term t
        /// </summary>
        public BranchingRule<TERM> ApplyInput(IContextCore<TERM> solver, TERM x, TERM t)
        {
            return ApplyInp(solver.True, solver, x, t);
        }

        internal abstract BranchingRule<TERM> ApplyInp(TERM pred, IContextCore<TERM> solver, TERM x, TERM t);

        /// <summary>
        /// Returns true iff the rule contains a RaiseRule.
        /// </summary>
        public abstract bool IsPartial { get; }

        internal abstract BranchingRule<TERM> Subst1(TERM path, IContextCore<TERM> solver, Dictionary<TERM, TERM> subst);

        internal abstract void ToBek(bool writeState, string tabs, Action<string> WriteLine, 
            Func<TERM, string> DescribeCond, Func<TERM, string> DescribeReg, Func<Sequence<TERM>, string> DescribeYields);

        internal abstract void WriteCode(bool writeState, Action<string> WriteLine, 
            Func<TERM, string> DescribeCond, Func<TERM, string> DescribeReg, 
            Func<Sequence<TERM>, string> DescribeYields, string errorCase);

        internal abstract int GetMaxYieldLength();

        /// <summary>
        /// Returns true iff for all register updates U, proj(U) is a ground value, i.e., independent 
        /// of the previous register and independent of the input character.
        /// </summary>
        /// <param name="prde">must be a valid prdicate over registers</param>
        /// <returns></returns>
        public abstract bool IsTrueForAllRegisterUpdates(Predicate<TERM> pred);

        protected TERM And(IBooleanAlgebra<TERM> solver, TERM a, TERM b)
        {
            if (a.Equals(solver.True))
                return b;
            else if (b.Equals(solver.True))
                return a;
            else if (b.Equals(a))
                return a;
            else
                return solver.MkAnd(a, b);
        }
        protected TERM Or(IBooleanAlgebra<TERM> solver, TERM a, TERM b)
        {
            if (a.Equals(solver.False))
                return b;
            else if (b.Equals(solver.False))
                return a;
            else if (b.Equals(a))
                return a;
            else
                return solver.MkOr(a, b);
        }

        protected TERM Not(IBooleanAlgebra<TERM> solver, TERM pred)
        {
            if (pred.Equals(solver.True))
                return solver.False;
            else if (pred.Equals(solver.False))
                return solver.True;
            else
                return solver.MkNot(pred);
        }

        public BranchingRule<TERM> Prune(Predicate<BaseRule<TERM>> RemoveIfTrue)
        {
            switch (this.RuleKind)
            {
                case BranchingRuleKind.Undef:
                    return this;
                case BranchingRuleKind.Base:
                    {
                        if (RemoveIfTrue((BaseRule<TERM>)this))
                            return UndefRule<TERM>.Default;
                        else
                            return this;

                    }
                case BranchingRuleKind.Ite:
                    {
                        var t = this.TrueCase.Prune(RemoveIfTrue);
                        var f = this.FalseCase.Prune(RemoveIfTrue);
                        if (f == this.FalseCase && t == this.TrueCase)
                            return this;
                        else if (t.RuleKind == BranchingRuleKind.Undef && f.RuleKind == BranchingRuleKind.Undef)
                            return t;
                        else
                            return new IteRule<TERM>(this.Condition, t, f);
                    }
                default:
                    throw new NotImplementedException(BranchingRuleKind.Switch.ToString());
            }
        }
    }

    /// <summary>
    /// Basic rule of an STb. 
    /// </summary>
    /// <typeparam name="TERM">the type of terms on the labels</typeparam>
    public class BaseRule<TERM> : BranchingRule<TERM>
    {
        Sequence<TERM> yields;
        TERM register;
        int state;

        /// <summary>
        /// Creates a new basic rule.
        /// </summary>
        /// <param name="yields">the sequence of output terms yielded by this rule</param>
        /// <param name="register">the register update produced by this rule</param>
        /// <param name="state">the target state of this rule</param>
        public BaseRule(Sequence<TERM> yields, TERM register, int state)
        {
            this.yields = yields;
            this.register = register;
            this.state = state;
        }

        /// <summary>
        /// Gets the sequence of output terms yielded by this rule.
        /// </summary>
        public override Sequence<TERM> Yields
        {
            get { return yields; }
        }

        /// <summary>
        /// Gets the register update produced by this rule.
        /// </summary>
        public override TERM Register
        {
            get { return register; }
        }

        /// <summary>
        /// Gets the target state of this rule.
        /// </summary>
        public override int State
        {
            get { return state; }
        }

        public override IEnumerable<Move<Rule<TERM>>> EnumerateMoves(IContextCore<TERM> solver, int source, TERM guard)
        {
            yield return Move<Rule<TERM>>.Create(source, state, Rule<TERM>.Mk(guard, register, yields.ToArray()));
        }

        public override TERM ExctractRaiseCondition(IContextCore<TERM> solver, TERM guard)
        {
            return solver.False;
        }

        public override string ToString()
        {
            return string.Format("State={0}, Register={1}, Yields={2}", State, Register, Yields);
        }

        public override IEnumerable<int> EnumerateStates()
        {
            yield return state;
        }

        public override bool IsPartial
        {
            get { return false; }
        }

        internal override BranchingRule<TERM> Concretize1(TERM path, IContextCore<TERM> solver, 
            Func<TERM, TERM> fBP, Func<TERM, TERM> fNBP, 
            Func<int, TERM, int> stateMap, TERM newReg, TERM inputVar)
        {
            ConsList<TERM> abstr_vals = null; //all possible values for a_tmp
                        var regAbstr = fBP(register);

            #region collect all possible distinct solutions into abstr_vals
            solver.MainSolver.Push();
            var a = solver.MkFreshConst("a", regAbstr);
            var c = solver.MkFreshConst("c", inputVar);
            var r = solver.MkFreshConst("r", newReg);

            var assertion = solver.ApplySubstitution(solver.MkAnd(path, solver.MkEq(a, regAbstr)), newReg, r, inputVar, c);
            //string tmp = ((IPrettyPrinter<TERM>)solver).PrettyPrint(assertion);
            solver.MainSolver.Assert(assertion);
            var model = solver.MainSolver.GetModel(solver.True, a);
            while (model != null)
            {
                var aVal = model[a].Value;
                abstr_vals = new ConsList<TERM>(aVal, abstr_vals);
                solver.MainSolver.Assert(solver.MkNeq(a, aVal));
                model = solver.MainSolver.GetModel(solver.True, a);
            }
            solver.MainSolver.Pop();
            #endregion

            if (abstr_vals == null) //list cannot be empty because the path is feasible
                throw new AutomataException(AutomataExceptionKind.InternalError); 

            //now create corresponding concrete outputs for the cases
            //note that, in case of a boolean abstraction, we need to add additional cases
            //according to the output values that were obtained

            var newRegister = fNBP(register);
            var newState = stateMap(state, abstr_vals.First);
            BranchingRule<TERM> rule = new BaseRule<TERM>(yields, newRegister, newState);

            abstr_vals = abstr_vals.Rest;
            while (abstr_vals != null)
            {
                newRegister = fNBP(register);
                newState = stateMap(state, abstr_vals.First);
                var brule = new BaseRule<TERM>(yields, newRegister, newState);
                TERM cond = solver.Simplify(solver.MkEq(abstr_vals.First, regAbstr));
                rule = new IteRule<TERM>(cond, brule, rule);
                abstr_vals = abstr_vals.Rest;
            }

            return rule;
        }

        internal override BranchingRule<TERM> Subst1(TERM path, IContextCore<TERM> solver, Dictionary<TERM, TERM> subst)
        {
            Sequence<TERM> newYields
                = new Sequence<TERM>(Array.ConvertAll(yields.ToArray(), y => solver.Simplify(solver.ApplySubstitution(y, subst))));
            TERM newRegister = solver.Simplify(solver.ApplySubstitution(register, subst));
            return new BaseRule<TERM>(newYields, newRegister, state);
        }

        internal override void ToBek(bool writeState, string tabs, Action<string> WriteLine, 
            Func<TERM, string> DescribeCond, Func<TERM, string> DescribeReg, 
            Func<Sequence<TERM>, string> DescribeYields)
        {
            string ys = DescribeYields(yields);
            string rs = DescribeReg(register);
            WriteLine(tabs + ys);
            if (rs != "")
                WriteLine(tabs + rs);
            if (writeState)
                WriteLine(tabs + string.Format("state := {0};", state));
        }

        internal override void WriteCode(bool writeState, Action<string> WriteLine,
            Func<TERM, string> DescribeCond, Func<TERM, string> DescribeReg,
            Func<Sequence<TERM>, string> DescribeYields, string errorCase)
        {
            string ys = DescribeYields(yields);
            string rs = DescribeReg(register);
            if (ys != "")
                WriteLine(ys);
            if (rs != "")
                WriteLine(rs);
            if (writeState)
                WriteLine(string.Format("state = {0};", state));
        }

        internal override int GetMaxYieldLength()
        {
            return yields.Length;
        }

        public override bool IsTrueForAllRegisterUpdates(Predicate<TERM> pred)
        {
            var res = pred(register);
            return res;
        }

        internal override BranchingRule<TERM> ReplaceAllStates1(int q, TERM r)
        {
            return new BaseRule<TERM>(yields, r, q);
        }

        public BranchingRule<TERM> Compose(int q, BranchingRule<TERM> rule, TERM y1, TERM y2, TERM y, TERM predicate, IContextCore<TERM> solver, Func<int, int, int> stateComposer)
        {
            if (rule is UndefRule<TERM>)
                return rule;
            if (this.yields.Length == 0)
            {
                int pq = stateComposer(state, q);
                var register1 = solver.ApplySubstitution(register, y1, solver.MkProj(0, y));
                var register2 = solver.MkProj(1, y);
                var newreg = solver.MkTuple(register1, register2);
                var res = new BaseRule<TERM>(Sequence<TERM>.Empty, newreg, pq);
                return res;
            }
            else
            {
                var y_1 = solver.MkProj(0, y);
                foreach (var o in yields)
                {
                    var o1 = solver.ApplySubstitution(o, y1, y_1);
                    //var rule1 = rule.Subst(solver, y2)

                }
            }

            return null;
        }

        internal override BranchingRule<TERM> ApplyInp(TERM pred, IContextCore<TERM> solver, TERM x, TERM t)
        {
            Sequence<TERM> newYields
                = new Sequence<TERM>(Array.ConvertAll(yields.ToArray(), y => solver.Simplify(solver.ApplySubstitution(y, x, t))));
            TERM newRegister = solver.Simplify(solver.ApplySubstitution(register, x, t));
            return new BaseRule<TERM>(newYields, newRegister, state);
        }

        public override BranchingRuleKind RuleKind
        {
            get { return BranchingRuleKind.Base; }
        }
    }

    /// <summary>
    /// If-then-else rule of an STb.
    /// </summary>
    public class IteRule<TERM> : BranchingRule<TERM>
    {
        TERM condition;
        BranchingRule<TERM> trueCase;
        BranchingRule<TERM> falseCase;

        public IteRule(TERM condition, BranchingRule<TERM> trueCase, BranchingRule<TERM> falseCase)
        {
            this.condition = condition;
            this.trueCase = trueCase;
            this.falseCase = falseCase;
        }

        public override string ToString()
        {
            var condStr = Condition.ToString();
            var fStr = FalseCase.ToString();
            var tStr = TrueCase.ToString();
            return string.Format("(if {0} then {1} else {2})", condStr, tStr, fStr);
        }

        /// <summary>
        /// The branch condition of the rule.
        /// </summary>
        public override TERM Condition
        {
            get { return condition; }
        }

        /// <summary>
        /// The branch taken when branch condition evaluates to true.
        /// </summary>
        public override BranchingRule<TERM> TrueCase
        {
            get { return trueCase; }
        }

        /// <summary>
        /// The branch taken when branch condition evaluates to false.
        /// </summary>
        public override BranchingRule<TERM> FalseCase
        {
            get { return falseCase; }
        }

        public override IEnumerable<Move<Rule<TERM>>> EnumerateMoves(IContextCore<TERM> solver, int source, TERM guard)
        {
            foreach (var move in trueCase.EnumerateMoves(solver, source, And(solver, guard, condition)))
                yield return move;
            foreach (var move in falseCase.EnumerateMoves(solver, source, And(solver, guard, solver.MkNot(condition))))
                yield return move;
        }

        public override TERM ExctractRaiseCondition(IContextCore<TERM> solver, TERM guard)
        {
            TERM tcond = trueCase.ExctractRaiseCondition(solver, And(solver, guard, condition));
            TERM fcond = falseCase.ExctractRaiseCondition(solver, And(solver, guard, solver.MkNot(condition)));
            return Or(solver, tcond, fcond);
        }

        public override IEnumerable<int> EnumerateStates()
        {
            foreach (int state in trueCase.EnumerateStates())
                yield return state;

            foreach (int state in falseCase.EnumerateStates())
                yield return state;
        }

        internal override BranchingRule<TERM> Concretize1(TERM path, IContextCore<TERM> solver, 
            Func<TERM, TERM> fBP, Func<TERM, TERM> fNBP, 
            Func<int, TERM, int> stateMap, TERM newReg, TERM inputVar)
        {
            TERM path_and_cond = solver.MkAnd(path, condition);
            TERM path_and_not_cond = solver.MkAnd(path, solver.MkNot(condition));
            var t = trueCase.Concretize1(path_and_cond, solver, fBP, fNBP, stateMap, newReg, inputVar);
            var f = falseCase.Concretize1(path_and_not_cond, solver, fBP, fNBP, stateMap, newReg, inputVar);
            return new IteRule<TERM>(condition, t, f);
        }


        internal override BranchingRule<TERM> Subst1(TERM path, IContextCore<TERM> solver, Dictionary<TERM,TERM> subst)
        {
            TERM condInst = solver.Simplify(solver.ApplySubstitution(condition, subst));
            TERM path_and_condInst = And(solver, path, condInst);
            TERM path_and_not_condInst = And(solver, path, solver.MkNot(condInst));
            if (!solver.IsSatisfiable(path_and_condInst))
            {
                return falseCase.Subst1(path, solver, subst);
            }
            if (!solver.IsSatisfiable(path_and_not_condInst))
            {
                return trueCase.Subst1(path, solver, subst);
            }
            else
            {
                var t = trueCase.Subst1(path_and_condInst, solver, subst);
                var f = falseCase.Subst1(path_and_not_condInst, solver, subst);
                if ((t is UndefRule<TERM>) && (f is UndefRule<TERM>))
                    return UndefRule<TERM>.Default;
                return new IteRule<TERM>(condInst, t, f);
            }
        }

        internal override void ToBek(bool writeState, string tabs, Action<string> WriteLine, 
            Func<TERM, string> DescribeCond, Func<TERM, string> DescribeReg, 
            Func<Sequence<TERM>, string> DescribeYields)
        {
            string condStr = DescribeCond(condition);
            WriteLine(tabs + "if (" + condStr + ") {");
            trueCase.ToBek(writeState, tabs + STbInfo.TAB, WriteLine, DescribeCond, DescribeReg, DescribeYields);
            WriteLine(tabs + "} else {");
            falseCase.ToBek(writeState, tabs + STbInfo.TAB, WriteLine, DescribeCond, DescribeReg, DescribeYields);
            WriteLine(tabs + "}");
        }

        internal override void WriteCode(bool writeState, Action<string> WriteLine, 
            Func<TERM, string> DescribeCond, Func<TERM, string> DescribeReg, 
            Func<Sequence<TERM>, string> DescribeYields, string errorCase)
        {
            StringBuilder sb_t = new StringBuilder();
            StringBuilder sb_f = new StringBuilder();
            trueCase.WriteCode(writeState, (x => {sb_t.AppendLine(x);}), DescribeCond, DescribeReg, DescribeYields, errorCase);
            falseCase.WriteCode(writeState, (x => {sb_f.AppendLine(x); }), DescribeCond, DescribeReg, DescribeYields, errorCase);
            var t_str = sb_t.ToString();
            var f_str = sb_f.ToString();
            if (t_str.Equals(f_str))
                WriteLine(t_str);
            else
            {
                string condStr = DescribeCond(condition);
                WriteLine("if (" + condStr + ") {");
                WriteLine(t_str + "} else {");
                WriteLine(f_str + "}");
            }
        }

        public override bool IsPartial
        {
            get { return (trueCase.IsPartial || falseCase.IsPartial); }
        }

        internal override int GetMaxYieldLength()
        {
            int m = falseCase.GetMaxYieldLength();
            int n = trueCase.GetMaxYieldLength();
            return (m <= n ? n : m);
        }

        public override bool IsTrueForAllRegisterUpdates(Predicate<TERM> pred)
        {
            return (trueCase.IsTrueForAllRegisterUpdates(pred) && falseCase.IsTrueForAllRegisterUpdates(pred));
        }

        internal override BranchingRule<TERM> ReplaceAllStates1(int q, TERM r)
        {
            return new IteRule<TERM>(this.condition, trueCase.ReplaceAllStates1(q, r), falseCase.ReplaceAllStates1(q, r));
        }

        internal override BranchingRule<TERM> ApplyInp(TERM pred, IContextCore<TERM> solver, TERM x, TERM input)
        {
            throw new NotImplementedException();
        }

        public override BranchingRuleKind RuleKind
        {
            get { return BranchingRuleKind.Ite; }
        }
    }

    /// <summary>
    /// Rule that raises an exception. The resulting state is a nonaccepting state.
    /// </summary>
    /// <typeparam name="TERM">the type of terms on the labels</typeparam>
    public class UndefRule<TERM> : BranchingRule<TERM>
    {
        readonly string exc;
        private static int __RaiseRuleId__ = 2;
        readonly int id;

        /// <summary>
        /// Fixed raise rule with Id=1 and Exc="E".
        /// </summary>
        public static readonly UndefRule<TERM> Default = new UndefRule<TERM>("Undef",1);

        UndefRule(string exc, int id)
        {
            this.exc = exc;
            this.id = id;
        }

        /// <summary>
        /// Creates a new rule that raises an exception. The new raise rule has a new identifier.
        /// </summary>
        /// <param name="exc">Description of the exception raised by the rule.</param>
        public UndefRule(string exc)
        {
            this.exc = exc;
            this.id = __RaiseRuleId__;
            __RaiseRuleId__ += 1;
        }

        /// <summary>
        /// Creates a new raise rule with Exc="E".
        /// </summary>
        public UndefRule()
        {
            this.exc = "E";
            this.id = __RaiseRuleId__;
            __RaiseRuleId__ += 1;
        }

        /// <summary>
        /// Description of the exception raised by the rule.
        /// </summary>
        public string Exc
        {
            get { return exc; }
        }

        /// <summary>
        /// Identifier of this raise rule.
        /// </summary>
        public int Id
        {
            get { return id; }
        }

        public override string ToString()
        {
            return "Undef";
        }

        public override IEnumerable<Move<Rule<TERM>>> EnumerateMoves(IContextCore<TERM> solver, int source, TERM guard)
        {
            yield break;
        }

        public override TERM ExctractRaiseCondition(IContextCore<TERM> solver, TERM guard)
        {
            return guard;
        }

        public override IEnumerable<int> EnumerateStates()
        {
            yield break;
        }

        internal override BranchingRule<TERM> Concretize1(TERM path, IContextCore<TERM> solver, 
            Func<TERM, TERM> fBP, Func<TERM, TERM> fNBP, 
            Func<int, TERM, int> stateMap, TERM newReg, TERM inputVar)
        {
            return this;
        }

        internal override BranchingRule<TERM> Subst1(TERM path, IContextCore<TERM> solver, Dictionary<TERM,TERM> subst)
        {
            return this;
        }

        internal override void ToBek(bool writeState, string tabs, Action<string> WriteLine, 
            Func<TERM, string> DescribeCond, Func<TERM, string> DescribeReg, 
            Func<Sequence<TERM>, string> DescribeYields)
        {
            string s = tabs + "raise " + exc + ";";
            WriteLine(s);
        }

        internal override void WriteCode(bool writeState, Action<string> WriteLine, 
            Func<TERM, string> DescribeCond, Func<TERM, string> DescribeReg, 
            Func<Sequence<TERM>, string> DescribeYields, string errorCase)
        {
            string s = (errorCase != null ? errorCase : "throw new System.Exception(\"" + exc + "\");");
            WriteLine(s);
        }

        public override bool IsPartial
        {
            get { return true; }
        }

        internal override int GetMaxYieldLength()
        {
            return 0;
        }

        public override bool IsTrueForAllRegisterUpdates(Predicate<TERM> pred)
        {
            return true;
        }

        internal override BranchingRule<TERM> ReplaceAllStates1(int q, TERM r)
        {
            return this;
        }

        internal override BranchingRule<TERM> ApplyInp(TERM pred, IContextCore<TERM> solver, TERM x, TERM input)
        {
            throw new NotImplementedException();
        }

        public override BranchingRuleKind RuleKind
        {
            get { return BranchingRuleKind.Undef; }
        }
    }

    /// <summary>
    /// Describes a switch statement.
    /// </summary>
    /// <typeparam name="TERM"></typeparam>
    public class SwitchRule<TERM> : BranchingRule<TERM>
    {
        public TERM input;
        public KeyValuePair<TERM, BranchingRule<TERM>>[] cases;
        public BranchingRule<TERM> defaultcase;

        public SwitchRule(TERM input, BranchingRule<TERM> defaultcase, params KeyValuePair<TERM, BranchingRule<TERM>>[] cases)
        {
            this.input = input;
            this.defaultcase = defaultcase;
            this.cases = cases;
        }

        public override IEnumerable<Move<Rule<TERM>>> EnumerateMoves(IContextCore<TERM> solver, int source, TERM guard)
        {
            TERM defaultcond = solver.True;
            foreach (var c in cases)
            {
                var set = solver.MkEq(input, c.Key);
                defaultcond = And(solver, defaultcond, solver.MkNot(set));
                foreach (var m in c.Value.EnumerateMoves(solver, source, And(solver, guard, set)))
                    yield return m;
            }
            foreach (var m in defaultcase.EnumerateMoves(solver, source, And(solver, guard, defaultcond)))
                yield return m;
        }

        public override TERM ExctractRaiseCondition(IContextCore<TERM> solver, TERM guard)
        {
            TERM cond = solver.False;
            TERM defaultcond = solver.True;
            foreach (var c in cases)
            {
                var set = solver.MkEq(input, c.Key);
                TERM guard1 = And(solver, guard, set);
                TERM cond1 = c.Value.ExctractRaiseCondition(solver, guard1);
                cond = Or(solver, cond, cond1);
                defaultcond = And(solver, defaultcond, solver.MkNot(set));
            }
            cond = Or(solver, cond, defaultcase.ExctractRaiseCondition(solver, And(solver, guard, defaultcond)));
            return cond;
        }

        public override IEnumerable<int> EnumerateStates()
        {
            foreach (var c in cases)
                foreach (var st in c.Value.EnumerateStates())
                    yield return st;
            foreach (var st in defaultcase.EnumerateStates())
                yield return st;
        }

        internal BranchingRule<TERM> ToIteForVisualization()
        {
            var res = defaultcase;
            for (int i = cases.Length-1; i >= 0; i--)
            {
                res = new IteRule<TERM>(cases[i].Key, cases[i].Value, res);
            }
            return res;
        }

        internal BranchingRule<TERM> ToIte(Func<TERM, TERM, TERM> mkEq)
        {
            var res = defaultcase;
            for (int i = cases.Length - 1; i >= 0; i--)
            {
                res = new IteRule<TERM>(mkEq(input, cases[i].Key), cases[i].Value, res);
            }
            return res;
        }

        internal override BranchingRule<TERM> ReplaceAllStates1(int q, TERM r)
        {
            var cases1 = Array.ConvertAll(cases, c => new KeyValuePair<TERM,BranchingRule<TERM>>(c.Key, c.Value.ReplaceAllStates1(q, r)));
            var defaultcase1 = defaultcase.ReplaceAllStates1(q, r);
            var rule = new SwitchRule<TERM>(input, defaultcase1, cases1);
            return rule;
        }

        internal override BranchingRule<TERM> Concretize1(TERM path, IContextCore<TERM> solver, Func<TERM, TERM> fBP, Func<TERM, TERM> fNBP, Func<int, TERM, int> stateMap, TERM newReg, TERM inputVar)
        {
            TERM defaultcond = solver.True;
            var cases1 = new KeyValuePair<TERM, BranchingRule<TERM>>[cases.Length];
            for (int i = 0; i < cases.Length; i++)
            {
                var cond = solver.MkEq(input, cases[i].Key);
                defaultcond = And(solver, defaultcond, solver.MkNot(cond));
                cases1[i] = new KeyValuePair<TERM, BranchingRule<TERM>>(cases[i].Key, cases[i].Value.Concretize1(And(solver, path, cond), solver, fBP, fNBP, stateMap, newReg, inputVar));
            }
            var defaultcase1 = defaultcase.Concretize1(And(solver, path, defaultcond), solver, fBP, fNBP, stateMap, newReg, inputVar);
            var rule = new SwitchRule<TERM>(input, defaultcase1, cases1);
            return rule;
        }

        public override bool IsPartial
        {
            get
            {
                foreach (var c in cases)
                    if (c.Value.IsPartial)
                        return true;

                return defaultcase.IsPartial;
            }
        }

        internal override BranchingRule<TERM> Subst1(TERM path, IContextCore<TERM> solver, Dictionary<TERM,TERM> subst)
        {
            TERM defaultcond = solver.True;
            var cases1 = new KeyValuePair<TERM, BranchingRule<TERM>>[cases.Length];
            for (int i = 0; i < cases.Length; i++)
            {
                var cond = solver.MkEq(input, cases[i].Key);
                defaultcond = And(solver, defaultcond, solver.MkNot(cond));
                cases1[i] = new KeyValuePair<TERM, BranchingRule<TERM>>(cases[i].Key, cases[i].Value.Subst1(And(solver, path, cond), solver, subst));
            }
            var defaultcase1 = defaultcase.Subst1(And(solver, path, defaultcond), solver, subst);
            var rule = new SwitchRule<TERM>(input, defaultcase1, cases1);
            return rule;
        }

        internal override void ToBek(bool writeState, string tabs, Action<string> WriteLine, Func<TERM, string> DescribeCond, Func<TERM, string> DescribeReg, Func<Sequence<TERM>, string> DescribeYields)
        {
            throw new NotImplementedException();
        }

        internal override int GetMaxYieldLength()
        {
            var k = defaultcase.GetMaxYieldLength();
            foreach (var c in cases)
                k = Math.Max(k, c.Value.GetMaxYieldLength());
            return k;
        }

        public override bool IsTrueForAllRegisterUpdates(Predicate<TERM> pred)
        {
            foreach (var c in cases)
                if (!c.Value.IsTrueForAllRegisterUpdates(pred))
                    return false;

            return defaultcase.IsTrueForAllRegisterUpdates(pred);
        }

        internal override void WriteCode(bool writeState, Action<string> WriteLine, Func<TERM, string> DescribeCond, Func<TERM, string> DescribeReg, Func<Sequence<TERM>, string> DescribeYields, string errorCase)
        {
            var caseCodes = new Dictionary<string, string>();
            var caseCodesInv = new Dictionary<string, HashSet<string>>(); 
            string defaultCode;
            var caseVals = new List<string>();

            StringBuilder defaultCodeSB = new StringBuilder();
            defaultcase.WriteCode(writeState, x => { defaultCodeSB.AppendLine(x); return; }, DescribeCond, DescribeReg, DescribeYields, errorCase);
            defaultCode = defaultCodeSB.ToString();

            foreach (var c in cases)
            {
                string v = DescribeCond(c.Key); 
                if (caseCodes.ContainsKey(v))
                    throw new AutomataException(AutomataExceptionKind.InvalidSwitchCase);
                caseVals.Add(v);
                StringBuilder caseSB = new StringBuilder();
                c.Value.WriteCode(writeState, x => { caseSB.AppendLine(x); return; }, DescribeCond, DescribeReg, DescribeYields, errorCase);
                var caseStr = caseSB.ToString();
                caseCodes.Add(v, caseStr);
                if (!caseCodesInv.ContainsKey(caseStr))
                    caseCodesInv[caseStr] = new HashSet<string>();
                caseCodesInv[caseStr].Add(v);
            }

            string inp = DescribeCond(input);

            HashSet<string> covered = new HashSet<string>();

            WriteLine("switch (" + inp + "){");
            foreach (var v in caseVals)
            {
                if (covered.Contains(v))
                    continue;

                string caseCode = caseCodes[v];
                covered.Add(v);
                covered.UnionWith(caseCodesInv[caseCode]);

                foreach (var w in caseCodesInv[caseCode])
                    WriteLine("case (" + w + "):");
                WriteLine("{" + caseCode + " break;}");
            }
            WriteLine("default:");
            WriteLine("{" + defaultCode + " break;}");
            WriteLine("}"); //end of switch
        }

        internal override BranchingRule<TERM> ApplyInp(TERM pred, IContextCore<TERM> solver, TERM x, TERM input)
        {
            throw new NotImplementedException();
        }

        public override BranchingRuleKind RuleKind
        {
            get { return BranchingRuleKind.Switch; }
        }
    }

}
