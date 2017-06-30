using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Automata.Z3;
using Microsoft.Automata;
using Microsoft.Z3;
using Microsoft.Bek.Frontend.Meta;
using Microsoft.Bek.Frontend;
using Microsoft.Bek.Frontend.AST;
using Microsoft.Bek.Model.Converters;

using STBuilderZ3 = Microsoft.Automata.STBuilder<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;
using STModel = Microsoft.Automata.STb<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;

using BV2Solver = Microsoft.Automata.BooleanAlgebras.DisjointUnionAlgebra<Microsoft.Automata.BDD, Microsoft.Automata.BDD>;
using BV2 = System.Tuple<Microsoft.Automata.BDD, Microsoft.Automata.BDD>;

namespace Microsoft.Bek.Model.Converters.Z3
{
    internal class BekProg2STb : ABekProg<STModel, STModel, Expr>
    {
        internal STBuilderZ3 stb;
        internal Sort charsort;
        internal CharSetSolver css;
        string name;

        BV2Solver css2;

        public BekProg2STb(AStrExpr<STModel, Expr> ase, STBuilderZ3 stb, Sort charsort, string name)
            : base(ase)
        {
            this.name = name;
            this.stb = stb;
            this.charsort = charsort;
            Z3Provider z3p = (Z3Provider)stb.Solver;
            css = z3p.CharSetProvider;
            css2 = new BV2Solver(z3p.CharSetProvider, z3p.CharSetProvider);
        }

        protected override STModel ReturnModel(BekProgram p, STModel res)
        {
            res.Name = p.ast.name;
            return res;
        }

        public override void Dispose()
        {
            //this.stb.Solver.Dispose();
        }

        public override STModel Convert(BekProgram a)
        {
            expr bek = ((returnstmt)a.ast.body).val;
            if (bek is replace)
                return ConvertReplace(bek as replace);
            else
                return base.Convert(a);
        }

        STModel ConvertReplace(replace repl)
        {
            //create a disjunction of all the regexes
            //each case terminated by the identifier
            int K = 0; //max pattern length
            //HashSet<int> finalReplacers = new HashSet<int>();

            //for efficieny keep lookup tables of character predicates to sets
            Dictionary<Expr, BDD> predLookup = new Dictionary<Expr, BDD>();

            Automaton<BDD> previouspatterns = Automaton<BDD>.MkEmpty(css);

            Automaton<BV2> N = Automaton<BV2>.MkFull(css2);

            var hasNoEndAnchor = new HashSet<int>();

            for (int i = 0; i < repl.CaseCount; i++)
            {
                replacecase rcase = repl.GetCase(i);           
                var pat = "^" + rcase.Pattern.val;
                var M = css.Convert("^" + rcase.Pattern.val, System.Text.RegularExpressions.RegexOptions.Singleline).Determinize().Minimize();

                #region check that the pattern is a feasible nonempty sequence
                if (M.IsEmpty)
                    throw new BekParseException(string.Format("Semantic error: pattern {0} is infeasible.", rcase.Pattern.ToString()));
                int _K;
                if (!M.CheckIfSequence(out _K))
                    throw new BekParseException(string.Format("Semantic error: pattern {0} is not a sequence.", rcase.Pattern.ToString()));
                if (_K == 0)
                    throw new BekParseException(string.Format("Semantic error: empty pattern {0} is not allowed.", rcase.Pattern.ToString()));
                K = Math.Max(_K, K);
                #endregion 

                var liftedMoves = new List<Move<BV2>>();
                var st = M.InitialState;
                var newFinalState = M.MaxState + 1;
                var endAnchor = css.MkCharConstraint( (char)i);

                //lift the moves to BV2 moves, adding end-markers
                while (!M.IsFinalState(st))
                {
                    var mv = M.GetMoveFrom(st);
                    var pair_cond = new BV2(mv.Label, css.False);
                    var liftedMove = new Move<BV2>(mv.SourceState, mv.TargetState, pair_cond);
                    liftedMoves.Add(liftedMove);
                    if (M.IsFinalState(mv.TargetState))
                    {
                        var end_cond = new BV2(css.False, endAnchor);
                        if (M.IsLoopState(mv.TargetState))
                        {
                            hasNoEndAnchor.Add(i);
                            //var loop_cond = css2.MkNot(end_cond);
                            //var loopMove = new Move<BV2>(mv.TargetState, mv.TargetState, loop_cond);
                            //liftedMoves.Add(loopMove);
                        }
                        var endMove = new Move<BV2>(mv.TargetState, newFinalState, end_cond);
                        liftedMoves.Add(endMove);
                    }
                    st = mv.TargetState;
                }
                var N_i = Automaton<BV2>.Create(css2, M.InitialState, new int[] { newFinalState }, liftedMoves);

                //Microsoft.Automata.Visualizer.ToDot(N_i, "N" + i , "C:\\Automata\\Docs\\Papers\\Bex\\N" + i +".dot", x => "(" + css.PrettyPrint(x.First) + "," + css.PrettyPrint(x.Second) + ")");

                N = N.Intersect(N_i.Complement());

                #region other approach: disallow overlapping patterns

                //Visualizer.ShowGraph(M2.Complement(css2), "M2", lab => { return "<" + css.PrettyPrint(lab.First) + "," + css.PrettyPrint(lab.Second) + ">"; });

                //note: keep here the original pattern, add only the start anchor to synchronize prefixes
                //var thispattern = css.Convert("^" + rcase.Pattern.val, System.Text.RegularExpressions.RegexOptions.Singleline).Determinize(css).Minimize(css);
                
                //var thispattern1 = thispattern.Minus(previouspatterns, css);
                //Visualizer.ShowGraph(thispattern1, "test", css.PrettyPrint);

                //#region check that thispattern does not overlap with any previous pattern
                //var common = thispattern.Intersect(previouspatterns, css);
                //if (!(common.IsEmpty))
                //{
                //    int j = 0;
                //    while ((j < i) && css.Convert("^" + repl.GetCase(j).Pattern.val,
                //        System.Text.RegularExpressions.RegexOptions.Singleline).Determinize(css).Intersect(thispattern, css).IsEmpty)
                //        j++;

                //    throw new BekParseException(rcase.id.line, rcase.id.pos, string.Format("Semantic error: pattern {0} overlaps pattern {1}.", 
                //        rcase.Pattern.ToString(), repl.GetCase(j).Pattern.ToString()));
                //}
                //previouspatterns = previouspatterns.Union(thispattern).RemoveEpsilons(css.MkOr); //TBD: better union
                //#endregion

               #endregion

            }
             
            N = N.Complement().Minimize();
            //Microsoft.Automata.Visualizer.ShowGraph(N, "N", x => "<" + css.PrettyPrint(x.First) + "," + css.PrettyPrint(x.Second) + ">");
            //Microsoft.Automata.Visualizer.ToDot(N, "N","C:\\Automata\\Docs\\Papers\\Bex\\N.dot", x => "(" + css.PrettyPrint(x.First) + "," + css.PrettyPrint(x.Second) + ")");

            var D = new Dictionary<int, int>();
            var G = new Dictionary<int, BDD>();

            #region compute distance from initial state and compute guard unions
            var S = new Stack<int>();
            D[N.InitialState] = 0;
            G[N.InitialState] = css.False;
            S.Push(N.InitialState);
            while (S.Count > 0)
            {
                var q = S.Pop();
                foreach (var move in N.GetMovesFrom(q))
                {
                    G[q] = css.MkOr(G[q], move.Label.Item1);
                    var p = move.TargetState;
                    var d = D[q] + 1;
                    if (!(N.IsFinalState(p)) && !D.ContainsKey(p))
                    {
                        D[p] = d;
                        G[p] = css.False;
                        S.Push(p);
                    }
                    if (!(N.IsFinalState(p)) && D[p] != d)
                        throw new BekException(string.Format("Unexpected error, inconsitent distances {0} and {1} to state {2}", D[p], d, p));
                }
            }

            #endregion

            #region check that outputs do not have out of bound variables
            foreach (var fs in N.GetFinalStates())
            {
                foreach (var move in N.GetMovesTo(fs))
                {
                    if (move.Label.Item2.IsEmpty)
                        throw new BekException("Internal error: missing end anchor");

                    //if (!css.IsSingleton(move.Condition.Second))
                    //{
                    //    var one = (int)css.GetMin(move.Condition.Second);
                    //    var two = (int)css.GetMax(move.Condition.Second);
                    //    throw new BekParseException(repl.GetCase(two).id.line, repl.GetCase(two).id.pos, string.Format("Ambiguous replacement patterns {0} and {1}.", repl.GetCase(one).Pattern, repl.GetCase(two).Pattern));
                    //}

                    //pick the minimum case identifer when there are several, essentially pick the earliest case
                    int id = (int)css.GetMin(move.Label.Item2); 

                    int distFromRoot = D[move.SourceState];
                    var e = repl.GetCase(id).Output;
                    HashSet<int> vars = new HashSet<int>();
                    foreach (var v in e.GetBoundVars())
                        if (v.GetVarId() >= distFromRoot)
                            throw new BekParseException(v.line, v.pos, string.Format("Syntax error: pattern variable '{0}' is out ouf bounds, valid range is from '#0' to '#{1}']", v.name, distFromRoot-1));           
                }

            }
            #endregion

            int finalState = N.FinalState;
            K = K - 1; //this many registers are needed

            var zeroChar = stb.Solver.MkCharExpr('\0');
            var STmoves = new List<Move<Rule<Expr>>>();
            var STstates = new HashSet<int>();
            var STdelta = new Dictionary<int, List<Move<Rule<Expr>>>>();
            var STdeltaInv = new Dictionary<int, List<Move<Rule<Expr>>>>();
            var FinalSTstates = new HashSet<int>();
            var STdeletedMoves = new HashSet<Move<Rule<Expr>>>();
            Action<Move<Rule<Expr>>> STmovesAdd = r =>
               {
                   var p = r.SourceState;
                   var q = r.TargetState;
                   STmoves.Add(r);
                   if (STstates.Add(p))
                   {
                       STdelta[p] = new List<Move<Rule<Expr>>>();
                       STdeltaInv[p] = new List<Move<Rule<Expr>>>();
                   }
                   if (STstates.Add(q))
                   {
                       STdelta[q] = new List<Move<Rule<Expr>>>();
                       STdeltaInv[q] = new List<Move<Rule<Expr>>>();
                   }
                   if (r.Label.IsFinal)
                       FinalSTstates.Add(p);
                   STdelta[p].Add(r);
                   STdeltaInv[q].Add(r);
               };
            var regsorts = new Sort[K];
            for (int j = 0; j < K; j++)
                regsorts[j] = stb.Solver.CharSort;
            var regsort = stb.Solver.MkTupleSort(regsorts);
            var regvar = stb.MkRegister(regsort);
            var initialRegisterValues = new Expr[K];
            for (int j = 0; j < K; j++)
                initialRegisterValues[j] = zeroChar;
            var initialRegister = stb.Solver.MkTuple(initialRegisterValues);

            Predicate<int> IsCaseEndState = s => { return N.OutDegree(s) == 1 && N.GetMoveFrom(s).Label.Item1.IsEmpty; };

            #region compute the forward moves and the completion moves
            var V = new HashSet<int>();
            S.Push(N.InitialState);
            while (S.Count > 0)
            {
                var p = S.Pop();

                #region forward moves
                foreach (var move in N.GetMovesFrom(p))
                {
                    var q = move.TargetState;
                    //this move occurs if p has both an end-move and a non-end-move 
                    //note that if p is an case-end-state then it is never pushed to S
                    if (N.IsFinalState(q)) 
                        continue;

                    var distance = D[p];
                    Expr chExpr;
                    Expr chPred;
                    MkExprPred(move.Label.Item1, out chExpr, out chPred);
                    predLookup[chPred] = move.Label.Item1;

                    Expr[] regUpds = new Expr[K];
                    for (int i=0; i < K; i++)
                    {
                        if (i == distance)
                            regUpds[i] = chExpr;
                        else //if (i < distance)
                            regUpds[i] = stb.Solver.MkProj(i,regvar);
                        //else
                        //    regUpds[i] = zeroChar;
                    }
                    Expr regExpr = stb.Solver.MkTuple(regUpds);
                    var moveST = stb.MkRule(p, q, chPred, regExpr); //there are no yields
                    STmovesAdd(moveST);

                    if (V.Add(q) && !IsCaseEndState(q))
                        S.Push(q);
                }
                #endregion

                
                #region completion is only enabled if there exists an else case
                if (repl.HasElseCase)
                {
                    var guards = G[p];
                    var guards0 = G[N.InitialState];

                    #region nonmatching cases to the initial state
                    var nomatch = css.MkNot(css.MkOr(guards, guards0));

                    if (!nomatch.IsEmpty)
                    {
                        Expr chExpr;
                        Expr nomatchPred;
                        MkExprPred(nomatch, out chExpr, out nomatchPred);
                        predLookup[nomatchPred] = nomatch;

                        var else_yields_list = new List<Expr>();
                        for (int i = 0; i < D[p]; i++)
                            else_yields_list.AddRange(GetElseYieldInstance(repl.ElseOutput, stb.Solver.MkProj(i, regvar)));
                        else_yields_list.AddRange(GetElseYieldInstance(repl.ElseOutput, stb.MkInputVariable(stb.Solver.CharSort)));

                        var else_yields = else_yields_list.ToArray();
                        var resetMove = stb.MkRule(p, N.InitialState, nomatchPred, initialRegister, else_yields);
                        STmovesAdd(resetMove);
                    }
                    #endregion

                    #region matching cases via the initial state
                    foreach (var move0 in N.GetMovesFrom(N.InitialState))
                    {
                        var g0 = move0.Label.Item1;
                        var match = css.MkAnd(css.MkNot(guards), g0);
                        if (!match.IsEmpty)
                        {
                            Expr chExpr;
                            Expr matchPred;
                            MkExprPred(match, out chExpr, out matchPred);
                            predLookup[matchPred] = match;


                            var resetYieldsList = new List<Expr>();
                            //for all unprocessed inputs produce the output yield according to the else case
                            for (int i = 0; i < D[p]; i++)
                                resetYieldsList.AddRange(GetElseYieldInstance(repl.ElseOutput, stb.Solver.MkProj(i, regvar)));
                            var resetYields = resetYieldsList.ToArray();

                            Expr[] regupd = new Expr[K];
                            regupd[0] = chExpr;
                            for (int j = 1; j < K; j++)
                            {
                                regupd[j] = zeroChar;
                            }
                            var regupdExpr = stb.Solver.MkTuple(regupd);
                            var resetMove = stb.MkRule(p, move0.TargetState, matchPred, regupdExpr, resetYields);
                            STmovesAdd(resetMove);
                        }

                    }
                    #endregion
                }
                #endregion
            }

            #endregion

            foreach (var last_move in N.GetMovesTo(N.FinalState))
            {
                //i is the case identifier
                int i = (int)css.GetMin(last_move.Label.Item2);

                if (hasNoEndAnchor.Contains(i))
                {
                    #region this corresponds to looping back to the initial state on the given input
                    //the final outputs produced after a successful pattern match

                    #region compute the output terms

                    int distFromRoot = D[last_move.SourceState];
                    Func<ident, Expr> registerMap = id =>
                    {
                        // --- already checked I think ---
                        if (!id.IsVar || id.GetVarId() >= distFromRoot)
                            throw new BekParseException(id.Line, id.Pos, string.Format("illeagal variable '{0}' in output", id.name));

                        if (id.GetVarId() == distFromRoot - 1) //the last reg update refers to the current variable
                            return stb.MkInputVariable(stb.Solver.CharSort);
                        else
                            return stb.Solver.MkProj(id.GetVarId(), regvar);
                    };
                    Expr[] yields;
                    var outp = repl.GetCase(i).Output;
                    if (outp is strconst)
                    {
                        var s = ((strconst)outp).val;
                        yields = Array.ConvertAll(s.ToCharArray(),
                            c => this.str_handler.iter_handler.expr_handler.Convert(new charconst("'" + StringUtility.Escape(c) + "'"), registerMap));
                    }
                    else //must be an explicit list construct
                    {
                        if (!(outp is functioncall) || !((functioncall)outp).id.name.Equals("string"))
                            throw new BekParseException("Invalid pattern output.");

                        var s = ((functioncall)outp).args;
                        yields = Array.ConvertAll(s.ToArray(),
                            e => this.str_handler.iter_handler.expr_handler.Convert(e, registerMap));
                    }
                    #endregion

                    //shortcut all the incoming transitions to the initial state
                    foreach (var move in STdeltaInv[last_move.SourceState])
                    {
                        //go to the initial state, i.e. the matching raps around
                        int p = move.SourceState;
                        int q0 = N.InitialState;
                        List<Expr> yields1 = new List<Expr>(move.Label.Yields); //incoming yields are 
                        yields1.AddRange(yields);
                        var rule = stb.MkRule(p, q0, move.Label.Guard, initialRegister, yields1.ToArray());
                        STmovesAdd(rule);
                        //STdeletedMoves.Add(move);
                        STmoves.Remove(move); //the move has been replaced
                    }
                    #endregion
                }
                else
                {
                    #region this is the end of the input stream case

                    #region compute the output terms

                    int distFromRoot = D[last_move.SourceState];
                    Func<ident, Expr> registerMap = id =>
                    {
                        if (!id.IsVar || id.GetVarId() >= distFromRoot)
                            throw new BekParseException(id.Line, id.Pos, string.Format("illeagal variable '{0}' in output", id.name));

                        return stb.Solver.MkProj(id.GetVarId(), regvar);
                    };
                    Expr[] yields;
                    var outp = repl.GetCase(i).Output;
                    if (outp is strconst)
                    {
                        var s = ((strconst)outp).val;
                        yields = Array.ConvertAll(s.ToCharArray(),
                            c => this.str_handler.iter_handler.expr_handler.Convert(new charconst("'" + c.ToString() + "'"), registerMap));
                    }
                    else //must be an explicit list construct
                    {
                        if (!(outp is functioncall) || !((functioncall)outp).id.name.Equals("string"))
                            throw new BekParseException("Invalid pattern output.");

                        var s = ((functioncall)outp).args;
                        yields = Array.ConvertAll(s.ToArray(),
                            e => this.str_handler.iter_handler.expr_handler.Convert(e, registerMap));
                    }
                    #endregion

                    int p = last_move.SourceState;
                    var rule = stb.MkFinalOutput(p, stb.Solver.True, yields);
                    STmovesAdd(rule);
                    #endregion
                }
            }

            if (repl.HasElseCase)
            {
                #region final completion (upon end of input) for all non-final states
                foreach (var p in STstates)
                {
                    if (!FinalSTstates.Contains(p) && !IsCaseEndState(p)) //there is no final rule for p, so add the default one
                    {
                        Expr[] finalYields;
                        finalYields = new Expr[D[p]];
                        for (int i = 0; i < finalYields.Length; i++)
                            finalYields[i] = stb.Solver.MkProj(i, regvar);
                        var p_finalMove = stb.MkFinalOutput(p, stb.Solver.True, finalYields);
                        STmovesAdd(p_finalMove);
                    }
                }
                #endregion
            }
            else
            {
                //in this case there is a final rule from the initial state
                var q0_finalMove = stb.MkFinalOutput(N.InitialState, stb.Solver.True);
                STmovesAdd(q0_finalMove);
            }

            var resST = stb.MkST(name, initialRegister, stb.Solver.CharSort, stb.Solver.CharSort, regsort, N.InitialState, STmoves);
            var resSTb = new STModel(stb.Solver, name, stb.Solver.CharSort, stb.Solver.CharSort, regsort, initialRegister, N.InitialState);

            //create STb from the moves, we use here the knowledge that the ST is deterministic
            //we also use the lookuptable of conditions to eliminate dead code
            
            
            //resST.ShowGraph();

            //resST.ToDot("C:\\Automata\\Docs\\Papers\\Bex\\B.dot");

            #region compute the rules of the resulting STb

            //V.Clear();
            //S.Push(resST.InitialState);
            //V.Add(resST.InitialState);

            foreach (var st in resST.GetStates())
            {
                var condUnion = css.False;
                var st_moves = new List<Move<Rule<Expr>>>();
                foreach (var move in resST.GetNonFinalMovesFrom(st))
                {
                    condUnion = css.MkOr(condUnion, predLookup[move.Label.Guard]);
                    st_moves.Add(move);
                }

                BranchingRule<Expr> st_rule;
                if (st_moves.Count > 0)
                {
                    //collect all rules with singleton guards and put them into a switch statement
                    var st_rules1 = new List<KeyValuePair<Expr, BranchingRule<Expr>>>();
                    var st_moves2 = new List<Move<Rule<Expr>>>();
                    foreach (var move in st_moves)
                    {
                        if (css.ComputeDomainSize(predLookup[move.Label.Guard]) == 1)
                        {
                            var v = stb.Solver.MkNumeral(css.Choose(predLookup[move.Label.Guard]), stb.Solver.CharSort);
                            var r = new BaseRule<Expr>(new Sequence<Expr>(move.Label.Yields),
                                                       move.Label.Update, move.TargetState);
                            st_rules1.Add(new KeyValuePair<Expr, BranchingRule<Expr>>(v, r));
                        }
                        else
                            st_moves2.Add(move);
                    }
                    BranchingRule<Expr> defaultcase = new UndefRule<Expr>("reject");
                    //make st_moves2 into an ite rule
                    if (st_moves2.Count > 0)
                    {
                        for (int j = st_moves2.Count - 1; j >= 0; j--)
                        {
                            var r = new BaseRule<Expr>(new Sequence<Expr>(st_moves2[j].Label.Yields),
                                                       st_moves2[j].Label.Update, st_moves2[j].TargetState);
                            if (j == (st_moves2.Count - 1) && condUnion.IsFull)
                                defaultcase = r;
                            else
                                defaultcase = new IteRule<Expr>(st_moves2[j].Label.Guard, r, defaultcase);
                        }
                    }
                    else if (condUnion.IsFull)
                    {
                        defaultcase = st_rules1[st_rules1.Count - 1].Value;
                        st_rules1.RemoveAt(st_rules1.Count - 1);
                    }
                    if (st_rules1.Count == 0)
                        st_rule = defaultcase;
                    else
                        st_rule = new SwitchRule<Expr>(stb.MkInputVariable(stb.Solver.CharSort), defaultcase, st_rules1.ToArray());
                }
                else
                {
                    st_rule = new UndefRule<Expr>("reject");
                }

                resSTb.AssignRule(st, st_rule);

                var st_finalrules = new List<Rule<Expr>>(resST.GetFinalRules(st));
                if (st_finalrules.Count > 1)    
                    throw new BekException("Unexpected error: multiple final rules per state.");
                if (st_finalrules.Count > 0)
                    resSTb.AssignFinalRule(st, new BaseRule<Expr>(new Sequence<Expr>(st_finalrules[0].Yields), initialRegister, st));
            }

            resSTb.ST = resST;
            resST.STb = resSTb;
            #endregion

            return resSTb;
        }

        private Expr[] GetElseYieldInstance(expr else_outp, Expr input_term)
        {
            Expr[] else_yields;
            if (else_outp is strconst)
            {
                var s = ((strconst)else_outp).val;
                else_yields = Array.ConvertAll(s.ToCharArray(),
                    c => this.str_handler.iter_handler.expr_handler.Convert(new charconst("'" + c.ToString() + "'"), x => null));
            }
            else //must be an explicit list construct
            {
                Func<ident, Expr> varMap = id =>
                {
                    if (!id.IsVar || id.GetVarId() != 0)
                        throw new BekParseException(id.Line, id.Pos, string.Format("illeagal variable '{0}' in else-output, only '#0' may be used", id.name));
                    return input_term;
                };
                if (!(else_outp is functioncall) || !((functioncall)else_outp).id.name.Equals("string"))
                    throw new BekParseException(else_outp.Line, else_outp.Pos, string.Format("Unexpected else-output '{0}'.", else_outp));

                var s = ((functioncall)else_outp).args;
                else_yields = Array.ConvertAll(s.ToArray(),
                    e => this.str_handler.iter_handler.expr_handler.Convert(e, varMap));
            }
            return else_yields;
        }

        private void MkExprPred(BDD moveCond, out Expr chExpr, out Expr chPred)
        {


            if (css.ComputeDomainSize(moveCond) == 1)
            {
                var ch = (char)css.Choose(moveCond);
                chExpr = stb.Solver.MkCharExpr(ch);
                chPred = stb.Solver.MkCharConstraint( ch);
            }
            else if (css.ComputeDomainSize(css.MkNot(moveCond)) == 1)
            {
                var ch = (char)css.Choose(css.MkNot(moveCond));
                chExpr = stb.MkInputVariable(stb.Solver.CharSort);
                chPred = stb.Solver.MkNot(stb.Solver.MkCharConstraint( ch));
            }
            else if (css.ComputeDomainSize(moveCond) == 2)
            {
                var ch1 = (char)css.GetMin(moveCond);
                var ch2 = css.GetMax(moveCond);
                chExpr = stb.MkInputVariable(stb.Solver.CharSort);
                chPred = stb.Solver.MkOr(stb.Solver.MkCharConstraint( ch1), stb.Solver.MkCharConstraint( ch2));
            }
            else if (css.ComputeDomainSize(css.MkNot(moveCond)) == 2)
            {
                var ch1 = (char)css.GetMin(css.MkNot(moveCond));
                var ch2 = css.GetMax(css.MkNot(moveCond));
                chExpr = stb.MkInputVariable(stb.Solver.CharSort);
                chPred = stb.Solver.MkAnd(stb.Solver.MkNot(stb.Solver.MkCharConstraint( ch1)), 
                                          stb.Solver.MkNot(stb.Solver.MkCharConstraint( ch2)));
            }
            else
            {
                var ranges = css.ToRanges(moveCond);
                chPred = stb.Solver.MkRangesConstraint(false, Array.ConvertAll(ranges, r => new char[] { (char)r.Item1, (char)r.Item2 }));
                chExpr = stb.MkInputVariable(stb.Solver.CharSort);
            }
        }
    }
}
