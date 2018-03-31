using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Automata.BooleanAlgebras
{
    public class BV128Algebra : IBooleanAlgebra<BV128>
    {
        MintermGenerator<BV128> mtg;
        public BV128Algebra()
        {
            mtg = new MintermGenerator<BV128>(this);
        }

        static BV128 GetAtom(int i)
        {
            if (i < 64)
                return new BV128(0, ((ulong)1) << i);
            else
                return new BV128(((ulong)1) << i, 0);
        }

        public BV128 MkOr(IEnumerable<BV128> predicates)
        {
            var res = BV128.All0;
            foreach (var x in predicates)
                res = res | x;
            return res;
        }

        public BV128 MkAnd(IEnumerable<BV128> predicates)
        {
            var res = BV128.All1;
            foreach (var x in predicates)
                res = res & x;
            return res;
        }

        public BV128 MkAnd(params BV128[] predicates)
        {
            if (predicates.Length == 0)
                return BV128.All1;

            var u1 = predicates[0].Item1;
            var u2 = predicates[0].Item2;

            for (int i = 1; i < predicates.Length; i++)
            {
                u1 = u1 & predicates[i].Item1;
                u2 = u2 & predicates[i].Item2;
            }

            var u = new BV128(u1, u2);
            return u;
        }

        public BV128 MkNot(BV128 predicate)
        {
            return ~ predicate;
        }

        public BV128 MkDiff(BV128 predicate1, BV128 predicate2)
        {
            return predicate1 & ~predicate2;
        }

        public bool AreEquivalent(BV128 predicate1, BV128 predicate2)
        {
            return predicate1.Equals(predicate2);
        }

        public BV128 MkSymmetricDifference(BV128 p1, BV128 p2)
        {
            return p1 ^ p2;
        }

        public bool CheckImplication(BV128 lhs, BV128 rhs)
        {
            return ((~lhs) | rhs).Equals(BV128.All1);
        }

        public bool IsExtensional
        {
            get { return true; }
        }

        public BV128 Simplify(BV128 predicate)
        {
            return predicate;
        }

        public bool IsAtomic
        {
            get { return true; }
        }

        public BV128 GetAtom(BV128 psi)
        {
            ulong H = psi.Item1;
            ulong L = psi.Item2;
            if (L != 0)
            {
                return new BV128(0, ExtractAtom(L));
            }
            else if (H != 0)
            {
                return new BV128(ExtractAtom(H), 0);
            }
            else
            {
                return BV128.All0;
            }
        }

        private static ulong ExtractAtom(ulong H)
        {
            if ((H & 0x00000000FFFFFFFF) != 0)
            {
                if ((H & 0xFFFF) != 0)
                {
                    return ExtractAtomFrom16bits(H);
                }
                else
                {
                    return (ExtractAtomFrom16bits((H >> 16)) << 16);
                }
            }
            else
            {
                if ((H & 0xFFFF00000000) != 0)
                {
                    return (ExtractAtomFrom16bits((H >> 32)) << 32);
                }
                else
                {
                    return (ExtractAtomFrom16bits((H >> 48)) << 48);
                }
            }
        }
        private static ulong ExtractAtomFrom16bits(ulong H)
        {
            if ((H & 0xFF) != 0)
            {
                #region first byte
                if ((H & 0xF) != 0)
                {
                    #region first nibble
                    if ((H & 3) != 0)
                    {
                        if ((H & 1) != 0)
                        {
                            return 1;
                        }
                        else
                        {
                            return 2;
                        }
                    }
                    else
                    {
                        if ((H & 4) != 0)
                        {
                            return 4;
                        }
                        else
                        {
                            return 8;
                        }
                    }
                    #endregion
                }
                else
                {
                    #region second nibble
                    if ((H & 0x30) != 0)
                    {
                        if ((H & 0x10) != 0)
                        {
                            return 0x10;
                        }
                        else
                        {
                            return 0x20;
                        }
                    }
                    else
                    {
                        if ((H & 0x40) != 0)
                        {
                            return 0x40;
                        }
                        else
                        {
                            return 0x80;
                        }
                    }
                    #endregion
                }
                #endregion
            }
            else
            {
                #region second byte
                if ((H & 0xF00) != 0)
                {
                    #region first nibble
                    if ((H & 0x300) != 0)
                    {
                        if ((H & 0x100) != 0)
                        {
                            return 0x100;
                        }
                        else
                        {
                            return 0x200;
                        }
                    }
                    else
                    {
                        if ((H & 0x400) != 0)
                        {
                            return 0x400;
                        }
                        else
                        {
                            return 0x800;
                        }
                    }
                    #endregion
                }
                else
                {
                    #region second nibble
                    if ((H & 0x3000) != 0)
                    {
                        if ((H & 0x1000) != 0)
                        {
                            return 0x1000;
                        }
                        else
                        {
                            return 0x2000;
                        }
                    }
                    else
                    {
                        if ((H & 0x4000) != 0)
                        {
                            return 0x4000;
                        }
                        else
                        {
                            return 0x8000;
                        }
                    }
                    #endregion
                }
                #endregion
            }
        }

        public bool EvaluateAtom(BV128 atom, BV128 psi)
        {
            return (atom & psi).Equals(atom);
        }

        public IEnumerable<Tuple<bool[], BV128>> GenerateMinterms(params BV128[] constraints)
        {
            return mtg.GenerateMinterms(constraints);
        }

        public BV128 MkAnd(BV128 predicate1, BV128 predicate2)
        {
            return predicate1 & predicate2;
        }

        public BV128 MkOr(BV128 predicate1, BV128 predicate2)
        {
            return predicate1 | predicate2;
        }

        public bool IsSatisfiable(BV128 predicate)
        {
            return !predicate.Equals(BV128.All0);
        }

        public BV128 True
        {
            get
            {
                return BV128.All1;
            }
        }

        public BV128 False
        {
            get
            {
                return BV128.All0;
            }
        }
    }

    /// <summary>
    /// 128-bit bitvector
    /// </summary>
    public class BV128 : Tuple<ulong,ulong>
    {
        /// <summary></summary>
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        /// <summary></summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary></summary>
        public override string ToString()
        {
            return String.Format("{0:X16}{1:X16}", this.Item1, Item2);
        }

        static BV128 max = new BV128(ulong.MaxValue, ulong.MaxValue);
        static BV128 min = new BV128(0, 0);

        /// <summary>
        /// All bits are 1
        /// </summary>
        public static BV128 All1
        {
            get { return max; }
        }

        /// <summary>
        /// All bits are 0
        /// </summary>
        public static BV128 All0
        {
            get { return min; }
        }

        /// <summary>
        /// Constructs a bitvector with 128 bits
        /// </summary>
        public BV128(ulong high, ulong low) : base(high, low)
        {
        }

        /// <summary>
        /// Bitwise AND
        /// </summary>
        public static BV128 operator &(BV128 x, BV128 y)
        {
            return new BV128(x.Item1 & y.Item1, x.Item2 & y.Item2);
        }

        /// <summary>
        /// Bitwise OR
        /// </summary>
        public static BV128 operator |(BV128 x, BV128 y)
        {
            return new BV128(x.Item1 | y.Item1, x.Item2 | y.Item2);
        }

        /// <summary>
        /// Bitwise XOR
        /// </summary>
        public static BV128 operator ^(BV128 x, BV128 y)
        {
            return new BV128(x.Item1 ^ y.Item1, x.Item2 ^ y.Item2);
        }

        /// <summary>
        /// Bitwise NOT
        /// </summary>
        public static BV128 operator ~(BV128 x)
        {
            return new BV128(~x.Item1, ~x.Item2);
        }

        ///// <summary>
        ///// Equivalent (identical)
        ///// </summary>
        //public static bool operator ==(BV128 x, BV128 y)
        //{
        //    return x.Item1 == y.Item1 && x.Item2 == y.Item2;
        //}

        ///// <summary>
        ///// Inequivalent (different)
        ///// </summary>
        //public static bool operator !=(BV128 x, BV128 y)
        //{
        //    return x.Item1 != y.Item1 || x.Item2 != y.Item2;
        //}

        /// <summary>
        /// Generates a random bitvector.
        /// </summary>
        public static BV128 Random()
        {
            var c = new Chooser();
            var u1 = c.ChooseBV64();
            var u2 = c.ChooseBV64();
            return new BV128(u1, u2);
        }
    }
}
