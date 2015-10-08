using System;
using System.Collections.Generic;

namespace Microsoft.Automata
{
    /// <summary>
    /// Represents a concrete value
    /// </summary>
    public interface IValue<TERM>
    {
        /// <summary> 
        /// A ground term.
        /// </summary>
        TERM Value { get; }

        /// <summary>
        /// Returns true if Value represents a numeral and outputs the numeral in n.
        /// Returns false otherwise and sets n to 0.
        /// </summary>
        bool TryGetNumeralValue(out int n);

        /// <summary>
        /// If Value is a list whose elements are numerals return 
        /// the corresponding string, otherwise return null.
        /// </summary>
        /// <param name="unicode">if true, extract numeric values above 65536 to proper UTF-16 code points, otherwise truncate them to single characters ignoring all bits in binary representation in positions 17 and higher</param>
        string GetStringValue(bool unicode);

        /// <summary>
        /// If Value is a list whose elements are numerals gets 
        /// GetStringValue(false), otherwise return null.
        /// </summary>
        string StringValue { get; }

        /// <summary>
        /// If the value is a list get the corresponding elements.
        /// </summary>
        List<TERM> GetList();
    }
}
