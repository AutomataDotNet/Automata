using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bek.Frontend.TreeOps
{
    [AttributeUsage(AttributeTargets.Field)]
    public class Child : System.Attribute
    { }

    [AttributeUsage(AttributeTargets.Field)]
    public class ChildList : System.Attribute
    { }

    [AttributeUsage(AttributeTargets.Field)]
    public abstract class AllowTypes : System.Attribute
    {
        public List<object> whichtypes;
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class MustMatchTag : System.Attribute
    {
        public MustMatchTag(string fieldname)
        {
            this.fieldname = fieldname;
        }

        public string fieldname;
    }
}
