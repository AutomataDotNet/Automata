using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Microsoft.Automata
{
    public class STbFromRegexBuilder<F, T, S>
    {
        IContext<F, T, S> solver;

        public STbFromRegexBuilder(IContext<F, T, S> solver)
        {
            this.solver = solver;
        }

        public STb<F, T, S> Mk(string regex, params string[] parseInfo)
        {
            if (parseInfo.Length % 2 != 0)
                throw new AutomataException(AutomataExceptionKind.InvalidArguments);

            var K = parseInfo.Length / 2;
            var args = new Tuple<string, string>[K];

            for (int i = 0; i < parseInfo.Length; i += 2)
                args[i / 2] = new Tuple<string, string>(parseInfo[i], parseInfo[i+1]);

            bool isLoop;
            var patternAutomataPairs = solver.CharSetProvider.ConvertCaptures(regex, out isLoop);
            var captureAutomata = new Dictionary<string, Automaton<BDD>>();
            foreach (var pair in patternAutomataPairs)
                if (pair.Item1 != "")
                    captureAutomata[pair.Item1] = pair.Item2;

            var captureSortPos = new Dictionary<string,int>();
            var captureSortName = new Dictionary<string,string>();
            for (int i = 0; i < args.Length; i += 1)
            {
                captureSortName[args[i].Item1] = args[i].Item2;
                captureSortPos[args[i].Item1] = i;
            }

            if (Array.Exists(patternAutomataPairs, pair => (pair.Item1 != "" && !captureSortName.ContainsKey(pair.Item1))))
                throw new AutomataException(AutomataExceptionKind.InvalidArguments);

            S[] argSorts = new S[K];
            for (int i = 0; i < K; i++)
            {
                if (!captureAutomata.ContainsKey(args[i].Item1))
                    throw new AutomataException(AutomataExceptionKind.InvalidArguments);
                else if (args[i].Item2 == "int")
                    argSorts[i] = solver.MkBitVecSort(32);
                else if (args[i].Item2 == "last")
                    argSorts[i] = solver.CharSort;
                else if (args[i].Item2 == "length")
                    argSorts[i] = solver.MkBitVecSort(32);
                else if (args[i].Item2 == "bool")
                    argSorts[i] = solver.BoolSort;
                else
                    throw new AutomataException(AutomataExceptionKind.InvalidArguments);
            }

            var regSort = solver.MkTupleSort(argSorts);
            var initReg = solver.MkTuple(Array.ConvertAll(argSorts, s => (s.Equals(solver.BoolSort) ? solver.False : solver.MkNumeral(0, solver.MkBitVecSort(32)))));
            var regVar = solver.MkVar(1, regSort);
            var inpVar = solver.MkVar(0, solver.CharSort);

            var stb = new STb<F, T, S>(solver, "stb", solver.CharSort, regSort, regSort, initReg, 0);


            var nextStateId = 0;
            var stateIdMap = new Dictionary<Tuple<int,int>,int>();
            Func<int,int,int> MkState = (n,q) => {
                int p;
                var nq = new Tuple<int,int>(n,q);
                if (stateIdMap.TryGetValue(nq, out p))
                    return p;
                else
                {
                    p = nextStateId;
                    nextStateId +=1;
                    stateIdMap[nq] = p;
                    return p;
                }
            };

            var allMoves = new List<Move<Tuple<BDD, T>>>();
            var bv32 = solver.MkBitVecSort(32);

            Func<T, T> AddZeros = (c) =>
            {
                uint k = (uint)(32 - (int)solver.CharSetProvider.Encoding);
                if (k == 0)
                    return c;
                else
                    return solver.MkZeroExt(k, c);
            };

            Func<int, T> ToInt = (i) =>
            {
                var elems = new T[K];
                for (int j = 0; j < K; j++)
                {
                    if (i == j)
                        elems[j] = solver.MkBvAdd(solver.MkBvMul(solver.MkNumeral(10, bv32), solver.MkProj(j, regVar)), solver.MkBvSub(AddZeros(inpVar), solver.MkNumeral((int)'0', bv32)));
                    else
                        elems[j] = solver.MkProj(j, regVar);
                }
                var res = solver.MkTuple(elems);
                return res;
            };

            Func<int, T> ToLen = (i) =>
            {
                var elems = new T[K];
                for (int j = 0; j < K; j++)
                {
                    if (i == j)
                        elems[j] = solver.MkBvAdd(solver.MkNumeral(1, bv32), solver.MkProj(j, regVar));
                    else
                        elems[j] = solver.MkProj(j, regVar);
                }
                var res = solver.MkTuple(elems);
                return res;
            };

            Func<int, T> KeepLast = (i) =>
            {
                var elems = new T[K];
                for (int j = 0; j < K; j++)
                {
                    if (i == j)
                        elems[j] = inpVar;
                    else
                        elems[j] = solver.MkProj(j, regVar);
                }
                var res = solver.MkTuple(elems);
                return res;
            };

            Func<int,bool, T> AssignBool = (i,b) =>
            {
                var elems = new T[K];
                for (int j = 0; j < K; j++)
                {
                    if (i == j)
                        elems[j] = (b ? solver.True : solver.False);
                    else
                        elems[j] = solver.MkProj(j, regVar);
                }
                var res = solver.MkTuple(elems);
                return res;
            };


            for (int i = 0; i < patternAutomataPairs.Length; i++)
            {
                var aut = patternAutomataPairs[i].Item2;
                if (patternAutomataPairs[i].Item1 == "")
                {
                    foreach (var m in aut.GetMoves())
                        allMoves.Add(Move<Tuple<BDD, T>>.Create(MkState(i, m.SourceState), MkState(i, m.TargetState), new Tuple<BDD, T>(m.Label, regVar)));
                }
                else if (captureSortName[patternAutomataPairs[i].Item1] == "int")
                {
                    foreach (var m in aut.GetMoves())
                        allMoves.Add(Move<Tuple<BDD, T>>.Create(MkState(i, m.SourceState), MkState(i, m.TargetState), new Tuple<BDD, T>(m.Label, ToInt(captureSortPos[patternAutomataPairs[i].Item1]))));
                }
                else if (captureSortName[patternAutomataPairs[i].Item1] == "length")
                {
                    foreach (var m in aut.GetMoves())
                        allMoves.Add(Move<Tuple<BDD, T>>.Create(MkState(i, m.SourceState), MkState(i, m.TargetState), new Tuple<BDD, T>(m.Label, ToLen(captureSortPos[patternAutomataPairs[i].Item1]))));
                }
                else if (captureSortName[patternAutomataPairs[i].Item1] == "last")
                {
                    foreach (var m in aut.GetMoves())
                        allMoves.Add(Move<Tuple<BDD, T>>.Create(MkState(i, m.SourceState), MkState(i, m.TargetState), new Tuple<BDD, T>(m.Label, KeepLast(captureSortPos[patternAutomataPairs[i].Item1]))));
                }
                else if (captureSortName[patternAutomataPairs[i].Item1] == "bool")
                {
                    var boolAcceptor = solver.CharSetProvider.Convert("^((T|t)rue|(F|f)alse)$").Intersect(patternAutomataPairs[i].Item2).Minimize();

                    if (boolAcceptor.IsEmpty)
                        throw new AutomataException(AutomataExceptionKind.CaptureIsInfeasibleAsBoolean);

                    patternAutomataPairs[i] = new Tuple<string, Automaton<BDD>>(patternAutomataPairs[i].Item1, boolAcceptor);

                    var _t = solver.CharSetProvider.MkCharSetFromRanges('t','t','T','T');
                    var _f = solver.CharSetProvider.MkCharSetFromRanges('f','f','F','F');
                    foreach (var m in boolAcceptor.GetMoves())
                    {
                        if (m.SourceState == boolAcceptor.InitialState)
                        {
                            if (solver.CharSetProvider.IsSatisfiable(solver.CharSetProvider.MkAnd(_t, m.Label)))
                                allMoves.Add(Move<Tuple<BDD, T>>.Create(MkState(i, m.SourceState), MkState(i, m.TargetState), new Tuple<BDD, T>(m.Label, AssignBool(captureSortPos[patternAutomataPairs[i].Item1],true))));
                            else if (solver.CharSetProvider.IsSatisfiable(solver.CharSetProvider.MkAnd(_f, m.Label)))
                                allMoves.Add(Move<Tuple<BDD, T>>.Create(MkState(i, m.SourceState), MkState(i, m.TargetState), new Tuple<BDD, T>(m.Label, AssignBool(captureSortPos[patternAutomataPairs[i].Item1], false))));
                        }
                        else
                        {
                            allMoves.Add(Move<Tuple<BDD, T>>.Create(MkState(i, m.SourceState), MkState(i, m.TargetState), new Tuple<BDD, T>(m.Label, regVar)));
                        }
                    }
                }
                else
                    throw new NotImplementedException();
            }

            for (int i = 0; i < patternAutomataPairs.Length-1; i++)
            {
                foreach (var q in patternAutomataPairs[i].Item2.GetFinalStates())
                    allMoves.Add(Move<Tuple<BDD, T>>.Epsilon(MkState(i, q), MkState(i + 1, patternAutomataPairs[i + 1].Item2.InitialState)));
            }

            var L = patternAutomataPairs.Length - 1;

            var finalStates = new List<int>();
            foreach (var f in patternAutomataPairs[L].Item2.GetFinalStates())
                finalStates.Add(MkState(L, f));

            var tmpAutE = Automaton<Tuple<BDD, T>>.Create(null, MkState(0, patternAutomataPairs[0].Item2.InitialState), finalStates, allMoves);
            Func<Tuple<BDD, T>, Tuple<BDD, T>, Tuple<BDD, T>> error = (f1, f2) =>
                {
                    throw new AutomataException(AutomataExceptionKind.InternalError);
                };

            var tmpAut = tmpAutE.RemoveEpsilons(error);
            tmpAut.isDeterministic = true;

            var name = "";
            foreach (var pair in args)
                name = (name == "" ? pair.Item2 : name + "_" + pair.Item2);
            var STB = Automaton2STb(tmpAut, name, regSort, initReg, isLoop);
            return STB;
        }

        public STb<F, T, S> Mk(string regex, params Tuple<string,STb<F,T,S>>[] args)
        {
            var K = args.Length;
            bool isLoop;
            var patternAutomataPairs = solver.CharSetProvider.ConvertCaptures(regex, out isLoop);
            var captureAutomata = new Dictionary<string, Automaton<BDD>>();
            var stbs = new Dictionary<string,STb<F,T,S>>();
            foreach (var arg in args)
            {
                if (stbs.ContainsKey(arg.Item1) || string.IsNullOrEmpty(arg.Item1))
                    throw new AutomataException(AutomataExceptionKind.InvalidArguments);
                stbs[arg.Item1] = arg.Item2;
            }
            foreach (var pair in patternAutomataPairs)
                if (pair.Item1 != "")
                    captureAutomata[pair.Item1] = pair.Item2;

            var captureSortPos = new Dictionary<string, int>();
            var captureSortName = new Dictionary<string, string>();
            for (int i = 0; i < args.Length; i += 1)
            {
                captureSortName[args[i].Item1] = args[i].Item2.OutputSort.ToString();
                captureSortPos[args[i].Item1] = i;
            }

            if (Array.Exists(patternAutomataPairs, pair => (pair.Item1 != "" && !captureSortName.ContainsKey(pair.Item1))))
                throw new AutomataException(AutomataExceptionKind.InvalidArguments);

            S[] argSorts = new S[K];
            for (int i = 0; i < K; i++)
            {
                if (!captureAutomata.ContainsKey(args[i].Item1))
                    throw new AutomataException(AutomataExceptionKind.InvalidArguments);
                if (!args[i].Item2.OutputSort.Equals(args[i].Item2.RegisterSort))
                    throw new AutomataException(AutomataExceptionKind.InvalidArguments);
                argSorts[i] = args[i].Item2.OutputSort;
            }

            var regSort = solver.MkTupleSort(argSorts);
            var regVar = solver.MkVar(1, regSort);
            var initReg = solver.MainSolver.FindOneMember(solver.MkEq(regVar, regVar)).Value;
            var inpVar = solver.MkVar(0, solver.CharSort);

            var stb = new STb<F, T, S>(solver, "stb", solver.CharSort, regSort, regSort, initReg, 0);

            var nextStateId = 0;
            var stateIdMap = new Dictionary<Tuple<int, int, int>, int>();
            Func<int, int, int, int> MkState = (n, q1, q2) =>
            {
                int p;
                var nq = new Tuple<int, int, int>(n, q1, q2);
                if (stateIdMap.TryGetValue(nq, out p))
                    return p;
                else
                {
                    p = nextStateId;
                    nextStateId += 1;
                    stateIdMap[nq] = p;
                    return p;
                }
            };

            var resSTB = new STb<F, T, S>(solver, "STB", solver.CharSort, solver.CharSort, solver.UnitSort, solver.UnitConst, 0);
            resSTB.AssignRule(0, new BaseRule<T>(new Sequence<T>(solver.MkCharVar(0)), solver.UnitConst, 0));
            resSTB.AssignFinalRule(0, new BaseRule<T>(Sequence<T>.Empty, solver.UnitConst, 0));

            for (int i = 0; i < patternAutomataPairs.Length; i++)
            {
                var aut = patternAutomataPairs[i].Item2;

                if (patternAutomataPairs[i].Item1 == "")
                {
                    var autSTMoves = new List<Move<Rule<T>>>();
                    foreach (var move in aut.GetMoves())
                    {
                        //move cannot be epsilon here
                        var cond = solver.ConvertFromCharSet(move.Label);
                        autSTMoves.Add(Move<Rule<T>>.Create(move.SourceState, move.TargetState, Rule<T>.Mk(cond, solver.UnitConst)));
                    }
                    foreach (var f in aut.GetFinalStates())
                    {
                        //collect guards of all moves exitingfrom f
                        var allGuardsFromF = solver.CharSetProvider.False;
                        foreach (var fmove in aut.GetMovesFrom(f))
                            allGuardsFromF = solver.CharSetProvider.MkOr(allGuardsFromF, fmove.Label);
                        var elseFromF = solver.ConvertFromCharSet(solver.CharSetProvider.MkNot(allGuardsFromF));
                        autSTMoves.Add(Move<Rule<T>>.Create(f, f, Rule<T>.Mk(elseFromF, solver.UnitConst, solver.MkCharVar(0))));
                        autSTMoves.Add(Move<Rule<T>>.Create(f, f, Rule<T>.MkFinal(solver.True)));
                    }

                    var autST = ST<F, T, S>.Create(solver, patternAutomataPairs[i].Item1, solver.UnitConst, solver.CharSort,
                        solver.CharSort, solver.UnitSort, aut.InitialState, autSTMoves);

                    var autSTb = autST.ToSTb();
                    resSTB = resSTB.Compose(autSTb);
                }
                else
                {
                    var stb1 = stbs[patternAutomataPairs[i].Item1];
                    if (!stb1.InputSort.Equals(solver.CharSort))
                        throw new AutomataException(AutomataExceptionKind.InvalidArguments);

                    var autSTMoves = new List<Move<Rule<T>>>();
                    foreach (var move in aut.GetMoves())
                    {
                        //move cannot be epsilon here
                        var cond = solver.ConvertFromCharSet(move.Label);
                        autSTMoves.Add(Move<Rule<T>>.Create(move.SourceState, move.TargetState, Rule<T>.Mk(cond, solver.UnitConst, inpVar)));
                    }
                    foreach (var f in aut.GetFinalStates())
                        autSTMoves.Add(Move<Rule<T>>.Create(f, f, Rule<T>.MkFinal(solver.True)));

                    var autST = ST<F, T, S>.Create(solver, patternAutomataPairs[i].Item1, solver.UnitConst, solver.CharSort,
                        solver.CharSort, solver.UnitSort, aut.InitialState, autSTMoves);

                    var autSTb = autST.ToSTb();

                    var stb2 = autSTb.Compose(stb1);

                    foreach (var f in stb.States)
                    {
                        var frule = stb.GetFinalRuleFrom(f);
                        if (frule.IsNotUndef)
                        {
                           //var frule1 = 
                        }
                    }
                }
            }

            throw new NotImplementedException();
        }

        STb<F,T,S> Automaton2STb(Automaton<Tuple<BDD, T>> aut, string name, S regSort, T initReg, bool isLoop)
        {
            if (isLoop)
                if (aut.HasMoreThanOneFinalState || aut.GetMovesCountFrom(aut.FinalState) > 0)
                     throw new AutomataException(AutomataExceptionKind.FinalStateMustBeSink);

            var stb = new STb<F,T,S>(solver, name, solver.CharSort, regSort, regSort, initReg, aut.InitialState);

            Func<int, int> getState = (p) =>
                {
                    if (isLoop && aut.IsFinalState(p))
                        return aut.InitialState;
                    else
                        return p;
                };

            Func<int, Sequence<T>> getYield = (p) =>
            {
                if (isLoop && aut.IsFinalState(p))
                    return Sequence<T>.Empty.Append(solver.MkVar(1, regSort));
                else
                    return Sequence<T>.Empty;
            };

            Func<int,T, T> getUpdate = (p,u) =>
            {
                if (isLoop && aut.IsFinalState(p))
                    return initReg;
                else
                    return u;
            };

            if (isLoop)
            {
                foreach (var q in aut.States)
                    if (!aut.IsFinalState(q))
                        stb.AssignRule(q, MkSTbRule(aut.GetMovesFrom(q), getState, getYield, getUpdate));
                stb.AssignFinalRule(aut.InitialState, new BaseRule<T>(Sequence<T>.Empty, solver.UnitConst, aut.InitialState));
            }
            else
            {
                foreach (var q in aut.States)
                    stb.AssignRule(q, MkSTbRule(aut.GetMovesFrom(q), getState, getYield, getUpdate));

                foreach (var f in aut.GetFinalStates())
                    stb.AssignFinalRule(f, new BaseRule<T>(Sequence<T>.Empty.Append(solver.MkVar(1, regSort)), solver.UnitConst, f));
            }

            return stb;
        }

        BranchingRule<T> MkSTbRule(IEnumerable<Move<Tuple<BDD, T>>> moves, Func<int, int> getState, Func<int, Sequence<T>> getYield, Func<int,T, T> getUpdate)
        {
            List<Tuple<T, BaseRule<T>>> cases = new List<Tuple<T, BaseRule<T>>>();
            var allGuards = solver.CharSetProvider.False;
            foreach (var m in moves)
            {
                cases.Add(new Tuple<T, BaseRule<T>>(ConvertToPredicate(m.Label.Item1),
                    new BaseRule<T>(getYield(m.TargetState), getUpdate(m.TargetState, m.Label.Item2), getState(m.TargetState))));
                allGuards = solver.CharSetProvider.MkOr(allGuards, m.Label.Item1);
            }
            BranchingRule<T> r = UndefRule<T>.Default;
            if (cases.Count > 0)
            {
                if (allGuards.IsFull)
                    r = cases[0].Item2;
                else
                    r = new IteRule<T>(cases[0].Item1, cases[0].Item2, UndefRule<T>.Default);
                for (int i = 1; i < cases.Count; i++)
                    r = new IteRule<T>(cases[i].Item1, cases[i].Item2, r);
            }
            return r;
        }

        T ConvertToPredicate(BDD set)
        {
            var ranges = solver.CharSetProvider.ToRanges(set);
            var res = solver.False;
            foreach (var range in ranges)
            {
                T charPred;
                if (range.Item1 == range.Item2)
                    charPred = solver.MkEq(solver.MkVar(0,solver.CharSort),solver.MkNumeral(range.Item1,solver.CharSort));
                else 
                    charPred = solver.MkAnd(solver.MkCharLe(solver.MkNumeral(range.Item1,solver.CharSort), solver.MkVar(0,solver.CharSort)),
                        solver.MkCharLe(solver.MkVar(0,solver.CharSort), solver.MkNumeral(range.Item2,solver.CharSort)));
                if (res.Equals(solver.False))
                    res = charPred;
                else
                    res = solver.MkOr(res,charPred);
            }
            return res;
        }
    }
}
