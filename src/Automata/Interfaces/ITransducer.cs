using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Automata
{
    /// <summary>
    /// Extends the IAutomaton interface with transducer specific methods.
    /// </summary>
    public interface ITransducer<RULE> : IAutomaton<RULE>
    {
        /// <summary>
        /// Returns true iff the label is a final rule.
        /// </summary>
        bool IsFinalRule(RULE rule);

        /// <summary>
        /// Provides a description of the guard of the rule.
        /// </summary>
        string DescribeGuard(RULE rule);

        /// <summary>
        /// Returns true iff the guard of the rule is unconditionally true.
        /// </summary>
        bool IsGuardTrue(RULE rule);

        /// <summary>
        /// Returns the nr of yields of the rule.
        /// </summary>
        int GetYieldsLength(RULE rule);

        /// <summary>
        /// Provides a description of the yields of the rule.
        /// </summary>
        string DescribeYields(RULE rule);

        /// <summary>
        /// Provides a description of the update of the rule.
        /// </summary>
        string DescribeUpdate(RULE rule);
    }
}
