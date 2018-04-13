using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.Automata
{
    /// <summary>
    /// Base class to represent Boolean combinations of regular expressions
    /// </summary>
    public abstract class BREX
    {
        /// <summary>
        /// Returns true if an optimized DFA can be constructed.
        /// </summary>
        /// <returns></returns>
        public abstract bool CanBeOptimized();

        /// <summary>
        /// Construct an optimized DFA.
        /// </summary>
        public abstract Automaton<BDD> Optimize();

        /// <summary>
        /// Value equality
        /// </summary>
        public override bool Equals(object obj)
        {
            var be = obj as BREX;
            if (be == null)
            {
                return false;
            }

            return be.Description.Equals(this.Description);
        }

        /// <summary>
        /// Hashcode of value
        /// </summary>
        public override int GetHashCode()
        {
            return this.Description.GetHashCode();
        }

        /// <summary>
        /// cached description
        /// </summary>
        string description = null;

        /// <summary>
        /// Description of this Boolean expression of regexes
        /// </summary>
        public string Description
        {
            get
            {
                if (this.description == null)
                {
                    this.description = this.ToString();
                }

                return this.description;
            }
        }

        /// <summary>
        /// gets the manager
        /// </summary>
        public abstract BREXManager Manager { get; }

        /// <summary>
        /// Gets the IsMatch method identifier for this expression
        /// </summary>
        public string Name
        {
            get
            {
                return this.Manager.AddBoolRegExp(this);
            }
        }
    }

    /// <summary>
    /// Leaf element of BREX
    /// </summary>
    public abstract class BREXLeaf : BREX
    {
        internal abstract Automaton<BDD> CreateNFA();

        /// <summary>
        /// The underlying DFA
        /// </summary>
        internal Automaton<BDD> automaton;

        bool createAutomatonCalled = false;
        /// <summary>
        /// Returns true if DFA creation succeeds within the required state bounds
        /// </summary>
        bool CreateAutomaton()
        {
            if (this.createAutomatonCalled)
            {
                return this.automaton != null;  
            }
            else
            {
                this.createAutomatonCalled = true;
                try
                {
                    var nfa = this.CreateNFA();
                    //try to determinize with 1sec timeout
                    var dfa = nfa.Determinize(Manager.Timeout);
                    //minimize
                    var mfa = dfa.Minimize();
                    //return false if the state limit is violated
                    if (mfa.StateCount > Manager.MaxNrOfStates)
                    {
                        return false;
                    }
                    this.automaton = mfa;
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public override bool CanBeOptimized()
        {
            return CreateAutomaton();
        }

        public override Automaton<BDD> Optimize()
        {
            if (!CreateAutomaton())
                throw new AutomataException(AutomataExceptionKind.AutomataConversionFailed);

            return automaton;
        }
    }

    /// <summary>
    /// Represents a like pattern
    /// </summary>
    public class BREXLike : BREXLeaf
    {
        /// <summary>
        /// gets the manager
        /// </summary>
        public override BREXManager Manager
        {
            get
            {
                return this.manager;
            }
        }

        /// <summary>
        /// like pattern with escape character
        /// </summary>
        Tuple<string, char> like;

        /// <summary>
        /// The manager
        /// </summary>
        BREXManager manager;

        /// <summary>
        /// Construct a literal automaton
        /// </summary>
        internal BREXLike(BREXManager manager, string pattern, char escape)
        {
            this.manager = manager;
            this.automaton = null;
            this.like = new Tuple<string, char>(pattern, escape);
        }

        /// <summary>
        /// Display the literal Boolean regular expression
        /// </summary>
        public override string ToString()
        {
            if (this.like.Item2 == '\0')
            {
                return string.Format("Like({0})", this.like.Item1);
            }
            else
            {
                return string.Format("Like({0},{1})", this.like.Item1, this.like.Item2);
            }
        }

        /// <summary>
        /// Value equality
        /// </summary>
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        /// <summary>
        /// Hashcode of value
        /// </summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        internal override Automaton<BDD> CreateNFA()
        {
            return this.manager.LikeConverter.Convert(this.like.Item1, this.like.Item2);
        }
    }

    /// <summary>
    /// Represents a .NET regex pattern
    /// </summary>
    public class BREXRegex : BREXLeaf
    {
        /// <summary>
        /// gets the manager
        /// </summary>
        public override BREXManager Manager
        {
            get
            {
                return this.manager;
            }
        }

        /// <summary>
        /// A .NET regex pattern with options
        /// </summary>
        Tuple<string, RegexOptions> regex;

        /// <summary>
        /// The manager
        /// </summary>
        BREXManager manager;

        /// <summary>
        /// Construct a literal automaton
        /// </summary>
        internal BREXRegex(BREXManager manager, string pattern, RegexOptions options = RegexOptions.None)
        {
            this.manager = manager;
            this.automaton = null;
            this.regex = new Tuple<string, RegexOptions>(pattern, options);
        }

        /// <summary>
        /// Display the literal Boolean regular expression
        /// </summary>
        public override string ToString()
        {
            if (this.regex.Item2 == RegexOptions.None)
            {
                return string.Format("Regex({0})", this.regex.Item1);
            }
            else
            {
                return string.Format("Regex({0},{1})", this.regex.Item1, this.regex.Item2);
            }
        }


        /// <summary>
        /// Value equality
        /// </summary>
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        /// <summary>
        /// Hashcode of value
        /// </summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        internal override Automaton<BDD> CreateNFA()
        {
            return this.manager.Solver.Convert(this.regex.Item1, this.regex.Item2);
        }
    }

    /// <summary>
    /// Represents the complement of a Boolean regular expression
    /// </summary>
    public class BREXComplement : BREX
    {
        /// <summary>
        /// The expression being negated
        /// </summary>
        public BREX Expr { get; }

        /// <summary>
        /// gets the manager
        /// </summary>
        public override BREXManager Manager
        {
            get
            {
                return this.Expr.Manager;
            }
        }

        /// <summary>
        /// Construct a literal automaton
        /// </summary>
        internal BREXComplement(BREX expr)
        {
            this.Expr = expr;
        }

        /// <summary>
        /// Display this Boolean regular expression.
        /// </summary>
        public override string ToString()
        {
            return string.Format("Not({0})", this.Expr.ToString());
        }

        /// <summary>
        /// Value equality
        /// </summary>
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        /// <summary>
        /// Hashcode of value
        /// </summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Returns true if Expr can be optimized
        /// </summary>
        /// <returns></returns>
        public override bool CanBeOptimized()
        {
            return Expr.CanBeOptimized();
        }

        Automaton<BDD> not = null;
        public override Automaton<BDD> Optimize()
        {
            if (not != null)
                return not;

            if (!Expr.CanBeOptimized())
                throw new AutomataException(AutomataExceptionKind.AutomataConversionFailed);

            not = Expr.Optimize().Complement();
            return not;
        }
    }

    /// <summary>
    /// Represents the conjunction of two Boolean regular expressions
    /// </summary>
    public class BREXConjunction : BREX
    {
        /// <summary>
        /// The first conjunct
        /// </summary>
        public BREX First { get; }

        /// <summary>
        /// The second conjunct
        /// </summary>
        public BREX Second { get; }

        /// <summary>
        /// gets the manager
        /// </summary>
        public override BREXManager Manager
        {
            get
            {
                return this.First.Manager;
            }
        }

        /// <summary>
        /// Constructs a conjunction expression
        /// </summary>
        internal BREXConjunction(BREX first, BREX second)
        {
            this.First = first;
            this.Second = second;
        }

        /// <summary>
        /// Displays this Boolean regular expression.
        /// </summary>
        public override string ToString()
        {
            return string.Format("And({0},{1})", this.First.ToString(), this.Second.ToString());
        }

        /// <summary>
        /// Value equality
        /// </summary>
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        /// <summary>
        /// Hashcode of value
        /// </summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        Automaton<BDD> product = null;
        bool optimizeCalled = false;
        public override bool CanBeOptimized()
        {
            if (optimizeCalled)
                return product != null;

            optimizeCalled = true;
            if (!First.CanBeOptimized() || !Second.CanBeOptimized())
                return false;

            var first = First.Optimize();
            var second = Second.Optimize();

            Automaton<BDD> prod = null;
            try
            {
                //one second timeout for the intersection to complete
                prod = first.Intersect(second, Manager.Timeout).Minimize();
                if (prod.StateCount > Manager.MaxNrOfStates)
                    return false;
            }
            catch
            {
                //timeout
                return false;
            }

            product = prod;
            return true;
        }

        public override Automaton<BDD> Optimize()
        {
            if (!CanBeOptimized())
                throw new AutomataException(AutomataExceptionKind.AutomataConversionFailed);

            return product;
        }
    }

    /// <summary>
    /// Represents the disjunction of two Boolean regular expressions
    /// </summary>
    public class BREXDisjunction : BREX
    {
        /// <summary>
        /// The first disjunct
        /// </summary>
        public BREX First { get; }

        /// <summary>
        /// The second disjunct
        /// </summary>
        public BREX Second { get; }

        /// <summary>
        /// gets the manager
        /// </summary>
        public override BREXManager Manager
        {
            get
            {
                return this.First.Manager;
            }
        }

        /// <summary>
        /// Constructs a disjunction expression
        /// </summary>
        internal BREXDisjunction(BREX first, BREX second)
        {
            this.First = first;
            this.Second = second;
        }

        /// <summary>
        /// Displays this Boolean regular expression.
        /// </summary>
        public override string ToString()
        {
            return string.Format("Or({0},{1})", this.First.ToString(), this.Second.ToString());
        }

        /// <summary>
        /// Value equality
        /// </summary>
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        /// <summary>
        /// Hashcode of value
        /// </summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        Automaton<BDD> sum = null;
        bool optimizeCalled = false;
        public override bool CanBeOptimized()
        {
            if (optimizeCalled)
                return sum != null;

            optimizeCalled = true;
            if (!First.CanBeOptimized() || !Second.CanBeOptimized())
                return false;

            var first = First.Optimize();
            var second = Second.Optimize();

            Automaton<BDD> s = null;
            try
            {

                s = first.Complement().Intersect(second.Complement(), Manager.Timeout).Complement().Minimize();
                if (s.StateCount > Manager.MaxNrOfStates)
                    return false;
            }
            catch
            {
                //timeout occured
                return false;
            }

            sum = s;
            return true;
        }

        public override Automaton<BDD> Optimize()
        {
            if (!CanBeOptimized())
                throw new AutomataException(AutomataExceptionKind.AutomataConversionFailed);

            return sum;
        }
    }
}
