using System;

namespace Microsoft.Automata
{
    /// <summary>
    /// Pretty printer for a given type.
    /// </summary>
    /// <typeparam name="T">element type</typeparam>
    public interface IPrettyPrinter<T>
    {
        /// <summary>
        /// Returns a string representation of the element t
        /// </summary>
        string PrettyPrint(T t);

        /// <summary>
        /// Returns a string representation of the element t, uses varLookup for custom variable name lookup.
        /// </summary>
        string PrettyPrint(T t, Func<T,string> varLookup);

        /// <summary>
        /// Returns a string representation of the element t, uses varLookup for custom variable name lookup.
        /// Uses the form (cond ? t : f) instead of ite(cond,t,f) for printing ite-terms.
        /// </summary>
        string PrettyPrintCS(T t, Func<T, string> varLookup);
    }
}
