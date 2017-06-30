using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Automata.Z3;
using Microsoft.Z3;
using Microsoft.Bek.Frontend.AST;
using Microsoft.Bek.Frontend;
using Microsoft.Automata;
using Microsoft.Bek.Frontend.Meta;
using Microsoft.Automata.Z3.Internal;

using STModel = Microsoft.Automata.STb<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;
using Rulez3 = Microsoft.Automata.Rule<Microsoft.Z3.Expr>;
using STBuilderZ3 = Microsoft.Automata.STBuilder<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;


namespace Microsoft.Bek.Model.Converters.Z3
{
    internal class IterExpr2STb : AIterExpr<STModel, Expr>
    {
        internal Sort charsort;
        STBuilderZ3 stb;


        public IterExpr2STb(ValueExpr expr_handler, STBuilderZ3 stb, Sort charsort)
            : base(expr_handler)
        { 
            this.charsort = charsort;
            this.stb = stb;
        }

        private class boolstate : HashSet<int>
        {
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

        public override STModel Convert(iterexpr ie, Symtab stab)
        {
            VarInfo I = new VarInfo(stb, stab, ie, charsort);

            var iterCases = ConsList<itercase>.Create(ie.GetNormalCases());
            var endCases = ConsList<itercase>.Create(ie.GetEndCases());

            BranchingRule<Expr> rule = CreateBranchingRule(iterCases, stb.Solver.True, I);
            BranchingRule<Expr> frule = CreateBranchingRule(endCases, stb.Solver.True, I);

            STModel st = new STModel(stb.Solver, "iterSTb", charsort, charsort, I.regSort, I.initReg, 0);
            st.AssignRule(0, rule);
            st.AssignFinalRule(0, frule);
            return st;
        }

        private BranchingRule<Expr> CreateBranchingRule(ConsList<itercase> iterCases, Expr pathCond, VarInfo I)
        {
            if (iterCases == null)
                return new BaseRule<Expr>(Sequence<Expr>.Empty, I.r, 0);
                //return new RaiseRule<Expr>();
            else
            {
                Expr cond = expr_handler.Convert(iterCases.First.cond, I.GetVarExpr);

                var pathCond_and_cond = stb.And(pathCond, cond);
                var pathCond_and_not_cond = stb.And(pathCond, stb.Not(cond));
                //check that the condition is satisfiable under the path condition
                if (stb.Solver.IsSatisfiable(pathCond_and_cond))
                {
                    var trueCase = CreateBodyRule(iterCases.First.body, pathCond_and_cond, I);

                    if (stb.Solver.IsSatisfiable(pathCond_and_not_cond))
                    {
                        var falseCase = CreateBranchingRule(iterCases.Rest, pathCond_and_not_cond, I);
                        if (falseCase == trueCase) //both are == haltRule
                            return trueCase;
                        else
                            return new IteRule<Expr>(cond, trueCase, falseCase);
                    }
                    else //the rest of the cases are not reachable
                        return trueCase;
                }
                else //the first case is not reachable, just consider the rest
                {
                    return CreateBranchingRule(iterCases.Rest, pathCond, I);
                }
            }
        }

        private BranchingRule<Expr> CreateBodyRule(List<iterstmt> iters, Expr pathCond, VarInfo I)
        {
            if (iters.Count == 0)
                return new UndefRule<Expr>();

            //iterate over the iters to create a single register update 
            Expr[] regs = new Expr[I.K];
            for (int i = 0; i < I.K; i++)
                regs[i] = I.GetRegExpr_i(i);

            //sequential semantics
            //Func<ident, Expr> idmap = (i => (I.binderid.Equals(I.GetBekVarNr(i)) ? I.c : (regs[I.GetRegNr(i)]))); 
            Func<ident, Expr> idmap = I.GetVarExpr;  //parallel semantics 

            List<Expr> yields = new List<Expr>();

            raisestmt raise = null;

            ifthenelse ite = null;

            foreach (var ist in iters)
            {
                iterassgn a = ist as iterassgn;
                if (a != null)
                {
                    var v = expr_handler.Convert(a.rhs, idmap);
                    regs[I.GetRegNr(a.lhs)] = v;
                }
                else if (ist is yieldstmt)
                {
                    yieldstmt y = ist as yieldstmt;

                    foreach (var e in y.args)
                    {
                        strconst s = e as strconst;
                        if (s == null)
                        {
                            yields.Add(expr_handler.Convert(e, idmap));
                        }
                        else
                        {
                            foreach (int sc in s.content)
                                yields.Add(stb.Solver.MkNumeral(sc, charsort));
                        }
                    }
                }
                else if (ist is ifthenelse)
                {
                    ite = (ifthenelse)ist; //we know that there can only be a single ite
                    break; //
                }
                else 
                {
                    raise = (raisestmt)ist;
                    break; //
                }
            }
            if (raise != null)
            {
                var rule = new UndefRule<Expr>(raise.exc);
                return rule;
            }
            else if (ite != null)
            {
                var branchCond = expr_handler.Convert(ite.cond, I.GetVarExpr); 
                //check feasability of the true and false branches, eliminate dead code
                var pathCond_and_branchCond = stb.And(pathCond, branchCond);
                var pathCond_and_not_branchCond = stb.And(pathCond, stb.Not(branchCond));
                if (!stb.Solver.IsSatisfiable(pathCond_and_branchCond))
                {
                    //the path condition implies the negated branch condition
                    return CreateBodyRule(ite.fcase, pathCond, I);
                }
                else if (!stb.Solver.IsSatisfiable(pathCond_and_not_branchCond))
                {
                    //the path condition implies the branch condition
                    return CreateBodyRule(ite.tcase, pathCond, I);
                }
                else
                {
                    var tCase = CreateBodyRule(ite.tcase, pathCond_and_branchCond, I);
                    var fCase = CreateBodyRule(ite.fcase, pathCond_and_not_branchCond, I);
                    var iterule = new IteRule<Expr>(branchCond, tCase, fCase);
                    return iterule;
                }
            }
            else
            {
                Expr regExpr = (regs.Length == 1 ? regs[0] : stb.Solver.MkTuple(regs));
                var rule = new BaseRule<Expr>(new Sequence<Expr>(yields.ToArray()), regExpr, 0);
                return rule;
            }
        }

        internal class VarInfo
        {
            Symtab stab;
            internal int binderid;
            ident binder;
            List<int> bekVarIds;
            List<Expr> bekVarVals;
            List<Sort> bekVarSorts;
            Dictionary<int, int> proj;
            STBuilderZ3 stb;
            internal Sort charsort;
            internal Sort regSort;
            internal Expr initReg;
            internal Expr c;
            internal Expr r;
            internal int K;
            Dictionary<int, Expr> varMap;
            //internal RaiseRule<Expr> haltRule;
            public VarInfo(STBuilderZ3 stb, Symtab stab, iterexpr ie, Sort charsort)
            {
                this.stab = stab;
                this.charsort = charsort;
                this.stb = stb;
                this.binder = ie.binder;
                binderid = stab.Get(ie.binder).id;
                bekVarIds = new List<int>(); 
                bekVarVals = new List<Expr>();  
                bekVarSorts = new List<Sort>();    

                proj = new Dictionary<int, int>();

                int regPosNr = 0;
                foreach (iterassgn ia in IterInfo.Initializers(ie, stab))
                {
                    proj[stab.Get(ia.lhs).id] = regPosNr;
                    bekVarIds.Add(stab.Get(ia.lhs).id);
                    bekVarVals.Add(MkExpr(stab.Get(ia.lhs).type, ia.rhs));
                    bekVarSorts.Add(BekTypeToSort(stab.Get(ia.lhs).type));
                    regPosNr += 1;
                }

                K = bekVarSorts.Count;

                //register sort
                regSort = (K == 0 ? stb.Solver.UnitSort : (K == 1 ? bekVarSorts[0] : stb.Solver.MkTupleSort(bekVarSorts.ToArray())));

                //initial register value
                initReg = (K == 0 ? stb.Solver.UnitConst : (K == 1 ? bekVarVals[0] : stb.Solver.MkTuple(bekVarVals.ToArray())));

                //input character variable
                c = this.stb.MkInputVariable(charsort);

                //register variable
                r = this.stb.MkRegister(regSort);

                //maps variable identifiers used in the bek program to corresponding term variables
                varMap = new Dictionary<int, Expr>();
                varMap[binderid] = c;                              //input character variable

                for (int i = 0; i < K; i++)                        //bek pgm variables
                    varMap[bekVarIds[i]] = (K == 1 ? r : stb.Solver.MkProj(i, r));

                //haltRule = new RaiseRule<Expr>();
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

            internal int GetRegNr(ident varid)
            {
                return proj[stab.Get(varid).id];
            }

            internal Expr GetVarExpr(ident varid)
            {
                return varMap[stab.Get(varid).id];
            }

            internal int GetBekVarNr(ident varid)
            {
                return stab.Get(varid).id;
            }

            internal Expr GetVarExpr(int varid)
            {
                return varMap[varid];
            }

            internal Expr GetRegExpr_i(int i)
            {
                return varMap[bekVarIds[i]];
            }
        }
    }
}
