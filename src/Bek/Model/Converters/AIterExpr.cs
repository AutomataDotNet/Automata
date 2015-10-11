using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Z3;
using Microsoft.Automata.Z3;
using Microsoft.Bek.Frontend;
using Microsoft.Bek.Frontend.AST;
using Microsoft.Bek.Frontend.TreeOps;
using Microsoft.Bek.Frontend.Meta;

namespace Microsoft.Bek.Model.Converters
{
    internal abstract class AIterExpr<S, T> {
        //public BekCharModes mode;

        public AIterExpr(AValueExpr<T> expr_handler  /*, BekCharModes mode*/) {
            this.expr_handler = expr_handler;
            //this.mode = mode;

            //if (expr_handler.mode != mode)
            //{
            //    throw new ModelException("Internal error: inconsistent encoding modes");
            //}
        }

        //public abstract Converters.Z3.Library Library { get; }

        internal AValueExpr<T> expr_handler;

        public abstract S Convert(iterexpr ie, Symtab stab);
    }

}
