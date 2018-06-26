using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Microsoft.Automata
{
    /// <summary>
    /// Bit vector algebra
    /// </summary>
    public class BVAlgebra : IPartitionedCharAlgebra<BV>
    {
        int nrOfBits;
        MintermGenerator<BV> mtg;
        CharSetSolver solver;
        internal DecisionTree dtree;

        BV zero;
        BV ones;

        ulong[] all0;
        ulong[] all1;

        internal BV[] atoms;

        internal BDD[] minterms;

        Dictionary<BDD, BV> predMap = new Dictionary<BDD, BV>();

        public BV MapPredToBV(BDD pred)
        {
            if (pred == null)
                return null;
            BV bv;
            if (!predMap.TryGetValue(pred, out bv))
            {
                bv = zero;
                for (int i = 0; i < minterms.Length; i++)
                    if (solver.IsSatisfiable(solver.MkAnd(pred, minterms[i])))
                        bv = bv | MkBV(i);
                predMap[pred] = bv;
            }
            return bv;
        }

        public BVAlgebra(CharSetSolver solver, BDD[] minterms)
        {
            this.minterms = minterms;
            this.solver = solver;
            this.nrOfBits = minterms.Length;
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
            this.zero = new BV(minterms.Length,  0, all0);
            this.ones = new BV(minterms.Length, (K==0 ? lastMask : ulong.MaxValue), all1);
            this.mtg = new MintermGenerator<BV>(this);
            this.dtree = DecisionTree.Create(solver, minterms);
            this.atoms = new BV[minterms.Length];
            for (int i = 0; i < minterms.Length; i++)
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
                return solver.Encoding;
            }
        }

        public CharSetSolver CharSetProvider
        {
            get
            {
                return solver;
            }
        }

        public bool AreEquivalent(BV predicate1, BV predicate2)
        {
            return predicate1.Equals(predicate2);
        }

        public bool CheckImplication(BV lhs, BV rhs)
        {
            return ((~lhs) | rhs).Equals(this.ones);
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
            return ~predicate;
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
            var bv = new BV(this.nrOfBits, first, more);
            return bv;
        }

        public BV MkRangeConstraint(char lower, char upper, bool caseInsensitive = false)
        {
            throw new AutomataException(AutomataExceptionKind.NotSupported);
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
            BV res = this.zero;
            for (int i = 0; i < minterms.Length; i++)
            {
                var conj = solver.MkAnd(minterms[i], set);
                if (solver.IsSatisfiable(conj))
                {
                    res = res | atoms[i];
                }
            }
            return res;
        }

        public bool TryConvertToCharSet(BV pred, out BDD set)
        {
            BDD res = solver.False;
            if (!pred.Equals(this.zero))
            {
                for (int i = 0; i < atoms.Length; i++)
                {
                    //construct the union of the corresponding atoms
                    if (!(pred & atoms[i]).Equals(this.zero))
                        res = solver.MkOr(res, minterms[i]);
                }
            }
            set = res;
            return true;
        }

        public BV MkCharPredicate(string name, BV pred)
        {
            throw new NotImplementedException();
        }

        public BV MkSet(uint e)
        {
            throw new NotImplementedException();
        }

        public string PrettyPrint(BV bv)
        {
            BDD set = solver.CharSetProvider.False;
            for (int i = 0; i < (nrOfBits < 64 ? nrOfBits : 64); i++)
            {
                if ((bv.first & ((ulong)1 << i)) != 0)
                {
                    set = set | minterms[i];
                }
            }
            for (int j = 0; j < bv.more.Length; j++)
            {
                for (int i = 0; i < 64; i++)
                {
                    if ((j + 1) * 64 + i < nrOfBits)
                    {
                        if ((bv.more[j] & ((ulong)1 << i)) != 0)
                        {
                            set = set | minterms[(((j + 1) * 64) + i)];
                        }
                    }
                }
            }
            var res = solver.CharSetProvider.PrettyPrint(set);
            return res;
        }

        public string PrettyPrint(BV t, Func<BV, string> varLookup)
        {
            throw new NotImplementedException();
        }

        public string PrettyPrintCS(BV t, Func<BV, string> varLookup)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Choose a random member from the BV set.
        /// The member is chosen from the union of the underlying minterm BDDs corresponding to s.
        /// </summary>
        public uint Choose(BV s)
        {
            if (s.Equals(this.zero))
                throw new AutomataException(AutomataExceptionKind.SetIsEmpty);
            BDD bdd;
            TryConvertToCharSet(s, out bdd);
            var res = solver.Choose(bdd);
            return res;
        }

        /// <summary>
        /// Choose a random member uniformly at random from the BV set.
        /// The member is chosen from the union of the underlying minterm BDDs corresponding to s.
        /// </summary>
        public char ChooseUniformly(BV s)
        {
            if (s.Equals(this.zero))
                throw new AutomataException(AutomataExceptionKind.SetIsEmpty);
            BDD bdd;
            TryConvertToCharSet(s, out bdd);
            var res = solver.ChooseUniformly(bdd);
            return res;
        }

        public BDD ConvertToCharSet(BV pred)
        {
            BDD res;
            if (TryConvertToCharSet(pred, out res))
                return res;
            else
                return null;
        }

        public BV[] GetPartition()
        {
            return atoms;
        }
    }

    /// <summary>
    /// Represents a bitvector
    /// </summary>
    public class BV : IComparable
    {
        int nrOfBits;
        internal ulong first;
        internal ulong[] more;

        /// <summary>
        /// Constructs a bitvector
        /// </summary>
        /// <param name="first">first 64 bits</param>
        /// <param name="more">remaining bits in 64 increments</param>
        internal BV(int nrOfBits, ulong first, params ulong[] more) 
        {
            this.nrOfBits = nrOfBits;
            this.first = first;
            this.more = more;
        }

        /// <summary>
        /// Bitwise AND
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BV operator &(BV x, BV y)
        {
            int k = (x.more.Length <= y.more.Length ? x.more.Length : y.more.Length);
            var first = x.first & y.first;
            var more = new ulong[k];
            for (int i = 0; i < k; i++)
            {
                more[i] = x.more[i] & y.more[i];
            }
            return new BV(x.nrOfBits, first, more);
        }

        /// <summary>
        /// Bitwise OR
        /// </summary>
        public static BV operator |(BV x, BV y)
        {
            int k = (x.more.Length <= y.more.Length ? x.more.Length : y.more.Length);
            var first = x.first | y.first;
            var more = new ulong[k];
            for (int i = 0; i < k; i++)
            {
                more[i] = x.more[i] | y.more[i];
            }
            return new BV(x.nrOfBits, first, more);
        }

        /// <summary>
        /// Bitwise XOR
        /// </summary>
        public static BV operator ^(BV x, BV y)
        {
            int k = (x.more.Length <= y.more.Length ? x.more.Length : y.more.Length);
            var first = x.first ^ y.first;
            var more = new ulong[x.more.Length];
            for (int i = 0; i < x.more.Length; i++)
            {
                more[i] = x.more[i] ^ y.more[i];
            }
            return new BV(x.nrOfBits, first, more);
        }

        /// <summary>
        /// Bitwise NOT
        /// </summary>
        public static BV operator ~(BV x)
        {
            var first = ~x.first;
            if (x.nrOfBits < 64)
                first = first & (((ulong)1 << x.nrOfBits) - 1);
            var more = new ulong[x.more.Length];
            int remNrOfBits = x.nrOfBits;
            for (int i = 0; i < x.more.Length; i++)
            {
                remNrOfBits = x.nrOfBits - 64;
                more[i] = ~x.more[i];
                if (remNrOfBits < 64)
                    more[i] = more[i] & (((ulong)1 << x.nrOfBits) - 1);
            }
            var notx = new BV(x.nrOfBits, first, more);
            return notx;
        }

        /// <summary>
        /// less than
        /// </summary>
        public static bool operator <(BV x, BV y)
        {
            return x.CompareTo(y) < 0;
        }

        /// <summary>
        /// greater than
        /// </summary>
        public static bool operator >(BV x, BV y)
        {
            return x.CompareTo(y) > 0;
        }

        /// <summary>
        /// less than or equal
        /// </summary>
        public static bool operator <=(BV x, BV y)
        {
            return x.CompareTo(y) <= 0;
        }

        /// <summary>
        /// greater than or equal
        /// </summary>
        public static bool operator >=(BV x, BV y)
        {
            return x.CompareTo(y) >= 0;
        }

        /// <summary>
        /// Shows which bits are true
        /// </summary>
        public override string ToString()
        {
            List<int> bits = new List<int>();
            for (int i = 0; i < (nrOfBits < 64 ? nrOfBits : 64); i++)
            {
                if ((first & ((ulong)1 << i)) != 0)
                {
                    bits.Add(i);
                }
            }
            for (int j = 0; j < more.Length; j++)
            {
                for (int i = 0; i < 64; i++)
                {
                    if ((j + 1) * 64 + i < nrOfBits)
                    {
                        if ((more[j] & ((ulong)1 << i)) != 0)
                        {
                            bits.Add(((j + 1) * 64) + i);
                        }
                    }
                }
            }
            return DisplayIntervals(bits);
        }

        internal static string DisplayIntervals(List<int> bits)
        {
            List<Tuple<int, int>> intervals = new List<Tuple<int, int>>();
            int last = -1;
            foreach (var b in bits)
            {
                if (last == -1)
                {
                    intervals.Add(new Tuple<int, int>(b, b));
                    last = 0;
                }
                else if (intervals[last].Item2 == b - 1)
                {
                    intervals[last] = new Tuple<int, int>(intervals[last].Item1, b);
                }
                else
                {
                    intervals.Add(new Tuple<int, int>(b, b));
                    last += 1;
                }
            }
            string res = "";
            foreach (var pair in intervals)
            {
                if (res != "")
                    res += ",";
                if (pair.Item1 == pair.Item2)
                    res += pair.Item1;
                else if (pair.Item2 == pair.Item1 + 1)
                    res += pair.Item1 + "," + pair.Item2;
                else
                    res += pair.Item1 + ".." + pair.Item2;
            }
            return  "[" + res + "]";
        }

        public override bool Equals(object obj)
        {
            BV bv = obj as BV;
            if (bv == null)
                return false;
            if (this == bv)
                return true;
            if (this.first != bv.first)
                return false;
            if (bv.more.Length != this.more.Length)
                return false;
            for (int i = 0; i < more.Length; i++)
            {
                if (more[i] != bv.more[i])
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            int h = first.GetHashCode();
            for (int i = 0; i < more.Length; i++)
            {
                h = (h << 5) ^ more[i].GetHashCode();
            }
            return h;
        }

        public int CompareTo(object obj)
        {
            BV that = obj as BV;
            if (that == null)
                return 1;
            else if (this.nrOfBits != that.nrOfBits)
                return (this.nrOfBits.CompareTo(that.nrOfBits));
            else
            {
                int k = this.more.Length;
                if (k > 0)
                {
                    int i = k - 1;
                    while (i >= 0)
                    {
                        var comp = this.more[i].CompareTo(that.more[i]);
                        if (comp == 0)
                            i = i - 1;
                        else
                            return comp;
                    }
                }
                return this.first.CompareTo(that.first);
            }
        }
    }
}
