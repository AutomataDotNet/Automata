using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using QUT.Gppg;

namespace Microsoft.Automata.MSO.Mona
{
    [Serializable]
    public class MonaParseException : Exception
    {
        internal LexLocation location = null;
        string _file = null;

        public int StartLine
        {
            get { return (location == null ? 0 : location.StartLine); }
        }
        public int StartColumn
        {
            get { return (location == null ? 0 : location.StartColumn + 1); }
        }
        public int EndLine
        {
            get { return (location == null ? 0 : location.EndLine); }
        }
        public int EndColumn
        {
            get { return (location == null ? 0 : location.EndColumn + 1); }
        }

        public bool HasLocationInfo
        {
            get { return location != null; }
        }

        internal MonaParseException()
            : base()
        {
        }
        internal MonaParseException(string message, string file = null)
            : base(message)
        {
            this._file = file;
        }
        internal MonaParseException(string message, System.Exception inner)
            : base(message, inner)
        {
        }

        internal MonaParseException(LexLocation location, string message, string file = null)
            : base(message)
        {
            this.location = location;
            this._file = file;
        }

        public override string ToString()
        {
            string res = "";
            if (location != null)
                res += string.Format("({0},{1},{2},{3})", StartLine, StartColumn, EndLine, EndColumn);
            if (res != "")
                res += ": ";
            res += "error : " + Message;
            return res;
        }
    }
}

