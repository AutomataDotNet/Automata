using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Bek.Frontend.TreeOps;
using Microsoft.Bek.Frontend.AST;
using Microsoft.Bek.Frontend;
using Microsoft.Bek.Frontend.Meta;

namespace Microsoft.Bek.Model.Converters
{
    internal abstract class AStrExpr<STModel, T>
    {
        //public BekCharModes mode;
        internal AIterExpr<STModel, T> iter_handler;
        private Func<ident, STModel> strexprmapping;
        private Symtab stab;

        //public abstract Z3.Library Library { get; }

        public AStrExpr(AIterExpr<STModel, T> iter_handler /*, BekCharModes mode*/)
        {
            //this.mode = mode;
            this.iter_handler = iter_handler;
            //if (iter_handler.mode != mode)
            //{
            //    throw new ModelException("Internal error: inconsistent encoding modes");
            //}
        }

        public STModel Convert(expr e, Func<ident, STModel> strexprmapping, Symtab stab)
        {
            this.strexprmapping = strexprmapping;
            this.stab = stab;
            return router.Visit(this, e, false);
        }

        private static FuncVisitor<AStrExpr<STModel, T>, expr, STModel> router = new FuncVisitor<AStrExpr<STModel, T>, expr, STModel>()
        {
            (AStrExpr<STModel, T> cc, iterexpr e) => cc.HandleIter(e),
            (AStrExpr<STModel, T> cc, ident e)    => cc.HandleIdent(e),
            (AStrExpr<STModel, T> cc, strconst e) => cc.HandleStrLiteral(e)
        };

        protected STModel HandleIter(iterexpr e)
        {
            return this.iter_handler.Convert(e, this.stab);
        }

        protected STModel HandleIdent(ident e)
        {
            return this.strexprmapping(e);
        }

        protected abstract STModel HandleStrLiteral(strconst e);
    }
}
