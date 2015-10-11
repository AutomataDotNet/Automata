using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Microsoft.Bek.Frontend.TreeOps
{
    class TreeInfo
    {
        static internal IEnumerable<FieldInfo> ChildEntries(Object o)
        {
            foreach (var f in o.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (f.GetCustomAttributes(typeof(Child), false).Length > 0)
                {
                    yield return f;
                }

            }
        }

        static internal IEnumerable<FieldInfo> ChildListEntries(object o)
        {
            foreach (var f in o.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (f.GetCustomAttributes(typeof(ChildList), false).Length > 0)
                {
                    yield return f;
                }
            }
        }

        static internal IEnumerable<FieldInfo> NonChildEntries(Object o)
        {
            foreach (var f in o.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (f.GetCustomAttributes(typeof(Child), false).Length == 0)
                {
                    yield return f;
                }
            }
        }


        static internal IEnumerable<Object> AllChildValues(Object o)
        {
            foreach (var f in o.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (f.GetCustomAttributes(typeof(Child), false).Length > 0)
                {
                    yield return f.GetValue(o);
                }
                else if (f.GetCustomAttributes(typeof(ChildList), false).Length > 0)
                {
                    // Type childtype = f.GetValue(o).GetType().GetGenericArguments()[0];
                    var enumerator = (System.Collections.IEnumerable)f.GetValue(o);
                    foreach (var elt in enumerator)
                    {
                        yield return elt;
                    }
                }
            }
        }
    }
}
