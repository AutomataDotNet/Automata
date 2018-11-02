using System;
using System.Collections.Generic;
using BvSetPair = System.Tuple<Microsoft.Automata.BDD, Microsoft.Automata.BDD>;
using BvSet_Int = System.Tuple<Microsoft.Automata.BDD, int>;
using BvSetKey = System.Tuple<int, Microsoft.Automata.BDD, Microsoft.Automata.BDD>;
using Microsoft.Automata.BooleanAlgebras;

namespace Microsoft.Automata
{
    /// <summary>
    /// Bitwise operations over BDDs.
    /// </summary>
    public interface IBDDAlgebra : IBooleanAlgebra<BDD>
    {
        BDD MkBitTrue(int bit);
        BDD MkBitFalse(int bit);
        BDD OmitBit(BDD bdd, int bit);
        BDD ShiftRight(BDD bdd, int k = 1);
        BDD ShiftLeft(BDD bdd, int k = 1);
        Tuple<BDD,BDD>[] Partition(BDD bdd, int k);
        BDD MkSetFromRange(uint lower, uint upper, int maxbit);
    }

    /// <summary>
    /// Solver for BDDs.
    /// </summary>
    public class BDDAlgebra : IBDDAlgebra
    {
        //Dictionary<BvSet_Int, BDD> restrictCache = new Dictionary<BvSet_Int, BDD>();
        Dictionary<BvSetPair, BDD> orCache = new Dictionary<BvSetPair, BDD>();
        Dictionary<BvSetPair, BDD> andCache = new Dictionary<BvSetPair, BDD>();
        Dictionary<BDD, BDD> notCache = new Dictionary<BDD, BDD>();
        Dictionary<BDD, BDD> srCache = new Dictionary<BDD, BDD>();
        Dictionary<BvSet_Int, BDD> shift_Cache = new Dictionary<BvSet_Int, BDD>(); 
        //Chooser _chooser_ = new Chooser();

        BDD _True;
        BDD _False;

        MintermGenerator<BDD> mintermGen;

        /// <summary>
        /// Construct a solver for bitvector sets.
        /// </summary>
        public BDDAlgebra()
        {
            mintermGen = new MintermGenerator<BDD>(this);
            _True = new BDD(this, -1, null, null);
            _False = new BDD(this, -2, null, null);
        }

        //internalize the creation of all charsets so that any two charsets with same bit and children are the same pointers
        Dictionary<BvSetKey, BDD> bvsetCache = new Dictionary<BvSetKey, BDD>();

        public BDD MkBvSet(int nr, BDD one, BDD zero)
        {
            var key = new BvSetKey(nr, one, zero);
            BDD set;
            if (!bvsetCache.TryGetValue(key, out set))
            {
                set = new BDD(this, nr, one, zero);
                bvsetCache[key] = set;
            }
            return set;
        }

        #region IBooleanAlgebra members

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

            var key = new BvSetPair(a, b); 
            BDD res;
            if (orCache.TryGetValue(key, out res))
                return res;

            if (b.Ordinal > a.Ordinal)
            {
                BDD t = MkOr(a, b.One);
                BDD f = MkOr(a, b.Zero);
                res = (t == f ? t : MkBvSet(b.Ordinal, t, f));
            }
            else if (a.Ordinal > b.Ordinal)
            {
                BDD t = MkOr(a.One, b);
                BDD f = MkOr(a.Zero, b);
                res = (t == f ? t : MkBvSet(a.Ordinal, t, f));
            }
            else //a.bit == b.bit
            {
                BDD t = MkOr(a.One, b.One);
                BDD f = MkOr(a.Zero, b.Zero);
                res = (t == f ? t : MkBvSet(a.Ordinal, t, f));
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

            var key = new BvSetPair(a, b);
            BDD res;
            if (andCache.TryGetValue(key, out res))
                return res;

            if (b.Ordinal > a.Ordinal)
            {
                BDD t = MkAnd(a, b.One);
                BDD f = MkAnd(a, b.Zero);
                res = (t == f ? t : MkBvSet(b.Ordinal, t, f));
            }
            else if (a.Ordinal > b.Ordinal)
            {
                BDD t = MkAnd(a.One, b);
                BDD f = MkAnd(a.Zero, b);
                res = (t == f ? t : MkBvSet(a.Ordinal, t, f));
            }
            else //a.bit == b.bit
            {
                BDD t = MkAnd(a.One, b.One);
                BDD f = MkAnd(a.Zero, b.Zero);
                res = (t == f ? t : MkBvSet(a.Ordinal, t, f));
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

            neg = MkBvSet(a.Ordinal, MkNot(a.One), MkNot(a.Zero));
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
        /// Gets the full set.
        /// </summary>
        public BDD True
        {
            get { return _True; }
        }

        /// <summary>
        /// Gets the empty set.
        /// </summary>
        public BDD False
        {
            get { return _False; }
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
        /// Two BDDs are by construction equivalent iff they are identical.
        /// </summary>
        public bool AreEquivalent(BDD a, BDD b)
        {
            return a == b;
        }

        #endregion

        #region bit-shift operations

        /// <summary>
        /// Shift all elements k (=1 by default) bits to the right. 
        /// For example if set denotes {*0000,*1110,*1111} then 
        /// ShiftRight(set) denotes {*000,*111} where * denotes any prefix of 0's or 1's.
        /// </summary>
        public BDD ShiftRight(BDD set, int k = 1)
        {
            if (k < 0)
                throw new AutomataException(AutomataExceptionKind.InvalidArgument);
            if (set.IsLeaf || k == 0)
                return set;
            return Shift_(set, 0 - k);
        }

        /// <summary>
        /// First applies ShiftRight and then sets bit k to 0.
        /// </summary>
        public BDD ShiftRight0(BDD set, int k)
        {
            var s1 = ShiftRight(set);
            var mask = MkBitFalse(k);
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
            if (k < 0)
                throw new AutomataException(AutomataExceptionKind.InvalidArgument);
            if (set.IsLeaf || k == 0)
                return set;
            return Shift_(set, k);
        }

        BDD Shift_(BDD set, int k)
        {
            if (set.IsLeaf || k == 0)
                return set;

            var key = new BvSet_Int(set, k);

            BDD res;
            if (shift_Cache.TryGetValue(key, out res))
                return res;

            int ordinal = set.Ordinal + k;
           
            if (ordinal < 0)
                res = True;  //if k is negative
            else
            {
                BDD zero = Shift_(set.Zero, k);
                BDD one = Shift_(set.One, k);

                if (zero == one)
                    res = zero;
                else
                    res = MkBvSet(ordinal, one, zero);
            }
            shift_Cache[key] = res;
            return res;
        }

        #endregion

        #region Minterm generation

        public IEnumerable<Tuple<bool[], BDD>> GenerateMinterms(params BDD[] sets)
        {
            return mintermGen.GenerateMinterms(sets);
        }

        #endregion

        /// <summary>
        /// Creates the set that contains all elements whose k'th bit is true.
        /// </summary>
        public BDD MkBitTrue(int k)
        {
            return MkBvSet(k, True, False);
        }

        /// <summary>
        /// Creates the set that contains all elements whose k'th bit is false.
        /// </summary>
        public BDD MkBitFalse(int k)
        {
            return MkBvSet(k, False, True);
        }

        public BDD MkSetFromElements(IEnumerable<uint> elems, int maxBit)
        {
            var s = False;
            foreach (var elem in elems)
            {
                s = MkOr(s, MkSetFrom(elem, maxBit));
            }
            return s;
        }

        public BDD MkSetFromElements(IEnumerable<int> elems, int maxBit)
        {
            var s = False;
            foreach (var elem in elems)
            {
                s = MkOr(s, MkSetFrom((uint)elem, maxBit));
            }
            return s;
        }

        public BDD MkSetFromElements(IEnumerable<ulong> elems, int maxBit)
        {
            var s = False;
            foreach (var elem in elems)
            {
                s = MkOr(s, MkSetFrom(elem, maxBit));
            }
            return s;
        }

        /// <summary>
        /// Make a set containing all integers whose bits up to maxBit equal n.
        /// </summary>
        /// <param name="n">the given integer</param>
        /// <param name="maxBit">bits above maxBit are unspecified</param>
        /// <returns></returns>
        public BDD MkSetFrom(uint n, int maxBit)
        {
            var cs = MkSetFromRange(n, n, maxBit);
            return cs;
        } 

        /// <summary>
        /// Make a set containing all integers whose bits up to maxBit equal n.
        /// </summary>
        /// <param name="n">the given integer</param>
        /// <param name="maxBit">bits above maxBit are unspecified</param>
        /// <returns></returns>
        public BDD MkSetFrom(ulong n, int maxBit)
        {
            var cs = MkSetFromRange(n, n, maxBit);
            return cs;
        }

        /// <summary>
        /// Make the set containing all values greater than or equal to m and less than or equal to n when considering bits between 0 and maxBit.
        /// </summary>
        /// <param name="m">lower bound</param>
        /// <param name="n">upper bound</param>
        /// <param name="maxBit">bits above maxBit are unspecified</param>
        public BDD MkSetFromRange(uint m, uint n, int maxBit)
        {
            if (n < m)
                return False;
            uint mask = (uint)1 << maxBit;
            //filter out bits greater than maxBit
            if (maxBit < 31)
            {
                uint filter = (mask << 1) - 1;
                m = m & filter;
                n = n & filter;
            }
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

        Dictionary<Tuple<int, Tuple<ulong, ulong>>, BDD> intervalCache = new Dictionary<Tuple<int, Tuple<ulong, ulong>>, BDD>();

        BDD CreateFromInterval1(uint mask, int bit, uint m, uint n)
        {
            BDD set;
            var pair = new Tuple<ulong, ulong>((ulong)m << 32, (ulong)n);
            var key = new Tuple<int, Tuple<ulong, ulong>>(bit, pair);

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
            var pair = new Tuple<ulong, ulong>(m, n);
            var key = new Tuple<int, Tuple<ulong, ulong>>(bit, pair);

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
        /// Bits above maxBit are ignored.
        /// </summary>
        public int GetRangeCount(BDD set, int maxBit)
        {
            return ToRanges64(set, maxBit).Length;
        }

        /// <summary>
        /// Convert the set into an equivalent array of uint ranges. 
        /// Bits above maxBit are ignored.
        /// The ranges are nonoverlapping and ordered. 
        /// If limit > 0 and there are more ranges than limit then return null.
        /// </summary>
        public Tuple<uint, uint>[] ToRanges(BDD set, int maxBit, int limit = 0)
        {
            var rc = new RangeConverter();
            var ranges = rc.ToRanges(set, maxBit);
            if (limit == 0 || ranges.Length <= limit)
                return ranges;
            else
                return null;
        }

        /// <summary>
        /// Convert the set into an equivalent array of ulong ranges. 
        /// Bits above maxBit are ignored.
        /// The ranges are nonoverlapping and ordered. 
        /// </summary>
        public Tuple<ulong, ulong>[] ToRanges64(BDD set, int maxBit)
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
        /// <param name="maxBit">bits above maxBit are ignored, maxBit must be at least set.Ordinal</param>
        /// <returns></returns>
        public uint ChooseUniformly(Chooser chooser, BDD set, int maxBit)
        {
            if (set == False)
                throw new AutomataException(AutomataExceptionKind.CharSetMustBeNonempty);
            if (maxBit < set.Ordinal)
                throw new AutomataException(AutomataExceptionKind.InvalidArguments);

            uint res = (chooser.ChooseBV32() & ~(((0xFFFFFFFF) << maxBit) << 1));

            while (set != True)
            {
                if (set.One == False) //the bit must be set to 0
                {
                    res = res & ~((uint)1 << set.Ordinal);
                    set = set.Zero;
                }
                else if (set.Zero == False) //the bit must be set to 1
                {
                    res = res | ((uint)1 << set.Ordinal);
                    set = set.One;
                }
                else //choose the branch proportional to cardinalities of left and right sides
                {
                    int leftSize = (int)ComputeDomainSize(set.Zero, set.Ordinal - 1);
                    int rightSize = (int)ComputeDomainSize(set.One, set.Ordinal - 1);
                    int choice = chooser.Choose(leftSize + rightSize);
                    if (choice < leftSize)
                    {
                        res = res & ~((uint)1 << set.Ordinal); //set the bit to 0
                        set = set.Zero;
                    }
                    else
                    {
                        res = res | ((uint)1 << set.Ordinal); //set the bit to 1
                        set = set.One;
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
        /// <param name="maxBit">bits above maxBit are ignored, maxBit must be at least set.Ordinal</param>
        /// <returns></returns>
        public ulong ChooseUniformly64(Chooser chooser, BDD set, int maxBit)
        {
            if (set == False)
                throw new AutomataException(AutomataExceptionKind.CharSetMustBeNonempty);
            if (maxBit < set.Ordinal)
                throw new AutomataException(AutomataExceptionKind.InvalidArguments);

            ulong res = (chooser.ChooseBV64() & ~(((0xFFFFFFFFFFFFFFFF) << maxBit) << 1));

            while (set != True)
            {
                if (set.One == False) //the bit must be set to 0
                {
                    res = res & ~(1UL << set.Ordinal);
                    set = set.Zero;
                }
                else if (set.Zero == False) //the bit must be set to 1
                {
                    res = res | (1UL << set.Ordinal);
                    set = set.One;
                }
                else //choose the branch proportional to cardinalities of left and right sides
                {
                    ulong leftSize = ComputeDomainSize(set.Zero, set.Ordinal - 1);
                    ulong rightSize = ComputeDomainSize(set.One, set.Ordinal - 1);
                    //convert both sizes proportioanally to integers
                    while (leftSize >= 0xFFFFFFFF || rightSize >= 0xFFFFFFFF)
                    {
                        leftSize = leftSize >> 1;
                        rightSize = rightSize >> 1;
                    }
                    int choice = chooser.Choose((int)(leftSize + rightSize));
                    if (choice < (int)leftSize)
                    {
                        res = res & ~((uint)1 << set.Ordinal); //set the bit to 0
                        set = set.Zero;
                    }
                    else
                    {
                        res = res | ((uint)1 << set.Ordinal); //set the bit to 1
                        set = set.One;
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
            if (maxBit < set.Ordinal)
                throw new AutomataException(AutomataExceptionKind.InvalidArguments);

            ulong res = (chooser.ChooseBV64() & ~(((0xFFFFFFFFFFFFFFFF) << maxBit) << 1));

            while (set != True)
            {
                if (set.One == False) //the bit must be set to 0
                {
                    res = res & ~((ulong)1 << set.Ordinal);
                    set = set.Zero;
                }
                else if (set.Zero == False) //the bit must be set to 1
                {
                    res = res | ((ulong)1 << set.Ordinal);
                    set = set.One;
                }
                else //choose the branch according to the bit in res
                {
                    if ((res & ((ulong)1 << set.Ordinal)) == 0)
                        set = set.Zero;
                    else
                        set = set.One;
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
            if (maxBit < set.Ordinal)
                throw new AutomataException(AutomataExceptionKind.InvalidArguments);

            ulong res = ~(0xFFFFFFFFFFFFFFFF << (maxBit + 1));

            while (!set.IsFull)
            {
                if (set.One == False) //the bit must be set to 0
                {
                    res = res & ~(1UL << set.Ordinal);
                    set = set.Zero;
                }
                else
                    set = set.One;
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
            if (maxBit < set.Ordinal)
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
                if (maxBit > set.Ordinal)
                {
                    res = (1UL << (maxBit - set.Ordinal)) * res;
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
                    sizeR = ((uint)1 << set.Ordinal);
                }
                else
                {
                    sizeR = ((uint)1 << (((set.Ordinal - 1) - set.One.Ordinal))) * CalculateCardinality1(set.One);
                }
            }
            else if (set.Zero.IsFull)
            {
                sizeL = (1UL << set.Ordinal);
                if (set.One.IsEmpty)
                {
                    sizeR = 0UL;
                }
                else
                {
                    sizeR = (1UL << (((set.Ordinal - 1) - set.One.Ordinal))) * CalculateCardinality1(set.One);
                }
            }
            else
            {
                sizeL = (1UL << (((set.Ordinal - 1) - set.Zero.Ordinal))) * CalculateCardinality1(set.Zero);
                if (set.One == False)
                {
                    sizeR = 0UL;
                }
                else if (set.One == True)
                {
                    sizeR = (1UL << set.Ordinal);
                }
                else
                {
                    sizeR = (1UL << (((set.Ordinal - 1) - set.One.Ordinal))) * CalculateCardinality1(set.One);
                }
            }
            size = sizeL + sizeR;
            sizeCache[set] = size;
            return size;
        }

        /// <summary>
        /// Get the lexicographically minimum bitvector in the set as a ulong.
        /// Assumes that the set is nonempty and that the ordinal of the BDD is at most 63.
        /// </summary>
        /// <param name="set">the given nonempty set</param>
        /// <returns>the lexicographically smallest bitvector in the set</returns>
        public ulong GetMin(BDD set)
        {
            return set.GetMin();
        }

        #endregion

        /// <summary>
        /// Make a BDD for the concrete value i with ordinal 31
        /// </summary>
        public BDD MkSet(uint i)
        {
            var set = this.MkSetFrom(i, 31);
            return set;
        }

        /// <summary>
        /// Make a BDD for the concrete value i with ordinal 63
        /// </summary>
        public BDD MkSet(ulong i)
        {
            var set = this.MkSetFrom(i, 63);
            return set;
        }

        /// <summary>
        /// Since the BvSet is always minimal simplify only returns the set itself
        /// </summary>
        public BDD Simplify(BDD set)
        {
            return set;
        }

        /// <summary>
        /// Project away the i'th bit. Assumes that bit is nonnegative.
        /// </summary>
        public BDD OmitBit(BDD bdd, int i)
        {
            if (bdd.IsLeaf)
                return bdd;

            if (bdd.Ordinal < i)
                return bdd;
            else if (bdd.Ordinal == i)
                return MkOr(bdd.One, bdd.Zero);
            else
                return ProjectBit_(bdd, i, new Dictionary<BDD, BDD>());
        }

        /// <summary>
        /// Project away all bits greater or equalt to i. Assumes that bit is nonnegative.
        /// </summary>
        public BDD OmitBitsAbove(BDD bdd, int i)
        {
            if (bdd.IsLeaf)
                return bdd;

            if (bdd.Ordinal < i)
                return bdd;
            else
                return OmitBitsAbove(bdd.One.Or(bdd.Zero), i);
        }

        private BDD ProjectBit_(BDD bdd, int bit, Dictionary<BDD, BDD> cache)
        {
            BDD res;
            if (!cache.TryGetValue(bdd, out res))
            {
                if (bdd.IsLeaf || bdd.Ordinal < bit)
                    res = bdd;
                else if (bdd.Ordinal == bit)
                    res = MkOr(bdd.One, bdd.Zero);
                else
                {
                    var bdd1 = ProjectBit_(bdd.One, bit, cache);
                    var bdd0 = ProjectBit_(bdd.Zero, bit, cache);
                    res = MkBvSet(bdd.Ordinal, bdd1, bdd0);
                }
                cache[bdd] = res;
            }
            return res;
        }


        public void Dispose()
        {
            ;
        }

        public bool IsExtensional
        {
            get { return true; }
        }


        public BDD MkSymmetricDifference(BDD p1, BDD p2)
        {
            return MkOr(MkAnd(p1, MkNot(p2)), MkAnd(p2, MkNot(p1)));
        }

        public bool CheckImplication(BDD lhs, BDD rhs)
        {
            return MkAnd(lhs, MkNot(rhs)).IsEmpty;
        }

        public bool IsAtomic
        {
            get { return false; }
        }

        public BDD GetAtom(BDD psi)
        {
            throw new AutomataException(AutomataExceptionKind.BooleanAlgebraIsNotAtomic);
        }


        public bool EvaluateAtom(BDD atom, BDD psi)
        {
            throw new AutomataException(AutomataExceptionKind.BooleanAlgebraIsNotAtomic);
        }

        public string PrettyPrintTerminal(int id)
        {
            if (_True.Ordinal == id)
                return "true";
            else if (_False.Ordinal == id)
                return "false";
            else
                throw new AutomataException(AutomataExceptionKind.UnexpectedMTBDDTerminal);
        }

        #region Serialializing and deserializing BDDs from dags encoded by ulongs arrays
        /// <summary>
        /// Serialize a BDD in a flat ulong array.
        /// The BDD may have at most 2^16 bits and 2^24 nodes.
        /// BDD.False is represented by ulong[]{0}.
        /// BDD.True is represented by ulong[]{0,0}.
        /// Element at index 0 is the false node,
        /// element at index 1 is the true node,
        /// and entry at index i>1 is node i and has the structure:
        /// (ordinal &lt;&lt; 48) | (trueNode &lt;&lt; 24) | falseNode.
        /// The root of the BDD (when different from True and False) is node 2.
        /// </summary>
        public ulong[] Serialize(BDD bdd)
        {
            if (bdd.IsEmpty)
                return new ulong[] { 0 };
            if (bdd.IsFull)
                return new ulong[] { 0, 0 };
            if (bdd.IsLeaf)
                throw new AutomataException(AutomataExceptionKind.MTBDDsNotSupportedForThisOperation);

            int nrOfNodes = bdd.CountNodes();

            if (nrOfNodes > (1 << 24))
                throw new AutomataException(AutomataExceptionKind.BDDSerializationNodeLimitViolation);

            if (bdd.Ordinal >= (1 << 16))
                throw new AutomataException(AutomataExceptionKind.BDDSerializationBitLimitViolation);

            ulong[] res = new ulong[nrOfNodes];

            //here we know that bdd is neither empty nor full
            var done = new Dictionary<BDD, int>();
            done[False] = 0;
            done[True] = 1;

            Stack<BDD> stack = new Stack<BDD>();
            stack.Push(bdd);
            done[bdd] = 2;

            int doneCount = 3;

            while (stack.Count > 0)
            {
                BDD b = stack.Pop();
                if (!done.ContainsKey(b.One))
                {
                    done[b.One] = (doneCount++);
                    stack.Push(b.One);
                }
                if (!done.ContainsKey(b.Zero))
                {
                    done[b.Zero] = (doneCount++);
                    stack.Push(b.Zero);
                }
                int bId = done[b];
                int fId = done[b.Zero];
                int tId = done[b.One];

                res[bId] = (((ulong)b.Ordinal) << 48) | (((ulong)tId) << 24) | ((uint)fId);
            }
            return res;
        }

        /// <summary>
        /// Recreates a BDD from a ulong array that has been created using Serialize.
        /// </summary>
        public BDD Deserialize(ulong[] arcs)
        {
            if (arcs.Length == 1)
                return False;
            if (arcs.Length == 2)
                return True;

            //organized by order
            var levelsMap = new Dictionary<int, List<int>>();
            List<int> levels = new List<int>();

            BDD[] bddMap = new BDD[arcs.Length];
            bddMap[0] = False;
            bddMap[1] = True;

            for (int i = 2; i < arcs.Length; i++)
            {
                ulong ordinal = (arcs[i] >> 48);
                int x = (int)ordinal;
                List<int> x_list;
                if (!levelsMap.TryGetValue(x, out x_list))
                {
                    x_list = new List<int>();
                    levelsMap[x] = x_list;
                    levels.Add(x);
                }
                x_list.Add(i);
            }

            //create the BDD nodes according to the level order
            //strating with the lowest ordinal
            //this is to ensure proper internalization
            levels.Sort();

            foreach (int x in levels)
            {
                foreach (int i in levelsMap[x])
                {
                    ulong oneU = (arcs[i] >> 24) & 0xFFFFFF;
                    int one = (int)oneU;
                    ulong zeroU = arcs[i] & 0xFFFFFF;
                    int zero = (int)zeroU;
                    if (one >= bddMap.Length || zero >= bddMap.Length)
                        throw new AutomataException(AutomataExceptionKind.BDDDeserializationError);
                    var oneBranch = bddMap[one];
                    var zeroBranch = bddMap[zero];
                    var bdd = MkBvSet(x, oneBranch, zeroBranch);
                    bddMap[i] = bdd;
                    if (bdd.Ordinal <= bdd.One.Ordinal || bdd.Ordinal <= bdd.Zero.Ordinal)
                        throw new AutomataException(AutomataExceptionKind.BDDDeserializationError);
                }
            }

            return bddMap[2];
        }
        #endregion


        #region partitioning into a disjunction of products

        public Tuple<BDD,BDD>[] Partition(BDD set, int ordinal)
        {
            if (set.IsEmpty)
            {
                return new Tuple<BDD, BDD>[] { };
            }
            else
            {
                BDD[] lower;
                if (set.IsLeaf || set.Ordinal <= ordinal)
                    lower = new BDD[] { set };
                else
                {
                    List<BDD> lower_list = new List<BDD>();
                    HashSet<BDD> done = new HashSet<BDD>();
                    SimpleStack<BDD> stack = new SimpleStack<BDD>();
                    stack.Push(set);
                    done.Add(set);
                    while (stack.IsNonempty)
                    {
                        BDD bdd = stack.Pop();
                        if (bdd.IsLeaf || bdd.Ordinal <= ordinal)
                        {
                            if (!bdd.IsEmpty)
                                lower_list.Add(bdd);
                        }
                        else
                        {
                            if (done.Add(bdd.Zero))
                                stack.Push(bdd.Zero);
                            if (done.Add(bdd.One))
                                stack.Push(bdd.One);
                        }
                    }
                    lower = lower_list.ToArray();
                }
                var pairs = Array.ConvertAll(lower, x => new Tuple<BDD,BDD>(x, ReplaceNode(set, x, ordinal)));
                return pairs;
            }
        }

        BDD ReplaceNode(BDD bdd, BDD node, int ordinal)
        {
            return ReplaceNode_(new Dictionary<BDD, BDD>(), bdd, node, ordinal);
        }

        BDD ReplaceNode_(Dictionary<BDD, BDD> ReplaceNode_Cache, BDD bdd, BDD node, int ordinal)
        {
            BDD res;
            if (ReplaceNode_Cache.TryGetValue(bdd, out res))
                return res;
            else if (bdd == node)
            {
                res = this.True;
            }
            else
            {
                if (bdd.IsLeaf || bdd.Ordinal <= ordinal)
                {
                    res = this.False;
                }
                else
                {
                    var zero = ReplaceNode_(ReplaceNode_Cache, bdd.Zero, node, ordinal);
                    var one = ReplaceNode_(ReplaceNode_Cache, bdd.One, node, ordinal);
                    if (one == zero)
                    {
                        res = one;
                    }
                    else
                    {
                        res = this.MkBvSet(bdd.Ordinal, one, zero);
                    }
                }
            }
            ReplaceNode_Cache[bdd] = res;
            return res;
        }

        #endregion
    }

    /// <summary>
    /// Solver for multi-terminal BDDs with leaf ordinals that map to predicates from a Boolean algebra over T
    /// </summary>
    public class BDDAlgebra<T> : IBDDAlgebra, ICartesianAlgebraBDD<T>
    {
        BDD<T> _True;
        BDD<T> _False;
        MintermGenerator<BDD> mintermGen;
        public readonly IBooleanAlgebra<T> LeafAlgebra;
        Func<T,T> GetId;

        public BDD True
        {
            get { return _True; }
        }

        public BDD False
        {
            get { return _False; }
        }

        public BDDAlgebra(IBooleanAlgebra<T> leafAlgebra)
        {
            this.LeafAlgebra = leafAlgebra;
            if (leafAlgebra.IsExtensional)
                this.GetId = (psi => psi);
            else if (leafAlgebra.IsAtomic)
                this.GetId = new PredicateTrie<T>(leafAlgebra).Search;
            else 
                this.GetId = new PredicateIdMapper<T>(leafAlgebra).GetId;
            mintermGen = new MintermGenerator<BDD>(this);
            //terminal2id[leafAlgebra.True] = -1;
            //terminal2id[leafAlgebra.False] = -2;
            //id2terminal[-1] = leafAlgebra.True;
            //id2terminal[-2] = leafAlgebra.False;
            _True = new BDD<T>(this, -1, leafAlgebra.True);
            _False = new BDD<T>(this, -2, leafAlgebra.False);
            leafCache[leafAlgebra.True] = _True;
            leafCache[leafAlgebra.False] = _False;
            //id2leaf[-1] = _True;
            //id2leaf[-2] = _False;
        }

        Dictionary<Tuple<BDD, BDD>, BDD> andCache = new Dictionary<Tuple<BDD, BDD>, BDD>();
        Dictionary<Tuple<BDD, BDD>, BDD> orCache = new Dictionary<Tuple<BDD, BDD>, BDD>();
        Dictionary<BDD, BDD> notCache = new Dictionary<BDD, BDD>();
        Dictionary<Tuple<int, BDD, BDD>, BDD> nodeCache = new Dictionary<Tuple<int, BDD, BDD>, BDD>();
        Dictionary<T, BDD<T>> leafCache = new Dictionary<T, BDD<T>>();

        //Dictionary<T, int> terminal2id = new Dictionary<T, int>();
        //Dictionary<int, BDD<T>> id2leaf = new Dictionary<int, BDD<T>>();
        //Dictionary<int, T> id2terminal = new Dictionary<int, T>();
        int nextLeafId = -3;

        internal BDD MkNode(int bit, BDD one, BDD zero)
        {
            var key = new Tuple<int, BDD, BDD>(bit, one, zero);
            BDD bdd;
            if (!nodeCache.TryGetValue(key, out bdd))
            {
                bdd = new BDD<T>(this, bit, one, zero);
                nodeCache[key] = bdd;
            }
            return bdd;
        }

        public BDD<T> MkLeaf(T pred)
        {
            BDD<T> leaf;
            var repr = GetId(pred);
            if (!leafCache.TryGetValue(repr, out leaf))
            {
                var leaf_id = this.nextLeafId--;
                leaf = new BDD<T>(this, leaf_id, repr);
                leafCache[pred] = leaf;
            }
            return leaf;
        }

        #region IBoolAlg members

        /// <summary>
        /// Make the union of a and b
        /// </summary>
        public BDD MkOr(BDD a, BDD b)
        {
            if (a == _False)
                return b;
            if (b == _False)
                return a;
            if (a == _True || b == _True)
                return _True;
            if (a == b)
                return a;

            var key = new Tuple<BDD, BDD>(a, b);
            BDD res;
            if (orCache.TryGetValue(key, out res))
                return res;

            if (a.IsLeaf && b.IsLeaf)
            {
                T a_leaf = ((BDD<T>)a).Leaf;
                T b_leaf = ((BDD<T>)b).Leaf;
                res = MkLeaf(LeafAlgebra.MkOr(a_leaf,b_leaf));
            }
            else if (a.IsLeaf || b.Ordinal > a.Ordinal)
            {
                BDD t = MkOr(a, b.One);
                BDD f = MkOr(a, b.Zero);
                res = (t == f ? t : MkNode(b.Ordinal, t, f));
            }
            else if (b.IsLeaf || a.Ordinal > b.Ordinal)
            {
                BDD t = MkOr(a.One, b);
                BDD f = MkOr(a.Zero, b);
                res = (t == f ? t : MkNode(a.Ordinal, t, f));
            }
            else //a.Ordinal == b.Ordinal and neither is leaf
            {
                BDD t = MkOr(a.One, b.One);
                BDD f = MkOr(a.Zero, b.Zero);
                res = (t == f ? t : MkNode(a.Ordinal, t, f));
            }

            orCache[key] = res;
            return res;
        }

        /// <summary>
        /// Make the intersection of a and b
        /// </summary>
        public BDD MkAnd(BDD a, BDD b)
        {
            if (a == _True)
                return b;
            if (b == _True)
                return a;
            if (a == _False || b == _False)
                return _False;
            if (a == b)
                return a;

            var key = new Tuple<BDD,BDD>(a, b);
            BDD res;
            if (andCache.TryGetValue(key, out res))
                return res;

            if (a.IsLeaf && b.IsLeaf)
            {
                T a_leaf = ((BDD<T>)a).Leaf;
                T b_leaf = ((BDD<T>)b).Leaf;
                res = MkLeaf(LeafAlgebra.MkAnd(a_leaf, b_leaf));
            }

            else if (a.IsLeaf || b.Ordinal > a.Ordinal)
            {
                BDD t = MkAnd(a, b.One);
                BDD f = MkAnd(a, b.Zero);
                res = (t == f ? t : MkNode(b.Ordinal, t, f));
            }
            else if (b.IsLeaf || a.Ordinal > b.Ordinal)
            {
                BDD t = MkAnd(a.One, b);
                BDD f = MkAnd(a.Zero, b);
                res = (t == f ? t : MkNode(a.Ordinal, t, f));
            }
            else //a.Ordinal == b.Ordinal and neither is leaf
            {
                BDD t = MkAnd(a.One, b.One);
                BDD f = MkAnd(a.Zero, b.Zero);
                res = (t == f ? t : MkNode(a.Ordinal, t, f));
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
            if (a == _False)
                return True;
            if (a == _True)
                return False;

            BDD neg;
            if (notCache.TryGetValue(a, out neg))
                return neg;

            if (a.IsLeaf)
            {
                T a_leaf = ((BDD<T>)a).Leaf;
                neg = MkLeaf(LeafAlgebra.MkNot(a_leaf));
            }
            else
                neg = MkNode(a.Ordinal, MkNot(a.One), MkNot(a.Zero));
            notCache[a] = neg;
            return neg;
        }

        /// <summary>
        /// Intersect all sets in the enumeration
        /// </summary>
        public BDD MkAnd(IEnumerable<BDD> sets)
        {
            BDD res = _True;
            foreach (BDD bdd in sets)
                res = MkAnd(res, bdd);
            return res;
        }

        /// <summary>
        /// Intersect all the sets.
        /// </summary>
        public BDD MkAnd(params BDD[] sets)
        {
            BDD res = _True;
            for (int i = 0; i < sets.Length; i++ )
                res = MkAnd(res, sets[i]);
            return res;
        }

        /// <summary>
        /// Take the union of all sets in the enumeration
        /// </summary>
        public BDD MkOr(IEnumerable<BDD> sets)
        {
            BDD res = _False;
            foreach (BDD bdd in sets)
                res = MkOr(res, bdd);
            return res;
        }

        /// <summary>
        /// Returns true if bdd is nonempty.
        /// </summary>
        public bool IsSatisfiable(BDD bdd)
        {
            return bdd != _False;
        }

        /// <summary>
        /// Two BDDs are by construction equivalent iff they are identical.
        /// </summary>
        public bool AreEquivalent(BDD a, BDD b)
        {
            return a == b;
        }

        #endregion

        #region Minterm generation

        public IEnumerable<Tuple<bool[], BDD>> GenerateMinterms(params BDD[] sets)
        {
            return mintermGen.GenerateMinterms(sets);
        }

        #endregion

        /// <summary>
        /// Creates the bdd that contains all elements whose k'th bit is true.
        /// </summary>
        public BDD MkBitTrue(int k)
        {
            return MkNode(k, _True, _False);
        }

        /// <summary>
        /// Creates the set that contains all elements whose k'th bit is false.
        /// </summary>
        public BDD MkBitFalse(int k)
        {
            return MkNode(k, _False, _True);
        }

        /// <summary>
        /// Identity function
        /// </summary>
        public BDD Simplify(BDD set)
        {
            return set;
        }

        /// <summary>
        /// Project away the i'th bit. Assumes that bit is nonnegative.
        /// </summary>
        public BDD OmitBit(BDD bdd, int bit)
        {
            if (bdd.IsLeaf | bdd.Ordinal < bit)
                return bdd;
            else if (bdd.Ordinal == bit)
                return MkOr(bdd.One, bdd.Zero);
            else
                return Omit_(bdd, bit, new Dictionary<BDD, BDD>());
        }

        public T ProjectLeaves(BDD<T> bdd)
        {
            if (bdd.IsLeaf)
                return bdd.Leaf;
            else
                return ProjectLeaves((BDD<T>)MkOr(bdd.One, bdd.Zero));
        }

        private BDD Omit_(BDD bdd, int bit, Dictionary<BDD, BDD> cache)
        {
            BDD res;
            if (!cache.TryGetValue(bdd, out res))
            {
                if (bdd.IsLeaf | bdd.Ordinal < bit)
                    res = bdd;
                else if (bdd.Ordinal == bit)
                    res = MkOr(bdd.One, bdd.Zero);
                else
                {
                    var bdd1 = Omit_(bdd.One, bit, cache);
                    var bdd0 = Omit_(bdd.Zero, bit, cache);
                    res = MkNode(bdd.Ordinal, bdd1, bdd0);
                }
                cache[bdd] = res;
            }
            return res;
        }


        public void Dispose()
        {
            ;
        }

        public bool IsExtensional
        {
            get { return true; }
        }

        /// <summary>
        /// Returns p-q.
        /// </summary>
        public BDD MkDifference(BDD p, BDD q)
        {
            return MkAnd(p, MkNot(q));
        }

        /// <summary>
        /// Returns (p-q)|(q-p).
        /// </summary>
        public BDD MkSymmetricDifference(BDD p, BDD q)
        {
            return MkOr(MkAnd(p, MkNot(q)), MkAnd(q, MkNot(p))); 
        }

        public bool CheckImplication(BDD lhs, BDD rhs)
        {
            return MkAnd(lhs, MkNot(rhs)) == _False;
        }

        public bool IsAtomic
        {
            get { return false; }
        }

        public BDD GetAtom(BDD psi)
        {
            throw new AutomataException(AutomataExceptionKind.BooleanAlgebraIsNotAtomic);
        }


        public bool EvaluateAtom(BDD atom, BDD psi)
        {
            throw new AutomataException(AutomataExceptionKind.BooleanAlgebraIsNotAtomic);
        }

        IBDDAlgebra ICartesianAlgebraBDD<T>.BDDAlgebra
        {
            get { return this; }
        }

        public IMonadicPredicate<BDD, T> MkCartesianProduct(BDD first, T second)
        {
            return (BDD<T>)MkAnd(first, MkLeaf(second));
        }

        public IBooleanAlgebra<T> Second
        {
            get { return LeafAlgebra; }
        }

        #region IBooleanAlgebra<ISumOfProducts<BDD, T>>

        public IEnumerable<Tuple<bool[], IMonadicPredicate<BDD, T>>> GenerateMinterms(params IMonadicPredicate<BDD, T>[] constraints)
        {
            BDD[] bdds = Array.ConvertAll(constraints, c => (BDD)c);
            foreach (var mt in GenerateMinterms(bdds))
                yield return new Tuple<bool[], IMonadicPredicate<BDD, T>>(mt.Item1, (BDD<T>)mt.Item2);
        }

        IMonadicPredicate<BDD, T> IBooleanAlgebraPositive<IMonadicPredicate<BDD, T>>.True
        {
            get { return (BDD<T>)_True; }
        }

        IMonadicPredicate<BDD, T> IBooleanAlgebraPositive<IMonadicPredicate<BDD, T>>.False
        {
            get { return (BDD<T>)_False; }
        }

        public IMonadicPredicate<BDD, T> MkOr(IEnumerable<IMonadicPredicate<BDD, T>> predicates)
        {
            BDD res = False;
            foreach (var p in predicates)
            {
                res = MkOr(res, (BDD)p);
            }
            return (BDD<T>)res;
        }

        public IMonadicPredicate<BDD, T> MkAnd(IEnumerable<IMonadicPredicate<BDD, T>> predicates)
        {
            BDD res = True;
            foreach (var p in predicates)
            {
                res = MkAnd(res, (BDD)p);
            }
            return (BDD<T>)res;
        }

        public IMonadicPredicate<BDD, T> MkAnd(params IMonadicPredicate<BDD, T>[] predicates)
        {
            BDD res = True;
            foreach (var p in predicates)
            {
                res = MkAnd(res, (BDD)p);
            }
            return (BDD<T>)res;
        }

        public IMonadicPredicate<BDD, T> MkNot(IMonadicPredicate<BDD, T> predicate)
        {
            return (BDD<T>)MkNot((BDD)predicate);
        }

        public bool AreEquivalent(IMonadicPredicate<BDD, T> predicate1, IMonadicPredicate<BDD, T> predicate2)
        {
            return AreEquivalent((BDD)predicate1, (BDD)predicate2);
        }

        public IMonadicPredicate<BDD, T> MkSymmetricDifference(IMonadicPredicate<BDD, T> p1, IMonadicPredicate<BDD, T> p2)
        {
            return (BDD<T>)MkSymmetricDifference((BDD)p1, (BDD)p2);
        }

        public bool CheckImplication(IMonadicPredicate<BDD, T> lhs, IMonadicPredicate<BDD, T> rhs)
        {
            return CheckImplication((BDD)lhs, (BDD)rhs);
        }

        public IMonadicPredicate<BDD, T> Simplify(IMonadicPredicate<BDD, T> predicate)
        {
            return predicate;
        }

        public IMonadicPredicate<BDD, T> GetAtom(IMonadicPredicate<BDD, T> psi)
        {
            throw new AutomataException(AutomataExceptionKind.BooleanAlgebraIsNotAtomic);
        }

        public bool EvaluateAtom(IMonadicPredicate<BDD, T> atom, IMonadicPredicate<BDD, T> psi)
        {
            throw new AutomataException(AutomataExceptionKind.BooleanAlgebraIsNotAtomic);
        }

        public IMonadicPredicate<BDD, T> MkAnd(IMonadicPredicate<BDD, T> p1, IMonadicPredicate<BDD, T> p2)
        {
            return (BDD<T>)MkAnd((BDD)p1, (BDD)p2);
        }

        public IMonadicPredicate<BDD, T> MkOr(IMonadicPredicate<BDD, T> p1, IMonadicPredicate<BDD, T> p2)
        {
            return (BDD<T>)MkOr((BDD)p1, (BDD)p2); 
        }

        public bool IsSatisfiable(IMonadicPredicate<BDD, T> predicate)
        {
            return IsSatisfiable((BDD)predicate);
        }

        #endregion

        public IMonadicPredicate<BDD, T> Omit(int bit, IMonadicPredicate<BDD, T> pred)
        {
            return (BDD<T>)OmitBit((BDD)pred, bit);
        }


        public BDD ShiftRight(BDD bdd, int k = 1)
        {
            throw new NotImplementedException();
        }

        public BDD ShiftLeft(BDD bdd, int k = 1)
        {
            throw new NotImplementedException();
        }


        public IMonadicPredicate<BDD, T> MkDiff(IMonadicPredicate<BDD, T> predicate1, IMonadicPredicate<BDD, T> predicate2)
        {
            return MkAnd(predicate1, MkNot(predicate2));
        }

        public BvSetPair[] Partition(BDD bdd, int k)
        {
            throw new NotImplementedException();
        }

        public BDD MkSetFromRange(uint lower, uint upper, int maxbit)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// For a given Boolean algebra maps all predicates to unique representatives.
    /// </summary>
    internal class PredicateIdMapper<T>
    {
        Dictionary<T, T> predMap = new Dictionary<T, T>();
        List<T> preds = new List<T>();
        IBooleanAlgebra<T> algebra;

        internal PredicateIdMapper(IBooleanAlgebra<T> algebra)
        {
            this.algebra = algebra;
        }

        /// <summary>
        /// For all p: p is equivalent to GetId(p).
        /// For all p and q: if p is equivalent to q then GetId(p)==GetId(q).
        /// </summary>
        /// <param name="p">given predicate</param>
        public T GetId(T pred)
        {
            T rep;
            if (predMap.TryGetValue(pred, out rep))
                return rep;
            else
            {
                foreach (var p in preds)
                {
                    if (algebra.AreEquivalent(pred, p))
                    {
                        predMap[pred] = p;
                        return p;
                    }
                }
                predMap[pred] = pred;
                preds.Add(pred);
                return pred;
            }
        }
    }

    internal class RangeConverter
    {
        Dictionary<BDD, Tuple<uint, uint>[]> rangeCache = new Dictionary<BDD, Tuple<uint, uint>[]>();

        internal RangeConverter()
        {
        }

        //e.g. if b = 6 and p = 2 and ranges = (in binary form) {[0000 1010, 0000 1110]} i.e. [x0A,x0E]
        //then res = {[0000 1010, 0000 1110], [0001 1010, 0001 1110], 
        //            [0010 1010, 0010 1110], [0011 1010, 0011 1110]}, 
        Tuple<uint, uint>[] LiftRanges(int b, int p, Tuple<uint, uint>[] ranges)
        {
            if (p == 0)
                return ranges;

            int k = b - p;
            uint maximal = ((uint)1 << k) - 1;

            Tuple<uint, uint>[] res = new Tuple<uint, uint>[(1 << p) * (ranges.Length)];
            int j = 0;
            for (uint i = 0; i < (1 << p); i++)
            {
                uint prefix = (i << k);
                foreach (var range in ranges)
                    res[j++] = new Tuple<uint, uint>(range.Item1 | prefix, range.Item2 | prefix);
            }

            //the range wraps around : [0...][...2^k-1][2^k...][...2^(k+1)-1]
            if (ranges[0].Item1 == 0 && ranges[ranges.Length - 1].Item2 == maximal)
            {
                //merge consequtive ranges, we know that res has at least two elements here
                List<Tuple<uint, uint>> res1 = new List<Tuple<uint, uint>>();
                var from = res[0].Item1;
                var to = res[0].Item2;
                for (int i = 1; i < res.Length; i++)
                {
                    if (to == res[i].Item1 - 1)
                        to = res[i].Item2;
                    else
                    {
                        res1.Add(new Tuple<uint, uint>(from, to));
                        from = res[i].Item1;
                        to = res[i].Item2;
                    }
                }
                res1.Add(new Tuple<uint, uint>(from, to));
                res = res1.ToArray();
            }

            //CheckBug(res);
            return res;
        }

        Tuple<uint, uint>[] ToRanges1(BDD set)
        {
            Tuple<uint, uint>[] ranges;
            if (!rangeCache.TryGetValue(set, out ranges))
            {
                int b = set.Ordinal;
                uint mask = (uint)1 << b;
                if (set.Zero.IsEmpty)
                {
                    #region 0-case is empty
                    if (set.One.IsFull)
                    {
                        var range = new Tuple<uint, uint>(mask, (mask << 1) - 1);
                        ranges = new Tuple<uint, uint>[] { range };
                    }
                    else //1-case is neither full nor empty
                    {
                        var ranges1 = LiftRanges(b, (b - set.One.Ordinal) - 1, ToRanges1(set.One));
                        ranges = new Tuple<uint, uint>[ranges1.Length];
                        for (int i = 0; i < ranges1.Length; i++)
                        {
                            ranges[i] = new Tuple<uint, uint>(ranges1[i].Item1 | mask, ranges1[i].Item2 | mask);
                        }
                    }
                    #endregion
                }
                else if (set.Zero.IsFull)
                {
                    #region 0-case is full
                    if (set.One.IsEmpty)
                    {
                        var range = new Tuple<uint, uint>(0, mask - 1);
                        ranges = new Tuple<uint, uint>[] { range };
                    }
                    else
                    {
                        var rangesR = LiftRanges(b, (b - set.One.Ordinal) - 1, ToRanges1(set.One));
                        var range = rangesR[0];
                        if (range.Item1 == 0)
                        {
                            ranges = new Tuple<uint, uint>[rangesR.Length];
                            ranges[0] = new Tuple<uint, uint>(0, range.Item2 | mask);
                            for (int i = 1; i < rangesR.Length; i++)
                            {
                                ranges[i] = new Tuple<uint, uint>(rangesR[i].Item1 | mask, rangesR[i].Item2 | mask);
                            }
                        }
                        else
                        {
                            ranges = new Tuple<uint, uint>[rangesR.Length + 1];
                            ranges[0] = new Tuple<uint, uint>(0, mask - 1);
                            for (int i = 0; i < rangesR.Length; i++)
                            {
                                ranges[i + 1] = new Tuple<uint, uint>(rangesR[i].Item1 | mask, rangesR[i].Item2 | mask);
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region 0-case is neither full nor empty
                    var rangesL = LiftRanges(b, (b - set.Zero.Ordinal) - 1, ToRanges1(set.Zero));
                    var last = rangesL[rangesL.Length - 1];

                    if (set.One.IsEmpty)
                    {
                        ranges = rangesL;
                    }

                    else if (set.One.IsFull)
                    {
                        var ranges1 = new List<Tuple<uint, uint>>();
                        for (int i = 0; i < rangesL.Length - 1; i++)
                            ranges1.Add(rangesL[i]);
                        if (last.Item2 == (mask - 1))
                        {
                            ranges1.Add(new Tuple<uint, uint>(last.Item1, (mask << 1) - 1));
                        }
                        else
                        {
                            ranges1.Add(last);
                            ranges1.Add(new Tuple<uint, uint>(mask, (mask << 1) - 1));
                        }
                        ranges = ranges1.ToArray();
                    }
                    else //general case: neither 0-case, not 1-case is full or empty
                    {
                        var rangesR0 = ToRanges1(set.One);

                        var rangesR = LiftRanges(b, (b - set.One.Ordinal) - 1, rangesR0);

                        var first = rangesR[0];

                        if (last.Item2 == (mask - 1) && first.Item1 == 0) //merge together the last and first ranges
                        {
                            ranges = new Tuple<uint, uint>[rangesL.Length + rangesR.Length - 1];
                            for (int i = 0; i < rangesL.Length - 1; i++)
                                ranges[i] = rangesL[i];
                            ranges[rangesL.Length - 1] = new Tuple<uint, uint>(last.Item1, first.Item2 | mask);
                            for (int i = 1; i < rangesR.Length; i++)
                                ranges[rangesL.Length - 1 + i] = new Tuple<uint, uint>(rangesR[i].Item1 | mask, rangesR[i].Item2 | mask);
                        }
                        else
                        {
                            ranges = new Tuple<uint, uint>[rangesL.Length + rangesR.Length];
                            for (int i = 0; i < rangesL.Length; i++)
                                ranges[i] = rangesL[i];
                            for (int i = 0; i < rangesR.Length; i++)
                                ranges[rangesL.Length + i] = new Tuple<uint, uint>(rangesR[i].Item1 | mask, rangesR[i].Item2 | mask);
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
        public Tuple<uint, uint>[] ToRanges(BDD set, int maxBit)
        {
            if (set.IsEmpty)
                return new Tuple<uint, uint>[] { };
            else if (set.IsFull)
                return new Tuple<uint, uint>[] { new Tuple<uint, uint>(0, ((((uint)1 << maxBit) << 1) - 1)) }; //note: maxBit could be 31
            else
                return LiftRanges(maxBit + 1, maxBit - set.Ordinal, ToRanges1(set));
        }
    }

    internal class RangeConverter64
    {
        Dictionary<BDD, Tuple<ulong, ulong>[]> rangeCache = new Dictionary<BDD, Tuple<ulong, ulong>[]>();

        internal RangeConverter64()
        {
        }

        //e.g. if b = 6 and p = 2 and ranges = (in binary form) {[0000 1010, 0000 1110]} i.e. [x0A,x0E]
        //then res = {[0000 1010, 0000 1110], [0001 1010, 0001 1110], 
        //            [0010 1010, 0010 1110], [0011 1010, 0011 1110]}, 
        Tuple<ulong, ulong>[] LiftRanges(int b, int p, Tuple<ulong, ulong>[] ranges)
        {
            if (p == 0)
                return ranges;

            int k = b - p;
            ulong maximal = ((ulong)1 << k) - 1;

            var res = new Tuple<ulong, ulong>[(1 << p) * (ranges.Length)];
            int j = 0;
            for (ulong i = 0; i < ((ulong)(1 << p)); i++)
            {
                ulong prefix = (i << k);
                foreach (var range in ranges)
                    res[j++] = new Tuple<ulong, ulong>(range.Item1 | prefix, range.Item2 | prefix);
            }

            //the range wraps around : [0...][...2^k-1][2^k...][...2^(k+1)-1]
            if (ranges[0].Item1 == 0 && ranges[ranges.Length - 1].Item2 == maximal)
            {
                //merge consequtive ranges, we know that res has at least two elements here
                var res1 = new List<Tuple<ulong, ulong>>();
                var from = res[0].Item1;
                var to = res[0].Item2;
                for (int i = 1; i < res.Length; i++)
                {
                    if (to == res[i].Item1 - 1)
                        to = res[i].Item2;
                    else
                    {
                        res1.Add(new Tuple<ulong, ulong>(from, to));
                        from = res[i].Item1;
                        to = res[i].Item2;
                    }
                }
                res1.Add(new Tuple<ulong, ulong>(from, to));
                res = res1.ToArray();
            }
            return res;
        }

        Tuple<ulong, ulong>[] ToRanges1(BDD set)
        {
            Tuple<ulong, ulong>[] ranges;
            if (!rangeCache.TryGetValue(set, out ranges))
            {
                int b = set.Ordinal;
                ulong mask = (ulong)1 << b;
                if (set.Zero.IsEmpty)
                {
                    #region 0-case is empty
                    if (set.One.IsFull)
                    {
                        var range = new Tuple<ulong, ulong>(mask, (mask << 1) - 1);
                        ranges = new Tuple<ulong, ulong>[] { range };
                    }
                    else //1-case is neither full nor empty
                    {
                        var ranges1 = LiftRanges(b, (b - set.One.Ordinal) - 1, ToRanges1(set.One));
                        ranges = new Tuple<ulong, ulong>[ranges1.Length];
                        for (int i = 0; i < ranges1.Length; i++)
                        {
                            ranges[i] = new Tuple<ulong, ulong>(ranges1[i].Item1 | mask, ranges1[i].Item2 | mask);
                        }
                    }
                    #endregion
                }
                else if (set.Zero.IsFull)
                {
                    #region 0-case is full
                    if (set.One.IsEmpty)
                    {
                        var range = new Tuple<ulong, ulong>(0, mask - 1);
                        ranges = new Tuple<ulong, ulong>[] { range };
                    }
                    else
                    {
                        var rangesR = LiftRanges(b, (b - set.One.Ordinal) - 1, ToRanges1(set.One));
                        var range = rangesR[0];
                        if (range.Item1 == 0)
                        {
                            ranges = new Tuple<ulong, ulong>[rangesR.Length];
                            ranges[0] = new Tuple<ulong, ulong>(0, range.Item2 | mask);
                            for (int i = 1; i < rangesR.Length; i++)
                            {
                                ranges[i] = new Tuple<ulong, ulong>(rangesR[i].Item1 | mask, rangesR[i].Item2 | mask);
                            }
                        }
                        else
                        {
                            ranges = new Tuple<ulong, ulong>[rangesR.Length + 1];
                            ranges[0] = new Tuple<ulong, ulong>(0, mask - 1);
                            for (int i = 0; i < rangesR.Length; i++)
                            {
                                ranges[i + 1] = new Tuple<ulong, ulong>(rangesR[i].Item1 | mask, rangesR[i].Item2 | mask);
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region 0-case is neither full nor empty
                    var rangesL = LiftRanges(b, (b - set.Zero.Ordinal) - 1, ToRanges1(set.Zero));
                    var last = rangesL[rangesL.Length - 1];

                    if (set.One.IsEmpty)
                    {
                        ranges = rangesL;
                    }

                    else if (set.One.IsFull)
                    {
                        var ranges1 = new List<Tuple<ulong, ulong>>();
                        for (int i = 0; i < rangesL.Length - 1; i++)
                            ranges1.Add(rangesL[i]);
                        if (last.Item2 == (mask - 1))
                        {
                            ranges1.Add(new Tuple<ulong, ulong>(last.Item1, (mask << 1) - 1));
                        }
                        else
                        {
                            ranges1.Add(last);
                            ranges1.Add(new Tuple<ulong, ulong>(mask, (mask << 1) - 1));
                        }
                        ranges = ranges1.ToArray();
                    }
                    else //general case: neither 0-case, not 1-case is full or empty
                    {
                        var rangesR0 = ToRanges1(set.One);

                        var rangesR = LiftRanges(b, (b - set.One.Ordinal) - 1, rangesR0);

                        var first = rangesR[0];

                        if (last.Item2 == (mask - 1) && first.Item1 == 0) //merge together the last and first ranges
                        {
                            ranges = new Tuple<ulong, ulong>[rangesL.Length + rangesR.Length - 1];
                            for (int i = 0; i < rangesL.Length - 1; i++)
                                ranges[i] = rangesL[i];
                            ranges[rangesL.Length - 1] = new Tuple<ulong, ulong>(last.Item1, first.Item2 | mask);
                            for (int i = 1; i < rangesR.Length; i++)
                                ranges[rangesL.Length - 1 + i] = new Tuple<ulong, ulong>(rangesR[i].Item1 | mask, rangesR[i].Item2 | mask);
                        }
                        else
                        {
                            ranges = new Tuple<ulong, ulong>[rangesL.Length + rangesR.Length];
                            for (int i = 0; i < rangesL.Length; i++)
                                ranges[i] = rangesL[i];
                            for (int i = 0; i < rangesR.Length; i++)
                                ranges[rangesL.Length + i] = new Tuple<ulong, ulong>(rangesR[i].Item1 | mask, rangesR[i].Item2 | mask);
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
        public Tuple<ulong, ulong>[] ToRanges(BDD set, int maxBit)
        {
            if (set.IsEmpty)
                return new Tuple<ulong, ulong>[] { };
            else if (set.IsFull)
                return new Tuple<ulong, ulong>[] { new Tuple<ulong, ulong>(0, ((((ulong)1 << maxBit) << 1) - 1)) }; //note: maxBit could be 31
            else
                return LiftRanges(maxBit + 1, maxBit - set.Ordinal, ToRanges1(set));
        }
    }
}
