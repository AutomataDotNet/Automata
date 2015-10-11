using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Microsoft.Bek.Frontend.TreeOps
{
    public class TagException : Exception
    {
        public TagException() : base() { }
        public TagException(string message) : base(message) { }
        public TagException(string message, System.Exception inner) : base(message, inner) { }
        public TagException(string f1, string type, string actual) :
            base(String.Format("Tag mismatch: {0}.{1} cannot have type {2} ", type, f1, actual))
        {
        }
    }

    public class TagMismatchException : TagException
    {
        public TagMismatchException() : base() { }
        public TagMismatchException(string message) : base(message) { }
        public TagMismatchException(string message, System.Exception inner) : base(message, inner) { }
        public TagMismatchException(string f1, string f2, string type) :
            base(String.Format("Tag mismatch: {0}.{1} and {0}.{2} do not have matching type tags", type, f1, f2))
        {
        }
    }

    class TagChecker
    {
        public static void Check(object o, Func<object, object> gettype)
        {

            foreach (var f in TreeInfo.ChildEntries(o))
            {
                object v = f.GetValue(o);
                AllowTypes[] annots = (AllowTypes[])f.GetCustomAttributes(typeof(AllowTypes), false);
                bool found = annots.Length == 0;

                for (int i = 0; i < annots.Length; ++i)
                {
                    var curlist = annots[i].whichtypes;
                    for (int j = 0; j < curlist.Count; ++j)
                    {
                        var t = gettype(v);
                        if (curlist[j].Equals(t))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found) break;
                }

                if (!found)
                    throw new TagException(f.Name, o.GetType().Name, gettype(v).ToString());

                MustMatchTag[] constraints = (MustMatchTag[])f.GetCustomAttributes(typeof(MustMatchTag), false);
                if (constraints.Length == 1) {
                    var f2 = o.GetType().GetField(constraints[0].fieldname);
                    var v2 = f2.GetValue(o);
                    if (!gettype(v).Equals(gettype(v2))) {
                        throw new TagMismatchException(f.Name, f2.Name, o.GetType().Name);
                    }
                }
                else if (constraints.Length > 1) {
                    throw new ArgumentOutOfRangeException();
                }

                Check(v, gettype);
            }

            foreach (var f in TreeInfo.ChildListEntries(o))
            {
                object v = f.GetValue(o);
                AllowTypes[] annots = (AllowTypes[])f.GetCustomAttributes(typeof(AllowTypes), false);
                
                var enumerator = (System.Collections.IEnumerable)f.GetValue(o);

                foreach (var elt in enumerator)
                {
                    bool found = annots.Length == 0;
                    for (int i = 0; i < annots.Length; ++i)
                    {
                        var curlist = annots[i].whichtypes;
                        for (int j = 0; j < curlist.Count; ++j)
                        {
                            if (curlist[j].Equals(gettype(elt)))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found) break;
                    }

                    if (!found)
                        throw new TagException(f.Name, o.GetType().Name, gettype(elt).ToString());

                    Check(elt, gettype);
                }
            }
        }
    }
}
