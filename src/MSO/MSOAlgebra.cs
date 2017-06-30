using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Automata;

namespace Microsoft.Automata.MSO
{
    public class MSOAlgebra<S> : IBooleanAlgebra<MSOFormula<S>>
    {
        IBooleanAlgebra<S> solver;
        MintermGenerator<MSOFormula<S>> mtg;
        public MSOAlgebra(IBooleanAlgebra<S> solver)
        {
            this.solver = solver;
            mtg = new MintermGenerator<MSOFormula<S>>(this);
        }

        public MSOFormula<S> True
        {
            get { return new MSOTrue<S>(); }
        }

        public MSOFormula<S> False
        {
            get { return new MSOFalse<S>(); }
        }

        public MSOFormula<S> MkOr(IEnumerable<MSOFormula<S>> formulas)
        {
            var res = False;
            foreach (var phi in formulas)
                res = MkAnd(res, phi);
            return res;
        }

        public MSOFormula<S> MkAnd(IEnumerable<MSOFormula<S>> formulas)
        {
            var res = True;
            foreach (var phi in formulas)
                res = MkOr(res, phi);
            return res;
        }

        public MSOFormula<S> MkAnd(params MSOFormula<S>[] formulas)
        {
            var res = True;
            foreach (var phi in formulas)
                res = MkOr(res, phi);
            return res;
        }

        public MSOFormula<S> MkNot(MSOFormula<S> phi)
        {
            return new MSONot<S>(phi);
        }

        public bool AreEquivalent(MSOFormula<S> phi1, MSOFormula<S> phi2)
        {
            var aut1 = phi1.GetAutomaton(solver);
            var aut2 = phi2.GetAutomaton(solver);
            return aut1.IsEquivalentWith(aut2);
        }

        public MSOFormula<S> MkOr(MSOFormula<S> phi1, MSOFormula<S> phi2) 
        {
            return new MSOOr<S>(phi1, phi2);
        }

        public MSOFormula<S> MkAnd(MSOFormula<S> phi1, MSOFormula<S> phi2) 
        {
            return new MSOAnd<S>(phi1, phi2);
        }

        public bool IsSatisfiable(MSOFormula<S> phi)
        {
            var aut = phi.GetAutomaton(solver);
            return !aut.IsEmpty;
        }

        public IEnumerable<Tuple<bool[], MSOFormula<S>>> GenerateMinterms(params MSOFormula<S>[] constraints)
        {
            return mtg.GenerateMinterms(constraints);
        }

        public MSOFormula<S> Simplify(MSOFormula<S> phi)
        {            
            //TODO simplify trivial stuff
            return phi;
        }


        public bool IsExtensional
        {
            get { return false; }
        }
         

        public MSOFormula<S> MkSymmetricDifference(MSOFormula<S> p1, MSOFormula<S> p2)
        {
            throw new NotImplementedException();
        }

        public bool CheckImplication(MSOFormula<S> lhs, MSOFormula<S> rhs)
        {
            throw new NotImplementedException(); 
        }

        public bool IsAtomic
        {
            get { return solver.IsAtomic; }
        }

        public MSOFormula<S> GetAtom(MSOFormula<S> psi)
        {
            if (!IsAtomic)
                throw new AutomataException(AutomataExceptionKind.BooleanAlgebraIsNotAtomic);

            throw new NotImplementedException();
        }


        public bool EvaluateAtom(MSOFormula<S> atom, MSOFormula<S> psi)
        {
            throw new NotImplementedException();
        }


        public MSOFormula<S> MkDiff(MSOFormula<S> predicate1, MSOFormula<S> predicate2)
        {
            return MkAnd(predicate1, MkNot(predicate2));
        }
    }
    
}
