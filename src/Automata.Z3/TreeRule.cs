using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Z3;

using Microsoft.Automata;

using Microsoft.Automata.Z3.Internal;

using ExprSet = Microsoft.Automata.OrderedSet<Microsoft.Z3.Expr>;

namespace Microsoft.Automata.Z3
{
    /// <summary>
    /// The acceptor base of a tree rule. 
    /// </summary>
    public class AcceptorBase
    {
        protected FuncDecl symbol;
        protected Expr guard;
        internal ExprSet[] lookahead;


        Tuple<FuncDecl, Sequence<ExprSet>> lhs;
        public Tuple<FuncDecl, Sequence<ExprSet>> LHS
        {
            get
            {
                return lhs;
            }
        }

        /// <summary>
        /// Gets the alphabet symbol.
        /// </summary>
        public FuncDecl Symbol { get { return symbol; } }

        /// <summary>
        /// Gets the attribute guard.
        /// </summary>
        public Expr Guard { get { return guard; } }

        /// <summary>
        /// The rank (number of child subtrees) of the symbol. 
        /// </summary>
        public int Rank
        {
            get { return lookahead.Length; }
        }

        /// <summary>
        /// Language state set for bottom element i, where i is in [0..Rank-1].
        /// </summary>
        public ExprSet Lookahead(int i)
        {
            //if (i < 0 || i >= lookahead.Length)
            //    throw new AutomataException(AutomataExceptionKind.TreeRule_InvalidChildIdentifier);

            return lookahead[i];
        }

        /// <summary>
        /// Returns true iff each lookahead state set is either empty or a singleton.
        /// </summary>
        public bool LookaheadIsFlat
        {
            get
            {
                return Array.TrueForAll(lookahead, x => (x.IsEmpty || x.IsSingleton));
            }
        }

        internal AcceptorBase(FuncDecl symbol, Expr guard, ExprSet[] lookahead)
        {
            this.symbol = symbol;
            this.guard = guard;
            this.lookahead = lookahead;
            this.lhs = new Tuple<FuncDecl, Sequence<ExprSet>>(symbol, new Sequence<ExprSet>(lookahead));
        }

        internal AcceptorBase(FuncDecl symbol, Expr guard, int rank)
        {
            this.symbol = symbol;
            this.guard = guard;
            ExprSet[] ts = new ExprSet[rank];
            for (int i = 0; i < rank; i++)
                ts[i] = new ExprSet();
            lookahead = ts;
        }

        public override bool Equals(object obj)
        {
            var ab = obj as AcceptorBase;
            if (ab == null)
                return false;
            if (!symbol.Equals(ab.symbol))
                return false;
            if (!guard.Equals(ab.guard))
                return false;
            if (lookahead.Length != ab.lookahead.Length)
                return false;
            for (int i=0; i < lookahead.Length; i++)
                if (!lookahead[i].Equals(ab.lookahead[i]))
                    return false;
            return true;
        }

        public override int GetHashCode()
        {
            int h = symbol.GetHashCode() + (guard.GetHashCode() << 1) + lookahead.Length;
            for (int i = 0; i < lookahead.Length; i++)
                h += (lookahead[i].GetHashCode() << (i + 2));
            return h;
        }

        public override string ToString()
        {
            var res = "(";
            res += symbol.Name;
            res += ",";
            res += guard.ToString();
            res += ",[";
            for (int i = 0; i < lookahead.Length; i++)
            {
                if (!res.EndsWith("["))
                    res += ",";
                res += "{";
                foreach (var p in lookahead[i])
                {
                    if (!res.EndsWith("{"))
                        res += ",";
                    res += p.ToString();
                }
                res += "}";
            }
            res += "]";
            res += ")";
            return res;
        }
    }

    /// <summary>
    /// Describes a symbolic tree transduction rule.
    /// </summary>
    public class TreeRule : AcceptorBase
    {
        internal Expr state;
        Expr output;

        /// <summary>
        /// Returns true iff the output of the rule is undefined.
        /// </summary>
        public bool IsAcceptorRule
        {
            get { return output == null; }
        }

        /// <summary>
        /// Returns true iff all states that occur in the rule satisfy the predicate phi.
        /// </summary>
        /// <param name="phi">predicate over states</param> 
        public bool IsTrueForAllStates(Func<Expr, bool> phi)
        {
            if (!phi(state))
                return false;
            else
            {
                for (int i = 0; i < Rank; i++)
                    foreach (var q in lookahead[i])
                        if (!phi(q))
                            return false;
                if (output == null)
                    return true;
                else
                    return CheckForAllStates(output, phi);
            }
        }

        private static bool CheckForAllStates(Expr output, Func<Expr, bool> phi)
        {
            if (output.ASTKind != Z3_ast_kind.Z3_APP_AST)
                throw new AutomataException(AutomataExceptionKind.TreeTransducer_UnexpectedOutputExpr);

            var f = output.FuncDecl;
            var args = output.Args;

            if (TreeTheory.IsTrans(f))
            {
                //check the state condition for the first argument
                bool res = phi(args[0]);
                return res;
            }
            else //must be constructor
            {
                for (int i = 1; i < args.Length; i++)
                    if (!CheckForAllStates(args[i], phi))
                        return false;
                return true;
            }
        }

        /// <summary>
        /// Gets the source state of the rule.
        /// </summary>
        public Expr State { get { return state; } }

        /// <summary>
        /// Gets the output tree of the rule, Output=null iff IsAcceptor=true.
        /// </summary>
        public Expr Output
        {
            get
            {
                return output;
            }
        }

        /// <summary>
        /// Create a tree rule with empty regular lookahead.
        /// </summary>
        internal TreeRule(Expr state, FuncDecl symbol, Expr guard, Expr output, int rank)
            : base(symbol, guard, rank)
        {
            this.state = state;
            this.output = output;
        }

        /// <summary>
        /// Create a tree rule with regular lookahead.
        /// </summary>
        internal TreeRule(Expr state, FuncDecl symbol, Expr guard, Expr output, params ExprSet[] lookahead)
            : base(symbol, guard, lookahead)
        {
            this.state = state;
            this.output = output;
        }

        /// <summary>
        /// Enumerates all pairs Expr[]{state,child_variable}.
        /// </summary>
        static IEnumerable<Expr[]> GetStatesOf(Expr t)
        {
            if (t.ASTKind == Z3_ast_kind.Z3_APP_AST)
            {
                var args = t.Args;
                if (TreeTheory.IsTrans(t.FuncDecl))
                    yield return args;
                else
                {
                    for (int i = 1; i < args.Length; i++)
                        foreach (var s in GetStatesOf(args[i]))
                            yield return s;
                }
            }
        }

        /// <summary>
        /// Enumerate all occurrences of $trans(q,subtree_id) in the output as pairs (q,subtree_id).
        /// </summary>
        public IEnumerable<Tuple<Expr,int>> EnumerateStatesInOutput()
        {
            if (output == null)
                yield break;

            foreach (var st in GetStatesOf(output))
            {
                yield return new Tuple<Expr, int>(st[0], GetVariableIndex(st[1]));
            }
        }

        int GetVariableIndex(Expr x)
        {
            if (!x.IsConst || !x.FuncDecl.Name.IsIntSymbol())
                throw new AutomataException(AutomataExceptionKind.ExpectingConstIntSymbol);
            return ((IntSymbol)x.FuncDecl.Name).Int;
        }

        /// <summary>
        /// Include all states in the output also in the lookahead. Delete the output.
        /// </summary>
        public TreeRule GetAcceptorRule()
        {
            if (output == null)
                return this;

            ExprSet[] given = new ExprSet[lookahead.Length];
            for (int i = 0; i < lookahead.Length; i++)
                given[i] = new ExprSet(lookahead[i]);

            foreach (var state_child in GetStatesOf(output))
                if (!TreeTheory.IsIdentityState(state_child[0])) //filter out identity states
                    given[(GetVariableIndex(state_child[1])) - 1].Add(state_child[0]);

            return new TreeRule(state, symbol, guard, null, given);
        }

        /// <summary>
        /// Include all states in the output also in the lookahead.
        /// </summary>
        public TreeRule Normalize()
        {
            if (output == null)
                return this;

            ExprSet[] given = new ExprSet[lookahead.Length];
            for (int i = 0; i < lookahead.Length; i++)
                given[i] = new ExprSet(lookahead[i]);

            foreach (var state_child in GetStatesOf(output))
                if (!TreeTheory.IsIdentityState(state_child[0])) //filter out identity states
                    given[(GetVariableIndex(state_child[1])) - 1].Add(state_child[0]);

            return new TreeRule(state, symbol, guard, output, given);
        }

        public override string ToString()
        {
            var res = (IsAcceptorRule ? "Lang(" : "Trans(");
            res += state.ToString();
            res += ",";
            res += Symbol.Name;
            res += ",";
            res += guard.ToString();
            res += ",[";
            for (int i = 0; i < lookahead.Length; i++)
            {
                if (!res.EndsWith("["))
                    res += ",";
                res += "{";
                foreach (var p in lookahead[i])
                {
                    if (!res.EndsWith("{"))
                        res += ",";
                    res += p.ToString();
                }
                res += "}";
            }
            res += "]";
            if (!IsAcceptorRule)
            {
                res += ",";
                res += output.ToString();
            }
            res += ")";
            return res;
        }

        public override bool Equals(object obj)
        {
            var tr = obj as TreeRule;
            if (tr == null)
                return false;
            if (!object.Equals(output,tr.output))
                return false;
            if (!state.Equals(tr.state))
                return false;
            return
                base.Equals(obj);
        }

        public override int GetHashCode()
        {
            int h = (output == null ? 0 : output.GetHashCode());
            h += (state.GetHashCode() << 1);
            h += base.GetHashCode();
            return h;
        }

        internal TreeRule MkIdRule(RankedAlphabet A)
        {
            if (!IsAcceptorRule)
                throw new AutomataException(AutomataExceptionKind.MkIdRule_RuleIsNotAcceptorRule);

            Expr[] args = new Expr[this.lookahead.Length + 1];
            args[0] = A.AttrVar;
            for (int i = 1; i <= this.lookahead.Length; i++)
            {
                if (!(this.lookahead[i-1].IsEmptyOrSingleton))
                    throw new AutomataException(AutomataExceptionKind.MkIdRule_RuleIsNotFlat);
                args[i] = (this.lookahead[i-1].IsEmpty ? A.tt.Z.MkApp(A.Trans, A.tt.identityState, A.ChildVar(i)) 
                                                       : A.tt.Z.MkApp(A.Trans, this.lookahead[i-1].SomeElement, A.ChildVar(i)));
            }
            var new_output = A.tt.Z.MkApp(symbol, args);
            var new_rule = new TreeRule(this.state, this.symbol, this.guard, new_output, this.lookahead);
            return new_rule;
        }
    }
}
