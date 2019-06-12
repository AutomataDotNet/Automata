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
        int CounterId { get; }
        bool ContainsSubCounter(ICounter counter);
    }
}
