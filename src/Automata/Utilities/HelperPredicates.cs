using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Automata.Utilities
{

    internal class HelperPredicates
    {
        List<string> helper_predicates;
        Dictionary<BDD, string> predicate_cache;
        CharSetSolver solver;
        BDD ascii;
        bool OptimzeForASCIIinput;

        public HelperPredicates(CharSetSolver solver, bool OptimzeForAsciiInput)
        {
            this.solver = solver;
            helper_predicates = new List<string>();
            predicate_cache = new Dictionary<BDD, string>();
            ascii = solver.MkRangeConstraint('\0', '\x7F');
            this.OptimzeForASCIIinput = OptimzeForAsciiInput;
        }

//        private static void GenerateCodeForBDD(StringBuilder code, BDD pred, string methid)
//        {
//            Dictionary<BDD, int> idMap = new Dictionary<BDD, int>();
//            int nextLabelId = 0;
//            Func<BDD, int> GetId = bdd =>
//            {
//                int bddid;
//                if (!idMap.TryGetValue(bdd, out bddid))
//                {
//                    bddid = nextLabelId++;
//                    idMap[bdd] = bddid;
//                }
//                return bddid;
//            };

//            HashSet<BDD> done = new HashSet<BDD>();
//            SimpleStack<BDD> todo = new SimpleStack<BDD>();
//            todo.Push(pred);
//            done.Add(pred);
//            StringBuilder leaves = new StringBuilder();
//            while (todo.IsNonempty)
//            {
//                var bdd = todo.Pop();
//                if (bdd.IsLeaf)
//                    leaves.Append(String.Format(@"
//                P{0}_{1}: return {2};", methid, GetId(bdd), bdd.IsEmpty ? "false" : "true"));
//                else
//                {
//                    if (bdd == pred) //skip the label
//                        code.Append(String.Format(@"
//                if ((c & 0x{1:X}) == 0) goto P{0}_{2}; else goto P{0}_{3};", methid, 1 << bdd.Ordinal, GetId(bdd.Zero), GetId(bdd.One)));
//                    else
//                        code.Append(String.Format(@"
//                P{0}_{1}: if ((c & 0x{2:X}) == 0) goto P{0}_{3}; else goto P{0}_{4};", methid, GetId(bdd), 1 << bdd.Ordinal, GetId(bdd.Zero), GetId(bdd.One)));
//                    if (done.Add(bdd.Zero))
//                        todo.Push(bdd.Zero);
//                    if (done.Add(bdd.One))
//                        todo.Push(bdd.One);
//                }
//            }
//            code.Append(leaves.ToString());
//        }

        private static void GenerateCodeForBDD(StringBuilder code, BDD pred, string methid)
        {
            code.Append(string.Format("return {0};", RangesToCode(pred.ToRanges()))); 
        }

        public string GeneratePredicate(BDD pred)
        {
            if (!pred.IsLeaf && pred.Ordinal > 31)
                throw new AutomataException(AutomataExceptionKind.OrdinalIsTooLarge);

            string res;
            if (!predicate_cache.TryGetValue(pred, out res))
            {
                if (OptimzeForASCIIinput)
                {
                    var predascii = pred.And(ascii);
                    var predascii_ranges = predascii.ToRanges();
                    var prednonascii = pred.Diff(ascii);
                    if (prednonascii.IsEmpty)
                    {
                        res = RangesToCode(predascii_ranges);
                    }
                    else
                    {
                        var asciiCase = RangesToCode(predascii_ranges);
                        var nonasciiCase = GeneratePredicateHelper(prednonascii);
                        res = string.Format("(x <= 0x7F ? {0} : {1})", asciiCase, nonasciiCase);
                    }
                }
                else
                    res = GeneratePredicateHelper(pred);
                predicate_cache[pred] = res;
            }
            return res;
        }

        private string GeneratePredicateHelper(BDD pred)
        {
            string res;
            //generate a predicate depending on how complex the condition is 
            if (!predicate_cache.TryGetValue(pred, out res))
            {
                if (pred.IsLeaf)
                {
                    if (pred.IsEmpty)
                        res = "false";
                    else if (pred.IsFull)
                        res = "true";
                    else
                        throw new AutomataException(AutomataExceptionKind.UnexpectedMTBDDTerminal);
                }
                else
                {
                    //if there is a single node in the BDD then 
                    //just inline the corresponding bit mask operation
                    if (pred.One.IsLeaf && pred.Zero.IsLeaf)
                        res = string.Format("x & 0x{0:X} {1} 0", 1 << pred.Ordinal, (pred.One.IsFull ? "!=" : "=="));
                    else
                    {
                        var ranges = pred.ToRanges();
                        if (ranges.Length <= 3)
                        {
                            res = RangesToCode(ranges);
                        }
                        else
                        {
                            //generate a method for checking this condition
                            StringBuilder helper_method = new StringBuilder();
                            //create a new method for this predicate
                            int methid = helper_predicates.Count;
                            helper_method.Append(String.Format(@"

        static bool P{0}(int x)
        {{
            return ", methid));

                            helper_method.Append(RangesToCode(ranges));
                            helper_method.Append(@";
        }");
                            helper_predicates.Add(helper_method.ToString());
                            res = string.Format("P{0}(x)", methid);
                        }
                    }
                }
                predicate_cache[pred] = res;
            }
            return res;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (string s in helper_predicates)
                sb.AppendLine(s);
            return sb.ToString();
        }

        //private static string RangesToCode(Tuple<uint, uint>[] ranges)
        //{
        //    StringBuilder cond = new StringBuilder();
        //    foreach (var range in ranges)
        //    {
        //        if (cond.Length > 0)
        //            cond.Append(" || ");
        //        if (range.Item1 == range.Item2)
        //            cond.AppendFormat("c == 0x{0:X}", range.Item1);
        //        else
        //            cond.AppendFormat("(0x{0:X} <= c && c <= 0x{1:X})", range.Item1, range.Item2);
        //    }
        //    return cond.ToString();
        //}

        private static string RangesToCode(Tuple<uint, uint>[] ranges)
        {
            if (ranges.Length == 0)
                return "false";
            return RangesToCode2(ranges, 0, ranges.Length - 1);
        }

        private static string RangesToCode2(Tuple<uint, uint>[] ranges, int first, int last)
        {
            if (first == last)
            {
                if (ranges[first].Item1 == ranges[first].Item2)
                    return string.Format("(x == 0x{0:X})", ranges[first].Item1);
                else
                    return string.Format("(0x{0:X} <= x && x <= 0x{1:X})", ranges[first].Item1, ranges[first].Item2);
            }
            else if ((last == (first + 1)) && (ranges[first].Item1 == ranges[first].Item2) && (ranges[last].Item1 == ranges[last].Item2))
            {
                return string.Format("(x == 0x{0:X} || x == 0x{1:X})", ranges[first].Item1, ranges[last].Item1);
            }
            else
            {
                int middle = (first + last + 1) / 2;
                string s1 = RangesToCode2(ranges, first, middle - 1);
                string s2 = RangesToCode3(ranges, middle, last);
                return string.Format("(x < 0x{0:X} ? {1} : {2})", ranges[middle].Item1, s1, s2);
            }
        }

        private static string RangesToCode3(Tuple<uint, uint>[] ranges, int first, int last)
        {
            //we know that c >= ranges[first].Item1
            if (first == last)
                return string.Format("(x <= 0x{0:X})", ranges[first].Item2);
            else
            {
                int middle = (first + last + 1) / 2;
                string s1 = RangesToCode3(ranges, first, middle - 1);
                string s2 = RangesToCode3(ranges, middle, last);
                return string.Format("(x < 0x{0:X} ? {1} : {2})", ranges[middle].Item1, s1, s2);
            }
        }
    }
}
