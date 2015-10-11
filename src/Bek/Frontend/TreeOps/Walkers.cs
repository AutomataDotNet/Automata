using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Runtime.Serialization;

namespace Microsoft.Bek.Frontend.TreeOps
{
    public class TreeCopier<RootType>
    {

        static public RootType Copy(RootType root)
        {
            return (RootType)CopyNode(root);
        }

        static internal Object CopyNode(object o)
        {
            object oprime = FormatterServices.GetUninitializedObject(o.GetType());

            foreach (var field in TreeInfo.NonChildEntries(o))
            {
                // value fields (e.g., strings and ints) are shared
                field.SetValue(oprime, field.GetValue(o));
            }
            foreach (var field in TreeInfo.ChildEntries(o))
            {
                field.SetValue(oprime, CopyNode(field.GetValue(o)));
            }
            foreach (var field in TreeInfo.ChildListEntries(o))
            {
                IList newlist = (IList)Activator.CreateInstance(field.GetValue(o).GetType());
                var enumerator = (System.Collections.IEnumerable)field.GetValue(o);
                foreach (object c in enumerator)
                {
                    newlist.Add(CopyNode(c));
                }
                field.SetValue(oprime, newlist);
            }

            return oprime;
        }
    }

    //public enum post_visit_action { Descend, Stop };
    //class TreeWalker<RootType> : IEnumerable<Func<TreeWalker<RootType>, Object, post_visit_action>>
    //{
    //    internal Dictionary<Type, Func<TreeWalker<RootType>, Object, post_visit_action>> handlers;
    //    internal Func<TreeWalker<RootType>, Object, post_visit_action> default_handler;
        
    //    public void set_def<S, T>(Func<S, T, post_visit_action> f) where S : TreeWalker<RootType> {
    //        default_handler = new Func<TreeWalker<RootType>, Object, post_visit_action>((walker, node) => f((S)walker, (T)node));
    //    }

    //    public TreeWalker()
    //    {
    //        handlers = new Dictionary<Type, Func<TreeWalker<RootType>, Object, post_visit_action>>();
    //    }

    //    public void Walk(RootType root)
    //    {
    //        var s = new Stack<Object>();
    //        s.Push(root);

    //        while (s.Count > 0)
    //        {
    //            Object cur = s.Pop();
    //            post_visit_action pva;
    //            if (handlers.ContainsKey(cur.GetType())) {
    //                pva = handlers[cur.GetType()](this, cur);
    //            }
    //            else {
    //                pva = this.default_handler(this, cur);
    //            }

    //            if (pva == post_visit_action.Descend)
    //            {
    //                foreach (Object o in TreeInfo.AllChildValues(cur))
    //                {
    //                    s.Push(o);
    //                }
    //            }
    //        }
    //    }

    //    public void Add<S, T>(Func<S, T, post_visit_action> f) where S : TreeWalker<RootType>
    //    {
    //        handlers[typeof(T)] = new Func<TreeWalker<RootType>, Object, post_visit_action>((walker, node) => f((S)walker, (T)node));
    //    }

    //    #region IEnumerator methods (to support seq initializers)

    //    public IEnumerator<Func<TreeWalker<RootType>, Object, post_visit_action>> GetEnumerator()
    //    {
    //        return handlers.Values.GetEnumerator();
    //    }

    //    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    //    {
    //        return handlers.Values.GetEnumerator();
    //    }

    //    #endregion
    //}

    public class Filter<ResType> : IEnumerable<KeyValuePair<string, Func<Object, bool> > >
    {
        internal Dictionary<string, Func<object, bool>> preds_;
        internal List<ResType> results_;

        public Filter()
        {
            preds_  = new Dictionary<string, Func<Object, bool>>();
            results_ = new List<ResType>();
        }

        static internal ActionVisitor<Filter<ResType>, object> gen = new ActionVisitor<Filter<ResType>, object>()
        {
           (Filter<ResType> filt, ResType cur) => filt.TestMatch(cur),
           (Filter<ResType> filt, object cur)  => filt.VisitChildren(cur)
        };

        internal void VisitChildren(object o)
        {
            foreach (object c in TreeInfo.AllChildValues(o))
            {
                gen.Visit(this, c, true);
            }
        }

        internal void TestMatch(ResType rt)
        {
            foreach (var kv in preds_)
            {
                if (!kv.Value(rt.GetType().GetField(kv.Key).GetValue(rt)))
                    return;
            }
            results_.Add(rt);
        }

        public IEnumerable<ResType> Apply(object input)
        {
            this.results_.Clear();

            IEnumerable inputprime = input as IEnumerable;
            if (inputprime != null)
            {
                foreach (object o in inputprime)
                {
                    gen.Visit(this, o, true);
                }
            }
            else
            {
                gen.Visit(this, input, true);
            }
            foreach (var e in results_)
            {
                yield return e;
            }
        }

        public void Add<T>(Func<T, bool> predicate) 
        {
            preds_.Add(predicate.Method.GetParameters()[0].Name, new Func<Object, bool>(x => predicate((T)x)));
        }

        #region IEnumerator methods (to support seq initializers)

        public IEnumerator<KeyValuePair<string, Func<object, bool>>> GetEnumerator()
        {
            return preds_.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return preds_.GetEnumerator();
        }

        #endregion
    }

    class Subst<ChildType> where ChildType : class
    {

        internal Func<object, object> subst_;
        internal Stack<parentstate> parents_;  

        public Subst()
        {
            this.parents_ = new Stack<parentstate>();
        }

        public void Apply<S, T>(object tree, Func<S, T> subst)
            where S : class, ChildType
            where T : class, ChildType
        {
            this.subst_ = new Func<object, object>(x => (x as S == null ? (T)null : subst(x as S)));
            this.parents_.Clear();
            VisitChildren(tree);
        }

        static internal ActionVisitor<Subst<ChildType>, object> gen = new ActionVisitor<Subst<ChildType>, object>()
        {
           (Subst<ChildType> subst, ChildType cur) => subst.CheckSubst(cur),
           (Subst<ChildType> subst, object cur)  => subst.VisitChildren(cur)
        };

        internal class parentstate
        {
            internal object parent;
            internal FieldInfo field;
            internal bool is_list;
            internal int pos;
        }

        internal void CheckSubst(ChildType cur)
        {
            parentstate ps = parents_.Peek();
            if (ps.parent == null)
                throw new ArgumentException("Can't substitute the root node");
            
            ChildType proposed = subst_(cur) as ChildType;

            // note: we only visit children if no substitution
            // is performed.
            if (proposed == null)
            {
                VisitChildren(cur);
            }
            else if (!ps.is_list) {
                ps.field.SetValue(ps.parent, proposed);
            }
            else if (ps.is_list)
            {
                IList thelist = (IList)ps.field.GetValue(ps.parent);
                thelist[ps.pos] = proposed; 
            }
        }

        internal void VisitChildren(object cur)
        {
            parents_.Push(new parentstate() { parent = cur, is_list = false });
            parentstate ps = parents_.Peek();
            foreach (var f in TreeInfo.ChildEntries(cur))
            {
                ps.field = f;
                gen.Visit(this, f.GetValue(cur), true);
            }
            ps.is_list = true;
            foreach (var f in TreeInfo.ChildListEntries(cur))
            {
                ps.field = f;
                ps.pos = 0;
                var enumerator = (System.Collections.IEnumerable)f.GetValue(cur);
                foreach (var c in enumerator)
                {
                    gen.Visit(this, c, true);
                    ++ps.pos;
                }
            }
            parents_.Pop();
        }
    }

    class SubstList<ChildType> where ChildType : class
    {

        internal Func<object, object> subst_;
        internal Stack<parentstate> parents_;

        public SubstList()
        {
            this.parents_ = new Stack<parentstate>();
        }

        public void Apply<S, T>(object tree, Func<S, IList<T> > subst)
            where S : class, T, ChildType
            where T : class
        {
            this.subst_ = new Func<object, object>(x => (x as S == null ? (IList<T>)null : subst(x as S)));
            this.parents_.Clear();
            VisitChildren(tree);
        }

        static internal ActionVisitor<SubstList<ChildType>, object> gen = new ActionVisitor<SubstList<ChildType>, object>()
        {
           (SubstList<ChildType> subst, ChildType cur) => subst.CheckSubst(cur),
           (SubstList<ChildType> subst, object cur)  => subst.VisitChildren(cur)
        };

        internal class parentstate
        {
            internal object parent;
            internal FieldInfo field;
            internal bool is_list;
            internal int pos;
        }

        internal void CheckSubst(ChildType cur)
        {
            parentstate ps = parents_.Peek();
            if (ps.parent == null)
                throw new ArgumentException("Can't substitute the root node");

            IList proposed = (IList)subst_(cur);

            // note: we only visit children if no substitution
            // is performed.
            if (proposed == null)
            {
                VisitChildren(cur);
            }
            else if (!ps.is_list && proposed.Count == 1)
            {
                ps.field.SetValue(ps.parent, proposed[0]);
            }
            else if (ps.is_list)
            {
                IList thelist = (IList)ps.field.GetValue(ps.parent);
                thelist.RemoveAt(ps.pos);
                for (int i = 0; i < proposed.Count; ++i)
                {
                    thelist.Insert(ps.pos + i, proposed[i]); // may throw
                }
            }
        }

        internal void VisitChildren(object cur)
        {
            parents_.Push(new parentstate() { parent = cur, is_list = false });
            parentstate ps = parents_.Peek();
            foreach (var f in TreeInfo.ChildEntries(cur))
            {
                ps.field = f;
                gen.Visit(this, f.GetValue(cur), true);
            }
            ps.is_list = true;
            foreach (var f in TreeInfo.ChildListEntries(cur))
            {
                ps.field = f;
                ps.pos = 0;
                var enumerator = (System.Collections.IList)f.GetValue(cur);
                for (ps.pos = 0; ps.pos < enumerator.Count; ++ps.pos)
                {
                    gen.Visit(this, enumerator[ps.pos], true);
                }
            }
            parents_.Pop();
        }
    }

}
