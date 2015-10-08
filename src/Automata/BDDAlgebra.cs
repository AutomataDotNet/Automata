using System;
using System.Collections.Generic;

namespace Microsoft.Automata
{
    /// <summary>
    /// Solver for BDDs.
    /// </summary>
    public class BDDAlgebra : IBoolAlgMinterm<BDD>
    {
        Dictionary<BvSet_Int, BDD> restrictCache = new Dictionary<BvSet_Int, BDD>();
        Dictionary<BvSetPair, BDD> orCache = new Dictionary<BvSetPair, BDD>();
        Dictionary<BvSetPair, BDD> andCache = new Dictionary<BvSetPair, BDD>();
        Dictionary<BDD, BDD> notCache = new Dictionary<BDD, BDD>();
        Dictionary<BDD, BDD> srCache = new Dictionary<BDD, BDD>();
        Dictionary<BvSet_Int, BDD> slCache = new Dictionary<BvSet_Int, BDD>();
        //Chooser _chooser_ = new Chooser();

        MintermGenerator<BDD> mintermGen;

        /// <summary>
        /// Construct a solver for bitvector sets.
        /// </summary>
        public BDDAlgebra()
        {
            mintermGen = new MintermGenerator<BDD>(this);
            mintermGen.HashCodesRespectEquivalence = true;
        }

        //internalize the creation of all charsets so that any two charsets with same bit and children are the same pointers
        Dictionary<BvSetKey, BDD> bvsetCache = new Dictionary<BvSetKey, BDD>();

        internal BDD MkBvSet(int bit, BDD one, BDD zero)
        {
            var key = new BvSetKey(bit, one, zero);
            BDD set;
            if (!bvsetCache.TryGetValue(key, out set))
            {
                set = new BDD(bit, one, zero);
                bvsetCache[key] = set;
            }
            return set;
        }

        #region IBoolAlg members

        /// <summary>
        /// Make the union of a and b
        /// </summary>
        public BDD MkOr(BDD a, BDD b)
        {
            if (a == False)
                return b;
            if (b == False)
                return a;
            if (a == True || b == True)
                return True;
            if (a == b)
                return a;

            var key = MkApplyKey(a, b);
            BDD res;
            if (orCache.TryGetValue(key, out res))
                return res;

            if (b.bit > a.bit)
            {
                BDD t = MkOr(a, Restrict(b.bit, true, b));
                BDD f = MkOr(a, Restrict(b.bit, false, b));
                res = (t == f ? t : MkBvSet(b.bit, t, f));
            }
            else if (a.bit > b.bit)
            {
                BDD t = MkOr(Restrict(a.bit, true, a), b);
                BDD f = MkOr(Restrict(a.bit, false, a), b);
                res = (t == f ? t : MkBvSet(a.bit, t, f));
            }
            else //a.bit == b.bit
            {
                BDD t = MkOr(Restrict(a.bit, true, a), Restrict(a.bit, true, b));
                BDD f = MkOr(Restrict(a.bit, false, a), Restrict(a.bit, false, b));
                res = (t == f ? t : MkBvSet(a.bit, t, f));
            }

            orCache[key] = res;
            return res;
        }

        /// <summary>
        /// Make the intersection of a and b
        /// </summary>
        public BDD MkAnd(BDD a, BDD b)
        {
            if (a == True)
                return b;
            if (b == True)
                return a;
            if (a == False || b == False)
                return False;
            if (a == b)
                return a;

            var key = MkApplyKey(a, b);
            BDD res;
            if (andCache.TryGetValue(key, out res))
                return res;

            if (b.bit > a.bit)
            {
                BDD t = MkAnd(a, Restrict(b.bit, true, b));
                BDD f = MkAnd(a, Restrict(b.bit, false, b));
                res = (t == f ? t : MkBvSet(b.bit, t, f));
            }
            else if (a.bit > b.bit)
            {
                BDD t = MkAnd(Restrict(a.bit, true, a), b);
                BDD f = MkAnd(Restrict(a.bit, false, a), b);
                res = (t == f ? t : MkBvSet(a.bit, t, f));
            }
            else //a.x == b.x
            {
                BDD t = MkAnd(Restrict(a.bit, true, a), Restrict(a.bit, true, b));
                BDD f = MkAnd(Restrict(a.bit, false, a), Restrict(a.bit, false, b));
                res = (t == f ? t : MkBvSet(a.bit, t, f));
            }

            andCache[key] = res;
            return res;
        }

        /// <summary>
        /// Make the difference a - b
        /// </summary>
        public BDD MkDiff(BDD a, BDD b)
        {
            return MkAnd(a, MkNot(b));
        }

        /// <summary>
        /// Complement a
        /// </summary>
        public BDD MkNot(BDD a)
        {
            if (a == False)
                return True;
            if (a == True)
                return False;

            BDD neg;
            if (notCache.TryGetValue(a, out neg))
                return neg;

            neg = MkBvSet(a.bit, MkNot(a.one), MkNot(a.zero));
            notCache[a] = neg;
            return neg;
        }

        /// <summary>
        /// Intersect all sets in the enumeration
        /// </summary>
        public BDD MkAnd(IEnumerable<BDD> sets)
        {
            BDD res = True;
            foreach (BDD bdd in sets)
                res = MkAnd(res, bdd);
            return res;
        }

        public BDD MkAnd(params BDD[] sets)
        {
            BDD res = True;
            foreach (BDD bdd in sets)
                res = MkAnd(res, bdd);
            return res;
        }

        /// <summary>
        /// Take the union of all sets in the enumeration
        /// </summary>
        public BDD MkOr(IEnumerable<BDD> sets)
        {
            BDD res = False;
            foreach (BDD bdd in sets)
                res = MkOr(res, bdd);
            return res;
        }

        /// <summary>
        /// Gets the full character set.
        /// </summary>
        public BDD True
        {
            get { return BDD.Full; }
        }

        /// <summary>
        /// Gets the empty character set.
        /// </summary>
        public BDD False
        {
            get { return BDD.Empty; }
        }

        /// <summary>
        /// Returns true if the set is nonempty.
        /// </summary>
        public bool IsSatisfiable(BDD set)
        {
            return set != False;
        }

        /// <summary>
        /// Returns true if a and b represent mathematically equal sets of characters.
        /// Two Charsets are by construction equivalent iff they are identical.
        /// </summary>
        public bool AreEquivalent(BDD a, BDD b)
        {
            return a == b;
        }

        #endregion

        #region bit-shift operations

        /// <summary>
        /// Shift all elements one bit to the right. 
        /// For example if set denotes {*0000,*1110,*1111} then 
        /// ShiftRight(set) denotes {*000,*111} where * denotes any prefix of 0's or 1's.
        /// </summary>
        public BDD ShiftRight(BDD set)
        {
            if (set.IsTrivial)
                return set;

            if (set.bit == 0)
                return True;

            BDD res;
            if (srCache.TryGetValue(set, out res))
                return res;

            BDD zero = ShiftRight(set.zero);
            BDD one = ShiftRight(set.one);

            if (zero == one)
                res = zero;
            else
                res = MkBvSet(set.bit - 1, one, zero);

            srCache[set] = res;
            return res;
        }

        /// <summary>
        /// First applies ShiftRight and then sets bit k to 0.
        /// </summary>
        public BDD ShiftRight0(BDD set, int k)
        {
            var s1 = ShiftRight(set);
            var mask = MkSetWithBitFalse(k);
            var res = MkAnd(mask, s1);
            return res;
        }

        /// <summary>
        /// Shift all elements k bits to the left. 
        /// For example if k=1 and set denotes {*0000,*1111} then 
        /// ShiftLeft(set) denotes {*00000,*00001,*11110,*11111} where * denotes any prefix of 0's or 1's.
        /// </summary>
        public BDD ShiftLeft(BDD set, int k = 1)
        {
            if (set.IsTrivial || k == 0)
                return set;
            //if (set.bit + k <= MSB)
            return ShiftLeft_(set, k);
            //else
            //{
            //    var set1 = ShiftLeft(set, k - 1);
            //    return ShiftLeft1(set1);
            //}
        }

        private BDD Collapse(BDD set, int k)
        {
            throw new NotImplementedException();
        }

        BDD ShiftLeft_(BDD set, int k)
        {
            if (set.IsTrivial || k == 0)
                return set;

            var key = new BvSet_Int(set, k);

            BDD res;
            if (slCache.TryGetValue(key, out res))
                return res;

            BDD zero = ShiftLeft_(set.zero, k);
            BDD one = ShiftLeft_(set.one, k);

            if (zero == one)
                res = zero;
            else
                res = MkBvSet(set.bit + k, one, zero);

            slCache[key] = res;
            return res;
        }

        BDD ShiftLeft1(BDD set)
        {
            if (set.IsTrivial)
                return set;

            var key = new BvSet_Int(set, 1);

            BDD res;
            if (slCache.TryGetValue(key, out res))
                return res;

            BDD zero = ShiftLeft1(set.zero);
            BDD one = ShiftLeft1(set.one);

            if (zero == one)
                res = zero;
            //else if (set.bit == MSB)
            //    res = MkOr(zero, one);
            else
                res = MkBvSet(set.bit + 1, one, zero);

            slCache[key] = res;
            return res;
        }

        #endregion

        #region internal helpers

        static BvSet_Int MkRestrictKey(int v, bool makeTrue, BDD bdd)
        {
            var res = new BvSet_Int(bdd, (v << 1) + (makeTrue ? 1 : 0));
            return res;
        }

        static BvSetPair MkApplyKey(BDD bdd1, BDD bdd2)
        {
            BvSetPair res = new BvSetPair(bdd1, bdd2);
            return res;
        }

        private class BvSetPair
        {
            BDD a;
            BDD b;
            internal BvSetPair(BDD a, BDD b)
            {
                this.a = a;
                this.b = b;
            }
            public override int GetHashCode()
            {
                return a.GetHashCode() + (b.GetHashCode() << 1);
            }
            public override bool Equals(object obj)
            {
                BvSetPair key = (BvSetPair)obj;
                return key.a == a && key.b == b;
            }
        }

        BDD Restrict(int bit, bool makeTrue, BDD bdd)
        {
            var key = MkRestrictKey(bit, makeTrue, bdd);
            BDD res;
            if (restrictCache.TryGetValue(key, out res))
                return res;

            if (bit > bdd.bit)
                res = bdd;
            else if (bdd.bit > bit)
            {
                BDD t = Restrict(bit, makeTrue, bdd.one);
                BDD f = Restrict(bit, makeTrue, bdd.zero);
                res = (f == t ? t : (f == bdd.zero && t == bdd.one ? bdd : MkBvSet(bdd.bit, t, f)));
            }
            else
                res = (makeTrue ? bdd.one : bdd.zero);
            restrictCache[key] = res;
            return res;
        }

        #endregion

        #region Minterm generation

        public IEnumerable<Pair<bool[], BDD>> GenerateMinterms(params BDD[] sets)
        {
            return mintermGen.GenerateMinterms(sets);
        }

        #endregion

        /// <summary>
        /// Creates the set that contains all elements whose k'th bit is true.
        /// </summary>
        public BDD MkSetWithBitTrue(int k)
        {
            return MkBvSet(k, True, False);
        }

        /// <summary>
        /// Creates the set that contains all elements whose k'th bit is false.
        /// </summary>
        public BDD MkSetWithBitFalse(int k)
        {
            return MkBvSet(k, False, True);
        }


        public BDD MkSetFromElements(IEnumerable<uint> elems, int maxBit)
        {
            var s = False;
            foreach (var elem in elems)
            {
                s = MkOr(s, MkSingleton(elem, maxBit));
            }
            return s;
        }

        public BDD MkSetFromElements(IEnumerable<int> elems, int maxBit)
        {
            var s = False;
            foreach (var elem in elems)
            {
                s = MkOr(s, MkSingleton((uint)elem, maxBit));
            }
            return s;
        }

        public BDD MkSetFromElements(IEnumerable<ulong> elems, int maxBit)
        {
            var s = False;
            foreach (var elem in elems)
            {
                s = MkOr(s, MkSingleton(elem, maxBit));
            }
            return s;
        }


        /// <summary>
        /// Make a singleton set.
        /// </summary>
        /// <param name="n">the single element in the set</param>
        /// <param name="maxBit">bits above maxBit are unspecified</param>
        /// <returns></returns>
        public BDD MkSingleton(uint n, int maxBit)
        {
            var cs = MkSetFromRange(n, n, maxBit);
            return cs;
        }

        /// <summary>
        /// Make a singleton set.
        /// </summary>
        /// <param name="n">the single element in the set</param>
        /// <param name="maxBit">bits above maxBit are unspecified</param>
        /// <returns></returns>
        public BDD MkSingleton(ulong n, int maxBit)
        {
            var cs = MkSetFromRange(n, n, maxBit);
            return cs;
        }

        /// <summary>
        /// Make the set containing all values greater than or equal to m and less than or equal to n.
        /// </summary>
        /// <param name="m">lower bound</param>
        /// <param name="n">upper bound</param>
        /// <param name="maxBit">bits above maxBit are unspecified</param>
        public BDD MkSetFromRange(uint m, uint n, int maxBit)
        {
            if (n < m)
                return False;
            uint mask = (uint)1 << maxBit;
            return CreateFromInterval1(mask, maxBit, m, n);
        }

        /// <summary>
        /// Make the set containing all values greater than or equal to m and less than or equal to n.
        /// </summary>
        /// <param name="m">lower bound</param>
        /// <param name="n">upper bound</param>
        /// <param name="maxBit">bits above maxBit are unspecified</param>
        public BDD MkSetFromRange(ulong m, ulong n, int maxBit)
        {
            if (n < m)
                return False;
            ulong mask = (ulong)1 << maxBit;
            return CreateFromInterval1(mask, maxBit, m, n);
        }

        Dictionary<Pair<int, Pair<ulong, ulong>>, BDD> intervalCache = new Dictionary<Pair<int, Pair<ulong, ulong>>, BDD>();

        BDD CreateFromInterval1(uint mask, int bit, uint m, uint n)
        {
            BDD set;
            var pair = new Pair<ulong, ulong>((ulong)m << 32, (ulong)n);
            var key = new Pair<int, Pair<ulong, ulong>>(bit, pair);

            if (intervalCache.TryGetValue(key, out set))
                return set;

            else
            {

                if (mask == 1) //base case: LSB 
                {
                    if (n == 0)  //implies that m==0 
                        set = MkBvSet(bit, False, True);
                    else if (m == 1) //implies that n==1
                        set = MkBvSet(bit, True, False);
                    else //m=0 and n=1, thus full range from 0 to ((mask << 1)-1)
                        set = True;
                }
                else if (m == 0 && n == ((mask << 1) - 1)) //full interval
                {
                    set = True;
                }
                else //mask > 1, i.e., mask = 2^b for some b > 0, and not full interval
                {
                    //e.g. m = x41 = 100 0001, n = x59 = 101 1001, mask = x40 = 100 0000, ord = 6 = log2(b)
                    uint mb = m & mask; // e.g. mb = b
                    uint nb = n & mask; // e.g. nb = b

                    if (nb == 0) // implies that 1-branch is empty
                    {
                        var fcase = CreateFromInterval1(mask >> 1, bit - 1, m, n);
                        set = MkBvSet(bit, False, fcase);
                    }
                    else if (mb == mask) // implies that 0-branch is empty
                    {
                        var tcase = CreateFromInterval1(mask >> 1, bit - 1, m & ~mask, n & ~mask);
                        set = MkBvSet(bit, tcase, False);
                    }
                    else //split the interval in two
                    {
                        var fcase = CreateFromInterval1(mask >> 1, bit - 1, m, mask - 1);
                        var tcase = CreateFromInterval1(mask >> 1, bit - 1, 0, n & ~mask);
                        set = MkBvSet(bit, tcase, fcase);
                    }
                }
                intervalCache[key] = set;
                return set;
            }
        }

        BDD CreateFromInterval1(ulong mask, int bit, ulong m, ulong n)
        {
            BDD set;
            var pair = new Pair<ulong, ulong>(m, n);
            var key = new Pair<int, Pair<ulong, ulong>>(bit, pair);

            if (intervalCache.TryGetValue(key, out set))
                return set;

            else
            {

                if (mask == 1) //base case: LSB 
                {
                    if (n == 0)  //implies that m==0 
                        set = MkBvSet(bit, False, True);
                    else if (m == 1) //implies that n==1
                        set = MkBvSet(bit, True, False);
                    else //m=0 and n=1, thus full range from 0 to ((mask << 1)-1)
                        set = True;
                }
                else if (m == 0 && n == ((mask << 1) - 1)) //full interval
                {
                    set = True;
                }
                else //mask > 1, i.e., mask = 2^b for some b > 0, and not full interval
                {
                    //e.g. m = x41 = 100 0001, n = x59 = 101 1001, mask = x40 = 100 0000, ord = 6 = log2(b)
                    ulong mb = m & mask; // e.g. mb = b
                    ulong nb = n & mask; // e.g. nb = b

                    if (nb == 0) // implies that 1-branch is empty
                    {
                        var fcase = CreateFromInterval1(mask >> 1, bit - 1, m, n);
                        set = MkBvSet(bit, False, fcase);
                    }
                    else if (mb == mask) // implies that 0-branch is empty
                    {
                        var tcase = CreateFromInterval1(mask >> 1, bit - 1, m & ~mask, n & ~mask);
                        set = MkBvSet(bit, tcase, False);
                    }
                    else //split the interval in two
                    {
                        var fcase = CreateFromInterval1(mask >> 1, bit - 1, m, mask - 1);
                        var tcase = CreateFromInterval1(mask >> 1, bit - 1, 0, n & ~mask);
                        set = MkBvSet(bit, tcase, fcase);
                    }
                }
                intervalCache[key] = set;
                return set;
            }
        }

        /// <summary>
        /// Convert the set into an equivalent array of ranges and return the number of such ranges.
        /// </summary>
        public int GetRangeCount(BDD set, int maxBit)
        {
            return ToRanges64(set, maxBit).Length;
        }

        /// <summary>
        /// Convert the set into an equivalent array of uint ranges. 
        /// The ranges are nonoverlapping and ordered. 
        /// </summary>
        public Pair<uint, uint>[] ToRanges(BDD set, int maxBit)
        {
            var rc = new RangeConverter();
            return rc.ToRanges(set, maxBit);
        }

        /// <summary>
        /// Convert the set into an equivalent array of ulong ranges. 
        /// The ranges are nonoverlapping and ordered. 
        /// </summary>
        public Pair<ulong, ulong>[] ToRanges64(BDD set, int maxBit)
        {
            var rc = new RangeConverter64();
            return rc.ToRanges(set, maxBit);
        }

        #region Member generation and choice
        /// <summary>
        /// Choose a member of the set uniformly, each member is chosen with equal probability. Assumes that the set is nonempty.
        /// </summary>
        /// <param name="chooser">element chooser</param>
        /// <param name="set">given set</param>
        /// <param name="maxBit">bits above maxBit are ignored, maxBit must be at least set.Bit</param>
        /// <returns></returns>
        public uint ChooseUniformly(Chooser chooser, BDD set, int maxBit)
        {
            if (set == False)
                throw new AutomataException(AutomataExceptionKind.CharSetMustBeNonempty);
            if (maxBit < set.bit)
                throw new AutomataException(AutomataExceptionKind.InvalidArguments);

            uint res = (chooser.ChooseBV32() & ~(((0xFFFFFFFF) << maxBit) << 1));

            while (set != True)
            {
                if (set.one == False) //the bit must be set to 0
                {
                    res = res & ~((uint)1 << set.bit);
                    set = set.zero;
                }
                else if (set.zero == False) //the bit must be set to 1
                {
                    res = res | ((uint)1 << set.bit);
                    set = set.one;
                }
                else //choose the branch proportional to cardinalities of left and right sides
                {
                    int leftSize = (int)ComputeDomainSize(set.zero, set.bit - 1);
                    int rightSize = (int)ComputeDomainSize(set.one, set.bit - 1);
                    int choice = chooser.Choose(leftSize + rightSize);
                    if (choice < leftSize)
                    {
                        res = res & ~((uint)1 << set.bit); //set the bit to 0
                        set = set.zero;
                    }
                    else
                    {
                        res = res | ((uint)1 << set.bit); //set the bit to 1
                        set = set.one;
                    }
                }
            }

            return res;
        }


        /// <summary>
        /// Choose a member of the set uniformly, each member is chosen with equal probability. Assumes that the set is nonempty.
        /// </summary>
        /// <param name="chooser">element chooser</param>
        /// <param name="set">given set</param>
        /// <param name="maxBit">bits above maxBit are ignored, maxBit must be at least set.Bit</param>
        /// <returns></returns>
        public ulong ChooseUniformly64(Chooser chooser, BDD set, int maxBit)
        {
            if (set == False)
                throw new AutomataException(AutomataExceptionKind.CharSetMustBeNonempty);
            if (maxBit < set.bit)
                throw new AutomataException(AutomataExceptionKind.InvalidArguments);

            ulong res = (chooser.ChooseBV64() & ~(((0xFFFFFFFFFFFFFFFF) << maxBit) << 1));

            while (set != True)
            {
                if (set.one == False) //the bit must be set to 0
                {
                    res = res & ~(1UL << set.bit);
                    set = set.zero;
                }
                else if (set.zero == False) //the bit must be set to 1
                {
                    res = res | (1UL << set.bit);
                    set = set.one;
                }
                else //choose the branch proportional to cardinalities of left and right sides
                {
                    ulong leftSize = ComputeDomainSize(set.zero, set.bit - 1);
                    ulong rightSize = ComputeDomainSize(set.one, set.bit - 1);
                    //convert both sizes proportioanally to integers
                    while (leftSize >= 0xFFFFFFFF || rightSize >= 0xFFFFFFFF)
                    {
                        leftSize = leftSize >> 1;
                        rightSize = rightSize >> 1;
                    }
                    int choice = chooser.Choose((int)(leftSize + rightSize));
                    if (choice < (int)leftSize)
                    {
                        res = res & ~((uint)1 << set.bit); //set the bit to 0
                        set = set.zero;
                    }
                    else
                    {
                        res = res | ((uint)1 << set.bit); //set the bit to 1
                        set = set.one;
                    }
                }
            }

            return res;
        }

        /// <summary>
        /// Choose a member of the set. Assumes that the set is nonempty.
        /// </summary>
        /// <param name="chooser">element chooser</param>
        /// <param name="set">given set</param>
        /// <param name="maxBit">bits over maxBit are ignored</param>
        /// <returns></returns>
        public ulong Choose(Chooser chooser, BDD set, int maxBit)
        {
            if (set.IsEmpty)
                throw new AutomataException(AutomataExceptionKind.CharSetMustBeNonempty);
            if (maxBit < set.bit)
                throw new AutomataException(AutomataExceptionKind.InvalidArguments);

            ulong res = (chooser.ChooseBV64() & ~(((0xFFFFFFFFFFFFFFFF) << maxBit) << 1));

            while (set != True)
            {
                if (set.one == False) //the bit must be set to 0
                {
                    res = res & ~((ulong)1 << set.bit);
                    set = set.zero;
                }
                else if (set.zero == False) //the bit must be set to 1
                {
                    res = res | ((ulong)1 << set.bit);
                    set = set.one;
                }
                else //choose the branch according to the bit in res
                {
                    if ((res & ((ulong)1 << set.bit)) == 0)
                        set = set.zero;
                    else
                        set = set.one;
                }
            }

            return res;
        }

        /// <summary>
        /// Get the lexicographically maximum bitvector in the set. Assumes that the set is nonempty.
        /// </summary>
        /// <param name="set">the given nonempty set</param>
        /// <param name="maxBit">bits above maxBit are ignored, b must be at least set.Bit</param>
        /// <returns>the lexicographically largest bitvector in the set</returns>
        public ulong GetMax(BDD set, int maxBit)
        {
            if (set == False)
                throw new AutomataException(AutomataExceptionKind.CharSetMustBeNonempty);
            if (maxBit < set.bit)
                throw new AutomataException(AutomataExceptionKind.InvalidArguments);

            ulong res = ~(0xFFFFFFFFFFFFFFFF << (maxBit + 1));

            while (!set.IsFull)
            {
                if (set.one == False) //the bit must be set to 0
                {
                    res = res & ~(1UL << set.bit);
                    set = set.zero;
                }
                else
                    set = set.one;
            }
            return res;
        }

        /// <summary>
        /// Calculate the number of elements in the set. Returns 0 when set is full and maxBit is 63.
        /// </summary>
        /// <param name="set">the given set</param>
        /// <param name="maxBit">bits above maxBit are ignored</param>
        /// <returns>the cardinality of the set</returns>
        public ulong ComputeDomainSize(BDD set, int maxBit)
        {
            if (maxBit < set.bit)
                throw new AutomataException(AutomataExceptionKind.InvalidArguments);

            if (set == False)
                return 0UL;
            else if (set == True)
            {
                return ((1UL << maxBit) << 1);
            }
            else
            {
                var res = CalculateCardinality1(set);
                //sizeCache.Clear();
                if (maxBit > set.bit)
                {
                    res = (1UL << (maxBit - set.bit)) * res;
                }
                return res;
            }
        }

        Dictionary<BDD, ulong> sizeCache = new Dictionary<BDD, ulong>();
        private ulong CalculateCardinality1(BDD set)
        {
            ulong size;
            if (sizeCache.TryGetValue(set, out size))
                return size;

            ulong sizeL;
            ulong sizeR;
            if (set.Zero.IsEmpty)
            {
                sizeL = 0;
                if (set.One.IsFull)
                {
                    sizeR = ((uint)1 << set.Bit);
                }
                else
                {
                    sizeR = ((uint)1 << (((set.Bit - 1) - set.One.Bit))) * CalculateCardinality1(set.One);
                }
            }
            else if (set.Zero.IsFull)
            {
                sizeL = (1UL << set.Bit);
                if (set.One.IsEmpty)
                {
                    sizeR = 0UL;
                }
                else
                {
                    sizeR = (1UL << (((set.Bit - 1) - set.One.Bit))) * CalculateCardinality1(set.One);
                }
            }
            else
            {
                sizeL = (1UL << (((set.Bit - 1) - set.Zero.Bit))) * CalculateCardinality1(set.Zero);
                if (set.One == False)
                {
                    sizeR = 0UL;
                }
                else if (set.One == True)
                {
                    sizeR = (1UL << set.Bit);
                }
                else
                {
                    sizeR = (1UL << (((set.Bit - 1) - set.One.Bit))) * CalculateCardinality1(set.One);
                }
            }
            size = sizeL + sizeR;
            sizeCache[set] = size;
            return size;
        }

        /// <summary>
        /// Get the lexicographically minimum bitvector in the set.
        /// Assumes that the set is nonempty.
        /// </summary>
        /// <param name="set">the given nonempty set</param>
        /// <returns>the lexicographically smallest bitvector in the set</returns>
        public ulong GetMin(BDD set)
        {
            if (set.IsEmpty)
                throw new AutomataException(AutomataExceptionKind.CharSetMustBeNonempty);

            if (set.IsFull)
                return (ulong)0;

            ulong res = 0;

            while (!set.IsFull)
            {
                if (set.zero.IsEmpty) //the bit must be set to 1
                {
                    res = res | ((ulong)1 << set.bit);
                    set = set.one;
                }
                else
                    set = set.zero;
            }

            return res;
        }

        #endregion

        private class BvSetKey
        {
            int bit;
            BDD left;
            BDD right;
            public BvSetKey(int bit, BDD left, BDD right)
            {
                this.bit = bit;
                this.left = left;
                this.right = right;
            }

            public override bool Equals(object obj)
            {
                BvSetKey csk = (BvSetKey)obj;
                return (bit == csk.bit && left == csk.left && right == csk.right);
            }

            public override int GetHashCode()
            {
                return bit + (left == null ? 0 : (left.GetHashCode() << 1) + right.GetHashCode());
            }
        }

        private class BvSet_Int
        {
            BDD a;
            int n;
            internal BvSet_Int(BDD a, int n)
            {
                this.a = a;
                this.n = n;
            }
            public override int GetHashCode()
            {
                return a.GetHashCode() + n;
            }
            public override bool Equals(object obj)
            {
                BvSet_Int key = (BvSet_Int)obj;
                return key.a == a && key.n == n;
            }
        }

        public BDD MkSet(uint e)
        {
            var set = this.MkSingleton(e, 31);
            return set;
        }

        public BDD MkSet(ulong e)
        {
            var set = this.MkSingleton(e, 63);
            return set;
        }

        public ulong Choose(BDD s)
        {
            var e = this.GetMin(s);
            return e;
        }

        /// <summary>
        /// Since the BvSet is always minimal simplify only returns the set itself
        /// </summary>
        public BDD Simplify(BDD set)
        {
            return set;
        }


        public void Dispose()
        {
            ;
        }
    }

    internal class RangeConverter
    {
        Dictionary<BDD, Pair<uint, uint>[]> rangeCache = new Dictionary<BDD, Pair<uint, uint>[]>();

        internal RangeConverter()
        {
        }

        //e.g. if b = 6 and p = 2 and ranges = (in binary form) {[0000 1010, 0000 1110]} i.e. [x0A,x0E]
        //then res = {[0000 1010, 0000 1110], [0001 1010, 0001 1110], 
        //            [0010 1010, 0010 1110], [0011 1010, 0011 1110]}, 
        Pair<uint, uint>[] LiftRanges(int b, int p, Pair<uint, uint>[] ranges)
        {
            if (p == 0)
                return ranges;

            int k = b - p;
            uint maximal = ((uint)1 << k) - 1;

            Pair<uint, uint>[] res = new Pair<uint, uint>[(1 << p) * (ranges.Length)];
            int j = 0;
            for (uint i = 0; i < (1 << p); i++)
            {
                uint prefix = (i << k);
                foreach (var range in ranges)
                    res[j++] = new Pair<uint, uint>(range.First | prefix, range.Second | prefix);
            }

            //the range wraps around : [0...][...2^k-1][2^k...][...2^(k+1)-1]
            if (ranges[0].First == 0 && ranges[ranges.Length - 1].Second == maximal)
            {
                //merge consequtive ranges, we know that res has at least two elements here
                List<Pair<uint, uint>> res1 = new List<Pair<uint, uint>>();
                var from = res[0].First;
                var to = res[0].Second;
                for (int i = 1; i < res.Length; i++)
                {
                    if (to == res[i].First - 1)
                        to = res[i].Second;
                    else
                    {
                        res1.Add(new Pair<uint, uint>(from, to));
                        from = res[i].First;
                        to = res[i].Second;
                    }
                }
                res1.Add(new Pair<uint, uint>(from, to));
                res = res1.ToArray();
            }

            //CheckBug(res);
            return res;
        }

        Pair<uint, uint>[] ToRanges1(BDD set)
        {
            Pair<uint, uint>[] ranges;
            if (!rangeCache.TryGetValue(set, out ranges))
            {
                int b = set.bit;
                uint mask = (uint)1 << b;
                if (set.Zero.IsEmpty)
                {
                    #region 0-case is empty
                    if (set.One.IsFull)
                    {
                        var range = new Pair<uint, uint>(mask, (mask << 1) - 1);
                        ranges = new Pair<uint, uint>[] { range };
                    }
                    else //1-case is neither full nor empty
                    {
                        var ranges1 = LiftRanges(b, (b - set.one.bit) - 1, ToRanges1(set.One));
                        ranges = new Pair<uint, uint>[ranges1.Length];
                        for (int i = 0; i < ranges1.Length; i++)
                        {
                            ranges[i] = new Pair<uint, uint>(ranges1[i].First | mask, ranges1[i].Second | mask);
                        }
                    }
                    #endregion
                }
                else if (set.Zero.IsFull)
                {
                    #region 0-case is full
                    if (set.One.IsEmpty)
                    {
                        var range = new Pair<uint, uint>(0, mask - 1);
                        ranges = new Pair<uint, uint>[] { range };
                    }
                    else
                    {
                        var rangesR = LiftRanges(b, (b - set.one.bit) - 1, ToRanges1(set.One));
                        var range = rangesR[0];
                        if (range.First == 0)
                        {
                            ranges = new Pair<uint, uint>[rangesR.Length];
                            ranges[0] = new Pair<uint, uint>(0, range.Second | mask);
                            for (int i = 1; i < rangesR.Length; i++)
                            {
                                ranges[i] = new Pair<uint, uint>(rangesR[i].First | mask, rangesR[i].Second | mask);
                            }
                        }
                        else
                        {
                            ranges = new Pair<uint, uint>[rangesR.Length + 1];
                            ranges[0] = new Pair<uint, uint>(0, mask - 1);
                            for (int i = 0; i < rangesR.Length; i++)
                            {
                                ranges[i + 1] = new Pair<uint, uint>(rangesR[i].First | mask, rangesR[i].Second | mask);
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region 0-case is neither full nor empty
                    var rangesL = LiftRanges(b, (b - set.zero.bit) - 1, ToRanges1(set.Zero));
                    var last = rangesL[rangesL.Length - 1];

                    if (set.One.IsEmpty)
                    {
                        ranges = rangesL;
                    }

                    else if (set.One.IsFull)
                    {
                        var ranges1 = new List<Pair<uint, uint>>();
                        for (int i = 0; i < rangesL.Length - 1; i++)
                            ranges1.Add(rangesL[i]);
                        if (last.Second == (mask - 1))
                        {
                            ranges1.Add(new Pair<uint, uint>(last.First, (mask << 1) - 1));
                        }
                        else
                        {
                            ranges1.Add(last);
                            ranges1.Add(new Pair<uint, uint>(mask, (mask << 1) - 1));
                        }
                        ranges = ranges1.ToArray();
                    }
                    else //general case: neither 0-case, not 1-case is full or empty
                    {
                        var rangesR0 = ToRanges1(set.One);

                        var rangesR = LiftRanges(b, (b - set.one.bit) - 1, rangesR0);

                        var first = rangesR[0];

                        if (last.Second == (mask - 1) && first.First == 0) //merge together the last and first ranges
                        {
                            ranges = new Pair<uint, uint>[rangesL.Length + rangesR.Length - 1];
                            for (int i = 0; i < rangesL.Length - 1; i++)
                                ranges[i] = rangesL[i];
                            ranges[rangesL.Length - 1] = new Pair<uint, uint>(last.First, first.Second | mask);
                            for (int i = 1; i < rangesR.Length; i++)
                                ranges[rangesL.Length - 1 + i] = new Pair<uint, uint>(rangesR[i].First | mask, rangesR[i].Second | mask);
                        }
                        else
                        {
                            ranges = new Pair<uint, uint>[rangesL.Length + rangesR.Length];
                            for (int i = 0; i < rangesL.Length; i++)
                                ranges[i] = rangesL[i];
                            for (int i = 0; i < rangesR.Length; i++)
                                ranges[rangesL.Length + i] = new Pair<uint, uint>(rangesR[i].First | mask, rangesR[i].Second | mask);
                        }

                    }
                    #endregion
                }
                rangeCache[set] = ranges;
            }
            return ranges;
        }

        /// <summary>
        /// Convert the set into an equivalent array of ranges. 
        /// The ranges are nonoverlapping and ordered. 
        /// </summary>
        public Pair<uint, uint>[] ToRanges(BDD set, int maxBit)
        {
            if (set.IsEmpty)
                return new Pair<uint, uint>[] { };
            else if (set.IsFull)
                return new Pair<uint, uint>[] { new Pair<uint, uint>(0, ((((uint)1 << maxBit) << 1) - 1)) }; //note: maxBit could be 31
            else
                return LiftRanges(maxBit + 1, maxBit - set.Bit, ToRanges1(set));
        }
    }

    internal class RangeConverter64
    {
        Dictionary<BDD, Pair<ulong, ulong>[]> rangeCache = new Dictionary<BDD, Pair<ulong, ulong>[]>();

        internal RangeConverter64()
        {
        }

        //e.g. if b = 6 and p = 2 and ranges = (in binary form) {[0000 1010, 0000 1110]} i.e. [x0A,x0E]
        //then res = {[0000 1010, 0000 1110], [0001 1010, 0001 1110], 
        //            [0010 1010, 0010 1110], [0011 1010, 0011 1110]}, 
        Pair<ulong, ulong>[] LiftRanges(int b, int p, Pair<ulong, ulong>[] ranges)
        {
            if (p == 0)
                return ranges;

            int k = b - p;
            ulong maximal = ((ulong)1 << k) - 1;

            var res = new Pair<ulong, ulong>[(1 << p) * (ranges.Length)];
            int j = 0;
            for (ulong i = 0; i < ((ulong)(1 << p)); i++)
            {
                ulong prefix = (i << k);
                foreach (var range in ranges)
                    res[j++] = new Pair<ulong, ulong>(range.First | prefix, range.Second | prefix);
            }

            //the range wraps around : [0...][...2^k-1][2^k...][...2^(k+1)-1]
            if (ranges[0].First == 0 && ranges[ranges.Length - 1].Second == maximal)
            {
                //merge consequtive ranges, we know that res has at least two elements here
                var res1 = new List<Pair<ulong, ulong>>();
                var from = res[0].First;
                var to = res[0].Second;
                for (int i = 1; i < res.Length; i++)
                {
                    if (to == res[i].First - 1)
                        to = res[i].Second;
                    else
                    {
                        res1.Add(new Pair<ulong, ulong>(from, to));
                        from = res[i].First;
                        to = res[i].Second;
                    }
                }
                res1.Add(new Pair<ulong, ulong>(from, to));
                res = res1.ToArray();
            }
            return res;
        }

        Pair<ulong, ulong>[] ToRanges1(BDD set)
        {
            Pair<ulong, ulong>[] ranges;
            if (!rangeCache.TryGetValue(set, out ranges))
            {
                int b = set.bit;
                ulong mask = (ulong)1 << b;
                if (set.Zero.IsEmpty)
                {
                    #region 0-case is empty
                    if (set.One.IsFull)
                    {
                        var range = new Pair<ulong, ulong>(mask, (mask << 1) - 1);
                        ranges = new Pair<ulong, ulong>[] { range };
                    }
                    else //1-case is neither full nor empty
                    {
                        var ranges1 = LiftRanges(b, (b - set.one.bit) - 1, ToRanges1(set.One));
                        ranges = new Pair<ulong, ulong>[ranges1.Length];
                        for (int i = 0; i < ranges1.Length; i++)
                        {
                            ranges[i] = new Pair<ulong, ulong>(ranges1[i].First | mask, ranges1[i].Second | mask);
                        }
                    }
                    #endregion
                }
                else if (set.Zero.IsFull)
                {
                    #region 0-case is full
                    if (set.One.IsEmpty)
                    {
                        var range = new Pair<ulong, ulong>(0, mask - 1);
                        ranges = new Pair<ulong, ulong>[] { range };
                    }
                    else
                    {
                        var rangesR = LiftRanges(b, (b - set.one.bit) - 1, ToRanges1(set.One));
                        var range = rangesR[0];
                        if (range.First == 0)
                        {
                            ranges = new Pair<ulong, ulong>[rangesR.Length];
                            ranges[0] = new Pair<ulong, ulong>(0, range.Second | mask);
                            for (int i = 1; i < rangesR.Length; i++)
                            {
                                ranges[i] = new Pair<ulong, ulong>(rangesR[i].First | mask, rangesR[i].Second | mask);
                            }
                        }
                        else
                        {
                            ranges = new Pair<ulong, ulong>[rangesR.Length + 1];
                            ranges[0] = new Pair<ulong, ulong>(0, mask - 1);
                            for (int i = 0; i < rangesR.Length; i++)
                            {
                                ranges[i + 1] = new Pair<ulong, ulong>(rangesR[i].First | mask, rangesR[i].Second | mask);
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region 0-case is neither full nor empty
                    var rangesL = LiftRanges(b, (b - set.zero.bit) - 1, ToRanges1(set.Zero));
                    var last = rangesL[rangesL.Length - 1];

                    if (set.One.IsEmpty)
                    {
                        ranges = rangesL;
                    }

                    else if (set.One.IsFull)
                    {
                        var ranges1 = new List<Pair<ulong, ulong>>();
                        for (int i = 0; i < rangesL.Length - 1; i++)
                            ranges1.Add(rangesL[i]);
                        if (last.Second == (mask - 1))
                        {
                            ranges1.Add(new Pair<ulong, ulong>(last.First, (mask << 1) - 1));
                        }
                        else
                        {
                            ranges1.Add(last);
                            ranges1.Add(new Pair<ulong, ulong>(mask, (mask << 1) - 1));
                        }
                        ranges = ranges1.ToArray();
                    }
                    else //general case: neither 0-case, not 1-case is full or empty
                    {
                        var rangesR0 = ToRanges1(set.One);

                        var rangesR = LiftRanges(b, (b - set.one.bit) - 1, rangesR0);

                        var first = rangesR[0];

                        if (last.Second == (mask - 1) && first.First == 0) //merge together the last and first ranges
                        {
                            ranges = new Pair<ulong, ulong>[rangesL.Length + rangesR.Length - 1];
                            for (int i = 0; i < rangesL.Length - 1; i++)
                                ranges[i] = rangesL[i];
                            ranges[rangesL.Length - 1] = new Pair<ulong, ulong>(last.First, first.Second | mask);
                            for (int i = 1; i < rangesR.Length; i++)
                                ranges[rangesL.Length - 1 + i] = new Pair<ulong, ulong>(rangesR[i].First | mask, rangesR[i].Second | mask);
                        }
                        else
                        {
                            ranges = new Pair<ulong, ulong>[rangesL.Length + rangesR.Length];
                            for (int i = 0; i < rangesL.Length; i++)
                                ranges[i] = rangesL[i];
                            for (int i = 0; i < rangesR.Length; i++)
                                ranges[rangesL.Length + i] = new Pair<ulong, ulong>(rangesR[i].First | mask, rangesR[i].Second | mask);
                        }

                    }
                    #endregion
                }
                rangeCache[set] = ranges;
            }
            return ranges;
        }

        /// <summary>
        /// Convert the set into an equivalent array of ranges. 
        /// The ranges are nonoverlapping and ordered. 
        /// </summary>
        public Pair<ulong, ulong>[] ToRanges(BDD set, int maxBit)
        {
            if (set.IsEmpty)
                return new Pair<ulong, ulong>[] { };
            else if (set.IsFull)
                return new Pair<ulong, ulong>[] { new Pair<ulong, ulong>(0, ((((ulong)1 << maxBit) << 1) - 1)) }; //note: maxBit could be 31
            else
                return LiftRanges(maxBit + 1, maxBit - set.Bit, ToRanges1(set));
        }
    }
}
