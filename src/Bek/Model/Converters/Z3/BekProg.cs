using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Automata.Z3;
using Microsoft.Z3;
using Microsoft.Bek.Frontend.Meta;
using Microsoft.Bek.Frontend;

using Microsoft.Automata;

using STBuilderZ3 = Microsoft.Automata.STBuilder<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;
using STModel = Microsoft.Automata.ST<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;

namespace Microsoft.Bek.Model.Converters.Z3
{
    internal class BekProg : ABekProg<STModel, STModel, Expr>
    {
        internal STBuilderZ3 stb;
        internal Sort charsort;


        public BekProg(AStrExpr<STModel, Expr> ase, STBuilderZ3 stb, Sort charsort)
            : base(ase)
        {
            this.stb = stb;
            this.charsort = charsort;
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
    }
}
