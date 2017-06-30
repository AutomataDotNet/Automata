using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Automata
{
    /// <summary>
    /// Boolean algebra whose predicates are Automata over S.
    /// </summary>
    internal class AutomataAlgebra<S> : IBooleanAlgebra<Automaton<S>>  
    {
        IBooleanAlgebra<S> solver;
        MintermGenerator<Automaton<S>> mtg;

        Automaton<S> empty;
        Automaton<S> full;

        /// <summary>
        /// Construct the automata algebra for the character solver for the predicate automata.
        /// </summary>
        /// <param name="solver"></param>
        public AutomataAlgebra(IBooleanAlgebra<S> solver)
        {
            this.solver = solver;
            mtg = new MintermGenerator<Automaton<S>>(this);
            this.empty = Automaton<S>.MkEmpty(solver);
            this.full = Automaton<S>.MkFull(solver);
        }

        /// <summary>
        /// Automaton that accepts everything.
        /// </summary>
        public Automaton<S> True
        {
            get { return full; }
        }

        /// <summary>
        /// Automaton that accepts nothing.
        /// </summary>
        public Automaton<S> False
        {
            get { return empty; }
        }

        /// <summary>
        /// Make a union automaton of the given automata
        /// </summary>
        public Automaton<S> MkOr(IEnumerable<Automaton<S>> automata)
        {
            var res = False;
            foreach (var aut in automata)
                res = Automaton<S>.MkSum(res, aut);
            return res;
        }

        /// <summary>
        /// Make an product automaton of the given automata
        /// </summary>
        public Automaton<S> MkAnd(IEnumerable<Automaton<S>> automata)
        {
            var res = True;
            foreach (var aut in automata)
                res = Automaton<S>.MkProduct(res, aut);
            return res;
        }

        /// <summary>
        /// Make an product automaton of the given automata
        /// </summary>
        public Automaton<S> MkAnd(params Automaton<S>[] automata)
        {
            var res = True;
            foreach (var aut in automata)
                res = Automaton<S>.MkProduct(res, aut);
            return res;
        }

        /// <summary>
        /// Complement the automaton
        /// </summary>
        /// <param name="aut"></param>
        /// <returns></returns>
        public Automaton<S> MkNot(Automaton<S> aut)
        {
            return aut.Complement();
        }

        /// <summary>
        /// Return true iff the two automata are equivalent
        /// </summary>
        public bool AreEquivalent(Automaton<S> aut1, Automaton<S> aut2)
        {
            return aut1.IsEquivalentWith(aut2);
        }

        /// <summary>
        /// Make a union automaton of the two automata
        /// </summary>
        public Automaton<S> MkOr(Automaton<S> aut1, Automaton<S> aut2) 
        {
            var res = Automaton<S>.MkSum(aut1, aut2);
            return res;
        }

        /// <summary>
        /// Make a product automaton of the two automata
        /// </summary>
        public Automaton<S> MkAnd(Automaton<S> aut1, Automaton<S> aut2)
        {
            var res = Automaton<S>.MkProduct(aut1, aut2);
            return res;
        }

        /// <summary>
        /// Returns true if the automaton accepts at least one input
        /// </summary>
        public bool IsSatisfiable(Automaton<S> aut)
        {
            return !aut.IsEmpty;
        }

        /// <summary>
        /// Generate minterms for the constraints
        /// </summary>
        public IEnumerable<Tuple<bool[], Automaton<S>>> GenerateMinterms(params Automaton<S>[] constraints)
        {
            return mtg.GenerateMinterms(constraints);
        }

        /// <summary>
        /// Minimizes the automaton
        /// </summary>
        public Automaton<S> Simplify(Automaton<S> aut)
        {            
            return aut.Determinize().Minimize();
        }

        /// <summary>
        /// Returns false
        /// </summary>
        public bool IsExtensional
        {
            get { return false; }
        }

        /// <summary>
        /// Makes the su=ymmetric difference
        /// </summary>
        public Automaton<S> MkSymmetricDifference(Automaton<S> p1, Automaton<S> p2)
        {
            return MkOr(MkAnd(p1, MkNot(p2)), MkAnd(p2, MkNot(p1)));
        }

        /// <summary>
        /// Returns true if L(lhs) is a subset of L(rhs)
        /// </summary>
        public bool CheckImplication(Automaton<S> lhs, Automaton<S> rhs)
        {
            return MkAnd(lhs, MkNot(rhs)).IsEmpty;
        }

        /// <summary>
        /// Retuns true iff the underlying character solver is atomic.
        /// </summary>
        public bool IsAtomic
        {
            get { return solver.IsAtomic; }
        }

        /// <summary>
        /// Gets the atom of a predicate.
        /// </summary>
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

        /// <summary>
        /// not implemented
        /// </summary>
        public bool EvaluateAtom(Automaton<S> atom, Automaton<S> psi)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Makes an automaton that accepts L(predicate1) - L(predicate2)
        /// </summary>
        public Automaton<S> MkDiff(Automaton<S> predicate1, Automaton<S> predicate2)
        {
            return MkAnd(predicate1, MkNot(predicate2));
        }
    }

    internal class RegexAlgebra : AutomataAlgebra<BDD>
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
                if (NrOfTrues(minterm.Item1) >= k)
                    yield return minterm.Item2;
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
