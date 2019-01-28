using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Automata
{
    public interface ICounter
    {
        int LowerBound { get; }
        int UpperBound { get; }
        string CounterName { get; }
        bool ContainsTheImmediateNestedCounter(ICounter counter);
    }
}
