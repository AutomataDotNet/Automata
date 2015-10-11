using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Bek.Frontend.AST;
using Microsoft.Bek.Frontend.TreeOps;
using Microsoft.Z3;
using Microsoft.Automata.Z3.Internal;
using Microsoft.Bek.Frontend.Meta;

namespace Microsoft.Bek.Model.Converters
{
    internal abstract class AValueExpr<T>
    {
        //public BekCharModes mode;

        public AValueExpr(/*BekCharModes mode*/)
        {
            //this.mode = mode;
        }

        //public abstract Converters.Z3.Library Library { get; }

        public T Convert(expr e, Func<ident, T> identmap)
        {
            this.identmap = identmap;
            var ret = router.Visit(this, e);
            if (ret == null)
                throw new ArgumentNullException();
            return ret;
        }

        private static FuncVisitor<AValueExpr<T>, expr, T> router = new FuncVisitor<AValueExpr<T>, expr, T>()
        {
            // arithmetic ops
            (AValueExpr<T> cc, boolconst be)   => cc.MkBool(be.val),
            (AValueExpr<T> cc, charconst be)   => cc.MkChar(be.val),
            (AValueExpr<T> cc, ident e)        => cc.identmap(e),
            // special functions that need custom 
            (AValueExpr<T> cc, functioncall e)     => cc.MkFunctionCall(e.id, e.args.ConvertAll<T>(cc.Convert).ToArray()),
            //strings
            (AValueExpr<T> cc, strconst e)     => cc.MkFunctionCall(new ident("string",0,0), e.content.ConvertAll<T>(cc.MkChar).ToArray()),
        };

        private Func<ident, T> identmap;

        private T Convert(expr e)
        {
            return router.Visit(this, e);
        }

        protected abstract T MkBool(bool val);
        protected abstract T MkChar(int val);
        protected abstract T MkFunctionCall(ident name, params T[] args);
    }
}
