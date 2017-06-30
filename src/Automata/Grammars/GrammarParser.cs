using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Automata.Grammars
{
    internal enum TokenType { NT, T, ARR, OR, ERR, IG, EOS }
    
    internal class Token {
        public TokenType t;
        public string content;
        public int length;
        override public string ToString() {
            string type = "";

            switch (t) {
                case TokenType.NT:
                    type = "NonExprinal(";
                    break;
                case TokenType.T:
                    type = "Exprinal(";
                    break;
                case TokenType.ARR:
                    type = "ARROW(";
                    break;
                case TokenType.OR:
                    type = "OR(";
                    break;
                case TokenType.ERR:
                    type = "ERR(";
                    break;
                case TokenType.IG:
                    type = "IG(";
                    break;
                case TokenType.EOS:
                    type = "END(";
                    break;
            }

            return type + content + ")";
        }
    }

    internal class Lexer
    {
        private string lexbuf;
        private Dictionary<TokenType, Regex> tokendescs = new Dictionary<TokenType, Regex>();

        public Lexer(string buf)
        {
            lexbuf = buf;
            tokendescs[TokenType.NT]  = new Regex(@"^([A-Z][A-Z0-9]*)"); // NonExprinal
            tokendescs[TokenType.T]   = new Regex(@"^([a-z<>\[\]])");    // Exprinal
            tokendescs[TokenType.ARR] = new Regex(@"^->");               // Arrow
            tokendescs[TokenType.OR]  = new Regex(@"^\|");               // Or
            tokendescs[TokenType.IG]  = new Regex(@"^\s+");              // Ignorables
        }

        private Token DoMatch() {
            Token next = new Token();
            next.t = TokenType.ERR;
            next.length = 1;
            next.content = "";
                
            foreach (KeyValuePair<TokenType, Regex> pair in tokendescs) 
            {
                Match m = pair.Value.Match(lexbuf);
                if (m.Success) 
                {
                    next.length = m.Groups[0].Length;
                    next.t = pair.Key;
                        
                    if (m.Groups.Count > 0) 
                    {
                        next.content = m.Groups[1].Value;
                    }

                    break;
                }
             } // end loop over token types
             return next;
        }

        public Token Next() {
            foreach (Token t in GetTokens()) {
                return t;
            }

            return null;
        }

        public IEnumerable<Token> GetTokens() 
        {
            while (lexbuf.Length > 0) 
            {
                Token next = DoMatch();
                lexbuf = lexbuf.Substring(next.length);

                if (next.t == TokenType.ERR)
                {
                    yield return next;
                    yield break;
                }

                if (next.t != TokenType.IG)
                {
                    yield return next;
                }
            }
            Token end = new Token();
            end.t = TokenType.EOS;
            end.content = "";
            end.length = 0;

            yield return (end);
            yield break;
        }
    }

    class ParseException : System.ApplicationException { }

    internal class GrammarParser<T>
    {
        private Lexer lexer;
        private Func<char,T> mkExprinal;
        private Grammars.Nonterminal startvar;
        private List<Grammars.Production> productions;

        private GrammarParser(Lexer lex, Func<char, T> mkExprinal)
        {
            lexer = lex;
            this.mkExprinal = mkExprinal;
            startvar = null;
            productions = new List<Grammars.Production>();
        }

        public static Grammars.ContextFreeGrammar Parse(Func<char, T> mkExprinal, string buf)
        {
            Lexer lex = new Lexer(buf);
            var gp = new GrammarParser<T>(lex, mkExprinal);
            gp.Parse();
            Grammars.ContextFreeGrammar G = gp.GetGrammar();
            return G;
        }
        

        private Grammars.Nonterminal ExpectNT()
        {
            Token next = lexer.Next();
            if (next.t != TokenType.NT)
            {
                throw new ParseException();
            }

            return new Grammars.Nonterminal(next.content);
        }

        private void ExpectArrow()
        {
            Token next = lexer.Next();
            if (next.t != TokenType.ARR)
            {
                throw new ParseException();
            }
        }

        private void Parse()
        {
            bool done = false;
            Token cur = null;
            Token last = null;

            Grammars.Nonterminal curlhs = ExpectNT();
            startvar = curlhs;

            ExpectArrow();
            List<Grammars.GrammarSymbol> currhs = new List<Grammars.GrammarSymbol>();
            
            while (!done)
            {
                last = cur;
                cur = lexer.Next();

                switch (cur.t)
                {
                    case TokenType.NT:
                        currhs.Add(new Grammars.Nonterminal(cur.content));
                        break;
                    case TokenType.T:
                        currhs.Add(new Terminal<T>(mkExprinal(cur.content[0]),cur.content));
                        break;
                    case TokenType.OR:
                        productions.Add(new Grammars.Production(curlhs, currhs.ToArray()));
                        currhs.Clear();
                        break;
                    case TokenType.ARR:
                        if (currhs.Count < 1) {
                            throw new ParseException();
                        }
                        if (last.t != TokenType.NT) {
                            throw new ParseException();
                        }

                        // downcast :(
                        Grammars.Nonterminal newlhs = (Grammars.Nonterminal)currhs[currhs.Count -1];
                        currhs.RemoveAt(currhs.Count -1);
                        productions.Add(new Grammars.Production(curlhs, currhs.ToArray()));
                        currhs.Clear();
                        curlhs = newlhs;
                        break;
                    case TokenType.EOS:
                        productions.Add(new Grammars.Production(curlhs, currhs.ToArray()));
                        currhs.Clear();
                        done = true;
                        break;
                    default:
                        throw new ParseException();            
                }
            }
        }

        private Grammars.ContextFreeGrammar GetGrammar()
        {
            return new Grammars.ContextFreeGrammar(startvar, productions);
        }
    }
}
