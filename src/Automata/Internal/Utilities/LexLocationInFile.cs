using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QUT.Gppg
{
    /// <summary>
    /// Lexical location extended with optional file name info.
    /// </summary>
    public class LexLocationInFile : LexLocation, IMerge<LexLocationInFile>
    {
        string file;
        /// <summary>
        /// Default no-arg constructor.
        /// </summary>
        public LexLocationInFile()
            : base()
        { }

        /// <summary>
        /// Source file of the location
        /// </summary>
        public string File
        {
            get
            {
                return (file == null ? "" : file);
            }
        }

        public LexLocationInFile(int sl, int sc, int el, int ec, string file) :
            base(sl, sc, el, ec)
        { this.file = file; }

        public LexLocationInFile Merge(LexLocationInFile last)
        {
            return new LexLocationInFile(this.StartLine, this.StartColumn, last.EndLine, last.EndColumn, file);
        }

        public static LexLocationInFile operator +(LexLocationInFile loc1, LexLocationInFile loc2)
        {
            return loc1.Merge(loc2);
        }

        public override string ToString()
        {
            return string.Format("{4}({0},{1},{2},{3})", StartLine, StartColumn, EndLine, EndColumn, (file == null ? "" : file));
        }
    }
}
