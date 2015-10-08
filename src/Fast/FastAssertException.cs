using System;

namespace Microsoft.Fast
{
    public class FastAssertException : Exception
    {
        public int line;
        public int pos;
        public string file;

        public FastAssertException() : base() { }
        public FastAssertException(string message) : base(message) { }
        public FastAssertException(string message, System.Exception inner) : base(message, inner) { }

        public FastAssertException(string assertion, int line, int pos)
            : base(string.Format("ASSERTION FAILED: '{2}'", line, pos, assertion))
        {
            this.line = line;
            this.pos = pos;
            this.file = "";
        }
    }
}
