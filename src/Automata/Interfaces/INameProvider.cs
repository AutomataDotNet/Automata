using System;

namespace Microsoft.Automata
{
    /// <summary>
    /// Allows to get and set a name as a string.
    /// </summary>
    public interface INameProvider
    {
        /// <summary>
        /// Gets and sets a name.
        /// </summary>
        string Name { get; set; }
    }
}
