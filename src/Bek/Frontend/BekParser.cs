using System;
using System.Collections.Generic;
using System.Text;

using System.Reflection;
using System.Globalization;

using Microsoft.Bek.Frontend.ParserImpl;
using Microsoft.Bek.Frontend.TreeOps;
using Microsoft.Bek.Frontend.AST;
using Microsoft.Bek.Frontend.Meta;

namespace Microsoft.Bek.Frontend
{
    public class BekProgram
    {
        public BekProgram(BekPgm ast, Symtab tab) {
            this.ast = ast;
            this.stab = tab;
        }
         
        public BekPgm ast;
        public Symtab stab;
    }

    public class BekParser
    {

        internal static string ExtractLine(string str, int line)
        {
            string[] splits = str.Split('\n');
            return splits[line].Replace("\r", "");
        }

        public static string ErrorDisplay(BekException e, string str, int line, int col)
        {
            string lineone = ExtractLine(str, line - 1);
            string linetwo = "".PadLeft(col) + "^";
            return lineone + "\n" + linetwo;
        }

        internal static BekPgm ParseFromString(string str)
        {
            try
            {
                var input = new Antlr.Runtime.ANTLRStringStream(str);
                var lexer = new bekLexer(input);
                var tokens = new Antlr.Runtime.CommonTokenStream(lexer);
                var parser = new bekParser(tokens);
                return parser.BekPgms()[0];
            }
            catch (Antlr.Runtime.MismatchedTokenException e)
            {
                string tok = (e.Token != null ? "'" + e.Token.Text + "'" : (e.Character >= 0 ? Microsoft.Automata.StringUtility.Escape((char)e.Character) : ""));
                string msg = "unexpected token " + tok;
                if (tok != "" && 0 <= e.Expecting && e.Expecting < ParserImpl.bekParser.tokenNames.Length)
                    msg += string.Format(" expecting {0}", ParserImpl.bekParser.tokenNames[e.Expecting]);
                throw new BekParseException(e.Line, e.CharPositionInLine, msg);
            }
            catch (Antlr.Runtime.FailedPredicateException e)
            {
                string msg = string.Format("unexpected '{0}' failed {1}", (e.Token != null ? e.Token.Text : ((char)e.Character).ToString()), e.PredicateText);
                throw new BekParseException(e.Line, e.CharPositionInLine, msg);
            }
            catch (Antlr.Runtime.NoViableAltException e)
            {
                string msg = string.Format("unexpected '{0}' no alternatives", (e.Token != null ? e.Token.Text : ((char)e.Character).ToString()));
                throw new BekParseException(e.Line, e.CharPositionInLine, msg);
            }
            catch (Antlr.Runtime.RecognitionException e)
            {
                string msg = string.Format("unexpected '{0}'", (e.Token != null ? e.Token.Text : ((char)e.Character).ToString()));
                throw new BekParseException(e.Line, e.CharPositionInLine, msg);
            }
            catch (BekParseException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw new BekParseException(1, 1, e.Message);
            }
        }

        internal static expr ParseExprFromString(string str)
        {
            try
            {
                var input = new Antlr.Runtime.ANTLRStringStream(str);
                var lexer = new bekLexer(input);
                var tokens = new Antlr.Runtime.CommonTokenStream(lexer);
                var parser = new bekParser(tokens);
                return parser.Comp_expr();
            }
            catch (Antlr.Runtime.MismatchedTokenException e)
            {
                string tok = (e.Token != null ? "'" + e.Token.Text + "'" : (e.Character >= 0 ? Microsoft.Automata.StringUtility.Escape((char)e.Character) : ""));
                string msg = "unexpected token " + tok;
                if (tok != "" && 0 <= e.Expecting && e.Expecting < ParserImpl.bekParser.tokenNames.Length)
                    msg += string.Format(" expecting {0}", ParserImpl.bekParser.tokenNames[e.Expecting]);
                throw new BekParseException(e.Line, e.CharPositionInLine, msg);
            }
            catch (Antlr.Runtime.FailedPredicateException e)
            {
                string msg = string.Format("unexpected '{0}' failed {1}", (e.Token != null ? e.Token.Text : ((char)e.Character).ToString()), e.PredicateText);
                throw new BekParseException(e.Line, e.CharPositionInLine, msg);
            }
            catch (Antlr.Runtime.NoViableAltException e)
            {
                string msg = string.Format("unexpected '{0}' no alternatives", (e.Token != null ? e.Token.Text : ((char)e.Character).ToString()));
                throw new BekParseException(e.Line, e.CharPositionInLine, msg);
            }
            catch (Antlr.Runtime.RecognitionException e)
            {
                string msg = string.Format("unexpected '{0}'", (e.Token != null ? e.Token.Text : ((char)e.Character).ToString()));
                throw new BekParseException(e.Line, e.CharPositionInLine, msg);
            }
            catch (BekParseException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw new BekParseException(1, 1, e.Message);
            }
        }

        public static BekProgram BekFromString(string program)
        {
            var tree = ParseFromString(program);
            var stab = new Symtab(tree);
            //TypeChecker.TypeCheck(tree, stab);
            var res = new BekProgram(tree, stab);
            return res;
        }

        public static List<BekProgram> DefsFromString(string programs)
        {
            var res = new List<BekProgram>();
            try
            {
                var input = new Antlr.Runtime.ANTLRStringStream(programs);
                var lexer = new bekLexer(input);
                var tokens = new Antlr.Runtime.CommonTokenStream(lexer);
                var parser = new bekParser(tokens);

                var resp = parser.BekPgms();
                foreach (BekPgm cur in resp)
                {
                    var stab = new Symtab(cur);
                    //TypeChecker.TypeCheck(cur, stab);
                    var p = new BekProgram(cur, stab);
                    //Library.PerformExpansions(p);
                    res.Add(p);
                }
            }
            catch (Antlr.Runtime.MismatchedTokenException e)
            {
                string tok = (e.Token != null ? "'" + e.Token.Text + "'" : (e.Character >= 0 ? Microsoft.Automata.StringUtility.Escape((char)e.Character) : ""));
                string msg = "unexpected token " + tok;
                if (tok != "" && 0 <= e.Expecting && e.Expecting < ParserImpl.bekParser.tokenNames.Length)
                    msg += string.Format(" expecting {0}", ParserImpl.bekParser.tokenNames[e.Expecting]);
                throw new BekParseException(e.Line, e.CharPositionInLine, msg);
            }
            catch (Antlr.Runtime.FailedPredicateException e)
            {
                string msg = string.Format("unexpected '{0}' failed {1}", (e.Token != null ? e.Token.Text : ((char)e.Character).ToString()), e.PredicateText);
                throw new BekParseException(e.Line, e.CharPositionInLine, msg);
            }
            catch (Antlr.Runtime.NoViableAltException e)
            {
                string msg = string.Format("unexpected '{0}' no alternatives", (e.Token != null ? e.Token.Text : ((char)e.Character).ToString()));
                throw new BekParseException(e.Line, e.CharPositionInLine, msg);
            }
            catch (Antlr.Runtime.RecognitionException e)
            {
                string msg = string.Format("unexpected '{0}'", (e.Token != null ? e.Token.Text : ((char)e.Character).ToString()));
                throw new BekParseException(e.Line, e.CharPositionInLine, msg);
            }
            catch (BekParseException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw new BekParseException(1, 1, e.Message);
            }
            return res;
        }

        public static expr ExprFromString(string exp0)
        {
            var expr1 = ParseExprFromString(exp0);
            return expr1;
        }
    }
}

