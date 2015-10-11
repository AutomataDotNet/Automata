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
    internal class IterInfo
    {
        private static Filter<iterassgn> assign_filter = new Filter<iterassgn>();
        private static Filter<yieldstmt> yield_filter = new Filter<yieldstmt>();

        public static IEnumerable<iterassgn> InitialBools(iterexpr e, Symtab stab)
        {
            foreach (iterassgn ia in assign_filter.Apply(e.initializer))
            {
                if (stab.Get(ia.lhs).type == BekTypes.BOOL)
                    yield return ia;
            }
        }

        public static IEnumerable<iterassgn> InitialChars(iterexpr e, Symtab stab)
        {
            foreach (iterassgn ia in assign_filter.Apply(e.initializer))
            {
                if (stab.Get(ia.lhs).type == BekTypes.CHAR)
                    yield return ia;
            }
        }

        public static IEnumerable<iterassgn> Initializers(iterexpr e, Symtab stab)
        {
            foreach (iterassgn ia in assign_filter.Apply(e.initializer))
            {
                yield return ia;
            }
        }

        public static IEnumerable<iterassgn> BoolUpdatess(itercase ic, Symtab stab)
        {
            foreach (iterassgn ia in assign_filter.Apply(ic))
            {
                if (stab.Get(ia.lhs).type == BekTypes.BOOL)
                    yield return ia;
            }
        }

        public static IEnumerable<iterassgn> CharUpdates(itercase ic, Symtab stab)
        {
            foreach (iterassgn ia in assign_filter.Apply(ic))
            {
                if (stab.Get(ia.lhs).type == BekTypes.CHAR)
                    yield return ia;
            }
        }

        public static IEnumerable<iterassgn> AllUpdates(itercase ic, Symtab stab=null)
        {
            foreach (iterassgn ia in assign_filter.Apply(ic))
            {
                yield return ia;
            }
        }

        public static IEnumerable<expr> YieldSeq(itercase ic)
        {
            foreach (yieldstmt ys in yield_filter.Apply(ic))
            {
                foreach (expr ce in ys.args)
                {
                    // This downcast should never fail, since
                    // preprocessing steps eliminate all non-charexpr
                    // yields...
                    yield return (expr)ce;
                }
            }
        }
    }
}
