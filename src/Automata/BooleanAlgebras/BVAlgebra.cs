using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.Automata
{
    public abstract class BVAlgebraBase
    {
        internal DecisionTree dtree;
        internal IntervalSet[] partition;
        internal int nrOfBits;

        internal BVAlgebraBase(DecisionTree dtree, IntervalSet[] partition, int nrOfBits)
        {
            this.dtree = dtree;
            this.partition = partition;
            this.nrOfBits = nrOfBits;
        }

        protected string SerializePartition()
        {
            string s = "";
            for (int i = 0; i < partition.Length; i++)
            {
                if (i > 0)
                    s += ";";
                s += partition[i].Serialize();
            }
            return s;
        }

        protected static IntervalSet[] DeserializePartition(string s)
        {
            var blocks = s.Split(';');
            var intervalSets = Array.ConvertAll(blocks, IntervalSet.Parse);
            return intervalSets;
        }
    }
    /// <summary>
    /// Bit vector algebra
    /// </summary>
    [Serializable]
    public class BVAlgebra : BVAlgebraBase, ICharAlgebra<BV>, ISerializable
    {
        [NonSerialized]
        MintermGenerator<BV> mtg;
        [NonSerialized]
        Chooser chooser = new Chooser();
        [NonSerialized]
        BV zero;
        [NonSerialized]
        BV ones;
        [NonSerialized]
        ulong[] all0;
        [NonSerialized]
        ulong[] all1;
        [NonSerialized]
        internal BV[] atoms;

        public ulong ComputeDomainSize(BV set)
        {
            int size = 0;
            for (int i = 0; i < atoms.Length; i++)
            {
                if (IsSatisfiable(set & atoms[i]))
                    size += partition[i].Count;
            }
            return (ulong)size;
        }

        public BV MapPredToBV(BDD pred, BDD[] minterms)
        {
            if (pred == null)
                return null;
            var alg = pred.algebra;
            BV bv;

            bv = zero;
            for (int i = 0; i < minterms.Length; i++)
                if (alg.IsSatisfiable(alg.MkAnd(pred, minterms[i])))
                    bv = bv | MkBV(i);

            return bv;
        }

        public static BVAlgebra Create(CharSetSolver solver, BDD[] minterms)
        {
            var dtree = DecisionTree.Create(solver, minterms);
            var partitionBase = Array.ConvertAll(minterms, m => m.ToRanges());
            var partition = Array.ConvertAll(partitionBase, p => new IntervalSet(p));
            return new BVAlgebra(dtree, partition);
        }

        private BVAlgebra(DecisionTree dtree, IntervalSet[] partition) : base(dtree, partition, partition.Length)
        {
            var K = (nrOfBits - 1) / 64;
            int last = nrOfBits % 64;
            ulong lastMask = (last == 0 ? ulong.MaxValue : (((ulong)1 << last) - 1));
            all0 = new ulong[K];
            all1 = new ulong[K];
            for (int i = 0; i < K; i++)
            {
                all0[0] = 0;
                if (i < K - 1)
                {
                    all1[i] = ulong.MaxValue;
                }
                else
                {
                    all1[i] = lastMask;
                }
            }
            this.zero = new BV(0, all0);
            this.ones = new BV((K == 0 ? lastMask : ulong.MaxValue), all1);
            this.mtg = new MintermGenerator<BV>(this);
            this.atoms = new BV[nrOfBits];
            for (int i = 0; i < nrOfBits; i++)
            {
                atoms[i] = MkBV(i);
            }
        }

        public BV False
        {
            get
            {
                return zero;
            }
        }

        public bool IsAtomic
        {
            get
            {
                return true;
            }
        }

        public bool IsExtensional
        {
            get
            {
                return true;
            }
        }

        public BV True
        {
            get
            {
                return ones;
            }
        }

        public BitWidth Encoding
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public CharSetSolver CharSetProvider
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public bool AreEquivalent(BV predicate1, BV predicate2)
        {
            return predicate1.Equals(predicate2);
        }

        public bool CheckImplication(BV lhs, BV rhs)
        {
            return ((MkNot(lhs)) | rhs).Equals(this.ones);
        }

        public bool EvaluateAtom(BV atom, BV psi)
        {
            return (atom & psi).Equals(atom);
        }

        public IEnumerable<Tuple<bool[], BV>> GenerateMinterms(params BV[] constraints)
        {
            return this.mtg.GenerateMinterms(constraints);
        }

        public BV GetAtom(BV psi)
        {
            if (psi.first != 0)
            {
                var atomId = GetAtomId(psi.first);
                return this.atoms[atomId];
            }
            for (int i=0; i < psi.more.Length; i++)
            {
                if (psi.more[i] != 0)
                {
                    int id = GetAtomId(psi.more[i]);
                    return this.atoms[(i+1)*64 + id];
                }
            }
            return zero;
        }

        static int GetAtomId(ulong psi)
        {
            if ((psi & 0xFFFFFFFF) != 0)
            {
                #region get atom from first word
                if ((psi & 0xFFFF) != 0)
                {
                    if ((psi & 0xFF) != 0)
                    {
                        #region first byte
                        if ((psi & 0xF) != 0)
                        {
                            #region first nibble of first byte
                            if ((psi & 3) != 0)
                            {
                                if ((psi & 1) != 0)
                                    return 0;
                                else
                                    return 1;
                            }
                            else
                            {
                                if ((psi & 4) != 0)
                                    return 2;
                                else
                                    return 3;
                            }
                            #endregion
                        }
                        else
                        {
                            #region second nibble of first byte
                            if ((psi & 0x30) != 0)
                            {
                                if ((psi & 0x10) != 0)
                                    return 4;
                                else
                                    return 5;
                            }
                            else
                            {
                                if ((psi & 0x40) != 0)
                                    return 6;
                                else
                                    return 7;
                            }
                            #endregion
                        }
                        #endregion
                    }
                    else
                    {
                        #region second byte
                        if ((psi & 0xF00) != 0)
                        {
                            #region first nibble of second byte
                            if ((psi & 0x300) != 0)
                            {
                                if ((psi & 0x100) != 0)
                                    return 8;
                                else
                                    return 9;
                            }
                            else
                            {
                                if ((psi & 0x400) != 0)
                                    return 10;
                                else
                                    return 11;
                            }
                            #endregion
                        }
                        else
                        {
                            #region second nibble of second byte
                            if ((psi & 0x3000) != 0)
                            {
                                if ((psi & 0x1000) != 0)
                                    return 12;
                                else
                                    return 13;
                            }
                            else
                            {
                                if ((psi & 0x4000) != 0)
                                    return 14;
                                else
                                    return 15;
                            }
                            #endregion
                        }
                        #endregion
                    }
                }
                else
                {
                    if ((psi & 0xFF0000) != 0)
                    {
                        #region first byte
                        if ((psi & 0xF0000) != 0)
                        {
                            #region first nibble of first byte
                            if ((psi & 0x30000) != 0)
                            {
                                if ((psi & 0x10000) != 0)
                                    return 16;
                                else
                                    return 17;
                            }
                            else
                            {
                                if ((psi & 0x40000) != 0)
                                    return 18;
                                else
                                    return 19;
                            }
                            #endregion
                        }
                        else
                        {
                            #region second nibble of first byte
                            if ((psi & 0x300000) != 0)
                            {
                                if ((psi & 0x100000) != 0)
                                    return 20;
                                else
                                    return 21;
                            }
                            else
                            {
                                if ((psi & 0x400000) != 0)
                                    return 22;
                                else
                                    return 23;
                            }
                            #endregion
                        }
                        #endregion
                    }
                    else
                    {
                        #region second byte
                        if ((psi & 0xF000000) != 0)
                        {
                            #region first nibble of second byte
                            if ((psi & 0x3000000) != 0)
                            {
                                if ((psi & 0x1000000) != 0)
                                    return 24;
                                else
                                    return 25;
                            }
                            else
                            {
                                if ((psi & 0x4000000) != 0)
                                    return 26;
                                else
                                    return 27;
                            }
                            #endregion
                        }
                        else
                        {
                            #region second nibble of second byte
                            if ((psi & 0x30000000) != 0)
                            {
                                if ((psi & 0x10000000) != 0)
                                    return 28;
                                else
                                    return 29;
                            }
                            else
                            {
                                if ((psi & 0x40000000) != 0)
                                    return 30;
                                else
                                    return 31;
                            }
                            #endregion
                        }
                        #endregion
                    }
                }
                #endregion
            }
            else
            {
                #region get atom from second word
                if ((psi & 0xFFFF00000000) != 0)
                {
                    if ((psi & 0xFF00000000) != 0)
                    {
                        #region first byte
                        if ((psi & 0xF00000000) != 0)
                        {
                            #region first nibble of first byte
                            if ((psi & 0x300000000) != 0)
                            {
                                if ((psi & 0x100000000) != 0)
                                    return 32;
                                else
                                    return 33;
                            }
                            else
                            {
                                if ((psi & 0x400000000) != 0)
                                    return 34;
                                else
                                    return 35;
                            }
                            #endregion
                        }
                        else
                        {
                            #region second nibble of first byte
                            if ((psi & 0x3000000000) != 0)
                            {
                                if ((psi & 0x1000000000) != 0)
                                    return 36;
                                else
                                    return 37;
                            }
                            else
                            {
                                if ((psi & 0x4000000000) != 0)
                                    return 38;
                                else
                                    return 39;
                            }
                            #endregion
                        }
                        #endregion
                    }
                    else
                    {
                        #region second byte
                        if ((psi & 0xF0000000000) != 0)
                        {
                            #region first nibble of second byte
                            if ((psi & 0x30000000000) != 0)
                            {
                                if ((psi & 0x10000000000) != 0)
                                    return 40;
                                else
                                    return 41;
                            }
                            else
                            {
                                if ((psi & 0x40000000000) != 0)
                                    return 42;
                                else
                                    return 43;
                            }
                            #endregion
                        }
                        else
                        {
                            #region second nibble of second byte
                            if ((psi & 0x300000000000) != 0)
                            {
                                if ((psi & 0x100000000000) != 0)
                                    return 44;
                                else
                                    return 45;
                            }
                            else
                            {
                                if ((psi & 0x400000000000) != 0)
                                    return 46;
                                else
                                    return 47;
                            }
                            #endregion
                        }
                        #endregion
                    }
                }
                else
                {
                    if ((psi & 0xFF000000000000) != 0)
                    {
                        #region first byte
                        if ((psi & 0xF000000000000) != 0)
                        {
                            #region first nibble of first byte
                            if ((psi & 0x3000000000000) != 0)
                            {
                                if ((psi & 0x1000000000000) != 0)
                                    return 48;
                                else
                                    return 49;
                            }
                            else
                            {
                                if ((psi & 0x4000000000000) != 0)
                                    return 50;
                                else
                                    return 51;
                            }
                            #endregion
                        }
                        else
                        {
                            #region second nibble of first byte
                            if ((psi & 0x30000000000000) != 0)
                            {
                                if ((psi & 0x10000000000000) != 0)
                                    return 52;
                                else
                                    return 53;
                            }
                            else
                            {
                                if ((psi & 0x40000000000000) != 0)
                                    return 54;
                                else
                                    return 55;
                            }
                            #endregion
                        }
                        #endregion
                    }
                    else
                    {
                        #region second byte
                        if ((psi & 0xF00000000000000) != 0)
                        {
                            #region first nibble of second byte
                            if ((psi & 0x300000000000000) != 0)
                            {
                                if ((psi & 0x100000000000000) != 0)
                                    return 56;
                                else
                                    return 57;
                            }
                            else
                            {
                                if ((psi & 0x400000000000000) != 0)
                                    return 58;
                                else
                                    return 59;
                            }
                            #endregion
                        }
                        else
                        {
                            #region second nibble of second byte
                            if ((psi & 0x3000000000000000) != 0)
                            {
                                if ((psi & 0x1000000000000000) != 0)
                                    return 60;
                                else
                                    return 61;
                            }
                            else
                            {
                                if ((psi & 0x4000000000000000) != 0)
                                    return 62;
                                else
                                    return 63;
                            }
                            #endregion
                        }
                        #endregion
                    }
                }
                #endregion
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSatisfiable(BV predicate)
        {
            return !predicate.Equals(zero);
        }

        public BV MkAnd(params BV[] predicates)
        {
            var and = ones;
            for (int i = 0; i < predicates.Length; i++)
            {
                and = and & predicates[i];
                if (and.Equals(zero))
                    return zero;
            }
            return and;
        }

        public BV MkAnd(IEnumerable<BV> predicates)
        {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BV MkAnd(BV predicate1, BV predicate2)
        {
            return predicate1 & predicate2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BV MkDiff(BV predicate1, BV predicate2)
        {
            return predicate1 & ~predicate2; 
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BV MkNot(BV predicate)
        {
            return ones & ~predicate;
        }

        public BV MkOr(IEnumerable<BV> predicates)
        {
            var res = zero;
            foreach (var p in predicates)
            {
                res = res | p;
                if (res.Equals(ones))
                    return ones;
            }
            return res;
        }

        public BV MkOr(BV predicate1, BV predicate2)
        {
            return predicate1 | predicate2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BV MkSymmetricDifference(BV p1, BV p2)
        {
            return p1 ^ p2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BV Simplify(BV predicate)
        {
            return predicate;
        }

        public BV MkBV(params int[] truebits)
        {
            ulong first = 0;
            var more = new ulong[this.all0.Length];
            for (int i = 0; i < truebits.Length; i++)
            {
                int b = truebits[i];
                if (b >= nrOfBits || b < 0)
                    throw new AutomataException(AutomataExceptionKind.BitOutOfRange);
                int k = b / 64;
                int j = b % 64;
                if (k == 0)
                    first = first | ((ulong)1 << j);
                else
                    more[k-1] = more[k-1] | ((ulong)1 << j);
            }
            var bv = new BV(first, more);
            return bv;
        }

        public BV MkRangeConstraint(char lower, char upper, bool caseInsensitive = false)
        {
            throw new NotSupportedException();
        }

        public BV MkCharConstraint(char c, bool caseInsensitive = false)
        {
            if (caseInsensitive == true)
                throw new AutomataException(AutomataExceptionKind.NotSupported);

            int i = this.dtree.GetId(c);
            return this.atoms[i];
        }

        public int GetIdOfChar(char c)
        {
            return this.dtree.GetId(c);
        }

        public BV MkRangesConstraint(bool caseInsensitive, IEnumerable<char[]> ranges)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Assumes that set is a union of some minterms (or empty).
        /// If null then null is returned.
        /// </summary>
        public BV ConvertFromCharSet(BDD set)
        {
            if (set == null)
                return null;
            var alg = set.algebra;
            BV res = this.zero;
            for (int i = 0; i < partition.Length; i++)
            {
                BDD bdd_i = partition[i].AsBDD(alg);
                var conj = alg.MkAnd(bdd_i, set);
                if (alg.IsSatisfiable(conj))
                {
                    res = res | atoms[i];
                }
            }
            return res;
        }

        public bool TryConvertToCharSet(IBDDAlgebra solver, BV pred, out BDD set)
        {
            BDD res = solver.False;
            if (!pred.Equals(this.zero))
            {
                for (int i = 0; i < atoms.Length; i++)
                {
                    //construct the union of the corresponding atoms
                    if (!(pred & atoms[i]).Equals(this.zero))
                    {
                        BDD bdd_i = partition[i].AsBDD(solver);
                        res = solver.MkOr(res, bdd_i);
                    }
                }
            }
            set = res;
            return true;
        }

        /// <summary>
        /// Pretty print the bitvector predicate as a character class.
        /// </summary>
        /// <param name="bv">given bitvector predicate</param>
        public string PrettyPrint(BV bv)
        {
            var lab1 = PrettyPrintHelper(bv, false);
            var lab2 = PrettyPrintHelper(MkNot(bv), true);
            if (lab1.Length <= lab2.Length)
                return lab1;
            else
                return lab2;

        }

        string PrettyPrintHelper(BV bv, bool complement)
        {
            List<IntervalSet> sets = new List<IntervalSet>();
            for (int i = 0; i < atoms.Length; i++)
                if (IsSatisfiable(bv & atoms[i]))
                    sets.Add(partition[i]);
            var set = IntervalSet.Merge(sets);
            var res = set.ToCharacterClass(complement);
            return res;
        }

        public uint Choose(BV s)
        {
            for (int i = 0; i < atoms.Length; i++)
            {
                if (IsSatisfiable(atoms[i] & s))
                {
                    return (char)partition[i].Min;
                }
            }
            throw new AutomataException(AutomataExceptionKind.SetIsEmpty);
        }

        /// <summary>
        /// Choose a random member uniformly at random from the BV set.
        /// </summary>
        public char ChooseUniformly(BV s)
        {
            int K = (int)ComputeDomainSize(s);
            int k = chooser.Choose(K);
            for (int i = 0; i < atoms.Length; i++)
            {
                if (IsSatisfiable(atoms[i] & s))
                {
                    if (k < partition[i].Count)
                        return (char)partition[i][k];
                    else
                        k = k - partition[i].Count;
                }
            }
            throw new AutomataException(AutomataExceptionKind.SetIsEmpty);
        }

        public BDD ConvertToCharSet(IBDDAlgebra solver, BV pred)
        {
            BDD res = solver.False;
            if (!pred.Equals(this.zero))
            {
                for (int i = 0; i < atoms.Length; i++)
                {
                    //construct the union of the corresponding atoms
                    if (!(pred & atoms[i]).Equals(this.zero))
                    {
                        BDD bdd_i = partition[i].AsBDD(solver);
                        res = solver.MkOr(res, bdd_i);
                    }
                }
            }
            return res;
        }

        public BV[] GetPartition()
        {
            return atoms;
        }

        public IEnumerable<char> GenerateAllCharacters(BV set)
        {
            for (int i = 0; i < atoms.Length; i++)
            {
                if (IsSatisfiable(atoms[i] & set))
                    foreach (uint elem in partition[i].Enumerate())
                        yield return (char)elem;
            }
        }

        #region serialization
        /// <summary>
        /// Serialize
        /// </summary>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("d", dtree);
            info.AddValue("p", SerializePartition());
        }

        /// <summary>
        /// Deserialize
        /// </summary>
        public BVAlgebra(SerializationInfo info, StreamingContext context)
            : this((DecisionTree)info.GetValue("d", typeof(DecisionTree)),
                  DeserializePartition(info.GetString("p")))
        {
        }
        #endregion

        #region not implemented
        public BV MkCharPredicate(string name, BV pred)
        {
            throw new NotImplementedException();
        }

        public BV MkSet(uint e)
        {
            throw new NotImplementedException();
        }

        public string PrettyPrint(BV t, Func<BV, string> varLookup)
        {
            throw new NotImplementedException();
        }

        public string PrettyPrintCS(BV t, Func<BV, string> varLookup)
        {
            throw new NotImplementedException();
        }
        #endregion

        /// <summary>
        /// calls bv.Serialize()
        /// </summary>
        public string SerializePredicate(BV bv)
        {
            return bv.Serialize();
        }

        /// <summary>
        /// calls BV.Deserialize(s)
        /// </summary>
        public BV DeserializePredicate(string s)
        {
            return BV.Deserialize(s);
        }
    }
}
