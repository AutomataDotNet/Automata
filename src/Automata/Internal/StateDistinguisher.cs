using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Pair = System.Tuple<int, int>;
using System.Collections;

namespace Microsoft.Automata
{
    /// <summary>
    /// Utility for distinguishing states of a deterministic symbolic automaton. Uses Moore's algorithm.
    /// </summary>
    public class StateDistinguisher_Moore<T>
    {

        Automaton<T> aut;
        IBooleanAlgebra<T> solver;
        Dictionary<Tuple<int, int>, ConsList<T>> distinguisher = new Dictionary<Tuple<int, int>, ConsList<T>>();
        Dictionary<Tuple<int, int>, string> distinguisherConcrete = new Dictionary<Tuple<int, int>, string>();

        //Dictionary<string, List<Pair>> distinguishingStringsMap = new Dictionary<string, List<Pair>>();
        List<string> distinguishingStringsSeq = new List<string>();

        Func<int, int, Pair> MkPair = (x, y) => (x <= y ? new Pair(x, y) : new Pair(y, x));

        Func<T, char> concretize;

        Dictionary<T, char> selectedCharacterMap = new Dictionary<T, char>();

        internal StateDistinguisher_Moore(Automaton<T> aut, Func<T, char> concretize = null)
        {
            this.aut = aut;
            this.solver = aut.Algebra;
            this.concretize = concretize;
            Initialize();
        }

        /// <summary>
        /// Based on MinimizeMoore method/technique.
        /// </summary>
        void Initialize()
        {
            var fa = aut;

            var currLayer = new SimpleStack<Tuple<int, int>>();
            var nextLayer = new SimpleStack<Tuple<int, int>>();

            //any pair of states (p,q) where one state is final and the other is not are distinguished by 
            //the empty list represented by null, this set of distinguishing sequences is trivially suffix-closed
            foreach (var p in fa.GetStates())
                if (!fa.IsFinalState(p))
                    foreach (var q in fa.GetFinalStates())
                    {
                        var pair = MkPair(p, q);
                        if (!distinguisher.ContainsKey(pair))
                        {
                            //the empty sequence distinguishes the states
                            distinguisherConcrete[pair] = "";
                            //List<Pair> pairs;
                            //if (!distinguishingStringsMap.TryGetValue("", out pairs))
                            //{
                            //    pairs = new List<Tuple<int, int>>();
                            //    distinguishingStringsMap[""] = pairs;
                            //    distinguishingStringsSeq.Add("");
                            //}
                            //pairs.Add(pair);
                            distinguisher[pair] = null;
                            nextLayer.Push(pair);
                        }
                    }

            //if a target pair of states is distinguished by some sequence
            //then any pair entering those states is also distinguished by extending that sequence
            //work breadth-first to maintain shortest witnesses
            while (nextLayer.IsNonempty)
            {
                var tmp = currLayer;
                currLayer = nextLayer;
                nextLayer = new SimpleStack<Tuple<int, int>>();
                while (currLayer.IsNonempty)
                {
                    var targetpair = currLayer.Pop();
                    foreach (var m1 in fa.GetMovesTo(targetpair.Item1))
                        foreach (var m2 in fa.GetMovesTo(targetpair.Item2))
                            if (m1.SourceState != m2.SourceState)
                            {
                                var psi = solver.MkAnd(m1.Label, m2.Label);
                                if (solver.IsSatisfiable(psi))
                                {
                                    var sourcepair = MkPair(m1.SourceState, m2.SourceState);
                                    if (!distinguisher.ContainsKey(sourcepair))
                                    {
                                        //add a new distinguishing sequence for the source pair
                                        //it extends a sequence of the target pair
                                        //thus the sequences remain suffix-closed
                                        if (concretize != null)
                                        {
                                            #region when concretization function is given precompute concrete distinguishers
                                            char c;
                                            if (!selectedCharacterMap.TryGetValue(psi, out c))
                                            {
                                                c = concretize(psi);
                                                selectedCharacterMap[psi] = c;
                                            }
                                            var s = c + distinguisherConcrete[targetpair];
                                            distinguisherConcrete[sourcepair] = s;

                                            //List<Pair> pairs;
                                            //if (!distinguishingStringsMap.TryGetValue(s, out pairs))
                                            //{
                                            //    pairs = new List<Tuple<int, int>>();
                                            //    distinguishingStringsMap[s] = pairs;
                                            //    distinguishingStringsSeq.Add(s);
                                            //}
                                            //pairs.Add(sourcepair);
                                            #endregion
                                        }

                                        var list = new ConsList<T>(psi, distinguisher[targetpair]);
                                        distinguisher[sourcepair] = list;

                                        nextLayer.Push(sourcepair);
                                    }
                                }
                            }
                }
            }
        }

        /// <summary>
        /// Output distinguishability witness for p and q if p and q are distinguishable.
        /// If return value is true and witness is null then the witness is the empty sequence.
        /// </summary>
        public bool AreDistinguishable(int p, int q, out ConsList<T> witness)
        {
            var pq = MkPair(p, q);
            return distinguisher.TryGetValue(pq, out witness);
        }

        /// <summary>
        /// Output concrete distinguishability witness for p and q if p and q are distinguishable.
        /// Not supported if selectCharacter function has not been provided.
        /// </summary>
        public bool AreDistinguishable(int p, int q, out string witness)
        {
            if (concretize == null)
                throw new AutomataException(AutomataExceptionKind.NotSupported);

            var pq = MkPair(p, q);
            return distinguisherConcrete.TryGetValue(pq, out witness);
        }

        /// <summary>
        /// Return the list of all all distinguishing strings.
        /// Not supported if selectCharacter function has not been provided.
        /// </summary>
        public List<string> GetAllDistinguishingStrings()
        {
            if (concretize == null)
                throw new AutomataException(AutomataExceptionKind.NotSupported);

            return distinguishingStringsSeq;
        }

        /// <summary>
        /// Returns true if s is a distinguishing string for some pair of states.
        /// </summary>
        public bool IsDistinguishingString(string s)
        {
            if (concretize == null)
                throw new AutomataException(AutomataExceptionKind.NotSupported);

            return distinguishingStringsSeq.Contains(s);
        }

        ///// <summary>
        ///// Returns true if s is a distinguishing string for some pair of states
        ///// and outputs the list of all such pairs.
        ///// </summary>
        //public bool TryGetDistinguishedPairsOfStates(string s, out List<Pair> statepairs)
        //{
        //    if (concretize == null)
        //        throw new AutomataException(AutomataExceptionKind.NotSupported);

        //    return distinguishingStringsMap.TryGetValue(s, out statepairs);
        //}

        int __DistinguisherCount = -1;
        /// <summary>
        /// Return the count of distinct distinguishing sequences
        /// </summary>
        public int DistinguisherCount
        {
            get
            {
                if (__DistinguisherCount == -1)
                {
                    if (concretize != null)
                        __DistinguisherCount = distinguishingStringsSeq.Count;
                    else
                        __DistinguisherCount = new HashSet<Sequence<T>>(EnumerateDistinguisherSequences()).Count;
                }
                return __DistinguisherCount;
            }
        }

        IEnumerable<Sequence<T>> EnumerateDistinguisherSequences()
        {
            foreach (var pair in distinguisher)
                yield return new Sequence<T>(pair.Value);
        }
    }

    /// <summary>
    /// Utility for distinguishing states of a deterministic symbolic automaton. 
    /// Uses symbolic partition refinement, based on symbolic minimization from POPL'14.
    /// </summary>
    public class StateDistinguisher<T>
    {
        IBooleanAlgebra<T> solver;
        Dictionary<Block, SplitNode> splithistory = new Dictionary<Block, SplitNode>();
        Block initialFinalBlock;
        SplitNode initialFinalNode;
        Block initialNonfinalBlock;
        SplitNode initialNonfinalNode;
        Automaton<T> autom;
        //bool sinkStateWasAdded;
        Block[] finalBlocks;
        Block[] nonfinalBlocks;
        Sequence<T>[] distinguishingSequences = null;
        bool preferLongest;

        Func<T, char> concretize;
        Dictionary<T, char> __concretize = new Dictionary<T, char>();
        Dictionary<Tuple<Block, Block>, string> __concreteDistinguishers = new Dictionary<Tuple<Block,Block>, string>();
        char Concretize(T pred)
        {
            char c;
            if (!__concretize.TryGetValue(pred, out c))
            {
                c = concretize(pred);
                __concretize[pred] = c;
            }
            return c;
        }

        /// <summary>
        /// Get the distinguishing string between the two states.
        /// Assumes that the choice function is defined.
        /// Returns null if the states are equivalent.
        /// </summary>
        public string GetDistinguishingString(int p, int q)
        {
            if ((autom.IsFinalState(p) && !autom.IsFinalState(q)) || (autom.IsFinalState(q) && !autom.IsFinalState(p)))
                return "";

            var A = Blocks[p];
            var B = Blocks[q];

            var AB = (A.Elem() < B.Elem() ? new Tuple<Block, Block>(A, B) : new Tuple<Block, Block>(B, A));

            string d;
            if (!__concreteDistinguishers.TryGetValue(AB, out d))
            {
                var seq = __GetDistinguishingSequence(A, B);
                d = new string(Array.ConvertAll(seq.ToArray(), Concretize));
                __concreteDistinguishers[AB] = d;
            }
            return d;
        }

        Dictionary<int, Block> Blocks = new Dictionary<int, Block>();

        /// <summary>
        /// Creates an instance of state distinguisher for the given dfa.
        /// </summary>
        /// <param name="dfa">deterministic automaton</param>
        /// <param name="choice">choice function for selecting characters from satisfiable predicates</param>
        /// <param name="optimize">if true then optimizes the computed distinguishing sequences</param>
        public StateDistinguisher(Automaton<T> dfa, Func<T,char> choice = null, bool optimize = true, bool preferLongest = false)
        {
            this.preferLongest = preferLongest;
            this.concretize = choice;
            solver = dfa.Algebra;
            this.autom = dfa.MakeTotal();
            //this.sinkStateWasAdded = (this.autom.StateCount > dfa.StateCount);
            ComputeSplitHistory();
            ComputeFinalAndNonfinalBlocks();
            if (optimize)
                PreCompute__GetDistinguishingSeq();
        }

        void ComputeFinalAndNonfinalBlocks()
        {
            List<Block> f_blocks = new List<Block>();
            List<Block> nf_blocks = new List<Block>();
            foreach (var block in splithistory.Keys)
            {
                if (autom.IsFinalState(block.Elem()))
                    f_blocks.Add(block);
                else
                    nf_blocks.Add(block);
            }
            finalBlocks = f_blocks.ToArray();
            nonfinalBlocks = nf_blocks.ToArray();
        }

        /// <summary>
        /// Based on algorithm MinSFA from POPL14.
        /// </summary>
        void ComputeSplitHistory()
        {
            var fa = autom;
            if (!fa.isDeterministic)
            {
                throw new AutomataException(AutomataExceptionKind.AutomatonIsNondeterministic);
            }

            this.initialFinalBlock = new Block(fa.GetFinalStates());
            this.initialNonfinalBlock = new Block(fa.GetNonFinalStates());
            this.initialFinalNode = new SplitNode();
            this.initialNonfinalNode = new SplitNode();
            splithistory[initialFinalBlock] = this.initialFinalNode;
            splithistory[initialNonfinalBlock] = this.initialNonfinalNode;

            foreach (var q in fa.GetFinalStates()) Blocks[q] = initialFinalBlock;
            foreach (var q in fa.GetNonFinalStates()) Blocks[q] = initialNonfinalBlock;

            var W = new BlockStack();
            var W1 = new BlockStack();

            if (initialNonfinalBlock.Count < initialFinalBlock.Count)
                W1.Push(initialNonfinalBlock);
            else
                W1.Push(initialFinalBlock);

            Func<T, T, T> MkDiff = (x, y) => solver.MkAnd(x, solver.MkNot(y));

            //use breath first search wrt new elements

            while (!W1.IsEmpty)
            {
                var tmp = W;
                W = W1;
                W1 = tmp;

                while (!W.IsEmpty)
                {
                    var B = W.Pop();

                    var Bcopy = new List<int>(B);             //make a copy of B for iterating over its elemenents
                    var Gamma = new Dictionary<int, T>();     //joined conditions leading to B from states leading to B
                    foreach (var q in Bcopy)
                    {
                        foreach (var move in fa.GetMovesTo(q)) //moves leading to q
                            if (Blocks[move.SourceState].Count > 1) //singleton blocks cannot be further split
                                if (Gamma.ContainsKey(move.SourceState))
                                    Gamma[move.SourceState] = solver.MkOr(Gamma[move.SourceState], move.Label);
                                else
                                    Gamma[move.SourceState] = move.Label;
                    }

                    //if x is not in Gamma.Keys then return False else return Gamma[q]
                    //this way initial (_,B)-splitting is not required
                    Func<int, T> GAMMA = (x) =>
                    {
                        T pred;
                        if (Gamma.TryGetValue(x, out pred))
                            return pred;
                        else
                            return solver.False;
                    };

                    var relevant2 = new HashSet<Block>();
                    foreach (var q in Gamma.Keys)
                        if (Blocks[q].Count > 1)
                            relevant2.Add(Blocks[q]); //collect the relevant blocks
                    var relevantList = new List<Block>(relevant2);

                    //only relevant blocks are potentially split               
                    while (relevantList.Count > 0)
                    {
                        var P = relevantList[0];
                        relevantList.RemoveAt(0);

                        var PE = P.GetEnumerator();
                        PE.MoveNext();

                        var P1 = new Block();
                        bool splitFound = false;

                        //psi may be false here
                        T psi = GAMMA(PE.Current);
                        P1.Add(PE.Current); //note that PE has at least 2 elements

                        #region compute P1 as the new sub-block of P
                        while (PE.MoveNext())
                        {
                            var q = PE.Current;
                            var phi = GAMMA(q);

                            if (splitFound)
                            {
                                var psi_and_phi = solver.MkAnd(psi, phi);
                                if (solver.IsSatisfiable(psi_and_phi))
                                {
                                    psi = psi_and_phi;
                                    P1.Add(q);
                                }
                            }
                            else
                            {
                                var psi_min_phi = MkDiff(psi, phi);
                                if (solver.IsSatisfiable(psi_min_phi))
                                {
                                    psi = psi_min_phi;
                                    splitFound = true;
                                }
                                else // [[psi]] is subset of [[phi]]
                                {
                                    var phi_min_psi = MkDiff(phi, psi);
                                    if (!solver.IsSatisfiable(phi_min_psi))
                                        // [[phi]] is subset of [[psi]]
                                        P1.Add(q); //psi and phi are equivalent
                                    else
                                    {
                                        //there is some a: q --a--> B and p --a--> compl(B)
                                        P1.Clear();
                                        P1.Add(q);
                                        psi = phi_min_psi;
                                        splitFound = true;
                                    }
                                }
                            }
                        }
                        #endregion

                        #region split P
                        //if P1.Count == P.Count then nothing was split
                        if (P1.Count < P.Count)
                        {
                            var node = splithistory[P];
                            node.Split(psi);
                            //which one is left or right does not matter
                            splithistory[P] = node.left;
                            splithistory[P1] = node.right;

                            foreach (var p in P1)
                            {
                                P.Remove(p);
                                Blocks[p] = P1;
                            }

                            if (W.Contains(P) || W1.Contains(P))
                                W1.Push(P1);
                            else if (P.Count <= P1.Count)
                                W1.Push(P);
                            else
                                W1.Push(P1);

                            if (P.Count > 1)
                                relevantList.Add(P);
                            if (P1.Count > 1)
                                relevantList.Add(P1);
                        }
                        #endregion
                    }
                }
            }
        }

        Dictionary<Tuple<Block, Block>, Sequence<T>> __GetDistinguishingSeq = new Dictionary<Tuple<Block, Block>, Sequence<T>>();

        /// <summary>
        /// Get the distinguishing string between the two states.
        /// Returns null if the states are equivalent.
        /// </summary>
        public Sequence<T> GetDistinguishingSequence(int p, int q)
        {
            if ((autom.IsFinalState(p) && !autom.IsFinalState(q)) || (autom.IsFinalState(q) && !autom.IsFinalState(p)))
                return Sequence<T>.Empty;

            var A = Blocks[p];
            var B = Blocks[q];
            return __GetDistinguishingSequence(A, B);
        }

        /// <summary>
        /// Enumerate all the distinguishing sequences between all pairs of nonequivalent states.
        /// Used for testing purposes.
        /// </summary>
        public IEnumerable<Tuple<Tuple<int,int>,List<ConsList<T>>>> EnumerateAllDistinguishingSequences()
        {
            for (int i = 0; i < finalBlocks.Length - 1; i++)
            {
                var A = finalBlocks[i];
                for (int j = i + 1; j < finalBlocks.Length; j++)
                {
                    var B = finalBlocks[j];
                    yield return new Tuple<Tuple<int, int>, List<ConsList<T>>>(new Tuple<int, int>(A.Elem(), B.Elem()),
                        new List<ConsList<T>>(EnumerateAllDistinguishingSequences(A.Elem(), B.Elem())));
                }
            }
            for (int i = 0; i < nonfinalBlocks.Length - 1; i++)
            {
                var A = nonfinalBlocks[i];
                for (int j = i + 1; j < nonfinalBlocks.Length; j++)
                {
                    var B = nonfinalBlocks[j];
                    yield return new Tuple<Tuple<int, int>, List<ConsList<T>>>(new Tuple<int, int>(A.Elem(), B.Elem()),
                        new List<ConsList<T>>(EnumerateAllDistinguishingSequences(A.Elem(), B.Elem())));
                }
            }
        }


        /// <summary>
        /// Enumerate all the distinguishing sequences between the two states.
        /// Returns null if the states are equivalent.
        /// </summary>
        public IEnumerable<ConsList<T>> EnumerateAllDistinguishingSequences(int p, int q)
        {
            var A = Blocks[p];
            var B = Blocks[q];
            if (A != B)
                foreach (var d in __EnumerateAllDistinguishingSequences(A, B))
                    yield return d;
        }

        IEnumerable<ConsList<T>> __EnumerateAllDistinguishingSequences(Block A, Block B)
        {
            if (A == B)
                throw new AutomataException(AutomataExceptionKind.InternalError_DistinguishingSequenceGeneration);
            else
            {
                var Anode = splithistory[A];
                var Bnode = splithistory[B];
                if ((Anode.root == initialFinalNode && Bnode.root == initialNonfinalNode) ||
                    (Bnode.root == initialFinalNode && Anode.root == initialNonfinalNode))
                    yield return null;
                else
                {
                    var ancestor = Anode.FindCommonAncestorWith(Bnode);
                    foreach (var d in __EnumAllDistingusihingSequences(A, B, ancestor.splitter))
                        yield return d;
                }
            }
        }

        IEnumerable<ConsList<T>> __EnumAllDistingusihingSequences(Block A, Block B, T cond)
        {
            foreach (var pmove in autom.GetMovesFrom(A.Elem()))
                foreach (var qmove in autom.GetMovesFrom(B.Elem()))
                {
                    var psi = solver.MkAnd(cond, pmove.Label, qmove.Label);
                    if (solver.IsSatisfiable(psi))
                    {
                        foreach (var d in __EnumerateAllDistinguishingSequences(Blocks[pmove.TargetState], Blocks[qmove.TargetState]))
                            yield return new ConsList<T>(psi, d);
                    }
                }
        }

        Sequence<T> __GetDistinguishingSequence(Block A, Block B)
        {
            if (A == B)
                return null;

            var a = A.Elem();
            var b = B.Elem();

            var AB = (a < b ? new Tuple<Block, Block>(A, B) : new Tuple<Block, Block>(B, A));

            Sequence<T> d;
            if (!__GetDistinguishingSeq.TryGetValue(AB, out d))
            {
                var Anode = splithistory[A];
                var Bnode = splithistory[B];

                var ancestor = Anode.FindCommonAncestorWith(Bnode);

                var pred = ancestor.splitter;
                d = __GetDistingusihingSequence(a, b, pred);
                if (d == null)
                    throw new AutomataException(AutomataExceptionKind.InternalError_DistinguishingSequenceGeneration);
                __GetDistinguishingSeq[AB] = d;
            }
            return d;
        }

        Sequence<T> __GetDistingusihingSequence(int p, int q, T cond)
        {
            var candidates = new Dictionary<Sequence<T>, T>();
            foreach (var pmove in autom.GetMovesFrom(p))
                foreach (var qmove in autom.GetMovesFrom(q))
                {
                    var psi = solver.MkAnd(cond, pmove.Label, qmove.Label);
                    if (solver.IsSatisfiable(psi))
                    {
                        var v = GetDistinguishingSequence(pmove.TargetState, qmove.TargetState);
                        if (v == null)
                            throw new AutomataException(AutomataExceptionKind.InternalError_DistinguishingSequenceGeneration);
                        T phi;
                        if (candidates.TryGetValue(v, out phi))
                            candidates[v] = solver.MkOr(psi, phi);
                        else
                            candidates[v] = psi;
                    }
                }
            Sequence<T> rest;
            if (preferLongest)
                rest = ChooseLongestKey(candidates);
            else
                rest = ChooseShortestKey(candidates);
            T first = candidates[rest];
            var res = new Sequence<T>(first).Append(rest);
            return res;
        }

        private Sequence<T> ChooseLongestKey(Dictionary<Sequence<T>, T> candidates)
        {
            Sequence<T> key = null;
            foreach (var k in candidates.Keys)
                key = (key == null ? k : (k.Length > key.Length ? k : key));
            return key;
        }

        private Sequence<T> ChooseShortestKey(Dictionary<Sequence<T>, T> candidates)
        {
            Sequence<T> key = null;
            foreach (var k in candidates.Keys)
                key = (key == null ? k : (k.Length < key.Length ? k : key));
            return key;
        }

        /// <summary>
        /// Returns all the distinguishing sequences.
        /// </summary>
        public Sequence<T>[] GetAllDistinguishingSequences()
        {
            if (distinguishingSequences == null)
            {
                HashSet<Sequence<T>> dseqs_set = new HashSet<Sequence<T>>();
                List<Sequence<T>> dseqs = new List<Sequence<T>>();
                if (initialNonfinalBlock.IsEmpty || initialFinalBlock.IsEmpty)
                    //degenerate case, there are no distinguishing strings
                    distinguishingSequences = new Sequence<T>[] { };
                else
                {
                    dseqs_set.Add(Sequence<T>.Empty);
                    dseqs.Add(Sequence<T>.Empty);

                    for (int i = 0; i < finalBlocks.Length - 1; i++)
                    {
                        var p = finalBlocks[i].Elem();
                        for (int j = i + 1; j < finalBlocks.Length; j++)
                        {
                            var q = finalBlocks[j].Elem();
                            var d = GetDistinguishingSequence(p, q);
                            if (dseqs_set.Add(d))
                                dseqs.Add(d);
                        }
                    }

                    for (int i = 0; i < nonfinalBlocks.Length - 1; i++)
                    {
                        var p = nonfinalBlocks[i].Elem();
                        for (int j = i + 1; j < nonfinalBlocks.Length; j++)
                        {
                            var q = nonfinalBlocks[j].Elem();
                            var d = GetDistinguishingSequence(p, q);
                            if (dseqs_set.Add(d))
                                dseqs.Add(d);
                        }
                    }

                    distinguishingSequences = dseqs.ToArray();
                }
            }
            return distinguishingSequences;
        }

        /// <summary>
        /// Returns all the distinguishing strings. Assumes choice function is defined.
        /// </summary>
        public string[] GetAllDistinguishingStrings()
        {
            var seqs = GetAllDistinguishingSequences();
            string[] strs = Array.ConvertAll(seqs, s => new string(Array.ConvertAll(s.ToArray(), Concretize)));
            return strs;
        }

        void PreCompute__GetDistinguishingSeq()
        {
            var distinguisher = new Dictionary<Tuple<Block, Block>, Sequence<T>>();
            __GetDistinguishingSeq = new Dictionary<Tuple<Block, Block>, Sequence<T>>();

            HashSet<Sequence<T>> dseqs_set = new HashSet<Sequence<T>>();
            List<Sequence<T>> dseqs = new List<Sequence<T>>();
            if (initialNonfinalBlock.IsEmpty || initialFinalBlock.IsEmpty)
            {
                //degenerate case, there are no distinguishing strings
                distinguishingSequences = new Sequence<T>[] { };
            }
            else
            {
                var overlaps = new Dictionary<Sequence<T>, OverlapWitness>();

                int maxDistinguisherLength = 0;
                dseqs_set.Add(Sequence<T>.Empty);
                dseqs.Add(Sequence<T>.Empty);

                for (int i = 0; i < finalBlocks.Length - 1; i++)
                {
                    var p = finalBlocks[i].Elem();
                    for (int j = i + 1; j < finalBlocks.Length; j++)
                    {
                        var q = finalBlocks[j].Elem();
                        var pq = (p < q ? new Tuple<Block, Block>(finalBlocks[i], finalBlocks[j]) : new Tuple<Block, Block>(finalBlocks[j], finalBlocks[i]));
                        var d = GetDistinguishingSequence(p, q);
                        distinguisher[pq] = d;
                        if (dseqs_set.Add(d))
                        {
                            if (d.Length > maxDistinguisherLength)
                                maxDistinguisherLength = d.Length;
                            dseqs.Add(d);
                            overlaps[d] = null;
                        }
                    }
                }

                for (int i = 0; i < nonfinalBlocks.Length - 1; i++)
                {
                    var p = nonfinalBlocks[i].Elem();
                    for (int j = i + 1; j < nonfinalBlocks.Length; j++)
                    {
                        var q = nonfinalBlocks[j].Elem();
                        var pq = (p < q ? new Tuple<Block, Block>(nonfinalBlocks[i], nonfinalBlocks[j]) : new Tuple<Block, Block>(nonfinalBlocks[j], nonfinalBlocks[i]));
                        var d = GetDistinguishingSequence(p, q);
                        distinguisher[pq] = d;
                        if (dseqs_set.Add(d))
                        {
                            if (d.Length > maxDistinguisherLength)
                                maxDistinguisherLength = d.Length;
                            dseqs.Add(d);
                            overlaps[d] = null;
                        }
                    }
                }
                //create a common distinguishing sequence for overlapping cases
                List<Sequence<T>>[] distinguishersOrderedByLength = new List<Sequence<T>>[maxDistinguisherLength];
                for (int i = 0; i < maxDistinguisherLength; i++)
                    distinguishersOrderedByLength[i] = new List<Sequence<T>>();
                for (int i = 0; i < dseqs.Count; i++)
                    if (dseqs[i].Length > 0)
                        distinguishersOrderedByLength[dseqs[i].Length - 1].Add(dseqs[i]);

                //create initial overlaps
                overlaps[Sequence<T>.Empty] = null;
                for (int i = 0; i < maxDistinguisherLength; i++)
                {
                    var di = distinguishersOrderedByLength[i];
                    for (int j = 0; j < di.Count; j++)
                    {
                        overlaps[di[j]] = new OverlapWitness(di[j].First, overlaps[di[j].Rest()]);
                    }
                }

                //normalize the distinguishing sequences in layers
                //assuming in each layer that the previous layer has been normalized
                for (int i = 0; i < maxDistinguisherLength; i++)
                {
                    //nromalize layer i, i.e., sequences on length i+1
                    var thislayer = distinguishersOrderedByLength[i];
                    for (int j = 0; j < thislayer.Count - 1; j++)
                    {
                        var t = thislayer[j];
                        var t_first = t.First;
                        var t_rest_overlap = overlaps[t.Rest()];

                        for (int k = j + 1; k < thislayer.Count; k++)
                        {
                            var s = thislayer[k];
                            var s_First = s.First;
                            var s_rest_overlap = overlaps[s.Rest()];

                            if (s_rest_overlap == t_rest_overlap || 
                                (s_rest_overlap != null && t_rest_overlap != null && s_rest_overlap.Deref == t_rest_overlap.Deref))
                            {
                                //check if the overlap can be extended to the whole sequence
                                var psi = solver.MkAnd(overlaps[s].GetFirst(), overlaps[t].GetFirst());
                                if (solver.IsSatisfiable(psi))
                                {
                                    //join the overlaps
                                    var ow = new OverlapWitness(psi, s_rest_overlap);
                                    overlaps[s].ReplaceWith(ow);
                                    overlaps[t].ReplaceWith(ow);
                                }
                            }
                        }
                    }
                }

                //recompute distinguishing sequences based on the normalized sequences

                dseqs_set.Clear();
                dseqs.Clear();
                dseqs.Add(Sequence<T>.Empty);

                foreach (var entry in distinguisher)
                {
                    var normalized_seq = new Sequence<T>(overlaps[entry.Value].ToArray());
                    if (dseqs_set.Add(normalized_seq))
                        dseqs.Add(normalized_seq);
                    __GetDistinguishingSeq[entry.Key] = normalized_seq;
                }

                distinguishingSequences = dseqs.ToArray();
            }
        }

        internal class SplitNode
        {
            internal T splitter;
            internal string position;

            internal SplitNode root;
            internal SplitNode left;
            internal SplitNode right;

            internal SplitNode()
            {
                this.splitter = default(T);
                this.root = this;
                this.position = "";
                this.left = null;
                this.right = null;
            }

            internal SplitNode(SplitNode parent, char pos)
            {
                this.splitter = default(T);
                this.root = parent.root;
                this.position = parent.position + pos;
                this.left = null;
                this.right = null;
            }

            internal SplitNode FindCommonAncestorWith(SplitNode other)
            {
                return root.FindNode(this.position, other.position, 0);
            }

            private SplitNode FindNode(string position1, string position2, int i)
            {
                if (i < position1.Length && i < position2.Length && position1[i] == position2[i])
                {
                    if (position1[i] == 'l')
                        return this.left.FindNode(position1, position2, i + 1);
                    else
                        return this.right.FindNode(position1, position2, i + 1);
                }
                else
                    return this;
            }

            internal void Split(T splitter)
            {
                this.splitter = splitter;
                this.left = new SplitNode(this, 'l');
                this.right = new SplitNode(this, 'r');
            }
        }

        internal class OverlapWitness
        {
            OverlapWitness replacement = null;
            T first;
            OverlapWitness rest;

            internal OverlapWitness Deref
            {
                get
                {
                    OverlapWitness deref = this;
                    while (deref.replacement != null)
                        deref = deref.replacement;
                    return deref;
                }
            }

            internal OverlapWitness(T first, OverlapWitness rest)
            {
                this.first = first;
                this.rest = rest;
            }

            internal void ReplaceWith(OverlapWitness other)
            {
                this.replacement = other;
            }

            internal T GetFirst()
            {
                if (replacement == null)
                    return first;
                else
                    return replacement.GetFirst();
            }

            internal OverlapWitness GetRest()
            {
                if (replacement == null)
                    return rest;
                else
                    return replacement.GetRest();
            }

            internal T[] ToArray()
            {
                List<T> elems = new List<T>();
                OverlapWitness ow = this;
                while (ow != null)
                {
                    while (ow.replacement != null)
                    {
                        ow = ow.replacement;
                    }
                    elems.Add(ow.first);
                    ow = ow.rest;
                }
                return elems.ToArray();
            }
        }
    }
}
