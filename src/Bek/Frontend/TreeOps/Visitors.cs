using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bek.Frontend.TreeOps
{
    public class SimpleVisitor<ParentType, ReturnType> : IEnumerable<Func<ParentType, ReturnType>>
    {
        internal Dictionary<Type, Func<ParentType, ReturnType>> handlers;

        public SimpleVisitor()
        {
            handlers = new Dictionary<Type, Func<ParentType, ReturnType>>();
        }

        public ReturnType Visit(ParentType pti, bool subclasses = false)
        {
            Func<ParentType, ReturnType> curhandler;
            if (handlers.TryGetValue(pti.GetType(), out curhandler))
            {
                return curhandler(pti);
            }
            else if (subclasses)
            {
                foreach (var kv in handlers)
                {
                    if (kv.Key.IsAssignableFrom(pti.GetType()))
                    {
                        return kv.Value(pti);
                    }
                }
            }
            throw new ArgumentException();
        }

        public void Add<T>(Func<T, ReturnType> f) where T : ParentType
        {
            handlers[typeof(T)] = new Func<ParentType, ReturnType>(x => f((T)x));
        }

        #region IEnumerator methods (to support seq initializers)

        public IEnumerator<Func<ParentType, ReturnType>> GetEnumerator()
        {
            return handlers.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return handlers.Values.GetEnumerator();
        }

        #endregion
    }

    public class FuncVisitor<ReceiverType, ParentType, ReturnType> : IEnumerable<Func<ReceiverType, ParentType, ReturnType>>
    {
                
        internal Dictionary<Type, Func<ReceiverType, ParentType, ReturnType>> handlers;
        internal List<KeyValuePair<Type, Func<ReceiverType, ParentType, ReturnType>>> handlerlist;

        public FuncVisitor()
        {
            handlers = new Dictionary<Type, Func<ReceiverType, ParentType, ReturnType>>();
            handlerlist = new List<KeyValuePair<Type, Func<ReceiverType, ParentType, ReturnType>>>();
        }

        public ReturnType Visit(ReceiverType rti, ParentType pti, bool subclasses=false)
        {
            Func<ReceiverType, ParentType, ReturnType> curhandler;
            if (handlers.TryGetValue(pti.GetType(), out curhandler))
            {
                return curhandler(rti, pti);
            }
            else if (subclasses)
            {
                foreach (var kv in handlerlist)
                {
                    if (kv.Key.IsAssignableFrom(pti.GetType()))
                    {
                        return kv.Value(rti, pti);
                    }
                }
            }
            throw new ArgumentException();
        }

        public void Add<S, T>(Func<S, T, ReturnType> f)
            where T : ParentType
            where S : ReceiverType
        {
            var wrappedf = new Func<ReceiverType, ParentType, ReturnType>((v, x) => f((S)v, (T)x));
            handlers[typeof(T)] = wrappedf;
            handlerlist.Add(new KeyValuePair<Type, Func<ReceiverType, ParentType, ReturnType>>(typeof(T), wrappedf));
        }

        #region IEnumerator methods (to support seq initializers)

        public IEnumerator<Func<ReceiverType, ParentType, ReturnType>> GetEnumerator()
        {
            return handlers.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return handlers.Values.GetEnumerator();
        }

        #endregion
    }
    
    public class ActionVisitor<ReceiverType, ParentType> : IEnumerable<Action<ReceiverType, ParentType>>
    {
        internal Dictionary<Type, Action<ReceiverType, ParentType>> handlers;
        internal List<KeyValuePair<Type, Action<ReceiverType, ParentType>>> handlerlist;

        public ActionVisitor()
        {
            handlers = new Dictionary<Type, Action<ReceiverType, ParentType>>();
            handlerlist = new List<KeyValuePair<Type, Action<ReceiverType, ParentType>>>();
        }

        public void Visit(ReceiverType rti, ParentType pti, bool subclasses = false)
        {
            Action<ReceiverType, ParentType> curhandler;
            if (handlers.TryGetValue(pti.GetType(), out curhandler))
            {
                curhandler(rti, pti);
                return;
            }
            else if (subclasses)
            {
                foreach (var kv in handlerlist)
                {
                    if (kv.Key.IsAssignableFrom(pti.GetType()))
                    {
                        kv.Value(rti, pti);
                        return;
                    }
                }
            }
            throw new ArgumentException();
        }


        public void Visit(ReceiverType rti, ParentType pti)
        {
            handlers[pti.GetType()](rti, pti);
        }

        public void Add<S, T>(Action<S, T> f)
            where T : ParentType
            where S : ReceiverType
        {
            var wrappedf = new Action<ReceiverType, ParentType>((v, x) => f((S)v, (T)x));
            handlers[typeof(T)] = wrappedf;
            handlerlist.Add(new KeyValuePair<Type, Action<ReceiverType, ParentType>>(typeof(T), wrappedf));
        }

        #region IEnumerator methods (to support seq initializers)

        public IEnumerator<Action<ReceiverType, ParentType>> GetEnumerator()
        {
            return handlers.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return handlers.Values.GetEnumerator();
        }

        #endregion
    }
}
