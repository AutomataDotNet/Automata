using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Automata
{
    class Program
    {
        static void Main(string[] args)
        {
            Utilities.UnicodeCategoryRangesGenerator.Generate("Microsoft.Automata.Generated", "UnicodeCategoryRanges", "");
            Utilities.IgnoreCaseRelationGenerator.Generate("Microsoft.Automata.Generated", "IgnoreCaseRelation", "");
        }
    }
}
