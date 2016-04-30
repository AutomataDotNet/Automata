using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Microsoft.Automata
{
    public interface ICompiledStringMatcher
    {
        string SourceCode { get; }
        bool IsMatch(string input); 
    }
}
