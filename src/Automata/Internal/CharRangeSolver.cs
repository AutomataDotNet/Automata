using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Automata.Internal
{
    public class CharRangeSolver : ICharAlgebra<HashSet<Pair<char,char>>>
    {

        BitWidth encoding;
        char minCharacter;
        char maxCharacter;
        MintermGenerator<HashSet<Pair<char, char>>> mtg;
        /// <summary>
        /// The character encoding used by the solver
        /// </summary>
        public BitWidth Encoding
        {
            get { return encoding; }
        }

        public CharRangeSolver(BitWidth encoding)
        {
            this.encoding = encoding;
            this.minCharacter = (char)0;
            this.maxCharacter = (encoding == BitWidth.BV7 ? '\x007F' :
                (encoding == BitWidth.BV8 ? '\x00FF' : '\xFFFF'));

            mtg = new MintermGenerator<HashSet<Pair<char, char>>>(this);
        }

        private bool hasoverlap(Pair<char,char> lhs, Pair<char, char> rhs, out Pair<char,char> res)
        {
            if (!(lhs.First.CompareTo(rhs.Second) <= 0 || rhs.First.CompareTo(lhs.Second) <= 0))
            {
                res = default(Pair<char, char>);
                return false;
            }
            res = new Pair<char, char>( (lhs.First.CompareTo(rhs.First) > 0 ? lhs.First : rhs.First),
                                        (rhs.Second.CompareTo(lhs.Second) < 0 ? rhs.Second : lhs.Second) );
            if (res.First.CompareTo(res.Second) > 0)
            {
                return false;
            }

            return true;
        }


        public HashSet<Pair<char, char>> MkOr(HashSet<Pair<char, char>> constraint1, HashSet<Pair<char, char>> constraint2)
        {
            var res = new HashSet<Pair<char,char>>(constraint1);
            res.UnionWith(constraint2);
            return res;
        }

        public HashSet<Pair<char, char>> MkOr(IEnumerable<HashSet<Pair<char, char>>> constraints)
        {
            var res = new HashSet<Pair<char,char>>();
            foreach (HashSet<Pair<char, char>> cur in constraints)
            {
                res.UnionWith(cur);
            }
            return res;
        }



        public HashSet<Pair<char, char>> MkAnd(HashSet<Pair<char, char>> constraint1, HashSet<Pair<char, char>> constraint2)
        {
            var res = new HashSet<Pair<char, char>>();

            foreach (Pair<char, char> a in constraint1)
            {
                foreach (Pair<char, char> b in constraint2)
                {
                    Pair<char, char> newpair;
                    if (hasoverlap(a, b, out newpair))
                    {
                        res.Add(newpair);
                    }
                }
            }
            return res;
        }

        public HashSet<Pair<char, char>> Simplify(HashSet<Pair<char, char>> constraint)
        {
            return constraint;
        }

        public HashSet<Pair<char, char>> MkAnd(IEnumerable<HashSet<Pair<char, char>>> constraints)
        {
            // monotonicity.
            HashSet<Pair<char,char>> res = null;
            foreach (HashSet<Pair<char, char>> cur in constraints)
            {
                if (res == null)
                {
                    res = new HashSet<Pair<char, char>>(cur);
                    continue;
                }

                res = MkAnd(res, cur);
                if (res.Count == 0)
                {
                    break;
                }
            }
            return res;
        }

        public HashSet<Pair<char, char>> MkAnd(params HashSet<Pair<char, char>>[] constraints)
        {
            // monotonicity.
            HashSet<Pair<char, char>> res = null;
            foreach (HashSet<Pair<char, char>> cur in constraints)
            {
                if (res == null)
                {
                    res = new HashSet<Pair<char, char>>(cur);
                    continue;
                }

                res = MkAnd(res, cur);
                if (res.Count == 0)
                {
                    break;
                }
            }
            return res;
        }

        // FAIL
        public HashSet<Pair<char, char>> MkNot(HashSet<Pair<char, char>> constraint)
        {
            var res = this.True;

            var curprime = new HashSet<Pair<char, char>>();
            // Invariant: we never return a malformed pair
            foreach (Pair<char, char> cur in constraint)
            {
                if (cur.First.CompareTo(minCharacter) > 0) {
                    curprime.Add(new Pair<char, char>(minCharacter, (char)(cur.First - '\x0001')));
                }

                if (cur.Second.CompareTo(maxCharacter) < 0)
                {
                    curprime.Add(new Pair<char, char>((char)(cur.Second + '\x0001'), maxCharacter));
                }

                res = MkAnd(res, curprime);
                curprime.Clear();
            }

            return res;
        }

        public HashSet<Pair<char, char>> True
        {
            get
            {
                var res = new HashSet<Pair<char, char>>();
                res.Add(new Pair<char, char>(minCharacter, maxCharacter));
                return res;

            }
        }

        public HashSet<Pair<char, char>> False
        {
            get { return new HashSet<Pair<char, char>>();  }
        }

        public HashSet<Pair<char, char>> MkRangeConstraint(bool caseInsensitive, char lower, char upper)
        {
            var res = new HashSet<Pair<char, char>>();

            // Assumption: [lower, higher] does not cross a case boundary; i.e. all elements
            // in the range are either upper or lower case.
            // TBD: this does not always work correctly
            if (caseInsensitive)
            {
                res.Add(new Pair<char, char>(System.Char.ToUpper(lower), System.Char.ToUpper(upper)));
                res.Add(new Pair<char, char>(System.Char.ToLower(lower), System.Char.ToLower(upper)));
            }
            else
            {
                res.Add(new Pair<char,char>(lower, upper));
            }
            return res;
        }

        public HashSet<Pair<char, char>> MkCharConstraint(bool caseInsensitive, char c)
        {
            var res = new HashSet<Pair<char, char>>();
            if (caseInsensitive)
            {
                res.Add(new Pair<char, char>(System.Char.ToUpper(c), System.Char.ToUpper(c)));
                res.Add(new Pair<char, char>(System.Char.ToLower(c), System.Char.ToLower(c)));
            }
            else
            {
                res.Add(new Pair<char, char>(c,c));
            }
            return res;
        }

        public HashSet<Pair<char, char>> MkRangesConstraint(bool caseInsensitive, IEnumerable<char[]> ranges)
        {
            // Assume: all the char[] in ranges have 2 elements exactly
            
            var res = new HashSet<Pair<char, char>>();

            foreach(char [] range in ranges)
            {
                var lower = range[0];
                var upper = range[1];

                // Assumption: [lower, higher] does not cross a case boundary; i.e. all elements
                // in the range are either upper or lower case. 
                //TBD: this does not always work properly
                if (caseInsensitive)
                {
                    res.Add(new Pair<char, char>(System.Char.ToUpper(lower), System.Char.ToUpper(upper)));
                    res.Add(new Pair<char, char>(System.Char.ToLower(lower), System.Char.ToLower(upper)));
                }
                else
                {
                    res.Add(new Pair<char,char>(lower, upper));
                }
            }
            return res;
        }

        public bool IsSatisfiable(HashSet<Pair<char, char>> constraint)
        {
            return constraint.Count > 0;
        }

        public bool AreEquivalent(HashSet<Pair<char, char>> constraint1, HashSet<Pair<char, char>> constraint2)
        {
            if (MkAnd(MkNot(constraint1), constraint2).Count > 0 || MkAnd(MkNot(constraint2), constraint1).Count > 0)
                return false;
            else
                return true;
        }

        public IEnumerable<Pair<bool[], HashSet<Pair<char, char>>>> GenerateMinterms(HashSet<Pair<char, char>>[] constraints)
        {
            return mtg.GenerateMinterms(constraints);
        }

        /*
         * original explicit implementation of minterms
         * 

        public IEnumerable<Pair<bool[], HashSet<Pair<char, char>>>> GenerateMinterms(HashSet<Pair<char, char>>[] constraints)
        {
            if (constraints.Length == 0)
                yield return new Pair<bool[], HashSet<Pair<char, char>>>(new bool[] { }, this.True);
            else
            {
                var mt = new Internal.Minterms<wrapwrap>(new wrapwrap(this, this.True));

                var seq = mt.GenerateCombinations(true, Array.ConvertAll(constraints, cur => new wrapwrap(this, cur)));

                foreach (var pair in seq)
                {
                    var outvar = pair.Second._contents;

                    yield return new Pair<bool[], HashSet<Pair<char, char>>>(pair.First, outvar);
                }
            }
        }


        class wrapwrap : Internal.ICapNeg
        {
            private CharRangeSolver _parent;
            internal HashSet<Pair<char, char>> _contents;
            internal HashSet<Pair<char, char>> _inverse;

            public wrapwrap(CharRangeSolver parent, HashSet<Pair<char, char>> wrapee)
            {
                _parent = parent;
                _contents = wrapee;
                _inverse = null;
            }

            public Internal.ICapNeg cap(Internal.ICapNeg b)
            {
                return new wrapwrap(_parent, _parent.MkAnd(_contents, ((wrapwrap)b)._contents));
            }

            public Internal.ICapNeg cup(Internal.ICapNeg b)
            {
                return new wrapwrap(_parent, _parent.MkOr(_contents, ((wrapwrap)b)._contents));
            }

            public Internal.ICapNeg minus(Internal.ICapNeg b)
            {
                var bprime = (wrapwrap)b;

                if (bprime._inverse == null)
                {
                    bprime._inverse = _parent.MkNot(bprime._contents);
                }
                return new wrapwrap(_parent, _parent.MkAnd(_contents, bprime._inverse));
            }

            public bool same_elts(Internal.ICapNeg b)
            {

                var bprime = (wrapwrap)b;

                var lhs = this.minus(b);
                var rhs = b.minus(this);

                return (lhs.is_empty() && rhs.is_empty());
            }

            public bool is_empty()
            {
                if (_contents.Count == 0)
                {
                    return true;
                }

                foreach (Pair<char, char> cur in _contents)
                {
                    if (cur.First.CompareTo(cur.Second) <= 0)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
        */

        #region IPrettyPrinter<HashSet<Pair<char,char>>> Members


        public string PrettyPrint(HashSet<Pair<char, char>> pred)
        {
            throw new NotImplementedException();
        }

        public string PrettyPrint(HashSet<Pair<char, char>> pred, Func<HashSet<Pair<char, char>>,string> varLookup)
        {
            throw new NotImplementedException();
        }

        #endregion


        #region ICharSolver<HashSet<Pair<char,char>>> Members


        public HashSet<Pair<char, char>> ConvertFromCharSet(BDD set)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region ICharSolver<HashSet<Pair<char,char>>> Members


        public CharSetSolver CharSetProvider
        {
            get { throw new NotImplementedException(); }
        }

        #endregion

        #region ICharSolverPred<HashSet<Pair<char,char>>> Members

        public HashSet<Pair<char, char>> MkCharPredicate(string name, HashSet<Pair<char, char>> pred)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IPrettyPrinter<HashSet<Pair<char,char>>> Members


        public string PrettyPrintCS(HashSet<Pair<char, char>> t, Func<HashSet<Pair<char, char>>, string> varLookup)
        {
            throw new NotImplementedException();
        }

        #endregion


        public bool TryConvertToCharSet(HashSet<Pair<char, char>> pred, out BDD set)
        {
            set = null;
            return false;
        }

        public HashSet<Pair<char, char>> MkSet(uint e)
        {
            return new HashSet<Pair<char, char>>(new Pair<char, char>[] { new Pair<char, char>((char)e, (char)e) });
        }


        public uint Choose(HashSet<Pair<char, char>> s)
        {
            if (s.Count == 0)
                throw new AutomataException(AutomataExceptionKind.SetIsEmpty);

            var e = s.GetEnumerator();
            e.MoveNext();
            var res = (uint)e.Current.Item1;
            return res;
        }


        public bool IsExtensional
        {
            get { return false; }
        }

        public HashSet<Pair<char, char>> MkSymmetricDifference(HashSet<Pair<char, char>> p1, HashSet<Pair<char, char>> p2)
        {
            return MkOr(MkAnd(p1, MkNot(p2)), MkAnd(p2, MkNot(p1)));
        }

        public bool CheckImplication(HashSet<Pair<char, char>> lhs, HashSet<Pair<char, char>> rhs)
        {
            return !IsSatisfiable(MkAnd(lhs, MkNot(rhs)));
        }


        public bool IsAtomic
        {
            get { return true; }
        }

        public HashSet<Pair<char, char>> GetAtom(HashSet<Pair<char, char>> psi)
        {
            if (psi.Count == 0)
                return psi;

            var e = psi.GetEnumerator();
            e.MoveNext();
            var elem = e.Current.Item1;
            var atom = new HashSet<Pair<char, char>>(new Pair<char, char>[] { new Pair<char, char>(elem, elem) });
            return atom;
        }


        public bool EvaluateAtom(HashSet<Pair<char, char>> atom, HashSet<Pair<char, char>> psi)
        {
            throw new NotImplementedException();
        }
    }
}
