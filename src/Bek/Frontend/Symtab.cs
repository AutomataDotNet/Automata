using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Bek.Frontend.TreeOps;
using Microsoft.Bek.Frontend.Meta;
using Microsoft.Bek.Frontend.AST;

namespace Microsoft.Bek.Frontend
{

    public class SymtabElt
    {
        static int nextid = 0;

        public SymtabElt(object def, BekTypes type)
        {
            this.id = nextid++;
            this.def = def;
            this.type = type;
        }

        public int id;
        public object def;
        public BekTypes type;
    }

    public class Symtab
    {
        internal Dictionary<ident, SymtabElt> symtab;
        internal Stack<Dictionary<string, SymtabElt>> names;

        public Symtab(BekPgm ast)
        {
            this.symtab = new Dictionary<ident, SymtabElt>();
            this.names = new Stack<Dictionary<string, SymtabElt>>();
            this.names.Push(new Dictionary<string, SymtabElt>());

            HandleLhs(ast.input, BekTypes.STR, true);

            stmt_visitor.Visit(this, ast.body);

            this.names = null;
        }

        public SymtabElt Get(ident i)
        {
            SymtabElt ret;
            if (!symtab.TryGetValue(i, out ret))
            {
                throw new BekParseException(i.line, i.pos, string.Format("'{0}' undefined", i.name));
            }
            return ret;
        }

        public void Alias(ident a, ident withb)
        {
            symtab[a] = symtab[withb];
        }

#region Dispatch visitors

        static internal ActionVisitor<Symtab, stmt> stmt_visitor = new ActionVisitor<Symtab, stmt>() 
        { 
          //(Symtab stab, assgn cur)      => stab.HandleLhs(cur.lhs, topexpr_visitor.Visit(stab, cur.rhs, true), true),
          (Symtab stab, returnstmt cur) => topexpr_visitor.Visit(stab, cur.val),
        };

        static internal FuncVisitor<Symtab, expr, BekTypes> topexpr_visitor = new FuncVisitor<Symtab, expr, BekTypes>() 
        { 
          (Symtab stab, iterexpr cur) => stab.HandleIter(cur),
          (Symtab stab, ident cur)    => stab.AddElt(cur, stab.GetElt(cur)).type,
          (Symtab stab, strconst cur) => BekTypes.STR,
          (Symtab stab, elemlist cur) => BekTypes.STR,
          (Symtab stab, functioncall cur) => BekTypes.ANY,
          (Symtab stab, replace cur) => BekTypes.STR,
          (Symtab stab, expr cur)     => { throw new BekParseException("Expected string expression"); }
        };

        static internal FuncVisitor<Symtab, expr, BekTypes> iter_init_visitor = new FuncVisitor<Symtab, expr, BekTypes>()
        {
          (Symtab stab, charconst e) => BekTypes.CHAR,
          (Symtab stab, boolconst e) => BekTypes.BOOL,
        };

#endregion

        internal void HandleLhs(ident i, BekTypes t, bool implicitdefine=false)
        {
            SymtabElt e;
            if (TryGetElt(i.name, out e))
            {
                if (implicitdefine)
                    throw new BekParseException(i.line, i.pos, string.Format("'{0}' already defined", i.name));
                if (e.type != t)
                    throw new BekParseException(i.line, i.pos, string.Format("'{0}' inconsistent type", i.name));

                this.symtab[i] = e;
            }
            else if (implicitdefine)
            {
                e = new SymtabElt(i, t);
                this.AddElt(i, e);
            }
            else
            {
                throw new BekParseException(i.line, i.pos, string.Format("'{0}' undefined", i.name));
            }
        }

        internal BekTypes HandleIter(iterexpr cur)
        {
            topexpr_visitor.Visit(this, cur.source);
            PushIter(cur.binder, cur.initializer);

            var identfilter = new Filter<ident>();
            foreach (var id in identfilter.Apply(cur.body))
            {
                AddElt(id, GetElt(id));
            }

            PopBlock();
            return BekTypes.STR;
        }

        internal void PushIter(ident binder, iterinit initializer)
        {
            this.PushBlock();

            // clear names
            this.RemoveElt(binder.name);
            foreach (var assgn in initializer.assgns)
            {
                this.RemoveElt(assgn.lhs.name);
            }

            // re-assign names
            foreach (var assgn in initializer.assgns)
            {
                this.HandleLhs(assgn.lhs, iter_init_visitor.Visit(this, assgn.rhs), true);
            }
            this.HandleLhs(binder, BekTypes.CHAR, true);
        }

        internal SymtabElt GetElt(ident i)
        {
            SymtabElt ret;
            if (!TryGetElt(i.name, out ret))
            {
                throw new BekParseException(i.line, i.pos, string.Format("'{0}' undefined", i.name));
            }
            return ret;
        }

        internal bool TryGetElt(string name, out SymtabElt e)
        {
            return this.names.Peek().TryGetValue(name, out e);
        }

        internal SymtabElt AddElt(ident i, SymtabElt e)
        {
            this.names.Peek()[i.name] = e;
            this.symtab[i] = e;
            return e;
        }

        internal void RemoveElt(string name)
        {
            this.names.Peek().Remove(name);
        }

        internal void PushBlock()
        {
            this.names.Push(new Dictionary<string, SymtabElt>(this.names.Peek()));
        }

        internal void PopBlock()
        {
            this.names.Pop();
        }
    }
}
