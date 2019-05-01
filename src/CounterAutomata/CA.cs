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
    public class CA_ST : ITransducer<Rule<Expr>>
    {
        internal int nrOfCounters;
        internal ST<FuncDecl, Expr, Sort> st;
        internal CABuilder_ST cab;

        public int InitialState
        {
            get
            {
                return st.InitialState;
            }
        }

        public IBooleanAlgebra<Rule<Expr>> Algebra
        {
            get
            {
                return st.Algebra;
            }
        }

        internal CA_ST(CABuilder_ST cab, int initialState, Expr[] initalCounterValues,
            IEnumerable<Move<Rule<Expr>>> movesandfinals, string name = "CA") 
        {
            this.cab = cab;
            this.nrOfCounters = initalCounterValues.Length;
            this.st = ST<FuncDecl, Expr, Sort>.Create(cab.z3p, name,
                (initalCounterValues.Length > 0 ?
                   cab.z3p.MkList(initalCounterValues) :
                   cab.z3p.GetNil(cab.z3p.MkListSort(cab.z3p.IntSort))),
                cab.z3p.CharSort, 
                cab.z3p.CharSort, 
                cab.z3p.MkListSort(cab.z3p.IntSort),
                initialState, 
                movesandfinals);
        }

        internal CA_ST(CABuilder_ST cab, int nrOfCounters, ST<FuncDecl, Expr, Sort> st)
        {
            this.cab = cab;
            this.nrOfCounters = nrOfCounters;
            this.st = st;
        }

        public bool IsFinalState(int state)
        {
            return st.IsFinalState(state);
        }

        public IEnumerable<Move<Rule<Expr>>> GetMoves()
        {
            return st.GetMoves();
        }

        public IEnumerable<Move<Rule<Expr>>> GetMovesFrom(int state)
        {
            return st.GetMovesFrom(state);
        }

        public IEnumerable<int> GetStates()
        {
            return st.GetStates();
        }

        public string DescribeState(int state)
        {
            return "q" + state;
        }

        string LookupVarName(Expr v)
        {
            if (v.Equals(cab.inpVar))
                return "x";
            else if (v.Equals(cab.regVar))
                return "c";
            else
                return v.ToString();

        }

        public string DescribeLabel(Rule<Expr> lab)
        {
            if (lab.IsFinal && lab.Guard.Equals(cab.z3p.True) && lab.Yields.Length == 0)
                return "";

            string s = ""; 
            s += cab.z3p.PrettyPrint(lab.guard, LookupVarName);
            s += "/";
            var upd = (lab.update == null ? "" : cab.z3p.PrettyPrint(lab.update, LookupVarName));
            s += (upd == "" ? "" : upd);
            return s;
        }

        public string DescribeStartLabel()
        {
            return cab.z3p.PrettyPrint(st.InitialRegister, LookupVarName);
        }

        public void ShowGraph()
        {
            Microsoft.Automata.DirectedGraphs.DgmlWriter.ShowGraph<Rule<Expr>>(-1,
                this,
                this.st.Name,
                DescribeLabel
                );
        }

        public bool IsFinalRule(Rule<Expr> rule)
        {
            return rule.IsFinal;
        }

        public string DescribeGuard(Rule<Expr> rule)
        {
            return cab.z3p.PrettyPrint(rule.guard, LookupVarName);
        }

        public bool IsGuardTrue(Rule<Expr> rule)
        {
            return rule.Guard.Equals(cab.z3p.True);
        }

        public int GetYieldsLength(Rule<Expr> rule)
        {
            return 0;
        }

        public string DescribeYields(Rule<Expr> rule)
        {
            return "";
        }

        public string DescribeUpdate(Rule<Expr> rule)
        {
            return cab.z3p.PrettyPrint(rule.update, LookupVarName);
        }

    }

    public class CABuilder_ST
    {
        public Z3Provider z3p;
        public Sort inpSort;
        public Sort regSort;
        public Expr regVar;
        public Expr inpVar;
        public CABuilder_ST()
        {
            z3p = new Z3Provider();
            z3p.SetCompactView();
            inpSort = z3p.CharSort;
            regSort = z3p.MkListSort(z3p.IntSort);
            regVar = z3p.MkVar(1, regSort);
            inpVar = z3p.MkVar(0, inpSort);
        }

        public Expr Counter(int j)
        {
            Expr rest = this.regVar;
            for (int i = 0; i < j; i++)
            {
                rest = z3p.MkRestOfList(rest);
            }
            Expr c_j = z3p.MkFirstOfList(rest);
            return c_j;
        }

        public Move<Rule<Expr>> MkTransition(int source, int target, Expr guard = null, Expr update = null)
        {
            var rule = Rule<Expr>.Mk((guard == null ? z3p.True : guard), (update == null ? regVar : update), inpVar);
            var move = Move<Rule<Expr>>.Create(source, target, rule);
            return move;
        }

        public Move<Rule<Expr>> MkFinalizer(int state, Expr guard = null)
        {
            var rule = Rule<Expr>.MkFinal(guard == null ? z3p.True : guard);
            var move = Move<Rule<Expr>>.Create(state, state, rule);
            return move;
        }

        public Expr Incr(Expr e)
        {
            var e1 = z3p.MkAdd(e, z3p.MkInt(1));
            return e1;
        }

        public Expr Decr(Expr e)
        {
            var e1 = z3p.MkSub(e, z3p.MkInt(1));
            return e1;
        }

        public Expr InputGuard(string charclass)
        {
            var aut = z3p.RegexConverter.Convert("^(" + charclass + ")*$", System.Text.RegularExpressions.RegexOptions.Singleline);
            var pred = aut.GetCondition(aut.InitialState, aut.InitialState);
            return pred;
        }

        public Expr MkUpdate(params Expr[] counterValues)
        {
            return z3p.MkList(counterValues);
        }

        public Expr MkUpdate(params int[] counterValues)
        {
            if (counterValues.Length == 0)
                return z3p.GetNil(regSort);
            return z3p.MkList(Array.ConvertAll(counterValues, c => z3p.MkInt(c)));
        }

        public Expr InputGuard(char c)
        {
            var pred = z3p.MkEq(this.inpVar, z3p.MkCharExpr(c));
            return pred;
        }

        public CA_ST MkCntAut(int initialState, Expr[] initalCounterValues,
            IEnumerable<Move<Rule<Expr>>> movesandfinals, string name = "CA")
        {
            var aut = new CA_ST(this, initialState, initalCounterValues, movesandfinals, name);
            return aut;
        }

        public CA_ST Intersect(CA_ST A, CA_ST B)
        {
            var AxB_st = A.st.Compose(B.st);
            AxB_st.Name = A.st.Name + "x" + B.st.Name;
            var AxB = new CA_ST(this, A.nrOfCounters + B.nrOfCounters, AxB_st);
            return AxB;
        }
    }

    public class CA : IAutomaton<Expr>
    {
        /// <summary>
        /// number of counters
        /// </summary>
        internal int k;

        /// <summary>
        /// underlying symbolic automaton
        /// </summary>
        internal Automaton<Expr> aut;

        /// <summary>
        /// final counter predicates of final states
        /// </summary>
        internal Dictionary<int, Expr> Pred;

        /// <summary>
        /// initial predicate of the initial state
        /// </summary>
        internal Expr p0;

        /// <summary>
        /// builder
        /// </summary>
        internal CABuilder cab;

        internal CA(CABuilder cab,
            int initialState,
            Expr initialCondition,
            int nrOfCounters,
            IEnumerable<Move<Expr>> moves,
            Dictionary<int, Expr> finalConditions)
        {
            this.cab = cab;
            this.k = nrOfCounters;
            this.p0 = initialCondition;
            this.Pred = finalConditions;
            this.aut = Automaton<Expr>.Create(cab.z3p, initialState, finalConditions.Keys,
                moves);
            Validate();
        }

        /// <summary>
        /// make sure that variable indices are between 1 and k
        /// </summary>
        private void Validate()
        {
            foreach (var x in cab.z3p.GetVars(p0))
            {
                int index = (int)cab.z3p.GetVarIndex(x);
                if (index <= 0 || (CABuilder.maxCounter & index) > k)
                    throw new AutomataException(AutomataExceptionKind.CounterIndexOutOfRange);
            }
            foreach (var move in aut.GetMoves())
                foreach (var x in cab.z3p.GetVars(move.Label))
                {
                    int index = (int)cab.z3p.GetVarIndex(x);
                    //index 0 is ok, it refers to the input variable
                    if (index < 0 || (CABuilder.maxCounter & index) > k)
                        throw new AutomataException(AutomataExceptionKind.CounterIndexOutOfRange);
                }
            foreach (var kv in Pred)
                foreach (var x in cab.z3p.GetVars(kv.Value))
                {
                    int index = (int)cab.z3p.GetVarIndex(x);
                    if (index <= 0 || (CABuilder.maxCounter & index) > k)
                        throw new AutomataException(AutomataExceptionKind.CounterIndexOutOfRange);
                }
        }

        /// <summary>
        /// underlying symbolic automaton
        /// </summary>
        public Automaton<Expr> Aut
        {
            get { return aut; }
        }

        /// <summary>
        /// initial state
        /// </summary>
        public int InitialState
        {
            get
            {
                return aut.InitialState;
            }
        }

        public IBooleanAlgebra<Expr> Algebra
        {
            get
            {
                return aut.Algebra;
            }
        }

        public bool IsFinalState(int state)
        {
            return aut.IsFinalState(state);
        }

        public IEnumerable<Move<Expr>> GetMoves()
        {
            return aut.GetMoves();
        }

        public IEnumerable<int> GetStates()
        {
            return aut.GetStates();
        }

        public string DescribeState(int state)
        {

            string initialConditionStr = "";
            //if (state == InitialState)
            //{
            //    if (!cab.z3p.True.Equals(initialCondition))
            //        initialConditionStr = "\r\nI:" + cab.z3p.PrettyPrint(initialCondition, LookupVarName);
            //}
            string finalConditionStr = "";
            if (IsFinalState(state))
            {
                var pred = Pred[state];
                if (!cab.z3p.True.Equals(pred))
                    finalConditionStr = ";" + cab.z3p.PrettyPrint(pred, LookupVarName);
            }
            return "q" + state + initialConditionStr + finalConditionStr;
        }

        string LookupVarName(Expr v)
        {
            if (cab.z3p.IsVar(v))
            {
                int index = (int)cab.z3p.GetVarIndex(v);
                if (index == 0)
                    return "x";
                else if (index <= CABuilder.maxCounter)
                    return "c" + index;
                else
                    return "c" + (CABuilder.maxCounter & index).ToString() + "'";
            }
            else
                return v.ToString();
        }

        public string DescribeLabel(Expr lab)
        {
            return cab.z3p.PrettyPrint(lab, LookupVarName);
        }

        public string DescribeStartLabel()
        {
            if (cab.z3p.True.Equals(p0))
                //omit "true" from the initializer transition
                return "";
            else
                return cab.z3p.PrettyPrint(p0, LookupVarName);
        }

        public void ShowGraph(string name = "CA")
        {
            Microsoft.Automata.DirectedGraphs.DgmlWriter.ShowGraph<Expr>(-1,
                this,
                name);
        }

        public IEnumerable<Move<Expr>> GetMovesFrom(int state)
        {
            throw new NotImplementedException();
        }
    }

    public class CABuilder
    {
        internal const int maxCounter = 0xFFFF;

        public Z3Provider z3p;
        public Sort inpSort;
        public Expr inpVar;
        public CABuilder()
        {
            z3p = new Z3Provider();
            z3p.SetCompactView();
            inpSort = z3p.CharSort;
            inpVar = z3p.CharVar;
        }

        public Expr Ctr(int i)
        {
            if (!(1 <= i && i <= maxCounter))
                throw new AutomataException(AutomataExceptionKind.CounterIndexOutOfRange);
            var c = z3p.MkVar((uint)i, z3p.IntSort);
            return c;
        }

        /// <summary>
        /// Next value of counter i
        /// </summary>
        public Expr Ctr_(int i)
        {
            if (!(1 <= i && i <= maxCounter))
                throw new AutomataException(AutomataExceptionKind.CounterIndexOutOfRange);
            int i_ = i | (1 << 16);
            var c = z3p.MkVar((uint)i_, z3p.IntSort);
            return c;
        }

        public Move<Expr> MkTransition(int source, int target, Expr guard)
        {
            var move = Move<Expr>.Create(source, target, guard);
            return move;
        }

        public Move<Rule<Expr>> MkFin(int state, Expr guard = null)
        {
            var rule = Rule<Expr>.MkFinal(guard == null ? z3p.True : guard);
            var move = Move<Rule<Expr>>.Create(state, state, rule);
            return move;
        }

        public Expr Incr(Expr e)
        {
            var e1 = z3p.MkAdd(e, z3p.MkInt(1));
            return e1;
        }

        public Expr Decr(Expr e)
        {
            var e1 = z3p.MkSub(e, z3p.MkInt(1));
            return e1;
        }

        public Expr InputGuard(string charclass)
        {
            var aut = z3p.RegexConverter.Convert("^(" + charclass + ")*$", System.Text.RegularExpressions.RegexOptions.Singleline);
            if (aut.StateCount != 1)
                throw new AutomataException(AutomataExceptionKind.InvalidRegexCharacterClass);

            var pred = aut.GetCondition(aut.InitialState, aut.InitialState);
            return pred;
        }

        public Expr InputGuard(char c)
        {
            var pred = z3p.MkEq(this.inpVar, z3p.MkCharExpr(c));
            return pred;
        }

        public CA MkCA(
            int initialState, 
            int firstCounterIndex, 
            int nrOfCounters,
            Expr initCond,
            IEnumerable<Move<Expr>> moves, 
            IEnumerable<KeyValuePair<int,Expr>> finalConditions)
        {
            var finalCondMap = new Dictionary<int, Expr>();
            foreach (var kv in finalConditions)
                finalCondMap[kv.Key] = kv.Value;
            var ca = new CA(this, initialState, initCond, nrOfCounters,
                moves, finalCondMap);
            return ca;
        }


        /// <summary>
        /// Assumes that psi has the form inpguard(x) & &_i ctrguard1(ci) & &_i ci'=ti 
        /// </summary>
        /// <param name="psi">formula encoding the whole transition</param>
        /// <param name="ctrguard">guard over counter variables c1..ck</param>
        /// <param name="inpguard">guard over input variable x</param>
        /// <param name="updates">array of length k containing update ti in position i</param>
        public void SplitCase(Expr psi, out Expr ctrguard, out Expr inpguard, out Expr[] updates)
        {

            //TBD

            var f = z3p.GetAppDecl(psi);
            if (f.DeclKind != Z3_decl_kind.Z3_OP_AND)
                throw new AutomataException(AutomataExceptionKind.ExpectingConjunction);

            var conjuncts = z3p.GetAppArgs(psi);
            if (conjuncts.Length != 3)
                throw new AutomataException(AutomataExceptionKind.InvalidArgument);

            inpguard = conjuncts[0];
            ctrguard = conjuncts[1];
            updates = z3p.GetAppArgs(conjuncts[2]);
        }

        //public CA Intersect(CA A, CA B)
        //{
        //    //first rename shift all variables in B by A.k amount
        //    var subst = new Dictionary<Expr, Expr>();
        //    for (int i = 1; i <= B.k; i++)
        //    {
        //        subst[Ctr(i)] = Ctr(i + A.k);
        //        subst[Ctr_(i)] = Ctr_(i + A.k);
        //    }
        //    var B1 = B.aut.RelpaceAllGuards(x => z3p.ApplySubstitution(x, subst));
        //    //lift A to rules


        //    var AxB = Automaton<Expr>.MkProduct_(A,B1, )
        //}

    }

    //public class CARule : Tuple<bool,Expr,Sequence<Expr>>
    //{
    //    public CAAlgebra alg;
    //    public CARule(CAAlgebra alg) : base(false, alg.cab.z3p.True, Sequence<Expr>.Empty)
    //    {
    //        this.alg = alg;
    //    }
    //    public CARule(CAAlgebra alg, bool isFinal, Expr inputGuard, params Expr[] counterUpdates) : base(isFinal, inputGuard, new Sequence<Expr>(counterUpdates))
    //    {
    //        this.alg = alg;
    //    }

    //    /// <summary>
    //    /// when true then the guard is a predicate over counters only
    //    /// </summary>
    //    public bool IsFinal
    //    {
    //        get { return this.Item1; }
    //    }

    //    public Expr Guard
    //    {

    //    }
    //}

    //public class CAAlgebra : IBooleanAlgebra<CARule>
    //{
    //    public CABuilder cab;
    //    public CAAlgebra(CABuilder cab)
    //    {
    //        this.cab = cab;
    //    }

    //    public CARule False
    //    {
    //        get
    //        {
    //            throw new NotImplementedException();
    //        }
    //    }

    //    public bool IsAtomic
    //    {
    //        get
    //        {
    //            throw new NotImplementedException();
    //        }
    //    }

    //    public bool IsExtensional
    //    {
    //        get
    //        {
    //            throw new NotImplementedException();
    //        }
    //    }

    //    public CARule True
    //    {
    //        get
    //        {
    //            throw new NotImplementedException();
    //        }
    //    }

    //    public bool AreEquivalent(CARule predicate1, CARule predicate2)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public bool CheckImplication(CARule lhs, CARule rhs)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public bool EvaluateAtom(CARule atom, CARule psi)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public IEnumerable<Tuple<bool[], CARule>> GenerateMinterms(params CARule[] constraints)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public CARule GetAtom(CARule psi)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public bool IsSatisfiable(CARule predicate)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public CARule MkAnd(params CARule[] predicates)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public CARule MkAnd(IEnumerable<CARule> predicates)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public CARule MkAnd(CARule predicate1, CARule predicate2)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public CARule MkDiff(CARule predicate1, CARule predicate2)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public CARule MkNot(CARule predicate)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public CARule MkOr(IEnumerable<CARule> predicates)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public CARule MkOr(CARule predicate1, CARule predicate2)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public CARule MkSymmetricDifference(CARule p1, CARule p2)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public CARule Simplify(CARule predicate)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
