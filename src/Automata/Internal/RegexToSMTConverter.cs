using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Automata
{
    internal class RegexToSMTConverter
    {
        RegexToAutomatonConverter<BDD> automConverter;
        CharSetSolver css;
        public CharSetSolver Solver { get { return css; } }
        char maxChar;
        public RegexToSMTConverter(BitWidth encoding)
        {
            css = new CharSetSolver(encoding);
            automConverter = css.RegexConverter;
            maxChar = (encoding == BitWidth.BV16 ? '\uFFFF' :
                (encoding == BitWidth.BV8 ? '\u00FF' : '\u007F'));
            CHAR = string.Format("(_ BitVec {0})", (int)encoding);
        }

        public RegexToSMTConverter(BitWidth encoding, string charSortAlias)
        {
            css = new CharSetSolver(encoding);
            automConverter = css.RegexConverter;
            maxChar = (encoding == BitWidth.BV16 ? '\uFFFF' :
                (encoding == BitWidth.BV8 ? '\u00FF' : '\u007F'));
            CHAR = charSortAlias;
        }

        string CHAR;

        Action<string> Write; 

        /// <summary>
        /// Convert a .Net regex to equivalent SMT lib format expression as a string
        /// </summary>
        /// <param name="regex">the given .NET regex pattern</param>
        public string ConvertRegex(string regex)
        {
            var sb = new StringBuilder();
            this.Write = ((string s) => { sb.Append(s); return; });
            RegexTree tree = RegexParser.Parse(regex, RegexOptions.None);
            ConvertNode(tree._root);
            string res = sb.ToString();
            Write = null;
            return res;
        }

        /// <summary>
        /// Convert a string to equivalent SMT lib format expression as a sequence of characters.
        /// </summary>
        /// <param name="seq">given string that denotes a sequence of characters</param>
        public string ConvertSeq(string seq)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < seq.Length; i++)
            {
                sb.Append("(seq-cons ");
                sb.Append(EscapeCharSMT(seq[i]));
                sb.Append(" ");
            }
            sb.Append(string.Format("(as seq-empty (Seq {0}))", CHAR));
            for (int i = 0; i < seq.Length; i++)
                sb.Append(")");
            return sb.ToString();
        }

        private void ConvertNode(RegexNode node)
        {
            switch (node._type)
            {
                case RegexNode.Alternate:
                    { ConvertNodeAlternate(node); return; }
                case RegexNode.Beginning:
                    { ConvertNodeBeginning(node); return; }
                case RegexNode.Bol:
                    { ConvertNodeBol(node); return; }
                case RegexNode.Capture:  // (...)
                    { ConvertNode(node.Child(0)); return; }
                case RegexNode.Concatenate:
                    { ConvertNodeConcatenate(node); return; }
                case RegexNode.Empty:
                    { ConvertNodeEmpty(node); return; }
                case RegexNode.End:
                    { ConvertNodeEnd(node); return; }
                case RegexNode.EndZ:
                    { ConvertNodeEndZ(node); return; }
                case RegexNode.Eol:
                    { ConvertNodeEol(node); return; }
                case RegexNode.Loop:
                    { ConvertNodeLoop(node); return; }
                case RegexNode.Multi:
                    { ConvertNodeMulti(node); return; }
                case RegexNode.Notone:
                    { ConvertNodeNotone(node); return; }
                case RegexNode.Notoneloop:
                    { ConvertNodeNotoneloop(node); return; }
                case RegexNode.One:
                    { ConvertNodeOne(node); return; }
                case RegexNode.Oneloop:
                    { ConvertNodeOneloop(node); return; }
                case RegexNode.Set:
                    { ConvertNodeSet(node); return; }
                case RegexNode.Setloop:
                    { ConvertNodeSetloop(node); return; }
                default:
                    throw new AutomataException(AutomataExceptionKind.RegexConstructNotSupported);
            }
        }

        private void ConvertNodeSetloop(RegexNode node)
        {
            var set = automConverter.CreateConditionFromSet(false, node._str);
            var ranges = css.ToRanges(set);
            int m = node._m;
            int n = node._n;
            string ran = GetSMTRanges(ranges);
            WriteLoop(ran, m, n);
        }

        private void ConvertNodeSet(RegexNode node)
        {
            var set = automConverter.CreateConditionFromSet(false, node._str);
            var ranges = css.ToRanges(set);
            Write(GetSMTRanges(ranges));
        }

        private string GetSMTRanges(IList<Tuple<uint, uint>> ranges)
        {
            string res = "";
            if (ranges.Count == 0)
                res = string.Format("(as re-empty-set (RegEx {0}))", CHAR);
            if (ranges.Count == 1)
                res = string.Format("(re-range {0} {1})", EscapeCharSMT((char)ranges[0].Item1), EscapeCharSMT((char)ranges[0].Item2));
            else
            {
                for (int i = 0; i < ranges.Count; i++)
                {
                    if (i < ranges.Count - 1)
                        res += "(re-union ";
                    else
                        res += " ";

                    res += string.Format("(re-range {0} {1})", EscapeCharSMT((char)ranges[i].Item1), EscapeCharSMT((char)ranges[i].Item2));
                }
                for (int i = 0; i < ranges.Count - 1; i++)
                    res += ")";
            }
            return res;
        }

        //loop with a singleton set
        private void ConvertNodeOneloop(RegexNode node)
        {
            //TBD:ignore case
            //bool ignoreCase = ((node._options & RegexOptions.IgnoreCase) != 0);
            char c = node._ch;
            string cond = string.Format("(re-range {0} {0})", EscapeCharSMT(c));
            WriteLoop(cond, node._m, node._n);
        }

        //loop with a negated singleton set
        private void ConvertNodeNotoneloop(RegexNode node)
        {
            //TBD:ignore case
            //bool ignoreCase = ((node._options & RegexOptions.IgnoreCase) != 0);
            char c = node._ch;
            string cond = NegateSingletonSet(c);
            WriteLoop(cond, node._m, node._n);
        }

        private string NegateSingletonSet(char c)
        {
            string cond = "";
            if (c == '\0')
                cond = string.Format("(re-range {0} {1})", EscapeCharSMT('\u0001'), EscapeCharSMT(maxChar));
            else if (c == maxChar)
                cond = string.Format("(re-range {0} {1})", EscapeCharSMT('\0'), EscapeCharSMT((char)(((int)maxChar) - 1)));
            else
            {
                string r1 = string.Format("(re-range {0} {1})", EscapeCharSMT('\0'), EscapeCharSMT((char)(((int)c) - 1)));
                string r2 = string.Format("(re-range {0} {1})", EscapeCharSMT((char)(((int)c) + 1)), EscapeCharSMT(maxChar));
                cond = string.Format("(re-union {0} {1})", r1, r2);
            }
            return cond;
        }

        private void WriteLoop(string cond, int m, int n)
        {
            if (m == 1 && n == 1)                             //case: r{1,1} = r
                Write(cond);
            else if (m == 0 && n == 1)                        //case: ?
                Write(string.Format("(re-option {0})",cond));
            else if (m == 0 && n == int.MaxValue)             //case: *
                Write(string.Format("(re-star {0})",cond));
            else if (m == 1 && n == int.MaxValue)             //case: + 
                Write(string.Format("(re-plus {0})",cond));
            else if (n == int.MaxValue)                       //case {m,}
                Write(string.Format("(re-concat ((_ re-loop {0} {0}) {1}) (re-star {1}))", m, cond));
            else                                              //case {m,n} 
                Write(string.Format("((_ re-loop {0} {1}) {2})", m, n, cond));
        }

        // Matches only node._ch (singleton set)
        private void ConvertNodeOne(RegexNode node)
        {
            //TBD: ignore case
            //bool ignoreCase = ((node._options & RegexOptions.IgnoreCase) != 0);
            char c = node._ch;
            Write(string.Format("(re-range {0} {0})", EscapeCharSMT(c))); 
        }

        //complement of the singleton set
        private void ConvertNodeNotone(RegexNode node)
        {
            //TBD: ignore case
            //bool ignoreCase = ((node._options & RegexOptions.IgnoreCase) != 0);
            char c = node._ch;
            string cond = NegateSingletonSet(c);
            Write(cond); 
        }

        //explicit string as a regex
        private void ConvertNodeMulti(RegexNode node)
        {
            //given sequence of characters
            string sequence = node._str;
            int count = sequence.Length;

            //TBD:
            //bool ignoreCase = ((node._options & RegexOptions.IgnoreCase) != 0);
            Write(string.Format("(re-of-seq {0})", ConvertSeq(sequence))); 
        }

        //loop constructs
        private void ConvertNodeLoop(RegexNode node)
        {
            var child = node._children[0];
            int m = node._m;
            int n = node._n;
            if (m == 1 && n == 1) //trivial case: r{1,1} = r
            {
                ConvertNode(child);
            }
            else if (m == 0 && n == 1) //case: ?
            {
                Write("(re-option ");
                ConvertNode(child);
                Write(")");
            }
            else if (m == 0 && n == int.MaxValue) //case: *
            {
                Write("(re-star ");
                ConvertNode(child);
                Write(")");
            }
            else if (m == 1 && n == int.MaxValue) //case: + 
            {
                Write("(re-plus ");
                ConvertNode(child);
                Write(")");
            }
            else if (n == int.MaxValue) //case {m,}
            {
                Write(string.Format("(re-concat ((_ re-loop {0} {0}) ",m));
                ConvertNode(child);
                Write(") (re-star ");
                ConvertNode(child);
                Write("))");
            }
            else //general case {m,n} 
            {
                Write(string.Format("((_ re-loop {0} {1}) ", m, n));
                ConvertNode(child);
                Write(")");
            }
        }

        //end anchors
        private void ConvertNodeEol(RegexNode node)
        {
            Write(ReEnd);
        }

        private void ConvertNodeEndZ(RegexNode node)
        {
            Write(ReEnd);
        }

        private void ConvertNodeEnd(RegexNode node)
        {
            Write(ReEnd);
        }

        //empty regex
        private void ConvertNodeEmpty(RegexNode node)
        {
            Write(ReEmpty);
        }

        string ReEnd { get { return string.Format("(as re-end (RegEx {0}))", CHAR); } }
        string ReEmpty { get { return string.Format("(as re-empty-seq (RegEx {0}))", CHAR); } }

        //concatenation
        private void ConvertNodeConcatenate(RegexNode node)
        {
            var children = node._children;
            if (children.Count == 1)
                ConvertNode(children[0]);
            else
            {
                for (int i = 0; i < children.Count; i++)
                {
                    if (i < children.Count-1)
                        Write("(re-concat ");
                    else
                        Write(" ");

                    ConvertNode(children[i]);
                }
                for (int i = 0; i < children.Count - 1; i++)
                    Write(")");
            }
        }

        //start anchors
        private void ConvertNodeBol(RegexNode node)
        {
            Write(ReBegin);
        }

        private void ConvertNodeBeginning(RegexNode node)
        {
            Write(ReBegin);
        }

        string ReBegin { get { return string.Format("(as re-begin (RegEx {0}))", CHAR); } }

        //union
        private void ConvertNodeAlternate(RegexNode node)
        {
            var children = node._children;
            if (children.Count == 1)
                ConvertNode(children[0]);
            else
            {
                for (int i = 0; i < children.Count; i++)
                {
                    if (i < children.Count - 1)
                        Write("(re-union ");
                    else
                        Write(" ");

                    ConvertNode(children[i]);
                }
                for (int i = 0; i < children.Count - 1; i++)
                    Write(")");
            }
        }

        #region SMT specific escaping
        /// <summary>
        /// Escape a character for SMT lib converter.
        /// </summary>
        string EscapeCharSMT(char c)
        {
            int code = (int)c;
            //if (code < 126 && char.IsLetterOrDigit(c))
            //    return c.ToString();
            //else
            if (Solver.Encoding == BitWidth.BV16)
                return ToBitVectorRepr16(code);
            else if (Solver.Encoding == BitWidth.BV8)
                return ToBitVectorRepr8(code);
            else if (Solver.Encoding == BitWidth.BV7)
                return ToBitVectorRepr7(code);
            else
                throw new NotImplementedException("Character representation not implemented for:" + Solver.Encoding);
        }

        static string ToBitVectorRepr16(int i)
        {
            string s = string.Format("{0:X}", i);
            if (s.Length == 1)
                s = "#x000" + s;
            else if (s.Length == 2)
                s = "#x00" + s;
            else if (s.Length == 3)
                s = "#x0" + s;
            else
                s = "#x" + s;
            return s;
        }

        static string ToBitVectorRepr8(int i)
        {
            string s = string.Format("{0:X}", i);
            if (s.Length == 1)
                s = "#x0" + s;
            else
                s = "#x" + s;
            return s;
        }

        static string ToBitVectorRepr7(int i)
        {
            int bit0 = ((i & 1) == 0 ? 0 : 1);
            int bit1 = ((i & 2) == 0 ? 0 : 1);
            int bit2 = ((i & 4) == 0 ? 0 : 1);
            int bit3 = ((i & 8) == 0 ? 0 : 1);
            int bit4 = ((i & 16) == 0 ? 0 : 1);
            int bit5 = ((i & 32) == 0 ? 0 : 1);
            int bit6 = ((i & 64) == 0 ? 0 : 1);
            string s = string.Format("#b{6}{5}{4}{3}{2}{1}{0}", bit0, bit1, bit2, bit3, bit4, bit5, bit6);
            return s;
        }
        #endregion
    }
}
