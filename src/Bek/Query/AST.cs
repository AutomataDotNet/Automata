using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Bek.Frontend.TreeOps;
using Microsoft.Bek.Frontend.Meta;
using System.Globalization;

using STModel = Microsoft.Automata.ST<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;
using STbModel = Microsoft.Automata.STb<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;
using SFAModel = Microsoft.Automata.SFA<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;

using Microsoft.Automata.Z3;
using Microsoft.Automata;
using Microsoft.Z3;
using Microsoft.Bek.Frontend;
using Microsoft.Bek.Model;

namespace Microsoft.Bek.Query
{
    /// <summary>
    /// Different kinds of expressions
    /// </summary>
    public enum ExpressionKind
    {
        /// <summary>
        /// query expression
        /// </summary>
        Query,
        /// <summary>
        /// expression can be evaluated as an automaton
        /// </summary>
        Automaton,
        /// <summary>
        /// expression can be evaluated as a transducer
        /// </summary>
        Transducer,
        /// <summary>
        /// expression is a variable identifier
        /// </summary>
        Variable,
        /// <summary>
        /// expression represents an integer value
        /// </summary>
        Integer,
    }

    /// <summary>
    /// Name identifiers with line and character position information.
    /// </summary>
    public class Identifier
    {
        string name;
        /// <summary>
        /// Name of the identifier.
        /// </summary>
        public string Name
        {
            get { return name; }
        }
        int line;
        /// <summary>
        /// Line nr.
        /// </summary>
        public int Line
        {
            get { return line; }
        }
        int pos;
        /// <summary>
        /// Character position in the line.
        /// </summary>
        public int Pos
        {
            get { return pos; }
        }

        public override string ToString()
        {
            return name;
        }

        public Identifier(string name, int line, int pos)
        {
            this.name = name;
            this.line = line;
            this.pos = pos + 1;
        }

    }
    /// <summary>
    /// Generic expression class for creation and algebraic manipulation of transducers and automata.
    /// </summary>
    public class Expression 
    {
        /// <summary>
        /// Application symbol of the expression.
        /// </summary>
        internal Identifier symbol;

        ExpressionKind kind;

        /// <summary>
        /// Gets what kind of expression this is.
        /// </summary>
        public ExpressionKind Kind { get { return kind; } }

        /// <summary>
        /// Application symbol of the expression.
        /// </summary>
        public Identifier Symbol { get { return symbol; } }

        /// <summary>
        /// Number of immediate subexpressions.
        /// </summary>
        public int Arity { get { return subexpressions.Count; } }

        /// <summary>
        /// Get the i'th subexpression, i must be in 0..Arity-1.
        /// </summary>
        public Expression Subexpression(int i)
        {
            return subexpressions[i];
        }

        /// <summary>
        /// Immediate subexpressions of the expression.
        /// </summary>
        internal List<Expression> subexpressions;

        internal Expression(string symb, ExpressionKind kind, params Expression[] exprs)
        {
            this.symbol = new Identifier(symb,1,1) ;
            this.kind = kind;
            this.subexpressions = new List<Expression>(exprs);
        }

        internal Expression(Identifier symb, ExpressionKind kind, params Expression[] exprs)
        {
            this.symbol = symb;
            this.kind = kind;
            this.subexpressions = new List<Expression>(exprs);
        }

        public override string ToString()
        {
            string res = symbol.ToString();
            if (subexpressions.Count > 0)
            {
                res += "(" + subexpressions[0].ToString();
                for (int i = 1; i < subexpressions.Count; i++)
                    res += "," + subexpressions[i].ToString();
                res += ")";
            }
            return res;
        }

        internal virtual SFAModel GetSFA(EvaluationContext ec)
        {
            throw new QueryParseException(this.symbol.Line,this.symbol.Pos, string.Format("Invalid automaton expression: {0}.",this.ToString()));
        }

        internal virtual STModel GetST(EvaluationContext ec)
        {
            throw new QueryParseException(this.symbol.Line, this.symbol.Pos, string.Format("Invalid transducer expression: {0}.", this.ToString()));
        }

        internal virtual void RunQuery(EvaluationContext ec)
        {
            throw new QueryParseException(this.symbol.Line, this.symbol.Pos, string.Format("Invalid query expression: {0}.", this.ToString()));
        }

        internal virtual string GetString(EvaluationContext ec)
        {
            throw new QueryParseException(this.symbol.Line, this.symbol.Pos, string.Format("Invalid string expression: {0}.", this.ToString()));
        }

        internal bool IsSFA(EvaluationContext ec)
        {
            if (kind == ExpressionKind.Variable)
                return ec.sfaMap.ContainsKey(symbol.Name);
            else
                return kind == ExpressionKind.Automaton;
        }

        /// <summary>
        /// Parse a sequence of queries. 
        /// Eeach query in the sequence must end with a ';'.
        /// Output also the individual query strings.
        /// </summary>
        public static List<Expression> ParseQueries(string query_sequence, out List<string> query_strings)
        {
            try
            {
                var input = new Antlr.Runtime.ANTLRStringStream(query_sequence);
                var lexer = new queryLexer(input);
                var tokens = new Antlr.Runtime.CommonTokenStream(lexer);
                var parser = new queryParser(tokens);
                var res = parser.Queries();
                query_strings = res.Item2;
                return res.Item1;
            }
            catch (Antlr.Runtime.MismatchedTokenException e)
            {

                string tok = (e.Token != null ? "'" + e.Token.Text + "'" : (e.Character >= 0 ? StringUtility.Escape((char)e.Character) : ""));
                string msg = "unexpected token " + tok;
                if (tok != "" && 0 <= e.Expecting && e.Expecting < queryParser.tokenNames.Length)
                    msg += string.Format(" expecting {0}", queryParser.tokenNames[e.Expecting]);
                throw new QueryParseException(e.Line, e.CharPositionInLine + 1, msg);
            }
            catch (Antlr.Runtime.FailedPredicateException e)
            {
                string msg = string.Format("unexpected '{0}' failed {1}", (e.Token != null ? e.Token.Text : ((char)e.Character).ToString()), e.PredicateText);
                throw new QueryParseException(e.Line, e.CharPositionInLine + 1, msg);
            }
            catch (Antlr.Runtime.NoViableAltException e)
            {
                string msg = string.Format("unexpected '{0}' no alternatives", (e.Token != null ? e.Token.Text : ((char)e.Character).ToString()));
                throw new QueryParseException(e.Line, e.CharPositionInLine + 1, msg);
            }
            catch (Antlr.Runtime.RecognitionException e)
            {
                string msg = string.Format("unexpected '{0}'", (e.Token != null ? e.Token.Text : ((char)e.Character).ToString()));
                throw new QueryParseException(e.Line, e.CharPositionInLine + 1, msg);
            }
            catch (QueryParseException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw new QueryParseException(1, 1, e.Message);
            }
        }

        /// <summary>
        /// Parse a single query expression.
        /// </summary>
        public static Expression ParseQuery(string query)
        {
            try
            {
                var input = new Antlr.Runtime.ANTLRStringStream(query);
                var lexer = new queryLexer(input);
                var tokens = new Antlr.Runtime.CommonTokenStream(lexer);
                var parser = new queryParser(tokens);
                var res = parser.Query();
                return res;
            }
            catch (Antlr.Runtime.MismatchedTokenException e)
            {
                string tok = (e.Token != null ? "'" + e.Token.Text + "'" : (e.Character >= 0 ? Microsoft.Automata.StringUtility.Escape((char)e.Character) : ""));
                string msg = "unexpected token " + tok;
                if (tok !="" && 0 <= e.Expecting && e.Expecting < queryParser.tokenNames.Length)
                    msg += string.Format(" expecting {0}", queryParser.tokenNames[e.Expecting]);
                throw new QueryParseException(e.Line, e.CharPositionInLine + 1, msg);
            }
            catch (Antlr.Runtime.FailedPredicateException e)
            {
                string msg = string.Format("unexpected '{0}' failed {1}", (e.Token != null ? e.Token.Text : ((char)e.Character).ToString()), e.PredicateText);
                throw new QueryParseException(e.Line, e.CharPositionInLine + 1, msg);
            }
            catch (Antlr.Runtime.NoViableAltException e)
            {
                string msg = string.Format("unexpected '{0}' no alternatives", (e.Token != null ? e.Token.Text : ((char)e.Character).ToString()));
                throw new QueryParseException(e.Line, e.CharPositionInLine + 1, msg);
            }
            catch (Antlr.Runtime.RecognitionException e)
            {
                string msg = string.Format("unexpected '{0}'", (e.Token != null ? e.Token.Text : ((char)e.Character).ToString()));
                throw new QueryParseException(e.Line, e.CharPositionInLine + 1, msg);
            }
            catch (QueryParseException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw new QueryParseException(1, 1, e.Message);
            }
        }

        internal static char GetChar(EvaluationContext ec, Expr t)
        {
            if (new List<Expr>(ec.solver.GetVars(t)).Count == 0)
            {
                //any character is fine
                return (char)ec.solver.Chooser.Choose(0xFFFF);
            }
            else
                return ec.solver.GetCharValue(ec.solver.MainSolver.FindOneMember(t).Value);
        }

        //eliminate double quotes from start and end, used by the parser, and unsescape if needed
        internal static string PreProcessString(string s)
        {
            bool literal = (s[0] == '@');
            if (literal)
                s = s.Substring(1);
            string v = s.Substring(1, s.Length - 2); //remove quotes
            if (!literal)
            {
                string w  = Microsoft.Automata.StringUtility.Unescape(v);
                return w;
            }
            else 
                return v;
            }
    }

    #region atomic expressions
    public class StringExpression : Expression
    {
        string s_value;
        StringExpression(Identifier id, string s_val) : base(id, ExpressionKind.Automaton) { this.s_value = s_val; }

        internal static StringExpression Mk(Identifier id) { return new StringExpression(id, PreProcessString(id.Name)); }

        public override string ToString()
        {
            return symbol.Name;
        }

        internal override SFAModel GetSFA(EvaluationContext ec)
        {
            return new SFAModel(ec.solver, ec.solver.CharSort, ec.solver.RegexConverter.ConvertString(s_value));
        }

        internal override string GetString(EvaluationContext ec)
        {
            return s_value;
        }
    }

    public class VariableExpression : Expression
    {
        public VariableExpression(Identifier s) : base(s, ExpressionKind.Variable) { }

        internal override SFAModel GetSFA(EvaluationContext ec)
        {
            return ec.GetSFA(symbol);
        }

        internal override STModel GetST(EvaluationContext ec)
        {
            var st = ec.GetST(symbol);
            //st.ShowGraph();
            return st;
        }
    }

    public class RegexExpression : Expression
    {
        public RegexExpression(Expression pat) : base("re", ExpressionKind.Automaton, pat) { }

        internal override SFAModel GetSFA(EvaluationContext ec)
        {
            //make an SFA out of the regex
            SFAModel sfa = new SFAModel(ec.solver, ec.solver.CharSort,  ec.solver.RegexConverter.Convert(subexpressions[0].GetString(ec)));
            return sfa;
        }
    }
    #endregion

    #region expressions for creating STs
    public class JoinExpression : Expression
    {
        public JoinExpression(Expression transducer1, Expression transducer2) : base("join", ExpressionKind.Transducer, transducer1, transducer2) { }

        internal override STModel GetST(EvaluationContext ec)
        {
            return subexpressions[0].GetST(ec) + subexpressions[1].GetST(ec);
        }
    }

    public class ExploreExpression : Expression
    {
        public ExploreExpression(Expression transducer) : base("explore", ExpressionKind.Transducer, transducer) { }

        internal override STModel GetST(EvaluationContext ec)
        {
            return subexpressions[0].GetST(ec).Explore();
        }
    }

    public class ExploreBoolsExpression : Expression
    {
        public ExploreBoolsExpression(Expression transducer) : base("exploreB", ExpressionKind.Transducer, transducer) { }

        internal override STModel GetST(EvaluationContext ec)
        {
            return subexpressions[0].GetST(ec).ExploreBools();
        }
    }

    public class RestrictExpression : Expression
    {
        public RestrictExpression(Expression transducer, Expression automaton) : base("restrict", ExpressionKind.Transducer, transducer, automaton) { }

        internal override STModel GetST(EvaluationContext ec)
        {
            return subexpressions[0].GetST(ec).RestrictDomain(subexpressions[1].GetSFA(ec));
        }
    }
    #endregion

    #region expressions for creating SFAs
    public class ComplementExpression : Expression
    {
        public ComplementExpression(Expression automaton) : base("~", ExpressionKind.Automaton, automaton) { }

        internal override SFAModel GetSFA(EvaluationContext ec)
        { 
            return subexpressions[0].GetSFA(ec).Complement();
        }
    }

    public class MinimizeExpression : Expression
    {
        public MinimizeExpression(Expression automaton) : base("minimize", ExpressionKind.Automaton, automaton) { }

        internal override SFAModel GetSFA(EvaluationContext ec)
        {
            return subexpressions[0].GetSFA(ec).Minimize();
        }
    }

    public class DeterminizeExpression : Expression
    {
        public DeterminizeExpression(Expression automaton) : base("determinize", ExpressionKind.Automaton, automaton) { }

        internal override SFAModel GetSFA(EvaluationContext ec)
        {
            return subexpressions[0].GetSFA(ec).Determinize();
        }
    }

    public class EliminateEpsilonsExpression : Expression
    {
        public EliminateEpsilonsExpression(Expression automaton) : base("eliminateEpsilons", ExpressionKind.Automaton, automaton) { }

        internal override SFAModel GetSFA(EvaluationContext ec)
        {
            return subexpressions[0].GetSFA(ec).EliminateEpsilons();
        }
    }

    public class IntersectExpression : Expression
    {
        public IntersectExpression(Expression automaton1, Expression automaton2) : base("intersect", ExpressionKind.Automaton, automaton1, automaton2) { }

        internal override SFAModel GetSFA(EvaluationContext ec)
        {
            return subexpressions[0].GetSFA(ec).IntersectWith(subexpressions[1].GetSFA(ec));
        }
    }

    public class MinusExpression : Expression
    {
        public MinusExpression(Expression automaton1, Expression automaton2) : base("minus", ExpressionKind.Automaton, automaton1, automaton2) { }

        internal override SFAModel GetSFA(EvaluationContext ec)
        {
            return subexpressions[0].GetSFA(ec).Minus(subexpressions[1].GetSFA(ec));
        }
    }

    public class InvimageExpression : Expression
    {
        public InvimageExpression(Expression transducer, Expression automaton) : base("invimage", ExpressionKind.Automaton, transducer, automaton) { }

        internal override SFAModel GetSFA(EvaluationContext ec)
        {
            return subexpressions[0].GetST(ec).ComputePreImage(subexpressions[1].GetSFA(ec)).ToSFA();
        }
    }

    public class DomainExpression : Expression
    {
        public DomainExpression(Expression transducer) : base("dom", ExpressionKind.Automaton, transducer) { }

        internal override SFAModel GetSFA(EvaluationContext ec)
        {
            return subexpressions[0].GetST(ec).ToSFA();
        }
    }

    public class ImageExpression : Expression
    {
        public ImageExpression(Expression transducer, Expression input) : base("image", ExpressionKind.Automaton, transducer, input) { }

        static string GetImage(STModel p, string inp)
        {
            var solver = p.Solver;
            var inList = solver.MkList(Array.ConvertAll(inp.ToCharArray(), e => solver.MkNumeral((int)e, p.InputSort)));
            p.AssertTheory();
            solver.MainSolver.Push();
            var outList = solver.MkFreshConst("outp", p.OutputListSort);
            var acc = p.MkAccept(inList, outList);
            var model = solver.MainSolver.GetModel(acc, outList);
            if (model == null)
                return null;
            var res = model[outList].GetList();
            solver.MainSolver.Pop();
            return new String(Array.ConvertAll(res.ToArray(), i => (char)solver.GetNumeralInt(i)));
        }

        internal override string GetString(EvaluationContext ec)
        {
            var st = subexpressions[0].GetST(ec);
            string arg = subexpressions[1].GetString(ec);
            if (arg == null)
                return null;
            else
            {
                var res = GetImage(st, arg);
                return res;
            }
        }

        internal override SFAModel GetSFA(EvaluationContext ec)
        {
            return new SFAModel(ec.solver, ec.solver.CharSort, ec.solver.RegexConverter.ConvertString(GetString(ec)));
        }


        //can also be run as a query
        internal override void RunQuery(EvaluationContext ec)
        {
            var res = GetString(ec);
            if (res == null)
                ec.tw.WriteLine("Result: Input not accepted.");
            else
                ec.tw.WriteLine("Result: {0}", Microsoft.Automata.StringUtility.Escape(res));
        }
    }
    #endregion

    #region expressions for queries
    public class LetExpression : Expression
    {
        public LetExpression(string variable, Expression expr) : base(variable, ExpressionKind.Query, expr) { }

        public override string ToString()
        {
            return symbol + "=" + subexpressions[0].ToString();
        }

        internal override void RunQuery(EvaluationContext ec)
        {
            Expression body = subexpressions[0];
            if (body.IsSFA(ec))
            {
                ec.sfaMap[symbol.Name] = body.GetSFA(ec);
            }
            else //must be transducer
            {
                ec.stMap[symbol.Name] = body.GetST(ec);
            }
            //ec.tw.WriteLine("Assigned variable {0}", symbol);
        }
    }

    public class PartialEquivalenceExpression : Expression
    {
        public PartialEquivalenceExpression(Expression transducer1, Expression transducer2) : base("eq1", ExpressionKind.Query, transducer1, transducer2) { }

        internal override void RunQuery(EvaluationContext ec)
        {
            var st1 = subexpressions[0].GetST(ec);
            var st2 = subexpressions[1].GetST(ec);

            ICounterexample<Expr> witness1=null;
            //var witness1 = st1.Diff(st2, 1); //try first to find a short counterexample
            if (witness1 == null && st1.Eq1(st2))
            {
                ec.tw.WriteLine("Result: Partial-Equivalent");
            }
            else
            {
                //var witness = (witness1 == null ? st1.Diff(st2) : witness1); //find, if needed, a longer witness
                ec.tw.WriteLine("Result: Not Partial-Equivalent");
                //ec.tw.WriteLine("Input      = {0}", Microsoft.Automata.StringUtility.Escape(witness.Input.StringValue));
                //ec.tw.WriteLine("Output/LHS = {0}", Microsoft.Automata.StringUtility.Escape(witness.Output1.StringValue));
                //ec.tw.WriteLine("Output/RHS = {0}", Microsoft.Automata.StringUtility.Escape(witness.Output2.StringValue));
            }
        }
    }

    public class BoundedEquivalenceExpression : Expression
    {
        public BoundedEquivalenceExpression(string bound, Expression transducer1, Expression transducer2) : base("eqB", ExpressionKind.Query, new Expression(bound, ExpressionKind.Integer), transducer1, transducer2) { }

        internal override void RunQuery(EvaluationContext ec)
        {
            int bound = int.Parse(subexpressions[0].symbol.Name);
            var st1 = subexpressions[1].GetST(ec);
            var st2 = subexpressions[2].GetST(ec);

            var witness = st1.Diff(st2, bound); 
            if (witness == null)
            {
                ec.tw.WriteLine("Result: Partial-Equivalent up to depth {0}", bound);
            }
            else
            {
                ec.tw.WriteLine("Result: Not Partial-Equivalent");
                ec.tw.WriteLine("Counterexample input: {0}", Microsoft.Automata.StringUtility.Escape(witness.Input.StringValue));
                ec.tw.WriteLine("LHS: {0}", Microsoft.Automata.StringUtility.Escape(witness.Output1.StringValue));
                ec.tw.WriteLine("RHS: {0}", Microsoft.Automata.StringUtility.Escape(witness.Output2.StringValue));
            }
        }
    }

    public class DomainEquivalenceExpression : Expression
    {
        public DomainEquivalenceExpression(Expression transducer1, Expression transducer2) : base("eqD", ExpressionKind.Query, transducer1, transducer2) { }

        internal override void RunQuery(EvaluationContext ec)
        {
            var sfa0 = (subexpressions[0].IsSFA(ec) ? subexpressions[0].GetSFA(ec) : subexpressions[0].GetST(ec).ToSFA());
            var sfa1 = (subexpressions[1].IsSFA(ec) ? subexpressions[1].GetSFA(ec) : subexpressions[1].GetST(ec).ToSFA());
            var sfa0_1 = sfa0.Minus(sfa1);
            if (sfa0_1.IsEmpty)
            {
                var sfa1_0 = sfa1.Minus(sfa0);
                if (sfa1_0.IsEmpty)
                {
                    ec.tw.WriteLine("Result: Domain-Equivalent");
                }
                else
                {
                    ec.tw.WriteLine("Result: Not Domain-Equivalent");
                    var w = new List<char>();
                    foreach (var t in sfa1_0.ChoosePathToSomeFinalState())
                        w.Add(GetChar(ec, t));
                    string s = new String(w.ToArray());
                    ec.tw.WriteLine("Counterexample: RHS-LHS contains {0}", Microsoft.Automata.StringUtility.Escape(s));
                }
            }
            else
            {
                ec.tw.WriteLine("Result: Not Domain-Equivalent");
                var w = new List<char>();
                foreach (var t in sfa0_1.ChoosePathToSomeFinalState())
                    w.Add(GetChar(ec, t));
                string s = new String(w.ToArray());
                ec.tw.WriteLine("Counterexample: dom(LHS)-dom(RHS) contains {0}", Microsoft.Automata.StringUtility.Escape(s));
            }
        }
    }

    public class FullEquivalenceExpression : Expression
    {
        public FullEquivalenceExpression(Expression transducer1, Expression transducer2) : base("eq", ExpressionKind.Query, transducer1, transducer2) { }

        internal override void RunQuery(EvaluationContext ec)
        {
            var st1 = subexpressions[0].GetST(ec);
            var st2 = subexpressions[1].GetST(ec);

            ICounterexample<Expr> witness1 = null;
            //var witness1 = st1.Diff(st2, 1); //try first to find a short counterexample
            if (witness1 == null && st1.Eq1(st2))
            {
                ec.tw.WriteLine("Result: Partial-Equivalent, checking domain equivalence ...");
                #region now check also domain-equivalence
                var sfa0 = st1.ToSFA();
                var sfa1 = st2.ToSFA();

                var sfa0_1 = sfa0.Minus(sfa1);
                if (sfa0_1.IsEmpty)
                {
                    ec.tw.WriteLine(" dom(LHS) is subset of dom(RHS)");
                    var sfa1_0 = sfa1.Minus(sfa0);
                    if (sfa1_0.IsEmpty)
                    {
                        ec.tw.WriteLine(" dom(RHS) is subset of dom(LHS)");
                        ec.tw.WriteLine("Result: Equivalent");
                    }
                    else
                    {
                        ec.tw.WriteLine("Result: Not Domain-Equivalent");
                        var w = new List<char>();
                        foreach (var t in sfa1_0.ChoosePathToSomeFinalState())
                            w.Add(Expression.GetChar(ec, t));
                        string s = new String(w.ToArray());
                        ec.tw.WriteLine("Counterexample: dom(RHS)-dom(LHS) contains {0}", Microsoft.Automata.StringUtility.Escape(s));
                    }
                }
                else
                {
                    ec.tw.WriteLine("Result: Not Domain-Equivalent");
                    var w = new List<char>();
                    foreach (var t in sfa0_1.ChoosePathToSomeFinalState())
                        w.Add(Expression.GetChar(ec, t));
                    string s = new String(w.ToArray());
                    ec.tw.WriteLine("Counterexample: dom(LHS)-dom(RHS) contains {0}", Microsoft.Automata.StringUtility.Escape(s));
                }
                #endregion
            }
            else
            {
                //var witness = (witness1 == null ? st1.Diff(st2) : witness1); //find, if needed, a longer witness
                ec.tw.WriteLine("Result: Not Partial-Equivalent");
                //ec.tw.WriteLine("Counterexample input: {0}", Microsoft.Automata.StringUtility.Escape(witness.Input.StringValue));
                //ec.tw.WriteLine("LHS: {0}", Microsoft.Automata.StringUtility.Escape(witness.Output1.StringValue));
                //ec.tw.WriteLine("RHS: {0}", Microsoft.Automata.StringUtility.Escape(witness.Output2.StringValue));
            }
        }
    }

    public class DisplayExpression : Expression
    {
        int NrOfElements = 0;
        public DisplayExpression(Expression expr) : base("display", ExpressionKind.Query, expr) { }

        public DisplayExpression(Expression expr, Identifier count)
            : base("display", ExpressionKind.Query, expr)
        {
            int k;
            if (!int.TryParse(count.Name, out k))
                throw new Bek.Query.QueryParseException(count.Line, count.Pos, "Invalid element count");
            NrOfElements = k;
        }

        internal override void RunQuery(EvaluationContext ec)
        {
            string n = "";
            if (subexpressions[0].IsSFA(ec))
            {
                var sfa = subexpressions[0].GetSFA(ec);
                n = String.Format("Fig_{0}", ec.imagecount);
                if (NrOfElements == 0)
                {
                    string orig_name = sfa.Name;
                    sfa.Name = n;
                    sfa.SaveAsDot(String.Format("{0}.dot", ec.imagecount));
                    sfa.Name = orig_name;
                }
                else
                {
                    var aut = sfa.Concretize(NrOfElements);
                    sfa.Solver.CharSetProvider.SaveAsDot(aut, n, String.Format("{0}.dot", ec.imagecount));
                }
            }
            else
            {
                if (NrOfElements == 0)
                {
                    var st = subexpressions[0].GetST(ec);
                    n = String.Format("Fig_{0}", ec.imagecount);
                    string orig_name = st.Name;
                    st.Name = n;
                    st.ToDot(String.Format("{0}.dot", ec.imagecount));
                    st.Name = orig_name;
                }
                else
                {
                    var fname = String.Format("{0}.dot", ec.imagecount);
                    var st = subexpressions[0].GetST(ec).Explore(NrOfElements);
                    var aut = st.MkInstance();
                    n = String.Format("Fig_{0}", ec.imagecount);
                    Microsoft.Automata.DirectedGraphs.DotWriter.AutomatonToDot<string>(x => x, aut, n, fname, Microsoft.Automata.DirectedGraphs.DotWriter.RANKDIR.LR, 12, true);
                }
            }
            ec.tw.WriteLine("Image rendered ({0})", n);
            ec.imagecount += 1;
        }
    }

    public class DotExpression : Expression
    {
        int NrOfElements = 0;
        public DotExpression(Expression expr) : base("dot", ExpressionKind.Query, expr) { }

        public DotExpression(Expression expr, Identifier count)
            : base("dot", ExpressionKind.Query, expr)
        {
            int k;
            if (!int.TryParse(count.Name, out k))
                throw new Bek.Query.QueryParseException(count.Line, count.Pos, "Invalid element count");
            NrOfElements = k;
        }

        internal override void RunQuery(EvaluationContext ec)
        {
            var sfa = subexpressions[0].GetSFA(ec);
            if (subexpressions[0].IsSFA(ec))
            {
                if (NrOfElements == 0)
                {
                    sfa.SaveAsDot(ec.tw);
                }
                else
                {
                    var aut = sfa.Concretize(NrOfElements);
                    Microsoft.Automata.DirectedGraphs.DotWriter.AutomatonToDot<BDD>(ec.solver.CharSetProvider.PrettyPrint, aut, sfa.Name, ec.tw, Microsoft.Automata.DirectedGraphs.DotWriter.RANKDIR.LR, 12, true);
                }
            }
            else
                subexpressions[0].GetST(ec).ToDot(ec.tw);
        }
    }

    public class SubsetExpression : Expression
    {
        public SubsetExpression(Expression automaton1, Expression automaton2) : base("subset", ExpressionKind.Query, automaton1, automaton2) { }

        internal override void RunQuery(EvaluationContext ec)
        {
            var aut1 = subexpressions[0].GetSFA(ec);
            var aut2 = subexpressions[1].GetSFA(ec);
            List<Expr> witness;
            bool diff = Automaton<Expr>.CheckDifference(aut1.Automaton, aut2.Automaton, -1,
                out witness);

            if (diff)
            {
                char[] s = Array.ConvertAll(witness.ToArray(), e => GetChar(ec, e));
                ec.tw.WriteLine("Result: Subset relation is False");
                ec.tw.WriteLine("Counterexample: LHS-RHS contains {0}",Automata.StringUtility.Escape(new String(s)));
            }
            else
            {
                ec.tw.WriteLine("Result: Subset relation is True");
            }
        }
    }

    public class IsEmptyExpression : Expression
    {
        public IsEmptyExpression(Expression automaton) : base("isempty", ExpressionKind.Query, automaton) { }

        internal override void RunQuery(EvaluationContext ec)
        {
            var aut1 = subexpressions[0].GetSFA(ec);
            if (aut1.IsEmpty)
            {
                ec.tw.WriteLine("Result: Automaton is empty");
            }
            else
            {
                ec.tw.WriteLine("Result: Automaton is nonempty");
                var w = new String(Array.ConvertAll(new List<Expr>(aut1.ChoosePathToSomeFinalState()).ToArray(), x => (char)ec.solver.GetNumeralUInt(ec.solver.MainSolver.FindOneMember(x).Value)));
                var w_escaped = Microsoft.Automata.StringUtility.Escape(w);
                ec.tw.WriteLine("Witness: {0}", w_escaped);
            }
        }
    }

    public class CsExpression : Expression
    {
        public CsExpression(Expression transducer) : base("cs", ExpressionKind.Query, transducer) { }

        internal override void RunQuery(EvaluationContext ec)
        {
            var st = subexpressions[0].GetST(ec);
            System.IO.StreamWriter tw = new System.IO.StreamWriter(st.Name + ".cs"); 
            st.ToSTb().ToCS((x => {ec.tw.WriteLine(x); tw.WriteLine(x);}), "Bek4funSample", st.Name, true);
            tw.Close();
        }
    }

    public class JsExpression : Expression
    {
        public JsExpression(Expression transducer) : base("js", ExpressionKind.Query, transducer) { }

        internal override void RunQuery(EvaluationContext ec)
        {
            var st = subexpressions[0].GetST(ec);
            STbModel stb = st.ToSTb();
            GenerateJavaScript(ec, stb);

            //if ((subexpressions[0] is VariableExpression) &&
            //    (ec.bekMap.ContainsKey(subexpressions[0].symbol.Name)))
            //{
            //    var bekpgm = ec.bekMap[subexpressions[0].symbol.Name];
            //    GenerateJavaScript(ec, bekpgm);
            //}
            //else
            //{
            //    var st = subexpressions[0].GetST(ec);
            //    STbModel stb = st.ToSTb();
            //    StringBuilder bekstr = new StringBuilder();
            //    stb.ToBek(bekstr);
            //    var s = bekstr;
            //    var bekpgm = BekParser.BekFromString(s.ToString());
            //    bekpgm.ast.AddLocalFunctions(ec.GetLocalFunctions());
            //    GenerateJavaScript(ec, bekpgm);
            //}
        }

        private void GenerateJavaScript(EvaluationContext ec, BekProgram bekpgm)
        {
            var js = new StringBuilder();
            var bek = bekpgm.ast;
            var oldname = bek.name;
            bek.name = "bek";
            bek.GenerateCode("JS", js);
            bek.name = oldname;

            string bek_js_master = (ec.bek_js_master_file != "" ? System.IO.File.ReadAllText(ec.bek_js_master_file) : "BEKJSCODE");
            string bek_js = bek_js_master.Replace("BEKJSCODE", js.ToString());

            System.IO.StreamWriter sw = new System.IO.StreamWriter("c.htm");
            sw.WriteLine(bek_js.ToString());
            sw.Close();

            ec.tw.WriteLine("Generated JavaScript for {0}:", subexpressions[0].ToString());
            ec.tw.WriteLine(js);
        }

        private void GenerateJavaScript(EvaluationContext ec, STbModel stb)
        {
            var js = new StringBuilder();
            stb.ToJS((x => { js.AppendLine(x); ec.tw.WriteLine(x); }));

            string bek_js_master = (ec.bek_js_master_file != "" ? System.IO.File.ReadAllText(ec.bek_js_master_file) : "BEKJSCODE");
            string bek_js = bek_js_master.Replace("BEKJSCODE", js.ToString());

            System.IO.StreamWriter sw = new System.IO.StreamWriter("c.htm");
            sw.WriteLine(bek_js.ToString());
            sw.Close();

            ec.tw.WriteLine("Generated JavaScript for {0}:", subexpressions[0].ToString());
            ec.tw.WriteLine(js);
        }

        //old version
//        private static void GenerateHtml(StringBuilder js)
//        {
//            System.IO.StreamWriter htm = new System.IO.StreamWriter("bek.js.htm");
//            htm.WriteLine("<html>\n<head>");
//            htm.WriteLine("<SCRIPT language = \"javascript\">");
//            htm.WriteLine(js.ToString());
//            htm.WriteLine("</SCRIPT>");
//            htm.WriteLine("<SCRIPT language = \"javascript\">");
//            htm.WriteLine(@"
//function runbek() { 
//        try {
//            A = document.frmOne.txtInputString.value;
//            B = bek(unescape(A));
//            document.frmOne.txtOutputString.value = escape(B);
//            document.frmOne.txtOutputLiteralString.value = B;
//            document.frmOne.txtError.value = '';
//        }
//        catch (e) {
//            document.frmOne.txtOutputString.value = '';
//            document.frmOne.txtOutputLiteralString.value = '';
//            document.frmOne.txtError.value = e.name; 
//        }
//}");
//            htm.WriteLine("</SCRIPT>");
//            htm.Write(@"
//</head>
//<body>
//<h1>Test the generated script</h1>
//Input is assumed to be an <i>escaped</i> JavaScript string. 
//<br/>
//<i>Example</i>: <tt>O%u0152E</tt> and <tt>%4F%u0152%45</tt>
//both represent <tt>O&#x0152;E</tt>
//<FORM NAME = frmOne>
//input: <INPUT TYPE = Text NAME = txtInputString size = 50 value =" + "\"\"" + @">
//<p>
//<INPUT Type = Button NAME = b1 VALUE = " + "\"run\"" + @" onClick = runbek()>
//</p>
//<p>
//output (escaped): <INPUT TYPE = Text NAME = txtOutputString size = 50 value = " + "\"\"" + @">
//<br />
//output (literal): <INPUT TYPE = Text NAME = txtOutputLiteralString size = 50 value = " + "\"\"" + @">
//<br />
//exception: <input TYPE= text   NAME = txtError size = 50>
//</p>
//</FORM>
//</body>
//</html> 
//");

//            htm.Close();
//        }
    }

    public class MembershipExpression : Expression
    {
        public MembershipExpression(Expression member, Expression automaton) : base("member", ExpressionKind.Query, member, automaton) { }

        internal override void RunQuery(EvaluationContext ec)
        {
            string s = subexpressions[0].GetString(ec);
            var aut_s = ec.solver.RegexConverter.ConvertString(s);
            var aut = subexpressions[1].GetSFA(ec);
            var prod = aut_s.Intersect(aut.Automaton);

            if (prod.IsEmpty)
            {
                ec.tw.WriteLine("Result: Membership is True");
            }
            else
            {
                ec.tw.WriteLine("Result: Membership is False");
            }
        }
    }
    #endregion
}
