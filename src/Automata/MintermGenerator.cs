using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Automata;

namespace Microsoft.Automata
{

    /// <summary>
    /// Provides a generic implementation for minterm generation over a given Boolean Algebra.
    /// </summary>
    /// <typeparam name="PRED">type of predicates</typeparam>
    public class MintermGenerator<PRED> 
    {
        IBooleanAlgebra<PRED> ba;

        bool hashCodesRespectEquivalence;

        /// <summary>
        /// Constructs a minterm generator for a given Boolean Algebra.
        /// </summary>
        /// <param name="ba">given Boolean Algebra</param>
        public MintermGenerator(IBooleanAlgebra<PRED> ba)
        {
            this.ba = ba;
            hashCodesRespectEquivalence = ba.IsExtensional;
        }

        /// <summary>
        /// Returns GenerateMinterms(true, preds).
        /// </summary>
        public IEnumerable<Tuple<bool[], PRED>> GenerateMinterms(params PRED[] preds)
        {
            return GenerateMinterms(true, preds);
        }

        /// <summary>
        /// Given an array of predidates {p_1, p_2, ..., p_n} where n>=0.
        /// Enumerate all satisfiable Boolean combinations Tuple({b_1, b_2, ..., b_n}, p)
        /// where p is satisfiable and equivalent to p'_1 &amp; p'_2 &amp; ... &amp; p'_n, 
        /// where p'_i = p_i if b_i = true and p'_i is Not(p_i) otherwise.
        /// If n=0 return Tuple({},True).
        /// </summary>
        /// <param name="preds">array of predicates</param>
        /// <param name="useEquivalenceChecking">optimization flag: if true, uses equivalence checking to cluster equivalent predicates; otherwise does not use equivalence checking</param>
        /// <returns>all minterms of the given predicate sequence</returns>
        public IEnumerable<Tuple<bool[], PRED>> GenerateMinterms(bool useEquivalenceChecking, params PRED[] preds)
        {
            if (preds.Length == 0)
            {
                yield return new Tuple<bool[], PRED>(new bool[] { }, ba.True);
            }
            else
            {
                var count = preds.Length;

                List<PRED> nonequivalentSets = new List<PRED>();

                //work only with nonequivalent sets as distinct elements 
                var indexLookup = new Dictionary<int, int>();
                var newIndexMap = new Dictionary<EquivClass, int>();
                var equivs = new List<List<int>>();

                for (int i = 0; i < count; i++)
                {
                    int newIndex;
                    EquivClass equiv = CreateEquivalenceClass(useEquivalenceChecking, preds[i]);
                    if (!newIndexMap.TryGetValue(equiv, out newIndex))
                    {
                        newIndex = newIndexMap.Count;
                        newIndexMap[equiv] = newIndex;
                        nonequivalentSets.Add(preds[i]);
                        equivs.Add(new List<int>());
                    }
                    indexLookup[i] = newIndex;
                    equivs[newIndex].Add(i);
                }

                //var pairs = new List<Tuple<IntSet, PRED>>(GenerateMinterms1(nonequivalentSets.ToArray()));
                //foreach (var pair in pairs)
                //{
                //    var characteristic = new bool[preds.Length];
                //    for (int i = 0; i < count; i++)
                //        if (pair.First.Contains(indexLookup[i]))
                //            characteristic[i] = true;
                //    yield return
                //        new Tuple<bool[], PRED>(characteristic, pair.Second);
                //}

                var tree = new PartitonTree<PRED>(ba);
                foreach (var psi in nonequivalentSets)
                    tree.Refine(psi);
                foreach (var leaf in tree.GetLeaves())
                {
                    var characteristic = new bool[preds.Length];
                    foreach (var k in leaf.GetPath())
                        foreach (var n in equivs[k])
                            characteristic[n] = true;
                    yield return
                        new Tuple<bool[], PRED>(characteristic, leaf.phi);
                }
            }
        }

        IEnumerable<Tuple<IntSet, PRED>> GenerateMinterms1(PRED[] sets)
        {
            var count = sets.Length;

            //first see if all can be false at the same time
            if (!Array.Exists(sets, set => (set.Equals(ba.True))))
            {
                PRED allFalse = ba.MkNot(ba.MkOr(sets));
                if (ba.IsSatisfiable(allFalse))
                    yield return new Tuple<IntSet, PRED>(IntSet.Empty, allFalse);
            }

            //a combination of rank k is a nonempty intersection of k+1 sets[i]'s
            var combinationsByRankMap = new Dictionary<int, Dictionary<IntSet, PRED>>();

            var combinationsOfCurrentRank = new Dictionary<IntSet, PRED>();
            for (int i = 0; i < sets.Length; i++)
                if (ba.IsSatisfiable(sets[i]))
                    combinationsOfCurrentRank.Add(new IntSet(i), sets[i]);

            //combinations of rank 0 are the individual sets[i]'s that are not false
            combinationsByRankMap[0] = combinationsOfCurrentRank;

            //sets of i's such that conjunctions of sets[i]'s are unsatisfiable
            var unsatisfiableCombinations = new HashSet<IntSet>();

            int rankBound = 1;
            for (int rank = 1; rank < count; rank++)
            {
                var combinationsOfNextRank = new Dictionary<IntSet, PRED>();
                foreach (var entry in combinationsOfCurrentRank)
                    for (int j = 0; j < count; j++)
                        if (!entry.Key.Contains(j))
                        {
                            var indexSet = entry.Key.Union(j);
                            if (!unsatisfiableCombinations.Contains(indexSet) &&
                                !combinationsOfNextRank.ContainsKey(indexSet))
                            {
                                var combination = ba.MkAnd(entry.Value, sets[j]);
                                if (!ba.IsSatisfiable(combination))
                                    unsatisfiableCombinations.Add(indexSet);
                                else
                                    combinationsOfNextRank[indexSet] = combination;
                            }
                        }
                if (combinationsOfNextRank.Count == 0)
                {
                    //there are no possible combinations of size rankBound or larger
                    rankBound = rank;
                    break;
                }
                else
                {
                    combinationsByRankMap[rank] = combinationsOfNextRank;
                    combinationsOfCurrentRank = combinationsOfNextRank;
                }
            }

            for (int rank = 0; rank < rankBound; rank++)
            {
                var combinationsOfRank = combinationsByRankMap[rank];
                foreach (var entry in combinationsOfRank)
                {
                    var combination = entry.Value;
                    var indexSet = entry.Key;
                    //add complements of sets[k]'s not in the set
                    for (int k = 0; k < count; k++)
                    {
                        if (!indexSet.Contains(k))
                        {
                            combination = ba.MkAnd(combination, ba.MkNot(sets[k]));
                            //if (!ba.IsSatisfiable(combination))
                            //    break; //the combination is not possible
                        }
                    }
                    if (ba.IsSatisfiable(combination))
                        yield return
                            new Tuple<IntSet, PRED>(indexSet, combination);
                }
            }
            if (combinationsByRankMap.ContainsKey(rankBound))
                foreach (var entry in combinationsByRankMap[rankBound])
                    yield return 
                        new Tuple<IntSet, PRED>(entry.Key, entry.Value);
        }

        EquivClass CreateEquivalenceClass(bool useEquivalenceChecking, PRED set)
        {
            return new EquivClass(useEquivalenceChecking, this, set);
        }

        internal IEnumerable<PartitonTree<PRED>> GenerateMintermsHelper(PRED[] preds)
        {
            var tree = new PartitonTree<PRED>(ba);
            foreach (var psi in preds)
                tree.Refine(psi);
            return tree.GetLeaves();
        }

        private class EquivClass
        {
            PRED set;
            MintermGenerator<PRED> gen;
            bool useEquivalenceChecking;

            internal EquivClass(bool useEquivalenceChecking, MintermGenerator<PRED> gen, PRED set)
            {
                this.set = set;
                this.gen = gen;
                this.useEquivalenceChecking = useEquivalenceChecking;
            }

            public override int GetHashCode()
            {
                if (useEquivalenceChecking && !gen.hashCodesRespectEquivalence)
                    //cannot rely on equivalent predicates having the same hashcode
                    //so all predicates end up in the same bucket that causes a linear search
                    //with Equals to check equivalence when useEquivalenceChecking=true
                    return 0; 
                else
                    return set.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (useEquivalenceChecking)
                    return gen.ba.AreEquivalent(set, ((EquivClass)obj).set);
                else
                    return set.Equals(((EquivClass)obj).set);
            }
        }
    }

    internal class PartitonTree<PRED>
    {
        PartitonTree<PRED> parent;
        int nr;
        internal PRED phi;
        IBooleanAlgebra<PRED> solver;
        PartitonTree<PRED> left;   
        PartitonTree<PRED> right;  //complement
        internal PartitonTree(IBooleanAlgebra<PRED> solver)
        {
            this.solver = solver;
            nr = -1;
            parent = null;
            this.phi = solver.True;
            this.left = null;
            this.right = null;
        }
        PartitonTree(IBooleanAlgebra<PRED> solver, int depth, PartitonTree<PRED> parent, PRED phi, PartitonTree<PRED> left, PartitonTree<PRED> right)
        {
            this.solver = solver;
            this.parent = parent;
            this.nr = depth;
            this.phi = phi;
            this.left = left;
            this.right = right;
        }

        internal void Refine(PRED psi)
        {

            if (left == null && right == null) 
            {
                #region leaf
                var phi_and_psi = solver.MkAnd(phi, psi);
                if (solver.IsSatisfiable(phi_and_psi))
                {
                    var phi_min_psi = solver.MkAnd(phi, solver.MkNot(psi));
                    if (solver.IsSatisfiable(phi_min_psi))
                    {
                        left = new PartitonTree<PRED>(solver, nr + 1, this, phi_and_psi, null, null);
                        right = new PartitonTree<PRED>(solver, nr + 1, this, phi_min_psi, null, null);
                    }
                    else // [[phi]] subset of [[psi]]
                        left = new PartitonTree<PRED>(solver, nr + 1, this, phi, null, null); //psi must true
                }
                else // [[phi]] subset of [[not(psi)]]
                    right = new PartitonTree<PRED>(solver, nr + 1, this, phi, null, null); //psi must be false
                #endregion
            }
            else if (left == null)
                right.Refine(psi);
            else if (right == null)
                left.Refine(psi);
            else
            {
                #region nonleaf
                var phi_and_psi = solver.MkAnd(phi, psi);
                if (solver.IsSatisfiable(phi_and_psi))
                {
                    var phi_min_psi = solver.MkAnd(phi, solver.MkNot(psi));
                    if (solver.IsSatisfiable(phi_min_psi))
                    {
                        left.Refine(psi);
                        right.Refine(psi);
                    }
                    else // [[phi]] subset of [[psi]]
                    {
                        left.ExtendLeft(); //psi is true
                        right.ExtendLeft();
                    }
                }
                else // [[phi]] subset of [[not(psi)]]
                {
                    left.ExtendRight();
                    right.ExtendRight(); //psi is false
                }
                #endregion
            }
        }

        private void ExtendRight()
        {
            if (left == null && right == null)
                right = new PartitonTree<PRED>(solver, nr + 1, this, phi, null, null);
            else if (left == null)
                right.ExtendRight();
            else if (right == null)
                left.ExtendRight();
            else
            {
                left.ExtendRight();
                right.ExtendRight();
            }
        }

        private void ExtendLeft()
        {
            if (left == null && right == null)
                left = new PartitonTree<PRED>(solver, nr + 1, this, phi, null, null);
            else if (left == null)
                right.ExtendLeft();
            else if (right == null)
                left.ExtendLeft();
            else
            {
                left.ExtendLeft();
                right.ExtendLeft();
            }
        }

        internal IEnumerable<int> GetPath()
        {
            for (var curr = this; curr.parent != null; curr = curr.parent)
                if (curr.parent.left == curr) //curr is the left child of its parent
                    yield return curr.nr;
        }

        internal IEnumerable<PartitonTree<PRED>> GetLeaves()
        {
            if (left == null && right == null)
                yield return this;
            else if (right == null)
                foreach (var leaf in left.GetLeaves())
                    yield return leaf;
            else if (left == null)
                foreach (var leaf in right.GetLeaves())
                    yield return leaf;
            else
            {
                foreach (var leaf in left.GetLeaves())
                    yield return leaf;
                foreach (var leaf in right.GetLeaves())
                    yield return leaf;
            }
        }

        public override string ToString()
        {
            IPrettyPrinter<PRED> pp = solver as IPrettyPrinter<PRED>;
            if (pp == null)
                return this.GetType().ToString();

            if (left == null && right == null)
            {
                return "Leaf(" + pp.PrettyPrint(phi) + ")";
            }
            else if (left == null)
            {
                return "Node(null," + right.ToString() + ")";
            }
            else if (right == null)
            {
                return "Node(" + left.ToString() + ",null)";
            }
            else
                return "Node(" + left.ToString() + "," + right.ToString() + ")";
        }
    }
}
