using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Bek.Frontend;
using Microsoft.Bek.Frontend.AST;
using Microsoft.Bek.Frontend.TreeOps;
using Microsoft.Bek.Frontend.Meta;

using Microsoft.Automata;

namespace Microsoft.Bek.Model.Converters
{
    internal abstract class ABekProg<STModel, S, T> : IConverter<STModel>
    {
        public ABekProg(AStrExpr<S, T> ase) {
            this.str_handler = ase;
        }

        protected AStrExpr<S, T> str_handler;

        public virtual STModel Convert(BekProgram a)
        {
            var stringdic = new Dictionary<int, S>();
            Func<ident, S> strmapping = x => stringdic[a.stab.Get(x).id];

            var ast = a.ast;
            var inputvar = ast.input;

            // should be exactly one return; this is checked
            // earlier so we assume it here
            var retfilter = new Filter<returnstmt>();


            if (ast.body is returnstmt)
            {
                returnstmt e = ast.body as returnstmt;
                return this.ReturnModel(a, str_handler.Convert(e.val, strmapping, a.stab));
            }
            else
                throw new BekException("Bek program not supported.");
        }

        //protected abstract S InputModel(ident i);

        protected abstract STModel ReturnModel(BekProgram p, S res);

        public abstract void Dispose();

        //public abstract ILibrary<Microsoft.Z3.Expr> Library { get; }


        ILibrary<Microsoft.Z3.Expr> IConverter<STModel>.Library
        {
            get { throw new NotImplementedException(); }
        }
    }
}
