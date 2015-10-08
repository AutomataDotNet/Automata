using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Automata
{
    /// <summary>
    /// Solver interface. Created from IContext.
    /// </summary>
    public interface ISolver<TERM>
    {
        /// <summary>
        /// Push a new logical context.
        /// </summary>
        void Push();

        /// <summary>
        /// Pop the current logical context.
        /// </summary>
        void Pop();

        /// <summary>
        /// All asserted constraints.
        /// </summary>
        TERM[] Assertions { get; }

        /// <summary>
        /// Number of asserted constraints.
        /// </summary>
        uint NumAssertions { get; }

        /// <summary>
        /// Check if the asserted constraints are satisfiable. 
        /// </summary>
        bool Check();

        /// <summary>
        /// Assert the formula in the solver.
        /// </summary>
        /// <param name="constraint"></param>
        void Assert(TERM constraint);

        /// <summary>
        /// Check satisfiability of the constraint in the solver without changing the assertions.
        /// </summary>
        bool IsSatisfiable(TERM constraint);

        #region Model generation

        /// <summary>
        /// If the assertion is satisfiable, returns an interpretation of the terms to be evaluated.
        /// The returned dictionary is empty if no terms are listed for evaluation.
        /// The assertion may not contain free variables.
        /// Returns null iff the assertion is unsatisfiable.
        /// </summary>
        IDictionary<TERM, IValue<TERM>> GetModel(TERM assertion, params TERM[] termsToEvaluate);

        /// <summary>
        /// Find all solutions of an open formula containing exactly one free variable.
        /// </summary>
        IEnumerable<IValue<TERM>> FindAllMembers(TERM openFormula);

        /// <summary>
        /// Find one solution of an open formula containing exactly one free variable.
        /// </summary>
        /// <param name="openFormula">formula containing exactly one free variable</param>
        /// <returns>a value satisfying the formula or null if no value was found</returns>
        IValue<TERM> FindOneMember(TERM openFormula);

        #endregion
    }
}
