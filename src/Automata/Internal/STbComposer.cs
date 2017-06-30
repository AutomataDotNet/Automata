using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Microsoft.Automata
{
    internal class STbComposer<FUNC, TERM, SORT>
    {
        STb<FUNC, TERM, SORT> A;
        STb<FUNC, TERM, SORT> B;
        IContext<FUNC, TERM, SORT> solver;
        SORT regSort;
        TERM regVar;
        TERM regVar_1;
        TERM regVar_2;

        TERM Subst(TERM e, params TERM[] subst)
        {
            var theta = new Dictionary<TERM, TERM>();
            for (int i=0; i< subst.Length; i+=2)
            {
                theta[subst[i]] = subst[i+1];
            }
            var res = solver.ApplySubstitution(e, theta);
            return res;
        }

        TERM Simpl(TERM e)
        {
            return solver.Simplify(e);
        }

        Func<TERM, TERM, TERM> JoinRegs;
        Func<TERM, TERM> ProjA;
        Func<TERM, TERM> ProjB;

        TERM MkPair(TERM t1, TERM t2)
        {
            if (t1.Equals(regVar_1) && t2.Equals(regVar_2))
                return regVar;
            else
                return solver.MkTuple(t1, t2);
        }

        internal STbComposer(STb<FUNC, TERM, SORT> A, STb<FUNC, TERM, SORT> B)
        {
            if (A.Solver != B.Solver)
                throw new AutomataException(AutomataExceptionKind.SolversAreNotIdentical);
            if (!A.OutputSort.Equals(B.InputSort))
                throw new AutomataException(AutomataExceptionKind.SortMismatch);
            this.A = A;
            this.B = B;
            this.solver = A.Solver;
            this.regSort = (solver.UnitSort.Equals(A.RegisterSort) ? B.RegisterSort :
                            (solver.UnitSort.Equals(B.RegisterSort) ? A.RegisterSort :
                             solver.MkTupleSort(A.RegisterSort, B.RegisterSort)));
            if (solver.UnitSort.Equals(A.RegisterSort))
            {
                JoinRegs = ((a, b) => b);
                ProjA = ((b) => solver.UnitConst);
                ProjB = ((b) => b);
            }
            else if (solver.UnitSort.Equals(B.RegisterSort))
            {
                JoinRegs = (a, b) => a;
                ProjA = ((a) => a);
                ProjB = ((a) => solver.UnitConst);
            }
            else
            {
                JoinRegs = (a, b) => solver.MkTuple(a, b);
                ProjA = ((ab) => solver.MkProj(0,ab));
                ProjB = ((ab) => solver.MkProj(1,ab));
            }

            this.regVar = solver.MkVar(1, regSort);
            this.regVar_1 = ProjA(regVar);
            this.regVar_2 = ProjB(regVar);
        }

        public STb<FUNC, TERM, SORT> Compose()
        {

            var stack = new SimpleStack<Tuple<int, int>>();
            int stateId = 1;
            var stateIdMap = new Dictionary<Tuple<int, int>, int>();
            var revStateIdMap = new Dictionary<int, Tuple<int, int>>();
            var q0A_x_q0B = new Tuple<int, int>(A.InitialState, B.InitialState);
            stack.Push(q0A_x_q0B);
            stateIdMap[q0A_x_q0B] = 0;
            revStateIdMap[0] = q0A_x_q0B;

            Func<int, int, int> ComposeStates = (x, y) =>
            {
                var xy = new Tuple<int, int>(x, y);
                int q;
                if (stateIdMap.TryGetValue(xy, out q))
                    return q;
                else
                {
                    q = stateId;
                    stateId += 1;
                    stateIdMap[xy] = q;
                    revStateIdMap[q] = xy;
                    stack.Push(xy);
                    return q;
                }
            };
            var A2B = new STb<FUNC, TERM, SORT>(solver, A.Name + "2" + B.Name, A.InputSort, B.OutputSort, regSort, JoinRegs(A.InitialRegister, B.InitialRegister), 0);

            //create normal composed rules
            while (stack.IsNonempty)
            {
                var qA_x_qB = stack.Pop();
                var qA = qA_x_qB.Item1;
                var qB = qA_x_qB.Item2;
                var ruleA = A.GetRuleFrom(qA);
                if (ruleA.IsNotUndef)
                {
                    var qAB_rule = Comp(solver.True, ruleA, qB, ComposeStates, false);
                    A2B.AssignRule(stateIdMap[qA_x_qB], qAB_rule);
                }
                else
                {
                    A2B.AssignRule(stateIdMap[qA_x_qB], UndefRule<TERM>.Default);
                }
            }
            foreach (var qAB in A2B.States)
            {
                var qA_x_qB = revStateIdMap[qAB];
                var qA = qA_x_qB.Item1;
                var qB = qA_x_qB.Item2;
                var ruleA = A.GetFinalRuleFrom(qA);
                if (ruleA.IsNotUndef)
                {
                    var qAB_Frule = Comp(solver.True, ruleA, qB, (p, q) => qAB, true);
                    A2B.AssignFinalRule(qAB, qAB_Frule);
                }
            }

            A2B.EliminateDeadends();


            //Func<Rule<TERM>, Rule<TERM>> ReplaceWithEpsilon = (r) =>
            //    {
                    
            //    };

            //var aut = A2B.ToST(true).automaton.RelpaceAllGuards(ReplaceWithEpsilon);


            return A2B;
        }


        BranchingRule<TERM> Comp(TERM pathCond, BranchingRule<TERM> ruleA, int qB, Func<int, int, int> stateComposer, bool isFinal)
        {
            if (ruleA is SwitchRule<TERM>)
                throw new NotImplementedException(ruleA.ToString());

            var ite = ruleA as IteRule<TERM>;
            if (ite != null)
            {
                var iteCondLifted = solver.ApplySubstitution(ite.Condition, A.RegVar, regVar_1);
                var pathCond_T = (pathCond.Equals(solver.True) ? iteCondLifted : solver.MkAnd(pathCond, iteCondLifted));
                var pathCond_F = (pathCond.Equals(solver.True) ? solver.MkNot(iteCondLifted) : solver.MkAnd(pathCond, solver.MkNot(iteCondLifted)));
                var res_T = Comp(pathCond_T, ite.TrueCase, qB, stateComposer, isFinal);
                var res_F = Comp(pathCond_F, ite.FalseCase, qB, stateComposer, isFinal);
                if (res_T is UndefRule<TERM> && res_F is UndefRule<TERM>)
                    return UndefRule<TERM>.Default;
                var res = new IteRule<TERM>(iteCondLifted, res_T, res_F);
                return res;
            }
            else
            {
                var br = ruleA as BaseRule<TERM>;
                if (br != null)
                {
                    var yieldsFromALifted = br.Yields.ConvertAll(t => solver.ApplySubstitution(t, A.RegVar, regVar_1));
                    var inputsToB = ConsList<TERM>.Create(yieldsFromALifted);
                    var regALifted = solver.ApplySubstitution(br.Register, A.RegVar, regVar_1);
                    var res = this.EvalB(inputsToB, br.State, regALifted, pathCond, qB, regVar_2, stateComposer, Sequence<TERM>.Empty, isFinal);
                    return res;
                }
                else
                {
                    return UndefRule<TERM>.Default;
                }
            }
        }

        private BranchingRule<TERM> EvalB(ConsList<TERM> inputs, int qA, TERM regA, TERM pathCond, int qB, TERM regB, Func<int, int, int> stateComposer, Sequence<TERM> outputFromB, bool isFinal)
        {
            if (inputs == null)
            {
                int qAB = stateComposer(qA, qB);
                if (isFinal)
                {
                    var fr = B.GetFinalRuleFrom(qB);
                    if (fr.IsNotUndef)
                        return this.CreateFinalComposedRule(pathCond, qAB, outputFromB, fr, regB);
                    else
                        return UndefRule<TERM>.Default;
                }
                else
                {
                    TERM regAB = (regA.Equals(regVar_1) && regB.Equals(regVar_2) ? regVar : JoinRegs(regA, regB));
                    var res = new BaseRule<TERM>(outputFromB, regAB, qAB);
                    return res;
                }
            }
            else
            {
                return EvalB_Rule(B.GetRuleFrom(qB), qA, regA, pathCond, inputs, regB, stateComposer, outputFromB, isFinal);
            }
        }

        private BranchingRule<TERM> CreateFinalComposedRule(TERM pathCond, int qAB, Sequence<TERM> outputFromB, BranchingRule<TERM> ruleB, TERM regB)
        {
            if (ruleB is SwitchRule<TERM>)
                throw new NotImplementedException(ruleB.ToString());

            var br = ruleB as BaseRule<TERM>;
            if (br != null)
            {
                var yields = br.Yields.ConvertAll(z => Simpl(Subst(z, B.RegVar, regB)));
                var outp = outputFromB.Append(yields);
                var res = new BaseRule<TERM>(outp, regVar, qAB);
                return res;
            }
            else
            {
                var ite = ruleB as IteRule<TERM>;
                if (ite != null)
                {
                    var condLifted = Subst(ite.Condition, B.RegVar, regB);
                    var path_and_condLifted = solver.MkAnd(pathCond, condLifted);
                    if (!solver.IsSatisfiable(path_and_condLifted))
                    {
                        //path ==> !condLifted, the true-branch is unreachable
                        var res = CreateFinalComposedRule(pathCond, qAB, outputFromB, ite.FalseCase, regB);
                        return res;
                    }
                    else
                    {
                        var path_and_not_condLifted = solver.MkAnd(pathCond, solver.MkNot(condLifted));
                        if (!solver.IsSatisfiable(path_and_not_condLifted))
                        {
                            //path ==> condLifted, the false-branch is unreachable
                            var res = CreateFinalComposedRule(pathCond, qAB, outputFromB, ite.TrueCase, regB);
                            return res;
                        }
                        else
                        {
                            var f = CreateFinalComposedRule(path_and_not_condLifted, qAB, outputFromB, ite.FalseCase, regB);
                            var t = CreateFinalComposedRule(path_and_condLifted, qAB, outputFromB, ite.TrueCase, regB);
                            if (t is UndefRule<TERM> && f is UndefRule<TERM>)
                                return UndefRule<TERM>.Default;

                            return new IteRule<TERM>(condLifted, t, f);
                        }
                    }
                }
                else
                    return UndefRule<TERM>.Default;
            }
        }

        private BranchingRule<TERM> EvalB_Rule(BranchingRule<TERM> ruleB, int qA, TERM regA, TERM pathCond, ConsList<TERM> inputs, TERM regB, Func<int, int, int> stateComposer, Sequence<TERM> outputFromB, bool isFinal)
        {
            if (ruleB == null)
                return UndefRule<TERM>.Default;

            if (ruleB is SwitchRule<TERM>)
            {
                var sw = ruleB as SwitchRule<TERM>;
                var ite = sw.ToIte(solver.MkEq);
                return EvalB_Rule(ite, qA, regA, pathCond, inputs, regB, stateComposer, outputFromB, isFinal);
            }

            var br = ruleB as BaseRule<TERM>;
            if (br != null)
            {
                var yields = br.Yields.ConvertAll(y => Simpl(Subst(y, B.InputVar, inputs.First, B.RegVar, regB)));
                var reg = Simpl(Subst(br.Register, B.InputVar, inputs.First, B.RegVar, regB));
                var outp = outputFromB.Append(yields);
                var res = EvalB(inputs.Rest, qA, regA, pathCond, br.State, reg, stateComposer, outp, isFinal);
                return res;
            }
            else
            {
                var ite = ruleB as IteRule<TERM>;
                if (ite != null)
                {
                    var condLifted = Subst(ite.Condition, B.InputVar, inputs.First, B.RegVar, regB);
                    var path_and_condLifted = solver.MkAnd(pathCond, condLifted);
                    if (!solver.IsSatisfiable(path_and_condLifted))
                    {
                        //path ==> !condLifted, the true-branch is unreachable
                        var res = EvalB_Rule(ite.FalseCase, qA, regA, pathCond, inputs, regB, stateComposer, outputFromB, isFinal);
                        return res;
                    }
                    else
                    {
                        var path_and_not_condLifted = solver.MkAnd(pathCond, solver.MkNot(condLifted));
                        if (!solver.IsSatisfiable(path_and_not_condLifted))
                        {
                            //path ==> condLifted, the false-branch is unreachable
                            var res = EvalB_Rule(ite.TrueCase, qA, regA, pathCond, inputs, regB, stateComposer, outputFromB, isFinal);
                            return res;
                        }
                        else
                        {
                            var t = EvalB_Rule(ite.TrueCase, qA, regA, path_and_condLifted, inputs, regB, stateComposer, outputFromB, isFinal);
                            var f = EvalB_Rule(ite.FalseCase, qA, regA, path_and_not_condLifted, inputs, regB, stateComposer, outputFromB, isFinal);
                            if (t is UndefRule<TERM> && f is UndefRule<TERM>)
                                return UndefRule<TERM>.Default;

                            return new IteRule<TERM>(condLifted, t, f);
                        }
                    }
                }
                else
                {
                    return UndefRule<TERM>.Default;
                }
            }
        }
    }

}
