using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Automata
{
    internal class CharRangeSolver : ICharAlgebra<HashSet<Tuple<char,char>>>
    {

        BitWidth encoding;
        char minCharacter;
        char maxCharacter;
        MintermGenerator<HashSet<Tuple<char, char>>> mtg;
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

            mtg = new MintermGenerator<HashSet<Tuple<char, char>>>(this);
        }

        private bool hasoverlap(Tuple<char,char> lhs, Tuple<char, char> rhs, out Tuple<char,char> res)
        {
            if (!(lhs.Item1.CompareTo(rhs.Item2) <= 0 || rhs.Item1.CompareTo(lhs.Item2) <= 0))
            {
                res = default(Tuple<char, char>);
                return false;
            }
            res = new Tuple<char, char>( (lhs.Item1.CompareTo(rhs.Item1) > 0 ? lhs.Item1 : rhs.Item1),
                                        (rhs.Item2.CompareTo(lhs.Item2) < 0 ? rhs.Item2 : lhs.Item2) );
            if (res.Item1.CompareTo(res.Item2) > 0)
            {
                return false;
            }

            return true;
        }


        public HashSet<Tuple<char, char>> MkOr(HashSet<Tuple<char, char>> constraint1, HashSet<Tuple<char, char>> constraint2)
        {
            var res = new HashSet<Tuple<char,char>>(constraint1);
            res.UnionWith(constraint2);
            return res;
        }

        public HashSet<Tuple<char, char>> MkOr(IEnumerable<HashSet<Tuple<char, char>>> constraints)
        {
            var res = new HashSet<Tuple<char,char>>();
            foreach (HashSet<Tuple<char, char>> cur in constraints)
            {
                res.UnionWith(cur);
            }
            return res;
        }



        public HashSet<Tuple<char, char>> MkAnd(HashSet<Tuple<char, char>> constraint1, HashSet<Tuple<char, char>> constraint2)
        {
            var res = new HashSet<Tuple<char, char>>();

            foreach (Tuple<char, char> a in constraint1)
            {
                foreach (Tuple<char, char> b in constraint2)
                {
                    Tuple<char, char> newpair;
                    if (hasoverlap(a, b, out newpair))
                    {
                        res.Add(newpair);
                    }
                }
            }
            return res;
        }

        public HashSet<Tuple<char, char>> Simplify(HashSet<Tuple<char, char>> constraint)
        {
            return constraint;
        }

        public HashSet<Tuple<char, char>> MkAnd(IEnumerable<HashSet<Tuple<char, char>>> constraints)
        {
            // monotonicity.
            HashSet<Tuple<char,char>> res = null;
            foreach (HashSet<Tuple<char, char>> cur in constraints)
            {
                if (res == null)
                {
                    res = new HashSet<Tuple<char, char>>(cur);
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

        public HashSet<Tuple<char, char>> MkAnd(params HashSet<Tuple<char, char>>[] constraints)
        {
            // monotonicity.
            HashSet<Tuple<char, char>> res = null;
            foreach (HashSet<Tuple<char, char>> cur in constraints)
            {
                if (res == null)
                {
                    res = new HashSet<Tuple<char, char>>(cur);
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
        public HashSet<Tuple<char, char>> MkNot(HashSet<Tuple<char, char>> constraint)
        {
            var res = this.True;

            var curprime = new HashSet<Tuple<char, char>>();
            // Invariant: we never return a malformed pair
            foreach (Tuple<char, char> cur in constraint)
            {
                if (cur.Item1.CompareTo(minCharacter) > 0) {
                    curprime.Add(new Tuple<char, char>(minCharacter, (char)(cur.Item1 - '\x0001')));
                }

                if (cur.Item2.CompareTo(maxCharacter) < 0)
                {
                    curprime.Add(new Tuple<char, char>((char)(cur.Item2 + '\x0001'), maxCharacter));
                }

                res = MkAnd(res, curprime);
                curprime.Clear();
            }

            return res;
        }

        public HashSet<Tuple<char, char>> True
        {
            get
            {
                var res = new HashSet<Tuple<char, char>>();
                res.Add(new Tuple<char, char>(minCharacter, maxCharacter));
                return res;

            }
        }

        public HashSet<Tuple<char, char>> False
        {
            get { return new HashSet<Tuple<char, char>>();  }
        }

        public HashSet<Tuple<char, char>> MkRangeConstraint(char lower, char upper, bool caseInsensitive = false)
        {
            var res = new HashSet<Tuple<char, char>>();

            // Assumption: [lower, higher] does not cross a case boundary; i.e. all elements
            // in the range are either upper or lower case.
            // TBD: this does not always work correctly
            if (caseInsensitive)
            {
                res.Add(new Tuple<char, char>(System.Char.ToUpper(lower), System.Char.ToUpper(upper)));
                res.Add(new Tuple<char, char>(System.Char.ToLower(lower), System.Char.ToLower(upper)));
            }
            else
            {
                res.Add(new Tuple<char,char>(lower, upper));
            }
            return res;
        }

        public HashSet<Tuple<char, char>> MkCharConstraint( char c, bool caseInsensitive = false)
        {
            var res = new HashSet<Tuple<char, char>>();
            if (caseInsensitive)
            {
                res.Add(new Tuple<char, char>(System.Char.ToUpper(c), System.Char.ToUpper(c)));
                res.Add(new Tuple<char, char>(System.Char.ToLower(c), System.Char.ToLower(c)));
            }
            else
            {
                res.Add(new Tuple<char, char>(c,c));
            }
            return res;
        }

        public HashSet<Tuple<char, char>> MkRangesConstraint(bool caseInsensitive, IEnumerable<char[]> ranges)
        {
            // Assume: all the char[] in ranges have 2 elements exactly
            
            var res = new HashSet<Tuple<char, char>>();

            foreach(char [] range in ranges)
            {
                var lower = range[0];
                var upper = range[1];

                // Assumption: [lower, higher] does not cross a case boundary; i.e. all elements
                // in the range are either upper or lower case. 
                //TBD: this does not always work properly
                if (caseInsensitive)
                {
                    res.Add(new Tuple<char, char>(System.Char.ToUpper(lower), System.Char.ToUpper(upper)));
                    res.Add(new Tuple<char, char>(System.Char.ToLower(lower), System.Char.ToLower(upper)));
                }
                else
                {
                    res.Add(new Tuple<char,char>(lower, upper));
                }
            }
            return res;
        }

        public bool IsSatisfiable(HashSet<Tuple<char, char>> constraint)
        {
            return constraint.Count > 0;
        }

        public bool AreEquivalent(HashSet<Tuple<char, char>> constraint1, HashSet<Tuple<char, char>> constraint2)
        {
            if (MkAnd(MkNot(constraint1), constraint2).Count > 0 || MkAnd(MkNot(constraint2), constraint1).Count > 0)
                return false;
            else
                return true;
        }

        public IEnumerable<Tuple<bool[], HashSet<Tuple<char, char>>>> GenerateMinterms(HashSet<Tuple<char, char>>[] constraints)
        {
            return mtg.GenerateMinterms(constraints);
        }

        /*
         * original explicit implementation of minterms
         * 

        public IEnumerable<Tuple<bool[], HashSet<Tuple<char, char>>>> GenerateMinterms(HashSet<Tuple<char, char>>[] constraints)
        {
            if (constraints.Length == 0)
                yield return new Tuple<bool[], HashSet<Tuple<char, char>>>(new bool[] { }, this.True);
            else
            {
                var mt = new Minterms<wrapwrap>(new wrapwrap(this, this.True));

                var seq = mt.GenerateCombinations(true, Array.ConvertAll(constraints, cur => new wrapwrap(this, cur)));

                foreach (var pair in seq)
                {
                    var outvar = pair.Second._contents;

                    yield return new Tuple<bool[], HashSet<Tuple<char, char>>>(pair.First, outvar);
                }
            }
        }


        class wrapwrap : ICapNeg
        {
            private CharRangeSolver _parent;
            internal HashSet<Tuple<char, char>> _contents;
            internal HashSet<Tuple<char, char>> _inverse;

            public wrapwrap(CharRangeSolver parent, HashSet<Tuple<char, char>> wrapee)
            {
                _parent = parent;
                _contents = wrapee;
                _inverse = null;
            }

            public ICapNeg cap(ICapNeg b)
            {
                return new wrapwrap(_parent, _parent.MkAnd(_contents, ((wrapwrap)b)._contents));
            }

            public ICapNeg cup(ICapNeg b)
            {
                return new wrapwrap(_parent, _parent.MkOr(_contents, ((wrapwrap)b)._contents));
            }

            public ICapNeg minus(ICapNeg b)
            {
                var bprime = (wrapwrap)b;

                if (bprime._inverse == null)
                {
                    bprime._inverse = _parent.MkNot(bprime._contents);
                }
                return new wrapwrap(_parent, _parent.MkAnd(_contents, bprime._inverse));
            }

            public bool same_elts(ICapNeg b)
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

                foreach (Tuple<char, char> cur in _contents)
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

        #region IPrettyPrinter<HashSet<Tuple<char,char>>> Members


        public string PrettyPrint(HashSet<Tuple<char, char>> pred)
        {
            throw new NotImplementedException();
        }

        public string PrettyPrint(HashSet<Tuple<char, char>> pred, Func<HashSet<Tuple<char, char>>,string> varLookup)
        {
            throw new NotImplementedException();
        }

        #endregion


        #region ICharSolver<HashSet<Tuple<char,char>>> Members


        public HashSet<Tuple<char, char>> ConvertFromCharSet(BDD set)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region ICharSolver<HashSet<Tuple<char,char>>> Members


        public CharSetSolver CharSetProvider
        {
            get { throw new NotImplementedException(); }
        }

        #endregion

        #region ICharSolverPred<HashSet<Tuple<char,char>>> Members

        public HashSet<Tuple<char, char>> MkCharPredicate(string name, HashSet<Tuple<char, char>> pred)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IPrettyPrinter<HashSet<Tuple<char,char>>> Members


        public string PrettyPrintCS(HashSet<Tuple<char, char>> t, Func<HashSet<Tuple<char, char>>, string> varLookup)
        {
            throw new NotImplementedException();
        }

        #endregion

        public HashSet<Tuple<char, char>> MkSet(uint e)
        {
            return new HashSet<Tuple<char, char>>(new Tuple<char, char>[] { new Tuple<char, char>((char)e, (char)e) });
        }


        public uint Choose(HashSet<Tuple<char, char>> s)
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

        public HashSet<Tuple<char, char>> MkSymmetricDifference(HashSet<Tuple<char, char>> p1, HashSet<Tuple<char, char>> p2)
        {
            return MkOr(MkAnd(p1, MkNot(p2)), MkAnd(p2, MkNot(p1)));
        }

        public bool CheckImplication(HashSet<Tuple<char, char>> lhs, HashSet<Tuple<char, char>> rhs)
        {
            return !IsSatisfiable(MkAnd(lhs, MkNot(rhs)));
        }


        public bool IsAtomic
        {
            get { return true; }
        }

        public HashSet<Tuple<char, char>> GetAtom(HashSet<Tuple<char, char>> psi)
        {
            if (psi.Count == 0)
                return psi;

            var e = psi.GetEnumerator();
            e.MoveNext();
            var elem = e.Current.Item1;
            var atom = new HashSet<Tuple<char, char>>(new Tuple<char, char>[] { new Tuple<char, char>(elem, elem) });
            return atom;
        }


        public bool EvaluateAtom(HashSet<Tuple<char, char>> atom, HashSet<Tuple<char, char>> psi)
        {
            throw new NotImplementedException();
        }


        public HashSet<Tuple<char, char>> MkDiff(HashSet<Tuple<char, char>> predicate1, HashSet<Tuple<char, char>> predicate2)
        {
            return MkAnd(predicate1, MkNot(predicate2));
        }

        public char ChooseUniformly(HashSet<Tuple<char, char>> s)
        {
            throw new NotImplementedException();
        }

        public BDD ConvertToCharSet(IBDDAlgebra alg, HashSet<Tuple<char, char>> pred)
        {
            throw new NotImplementedException();
        }

        public ulong ComputeDomainSize(HashSet<Tuple<char, char>> set)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<char> GenerateAllCharacters(HashSet<Tuple<char, char>> set)
        {
            throw new NotImplementedException();
        }

        public HashSet<Tuple<char, char>>[] GetPartition()
        {
            throw new NotImplementedException();
        }

        public string SerializePredicate(HashSet<Tuple<char, char>> s)
        {
            throw new NotImplementedException();
        }

        public HashSet<Tuple<char, char>> DeserializePredicate(string s)
        {
            throw new NotImplementedException();
        }
    }
}
