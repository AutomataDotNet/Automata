using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Automata
{
    /// <summary>
    /// Builder from elements of type T into symbolic regexes over S. 
    /// S is the type of elements of an effective Boolean algebra.
    /// T is the type of an AST of concrete regexes.
    /// Used as the main building block for converting regexes to symbolic regexes over S.
    /// </summary>
    internal class SymbolicRegexBuilder<T,S>
    {
        internal SymbolicRegex<S> epsilon;  
        internal SymbolicRegex<S> startAnchor;
        internal SymbolicRegex<S> endAnchor;
        internal SymbolicRegex<S> bolAnchor;
        internal SymbolicRegex<S> eolAnchor;
        internal SymbolicRegex<S> newLine;
        internal SymbolicRegex<S> dot;
        internal SymbolicRegex<S> all;
        internal SymbolicRegex<S> bolRegex;
        internal SymbolicRegex<S> eolRegex;
        ICharAlgebra<S> solver;
        //internal SymbolicRegex<S> nothing;
        /// <summary>
        /// Create a new incremental symbolic regex builder.
        /// </summary>
        /// <param name="solver">Effective Boolean algebra over S.</param>
        /// <param name="describe">Custom ToString function for S</param>
        public SymbolicRegexBuilder(ICharAlgebra<S> solver)
        {
            this.solver = solver;
            this.epsilon = SymbolicRegex<S>.MkEpsilon();
            this.startAnchor = SymbolicRegex<S>.MkStartAnchor();
            this.endAnchor = SymbolicRegex<S>.MkEndAnchor();
            this.eolAnchor = SymbolicRegex<S>.MkEolAnchor();
            this.bolAnchor = SymbolicRegex<S>.MkBolAnchor();
            this.newLine = SymbolicRegex<S>.MkSingleton(solver.MkCharConstraint('\n'), solver.PrettyPrint);
            this.dot = SymbolicRegex<S>.MkSingleton(solver.True, solver.PrettyPrint);
            this.all = SymbolicRegex<S>.MkLoop(this.dot, 0, int.MaxValue);
            this.bolRegex = SymbolicRegex<S>.MkLoop(SymbolicRegex<S>.MkConcat(this.all, this.newLine), 0, 1);
            this.eolRegex = SymbolicRegex<S>.MkLoop(SymbolicRegex<S>.MkConcat(this.newLine, this.all), 0, 1);
            //this.nothing = SymbolicRegex<S>.MkSingleton(solver.False, describe);
        }

        /// <summary>
        /// Make a disjunction of given regexes
        /// </summary>
        public SymbolicRegex<S> MkOr(params SymbolicRegex<S>[] regexes)
        {
            if (regexes.Length < 1)
                throw new AutomataException(AutomataExceptionKind.InvalidArgument);

            var sr = regexes[regexes.Length - 1];
            for (int i = regexes.Length - 2; i >= 0; i--)
                sr = SymbolicRegex<S>.MkOr(regexes[i], sr);

            return sr;
        }

        /// <summary>
        /// Make a concatenation of given regexes, if any regex is nothing then return nothing
        /// </summary>
        public SymbolicRegex<S> MkConcat(params SymbolicRegex<S>[] regexes)
        {
            if (regexes.Length < 1)
                throw new AutomataException(AutomataExceptionKind.InvalidArgument);

            var sr = regexes[regexes.Length - 1];
            if (IsNothing(sr))
            {
                return sr;
            }
            else
            {
                for (int i = regexes.Length - 2; i >= 0; i--)
                {
                    if (IsNothing(regexes[i]))
                    {
                        return regexes[i];
                    }
                    else
                    {
                        sr = SymbolicRegex<S>.MkConcat(regexes[i], sr);
                    }
                }
                return sr;
            }
        }

        /// <summary>
        /// Make loop regex
        /// </summary>
        public SymbolicRegex<S> MkLoop(SymbolicRegex<S> regex, int lower = 0, int upper = int.MaxValue)
        {
            return SymbolicRegex<S>.MkLoop(regex, lower, upper);
        }

        /// <summary>
        /// Make a singleton sequence regex
        /// </summary>
        public SymbolicRegex<S> MkSingleton(S set)
        {
            var s = SymbolicRegex<S>.MkSingleton(set, solver.PrettyPrint);
            return s;
        }

        /// <summary>
        /// Make an if-then-else regex (?(cond)left|right)
        /// </summary>
        /// <param name="cond">condition</param>
        /// <param name="left">true case</param>
        /// <param name="right">false case</param>
        /// <returns></returns>
        public SymbolicRegex<S> MkIfThenElse(SymbolicRegex<S> cond, SymbolicRegex<S> left, SymbolicRegex<S> right)
        {
            return SymbolicRegex<S>.MkIfThenElse(cond, left, right);
        }

        internal bool IsAll(SymbolicRegex<S> loop)
        {
            return loop.IsStar && loop.Left.Kind == SymbolicRegexKind.Singleton && this.solver.True.Equals(loop.Left.Set);
        }
        internal bool IsNothing(SymbolicRegex<S> elem)
        {
            return elem.Kind == SymbolicRegexKind.Singleton && this.solver.False.Equals(elem.Set);
        }
    }
}
