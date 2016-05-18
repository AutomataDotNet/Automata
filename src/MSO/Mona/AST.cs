using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using QUT.Gppg;

namespace Microsoft.Automata.MSO.Mona
{
    public abstract class Ast
    {
    }

    public class Token : Ast
    {
        public readonly string text;
        public string file;

        /// <summary>
        /// The line at which the text span starts.
        /// </summary>
        public int StartLine { get { return location.StartLine; } }

        /// <summary>
        /// The column at which the text span starts.
        /// </summary>
        public int StartColumn { get { return location.StartColumn; } }

        /// <summary>
        /// The line on which the text span ends.
        /// </summary>
        public int EndLine { get { return location.EndLine; } }

        /// <summary>
        /// The column of the first character
        /// beyond the end of the text span.
        /// </summary>
        public int EndColumn { get { return location.EndColumn; } }

        /// <summary>
        /// Source file name of the location or null.
        /// </summary>
        public string File { get { return file; } }

        LexLocation location;
        internal LexLocation Location
        {
            get
            {
                return location;
            }
        }

        Tokens kind;
        internal Tokens Kind
        {
            get { return kind; }
        }


        internal Token(string text, LexLocation loc, Tokens kind, string file)
        {
            this.text = text;
            this.location = loc;
            this.kind = kind;
            this.file = file;
        }

        internal Token(string name, Tokens kind)
        {
            this.text = name;
            this.location = new LexLocation();
            this.kind = kind;
            this.file = null;
        }

        internal Token(string name)
        {
            this.text = name;
            this.location = new LexLocation();
            this.kind = (Tokens)(-1);
            this.file = null;
        }

        public override string ToString()
        {
            return text;
        }

        public bool TryGetInt(out int k)
        {
            if (text.StartsWith("0x"))
                return int.TryParse(text.Substring(2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out k);
            else
                return int.TryParse(text, out k);
        }

        public int ToInt()
        {
            int k = 0;
            if (TryGetInt(out k))
                return k;
            else
                throw new MonaParseException(location, string.Format("cannot convert to integer: {0}", text));
        }
    }

    public class Program : Ast
    {
        public Token header;
        public Cons<Ast> declarations;

        internal Program(Token header, Cons<Ast> declarations)
        {
            this.header = header;
            this.declarations = declarations;
        }

        internal void Typecheck()
        {
            throw new NotImplementedException();
        }
    }

    public abstract class Expression : Ast 
    {
    }

    public abstract class Formula : Expression
    {
        public Token token;
        internal Formula(Token token)
        {
            this.token = token;
        }
    }

    public abstract class Term : Expression
    {
        public Token token;
        internal Term(Token token)
        {
            this.token = token;
        }
    }

    public class BooleanFormula : Formula 
    {
        public Formula[] subformulas;
        internal BooleanFormula(Token token, Formula[] subformulas)
            : base(token)
        {
            this.subformulas = subformulas;
        }
    }

    public class Atom : Formula
    {
        public Term[] terms;
        internal Atom(Token token, Term[] terms)
            : base(token)
        {
            this.terms = terms;
        }
    }

    public class PredApp : Formula
    {
        public Cons<Expression> expressions;
        internal PredApp(Token token, Cons<Expression> expressions)
            : base(token)
        {
            this.expressions = expressions;
        }
    }

    public class Q0Formula : Formula
    {
        public Cons<Token> vars;
        public Formula formula;
        internal Q0Formula(Token quantifier, Cons<Token> vars, Formula formula) :
            base(quantifier)
        {
            this.vars = vars;
            this.formula = formula;
        }
    }

    public class QFormula : Formula
    {
        public Cons<Token> universes;
        public Cons<VarWhere> vars;
        public Formula formula;
        internal QFormula(Token quantifier, Cons<VarWhere> vars, Formula formula, Cons<Token> universes = null) :
            base(quantifier)
        {
            this.universes = universes;
            this.vars = vars;
            this.formula = formula;
        }
    }

    public class VarWhere : Ast
    {
        public Token name;
        public Formula where;
        internal VarWhere(Token name, Formula where)
        {
            this.name = name;
            this.where = where;
        }

        public override string ToString()
        {
            if (where == null)
                return name.text;
            else
                return name.text + " where " + where.ToString();
        }
    }

    public class Name : Term
    {
        internal Name(Token name) : base(name)
        {
        }
    }

    public class Int : Term
    {
        int i;
        internal Int(Token val)
            : base(val)
        {
            if (!val.TryGetInt(out i))
                throw new MonaParseException(val.Location, "invalid integer format");
        }

        public int Value
        {
            get
            {
                return i;
            }
        }
    }

    public class FuncApp : Term
    {
        public Term[] args;
        internal FuncApp(Token func, params Term[] args)
            : base(func)
        {
            this.args = args;
        }
    }

    public class Set : Term
    {
        Cons<Term> elems;
        internal Set(Token lbrace, Cons<Term> elems) : base(lbrace)
        {
            this.elems = elems;
        }

    }

    /// <summary>
    /// Simply linked list of elements.
    /// </summary>
    /// <typeparam name="T">element type</typeparam>
    public class Cons<T> : Ast, IEnumerable<T>
    {
        T first;
        Cons<T> rest;
        int count;

        /// <summary>
        /// Gets the first element of this list.
        /// </summary>
        public T First
        {
            get
            {
                if (count == 0)
                    throw new InvalidOperationException("Empty list");
                return first;
            }
        }

        /// <summary>
        /// Gets the rest of the list.
        /// </summary>
        public Cons<T> Rest
        {
            get
            {
                if (count == 0)
                    throw new InvalidOperationException("Empty list");
                return rest;
            }
        }

        /// <summary>
        /// Gets the number of elements in the list.
        /// </summary>
        public int Count
        {
            get
            {
                return count;
            }
        }

        /// <summary>
        /// Gets the i'th element, i must be in the range 0..Count-1.
        /// The operation is linear in the length of the list.
        /// </summary>
        /// <param name="i">index of the element to get</param>
        public T GetElement(int i)
        {
            if (i < 0 || i >= count)
                throw new IndexOutOfRangeException();
            var curr = this;
            for (int j = 0; j < i; j++)
                curr = curr.rest;
            return curr.first;
        }

        /// <summary>
        /// Gets the i'th element, i must be in the range 0..Count-1.
        /// The operation is linear in the length of the list.
        /// </summary>
        /// <param name="i">index of the element to get</param>
        public T this[int i]
        {
            get
            {
                return GetElement(i);
            }
        }

        Cons()
        {
            this.first = default(T);
            this.rest = null;
            this.count = 0;
        }

        readonly static Cons<T> empty = new Cons<T>();

        /// <summary>
        /// The empty list.
        /// </summary>
        public static Cons<T> Empty
        {
            get { return empty; }
        }

        /// <summary>
        /// Returns true if the list is empty.
        /// </summary>
        public bool IsEmpty
        {
            get { return count == 0; }
        }

        /// <summary>
        /// Constructs a new Cons list.
        /// </summary>
        /// <param name="first">the first element</param>
        /// <param name="rest">the rest of the list</param>
        public Cons(T first, Cons<T> rest)
        {
            if (rest == null)
                throw new ArgumentNullException("rest");
            this.first = first;
            this.rest = rest;
            this.count = rest.count + 1;
        }

        /// <summary>
        /// Make a new list of the given elements.
        /// </summary>
        /// <param name="elems">given elements</param>
        public static Cons<T> Mk(params T[] elems)
        {
            var res = empty;
            for (int i = elems.Length - 1; i >= 0; i--)
                res = new Cons<T>(elems[i], res);
            return res;
        }

        /// <summary>
        /// Gets the enumerator of the elements in this Cons list.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            return new ConsEnumerator(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new ConsEnumerator(this);
        }

        class ConsEnumerator : IEnumerator<T>
        {
            Cons<T> orig;
            Cons<T> cons;
            T current;
            internal ConsEnumerator(Cons<T> cons)
            {
                this.cons = cons;
                this.orig = cons;
                this.current = default(T);
            }

            public T Current
            {
                get
                {
                    return current;
                }
            }

            public void Dispose()
            { }

            object System.Collections.IEnumerator.Current
            {
                get { return current; }
            }

            public bool MoveNext()
            {
                if (cons.IsEmpty)
                    return false;
                else
                {
                    current = cons.first;
                    cons = cons.rest;
                    return true;
                }
            }

            public void Reset()
            {
                this.cons = this.orig;
                this.current = default(T);
            }
        }

        //public override void PrettyPrint(StringBuilder sb)
        //{
        //    sb.Append("(");
        //    var curr = this;
        //    bool firstdone = false;
        //    while (!curr.isempty)
        //    {
        //        if (firstdone)
        //            sb.Append(" ");
        //        curr.first.PrettyPrint(sb);
        //        curr = curr.rest;
        //        firstdone = true;
        //    }
        //    sb.Append(")");
        //}

        /// <summary>
        /// Returns true iff the lists have the same length and have equal elements.
        /// </summary>
        /// <param name="list">other list to compare to</param>
        public bool Equals(Cons<T> list)
        {
            if (list == null)
                throw new ArgumentNullException("list");

            if (list.count != count)
                return false;

            var list1 = this;
            var list2 = list;
            for (int j = 0; j < count; j++)
            {
                if (!object.Equals(list1.first, list2.first))
                    return false;
                list1 = list1.rest;
                list2 = list2.rest;
            }
            return true;
        }
    }

    public class VarDecl : Ast
    {
        Token token;
        Cons<Token> univs;
        Cons<VarWhere> vars;
        internal VarDecl(Token token, Cons<Token> univs, Cons<VarWhere> vars)
        {
            this.token = token;
            this.univs = univs;
            this.vars = vars;
        }
    }
}
