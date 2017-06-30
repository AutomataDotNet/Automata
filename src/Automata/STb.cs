using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using System.Net;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Reflection;

using Microsoft.Automata;

namespace Microsoft.Automata
{
    /// <summary>
    /// Interface to deterministic symbolic transducers with branching rules.
    /// </summary>
    /// <typeparam name="TERM">type of terms</typeparam>
    public interface ISTb<TERM> : IPrettyPrinter<TERM>, IRegisterInfo<TERM>
    {
        /// <summary>
        /// Get the nonfinal rule from the given state.
        /// </summary>
        /// <param name="state">given start state</param>
        BranchingRule<TERM> GetRuleFrom(int state);

        /// <summary>
        /// Get the final rule from the given state.
        /// </summary>
        /// <param name="state">given start state</param>
        BranchingRule<TERM> GetFinalRuleFrom(int state);

        /// <summary>
        /// Gets the initial state.
        /// </summary>
        int InitialState { get; }

        /// <summary>
        /// List of all the states. 
        /// </summary>
        List<int> States { get; }

        string Name { get; }

    }

    /// <summary>
    /// Interface for checking a predicate over register updates.
    /// </summary>
    /// <typeparam name="TERM">type of terms</typeparam>
    public interface IRegisterInfo<TERM>
    {
        /// <summary>
        /// Returns true iff the predicate holds for all register update terms.
        /// </summary>
        bool IsTrueForAllRegisterUpdates(Predicate<TERM> pred);

        /// <summary>
        /// Gets the initial register.
        /// </summary>
        TERM InitialRegister { get; }
    }

    internal static class STbInfo
    {
        internal const string __input_variable = "c";
        internal const string __register_variable = "r";
        internal const string __empty_tuple = "T()";
        internal const string TAB = "  ";

        internal static void WriteLine(string line, TextWriter tw)
        {
            tw.WriteLine(line);
        }

        internal static void WriteLine(string line, StringBuilder sb)
        {
            sb.AppendLine(line);
        }
    }
    
    /// <summary>
    /// Symbolic Transducer with branching rules.
    /// </summary>
    public class STb<FUNC, TERM, SORT> : ISTb<TERM>
    {
        string name;
        IContext<FUNC, TERM, SORT> solver;
        SORT inputSort;
        SORT outputSort;
        SORT regSort;
        TERM inputVar; 
        TERM regVar;
        public TERM RegVar { get { return regVar; } }
        public TERM InputVar { get { return inputVar; } }
        TERM initReg; 
        SORT inListSort;
        SORT outListSort;
        int initState;
        Dictionary<int, BranchingRule<TERM>> ruleMap = new Dictionary<int, BranchingRule<TERM>>();
        Dictionary<int, BranchingRule<TERM>> finalMap = new Dictionary<int, BranchingRule<TERM>>();
        STBuilder<FUNC, TERM, SORT> stb;
        HashSet<int> stateSet;
        List<int> stateList;

        ST<FUNC, TERM, SORT> _ST = null;
        ST<FUNC, TERM, SORT> _ST_without_yields = null;
        /// <summary>
        /// Gets or sets the underlying ST, or null if it does not exist.
        /// </summary>
        public ST<FUNC, TERM, SORT> ST
        {
            get { return _ST; }
            set { _ST = value; }
        }

        int GetMaxIterYieldLength()
        {
            int m = 0;
            foreach (var rule in ruleMap.Values)
            {
                int n = rule.GetMaxYieldLength();
                if (n > m)
                    m = n;
            }
            return m;
        }

        int GetMaxFinalYieldLength()
        {
            int m = 0;
            foreach (var rule in finalMap.Values)
            {
                int n = rule.GetMaxYieldLength();
                if (n > m)
                    m = n;
            }
            return m;
        }

        public List<int> States
        {
            get { return stateList; }
        }

        public STb(IContext<FUNC, TERM, SORT> solver,
            string name, SORT inputSort, SORT outputSort, SORT regSort, TERM initReg, int initState)
        {
            this.name = name;
            this.solver = solver;
            this.stb = new STBuilder<FUNC, TERM, SORT>(solver);
            this.inputSort = inputSort;
            this.outputSort = outputSort;
            this.regSort = regSort;
            this.inputVar = solver.MkVar(0, inputSort);
            this.regVar = solver.MkVar(1, regSort);
            this.initReg = initReg;
            this.inListSort = solver.MkListSort(inputSort);
            this.outListSort = solver.MkListSort(outputSort);
            this.initState = initState;
            stateSet = new HashSet<int>();
            stateList = new List<int>();
            stateSet.Add(initState);
            stateList.Add(initState);
        }

        string LookupVarName(TERM term)
        {
            if (solver.GetVarIndex(term) == 0)
                return STbInfo.__input_variable;
            else if (solver.GetVarIndex(term) == 1)
                return STbInfo.__register_variable;
            else
                return term.ToString();
        }

        /// <summary>
        /// The associated SMT solver.
        /// </summary>
        public IContext<FUNC, TERM, SORT> Solver
        {
            get { return solver; }
        }

        public SORT InputSort { get { return inputSort; } }
        public SORT InputListSort { get { return inListSort; } }
        public SORT OutputSort { get { return outputSort; } }
        public SORT OutputListSort { get { return outListSort; } }
        public SORT RegisterSort { get { return regSort; } }
        public int StateCount { get { return stateSet.Count; } }

        #region INameProvider Members

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new AutomataException(AutomataExceptionKind.InvalidAutomatonName);
                name = value;
            }
        }

        #endregion

        /// <summary>
        /// Assigns the rule to the given state. Overwrites any previously assigned rule.
        /// </summary>
        /// <param name="state">start state</param>
        /// <param name="rule">the rule that applies from the given state</param>
        public void AssignRule(int state, BranchingRule<TERM> rule)
        {
            if (rule == null || rule.RuleKind == BranchingRuleKind.Undef)
                this.ruleMap.Remove(state);
            else
            {
                this.ruleMap[state] = rule;
                if (stateSet.Add(state))
                    stateList.Add(state);
                foreach (int s in rule.EnumerateStates())
                    if (stateSet.Add(s))
                        stateList.Add(s);
            }
        }

        /// <summary>
        /// Assigns the final rule to the given state. Overwrites any previously assigned rule.
        /// </summary>
        /// <param name="state">start state</param>
        /// <param name="rule">the final rule that applies from the given state</param>
        public void AssignFinalRule(int state, BranchingRule<TERM> rule)
        {
            if (rule == null || rule.RuleKind == BranchingRuleKind.Undef)
                this.finalMap.Remove(state);
            else
            {
                this.finalMap[state] = rule;
                if (stateSet.Add(state))
                    stateList.Add(state);
                foreach (int s in rule.EnumerateStates())
                    if (stateSet.Add(state))
                        stateList.Add(state);
            }
        }

        /// <summary>
        /// Returns true iff the given state is associated with a final rule.
        /// </summary>
        public bool IsFinalState(int state)
        {
            return finalMap.ContainsKey(state);
        }

        /// <summary>
        /// Gets the nonfinal rule assigned to the given start state. 
        /// </summary>
        /// <param name="state">given start state</param>
        public BranchingRule<TERM> GetRuleFrom(int state)
        {
            BranchingRule<TERM> rule;
            if (!ruleMap.TryGetValue(state, out rule))
                rule = UndefRule<TERM>.Default;
            return rule;
        }

        /// <summary>
        /// Gets the final rule assigned to the given start state. 
        /// </summary>
        /// <param name="state">given start state</param>
        public BranchingRule<TERM> GetFinalRuleFrom(int state)
        {
            BranchingRule<TERM> rule;
            if (!finalMap.TryGetValue(state, out rule))
                rule = UndefRule<TERM>.Default;
            return rule;
        }

        /// <summary>
        /// Gets the initial state.
        /// </summary>
        public int InitialState { get { return initState; } }

        /// <summary>
        /// Gets the initial register.
        /// </summary>
        public TERM InitialRegister { get { return initReg; } }

        #region initialize ST creation info
        bool _STregDefined = false;
        TERM _STreg = default(TERM);
        SORT _STregSort = default(SORT);
        TERM _haltVar = default(TERM);
        TERM _regVar = default(TERM);
        TERM _STinitReg = default(TERM);
        private void DefineSTreg()
        {
            if (_STregDefined)
                return;

            _STregDefined = true;
            _STregSort = (regSort.Equals(solver.UnitSort) ? solver.BoolSort : solver.MkTupleSort(solver.BoolSort, regSort));
            _STreg = stb.MkRegister(_STregSort);
            _haltVar = (regSort.Equals(solver.UnitSort) ? stb.MkRegister(solver.BoolSort) : solver.MkProj(0, _STreg));
            _regVar = (regSort.Equals(solver.UnitSort) ? solver.UnitConst : solver.MkProj(1, _STreg));
            _STinitReg = (regSort.Equals(solver.UnitSort) ? solver.False : solver.MkTuple(solver.False, initReg));
        }
        #endregion 
        TERM STreg 
        {
            get{
                return _STreg;
            }
        }
        TERM HaltVar
        {
            get
            {
                return _haltVar;
            }
        }
        TERM STb_regProj
        {
            get
            {
                return _regVar;
            }
        }
        TERM STinitReg
        {
            get
            {
                return _STinitReg;
            }
        }
        SORT STregSort
        {
            get
            {
                return _STregSort;
            }
        }

        IEnumerable<Move<Rule<TERM>>> EnumerateFlatMoves_orig()
        {
            foreach (int state in stateList)
            {
                #region nonfinal moves
                var rule = GetRuleFrom(state);
                if (rule.IsNotUndef)
                {
                    foreach (var move in rule.EnumerateMoves(solver, state, solver.True))
                    {
                        var STupdate = (regSort.Equals(solver.UnitSort) ? solver.False : //keep the haltflag false
                            solver.MkTuple(solver.False, solver.ApplySubstitution(move.Label.Update, regVar, STb_regProj)));
                        var STguard = stb.And(solver.MkNot(HaltVar), solver.ApplySubstitution(move.Label.Guard, regVar, STb_regProj));
                        var STyields =  Array.ConvertAll(move.Label.Yields, y => solver.ApplySubstitution(y, regVar, STb_regProj));
                        Rule<TERM> STrule = Rule<TERM>.Mk(STguard, STupdate, STyields);
                        yield return Move<Rule<TERM>>.Create(state, move.TargetState, STrule);
                    }
                    var raiseCond = rule.ExctractRaiseCondition(solver, solver.True);
                    if (!raiseCond.Equals(solver.False))
                    {
                        var STraiseCond = stb.And(solver.MkNot(HaltVar), solver.ApplySubstitution(raiseCond, regVar, STb_regProj));
                        var STupdate = (regSort.Equals(solver.UnitSort) ? solver.True : solver.MkTuple(solver.True, STb_regProj));
                        Rule<TERM> STrule = Rule<TERM>.Mk(STraiseCond, STupdate);
                        yield return Move<Rule<TERM>>.Create(state, state, STrule);
                    }
                }
                #endregion

                #region final moves
                var frule = GetFinalRuleFrom(state);
                if (frule.IsNotUndef)
                {
                    var raiseCond = frule.ExctractRaiseCondition(solver, solver.True);
                    var STraiseCond = stb.Or(HaltVar, (raiseCond.Equals(solver.False) ? solver.False :
                        solver.ApplySubstitution(raiseCond, regVar, STb_regProj)));
                    var noHalt = solver.MkNot(STraiseCond);

                    foreach (var move in frule.EnumerateMoves(solver, state, solver.True))
                    {
                        Rule<TERM> STrule = Rule<TERM>.MkFinal(
                            stb.And(noHalt, solver.ApplySubstitution(move.Label.Guard, regVar, STb_regProj)),
                            Array.ConvertAll(move.Label.Yields, y => solver.ApplySubstitution(y, regVar, STb_regProj)));
                        yield return Move<Rule<TERM>>.Create(state, move.TargetState, STrule);
                    }
                }
                #endregion
            }
        }

        IEnumerable<Move<Rule<TERM>>> EnumerateFlatMoves(bool omitYields = false)
        {
            if (omitYields)
            {
                return EnumerateFlatMovesWithoutYields();
            }
            else
            {
                return EnumerateFlatMovesWithYields();
            }
        }

        public IEnumerable<Tuple<ConsList<bool>, BaseRule<TERM>, Move<Rule<TERM>>>> EnumerateNonFinalMovesWithDirections(int state)
        {
            foreach (var elem in EnumerateNonFinalMovesWithDirectionsFor(state, GetRuleFrom(state), solver.True))
                yield return elem;
        }

        IEnumerable<Tuple<ConsList<bool>, BaseRule<TERM>, Move<Rule<TERM>>>> EnumerateNonFinalMovesWithDirectionsFor(int state, BranchingRule<TERM> stbrule, TERM pathCondition)
        {
            switch (stbrule.RuleKind)
            {
                case BranchingRuleKind.Undef:
                    yield break;
                case BranchingRuleKind.Base:
                    var rule1 = Rule<TERM>.Mk(pathCondition, stbrule.Register, stbrule.Yields.ToArray());
                    yield return new Tuple<ConsList<bool>, BaseRule<TERM>, Move<Rule<TERM>>>(null, (BaseRule<TERM>)stbrule, Move<Rule<TERM>>.Create(state, stbrule.State, rule1));
                    break;
                case BranchingRuleKind.Ite:
                    foreach (var m in EnumerateNonFinalMovesWithDirectionsFor(state, stbrule.TrueCase, And(pathCondition, stbrule.Condition)))
                        yield return new Tuple<ConsList<bool>, BaseRule<TERM>, Move<Rule<TERM>>>(new ConsList<bool>(true, m.Item1), m.Item2, m.Item3);
                    foreach (var m in EnumerateNonFinalMovesWithDirectionsFor(state, stbrule.FalseCase, And(pathCondition, solver.MkNot(stbrule.Condition))))
                        yield return new Tuple<ConsList<bool>, BaseRule<TERM>, Move<Rule<TERM>>>(new ConsList<bool>(false, m.Item1), m.Item2, m.Item3);
                    break;
                default:
                    throw new NotImplementedException(BranchingRuleKind.Switch.ToString());
            }
        }

        public IEnumerable<Tuple<ConsList<bool>, BaseRule<TERM>, Move<Rule<TERM>>>> EnumerateFinalMovesWithDirections(int state)
        {
            foreach (var elem in EnumerateFinalMovesWithDirectionsFor(state, GetFinalRuleFrom(state), solver.True))
                yield return elem;
        }

        IEnumerable<Tuple<ConsList<bool>, BaseRule<TERM>, Move<Rule<TERM>>>> EnumerateFinalMovesWithDirectionsFor(int state, BranchingRule<TERM> stbrule, TERM pathCondition)
        {
            switch (stbrule.RuleKind)
            {
                case BranchingRuleKind.Undef:
                    yield break;
                case BranchingRuleKind.Base:
                    var rule1 = Rule<TERM>.MkFinal(pathCondition, stbrule.Yields.ToArray());
                    yield return new Tuple<ConsList<bool>, BaseRule<TERM>, Move<Rule<TERM>>>(null, (BaseRule<TERM>)stbrule, Move<Rule<TERM>>.Create(state, state, rule1));
                    break;
                case BranchingRuleKind.Ite:
                    foreach (var m in EnumerateFinalMovesWithDirectionsFor(state, stbrule.TrueCase, And(pathCondition, stbrule.Condition)))
                        yield return new Tuple<ConsList<bool>, BaseRule<TERM>, Move<Rule<TERM>>>(new ConsList<bool>(true, m.Item1), m.Item2, m.Item3);
                    foreach (var m in EnumerateFinalMovesWithDirectionsFor(state, stbrule.FalseCase, And(pathCondition, solver.MkNot(stbrule.Condition))))
                        yield return new Tuple<ConsList<bool>, BaseRule<TERM>, Move<Rule<TERM>>>(new ConsList<bool>(false, m.Item1), m.Item2, m.Item3);
                    break;
                default:
                    throw new NotImplementedException(BranchingRuleKind.Switch.ToString());
            }
        }

        TERM And(TERM psi, TERM phi)
        {
            if (psi.Equals(solver.True))
                return phi;
            else if (phi.Equals(solver.True))
                return psi;
            else
                return solver.MkAnd(psi, phi);
        }



        IEnumerable<Move<Rule<TERM>>> EnumerateFlatMovesWithoutYields()
        {
            foreach (int state in stateList)
            {
                #region nonfinal moves
                var rule = GetRuleFrom(state);
                var targetStates = rule.GetAllStates();
                while (targetStates.IsNonempty)
                {
                    int targetState = targetStates.Last;
                    targetStates = targetStates.Butlast;
                    TERM guard;
                    TERM update;
                    bool ok = rule.TryGetGuardAndUpdate(solver, targetState, out guard, out update);
                    if (!ok)
                        throw new AutomataException(AutomataExceptionKind.InternalError);
                    yield return Move<Rule<TERM>>.Create(state, targetState, Rule<TERM>.Mk(guard, update));
                }
                #endregion

                #region final moves
                var frule = GetFinalRuleFrom(state);
                if (frule.IsNotUndef)
                {
                    TERM fguard;
                    TERM _;
                    if (frule.TryGetGuardAndUpdate(solver, state, out fguard, out _))
                        yield return Move<Rule<TERM>>.Create(state, state, Rule<TERM>.MkFinal(fguard));
                }
                #endregion
            }
        }

        IEnumerable<Move<Rule<TERM>>> EnumerateFlatMovesWithYields()
        {
            foreach (int state in stateList)
            {
                #region nonfinal moves
                var rule = GetRuleFrom(state);
                if (rule.IsNotUndef)
                {
                    foreach (var move in rule.EnumerateMoves(solver, state, solver.True))
                    {
                        yield return move;
                    }
                }
                #endregion

                #region final moves
                var frule = GetFinalRuleFrom(state);
                if (frule.IsNotUndef)
                {
                    foreach (var move in frule.EnumerateMoves(solver, state, solver.True))
                    {
                        yield return Move<Rule<TERM>>.Create(state, state, Rule<TERM>.MkFinal(move.Label.Guard, move.Label.Yields));
                    }
                }
                #endregion
            }
        }

        private TERM MkIteExpression(BranchingRule<TERM> rule)
        {
            var br = rule as BaseRule<TERM>;
            if (br != null)
                return br.Register;
            var ite = (IteRule<TERM>)rule;
            return solver.MkIte(ite.Condition, MkIteExpression(ite.TrueCase), MkIteExpression(ite.FalseCase));
        }



        /// <summary>
        /// Convert the STb to an equivalent ST by flattening the branching rules to individual moves.
        /// </summary>
        public ST<FUNC, TERM, SORT> ToST_orig()
        {
            DefineSTreg();

            var st = ST<FUNC, TERM, SORT>.Create(solver, string.Format("ST_{0}", name), STinitReg, inputSort, outputSort, STregSort, initState, EnumerateFlatMoves_orig());
            st._STb = this;  //record the original STb
            return st;
        }

        /// <summary>
        /// Convert the STb to an equivalent ST by flattening the branching rules to individual moves.
        /// The original state ids are preserved.
        /// Omits yields if omitYields is true.
        /// </summary>
        /// <param name="omitYields">if true, omit all yields from the generated ST, otherwise preserve the yields</param>
        /// <returns></returns>
        public ST<FUNC, TERM, SORT> ToST(bool omitYields = false)
        {
            if (omitYields)
            {
                if (_ST_without_yields != null)
                    return _ST_without_yields;
            }
            else
            {
                if (_ST != null)
                    return _ST;
            }

            var st = ST<FUNC, TERM, SORT>.Create(solver, name, initReg, inputSort, outputSort, regSort, initState, EnumerateFlatMoves(omitYields));
            if (omitYields)
            {
                _ST_without_yields = st;
            }
            else
            {
                _ST = st;
                st._STb = this;  //record the full conversion also in the st
            }
            return st;
        }

        #region IPrettyPrinter<TERM> Members

        public string PrettyPrint(TERM t)
        {
            return solver.PrettyPrint(t, (x => (x.Equals(inputVar) ? STbInfo.__input_variable : (x.Equals(regVar) ? STbInfo.__register_variable : solver.PrettyPrint(x)))));
        }

        public string PrettyPrint(TERM t, Func<TERM, string> varLookup)
        {
            return solver.PrettyPrint(t, varLookup);
        }

        #endregion

        #region graph visualization
        /// <summary>
        /// Saves the STb in dgml format in Name.dgml file in the working directory and starts a process for viewing the file.
        /// </summary>
        public void ShowGraph()
        {
            DirectedGraphs.DgmlWriter.ShowSTb(-1, this);
        }

        /// <summary>
        /// Saves the STb in dgml format in Name.dgml file in the working directory and starts a process for viewing the file.
        /// Restricts the edge labels to at most k characters.
        /// </summary>
        public void ShowGraph(int k)
        {
            DirectedGraphs.DgmlWriter.ShowSTb(k, this);
        }

        /// <summary>
        /// Saves the STb in dgml format in Name.dgml file in the working directory.
        /// </summary>
        public void ToDgml()
        {
            string filename = (Name.EndsWith(".dgml") ? name : name + ".dgml");
            StreamWriter sw = new StreamWriter(filename);
            DirectedGraphs.DgmlWriter.STbToDgml(-1, this, sw);
            sw.Close();
        }

        /// <summary>
        /// Saves the STb in dgml format in Name.dgml file in the working directory.
        /// Restricts the edge labels to at most k characters.
        /// </summary>
        public void ToDgml(int k)
        {
            string filename = (Name.EndsWith(".dgml") ? name : name + ".dgml");
            StreamWriter sw = new StreamWriter(filename);
            DirectedGraphs.DgmlWriter.STbToDgml(k, this, sw);
            sw.Close();
        }

        /// <summary>
        /// Saves the STb in dot format in <name>Name</name>.dot file in the working directory.
        /// </summary>
        public void ToDot()
        {
            string filename = (Name.EndsWith(".dot") ? name : name + ".dot");
            StreamWriter sw = new StreamWriter(filename);
            DirectedGraphs.DotWriter.STbToDot(-1, this, sw);
            sw.Close();
        }

        /// <summary>
        /// Saves the STb in dot format in the given file (adds the suffix '.dot' when mssing) in the working directory.
        /// </summary>
        public void ToDot(string file)
        {
            string filename = (file.EndsWith(".dot") ? file : file + ".dot");
            StreamWriter sw = new StreamWriter(filename);
            DirectedGraphs.DotWriter.STbToDot(-1, this, sw);
            sw.Close();
        }

        /// <summary>
        /// Saves the STb in dot format using the given text writer.
        /// </summary> 
        public void ToDot(System.IO.TextWriter tw)
        {
            DirectedGraphs.DotWriter.STbToDot(-1, this, tw);
        }

        /// <summary>
        /// Saves the STb in dot format in <name>Name</name>.dot file in the working directory.
        /// Restricts the edge labels to at most k characters.
        /// </summary>
        public void ToDot(int k)
        {
            string filename = (Name.EndsWith(".dot") ? name : name + ".dot");
            StreamWriter sw = new StreamWriter(filename);
            DirectedGraphs.DotWriter.STbToDot(k, this, sw);
            sw.Close();
        }

        /// <summary>
        /// Saves the STb in dot format in the given file (adds the suffix '.dot' when mssing) in the working directory.
        /// Restricts the edge labels to at most k characters.
        /// </summary>
        public void ToDot(int k, string file)
        {
            string filename = (file.EndsWith(".dot") ? file : file + ".dot");
            StreamWriter sw = new StreamWriter(filename);
            DirectedGraphs.DotWriter.STbToDot(k, this, sw);
            sw.Close();
        }

        /// <summary>
        /// Saves the STb in dot format using the given text writer.
        /// Restricts the edge labels to at most k characters.
        /// </summary> 
        public void ToDot(int k, System.IO.TextWriter tw)
        {
            DirectedGraphs.DotWriter.STbToDot(k, this, tw);
        }

        #endregion

        #region Exploration

        /// <summary>
        /// Computes an equivalent finite state symbolic transducer (if one exists) by exploring the registers using DFS.
        /// The computation does not terminate when the number of resulting states is infinite.
        /// The resulting ST does not use registers and therefore has register sort Solver.UnitSort, 
        /// initial register is Solver.UnitConst, and all (nonfinal) rules have register update Solver.UnitConst.
        /// The exploration preserves the branching structure.
        /// </summary>
        public STb<FUNC, TERM, SORT> Explore()
        {
            if (__Explore__done)
                return this;
            else
            {
                var res = ExploreAlgo(false);
                res.__Explore__done = true;
                res.__ExploreBools__done = true;
                return res;
            }
        }

        bool __ExploreBools__done = false;
        bool __Explore__done = false;
        /// <summary>
        /// Computes an equivalent STb by exploring all the Boolean and fixed-valued register components, 
        /// preserves all nonBoolean and non-fixed-valued register components as symbolic.
        /// Is equivalent to Explore() when all register components are Boolean or fixed-valued.
        /// </summary>
        public STb<FUNC, TERM, SORT> ExploreBools()
        {
            if (__ExploreBools__done)
                return this;
            else
            {
                var res = ExploreAlgo(true);
                res.__ExploreBools__done = true;
                return res;   
            }
        }

        /// <summary>
        /// Main exploration algo, useBP also implies that fixed-valued register components are explored.
        /// </summary>
        STb<FUNC, TERM, SORT> ExploreAlgo(bool useBP)
        {
            //extract the boolean registers and the nonboolean registers if useBP=true, 
            //otherwise perform full exploration
            TERM proj_concrete = regVar;
            TERM proj_symbolic = solver.UnitConst;
            Func<TERM, TERM, TERM> combine = ((x1,x2) => x1);
            if (useBP)
                stb.GetProjectionPair(this, out proj_concrete, out proj_symbolic, out combine);

            if (proj_concrete.Equals(solver.UnitConst))
                return this;

            //output abstraction functions
            Func<TERM, TERM> fBP = (t => solver.Simplify(solver.ApplySubstitution(proj_concrete, regVar, t)));
            Func<TERM, TERM> fNBP = (t => solver.Simplify(solver.ApplySubstitution(proj_symbolic, regVar, t)));

            var initBools = fBP(initReg);
            var newInitReg = fNBP(initReg);

            SORT newRegSort = Solver.GetSort(newInitReg);
            SORT bSort = Solver.GetSort(initBools);  
            TERM b = solver.MkVar(2, bSort);

            TERM raiseVar = solver.MkVar(3, solver.IntSort);
            TERM resultVar = solver.MkVar(4, regSort);

            TERM newReg = stb.MkRegister(newRegSort);
            string newName = (useBP ? string.Format("{0}_B", name) : string.Format("{0}_F", name));

            var stack = new Stack<Tuple<int, TERM>>();
            var states = new Dictionary<Tuple<int, TERM>, int>();

            var initPair = new Tuple<int, TERM>(0, initBools);
            states[initPair] = 0;
            int __stateCntr__ = 1;
            stack.Push(initPair);

            Func<int, TERM, int> stateMap = ((s,t) =>
                {
                    var p = new Tuple<int,TERM>(s,t);
                    int state;
                    if (!states.TryGetValue(p, out state))
                    {
                        state = __stateCntr__;
                        __stateCntr__ += 1;
                        states[p] = state;
                        stack.Push(p);
                    }
                    return state;
                });

            Func<int, TERM, int> stateMap0 = ((s, t) =>
            {
                return s;
            });

            var newRuleMap = new Dictionary<int, BranchingRule<TERM>>();
            var newSTb = new STb<FUNC, TERM, SORT>(solver, newName, inputSort, outputSort, newRegSort, newInitReg, 0);

            while (stack.Count > 0)
            {
                var pair = stack.Pop();
                var sourceState = states[pair];

                //make an instance of the input register state wrt pair.Second that is the reached abstract value 
                //var rIn = (useBP ? stb.MkBPInstance(regVar, pair.Second, newReg) : pair.Second);
                var rIn = combine(pair.Item2, newReg);

                if (ruleMap.ContainsKey(pair.Item1))
                {
                    var rule = ruleMap[pair.Item1];
                    //create an instance of the rule for the given input register
                    BranchingRule<TERM> ruleInst = rule.Subst(solver, regVar, rIn);

                    //concretize the rule with respect to all possible abstracted outputs
                    BranchingRule<TERM> newRule = ruleInst.Concretize(solver, fBP, fNBP, stateMap, newReg, inputVar);

                    newSTb.AssignRule(sourceState, newRule);
                }

                if (finalMap.ContainsKey(pair.Item1))
                {
                    var rule = finalMap[pair.Item1];
                    //create an instance of the rule for the given input register
                    BranchingRule<TERM> ruleInst = rule.Subst(solver, regVar, rIn);

                    //concretize the rule with respect to all possible abstracted outputs
                    //note that, here the inputVar must not occur in the rules, this is not actually checked
                    BranchingRule<TERM> newRule = ruleInst.Concretize(solver, r => initBools, r => newInitReg, (s, r) => s, newReg, inputVar);

                    newSTb.AssignFinalRule(sourceState, newRule/*.ReplaceAllStatesAndRegisters(pair.First, newInitReg)*/);
                }
            }

            return newSTb;
        }

        #endregion

        #region Bek conversion
        /// <summary>
        /// Converts the STb to a textual representation as a Bek program.
        /// Writes the text to the given text writer tw.
        /// </summary>
        public void ToBek(TextWriter tw)
        {
            ToBek1(tw.WriteLine);
        }

        /// <summary>
        /// Converts the STb to a textual representation as a Bek program.
        /// Saves the resulting bek program in the given file.
        /// </summary>
        public void ToBek(string file)
        {
            StreamWriter sr = new StreamWriter(file);
            ToBek1(sr.WriteLine);
            sr.Close();
        }

        /// <summary>
        /// Converts the STb to a textual representation as a Bek program.
        /// Writes the text to the given string builder sb.
        /// </summary>
        public void ToBek(StringBuilder sb)
        {
            ToBek1(((string s) => {sb.AppendLine(s); return;}));
        }

        void ToBek1(Action<string> WriteLine)
        {
            List<KeyValuePair<TERM,TERM>> regproj;
            var regSkel = stb.MkTupleVarSkeleton(regVar, out regproj);
            
            Dictionary<TERM,string> regNames = new Dictionary<TERM,string>();
            for (int i=0; i< regproj.Count; i++)
                regNames[regproj[i].Key] = "r"+i;

            Func<TERM,string> varNameFunc= (t => 
                {
                    if (t.Equals(inputVar))
                        return "c";
                    else if (regNames.ContainsKey(t))
                        return regNames[t];
                    else 
                        return t.ToString();
                });

            //given term t first replace the register variable with the skeleton
            //then simplify, that gets rid of tuple operations and also put the term in NNF
            //then display the resulting term using the register names for the skeleton variables r0..rk
            //this will produce a string that is a valid bek-expression
            Func<TERM, string> DescribeCond = (t =>
                {
                    var t1 = solver.ApplySubstitution(t, regVar, regSkel);
                    var t2 = solver.Simplify(t1);
                    var t3 = solver.ToNNF(t2);
                    string s = solver.PrettyPrint(t3, varNameFunc);
                    return s;
                });

            Func<TERM, string> DescribeReg = (r => 
                {
                    string s = "";
                    for (int i=0; i< regproj.Count; i++)
                    {
                        var r1 = solver.ApplySubstitution(r, regVar, regSkel);
                        var _r = regNames[regproj[i].Key];
                        var t1 = solver.ApplySubstitution(regproj[i].Value, regVar, r1);
                        var t2 = solver.Simplify(t1);
                        var _v = solver.PrettyPrint(t2, varNameFunc);
                        if (!_r.Equals(_v)) //otherwise trivial update, omit it
                            s += string.Format("{0} := {1};", _r, _v);
                    }
                    return s;
                });

            Func<Sequence<TERM>, string> DescribeYields = (ys =>
                {
                    string s = "yield(";
                    for (int i=0; i< ys.Length; i++)
                    {
                        if (i > 0)
                            s += ", ";
                        s += solver.PrettyPrint(solver.Simplify(solver.ApplySubstitution(ys[i], regVar, regSkel)), varNameFunc);
                    }
                    s += ");";
                    return s;
                });


            StringBuilder initValues = new StringBuilder();
            for (int i = 0; i < regproj.Count; i++)
            {
                TERM initval = solver.Simplify(solver.ApplySubstitution(regproj[i].Value, regVar, initReg));
                string initvalStr = solver.PrettyPrint(initval);
                initValues.AppendFormat("r{0} := {1};", i, initvalStr);
            }
            if (stateList.Count > 1)
                initValues.AppendFormat("state := {0};", stateList[0]);

            WriteLine("program "+ name + "(input){");
            WriteLine("return iter(c in input)[" + initValues +"]{");

            if (stateList.Count > 1)
            {
                #region more than one state
                for (int i = 0; i < stateList.Count; i++)
                {
                    var state = stateList[i];
                    if (ruleMap.ContainsKey(state))
                    {
                        WriteLine(string.Format("case (state == {0}):", state));
                        ruleMap[state].ToBek(true, STbInfo.TAB, WriteLine, DescribeCond, DescribeReg, DescribeYields);
                    }
                }
                for (int i = 0; i < stateList.Count; i++)
                {
                    var state = stateList[i];
                    if (finalMap.ContainsKey(state))
                    {
                        WriteLine(string.Format("end case (state == {0}):", state));
                        finalMap[state].ToBek(true, STbInfo.TAB, WriteLine, DescribeCond, DescribeReg, DescribeYields);
                    }
                }
                WriteLine("};}");
                #endregion
            }
            else
            {
                #region only one state, ignore the state
                var state = stateList[0];
                if (ruleMap.ContainsKey(state))
                {
                    WriteLine("case (true):");
                    ruleMap[state].ToBek(false, STbInfo.TAB, WriteLine, DescribeCond, DescribeReg, DescribeYields);
                }
                if (finalMap.ContainsKey(state))
                {
                    WriteLine("end case (true):");
                    finalMap[state].ToBek(false, STbInfo.TAB, WriteLine, DescribeCond, DescribeReg, DescribeYields);
                }
                WriteLine("};}");
                #endregion
            }
        }

        #endregion

        #region Code generation


        internal class CSoptions
        {
            internal string namespacename;
            internal string classname;
            internal bool generateSFT;
            internal bool useArray;
            internal bool returnNullOnReject;
            internal CSoptions(string namespacename, string classname, bool generateSFT, bool useArray, bool returnNullOnReject)
            {
                this.namespacename = namespacename;
                this.classname = classname;
                this.generateSFT = generateSFT;
                this.useArray = useArray;
                this.returnNullOnReject = returnNullOnReject;
            }
        }


        /// <summary>
        /// Generates code for the transducer as a static CSharp method &lt;namespacename&gt;.&lt;classname&gt;.Apply.
        /// </summary>
        /// <param name="file">generated code is written to this file</param>
        /// <param name="namespacename">if null or empty then "tmp" is used</param>
        /// <param name="classname">if null or empty then the name of the transducer is used</param>
        /// <param name="generateSFT">if true and registers are not used then generate methods for finite state streaming of input to output</param>
        /// <param name="useArray">if true then generates code using Array instead of StringBuilder and generateSFT option is then ignored</param>
        /// <param name="returnNullOnReject">if true then if the input is rejected then null is returned instead of exception being thrown</param>
        public void ToCS(string file, string namespacename = null, string classname = null, bool generateSFT = false, bool useArray = false, bool returnNullOnReject = false)
        {
            StreamWriter sr = new StreamWriter(file);
            ToCS(sr.WriteLine, namespacename, classname, generateSFT, useArray, returnNullOnReject);
            sr.Close();
        }

        /// <summary>
        /// Generates code for the transducer as a static CSharp method &lt;namespacename&gt;.&lt;classname&gt;.Apply.
        /// </summary>
        /// <param name="WriteLine">generated code is written by invoking this action</param>
        /// <param name="namespacename">if null or empty then "tmp" is used</param>
        /// <param name="classname">if null or empty then the name of the transducer is used</param>
        /// <param name="generateSFT">if true and registers are not used then generate methods for finite state streaming of input to output</param>
        /// <param name="useArray">if true then generates code using Array instead of StringBuilder and generateSFT option is then ignored</param>
        /// <param name="returnNullOnReject">if true then if the input is rejected then null is returned instead of exception being thrown</param>
        public void ToCS(Action<string> WriteLine, string namespacename = null, string classname = null, bool generateSFT = false, bool useArray = false, bool returnNullOnReject = false)
        {
            var nn = (string.IsNullOrEmpty(namespacename) ? "tmp" : namespacename);
            var cn = (string.IsNullOrEmpty(classname) ? Name : classname);
            ToCS(WriteLine, new CSoptions(nn, cn, solver.UnitSort.Equals(this.regSort) && generateSFT, useArray, returnNullOnReject));
        }

        void ToCS(Action<string> WriteLine, CSoptions options)
        {
            List<KeyValuePair<TERM, TERM>> regproj;
            var regSkel = stb.MkTupleVarSkeleton(regVar, out regproj);

            Dictionary<TERM, string> regNames = new Dictionary<TERM, string>();
            for (int i = 0; i < regproj.Count; i++)
                regNames[regproj[i].Key] = "r" + i;

            #region Helper functions

            Func<TERM, string> varNameFunc = (t =>
            {
                if (t.Equals(inputVar))
                    return "c";
                else if (regNames.ContainsKey(t))
                    return regNames[t];
                else
                    return t.ToString();
            });

            //given term t first replace the register variable with the skeleton
            //then simplify, that gets rid of tuple operations and also put the term in NNF
            //then display the resulting term using the register names for the skeleton variables r0..rk
            //this will produce a string that is a valid C#-expression
            Func<TERM, string> DescribeCond = (t =>
            {
                var t1 = solver.ApplySubstitution(t, regVar, regSkel);
                var t2 = solver.Simplify(t1);
                var t3 = solver.ToNNF(t2);
                string s = solver.PrettyPrintCS(t3, varNameFunc);
                return s;
            });

            #region describing register updates

            Func<TERM, string> DescribeReg = (r =>
            {
                string s = "";
                for (int i = 0; i < regproj.Count; i++)
                {
                    var r1 = solver.ApplySubstitution(r, regVar, regSkel);
                    var _r = regNames[regproj[i].Key];
                    var t1 = solver.ApplySubstitution(regproj[i].Value, regVar, r1);
                    var t2 = solver.Simplify(t1);
                    var _v = solver.PrettyPrintCS(t2, varNameFunc);
                    if (!_r.Equals(_v)) //otherwise trivial update, omit it
                        s += string.Format("{0} = {1};", _r, _v);
                }
                return s;
            });

            Func<TERM, string> SkipReg = (r =>
            {
                return "";
            });

            #endregion

            #region describing yields

            Func<Sequence<TERM>, string> DescribeYields = (ys =>
            {
                if (options.useArray)
                {
                    string s = "";
                    for (int i = 0; i < ys.Length; i++)
                    {
                        string expr = solver.PrettyPrintCS(solver.Simplify(solver.ApplySubstitution(ys[i], regVar, regSkel)), varNameFunc);
                        s += string.Format("output[pos++] = ((char){0});", expr);
                    }
                    return s;
                }
                else
                {

                    if (ys.Length == 0)
                        return "";
                    if (ys.Length == 1)
                        return string.Format("output.Append((char){0});", solver.PrettyPrintCS(solver.Simplify(solver.ApplySubstitution(ys[0], regVar, regSkel)), varNameFunc));
                    string s = "new char[]{";
                    for (int i = 0; i < ys.Length; i++)
                    {
                        s += "(char)" + solver.PrettyPrintCS(solver.Simplify(solver.ApplySubstitution(ys[i], regVar, regSkel)), varNameFunc);
                        if (i < ys.Length - 1) s += ", ";
                    }
                    s += "}";
                    return string.Format("output.Append({0});", s);
                }
            });

            Func<Sequence<TERM>, string> DescribeYieldsWithYieldReturns = (ys =>
            {
                if (ys.Length == 0)
                    return "yield break;";
                string s = "";
                for (int i = 0; i < ys.Length; i++)
                {
                    s += string.Format("yield return ((char){0}); ", solver.PrettyPrintCS(solver.Simplify(solver.ApplySubstitution(ys[i], regVar, regSkel)), varNameFunc));
                }
                return s;
            });

            Func<Sequence<TERM>, string> DescribeYieldsWithString = (ys =>
            {
                if (ys.Length == 0)
                    return "return \"\";";
                string s = "return new System.String(new char[] {";
                for (int i = 0; i < ys.Length; i++)
                {
                    s += string.Format("((char){0}),", solver.PrettyPrintCS(solver.Simplify(solver.ApplySubstitution(ys[i], regVar, regSkel)), varNameFunc));
                }
                s += "});";
                return s;
            });

            Func<Sequence<TERM>, string> SkipYields = (ys =>
            {
                return "";
            });

            #endregion

            Func<string, string> DescribeError;
            if (options.returnNullOnReject)
                DescribeError = (e => "return null;");
            else
                DescribeError = (e => "throw new System.Exception(\"" + options.classname + "\");");

            #endregion

            StringBuilder initValues = new StringBuilder();

            for (int i = 0; i < regproj.Count; i++)
            {
                TERM initval = solver.Simplify(solver.ApplySubstitution(regproj[i].Value, regVar, initReg));
                string initvalStr = solver.PrettyPrint(initval);
                string cstype = (solver.GetSort(initval).Equals(solver.BoolSort) ? "bool" : "int");
                initValues.AppendFormat("{2} r{0} = {1};", i, initvalStr, cstype);
            }

            bool multiState = (stateList.Count > 1);

            if (multiState)
                initValues.AppendFormat("int state = {0};", stateList[0]);

            int worstCaseOutputCharsPerInputChar = GetMaxIterYieldLength();
            int upperBound = 16 * 1024;
            string charsToAllocate =
                string.Format("(input.Length >= {0} ? input.Length : (int)(System.Math.Min({0}, (long)input.Length * {1})))", upperBound, worstCaseOutputCharsPerInputChar);

            //int n = GetMaxFinalYieldLength();

            WriteLine(""); 
            WriteLine("namespace " + options.namespacename + " {");
            WriteLine("public class " + options.classname);
            WriteLine("{");
            // generate local static functions for user defined helper functions
            StringBuilder sb_lib = new StringBuilder();
            solver.Library.GenerateCode("C#", sb_lib);
            WriteLine(sb_lib.ToString());

            if (!options.generateSFT)
            {
                if (this.ST != null && this.ST.YieldsAreEmpty && this.ST.IsRegisterFree)
                {
                    #region Apply method
                    WriteLine("public static string Apply(string input){");
                    WriteLine("var chars = input.ToCharArray();"); //the conversion to char array is very important, boosts performance by >50%
                    WriteLine(initValues.ToString());
                    WriteLine("for (int i = 0; i < chars.Length; i++){");
                    WriteLine("int c = (int)chars[i];");
                    WriteSwitchStatements(WriteLine, DescribeCond, DescribeReg, DescribeYields, DescribeError);
                    WriteLine("return \"\";");
                    WriteLine("}"); //end of program
                    #endregion
                }
                else
                {
                    #region Apply method
                    WriteLine("public static string Apply(string input){");
                    if (options.useArray)
                    {
                        WriteLine(string.Format("var output = new char[(input.Length * {0}) + {1}];", GetMaxIterYieldLength(), GetMaxFinalYieldLength()));
                        WriteLine("int pos = 0;");
                    }
                    else
                        WriteLine(string.Format("var output = new System.Text.StringBuilder();"/*, charsToAllocate)*/));
                    WriteLine(initValues.ToString());

                    WriteLine("var chars = input.ToCharArray();"); //the conversion to char array is very important, boosts performance by >50%
                    WriteLine("for (int i = 0; i < chars.Length; i++){");
                    WriteLine("int c = (int)chars[i];");
                    WriteSwitchStatements(WriteLine, DescribeCond, DescribeReg, DescribeYields, DescribeError);
                    if (options.useArray)
                        WriteLine("return new string(output,0,pos);");
                    else
                        WriteLine("return output.ToString();");
                    WriteLine("}"); //end of program
                    #endregion
                }
            }
            else
            {
                int errorState = -1;

                #region Apply

                WriteLine("");
                WriteLine("public static System.Collections.Generic.IEnumerable<char> Apply(System.Collections.Generic.IEnumerable<char> input){");
                WriteLine("int state = q0;");
                WriteLine("foreach (char c in input)");
                WriteLine("{");
                WriteLine("  foreach (char d in Psi(state, (int)c))");
                WriteLine("    yield return d;");
                WriteLine("  state = Delta(state, (int)c);");
                WriteLine("}");
                WriteLine("if (F.Contains(state))");
                WriteLine("{");
                WriteLine("  foreach (char d in Psi(state, -1))");
                WriteLine("    yield return d;");
                WriteLine("}");
                WriteLine("else");
                WriteLine("  throw new System.Exception(\"" + options.classname + "\");");
                WriteLine("}");
                WriteLine("");

                #endregion

                #region IDeterministicFiniteStateTransducer interface implementation generation

                string states = "";
                string fstates = "";
                for (int i = 0; i < stateList.Count; i++)
                {
                    states += stateList[i] + ", ";
                    if (finalMap.ContainsKey(stateList[i]))
                        fstates += stateList[i] + ", ";
                }
                WriteLine("");
                WriteLine("public static System.Collections.Generic.ICollection<int> Q { get { return new int[]{ " + states + "-1 }; } }");
                WriteLine("");
                WriteLine("public static int q0 { get { return " + initState + " ; } }");
                WriteLine("");
                WriteLine("public static System.Collections.Generic.ICollection<int> F { get { return new int[]{ " + fstates + " }; } }");
                //WriteLine("");
                //WriteLine("public System.Collections.Generic.IEnumerable<char> Sigma { get { throw new NotImplementedException(); } }");
                WriteLine("");
                WriteLine("public static int Delta(int state, int c){");
                WriteLine("if (c < 0) return state;");
                WriteLine("switch (state){");
                for (int i = 0; i < stateList.Count; i++)
                {
                    var state = stateList[i];
                    if (ruleMap.ContainsKey(state))
                    {
                        var rule = ruleMap[state];

                        WriteLine(string.Format("case ({0}): ", state) + "{");
                        rule.WriteCode(true, WriteLine, DescribeCond, SkipReg, SkipYields, "state = " + errorState + ";");
                        WriteLine("break;}");
                    }
                }
                WriteLine("default: {");
                WriteLine("state = " + errorState + ";");
                WriteLine("break;");
                WriteLine("}");
                WriteLine("}"); //end of switch
                WriteLine("return state;");
                WriteLine("}"); //end of method
                WriteLine("");

                #region Psi
                WriteLine("public static System.Collections.Generic.IEnumerable<char> Psi(int state, int c){");
                WriteLine("if (c >= 0) {");
                WriteLine("switch (state){");
                for (int i = 0; i < stateList.Count; i++)
                {
                    var state = stateList[i];
                    if (ruleMap.ContainsKey(state))
                    {
                        var rule = ruleMap[state];

                        WriteLine(string.Format("case ({0}): ", state) + "{");
                        rule.WriteCode(false, WriteLine, DescribeCond, SkipReg, DescribeYieldsWithYieldReturns, "yield break;");
                        WriteLine("break;}");
                    }
                }
                WriteLine("default: {");
                WriteLine("yield break;");
                WriteLine("}");
                WriteLine("}"); //end of switch
                WriteLine("}"); //end of then
                WriteLine("else {");
                WriteLine("switch (state){");
                for (int i = 0; i < stateList.Count; i++)
                {
                    var state = stateList[i];
                    if (finalMap.ContainsKey(state))
                    {
                        var rule = finalMap[state];

                        WriteLine(string.Format("case ({0}): ", state) + "{");
                        rule.WriteCode(false, WriteLine, DescribeCond, SkipReg, DescribeYieldsWithYieldReturns, "yield break;");
                        WriteLine("break;}");
                    }
                }
                WriteLine("default: {");
                WriteLine("yield break;");
                WriteLine("}");
                WriteLine("}"); //end of switch
                WriteLine("}"); //end of else
                WriteLine("}"); //end of method
                #endregion

                WriteLine("");

                /*
                #region Psi2
                WriteLine("public string Psi2(int state, int c){");
                WriteLine("if (c >= 0) {");
                WriteLine("switch (state){");
                for (int i = 0; i < stateList.Count; i++)
                {
                    var state = stateList[i];
                    if (ruleMap.ContainsKey(state))
                    {
                        var rule = ruleMap[state];

                        WriteLine(string.Format("case ({0}): ", state) + "{");
                        rule.ToCS(false, STbInfo.TAB, WriteLine, DescribeCond, SkipReg, DescribeYieldsWithString, "return \"\";");
                        WriteLine("}");
                    }
                }
                WriteLine("default: {");
                WriteLine("return \"\";");
                WriteLine("}");
                WriteLine("}"); //end of switch
                WriteLine("}"); //end of then
                WriteLine("else {");
                WriteLine("switch (state){");
                for (int i = 0; i < stateList.Count; i++)
                {
                    var state = stateList[i];
                    if (finalMap.ContainsKey(state))
                    {
                        var rule = finalMap[state];

                        WriteLine(string.Format("case ({0}): ", state) + "{");
                        rule.ToCS(false, STbInfo.TAB, WriteLine, DescribeCond, SkipReg, DescribeYieldsWithString, "return \"\";");
                        WriteLine("}");
                    }
                }
                WriteLine("default: {");
                WriteLine("return \"\";");
                WriteLine("}");
                WriteLine("}"); //end of switch
                WriteLine("}"); //end of else
                WriteLine("}"); //end of method
                #endregion
                */

                #endregion
            }

            WriteLine("}"); //end of class 
            WriteLine("}"); //end of namespace
        }

        /// <summary>
        /// Generate C code.
        /// </summary>
        /// <param name="file">target file of the generated code</param>
        public void ToC(string file)
        {
            var sr = new StreamWriter(file);
            ToC(sr.WriteLine);
            sr.Close();
        }

        /// <summary>
        /// Generate C code.
        /// </summary>
        public void ToC(Action<string> WriteLine)
        {
            List<KeyValuePair<TERM, TERM>> regproj;
            var regSkel = stb.MkTupleVarSkeleton(regVar, out regproj);

            Dictionary<TERM, string> regNames = new Dictionary<TERM, string>();
            for (int i = 0; i < regproj.Count; i++)
                regNames[regproj[i].Key] = "r" + i;

            #region Helper functions
            Func<TERM, string> varNameFunc = (t =>
            {
                if (t.Equals(inputVar))
                    return "c";
                else if (regNames.ContainsKey(t))
                    return regNames[t];
                else
                    return t.ToString();
            });

            //given term t first replace the register variable with the skeleton
            //then simplify, that gets rid of tuple operations and also put the term in NNF
            //then display the resulting term using the register names for the skeleton variables r0..rk
            //this will produce a string that is a valid C#-expression
            Func<TERM, string> DescribeCond = (t =>
            {
                var t1 = solver.ApplySubstitution(t, regVar, regSkel);
                var t2 = solver.Simplify(t1);
                var t3 = solver.ToNNF(t2);
                string s = solver.PrettyPrintCS(t3, varNameFunc);
                return s;
            });

            Func<TERM, string> DescribeReg = (r =>
            {
                string s = "";
                for (int i = 0; i < regproj.Count; i++)
                {
                    var r1 = solver.ApplySubstitution(r, regVar, regSkel);
                    var _r = regNames[regproj[i].Key];
                    var t1 = solver.ApplySubstitution(regproj[i].Value, regVar, r1);
                    var t2 = solver.Simplify(t1);
                    var _v = solver.PrettyPrintCS(t2, varNameFunc);
                    if (!_r.Equals(_v)) //otherwise trivial update, omit it
                        s += string.Format("{0} = {1};", _r, _v);
                }
                return s;
            });

            Func<Sequence<TERM>, string> DescribeYields = (ys =>
            {
                string s = "";
                for (int i = 0; i < ys.Length; i++)
                {
                    string expr = solver.PrettyPrintCS(solver.Simplify(solver.ApplySubstitution(ys[i], regVar, regSkel)), varNameFunc);
                    s += string.Format("output[pos++] = ((char){0});", expr);
                }
                return s;
            });

            Func<string, string> DescribeError = (e => "throw (\"" + name + "\");");

            #endregion

            StringBuilder initValues = new StringBuilder();
            for (int i = 0; i < regproj.Count; i++)
            {
                TERM initval = solver.Simplify(solver.ApplySubstitution(regproj[i].Value, regVar, initReg));
                string initvalStr = solver.PrettyPrint(initval);
                string cstype = (solver.GetSort(initval).Equals(solver.BoolSort) ? "bool" : "int");
                initValues.AppendFormat("{2} r{0} = {1};", i, initvalStr, cstype);
            }


            if (stateList.Count > 1)
                initValues.AppendFormat("int state = {0};", stateList[0]);

            int worstCaseOutputCharsPerInputChar = GetMaxIterYieldLength();

            //int n = GetMaxFinalYieldLength();

            WriteLine("");
            WriteLine("#include <iostream>");
            WriteLine("using namespace std;");

            //user defined functions
            StringBuilder lib_sb = new StringBuilder();
            solver.Library.GenerateCode("C", lib_sb);
            WriteLine(lib_sb.ToString());

            //the C function
            WriteLine(string.Format("char* {0}(const char* input, int len)", name) + "{");
            WriteLine(string.Format("char* output = new char[(len * {0}) + {1}];", GetMaxIterYieldLength(), GetMaxFinalYieldLength()));
            WriteLine(initValues.ToString());
            WriteLine("int pos = 0;");
            WriteLine("for (int i = 0; i < len; i++){");
            WriteLine("int c = (int)input[i];");
            WriteSwitchStatements(WriteLine, DescribeCond, DescribeReg, DescribeYields, DescribeError);
            WriteLine("return output;");
            WriteLine("}"); //end of program
        }

        private void WriteSwitchStatements(Action<string> WriteLine, Func<TERM, string> DescribeCond, 
            Func<TERM, string> DescribeReg, Func<Sequence<TERM>, string> DescribeYields, Func<string, string> DescribeError)
        {
            if (stateList.Count > 1)
            {
                WriteLine("switch (state){");
                for (int i = 0; i < stateList.Count; i++)
                {
                    var state = stateList[i];
                    var rule = GetRuleFrom(state);
                    if (i < stateList.Count - 1)
                        WriteLine(string.Format("case ({0}): ", state) + "{");
                    else
                        WriteLine("default: {");
                    rule.WriteCode(true, WriteLine, DescribeCond, DescribeReg, DescribeYields, DescribeError(name));
                    WriteLine("break;}");
                }
                WriteLine("}"); //end switch 
                WriteLine("}"); //end for
                WriteLine("switch (state){"); //final output switch
                var finalYields = new Dictionary<int, string>();
                var finalYieldsInv = new Dictionary<string, HashSet<int>>();
                var actualFinalCases = new List<int>();
                for (int i = 0; i < stateList.Count; i++)
                {
                    var state = stateList[i];
                    var frule = GetFinalRuleFrom(state);
                    var sb = new StringBuilder();
                    frule.WriteCode(false, s => { sb.AppendLine(s); return; }, 
                        DescribeCond, r => "", DescribeYields, DescribeError(name));
                    var yieldStr = sb.ToString();
                    if (!finalYieldsInv.ContainsKey(yieldStr))
                    {
                        actualFinalCases.Add(state);
                        finalYields[state] = yieldStr;
                        finalYieldsInv[yieldStr] = new HashSet<int>();
                    }
                    finalYieldsInv[yieldStr].Add(state);
                }

                for (int i = 0; i < actualFinalCases.Count; i++)
                {
                    var state = actualFinalCases[i];
                    var finalYield = finalYields[state];
                    if (i < actualFinalCases.Count - 1)
                    {
                        //WriteLine(string.Format("case ({0}): ", state));
                        foreach (var st in finalYieldsInv[finalYield])
                            WriteLine(string.Format("case ({0}): ", st));
                        WriteLine("{");
                    }
                    else
                        WriteLine("default: {");
                    WriteLine(finalYield + " break;}");
                }
                WriteLine("}"); //end of final output switch
            }
            else
            {
                var state = stateList[0];
                var rule = GetRuleFrom(state);
                rule.WriteCode(false, WriteLine, DescribeCond, DescribeReg, DescribeYields, DescribeError(name));
                WriteLine("}"); //end for
                var frule = GetFinalRuleFrom(state);
                frule.WriteCode(false, WriteLine, DescribeCond, r => "", DescribeYields, DescribeError(name));
            }
        }


        /// <summary>
        /// Generate JavaScript.
        /// </summary>
        /// <param name="file">file where the generated code is written</param>
        /// <param name="addHtmlPageWrapper">if true create a simple html wrapper around the code so it can be executed in a browser</param>
        /// <param name="methodname">name for the generated method</param>
        public void ToJS(string file, bool addHtmlPageWrapper = false, string methodname = "bek")
        {
            StreamWriter sr = new StreamWriter(file);
            ToJS(sr.WriteLine, addHtmlPageWrapper, methodname);
            sr.Close();
        }

        /// <summary>
        /// Generate JavaScript.
        /// </summary>
        /// <param name="WriteLine">assumed to be the output fuction</param>
        /// <param name="addHtmlPageWrapper">if true create a simple html wrapper around the code so it can be executed in a browser</param>
        /// <param name="methodname">name for the generated method</param>
        public void ToJS(Action<string> WriteLine, bool addHtmlPageWrapper = false, string methodname = "bek")
        {
            if (addHtmlPageWrapper)
            {
                #region start html page

                WriteLine("<html>\n<head>");
                WriteLine("<SCRIPT language = \"javascript\">");

                #endregion
            }

            List<KeyValuePair<TERM, TERM>> regproj;
            var regSkel = stb.MkTupleVarSkeleton(regVar, out regproj);

            Dictionary<TERM, string> regNames = new Dictionary<TERM, string>();
            for (int i = 0; i < regproj.Count; i++)
                regNames[regproj[i].Key] = "r" + i;

            #region Helper functions

            Func<TERM, string> varNameFunc = (t =>
            {
                if (t.Equals(inputVar))
                    return "c";
                else if (regNames.ContainsKey(t))
                    return regNames[t];
                else
                    return t.ToString();
            });

            //given term t first replace the register variable with the skeleton
            //then simplify, that gets rid of tuple operations and also put the term in NNF
            //then display the resulting term using the register names for the skeleton variables r0..rk
            //this will produce a string that is a valid C#-expression
            Func<TERM, string> DescribeCond = (t =>
            {
                var t1 = solver.ApplySubstitution(t, regVar, regSkel);
                var t2 = solver.Simplify(t1);
                var t3 = solver.ToNNF(t2);
                string s = solver.PrettyPrintCS(t3, varNameFunc);
                return s;
            });

            #region describing register updates

            Func<TERM, string> DescribeReg = (r =>
            {
                string s = "";
                for (int i = 0; i < regproj.Count; i++)
                {
                    var r1 = solver.ApplySubstitution(r, regVar, regSkel);
                    var _r = regNames[regproj[i].Key];
                    var t1 = solver.ApplySubstitution(regproj[i].Value, regVar, r1);
                    var t2 = solver.Simplify(t1);
                    var _v = solver.PrettyPrintCS(t2, varNameFunc);
                    if (!_r.Equals(_v)) //otherwise trivial update, omit it
                        s += string.Format("{0} = {1};", _r, _v);
                }
                return s;
            });

            Func<TERM, string> SkipReg = (r =>
            {
                return "";
            });

            #endregion

            #region describing yields
            Func<Sequence<TERM>, string> DescribeYields = (ys =>
            {
                if (ys.Length == 0)
                    return "";
                else
                {
                    string s = "result.push(String.fromCharCode(";
                    for (int i = 0; i < ys.Length; i++)
                    {
                        s += solver.PrettyPrintCS(solver.Simplify(solver.ApplySubstitution(ys[i], regVar, regSkel)), varNameFunc);
                        if (i < ys.Length - 1) s += ",";
                    }
                    s += "));";
                    return s;
                }
            });
            #endregion

            Func<string, string> DescribeError = (e => "throw {name:'" + name + "'};");

            #endregion

            StringBuilder initValues = new StringBuilder();

            for (int i = 0; i < regproj.Count; i++)
            {
                TERM initval = solver.Simplify(solver.ApplySubstitution(regproj[i].Value, regVar, initReg));
                string initvalStr = solver.PrettyPrint(initval);
                initValues.AppendFormat("var r{0} = {1};", i, initvalStr);
            }

            bool multiState = (stateList.Count > 1);

            if (multiState)
                initValues.AppendFormat("var state = {0};", stateList[0]);
 
            WriteLine("function " + methodname + "(input){");
            // generate local static functions for user defined helper functions
            StringBuilder sb_lib = new StringBuilder();
            solver.Library.GenerateCode("JS", sb_lib);
            WriteLine(sb_lib.ToString());
            WriteLine("var result = new Array();");
            WriteLine(initValues.ToString());
            WriteLine("for (var i = 0; i < input.length; i++){");
            WriteLine("var c = input.charCodeAt(i);");
            WriteSwitchStatements(WriteLine, DescribeCond, DescribeReg, DescribeYields, DescribeError);
            WriteLine("return result.join('');");
            WriteLine("}"); //end of function

            if (addHtmlPageWrapper)
            {
                #region end html page

                WriteLine(@"
                function run" + methodname + @"() { 
                        try {
                            A = document.frmOne.txtInputString.value;
                            B = " + methodname + @"(unescape(A));
                            document.frmOne.txtOutputString.value = escape(B);
                            document.frmOne.txtOutputLiteralString.value = B;
                            document.frmOne.txtError.value = '';
                        }
                        catch (e) {
                            document.frmOne.txtOutputString.value = '';
                            document.frmOne.txtOutputLiteralString.value = '';
                            document.frmOne.txtError.value = e.name; 
                        }
                }");
                WriteLine("</SCRIPT>");
                WriteLine(@"
                </head>
                <body>
                <h1>Test the generated script</h1>
                Input is assumed to be an <i>escaped</i> JavaScript string. 
                <br/>
                <i>Example</i>: <tt>O%u0152E</tt> and <tt>%4F%u0152%45</tt>
                both represent <tt>O&#x0152;E</tt>
                <FORM NAME = frmOne>
                input: <INPUT TYPE = Text NAME = txtInputString size = 50 value =" + "\"\"" + @">
                <p>
                <INPUT Type = Button NAME = b1 VALUE = " + "\"run\"" + @" onClick = run" + methodname + @"()>
                </p>
                <p>
                output (escaped): <INPUT TYPE = Text NAME = txtOutputString size = 50 value = " + "\"\"" + @">
                <br />
                output (literal): <INPUT TYPE = Text NAME = txtOutputLiteralString size = 50 value = " + "\"\"" + @">
                <br />
                exception: <input TYPE= text   NAME = txtError size = 50>
                </p>
                </FORM>
                </body>
                </html> 
                ");

                #endregion
            }
        }
        #endregion

        #region IPrettyPrinter<TERM> Members


        public string PrettyPrintCS(TERM t, Func<TERM, string> varLookup)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<char> Psi(int state, char ch)
        {
            int c = (int)ch;
            switch (state)
            {
                case (0) :
                    if (c > 'd')
                    {
                        yield return 'b';
                    }
                    else
                    {
                        yield return 'a';
                    }
                    break;
                case (1):
                    yield break;
            }
        }

        #endregion

        /// <summary>
        /// Returns true iff pred holds for all register update terms.
        /// </summary>
        public bool IsTrueForAllRegisterUpdates(Predicate<TERM> pred)
        {
            foreach (var st in stateList)
            {
                var rule = GetRuleFrom(st);
                if (rule.IsNotUndef)
                    if (!rule.IsTrueForAllRegisterUpdates(pred))
                        return false;
            }
            return true;
        }

        /// <summary>
        /// Generate C# and compile the generated C#.
        /// </summary>
        /// <param name="namespacename">given namespace name</param>
        /// <param name="classname">given class name</param>
        /// <param name="useArray">use Array instead of StringBuilder internally</param>
        /// <returns></returns>
        public IExecutableTransducer Compile(string namespacename = null, string classname = null, bool useArray = false, bool returnNullOnReject = false)
        {
            return new CompiledCSMethod<FUNC, TERM, SORT>(this, namespacename, classname, useArray, returnNullOnReject);
        }

        public STb<FUNC, TERM, SORT> Compose(STb<FUNC, TERM, SORT> other)
        {
            var composer = new STbComposer<FUNC, TERM, SORT>(this, other);
            var res = composer.Compose();
            return res;
        }

        public STb<FUNC, TERM, SORT> Simplify(int bound = -1)
        {
            var simplifier = new STbSimplifier<FUNC, TERM, SORT>(this);
            return simplifier.Simplify(bound);
        }

        /// <summary>
        /// Eliminate all states that do not lead to some final state.
        /// </summary>
        public void EliminateDeadends()
        {
            var st = ToST(true);
            if (st.StateCount < this.StateCount)
            {

                var prunedStateList = new List<int>();
                var prunedRulemap = new Dictionary<int,BranchingRule<TERM>>();
                var prunedFinalRulemap = new Dictionary<int,BranchingRule<TERM>>();
                stateSet.Clear();
                Predicate<BaseRule<TERM>> IsNotValidState = (r) =>
                    {
                        return !st.automaton.IsState(r.State);
                    };

                foreach (int q in st.GetStates())
                {
                    prunedStateList.Add(q);
                    stateSet.Add(q);
                    var q_rule = GetRuleFrom(q).Prune(IsNotValidState);
                    if (q_rule.IsNotUndef)
                        prunedRulemap[q] = q_rule;
                    var q_frule = GetFinalRuleFrom(q);
                    if (q_frule.IsNotUndef)
                        prunedFinalRulemap[q] = q_frule;
                }
                stateList = prunedStateList;
                ruleMap = prunedRulemap;
                finalMap = prunedFinalRulemap;
            }
        }
    }

    /// <summary>
    /// Provides methods to execute a string transducer.
    /// </summary>
    public interface IExecutableTransducer
    {
        /// <summary>
        /// Applies the string transducer to the given input string.
        /// </summary>
        string Apply(string input);
        /// <summary>
        /// Gets the compiled source code.
        /// </summary>
        string Source
        {
            get;
        }
    }

    internal class CompiledCSMethod<FUNC, TERM, SORT> : IExecutableTransducer
    {
        string source;
        MethodInfo mi;
        string namespacename;
        string classname;
        bool returnNullOnReject;

        internal CompiledCSMethod(STb<FUNC, TERM, SORT> st, string namespacename, string classname, bool useArray, bool returnNullOnReject)
        {
            this.returnNullOnReject = returnNullOnReject;
            this.namespacename = namespacename;
            this.classname = classname;
            var sb = new StringBuilder();
            st.ToCS(((string s) => { sb.AppendLine(s); return; }), namespacename, classname, false, useArray, returnNullOnReject);
            var csc = new CSharpCodeProvider();
            var parameters = new CompilerParameters();
            parameters.GenerateInMemory = true;
            parameters.CompilerOptions = "/o";  //optimize the code
            parameters.ReferencedAssemblies.Add("System.dll");
            CompilerResults results = csc.CompileAssemblyFromSource(parameters, sb.ToString());
            if (results.Errors.HasErrors)
            {
                throw new AutomataException("C# compilation error: " + results.Errors[0].ErrorText);
            }
            else
            {
                var cls = results.CompiledAssembly.GetTypes()[0];
                var method = cls.GetMethod("Apply", BindingFlags.Static | BindingFlags.Public);
                mi = method;
                source = sb.ToString();
            }
        }

        public string Apply(string input)
        {
            try
            {
                string res = (string)mi.Invoke(null, new object[] { input });
                return res;
            }
            catch (TargetInvocationException e)
            {
                throw e.InnerException;
            }
        }


        public string Source
        {
            get { return source; }
        }
    }

    /// <summary></summary>
    public enum ParseType
    {
        /// <summary></summary>
        Boolean,
        /// <summary></summary>
        Int32,
        /// <summary></summary>
        String,
        /// <summary></summary>
        Skip
    }
    //internal class STbFromPattern
    //{
    //    void foo()
    //    {
    //        System.Text.RegularExpressions.Match m;
    //        m.

    //    }
    //}

}

