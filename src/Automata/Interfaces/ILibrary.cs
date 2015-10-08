using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Automata
{
    public interface ILibrary<TERM>
    {
        void DefineFunction(string name, TERM body, params TERM[] vars);

        TERM ApplyFunction(string name, params TERM[] args);

        void GenerateCode(string language, StringBuilder sb);
    }
}
