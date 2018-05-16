using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Automata.BooleanAlgebras
{
    class BV64Algebra : IBooleanAlgebra<ulong>
    {
        MintermGenerator<ulong> mtg;
        public BV64Algebra()
        {
            mtg = new MintermGenerator<ulong>(this);
        }
        public ulong False
        {
            get
            {
                return 0;
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
                return ulong.MaxValue;
            }
        }

        public bool AreEquivalent(ulong predicate1, ulong predicate2)
        {
            return predicate1 == predicate2;
        }

        public bool CheckImplication(ulong lhs, ulong rhs)
        {
            return ((~lhs) | rhs) == ulong.MaxValue;
        }

        public bool EvaluateAtom(ulong atom, ulong psi)
        {
            return (atom & psi) == atom;
        }

        public IEnumerable<Tuple<bool[], ulong>> GenerateMinterms(params ulong[] constraints)
        {
            return mtg.GenerateMinterms(constraints);
        }

        /// <summary>
        /// Gets 2^n where n is the least n such that the bit is set, returns 0 if psi is 0.
        /// </summary>
        public ulong GetAtom(ulong psi)
        {
            if (psi == 0)
                return 0;
            else if ((psi & 0xFFFFFFFF) != 0)
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

        public bool IsSatisfiable(ulong predicate)
        {
            return predicate != 0;
        }

        public ulong MkAnd(params ulong[] predicates)
        {
            ulong res = ulong.MaxValue;
            for (int i = 0; i < predicates.Length; i++)
            {
                res = res & predicates[i];
                if (res == 0)
                    return 0;
            }
            return res;
        }

        public ulong MkAnd(IEnumerable<ulong> predicates)
        {
            throw new NotImplementedException();
        }

        public ulong MkAnd(ulong predicate1, ulong predicate2)
        {
            return predicate1 & predicate2;
        }

        public ulong MkDiff(ulong predicate1, ulong predicate2)
        {
            return predicate1 & ~predicate2;
        }

        public ulong MkNot(ulong predicate)
        {
            return ~predicate;
        }

        public ulong MkOr(IEnumerable<ulong> predicates)
        {
            throw new NotImplementedException();
        }

        public ulong MkOr(ulong predicate1, ulong predicate2)
        {
            return predicate1 | predicate2;
        }

        public ulong MkSymmetricDifference(ulong p1, ulong p2)
        {
            return p1 ^ p2;
        }

        public ulong Simplify(ulong predicate)
        {
            return predicate;
        }
    }
}
