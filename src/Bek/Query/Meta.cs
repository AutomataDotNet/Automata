using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Bek.Query
{
    public class QueryException : Exception
    {
        public QueryException() : base() { }
        public QueryException(string message) : base(message) { }
        public QueryException(string message, System.Exception inner) : base(message, inner) { }
    }


    public class QueryParseException : QueryException
    {
        public QueryParseException() : base() { }
        public QueryParseException(string message) : base(message) { }
        public QueryParseException(string message, System.Exception inner) : base(message, inner) { }
        public QueryParseException(int line, int pos, System.Exception inner)
            : base("", inner)
        {
            this.line = line;
            this.pos = pos;
        }
        public QueryParseException(int line, int pos, string msg)
            : base(msg)
        {
            this.line = line;
            this.pos = pos;
        }

        public int line;
        public int pos;
    }
}
