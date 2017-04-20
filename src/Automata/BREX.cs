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
        /// Evaluate the expression to an automaton
        /// </summary>
        internal abstract Automaton<BDD> ConvertToAutomaton();

        /// <summary>
        /// If optimize has already been invoked, this is the result.
        /// </summary>
        Automaton<BDD> optimizedAutomaton = null;

        /// <summary>
        /// Evaluate the expression to an automaton and optimize it for code generation.
        /// </summary>
        public Automaton<BDD> Optimize()
        {
            if (this.optimizedAutomaton == null)
            {
                this.optimizedAutomaton = this.ConvertToAutomaton().RemoveEpsilons().Determinize().Minimize();
            }

            return this.optimizedAutomaton;
        }

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
    /// Represents a like pattern
    /// </summary>
    public class BREXLike : BREX
    {
        /// <summary>
        /// The underlying automaton
        /// </summary>
        Automaton<BDD> automaton;

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
        /// returns true if automaton creation succeeds
        /// </summary>
        public bool CreateAutomaton()
        {
            if (this.automaton != null)
            {
                return true;
            }

            try
            {
                this.automaton = this.manager.LikeConverter.Convert(this.like.Item1, this.like.Item2);
                return true;
            }
            catch (AutomataException)
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the underlying automaton
        /// </summary>
        override internal Automaton<BDD> ConvertToAutomaton()
        {
            if (CreateAutomaton())
                return this.automaton;
            else
                throw new AutomataException(AutomataExceptionKind.AutomataConversionFailed);
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
    }

    /// <summary>
    /// Represents a .NET regex pattern
    /// </summary>
    public class BREXRegex : BREX
    {
        /// <summary>
        /// The underlying automaton
        /// </summary>
        Automaton<BDD> automaton;

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
        /// returns true if automaton creation succeeds
        /// </summary>
        public bool CreateAutomaton()
        {
            if (this.automaton != null)
            {
                return true;
            }

            try
            {
                this.automaton = this.manager.Solver.Convert(this.regex.Item1, this.regex.Item2);
                return true;
            }
            catch (AutomataException)
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the underlying automaton. 
        /// Assumes that CreateAutomaton() returns true. 
        /// </summary>
        override internal Automaton<BDD> ConvertToAutomaton()
        {
            if (CreateAutomaton())
                return this.automaton;
            else
                throw new AutomataException(AutomataExceptionKind.AutomataConversionFailed);
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
        /// Returns the complement of the automaton of the expression being negated
        /// </summary>
        override internal Automaton<BDD> ConvertToAutomaton()
        {
            return this.Expr.ConvertToAutomaton().Complement();
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
        /// Returns the intersection automaton
        /// </summary>
        override internal Automaton<BDD> ConvertToAutomaton()
        {
            return this.First.ConvertToAutomaton().Intersect(this.Second.ConvertToAutomaton());
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
        /// Returns the union automaton
        /// </summary>
        override internal Automaton<BDD> ConvertToAutomaton()
        {
            return this.First.ConvertToAutomaton().Union(this.Second.ConvertToAutomaton());
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
    }
}
