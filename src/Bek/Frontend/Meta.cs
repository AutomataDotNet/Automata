using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Bek.Frontend.AST;
using Microsoft.Bek.Frontend.TreeOps;

namespace Microsoft.Bek.Frontend.Meta
{
    public enum BekCharModes
    {
        //Unicode: code points are always < 2^16; all others are addressed using surrogate pairs as in .NET
        //BV32: only valid code points; surrogate pairs are consumed eagerly and converted to their target code point

        /// <summary>
        /// 7 bit ASCII encoding
        /// </summary>
        BV7 = 7,
        /// <summary>
        /// 8 bit Extended ASCII encoding
        /// </summary>
        BV8 = 8,
        /// <summary>
        /// 16 bit bit-vector encoding
        /// </summary>
        BV16 = 16,
        /// <summary>
        /// 32 bit bit-vector encoding
        /// </summary>
        BV32 = 32
    }

    public enum BekTypes
    {
        CHAR,
        STR,
        BOOL,
        ANY
    }

    public class AllowBekTypes : AllowTypes
    {
        public AllowBekTypes(params BekTypes[] allowed)
        {
            this.whichtypes = new List<object>();
            foreach (var b in allowed)
            {
                whichtypes.Add(b);
            }
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(typeof(AllowBekTypes).Name);
            sb.Append("(");
            for (int i = 0; i < whichtypes.Count; i++)
            {
                if (i > 0)
                    sb.Append(",");
                sb.Append(whichtypes[i].ToString());
            }
            sb.Append(")");
            return sb.ToString();
        }
    }

    public class BekException : Exception
    {
        public BekException() : base() { }
        public BekException(string message) : base(message) { }
        public BekException(string message, System.Exception inner) : base(message, inner) { }
    }

    public class BekParseException : BekException {
        public BekParseException() : base() { }
        public BekParseException(string message) : base(message) { }
        public BekParseException(string message, System.Exception inner) : base(message, inner) { }
        public BekParseException(int line, int pos, Exception inner) : base("", inner) {
            this.line = line;
            this.pos = pos;
        }
        public BekParseException(int line, int pos, string message)
            : base(message)
        {
            this.line = line;
            this.pos = pos;
        }

        public int line;
        public int pos;
    }
}
