using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.Automata.Templates;

namespace Microsoft.Automata
{
    /// <summary>
    /// Manages BREX expressions
    /// </summary>
    public class BREXManager
    {
        /// <summary>
        /// BoolRegExp to name dictionary
        /// </summary>
        Dictionary<BREX, string> boolRegExp2name = new Dictionary<BREX, string>();

        /// <summary>
        /// Index appended to different macher method names
        /// </summary>
        int index = 1;

        /// <summary>
        /// prefix of generated matcher names
        /// </summary>
        public string MatcherPrefix { get; }

        /// <summary>
        /// Converter for regexes
        /// </summary>
        public CharSetSolver Solver { get; }

        /// <summary>
        /// Converter for Like expressions
        /// </summary>
        internal LikePatternToAutomatonConverter<BDD> LikeConverter { get; }

        /// <summary>
        /// Add BREX expression to the pool of existing ones, returns a name that will identify the expression.
        /// </summary>
        public string AddBoolRegExp(BREX boolRegExp)
        {
            string name;
            if (!this.boolRegExp2name.TryGetValue(boolRegExp, out name))
            {
                name = string.Format("{0}{1}", this.MatcherPrefix, this.index);
                this.index += 1;
                this.boolRegExp2name[boolRegExp] = name;
            }

            return name;
        }

        /// <summary>
        /// Used in C++ for string type
        /// </summary>
        public string CppStringTypeName { get; }

        /// <summary>
        /// The maximum number of states allowed in a generated automaton.
        /// </summary>
        internal int MaxNrOfStates { get; }

        /// <summary>
        /// Timeout for automata constructions.
        /// </summary>
        internal int Timeout { get; }


        /// <summary>
        /// Constructs an instance of the manager
        /// </summary>
        /// <param name="matcherPrefix">prefix of generated matcher method names, default is "Matcher"</param>
        /// <param name="cppStringTypeName">identifier in C++ for string type, default is "FString"</param>
        /// <param name="maxNrOfStates">maximum number of allowed states in a generated automaton, default is 1000, 0 or negative number implies there is no bound</param>
        /// <param name="timeout">timeout in ms for automata constructions, default is 1000, 0 or negative number implies there is no bound</param>
        public BREXManager(string matcherPrefix = "Matcher", string cppStringTypeName = "FString", int maxNrOfStates = 1000, int timeout = 1000)
        {
            this.Solver = new CharSetSolver();
            this.LikeConverter = new LikePatternToAutomatonConverter<BDD>(this.Solver);
            this.MatcherPrefix = matcherPrefix;
            this.CppStringTypeName = cppStringTypeName;
            this.MaxNrOfStates = (maxNrOfStates <= 0 ? int.MaxValue : maxNrOfStates);
            this.Timeout = timeout;
        }

        /// <summary>
        /// Generate text representing c++ code from the automata text template
        /// </summary>
        public string GenerateCpp()
        {
            var name2automaton = new Dictionary<string, Automaton<BDD>>();
            foreach (var kv in this.boolRegExp2name)
            {
                name2automaton[kv.Value] = kv.Key.Optimize();
            }

            if (name2automaton.Count > 0)
            {
                var regexesTemplate = new AutomataTextTemplate(this, name2automaton);
                return regexesTemplate.TransformText();
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Creates a basic BoolRegExp for like expression.
        /// </summary>
        /// <param name="pattern">like pattern</param>
        /// <param name="escape">escape character, default is '\0'</param>
        /// <returns></returns>
        public BREXLike MkLike(string pattern, char escape = '\0')
        {
            return new BREXLike(this, pattern, escape);
        }

        /// <summary>
        /// Creates a basic BoolRegExp for regex expression.
        /// </summary>
        /// <param name="pattern">.NET regex pattern</param>
        /// <param name="options">regex options, default is 'RegexOptions.None'</param>
        /// <returns></returns>
        public BREXRegex MkRegex(string pattern, System.Text.RegularExpressions.RegexOptions options = System.Text.RegularExpressions.RegexOptions.None)
        {
            return new BREXRegex(this, pattern, options);
        }

        /// <summary>
        /// Creates a conjunction
        /// </summary>
        public BREXConjunction MkAnd(BREX first, BREX second)
        {
            return new BREXConjunction(first, second);
        }

        /// <summary>
        /// Creates a disjunction
        /// </summary>
        public BREXDisjunction MkOr(BREX first, BREX second)
        {
            return new BREXDisjunction(first, second);
        }

        /// <summary>
        /// Creates a complement
        /// </summary>
        public BREXComplement MkNot(BREX expr)
        {
            return new BREXComplement(expr);
        }

    }

    /// <summary>
    /// Helper predicates generated from BDDs
    /// </summary>
    public class BDDHelperPredicates
    {
        /// <summary>
        /// Helper predicate
        /// </summary>
        public struct Predicate
        {
            /// <summary>
            /// method name
            /// </summary>
            public string Name;

            /// <summary>
            /// method body
            /// </summary>
            public string Body;
        }

        /// <summary>
        /// list of helper predicates
        /// </summary>
        List<Predicate> helperPredicates;

        /// <summary>
        /// predicate cache
        /// </summary>
        Dictionary<BDD, string> predicateCache;

        /// <summary>
        /// character solver
        /// </summary>
        CharSetSolver solver;

        /// <summary>
        /// BDD representing ascii characters
        /// </summary>
        BDD ascii;

        /// <summary>
        /// if true then optimize for ascii
        /// </summary>
        bool optimzeForASCIIinput;

        /// <summary>
        /// prefix of all helper method names, default is "Regex"
        /// </summary>
        string prefix;

        /// <summary>
        /// Create instance of HelperPredicates 
        /// </summary>
        public BDDHelperPredicates(CharSetSolver solver, bool optimzeForAsciiInput, string prefix = "Regex")
        {
            this.solver = solver;
            this.helperPredicates = new List<Predicate>();
            this.predicateCache = new Dictionary<BDD, string>();
            this.ascii = solver.MkRangeConstraint('\0', '\x7F');
            this.optimzeForASCIIinput = optimzeForAsciiInput;
            this.prefix = prefix;
        }

        /// <summary>
        /// Generate method code for a given BDD with given methid as method name
        /// </summary>
        private static void GenerateCodeForBDD(StringBuilder code, BDD pred, string methid)
        {
            Dictionary<BDD, int> idMap = new Dictionary<BDD, int>();
            int nextLabelId = 0;
            Func<BDD, int> getId = bdd =>
            {
                int bddid;
                if (!idMap.TryGetValue(bdd, out bddid))
                {
                    bddid = nextLabelId++;
                    idMap[bdd] = bddid;
                }

                return bddid;
            };

            HashSet<BDD> done = new HashSet<BDD>();
            SimpleStack<BDD> todo = new SimpleStack<BDD>();
            todo.Push(pred);
            done.Add(pred);
            StringBuilder leaves = new StringBuilder();
            while (todo.IsNonempty)
            {
                var bdd = todo.Pop();
                if (bdd.IsLeaf)
                {
                    leaves.Append(String.Format("\r\n        P{0}_{1}: return {2};", methid, getId(bdd), bdd.IsEmpty ? "false" : "true"));
                }
                else
                {
                    BDD exit = !bdd.Zero.IsLeaf && (bdd.Zero.Zero == bdd.One || bdd.Zero.One == bdd.One) ? bdd.One : bdd.Zero;
                    BDD continuation = bdd;
                    int zeros = 0;
                    int ones = 0;
                    while (true)
                    {
                        if (exit == continuation.Zero)
                        {
                            zeros |= 1 << continuation.Ordinal;
                            continuation = continuation.One;
                        }
                        else if (exit == continuation.One)
                        {
                            ones |= 1 << continuation.Ordinal;
                            continuation = continuation.Zero;
                        }
                        else
                        {
                            break;
                        }
                    }

                    var anyZero = string.Format("(c & 0x{0:X}) != 0x{0:X}", zeros);
                    var anyOne = string.Format("(c & 0x{0:X}) != 0", ones);
                    var exitCondition = zeros == 0 ? anyOne : ones == 0 ? anyZero : string.Format("({0}) || ({1})", anyOne, anyZero);
                    code.Append(string.Format(@"
        "));
                    if (bdd != pred)
                    {
                        code.Append(string.Format("P{0}_{1}: ", methid, getId(bdd)));
                    }

                    code.Append(string.Format("if ({2}) goto P{0}_{3}; else goto P{0}_{4};", methid, getId(bdd), exitCondition, getId(exit), getId(continuation)));
                    if (done.Add(exit))
                    {
                        todo.Push(exit);
                    }

                    if (done.Add(continuation))
                    {
                        todo.Push(continuation);
                    }
                }
            }

            code.Append(leaves.ToString());
        }

        //private static void GenerateCodeForBDD(StringBuilder code, BDD pred, string methid)
        //{
        //    code.Append(string.Format("return {0};", RangesToCode(pred.ToRanges())));
        //}

        /// <summary>
        /// Generate a string representing a predicate that is equivalent to the BDD pred
        /// </summary>
        public string GeneratePredicate(BDD pred)
        {
            if (!pred.IsLeaf && pred.Ordinal > 31)
            {
                throw new AutomataException(AutomataExceptionKind.OrdinalIsTooLarge);
            }

            string res;
            if (!this.predicateCache.TryGetValue(pred, out res))
            {
                if (this.optimzeForASCIIinput)
                {
                    var predascii = pred.And(this.ascii);
                    var predascii_ranges = predascii.ToRanges();
                    var prednonascii = pred.Diff(this.ascii);
                    if (prednonascii.IsEmpty)
                    {
                        res = RangesToCode(predascii_ranges);
                    }
                    else
                    {
                        var asciiCase = RangesToCode(predascii_ranges);
                        var nonasciiCase = this.GeneratePredicateHelper(prednonascii);
                        res = string.Format("(c <= 0x7F ? {0} : {1})", asciiCase, nonasciiCase);
                    }
                }
                else
                {
                    res = this.GeneratePredicateHelper(pred);
                }

                this.predicateCache[pred] = res;
            }

            return res;
        }

        /// <summary>
        /// Predicate for the BDD predicate
        /// </summary>
        private string GeneratePredicateHelper(BDD pred)
        {
            string res;

            //generate a predicate depending on how complex the condition is 
            if (!this.predicateCache.TryGetValue(pred, out res))
            {
                if (pred.IsLeaf)
                {
                    if (pred.IsEmpty)
                    {
                        res = "false";
                    }
                    else if (pred.IsFull)
                    {
                        res = "true";
                    }
                    else
                    {
                        throw new AutomataException(AutomataExceptionKind.UnexpectedMTBDDTerminal);
                    }
                }
                else
                {
                    //if there is a single node in the BDD then 
                    //just inline the corresponding bit mask operation
                    if (pred.One.IsLeaf && pred.Zero.IsLeaf)
                    {
                        res = string.Format("c & 0x{0:X} {1} 0", 1 << pred.Ordinal, pred.One.IsFull ? "!=" : "==");
                    }
                    else
                    {
                        var ranges = pred.ToRanges();
                        if (ranges.Length <= 3)
                        {
                            res = RangesToCode(ranges);
                        }
                        else
                        {
                            //generate a method for checking this condition
                            StringBuilder helper_method = new StringBuilder();

                            //create a new method for this predicate
                            var methid = this.helperPredicates.Count;
                            var methodName = string.Format("{0}_predicate{1}", this.prefix, methid);

                            GenerateCodeForBDD(helper_method, pred, methid.ToString());

                            //                            helper_method.Append("        return ").Append(RangesToCode(ranges));
                            this.helperPredicates.Add(new Predicate { Name = methodName, Body = helper_method.ToString() });
                            res = string.Format("{0}(c)", methodName);
                        }
                    }
                }

                this.predicateCache[pred] = res;
            }

            return res;
        }

        /// <summary>
        /// Format name and body
        /// </summary>
        public string Format(Func<string, string, string> nameAndBodyToDeclaration)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var predicate in this.helperPredicates)
            {
                sb.AppendLine(nameAndBodyToDeclaration(predicate.Name, predicate.Body));
            }

            return sb.ToString();
        }

        //private static string RangesToCode(Tuple<uint, uint>[] ranges)
        //{
        //    StringBuilder cond = new StringBuilder();
        //    foreach (var range in ranges)
        //    {
        //        if (cond.Length > 0)
        //            cond.Append(" || ");
        //        if (range.Item1 == range.Item2)
        //            cond.AppendFormat("c == 0x{0:X}", range.Item1);
        //        else
        //            cond.AppendFormat("(0x{0:X} <= c && c <= 0x{1:X})", range.Item1, range.Item2);
        //    }
        //    return cond.ToString();
        //}

        /// <summary>
        /// Convert the pairs into a range condition expression
        /// </summary>
        private static string RangesToCode(Tuple<uint, uint>[] ranges)
        {
            if (ranges.Length == 0)
            {
                return "false";
            }

            return RangesToCode2(ranges, 0, ranges.Length - 1);
        }

        /// <summary>
        /// Convert the pairs into a range condition expression helper function
        /// </summary>
        private static string RangesToCode2(Tuple<uint, uint>[] ranges, int first, int last)
        {
            if (first == last)
            {
                if (ranges[first].Item1 == ranges[first].Item2)
                {
                    return string.Format("(c == 0x{0:X})", ranges[first].Item1);
                }
                else
                {
                    return string.Format("(0x{0:X} <= c && c <= 0x{1:X})", ranges[first].Item1, ranges[first].Item2);
                }
            }
            else if ((last == (first + 1)) && (ranges[first].Item1 == ranges[first].Item2) && (ranges[last].Item1 == ranges[last].Item2))
            {
                return string.Format("(c == 0x{0:X} || c == 0x{1:X})", ranges[first].Item1, ranges[last].Item1);
            }
            else
            {
                int middle = (first + last + 1) / 2;
                string s1 = RangesToCode2(ranges, first, middle - 1);
                string s2 = RangesToCode3(ranges, middle, last);
                return string.Format("(c < 0x{0:X} ? {1} : {2})", ranges[middle].Item1, s1, s2);
            }
        }

        /// <summary>
        /// Helper method for ranges to code generation that uses binary search in intervals
        /// </summary>
        private static string RangesToCode3(Tuple<uint, uint>[] ranges, int first, int last)
        {
            //we know that c >= ranges[first].Item1
            if (first == last)
            {
                return string.Format("(c <= 0x{0:X})", ranges[first].Item2);
            }
            else
            {
                int middle = (first + last + 1) / 2;
                string s1 = RangesToCode3(ranges, first, middle - 1);
                string s2 = RangesToCode3(ranges, middle, last);
                return string.Format("(c < 0x{0:X} ? {1} : {2})", ranges[middle].Item1, s1, s2);
            }
        }
    }

    /// <summary>
    /// Converts like expressions to Automata
    /// </summary>
    internal class LikePatternToAutomatonConverter<S>
    {
        /// <summary>
        /// Automaton converter.
        /// </summary>
        interface ILikeNode
        {
            /// <summary>
            /// Convert to automaton
            /// </summary>
            Automaton<S> ToAutomaton(RegexToAutomatonBuilder<ILikeNode, S> builder, ICharAlgebra<S> solver);
        }

        /// <summary>
        /// Root node of a Like node.
        /// </summary>
        class LikeRoot : ILikeNode
        {
            /// <summary>
            /// children of the node
            /// </summary>
            ILikeNode[] children;

            /// <summary>
            /// Construct a root node with given child nodes
            /// </summary>
            public LikeRoot(IEnumerable<ILikeNode> children)
            {
                this.children = children.ToArray();
            }

            /// <summary>
            /// Convert to Automaton
            /// </summary>
            public Automaton<S> ToAutomaton(RegexToAutomatonBuilder<ILikeNode, S> builder, ICharAlgebra<S> solver)
            {
                return builder.MkConcatenate(this.children, implicitAnchors: true);
            }
        }

        /// <summary>
        /// Represents a string element.
        /// </summary>
        class LikeString : ILikeNode
        {
            /// <summary>
            /// characters in the string
            /// </summary>
            char[] chars;

            /// <summary>
            /// Create a string element with given characters
            /// </summary>
            public LikeString(IEnumerable<char> chars)
            {
                this.chars = chars.ToArray();
            }

            /// <summary>
            /// Convert to automaton
            /// </summary>
            public Automaton<S> ToAutomaton(RegexToAutomatonBuilder<ILikeNode, S> builder, ICharAlgebra<S> solver)
            {
                return builder.MkSeq(this.chars.Select(c => solver.MkRangeConstraint(c, c)).ToArray());
            }
        }

        /// <summary>
        /// Represents a character set.
        /// </summary>
        class LikeCharSet : ILikeNode
        {
            /// <summary>
            /// characters in the set
            /// </summary>
            char[] set;

            /// <summary>
            /// flag is true if the set is negated
            /// </summary>
            bool negate;

            /// <summary>
            /// Construct a character set with the given characters
            /// </summary>
            public LikeCharSet(IEnumerable<char> set, bool negate)
            {
                this.set = set.ToArray();
                this.negate = negate;
            }

            /// <summary>
            /// Convert to automaton
            /// </summary>
            public Automaton<S> ToAutomaton(RegexToAutomatonBuilder<ILikeNode, S> builder, ICharAlgebra<S> solver)
            {
                if (this.set.Length == 0)
                {
                    return builder.MkSeq(solver.False);
                }

                var moveCond = solver.MkOr(this.set.Select(c => solver.MkCharConstraint(c)));
                moveCond = this.negate ? solver.MkNot(moveCond) : moveCond;
                return builder.MkSeq(moveCond);
            }
        }

        /// <summary>
        /// Represents a character range.
        /// </summary>
        class LikeCharRange : ILikeNode
        {
            /// <summary>
            /// start and end elements
            /// </summary>
            char start, end;

            /// <summary>
            /// true if the range is negated
            /// </summary>
            bool negate;

            /// <summary>
            /// Constructs a range
            /// </summary>
            public LikeCharRange(char start, char end, bool negate)
            {
                this.start = start;
                this.end = end;
                this.negate = negate;
            }

            /// <summary>
            /// Convert to automaton
            /// </summary>
            public Automaton<S> ToAutomaton(RegexToAutomatonBuilder<ILikeNode, S> builder, ICharAlgebra<S> solver)
            {
                var moveCond = solver.MkRangeConstraint(this.start, this.end);
                moveCond = this.negate ? solver.MkNot(moveCond) : moveCond;
                return builder.MkSeq(moveCond);
            }
        }

        /// <summary>
        /// Represents an any node.
        /// </summary>
        class LikeAny : ILikeNode
        {
            /// <summary>
            /// Convert to automaton
            /// </summary>
            public Automaton<S> ToAutomaton(RegexToAutomatonBuilder<ILikeNode, S> builder, ICharAlgebra<S> solver)
            {
                return builder.MkSeq(solver.True);
            }
        }

        /// <summary>
        /// Represents an wildcard.
        /// </summary>
        class LikeWildcard : ILikeNode
        {
            /// <summary>
            /// Convert to automaton
            /// </summary>
            public Automaton<S> ToAutomaton(RegexToAutomatonBuilder<ILikeNode, S> builder, ICharAlgebra<S> solver)
            {
                return builder.MkLoop(new LikeAny(), 0, int.MaxValue);
            }
        }

        /// <summary>
        /// Represents a false node
        /// </summary>
        class LikeFalse : ILikeNode
        {
            /// <summary>
            /// Convert to automaton
            /// </summary>
            public Automaton<S> ToAutomaton(RegexToAutomatonBuilder<ILikeNode, S> builder, ICharAlgebra<S> solver)
            {
                return builder.MkSeq(solver.False);
            }
        }

        /// <summary>
        /// Underlying character solver.
        /// </summary>
        ICharAlgebra<S> solver;

        /// <summary>
        /// Underlying regex to automaton builder.
        /// </summary>
        RegexToAutomatonBuilder<ILikeNode, S> builder;

        /// <summary>
        /// Create instance of LikeToAutomatonConverter for a given character solver.
        /// </summary>
        public LikePatternToAutomatonConverter(ICharAlgebra<S> solver)
        {
            this.solver = solver;
            this.builder = new RegexToAutomatonBuilder<ILikeNode, S>(solver, this.TokenToAutomaton);
            //add implicit start and end anchors
            this.builder.isBeg = false;
            this.builder.isEnd = false;
        }

        /// <summary>
        /// Convert expression to automaton
        /// </summary>
        public Automaton<S> Convert(string expression, char? escapeCharacter)
        {
            var node = this.ParseString(expression.GetEnumerator(), escapeCharacter);
            return node.ToAutomaton(this.builder, this.solver);
        }

        /// <summary>
        /// Like node parser from string
        /// </summary>
        ILikeNode ParseString(CharEnumerator chars, char? escapeCharacter)
        {
            List<ILikeNode> nodes = new List<ILikeNode>();
            List<char> currentString = null;

            Action<ILikeNode> appendNonCharNode = n =>
            {
                if (currentString != null)
                {
                    nodes.Add(new LikeString(currentString));
                    currentString = null;
                }

                nodes.Add(n);
            };

            Action<char> appendCharNode = c =>
            {
                if (currentString == null)
                {
                    currentString = new List<char>();
                }

                currentString.Add(c);
            };

            while (chars.MoveNext())
            {
                switch (chars.Current)
                {
                    case '%':
                        appendNonCharNode(new LikeWildcard());
                        break;
                    case '_':
                        appendNonCharNode(new LikeAny());
                        break;
                    case '[':
                        var classNode = this.ParseCharClass(chars, escapeCharacter);
                        if (classNode == null)
                        {
                            // Match the behavior of ScopeLikeUtil in ScopeContainers.h
                            return new LikeFalse();
                        }

                        appendNonCharNode(classNode);
                        break;
                    default:
                        if (escapeCharacter.HasValue && escapeCharacter.Value == chars.Current)
                        {
                            // Match the behavior of ScopeLikeUtil in ScopeContainers.h
                            if (!chars.MoveNext())
                            {
                                return new LikeFalse();
                            }
                        }

                        appendCharNode(chars.Current);
                        break;
                }
            }

            if (currentString != null)
            {
                nodes.Add(new LikeString(currentString));
                currentString = null;
            }

            return new LikeRoot(nodes);
        }

        /// <summary>
        /// Like node parser from character class
        /// </summary>
        ILikeNode ParseCharClass(CharEnumerator chars, char? escapeCharacter)
        {
            if (!chars.MoveNext())
            {
                return null;
            }

            bool negate = false;
            if (chars.Current == '^')
            {
                negate = true;
                if (!chars.MoveNext())
                {
                    return null;
                }

                if (chars.Current == ']')
                {
                    return new LikeString("^");
                }
            }

            string insides = string.Empty;
            var escaped = new List<bool>();

            while (true)
            {
                if (escapeCharacter.HasValue && escapeCharacter.Value == chars.Current)
                {
                    if (!chars.MoveNext())
                    {
                        return null;
                    }

                    insides += chars.Current;
                    escaped.Add(true);
                }
                else
                {
                    if (chars.Current == ']')
                    {
                        break;
                    }

                    insides += chars.Current;
                    escaped.Add(false);
                }

                if (!chars.MoveNext())
                {
                    return null;
                }
            }

            if (insides.Length == 3 && insides[1] == '-' && !escaped[1])
            {
                return new LikeCharRange(insides[0], insides[2], negate);
            }
            else
            {
                var filteredChars = insides
                    .Select((character, index) => new { character, index })
                    .Zip(escaped, (x, isEscaped) => new { x.character, x.index, isEscaped })
                    .Where(x => x.character != '-' || x.isEscaped || x.index == 0 || x.index == insides.Length - 1)
                    .Select(x => x.character);

                return new LikeCharSet(filteredChars, negate);
            }
        }

        /// <summary>
        /// Convert token to auomaton
        /// </summary>
        Automaton<S> TokenToAutomaton(ILikeNode token)
        {
            return token.ToAutomaton(this.builder, this.solver);
        }
    }
}
