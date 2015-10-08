using System;
using Microsoft.Fast.AST;

namespace Microsoft.Fast
{
    [Serializable]
    public class FastParseException : Exception
    {
        internal FastLexLocation location = null;
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

        public string File
        {
            get
            {
                if (_file != null)
                    return _file;
                else
                    return location.File;
            }
        }

        internal FastParseException()
            : base()
        {
        }
        internal FastParseException(string message, string file = null)
            : base(message)
        {
            this._file = file;
        }
        internal FastParseException(string message, System.Exception inner)
            : base(message, inner)
        {
        }

        internal FastParseException(FastLexLocation location, string message, string file = null)
            : base(message)
        {
            this.location = location;
            this._file = file;
        }

        public override string ToString()
        {
            string res = "";
            if (File != null)
                res += File;
            if (location != null)
                res += string.Format("({0},{1},{2},{3})", StartLine, StartColumn, EndLine, EndColumn);
            if (res != "")
                res += ": ";
            res += "error : " + Message;
            return res;
        }
    }
}
