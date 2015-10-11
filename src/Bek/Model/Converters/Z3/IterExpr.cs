using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Automata.Z3;
using Microsoft.Z3;
using Microsoft.Bek.Frontend.AST;
using Microsoft.Bek.Frontend;
using Microsoft.Automata;
using Microsoft.Bek.Frontend.Meta;
using Microsoft.Automata.Z3.Internal;

using STModel = Microsoft.Automata.ST<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;
using Rulez3 = Microsoft.Automata.Rule<Microsoft.Z3.Expr>;
using STBuilderZ3 = Microsoft.Automata.STBuilder<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;


namespace Microsoft.Bek.Model.Converters.Z3
{
    internal class IterExpr : AIterExpr<STModel, Expr>
    {
        internal STBuilderZ3 stb;
        internal Sort charsort;

        public IterExpr(ValueExpr expr_handler, STBuilderZ3 stb, Sort charsort) : base(expr_handler) {
            this.stb = stb;
            this.charsort = charsort;
        }

        private class boolstate : HashSet<int> {
            public boolstate() : base() { }
            public boolstate(boolstate o) : base(o as IEnumerable<int>) { }
        };

        Sort BekTypeToSort(BekTypes btype)
        {
            switch (btype)
            {
                case BekTypes.BOOL:
                    return stb.Solver.BoolSort;
                case BekTypes.CHAR:
                    return charsort;
                case BekTypes.STR:
                    return stb.Solver.MkListSort(charsort);
                default:
                    throw new BekException(); //TBD: introduce exception kinds
            }
        }

        Expr MkExpr(BekTypes btype, expr e)
        {
            switch (btype)
            {
                case BekTypes.BOOL:
                    return (((boolconst)e).val ? stb.Solver.True : stb.Solver.False);
                case BekTypes.CHAR:
                    return stb.Solver.MkNumeral(((charconst)e).val, charsort);
                case BekTypes.STR:
                    return stb.Solver.MkListFromString(((strconst)e).val, charsort);
                default:
                    throw new BekException(); //TBD: introduce exception kinds
            }
        }

        public override STModel Convert(iterexpr ie, Symtab stab)
        {
            var solver = stb.Solver;
            int binderid = stab.Get(ie.binder).id;

            List<int> bekVarIds = new List<int>();
            List<Expr> bekVarVals = new List<Expr>();
            List<Sort> bekVarSorts = new List<Sort>();
            Dictionary<int, int> proj = new Dictionary<int, int>();

            foreach (iterassgn ia in IterInfo.Initializers(ie, stab))
            {
                proj[stab.Get(ia.lhs).id] = bekVarIds.Count;
                bekVarIds.Add(stab.Get(ia.lhs).id);
                bekVarVals.Add(MkExpr(stab.Get(ia.lhs).type, ia.rhs));
                bekVarSorts.Add(BekTypeToSort(stab.Get(ia.lhs).type));
            }

            int K = bekVarSorts.Count;

            //register sort
            Sort regSort = (K == 0 ? solver.UnitSort :
                (K == 1 ? bekVarSorts[0] : solver.MkTupleSort(bekVarSorts.ToArray())));

            //initial register value
            Expr initReg = (K == 0 ? solver.UnitConst :
                (K == 1 ? bekVarVals[0] : solver.MkTuple(bekVarVals.ToArray())));

            //input character variable
            Expr c = this.stb.MkInputVariable(charsort);

            //register variable
            Expr r = this.stb.MkRegister(regSort);

            //maps variable identifiers used in the bek program to corresponding term variables
            Dictionary<int, Expr> varMap = new Dictionary<int, Expr>();
            varMap[binderid] = c;
            for (int i = 0; i < K; i++)
                varMap[bekVarIds[i]] = (K == 1 ? r : solver.MkProj(i, r));

            List<Move<Rulez3>> moves = new List<Move<Rulez3>>();

            Expr previousCaseNegated = solver.True;

            foreach (itercase curcase in ie.GetNormalCases())
            {
                if (!solver.IsSatisfiable(previousCaseNegated))
                    break;

                //initial symbolic values are the previous register values
                Expr[] regs0 = new Expr[K];
                for (int i = 0; i < K; i++)
                    regs0[i] = varMap[bekVarIds[i]];

                Expr[] regs = new Expr[K];
                for (int i = 0; i < K; i++)
                    regs[i] = regs0[i];


                //gets the current symbolic value of ident
                Func<ident, Expr> idents = x =>
                {
                    SymtabElt se = stab.Get(x);
                    if (se.id == binderid)
                        return c;             //the input character
                    return regs0[proj[se.id]]; //the current sybolic value of x
                };

                //current condition is the case condition and not the previous case conditions
                Expr casecond = this.expr_handler.Convert(curcase.cond, idents);
                Expr guard = stb.And(previousCaseNegated, casecond);
                Expr not_casecond = stb.Not(casecond);
                previousCaseNegated = stb.And(previousCaseNegated, solver.MkNot(casecond));
                List<Expr> yields = new List<Expr>();
                if (solver.IsSatisfiable(guard))
                {
                    #region iterate over the iter statements in the body
                    foreach (iterstmt ist in curcase.body)
                    {
                        //gets the current symbolic value of ident
                        //note that the symbolic value may have been updated by a previous assignment
                        Func<ident, Expr> idents1 = x =>
                        {
                            SymtabElt se = stab.Get(x);
                            if (se.id == binderid)
                                return c;             //the input character
                            return regs0[proj[se.id]]; //the current sybolic value of x
                        };

                        iterassgn a = ist as iterassgn;
                        if (a != null)
                        {
                            var v = expr_handler.Convert(a.rhs, idents1);
                            regs[proj[stab.Get(a.lhs).id]] = v;
                        }
                        else
                        {
                            yieldstmt y = ist as yieldstmt;
                            if (y != null)
                            {
                                foreach (var e in y.args)
                                {
                                    strconst s = e as strconst;
                                    if (s == null)
                                    {
                                        yields.Add(expr_handler.Convert(e, idents1));
                                    }
                                    else
                                    {
                                        foreach (int sc in s.content)
                                            yields.Add(solver.MkNumeral(sc, charsort));
                                    }
                                }
                            }
                            else
                                throw new BekException(); //TBD: undefined case
                        }
                    }
                    #endregion
                    Expr upd = (K == 0 ? solver.UnitConst : (K == 1 ? regs[0] : solver.MkTuple(regs)));
                    moves.Add(stb.MkRule(0, 0, guard, upd, yields.ToArray()));
                }
            }

            previousCaseNegated = solver.True;
            bool noEndCases = true;

            foreach (itercase curcase in ie.GetEndCases())
            {
                noEndCases = false;

                if (!solver.IsSatisfiable(previousCaseNegated))
                    break;

                //initial symbolic values are the previous register values
                Expr[] regs = new Expr[K];
                for (int i = 0; i < K; i++)
                    regs[i] = varMap[bekVarIds[i]];

                //gets the current symbolic value of ident
                Func<ident, Expr> idents = x =>
                {
                    SymtabElt se = stab.Get(x);
                    if (se.id == binderid)
                        throw new BekException("Input var must not occur in an end case");
                    return regs[proj[se.id]]; //the current sybolic value of x
                };

                //current condition is the case condition and not the previous case conditions
                Expr casecond = this.expr_handler.Convert(curcase.cond, idents);
                Expr guard = stb.And(previousCaseNegated, casecond);
                Expr not_casecond = stb.Not(casecond);
                previousCaseNegated = stb.And(previousCaseNegated, solver.MkNot(casecond));
                List<Expr> yields = new List<Expr>();
                if (solver.IsSatisfiable(guard))
                {
                    #region iterate over the iter statements in the body
                    foreach (iterstmt ist in curcase.body)
                    {
                        //gets the current symbolic value of ident
                        //note that the symbolic value may have been updated by a previous assignment
                        Func<ident, Expr> idents1 = x =>
                        {
                            SymtabElt se = stab.Get(x);
                            if (se.id == binderid)
                                throw new BekException("Input var must not occur in an end case");
                            return regs[proj[se.id]]; //the current sybolic value of x
                        };

                        iterassgn a = ist as iterassgn;
                        if (a != null)
                        {
                            var v = expr_handler.Convert(a.rhs, idents1);
                            regs[proj[stab.Get(a.lhs).id]] = v;
                        }
                        else
                        {
                            yieldstmt y = ist as yieldstmt;
                            if (y != null)
                            {
                                foreach (var e in y.args)
                                {
                                    strconst s = e as strconst;
                                    if (s == null)
                                    {
                                        yields.Add(expr_handler.Convert(e, idents1));
                                    }
                                    else
                                    {
                                        foreach (int sc in s.content)
                                            yields.Add(solver.MkNumeral(sc, charsort));
                                    }
                                }
                            }
                            else
                                throw new BekException(); //TBD: undefined case
                        }
                    }
                    #endregion
                    moves.Add(stb.MkFinalOutput(0, guard, yields.ToArray()));
                }
            }

            //if no end cases were given, assume default true end case with empty yield
            if (noEndCases)
                moves.Add(stb.MkFinalOutput(0, solver.True));

            STModel iterST = STModel.Create(solver, "iter", initReg, charsort, charsort, regSort, 0, moves);
            iterST.Simplify();
            return iterST;
        }
    }
}
