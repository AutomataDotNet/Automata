using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Automata.Z3;
using Microsoft.Automata;
using Microsoft.Z3;
using Microsoft.Bek.Frontend.Meta;

using STz3 = Microsoft.Automata.ST<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;
using STbz3 = Microsoft.Automata.STb<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;
using STBuilderZ3 = Microsoft.Automata.STBuilder<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;


namespace Microsoft.Bek.Model.Converters.Z3
{
    internal class StrExpr : AStrExpr<STz3, Expr>
    {
        private STBuilderZ3 stb;
        private Sort charsort;
        public StrExpr(AIterExpr<STz3, Expr> ith, STBuilderZ3 stb, Sort charsort) : base(ith /*, mode*/)
        {
            this.stb = stb;
            this.charsort = charsort;
        }

        protected override STz3 HandleStrLiteral(Frontend.AST.strconst e)
        {
            throw new NotImplementedException();
        }

    }

    internal class StrExpr2STb : AStrExpr<STbz3, Expr>
    {
        private STBuilderZ3 stb;
        private Sort charsort;
        public StrExpr2STb(AIterExpr<STbz3, Expr> ith, STBuilderZ3 stb, Sort charsort)
            : base(ith /*, mode*/)
        {
            this.stb = stb;
            this.charsort = charsort;
        }

        protected override STbz3 HandleStrLiteral(Frontend.AST.strconst e)
        {
            //throw new NotImplementedException();
            //create an STb that removes the input and just outputs the output string
            STbz3 res = new STbz3(this.stb.Solver, "fixed", this.charsort, this.charsort, this.stb.Solver.UnitSort, this.stb.Solver.UnitConst, 0);
            var rule = new BaseRule<Expr>(Sequence<Expr>.Empty, this.stb.Solver.UnitConst, 0);
            res.AssignRule(0, rule);
            List<Expr> elems = new List<Expr>();
            foreach (var c in e.val)
                elems.Add(this.stb.Solver.MkCharExpr(c));
            var yield = new Sequence<Expr>(elems.ToArray());
            var frule = new BaseRule<Expr>(yield, this.stb.Solver.UnitConst, 0);
            res.AssignFinalRule(0, frule);
            return res;
        }

        BekCharModes ConvertCharEncoding(Microsoft.Automata.BitWidth enc)
        {
            switch (enc)
            {
                case Automata.BitWidth.BV16: return BekCharModes.BV16;
                case Automata.BitWidth.BV7: return BekCharModes.BV7;
                case Automata.BitWidth.BV32: return BekCharModes.BV32;
                case Automata.BitWidth.BV8: return BekCharModes.BV8;
                default:
                    throw new BekException();
            }
        }
    }
}
