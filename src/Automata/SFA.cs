using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Automata;

namespace Microsoft.Automata
{
    /// <summary>
    /// Symbolic Finite Automaton (SFA) associated with a given SMT solver.
    /// Contains core algorithms for manipulating SFAs.
    /// </summary>
    /// <typeparam name="FUNC">function declarations, each function declaration has domain and range sorts</typeparam>
    /// <typeparam name="TERM">terms, each term has a fixed sort</typeparam>
    /// <typeparam name="SORT">sorts correspond to different subuniverses of elements</typeparam>
    public class SFA<FUNC, TERM, SORT> : AutomatonSerializer<TERM>, INameProvider,
        IAutomaton<TERM>, IAcceptor<FUNC, TERM, SORT>
    {
        IContext<FUNC, TERM, SORT> solver;
        string name; 
           
        SORT inpSort; 

        FUNC acc;

        #region INameProvider Members

        /// <summary>
        /// Gets or sets the name of the SFA
        /// </summary>
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

        Automaton<TERM> automaton;

        /// <summary>
        /// Underlying basic automaton
        /// </summary>
        public Automaton<TERM> Automaton
        {
            get { return automaton; }
        }

        /// <summary>
        /// Creates a new SFA.
        /// </summary>
        /// <param name="solver">solver of character constraints</param>
        /// <param name="sort">input element sort</param>
        /// <param name="name">name of the automaton</param>
        /// <param name="autom">underlying basic automaton</param>
        public SFA(IContext<FUNC, TERM, SORT> solver, SORT sort, string name, Automaton<TERM> autom)
        {
            this.Name = name;
            if (solver == null)
                throw new ArgumentNullException("solver");
            if (autom == null)
                throw new ArgumentNullException("autom");
            this.solver = solver;
            this.automaton = autom;
            this.inpSort = sort;
        }

        /// <summary>
        /// Creates the SFA(solver,sort,"SFA",autom) 
        /// </summary>
        public SFA(IContext<FUNC, TERM, SORT> solver, SORT sort, Automaton<TERM> autom)
            : this(solver, sort, "SFA", autom)
        {
        }

        /// <summary>
        /// Creates an automaton that accepts the union of L(this) and L(B)
        /// </summary>
        /// <param name="B">given automaton</param>
        /// <returns>an automaton that accepts the union of L(this) and L(B)</returns>
        public SFA<FUNC, TERM, SORT> Union(SFA<FUNC, TERM, SORT> B)
        {
            if (B == null)
                throw new ArgumentNullException("B");

            CheckCompatibiltyWith(B);

            string prodName = string.Format("[{0}_x_{1}]", name, B.Name);
            var AxB = Automaton<TERM>.MkSum(automaton, B.automaton);
            return new SFA<FUNC, TERM, SORT>(solver, inpSort, prodName, AxB);
        }

        /// <summary>
        /// Creates an automaton that accepts the intersection of L(this) and L(B)
        /// </summary>
        /// <param name="B">given automaton</param>
        /// <returns>an automaton that accepts the intersection of L(this) and L(B)</returns>
        public SFA<FUNC, TERM, SORT> IntersectWith(SFA<FUNC, TERM, SORT> B)
        {
            if (B == null)
                throw new ArgumentNullException("B");

            CheckCompatibiltyWith(B);

            string prodName = string.Format("[{0}_x_{1}]", name, B.Name);
            var AxB = Automaton<TERM>.MkProduct(automaton, B.automaton);
            return new SFA<FUNC, TERM, SORT>(solver, inpSort, prodName, AxB);
        }

        /// <summary>
        /// Creates an automaton that accepts L(this)\L(B)
        /// </summary>
        /// <param name="B">given automaton to subtract from current automaton</param>
        /// <returns>an automaton that accepts L(this)\L(B)</returns>
        public SFA<FUNC, TERM, SORT> Minus(SFA<FUNC, TERM, SORT> B)
        {
            if (B == null)
                throw new ArgumentNullException("B");

            CheckCompatibiltyWith(B);

            string diffName = string.Format("[{0}_-_{1}]", name, B.Name);
            var AmB = Automaton<TERM>.MkDifference(automaton, B.automaton, 0);
            return new SFA<FUNC, TERM, SORT>(solver, inpSort, diffName, AmB);
        }

        private void CheckCompatibiltyWith(SFA<FUNC, TERM, SORT> B)
        {
            if (solver != B.solver)
                throw new AutomataException(AutomataExceptionKind.SolversAreNotIdentical);

            if (!Object.Equals(inpSort, B.inpSort))
                throw new AutomataException(AutomataExceptionKind.InputSortsMustBeIdentical);
        }

        /// <summary>
        /// Creates an SFA that accepts L(A)\L(B)
        /// </summary>
        /// <param name="A">given automaton to subtract from</param>
        /// <param name="B">subtractor automaton</param>
        /// <returns>an automaton that accepts L(A)\L(B)</returns>
        public static SFA<FUNC, TERM, SORT> operator -(SFA<FUNC, TERM, SORT> A, SFA<FUNC,TERM, SORT> B)
        {
            if (A == null)
                throw new ArgumentNullException("A");
            var AmB = A.Minus(B);
            return AmB;
        }

        /// <summary>
        /// Returns true if the language of the current SFA is a subset of L(A)
        /// </summary>
        /// <param name="A">superset automaton</param>
        /// <returns>true if L(this) is subset of L(A)</returns>
        public bool IsSubsetOf(SFA<FUNC, TERM, SORT> A)
        {
            if (A == null)
                throw new ArgumentNullException("A");

            CheckCompatibiltyWith(A);

            List<TERM> w;
            bool res = Automaton<TERM>.CheckDifference(this.automaton, A.automaton, 0, out w);
            return !res;
        }


        /// <summary>
        /// Returns true if L(A) is a subset of L(B)
        /// </summary>
        /// <param name="A">subset automaton</param>
        /// <param name="B">superset automaton</param>
        /// <returns>true if L(A) is subset of L(A)</returns>
        public static bool operator <=(SFA<FUNC, TERM, SORT> A, SFA<FUNC, TERM, SORT> B)
        {
            if (A == null)
                throw new ArgumentNullException("A");
            return A.IsSubsetOf(B);
        }

        /// <summary>
        /// Returns true if L(B) is a subset of L(A)
        /// </summary>
        /// <param name="A">superset automaton</param>
        /// <param name="B">subset automaton</param>
        /// <returns>true if L(B) is subset of L(A)</returns>
        public static bool operator >=(SFA<FUNC, TERM, SORT> A, SFA<FUNC, TERM, SORT> B)
        {
            if (B == null)
                throw new ArgumentNullException("B");
            return B.IsSubsetOf(A);
        }

        /// <summary>
        /// Returns true if L(A) is a proper subset of L(B)
        /// </summary>
        /// <param name="A">subset automaton</param>
        /// <param name="B">superset automaton</param>
        /// <returns>true if L(A) is a proper subset of L(A)</returns>
        public static bool operator <(SFA<FUNC, TERM, SORT> A, SFA<FUNC, TERM, SORT> B)
        {
            if (A == null)
                throw new ArgumentNullException("A");
            if (B == null)
                throw new ArgumentNullException("B");
            return A.IsSubsetOf(B) && !B.IsSubsetOf(A);
        }

        /// <summary>
        /// Returns true if L(B) is a proper subset of L(A)
        /// </summary>
        /// <param name="A">superset automaton</param>
        /// <param name="B">subset automaton</param>
        /// <returns>true if L(B) is a proper subset of L(A)</returns>
        public static bool operator >(SFA<FUNC, TERM, SORT> A, SFA<FUNC, TERM, SORT> B)
        {
            if (B == null)
                throw new ArgumentNullException("B");
            if (A == null)
                throw new ArgumentNullException("A");
            return B.IsSubsetOf(A) && !A.IsSubsetOf(B);
        }

        /// <summary>
        /// Creates an automaton that accepts the intersection of L(A) and L(B)
        /// </summary>
        /// <param name="A">first argument of the intersection</param>
        /// <param name="B">second argument of the intersection</param>
        /// <returns>an automaton that accepts the intersection of L(A) and L(B)</returns>
        public static SFA<FUNC, TERM, SORT> operator *(SFA<FUNC, TERM, SORT> A, SFA<FUNC, TERM, SORT> B)
        {
            if (A == null)
                throw new ArgumentNullException("A");
            var AxB = A.IntersectWith(B);
            return AxB;
        }

        //#region Visualization in graph form

        /// <summary>
        /// Saves the automaton in <paramref name="Name"/>.dgml file in the working directory
        /// and opens the file in a new process.
        /// </summary>
        override public void ShowGraph(int nrOfElements)
        {
            if (nrOfElements <= 0)
            {
                Microsoft.Automata.DirectedGraphs.DgmlWriter.ShowGraph<TERM>(-1, this, name);
            }
            else
            {
                var aut = Concretize(nrOfElements);
                this.Solver.CharSetProvider.ShowGraph(aut, this.name);
            }
        }

        ///// <summary>
        ///// Saves the automaton in dgml format in <paramref name="Name"/>.dgml file in the working directory.
        ///// </summary>
        //public void SaveAsDgml()
        //{
        //    Microsoft.Automata.DirectedGraphs.DgmlWriter.AutomatonToDgml<TERM>(this, name);
        //}

        ///// <summary>
        ///// Saves the automaton in dot format in <paramref name="Name"/>.dot file in the working directory.
        ///// </summary>
        //public void SaveAsDot()
        //{
        //    Microsoft.Automata.DirectedGraphs.DotWriter.AutomatonToDot<TERM>(this.DescribeLabel, this, name, name, DirectedGraphs.DotWriter.RANKDIR.TB, 12);
        //}

        ///// <summary>
        ///// Saves the automaton in dgml format in tw.
        ///// </summary>
        //public void SaveAsDgml(System.IO.TextWriter tw)
        //{
        //    Microsoft.Automata.DirectedGraphs.DgmlWriter.AutomatonToDgml<TERM>(this, tw);
        //}

        ///// <summary>
        ///// Saves the automaton in dot format in tw.
        ///// </summary>
        //public void SaveAsDot(System.IO.TextWriter tw)
        //{
        //    Microsoft.Automata.DirectedGraphs.DotWriter.AutomatonToDot<TERM>(this.DescribeLabel, this, name, tw, DirectedGraphs.DotWriter.RANKDIR.TB, 12, true);
        //}
        //#endregion

        /// <summary>
        /// Returns true if the automaton accepts no strings
        /// </summary>
        public bool IsEmpty
        {
            get { return automaton.IsEmpty; }
        }

        /// <summary>
        /// Eliminate epsilon moves fom the automaton
        /// </summary>
        /// <returns>equivalent automaton without epsilon-moves</returns>
        public SFA<FUNC, TERM, SORT> EliminateEpsilons()
        {
            var a = automaton.RemoveEpsilons();
            return new SFA<FUNC, TERM, SORT>(solver, inpSort, string.Format("ElimE[{0}]", name), a);
        }

        /// <summary>
        /// Complement the automaton, the resulting SFA accepts all strings not accepted by this SFA
        /// </summary>
        public SFA<FUNC, TERM, SORT> Complement()
        {
            Automaton<TERM> full = Automaton<TERM>.MkFull(this.automaton.Algebra);
            var compl = Automaton<TERM>.MkDifference(full, automaton.MakeTotal(), 0);
            return new SFA<FUNC, TERM, SORT>(solver, inpSort, string.Format("Compl[{0}]", name), compl);
        }

        /// <summary>
        /// Creates an automaton that accepts the complement of the language of A.
        /// </summary>
        /// <param name="A">automaton to be complemented</param>
        /// <returns>complement of A</returns>
        public static SFA<FUNC, TERM, SORT> operator !(SFA<FUNC, TERM, SORT> A)
        {
            if (A == null)
                throw new ArgumentNullException("A");
            return A.Complement();
        }

        /// <summary>
        /// Creates an equivalent deterministic automaton
        /// </summary>
        public SFA<FUNC, TERM, SORT> Determinize()
        {
            if (automaton.IsDeterministic)
                return this;
            var det = automaton.Determinize();
            return new SFA<FUNC, TERM, SORT>(solver, inpSort, "Det[" + name + "]", det);
        }


        /// <summary>
        /// Creates an equivalent total automaton.
        /// </summary>
        public SFA<FUNC, TERM, SORT> MakeTotal()
        {
            var tot = automaton.MakeTotal();
            return new SFA<FUNC, TERM, SORT>(solver, inpSort, string.Format("Tot[{0}]", name), tot);
        }

        /// <summary>
        /// Creates an equivalent minimal automaton.
        /// </summary>
        public SFA<FUNC, TERM, SORT> Minimize()
        {
            var det = this.Determinize();
            var min = det.automaton.Minimize();
            return new SFA<FUNC, TERM, SORT>(solver, inpSort, "Min[" + name + "]", min);
        }

        /// <summary>
        /// Creates an equivalent minimal automaton.
        /// </summary>
        public SFA<FUNC, TERM, SORT> MinimizeMoore(int timeout)
        {
            var det = this.Determinize();
            var min = det.automaton.MinimizeClassical(timeout,false);
            return new SFA<FUNC, TERM, SORT>(solver, inpSort, "Min[" + name + "]", min);
        }

        /// <summary>
        /// Creates an equivalent minimal automaton.
        /// </summary>
        public SFA<FUNC, TERM, SORT> MinimizeHopcroft(int timeout)
        {
            var det = this.Determinize();
            var min = det.automaton.MinimizeHopcroft(timeout);
            return new SFA<FUNC, TERM, SORT>(solver, inpSort, "Min[" + name + "]", min);
        }

        #region normalization of labels
        /// <summary>
        /// Normalize the labels.
        /// </summary>
        public SFA<FUNC, TERM, SORT> NormalizeLabels()
        {
            var res = Automaton<TERM>.Create(automaton.Algebra, automaton.InitialState, automaton.GetFinalStates(), NormalizeLabels(automaton.GetMoves()));
            return new SFA<FUNC, TERM, SORT>(solver, inpSort, "Norm[" + name + "]", res);
        }

        private IEnumerable<Move<TERM>> NormalizeLabels(IEnumerable<Move<TERM>> moves)
        {
            var done = new Dictionary<TERM, TERM>();
            foreach (var move in moves)
                if (done.ContainsKey(move.Label))
                    yield return Move<TERM>.Create(move.SourceState, move.TargetState, done[move.Label]);
                else
                {
                    var simpl = NormalizeCondition(move.Label);
                    done[move.Label] = simpl;
                    yield return Move<TERM>.Create(move.SourceState, move.TargetState, simpl);
                }
        }

        private TERM NormalizeCondition(TERM term)
        {
            if (!solver.IsSatisfiable(solver.MkNot(term)))
                return solver.True;
            else
                return solver.Simplify(term);
        }

        //private Expr NormalizeCondition(Expr term)
        //{
        //    var _i = ab.CharVar;
        //    if (!ab.IsSatisfiable(ab.MkNot(term)))
        //        return ab.True;
        //    else
        //    {
        //        //enumerate all solutions explicitly
        //        List<int> solutions = new List<int>();
        //        foreach (var v in ab.Z3p.EnumerateAllMembers(term))
        //        {
        //            int n;
        //            if (v.TryGetNumeralValue(out n))
        //                solutions.Add(n);
        //            else
        //                throw new Exception("unexpected character solution");
        //        }
        //        solutions.Sort();
        //        var ranges = new Microsoft.Automata.Utilities.UnicodeCategoryRangesGenerator.Ranges();
        //        foreach (char c in solutions)
        //            ranges.Add((int)c);
        //        List<TERM> rangeConds = new List<TERM>();
        //        foreach (var range in ranges.ranges)
        //        {
        //            if (range[0] == range[1])
        //                rangeConds.Add(ab.Z3p.MkEq(_i, ab.MkChar((char)range[0])));
        //            else
        //                rangeConds.Add(ab.MkAnd(ab.MkCharLe(ab.MkChar((char)range[0]), _i),
        //                                        ab.MkCharLe(_i, ab.MkChar((char)range[1]))));
        //        }
        //        return ab.MkOr(rangeConds.ToArray());
        //    }
        //}

        #endregion


        #region IAutomaton Members

        public int InitialState
        {
            get { return automaton.InitialState; }
        }

        public bool IsFinalState(int state)
        {
            return automaton.IsFinalState(state);
        }

        public IEnumerable<Move<TERM>> GetMoves()
        {
            return automaton.GetMoves();
        }

        public IEnumerable<int> GetStates()
        {
            return automaton.GetStates();
        }

        public string DescribeState(int state)
        {
            return automaton.DescribeState(state);
        }

        public string DescribeLabel(TERM lab)
        {
            return Solver.PrettyPrint(lab, c => (c.Equals(solver.MkVar(0,inpSort)) ? "c" : c.ToString()));
        }

        #endregion


        #region IAcceptor<FuncDecl,Expr,Sort> Members

        bool theoryIsAsserted = false;

        /// <summary>
        /// Assert the theory of the SFA as an auxiliary background theory to the given SMT solver.
        /// Reasserts the theory if the acceptor is not is scope.
        /// </summary>
        public void AssertTheory()
        {
            if (!theoryIsAsserted)
            {
                acc = MkAcc();
                theoryIsAsserted = true;
            }
        }

        FUNC MkAcc()
        {
            var stringSort = solver.MkListSort(inpSort);
            var StringSort = stringSort;
            var EmptyString = solver.GetNil(stringSort);
            var _char = solver.MkVar(0, inpSort);

            Dictionary<int, FUNC> acceptDecls = new Dictionary<int, FUNC>();
            foreach (int state in automaton.States)
                acceptDecls[state] = solver.MkFreshFuncDecl(name + state,
                                                 new SORT[] { stringSort }, solver.BoolSort);

            TERM x = solver.MkVar(0, StringSort);
            TERM xIsEmpty = solver.MkEq(x, EmptyString);

            foreach (int state in automaton.States)
            {
                //create axioms for the transitions 
                //for each state n and transitions 
                //  (n,cond1,n1),...,(n,condk,nk),(n,m1),...,(n,ml)
                //create the axiom 
                //  accept_n(x) <=> ((x != nil) & 
                //                         OR_{1<=i<=k} ( condi[head(x)] & accept_ni(tail(x))) |
                //                       OR_{1<=i<=l} accept_mi(x) |
                //                       ite(n is final, x=nil, false)
                //assumes that the FA does not include epsilon-loops
                //or else terminaton is not guaranteed

                List<Move<TERM>> trans = new List<Move<TERM>>(automaton.GetMovesFrom(state));
                TERM lhs = solver.MkApp(acceptDecls[state], x);
                List<TERM> rhs_cases = new List<TERM>();
                List<TERM> rhs_extra_cases = new List<TERM>();

                foreach (Move<TERM> t in trans)
                    if (t.IsEpsilon)
                        rhs_extra_cases.Add(solver.MkApp(acceptDecls[t.TargetState], x));
                    else
                        rhs_cases.Add(solver.MkAnd(solver.ApplySubstitution(t.Label, _char, solver.MkFirstOfList(x)),
                                            solver.MkApp(acceptDecls[t.TargetState], solver.MkRestOfList(x))));

                if (automaton.IsFinalState(state))
                    rhs_extra_cases.Add(xIsEmpty);

                TERM rhs_extra = (rhs_extra_cases.Count == 0 ? solver.False :
                    (rhs_extra_cases.Count == 1 ? rhs_extra_cases[0] :
                     solver.MkOr(rhs_extra_cases.ToArray())));

                TERM rhs_nonempty_case = solver.MkAnd(solver.MkNot(xIsEmpty), solver.MkOr(rhs_cases.ToArray()));

                TERM rhs = (rhs_extra.Equals(solver.False) ? rhs_nonempty_case :
                            solver.MkOr(rhs_extra, rhs_nonempty_case));

                solver.MainSolver.Assert(solver.MkAxiom(lhs, rhs, x));
            }

            return acceptDecls[automaton.InitialState];
        }

        /// <summary>
        /// Gets the symbolic acceptor of the SFA.
        /// A relation symbol with one argument with sort InputListSort.
        /// Assumes that the theory has been asserted.
        /// </summary>
        public FUNC Acceptor
        {
            get
            {
                if (!theoryIsAsserted)
                    throw new AutomataException(AutomataExceptionKind.TheoryIsNotAsserted);

                return acc;
            }
        }

        /// <summary>
        /// Constructs the automaton acceptor atom 'Acceptor(inList)' for the SFA.
        /// Assumes that the theory has been asserted and is in scope.
        /// </summary>
        /// <param name="inList">any term of sort InputListSort</param>
        public TERM MkAccept(TERM inList)
        {
            if (!solver.GetSort(inList).Equals(this.InputListSort))
                throw new AutomataException(AutomataExceptionKind.SortMismatch);

            if (!theoryIsAsserted)
                throw new AutomataException(AutomataExceptionKind.TheoryIsNotAsserted);

            return solver.MkApp(acc, inList);
        }

        /// <summary>
        /// Given SMT solver of the SFA.
        /// </summary>
        public IContext<FUNC, TERM, SORT> Solver
        {
            get { return solver; }
        }

        #endregion

        /// <summary>
        /// Sort of the input elements in lists accepted by the SFA.
        /// </summary>
        public SORT InputSort
        {
            get { return inpSort; }
        }

        /// <summary>
        /// Sort of the input lists accepted by the SFA.
        /// </summary>
        public SORT InputListSort
        {
            get { return solver.MkListSort(inpSort); }
        }

        #region member generation
        /// <summary>
        /// Choose a path of symbolic terms from the initial state to some final state. 
        /// Uses Solver.Chooser to control the random choices.
        /// </summary>
        public IEnumerable<TERM> ChoosePathToSomeFinalState()
        {
            return automaton.ChoosePathToSomeFinalState(Solver.Chooser);
        }
        #endregion


        public int StateCount { get { return automaton.StateCount; } }

        public int MoveCount { get { return automaton.MoveCount; } }

        #region IAutomaton<TERM> Members


        public string DescribeStartLabel()
        {
            return "";
        }

        #endregion

        #region Conretize
        /// <summary>
        /// Concertize the SFA by including at most k characters in the label.
        /// </summary>
        /// <param name="k">upper limit on the number of included characters in the output automaton, when 0 or negative then include all elements</param>
        /// <returns></returns>
        public Automaton<BDD> Concretize(int k = 0)
        {
            //if (k <= 0)
            //    throw new AutomataException(AutomataExceptionKind.InvalidArgument);

            var mem = new Dictionary<TERM, BDD>();

            var concrete_moves = new List<Move<BDD>>();

            var moveMap = new Dictionary<Tuple<int, int>, BDD>();
            Action<int,int,BDD> AddMove = (from,to,guard) =>
                {
                    BDD pred;
                    var key = new Tuple<int,int>(from, to);
                    if (moveMap.TryGetValue(key, out pred))
                        pred = solver.CharSetProvider.MkOr(pred, guard);
                    else
                        pred = guard;
                    moveMap[key] = pred;
                };

            Predicate<TERM> IsGround = t => 
                {
                    foreach (var v in solver.GetVars(t))
                        return false;
                    return true;
                };

            foreach (var move in automaton.GetMoves())
            {
                if (move.IsEpsilon)
                {
                    concrete_moves.Add(Move<BDD>.Epsilon(move.SourceState, move.TargetState));
                    continue;
                }
                BDD set;
                if (mem.TryGetValue(move.Label, out set))
                {
                    AddMove(move.SourceState, move.TargetState, set);
                    //concrete_moves.Add(Move<BvSet>.M(move.SourceState, move.TargetState, set));
                    continue;
                }
                if (k > 0)
                {
                    if (IsGround(move.Label))  //must be satisfiable so same as true
                    {
                        set = solver.CharSetProvider.MkRangeConstraint((char)0, (char)(k - 1));
                        mem[move.Label] = set;
                        AddMove(move.SourceState, move.TargetState, set);
                        //concrete_moves.Add(Move<BvSet>.M(move.SourceState, move.TargetState, set));
                        continue;
                    }
                    var elems = new List<uint>();
                    foreach (var v in solver.MainSolver.FindAllMembers(move.Label))
                    {
                        elems.Add(solver.GetNumeralUInt(v.Value));
                        if (elems.Count == k)
                            break;
                    }
                    set = solver.CharSetProvider.MkSetFromElements(elems, ((int)solver.CharSetProvider.Encoding) - 1);
                    mem[move.Label] = set;
                    AddMove(move.SourceState, move.TargetState, set);
                    //concrete_moves.Add(Move<BvSet>.M(move.SourceState, move.TargetState, set));
                }
                else
                {

                    BDD cond = solver.ConvertToCharSet(solver.CharSetProvider, move.Label);
                    if (cond!=null)
                        throw new AutomataException(AutomataExceptionKind.ConditionCannotBeConvertedToCharSet);

                    mem[move.Label] = cond;
                    AddMove(move.SourceState, move.TargetState, cond);
                    //concrete_moves.Add(Move<BvSet>.M(move.SourceState, move.TargetState, cond));
                }
            }
            foreach (var entry in moveMap)
                concrete_moves.Add(Move<BDD>.Create(entry.Key.Item1, entry.Key.Item2, entry.Value));

            var res = Automaton<BDD>.Create(this.solver.CharSetProvider, this.automaton.InitialState, this.automaton.GetFinalStates(), concrete_moves);
            return res;
        }

        public IEnumerable<Move<TERM>> GetMovesFrom(int state)
        {
            return automaton.GetMovesFrom(state);
        }
        #endregion


        public IBooleanAlgebra<TERM> Algebra
        {
            get { return solver; }
        }
    }
}

