using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Automata
{
    public class AutomataAlgebra<S> : IBooleanAlgebra<Automaton<S>>
    {
        IBooleanAlgebra<S> solver;
        MintermGenerator<Automaton<S>> mtg;

        Automaton<S> empty;
        Automaton<S> full;

        public AutomataAlgebra(IBooleanAlgebra<S> solver)
        {
            this.solver = solver;
            mtg = new MintermGenerator<Automaton<S>>(this);
            this.empty = Automaton<S>.MkEmpty(solver);
            this.full = Automaton<S>.MkFull(solver);
        }

        public Automaton<S> True
        {
            get { return full; }
        }

        public Automaton<S> False
        {
            get { return empty; }
        }

        public Automaton<S> MkOr(IEnumerable<Automaton<S>> automata)
        {
            var res = False;
            foreach (var aut in automata)
                res = Automaton<S>.MkSum(res, aut);
            return res;
        }

        public Automaton<S> MkAnd(IEnumerable<Automaton<S>> automata)
        {
            var res = True;
            foreach (var aut in automata)
                res = Automaton<S>.MkProduct(res, aut);
            return res;
        }

        public Automaton<S> MkAnd(params Automaton<S>[] automata)
        {
            var res = True;
            foreach (var aut in automata)
                res = Automaton<S>.MkProduct(res, aut);
            return res;
        }

        public Automaton<S> MkNot(Automaton<S> aut)
        {
            return aut.Complement();
        }

        public bool AreEquivalent(Automaton<S> aut1, Automaton<S> aut2)
        {
            return aut1.IsEquivalentWith(aut2);
        }

        public Automaton<S> MkOr(Automaton<S> aut1, Automaton<S> aut2) 
        {
            var res = Automaton<S>.MkSum(aut1, aut2);
            return res;
        }

        public Automaton<S> MkAnd(Automaton<S> aut1, Automaton<S> aut2)
        {
            var res = Automaton<S>.MkProduct(aut1, aut2);
            return res;
        }

        public bool IsSatisfiable(Automaton<S> aut)
        {
            return !aut.IsEmpty;
        }

        public IEnumerable<Pair<bool[], Automaton<S>>> GenerateMinterms(params Automaton<S>[] constraints)
        {
            return mtg.GenerateMinterms(constraints);
        }

        public Automaton<S> Simplify(Automaton<S> aut)
        {            
            return aut.Determinize().Minimize();
        }

        public bool IsExtensional
        {
            get { return false; }
        }

        public Automaton<S> MkSymmetricDifference(Automaton<S> p1, Automaton<S> p2)
        {
            return MkOr(MkAnd(p1, MkNot(p2)), MkAnd(p2, MkNot(p1)));
        }

        public bool CheckImplication(Automaton<S> lhs, Automaton<S> rhs)
        {
            return MkAnd(lhs, MkNot(rhs)).IsEmpty;
        }

        public bool IsAtomic
        {
            get { return solver.IsAtomic; }
        }

        public Automaton<S> GetAtom(Automaton<S> psi)
        {
            if (!IsAtomic)
                throw new AutomataException(AutomataExceptionKind.BooleanAlgebraIsNotAtomic);

            if (psi.IsEmpty)
                return psi;

            var path = new List<S>(psi.ChoosePathToSomeFinalState(new Chooser()));
            var moves = new List<Move<S>>();
            for (int i = 0; i < path.Count; i++)
                moves.Add(Move<S>.Create(i, i + 1, solver.GetAtom(path[i])));

            var atom = Automaton<S>.Create(solver, 0, new int[] { path.Count }, moves);
            return atom;
        }


        public bool EvaluateAtom(Automaton<S> atom, Automaton<S> psi)
        {
            throw new NotImplementedException();
        }


        public Automaton<S> MkDiff(Automaton<S> predicate1, Automaton<S> predicate2)
        {
            return MkAnd(predicate1, MkNot(predicate2));
        }
    }

    public class RegexAlgebra : AutomataAlgebra<BDD>
    {
        CharSetSolver solver;
        public RegexAlgebra(CharSetSolver solver) : base(solver)
        {
            this.solver = solver;
        }

        public IEnumerable<Automaton<BDD>> GenerateCandidateParts(int k, params string[] regexes)
        {
            var automata = Array.ConvertAll(regexes, regex => solver.Convert(regex, System.Text.RegularExpressions.RegexOptions.Singleline));
            foreach (var minterm in GenerateMinterms(automata))
                if (NrOfTrues(minterm.First) >= k)
                    yield return minterm.Second;
        }

        public Automaton<BDD> GenerateCandidate(int k, params string[] regexes)
        {
            var cand = MkOr(GenerateCandidateParts(k, regexes));
            return cand;
        }

        public void ComputeCardinality(Automaton<BDD> aut)
        {
            aut.ComputeProbabilities(solver.ComputeDomainSize);
        }

        static int NrOfTrues(bool[] bools)
        {
            int k = 0;
            foreach (var b in bools)
                if (b)
                    k += 1;
            return k;
        }
    }
}
