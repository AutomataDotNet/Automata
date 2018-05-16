using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Automata.Utilities
{
    internal class IgnoreCaseTransformer
    {
        BDD IgnoreCaseRel;
        BDD domain;
        CharSetSolver solver;

        public IgnoreCaseTransformer(CharSetSolver charSetSolver)
        {
            this.solver = charSetSolver;
            IgnoreCaseRel = charSetSolver.Deserialize(Microsoft.Automata.Generated.IgnoreCaseRelation.ignorecase);
            domain = IgnoreCaseRel.ShiftRight(16);
        }

        /// <summary>
        /// For all letters in the bdd add their lower and upper case equivalents.
        /// </summary>
        public BDD Apply(BDD bdd)
        {
            if (domain.And(bdd).IsEmpty)
                return bdd;
            else
            {
                var ignorecase = bdd.And(IgnoreCaseRel).ShiftRight(16);
                var res = ignorecase.Or(bdd);
                return res;
            }
        }

        public bool IsInDomain(char c)
        {
            BDD c_bdd = solver.MkCharConstraint(c);
            if (c_bdd.And(domain).IsEmpty)
                return false;
            else
                return true;
        }
    }
}
