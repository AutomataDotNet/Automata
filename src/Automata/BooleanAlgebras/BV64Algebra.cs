using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.Automata
{
    /// <summary>
    /// Bit vector algebra of up to 64 bits
    /// </summary>
    [Serializable]
    public class BV64Algebra : BVAlgebraBase, ICharAlgebra<ulong>, ISerializable
    {
        [NonSerialized]
        MintermGenerator<ulong> mtg;
        [NonSerialized]
        Chooser chooser = new Chooser();
        [NonSerialized]
        ulong zero = 0;
        [NonSerialized]
        ulong all;
        [NonSerialized]
        internal ulong[] atoms;

        public ulong ComputeDomainSize(ulong set)
        {
            int size = 0;
            for (int i = 0; i < atoms.Length; i++)
            {
                if (IsSatisfiable(set & atoms[i]))
                    size += partition[i].Count;
            }
            return (ulong)size;
        }

        public ulong MapPredToBV(BDD pred, BDD[] minterms)
        {
            if (pred == null)
                return zero;
            var alg = pred.algebra;
            ulong bv;

            bv = zero;
            for (int i = 0; i < minterms.Length; i++)
                if (alg.IsSatisfiable(alg.MkAnd(pred, minterms[i])))
                    bv = bv | MkBV(i);

            return bv;
        }

        public static BV64Algebra Create(CharSetSolver solver, BDD[] minterms)
        {
            if (minterms.Length > 64)
                throw new AutomataException(AutomataExceptionKind.NrOfMintermsCanBeAtMost64);
            var dtree = DecisionTree.Create(solver, minterms);
            var partitionBase = Array.ConvertAll(minterms, m => m.ToRanges());
            var partition = Array.ConvertAll(partitionBase, p => new IntervalSet(p));
            return new BV64Algebra(dtree, partition);
        }

        private BV64Algebra(DecisionTree dtree, IntervalSet[] partition) : base(dtree, partition, partition.Length)
        {
            this.all = ulong.MaxValue >> (64 - this.nrOfBits);
            this.mtg = new MintermGenerator<ulong>(this);
            this.atoms = new ulong[this.nrOfBits];
            for (int i = 0; i < this.nrOfBits; i++)
            {
                atoms[i] = ((ulong)1) << i;
            }
        }

        /// <summary>
        /// Create a variant of the algebra where each minterms is replaced with a singleton set starting from '0'
        /// Used for testing purposes.
        /// </summary>
        internal BV64Algebra ReplaceMintermsWithVisibleCharacters()
        {
            Func<int, int> f = x =>
            {
                int k;
                if (x <= 26)
                    k = ('A' + (x - 1));
                else if (x <= 52)
                    k = ('a' + (x - 27));
                else if (x <= 62)
                    k = ('0' + (x - 53));
                else
                    k = '=';
                return k;
            };
            var simplified_partition = new IntervalSet[this.partition.Length];
            int[] precomp = new int[256];
            for (int i=1; i < simplified_partition.Length; i++)
            {
                int k = f(i);
                simplified_partition[i] = new IntervalSet(new Tuple<uint, uint>((uint)k,(uint)k));
                precomp[k] = i;
            }
            var zeroIntervals = new List<Tuple<uint, uint>>();
            int lower = 0;
            int upper = 0;
            for (int i = 1; i <= 'z' + 1; i++)
            {
                if (precomp[i] == 0)
                {
                    if (upper == i - 1)
                        upper += 1;
                    else
                    {
                        zeroIntervals.Add(new Tuple<uint, uint>((uint)lower, (uint)upper));
                        lower = i;
                        upper = i;
                    }             
                }
            }
            zeroIntervals.Add(new Tuple<uint, uint>((uint)lower, 0xFFFF));
            simplified_partition[0] = new IntervalSet(zeroIntervals.ToArray());

            var simplified_dtree = new DecisionTree(precomp, new DecisionTree.BST(0, null, null));
            return new BV64Algebra(simplified_dtree, simplified_partition);
        }

        public ulong False
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

        public ulong True
        {
            get
            {
                return all;
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
                return RegexExtensionMethods.Context;
            }
        }

        public bool AreEquivalent(ulong predicate1, ulong predicate2)
        {
            return predicate1 == predicate2;
        }

        public bool CheckImplication(ulong lhs, ulong rhs)
        {
            return (lhs | rhs) == rhs;
        }

        public bool EvaluateAtom(ulong atom, ulong psi)
        {
            return (atom & psi).Equals(atom);
        }

        public IEnumerable<Tuple<bool[], ulong>> GenerateMinterms(params ulong[] constraints)
        {
            return this.mtg.GenerateMinterms(constraints);
        }

        public ulong GetAtom(ulong psi)
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
                                    return 1;
                                else
                                    return 2;
                            }
                            else
                            {
                                if ((psi & 4) != 0)
                                    return 4;
                                else
                                    return 8;
                            }
                            #endregion
                        }
                        else
                        {
                            #region second nibble of first byte
                            if ((psi & 0x30) != 0)
                            {
                                if ((psi & 0x10) != 0)
                                    return 0x10;
                                else
                                    return 0x20;
                            }
                            else
                            {
                                if ((psi & 0x40) != 0)
                                    return 0x40;
                                else
                                    return 0x80;
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
                                    return 0x100;
                                else
                                    return 0x200;
                            }
                            else
                            {
                                if ((psi & 0x400) != 0)
                                    return 0x400;
                                else
                                    return 0x800;
                            }
                            #endregion
                        }
                        else
                        {
                            #region second nibble of second byte
                            if ((psi & 0x3000) != 0)
                            {
                                if ((psi & 0x1000) != 0)
                                    return 0x1000;
                                else
                                    return 0x2000;
                            }
                            else
                            {
                                if ((psi & 0x4000) != 0)
                                    return 0x4000;
                                else
                                    return 0x8000;
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
                                    return 0x10000;
                                else
                                    return 0x20000;
                            }
                            else
                            {
                                if ((psi & 0x40000) != 0)
                                    return 0x40000;
                                else
                                    return 0x80000;
                            }
                            #endregion
                        }
                        else
                        {
                            #region second nibble of first byte
                            if ((psi & 0x300000) != 0)
                            {
                                if ((psi & 0x100000) != 0)
                                    return 0x100000;
                                else
                                    return 0x200000;
                            }
                            else
                            {
                                if ((psi & 0x400000) != 0)
                                    return 0x400000;
                                else
                                    return 0x800000;
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
                                    return 0x1000000;
                                else
                                    return 0x2000000;
                            }
                            else
                            {
                                if ((psi & 0x4000000) != 0)
                                    return 0x4000000;
                                else
                                    return 0x8000000;
                            }
                            #endregion
                        }
                        else
                        {
                            #region second nibble of second byte
                            if ((psi & 0x30000000) != 0)
                            {
                                if ((psi & 0x10000000) != 0)
                                    return 0x10000000;
                                else
                                    return 0x20000000;
                            }
                            else
                            {
                                if ((psi & 0x40000000) != 0)
                                    return 0x40000000;
                                else
                                    return 0x80000000;
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
                                    return 0x100000000;
                                else
                                    return 0x200000000;
                            }
                            else
                            {
                                if ((psi & 0x400000000) != 0)
                                    return 0x400000000;
                                else
                                    return 0x800000000;
                            }
                            #endregion
                        }
                        else
                        {
                            #region second nibble of first byte
                            if ((psi & 0x3000000000) != 0)
                            {
                                if ((psi & 0x1000000000) != 0)
                                    return 0x1000000000;
                                else
                                    return 0x2000000000;
                            }
                            else
                            {
                                if ((psi & 0x4000000000) != 0)
                                    return 0x4000000000;
                                else
                                    return 0x8000000000;
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
                                    return 0x10000000000;
                                else
                                    return 0x20000000000;
                            }
                            else
                            {
                                if ((psi & 0x40000000000) != 0)
                                    return 0x40000000000;
                                else
                                    return 0x80000000000;
                            }
                            #endregion
                        }
                        else
                        {
                            #region second nibble of second byte
                            if ((psi & 0x300000000000) != 0)
                            {
                                if ((psi & 0x100000000000) != 0)
                                    return 0x100000000000;
                                else
                                    return 0x200000000000;
                            }
                            else
                            {
                                if ((psi & 0x400000000000) != 0)
                                    return 0x400000000000;
                                else
                                    return 0x800000000000;
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
                                    return 0x1000000000000;
                                else
                                    return 0x2000000000000;
                            }
                            else
                            {
                                if ((psi & 0x4000000000000) != 0)
                                    return 0x4000000000000;
                                else
                                    return 0x8000000000000;
                            }
                            #endregion
                        }
                        else
                        {
                            #region second nibble of first byte
                            if ((psi & 0x30000000000000) != 0)
                            {
                                if ((psi & 0x10000000000000) != 0)
                                    return 0x10000000000000;
                                else
                                    return 0x20000000000000;
                            }
                            else
                            {
                                if ((psi & 0x40000000000000) != 0)
                                    return 0x40000000000000;
                                else
                                    return 0x80000000000000;
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
                                    return 0x100000000000000;
                                else
                                    return 0x200000000000000;
                            }
                            else
                            {
                                if ((psi & 0x400000000000000) != 0)
                                    return 0x400000000000000;
                                else
                                    return 0x800000000000000;
                            }
                            #endregion
                        }
                        else
                        {
                            #region second nibble of second byte
                            if ((psi & 0x3000000000000000) != 0)
                            {
                                if ((psi & 0x1000000000000000) != 0)
                                    return 0x1000000000000000;
                                else
                                    return 0x2000000000000000;
                            }
                            else
                            {
                                if ((psi & 0x4000000000000000) != 0)
                                    return 0x4000000000000000;
                                else
                                    return 0x8000000000000000;
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
        public bool IsSatisfiable(ulong predicate)
        {
            return predicate != zero;
        }

        public ulong MkAnd(params ulong[] predicates)
        {
            var and = all;
            for (int i = 0; i < predicates.Length; i++)
            {
                and = and & predicates[i];
                if (and == zero)
                    return zero;
            }
            return and;
        }

        public ulong MkAnd(IEnumerable<ulong> predicates)
        {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong MkAnd(ulong predicate1, ulong predicate2)
        {
            return predicate1 & predicate2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong MkDiff(ulong predicate1, ulong predicate2)
        {
            return predicate1 & ~predicate2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong MkNot(ulong predicate)
        {
            return all & ~predicate;
        }

        public ulong MkOr(IEnumerable<ulong> predicates)
        {
            var res = zero;
            foreach (var p in predicates)
            {
                res = res | p;
                if (res == all)
                    return all;
            }
            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong MkOr(ulong predicate1, ulong predicate2)
        {
            return predicate1 | predicate2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong MkSymmetricDifference(ulong p1, ulong p2)
        {
            return (p1 ^ p2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Simplify(ulong predicate)
        {
            return predicate;
        }

        public ulong MkBV(params int[] truebits)
        {
            ulong bv = 0;
            for (int i = 0; i < truebits.Length; i++)
            {
                int b = truebits[i];
                if (b >= nrOfBits || b < 0)
                    throw new AutomataException(AutomataExceptionKind.BitOutOfRange);
                bv = bv | ((ulong)1 << i);
            }
            return bv;
        }

        public ulong MkRangeConstraint(char lower, char upper, bool caseInsensitive = false)
        {
            throw new NotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong MkCharConstraint(char c, bool caseInsensitive = false)
        {
            if (caseInsensitive == true)
                throw new AutomataException(AutomataExceptionKind.NotSupported);
            return this.atoms[this.dtree.GetId(c)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetIdOfChar(char c)
        {
            return this.dtree.GetId(c);
        }

        public ulong MkRangesConstraint(bool caseInsensitive, IEnumerable<char[]> ranges)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Assumes that set is a union of some minterms (or empty).
        /// If null then 0 is returned.
        /// </summary>
        public ulong ConvertFromCharSet(BDD set)
        {
            if (set == null)
                return zero;
            var alg = set.algebra;
            ulong res = this.zero;
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

        public bool TryConvertToCharSet(IBDDAlgebra solver, ulong pred, out BDD set)
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
        public string PrettyPrint(ulong bv)
        {
            var lab1 = PrettyPrintHelper(bv, false);
            var lab2 = PrettyPrintHelper(~bv, true);
            if (lab1.Length <= lab2.Length)
                return lab1;
            else
                return lab2;

        }

        string PrettyPrintHelper(ulong bv, bool complement)
        {
            List<IntervalSet> sets = new List<IntervalSet>();
            for (int i = 0; i < atoms.Length; i++)
                if (IsSatisfiable(bv & atoms[i]))
                    sets.Add(partition[i]);
            var set = IntervalSet.Merge(sets);
            var res = set.ToCharacterClass(complement);
            return res;
        }

        public uint Choose(ulong s)
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
        /// Choose a random member uniformly at random from the ulong set.
        /// </summary>
        public char ChooseUniformly(ulong s)
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

        public BDD ConvertToCharSet(IBDDAlgebra solver, ulong pred)
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

        public ulong[] GetPartition()
        {
            return atoms;
        }

        public IEnumerable<char> GenerateAllCharacters(ulong set)
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
        public BV64Algebra(SerializationInfo info, StreamingContext context)
            : this((DecisionTree)info.GetValue("d", typeof(DecisionTree)), 
                  DeserializePartition(info.GetString("p")))
        {
        }

        /// <summary>
        /// Serialize s as a hexadecimal numeral using lowercase letters
        /// </summary>
        /// <param name="s">given predicate</param>
        public string SerializePredicate(ulong s)
        {
            return s.ToString("x");
        }

        /// <summary>
        /// Deserialize s from a string created by SerializePredicate
        /// </summary>
        /// <param name="s">given hexadecimal numeral representation</param>
        public ulong DeserializePredicate(string s)
        {
            return ulong.Parse(s, System.Globalization.NumberStyles.HexNumber);
        }
        #endregion

        #region not implemented
        public ulong MkCharPredicate(string name, ulong pred)
        {
            throw new NotImplementedException();
        }

        public ulong MkSet(uint e)
        {
            throw new NotImplementedException();
        }

        public string PrettyPrint(ulong t, Func<ulong, string> varLookup)
        {
            throw new NotImplementedException();
        }

        public string PrettyPrintCS(ulong t, Func<ulong, string> varLookup)
        {
            throw new NotImplementedException();
        }
        #endregion

    }
}