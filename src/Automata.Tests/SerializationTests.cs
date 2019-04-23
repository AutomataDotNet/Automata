using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

using Microsoft.Automata;
using Microsoft.Automata.Generated;
using Microsoft.Automata.Rex;
using Microsoft.Automata.Utilities;

using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Formatters.Soap;
using System.IO;
using System.Web;

using System.Linq;

namespace Automata.Tests
{
    [TestClass]
    public class SerializationTests
    {
        [TestMethod]
        public void TestSerialization_DecisionTree()
        {
            var bstLeaf1 = new DecisionTree.BST(10, null, null);
            var bstLeaf2 = new DecisionTree.BST(5, null, null);
            var bst = new DecisionTree.BST(7, bstLeaf1, bstLeaf2);
            var set = new IntervalSet(new Tuple<uint, uint>(0x61, 0x71));
            var dt = new DecisionTree(new int[] { 1, 2, 3 }, bst);
            SerializeObjectToFile_bin("dt.bin", dt);
            var dt_ = (DecisionTree)DeserializeObjectFromFile_bin("dt.bin");
            Assert.AreEqual<string>(dt.ToString(), dt_.ToString());
        }

        [TestMethod]
        public void TestSerialization_BooleanDecisionTree()
        {
            var bstLeaf1 = new DecisionTree.BST(10, null, null);
            var bstLeaf2 = new DecisionTree.BST(5, null, null);
            var bst = new DecisionTree.BST(7, bstLeaf1, bstLeaf2);
            var set = new IntervalSet(new Tuple<uint, uint>(0x61, 0x71));
            var dt = new BooleanDecisionTree(new bool[] { true, false, true }, bst);
            SerializeObjectToFile_bin("dt.bin", dt);
            var dt_ = (BooleanDecisionTree)DeserializeObjectFromFile_bin("dt.bin");
            Assert.AreEqual<string>(dt.ToString(), dt_.ToString());
        }

        [TestMethod]
        public void TestSerialization_BV()
        {
            var bv = new BV(1, 1);
            SerializeObjectToFile_bin("bbv.bin", bv);
            var bv_ = (BV)DeserializeObjectFromFile_bin("bbv.bin");
            Assert.AreEqual<string>(bv.ToString(), bv_.ToString());
        }

        [TestMethod]
        public void TestSerialization_BST()
        {
            var left = new DecisionTree.BST(5, null, null);
            var right = new DecisionTree.BST(20, null, null);
            var bst1 = new DecisionTree.BST(10, left, right);
            var bst = new DecisionTree.BST(1, bst1, right);
            var str = bst.Serialize();
            var bst2 = DecisionTree.BST.Deserialize(str);
            Assert.IsTrue(!bst.IsLeaf && !bst.Left.IsLeaf && bst2.Right.IsLeaf);
            Assert.IsTrue(bst2.Node == 1 && bst2.Left.Node == 10 && bst2.Right.Node == 20);
            Assert.IsTrue(str == bst2.Serialize());
        }

        [TestMethod]
        public void TestSerialization_BST_leaf()
        {
            var leaf = new DecisionTree.BST(5, null, null);
            var s = leaf.Serialize();
            var bst = DecisionTree.BST.Deserialize(s);
            Assert.IsTrue(bst.IsLeaf);
            Assert.IsTrue(bst.Node == 5);
            Assert.IsTrue(s == bst.Serialize());
        }

        [TestMethod]
        public void TestSerialization_BVAlgebra()
        {
            var css = new CharSetSolver();
            var regex = new Regex(@"[0-9]");
            var regex_bdd = regex.ConvertToSymbolicRegexBDD(css);
            var minterms = regex_bdd.ComputeMinterms();
            var bva = BVAlgebra.Create(css, minterms);
            SerializeObjectToFile_bin("bva.bin", bva);
            var bva_ = (BVAlgebra)DeserializeObjectFromFile_bin("bva.bin");
            Assert.AreEqual<int>(bva.atoms.Length, bva_.nrOfBits);
            Assert.AreEqual<string>(bva.dtree.ToString(), bva_.dtree.ToString());
            Assert.AreEqual<Sequence<BV>>(new Sequence<BV>(bva.atoms), new Sequence<BV>(bva_.atoms));
        }

        [TestMethod]
        public void TestSerialization_BVAlgebra_Soap()
        {
            var css = new CharSetSolver();
            var regex = new Regex(@"[0-9]");
            var regex_bdd = regex.ConvertToSymbolicRegexBDD(css);
            var minterms = regex_bdd.ComputeMinterms();
            var bva = BVAlgebra.Create(css, minterms);
            SerializeObjectToFile_soap("bva.soap", bva);
            var bva_ = (BVAlgebra)DeserializeObjectFromFile_soap("bva.soap");
            Assert.AreEqual<int>(bva.atoms.Length, bva_.nrOfBits);
            Assert.AreEqual<string>(bva.dtree.ToString(), bva_.dtree.ToString());
            Assert.AreEqual<Sequence<BV>>(new Sequence<BV>(bva.atoms), new Sequence<BV>(bva_.atoms));
        }

        [TestMethod]
        public void TestSerialization_SymbolicRegex()
        {
            var css = new CharSetSolver();
            var regex = new Regex(@"\d");
            var regex_bdd = regex.ConvertToSymbolicRegexBDD(css);
            var minterms = regex_bdd.ComputeMinterms();
            var bva = BVAlgebra.Create(css, minterms);
        }

        [TestMethod]
        public void TestSerialization_SymbolicRegexMatcher()
        {
            var regex = new Regex(@"[0-9]");
            var matcher = (SymbolicRegex<ulong>)regex.Compile();
            matcher.Serialize("matcher.bin");
            var matcher_ = SymbolicRegex<ulong>.Deserialize("matcher.bin");
        }

        [TestMethod]
        public void TestSerialization_StartAnchorBugFix()
        {
            var regex1 = new Regex(@"b|a{1,2}");
            var matcher1 = (SymbolicRegex<ulong>)regex1.Compile();
            matcher1.Serialize("test1.bin");
            var matcher1_ = SymbolicRegex<ulong>.Deserialize("test1.bin");
            ////---------------------
            //var regex2 = new Regex(@"b(ba|a)?b");
            //var matcher2 = (SymbolicRegexMatcher<BV>)regex2.Compile();
            //matcher2.Serialize("test2.bin");
            //var matcher2_ = SymbolicRegexMatcher<BV>.Deserialize("test2.bin");
            //---------------------
            //Assert.IsTrue(matcher1_.Pattern.Right.Left.Left.Kind == SymbolicRegexKind.Or);
            //Assert.IsTrue(matcher1_.Pattern.Right.Left.Left.OrCount == 2);
            ////---
            //var hs = new HashSet<SymbolicRegexNode<BV>>(matcher1_.Pattern.Right.Left.Left.Alts);
            ////matcher1_.Pattern.Right.Left.Left.ShowGraph(0,"m1");
            ////matcher2_.Pattern.Right.Left.Left.ShowGraph(0,"m2");
            //matcher2_.Pattern.ShowGraph(0, "p2");

        }

        [TestMethod]
        public void TestSerialization_Roundtrip_SingleRegex()
        {
            var m1 = new Regex(@"[A-Z0-9][0-9]*").Compile();
            var s1 = new FileStream("test.bin", FileMode.Create);
            new BinaryFormatter().Serialize(s1, m1);
            s1.Close();
            var s2 = new FileStream("test.bin", FileMode.Open);
            var m2 = (IMatcher)new BinaryFormatter().Deserialize(s2);
            s2.Close();
            var input = "acac1111ghdfhdg22dfd3fd";
            var all = m2.Matches(input);
            var two = m2.Matches(input, 2);
            Assert.IsTrue(all.Length == 3);
            Assert.IsTrue(two.Length == 2);
            Assert.AreEqual<string>("1111", input.Substring(all[0].Item1, all[0].Item2));
            Assert.AreEqual<string>("1111", input.Substring(two[0].Item1, two[0].Item2));
            Assert.AreEqual<string>("22", input.Substring(all[1].Item1, all[1].Item2));
            Assert.AreEqual<string>("22", input.Substring(two[1].Item1, two[1].Item2));
            Assert.AreEqual<string>("3", input.Substring(all[2].Item1, all[2].Item2));
        }

        [TestMethod]
        public void TestSerialization_Roundtrip_MultiRegex()
        { 
            var r1 = new Regex(".*[0-9].*");
            var r2 = new Regex(".*[A-Z].*");
            var r3 = new Regex(@"[^\s]{3,5}");
            //creates a conjunction-pattern of the three regexes, 
            //order is not important, r2.Compile(r3, r1) is the same
            var m1 = r1.Compile(r2, r3);
            var s1 = new FileStream("test.soap", FileMode.Create);
            new SoapFormatter().Serialize(s1, m1);
            s1.Close();
            var s2 = new FileStream("test.soap", FileMode.Open);
            var m2 = (IMatcher)new SoapFormatter().Deserialize(s2);
            s2.Close();
            var input = "asd 1X1s dsd 77777 AAAAA sdsd 3B3sbsbsb ggg";
            var all = m2.Matches(input);
            Assert.IsTrue(all.Length == 2);
            Assert.AreEqual<string>("1X1s", input.Substring(all[0].Item1, all[0].Item2));
            Assert.AreEqual<string>("3B3sb", input.Substring(all[1].Item1, all[1].Item2));
        }

        [TestMethod]
        public void TestSerialization_Roundtrip_SingleRegex_OneMatchAtATime()
        {
            var m1 = new Regex(@"[0-9]+").Compile();
            var s1 = new FileStream("test.bin", FileMode.Create);
            new BinaryFormatter().Serialize(s1, m1);
            s1.Close();
            var s2 = new FileStream("test.bin", FileMode.Open);
            var m2 = (IMatcher)new BinaryFormatter().Deserialize(s2);
            s2.Close();
            var input = "acac1111ghdfhdg22dfd3fd";
            //first match
            var first = m2.Matches(input, 1, 0);
            Assert.AreEqual<string>("1111", input.Substring(first[0].Item1, first[0].Item2));
            //second match
            var start2 = first[0].Item1 + first[0].Item2 + 1;
            var second = m2.Matches(input, 1, start2);
            Assert.AreEqual<string>("22", input.Substring(second[0].Item1, second[0].Item2));
            //third match
            var start3 = second[0].Item1 + second[0].Item2 + 1;
            var third = m2.Matches(input, 1, start3);
            Assert.AreEqual<string>("3", input.Substring(third[0].Item1, third[0].Item2));
        }

        [TestMethod]
        public void TestSerialization_Roundtrip_SingleRegex_OneMatchAtATime_Soap()
        {
            var m1 = new Regex(@"[0-9]+").Compile();
            var s1 = new FileStream("test.soap", FileMode.Create);
            new SoapFormatter().Serialize(s1, m1);
            s1.Close();
            var s2 = new FileStream("test.soap", FileMode.Open);
            var m2 = (IMatcher)new SoapFormatter().Deserialize(s2);
            s2.Close();
            var input = "acac1111ghdfhdg22dfd3fd";
            //first match
            var first = m2.Matches(input, 1, 0);
            Assert.AreEqual<string>("1111", input.Substring(first[0].Item1, first[0].Item2));
            //second match
            var start2 = first[0].Item1 + first[0].Item2 + 1;
            var second = m2.Matches(input, 1, start2);
            Assert.AreEqual<string>("22", input.Substring(second[0].Item1, second[0].Item2));
            //third match
            var start3 = second[0].Item1 + second[0].Item2 + 1;
            var third = m2.Matches(input, 1, start3);
            Assert.AreEqual<string>("3", input.Substring(third[0].Item1, third[0].Item2));
        }

        [TestMethod]
        public void TestSerializeSimplified()
        {
            var regex = new Regex(@"\w\d+");
            var regex_simpl = new Regex(@"[BA]A+");
            var m1 = regex.Compile() as SymbolicRegexUInt64;
            var s1 = new FileStream("test.soap", FileMode.Create);
            m1.SerializeSimplified(s1, new SoapFormatter());
            s1.Close();
            var m2 = RegexMatcher.Deserialize("test.soap", new SoapFormatter());
            var input = "zzzBBBBAAAAzzzz";
            //first match
            var matches = m2.Matches(input);
            Assert.IsTrue(matches.Length == 1);
            var expected = regex_simpl.Matches(input);
            Assert.IsTrue(expected.Count == 1);
            Assert.IsTrue(expected[0].Value == input.Substring(matches[0].Item1, matches[0].Item2));
        }

        [TestMethod]
        public void TestSerializeDotPlus()
        {
            var regex = new Regex(@".+", RegexOptions.Singleline);
            var m1 = regex.Compile(true,false) as SymbolicRegexUInt64;
            var s1 = new FileStream("dotplus.soap", FileMode.Create);
            m1.Serialize(s1, new SoapFormatter());
            s1.Close();
            var s2 = new FileStream("dotplus.soap", FileMode.Open);
            var m2 = (SymbolicRegexUInt64)new SoapFormatter().Deserialize(s2);
            s2.Close();
            Assert.IsTrue(m2.Pattern.IsPlus);
        }

        //[TestMethod]
        public void GenerateSimplifiedRegexes()
        {
            GenerateBatch("../../../../Automata.Tests/Samples_/regexesWithoutAnchors.txt", "Simpl", true);
            //GenerateBatch("../../../../Automata.Tests/Samples/regexes.txt", "Set2", true);
        }

        void GenerateBatch(string file, string batch_name, bool simplify)
        {
            Regex[] regexes = Array.ConvertAll(File.ReadAllLines(file), x => new Regex(x, RegexOptions.Singleline));
            List<string> regexes2 = new List<string>();
            HashSet<string> regexes2_ = new HashSet<string>();
            string dir = "../../../../Automata.Tests/Samples/";
            string dir_batch = dir + batch_name;
            if (!Directory.Exists(dir_batch))
                Directory.CreateDirectory(dir_batch);

            //Predicate<SymbolicRegexNode<ulong>> isNonMonadic = node =>
            //{
            //    return (
            //      node.kind == SymbolicRegexKind.Loop &&
            //      node.left.kind != SymbolicRegexKind.Singleton &&
            //      !node.IsStar &&
            //      !node.IsMaybe &&
            //      !node.IsPlus);
            //};

            //Predicate<SymbolicRegexNode<ulong>> isCountingLoop = node =>
            //{
            //    return (node.kind == SymbolicRegexKind.Loop &&
            //    !node.IsStar &&
            //      !node.IsMaybe &&
            //      !node.IsPlus);
            //};

            //int nrOfCountingLoops = 0;

            for (int i = 0; i < regexes.Length; i++)
            {
                var regex = regexes[i];
                RegexMatcher m = null;
                string reasonwhynot;
                if (regex.IsCompileSupported(out reasonwhynot))
                {
                    m = regex.Compile(true, false);
                    //if (m.Pattern.ExistsNode(isCountingLoop))
                    {
                        //nrOfCountingLoops += 1;
                        //bool monadic = !m.Pattern.ExistsNode(isNonMonadic);
                        //if (monadic)
                        {
                            var s = new FileStream(dir_batch + "/r" + (regexes2.Count).ToString() + ".soap", FileMode.Create);
                            if (simplify)
                                m.SerializeSimplified(s, new SoapFormatter());
                            else
                                m.Serialize(s, new SoapFormatter());
                            s.Close();
                            var s2 = new FileStream(dir_batch + "/r" + (regexes2.Count).ToString() + ".soap", FileMode.Open);
                            var m2 = (SymbolicRegexUInt64)new SoapFormatter().Deserialize(s2);
                            s2.Close();
                            //var aut = m2.Pattern.Explore();
                            var r2 = m2.Pattern.ToString();
                            if (regexes2_.Add(r2))
                            {
                                regexes2.Add(r2);
                            }
                        }
                    }
                }
            }
            File.WriteAllLines(dir + batch_name + ".txt", regexes2.ToArray());
        }

        public static void SerializeObjectToFile_bin(string file, object obj)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, obj);
            stream.Close();
        }

        public static void SerializeObjectToFile_soap(string file, object obj)
        {
            IFormatter formatter = new SoapFormatter();
            Stream stream = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, obj);
            stream.Close();
        }

        public static void SerializeObjectToFile_osf(string file, object obj)
        {
            IFormatter formatter = new System.Web.UI.ObjectStateFormatter();
            Stream stream = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, obj);
            stream.Close();
        }

        public static object DeserializeObjectFromFile_bin(string file)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None);
            object obj = formatter.Deserialize(stream);
            stream.Close();
            return obj;
        }

        public static object DeserializeObjectFromFile_soap(string file)
        {
            IFormatter formatter = new SoapFormatter();
            Stream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None);
            object obj = formatter.Deserialize(stream);
            stream.Close();
            return obj;
        }

        public static object DeserializeObjectFromFile_osf(string file)
        {
            IFormatter formatter = new System.Web.UI.ObjectStateFormatter();
            Stream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None);
            object obj = formatter.Deserialize(stream);
            stream.Close();
            return obj;
        }

        [TestMethod]
        public void TestSerialization_IgnoreCase_BugFix()
        {
            var regex1 = new System.Text.RegularExpressions.Regex(@"[aA][bB][cC]?[dD]"
                , System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
            var matcher1 = regex1.Compile();
            matcher1.Serialize("test.bin");
            var matcher2 = RegexMatcher.Deserialize("test.bin");

            string input = @"xabdx";

            var res1 = matcher1.Matches(input);
            var res2 = matcher2.Matches(input);

            Assert.IsTrue(res1[0].Item1 == 1 && res1[0].Item2 == 3);
            Assert.AreEqual(res1.Length, res2.Length);
            Assert.IsTrue(res1[0].Equals(res2[0]));
        }

        [TestMethod]
        public void TestSerialization_MatchCorrectSurrogatePair()
        {
            var regex1 = new System.Text.RegularExpressions.Regex(@"[\uD800-\uDBFF][\uDC00-\uDFFF]"
                , System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
            var matcher1 = regex1.Compile(); 
            matcher1.Serialize("surrogatepair.soap", new System.Runtime.Serialization.Formatters.Soap.SoapFormatter());
            var matcher2 = RegexMatcher.Deserialize("surrogatepair.soap", new System.Runtime.Serialization.Formatters.Soap.SoapFormatter());

            string input = "_\uD809\uDD03_";

            var res1 = matcher1.Matches(input);
            var res2 = matcher2.Matches(input);

            Assert.IsTrue(res1[0].Item1 == 1 && res1[0].Item2 == 2);
            Assert.AreEqual(res1.Length, res2.Length);
            Assert.IsTrue(res1[0].Equals(res2[0]));
        }

        [TestMethod]
        public void TestSerialization_MatchIncorrectSurrogatePair()
        {
            var regex1 = new System.Text.RegularExpressions.Regex(@"[\uDC00-\uDFFF][\uD800-\uDBFF]"
                , System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
            var matcher1 = regex1.Compile();
            matcher1.Serialize("badsurrogatepair.bin");
            var matcher2 = RegexMatcher.Deserialize("badsurrogatepair.bin");

            string input = "_\uDD03\uD809_";

            var res1 = matcher1.Matches(input);
            var res2 = matcher2.Matches(input);

            Assert.IsTrue(res1[0].Item1 == 1 && res1[0].Item2 == 2);
            Assert.AreEqual(res1.Length, res2.Length);
            Assert.IsTrue(res1[0].Equals(res2[0]));
        }

        [TestMethod]
        public void TestSerialization_comprehensive_IgnoreCase()
        {
            TestSerialization_comprehensive_(RegexOptions.Singleline | RegexOptions.IgnoreCase);
        }

        [TestMethod]
        public void TestSerialization_comprehensive()
        {
            TestSerialization_comprehensive_(RegexOptions.Singleline);
        }

        void TestSerialization_comprehensive_(RegexOptions options)
        {
            var regexesFile = "../../../../Automata.Tests/Samples/matcher_test_set.txt";
            var regexes = Array.ConvertAll(File.ReadAllLines(regexesFile), x => new Regex(x, options));
            int k = regexes.Length;
            for (int i = 0; i < k; i++)
            {
                var regex = regexes[i];
                RegexMatcher matcher;
                string reasonwhyfailed;
                if (regex.TryCompile(out matcher, out reasonwhyfailed))
                {
                    matcher.Serialize("tmp.bin");
                    var matcher_ = RegexMatcher.Deserialize("tmp.bin");
                    var input = matcher.GenerateRandomMatch();
                    var matches = matcher.Matches(input);
                    var matches_ = matcher_.Matches(input);
                    Assert.IsTrue(matches_.Length == matches.Length);
                    Assert.AreEqual(matches[0], matches_[0]);
                }
                else
                {
                    Assert.Fail("Regex compilation failed: " + reasonwhyfailed);
                }
            }
        }

        [TestMethod]
        public void TestCustomStringSerialization()
        {
            CustomSerializeDeserializeString("A\uD809\uDD03B");
            CustomSerializeDeserializeString("A\uDD03\uD809B");
            CustomSerializeDeserializeString("A\uDD03B\uD809");
            CustomSerializeDeserializeString("");
            CustomSerializeDeserializeString("1");
            CustomSerializeDeserializeString("1,2");
            CustomSerializeDeserializeString("\n\r");
        }

        public void CustomSerializeDeserializeString(string input)
        {
            var s = StringUtility.SerializeStringToCharCodeSequence(input);
            var d = StringUtility.DeserializeStringFromCharCodeSequence(s);
            Assert.AreEqual<string>(input, d);
        }

        const string customBase32alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        static string MkBase32String(int length)
        {
            var rnd = new Random(length);
            var s = new string(Array.ConvertAll(new char[length], c => customBase32alphabet[rnd.Next(0, 32)]));
            return s;
        }

        const string customalphabet65 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_/=";
        static string MkString65(int length)
        {
            var rnd = new Random(length);
            var s = new string(Array.ConvertAll(new char[length], c => customalphabet65[rnd.Next(0, 65)]));
            return s;
        }

        [TestMethod]
        public void TestSerialization_ShortStringAsRegex()
        {
            TestSerialization_StringAsRegex_soap("short", "short seq");
        }

        [TestMethod]
        public void TestSerialization_ShortStringAsRegex2()
        {
            TestSerialization_StringAsRegex_soap("shorta{0,3}", "short nonseq");
        }

        [TestMethod]
        public void TestSerialization_MediumStringAsRegex()
        {
            TestSerialization_StringAsRegex_soap(MkBase32String(100), "medium case32 seq");
        }

        [TestMethod]
        public void TestSerialization_MediumStringAsRegexWithBValphabet()
        {
            TestSerialization_StringAsRegex_soap(customalphabet65, "medium case65 seq");
        }

        [TestMethod]
        public void TestSerialization_LongStringAsRegex32()
        {
            TestSerialization_StringAsRegex_soap(MkBase32String(2018), "long case32 seq");
        }

        [TestMethod]
        public void TestSerialization_LongStringAsRegex65_sequence()
        {
            TestSerialization_StringAsRegex_soap(MkString65(2018), "long case65 seq");
        }

        [TestMethod]
        public void TestSerialization_LongStringAsRegex65_nonsequence()
        {
            //for (int i = 0; i < 5; i++)
            TestSerialization_StringAsRegex_soap(MkString65(2018) + "@{0,4}", "long case65 nonseq");
        }

        public void TestSerialization_StringAsRegex_soap(string regex, string info)
        {
            Console.WriteLine("-----------------------" + info + "-----------------------");
            var r = new Regex(regex, RegexOptions.IgnoreCase);
            var sr = CompileAndPrintStats(r, info);
            //    var str = sr.GenerateRandomMatch();
            //    var k = str.Length;
            //    var input = "---" + str + "---" + str + "---";
            //    var m1 = MatchAndPrintStats(sr, input, info + "BDD");
            //    Assert.IsTrue(m1.Length == 2);
            //    Assert.IsTrue(m1[0].Item1 == 3);
            //    Assert.IsTrue(m1[0].Item2 == k);
            //    Assert.IsTrue(m1[1].Item1 == 6 + k);
            //    Assert.IsTrue(m1[1].Item2 == k);
            //    //---
            //    SerializeAndPrintStats(sr, "");
            //    var t = System.Environment.TickCount;
            //    var sr2 = DeserializeAndPrintStats(info);
            //    var m2 = MatchAndPrintStats(sr2, input, info + "BV");
            //    AssertEquivalence(sr, sr2);
            //    Assert.IsTrue(m2.Length == 2);
            //    Assert.IsTrue(m2[0].Item1 == 3);
            //    Assert.IsTrue(m2[0].Item2 == k);
            //    Assert.IsTrue(m2[1].Item1 == 6 + k);
            //    Assert.IsTrue(m2[1].Item2 == k);
        }

        Tuple<int,int>[] MatchAndPrintStats(RegexMatcher matcher, string input, string info)
        {
            var t = System.Environment.TickCount;
            var m = matcher.Matches(input);
            t = System.Environment.TickCount - t;
            Console.WriteLine("{2}) Match count={0}, time={1}ms", m.Length, t, info);
            return m;
        }

        void SerializeAndPrintStats(RegexMatcher matcher, string info)
        {
            var t = System.Environment.TickCount;
            matcher.Serialize("test.soap", new SoapFormatter());
            t = System.Environment.TickCount - t;
            Console.WriteLine("{1} serializaton time={0}ms", t, info);
        }

        RegexMatcher DeserializeAndPrintStats(string info)
        {
            var t = System.Environment.TickCount;
            var rm = RegexMatcher.Deserialize("test.soap", new SoapFormatter());
            t = System.Environment.TickCount - t;
            Console.WriteLine("{1} deserializaton time={0}ms", t, info);
            return rm;
        }

        RegexMatcher CompileAndPrintStats(Regex regex, string info)
        {
            var t = System.Environment.TickCount;
            var rm = regex.Compile();
            t = System.Environment.TickCount - t;
            Console.WriteLine("{1} compilation time={0}ms", t, info);
            return rm;
        }

        static void AssertEquivalence(RegexMatcher sr1, RegexMatcher sr2)
        {
            if (sr1 is SymbolicRegexBV)
            {
                var sr1_ = sr1 as SymbolicRegexBV;
                var sr2_ = sr2 as SymbolicRegexBV;
                Assert.IsTrue(sr1_.Pattern.Equals(sr2_.Pattern));
                Assert.IsTrue(sr1_.ReversePattern.Equals(sr2_.ReversePattern));
                Assert.IsTrue(sr1_.DotStarPattern.Equals(sr2_.DotStarPattern));
            }
            else if (sr1 is SymbolicRegexUInt64)
            {
                var sr1_ = sr1 as SymbolicRegexUInt64;
                var sr2_ = sr2 as SymbolicRegexUInt64;
                Assert.IsTrue(sr1_.Pattern.Equals(sr2_.Pattern));
                Assert.IsTrue(sr1_.ReversePattern.Equals(sr2_.ReversePattern));
                Assert.IsTrue(sr1_.DotStarPattern.Equals(sr2_.DotStarPattern));
            }
        }

        [TestMethod]
        public void TestSerialization_BV2()
        {
            var sr = (SymbolicRegexBV)new Regex(customalphabet65 + ".*").Compile();
            var solver = sr.builder.solver;
            var atoms = solver.GetPartition();
            for (int i = 0; i < atoms.Length; i++)
            {
                var A = atoms[i];
                var A_str = solver.SerializePredicate(A);
                var A2 = solver.DeserializePredicate(A_str);
                Assert.AreEqual<BV>(A, A2);
            }
        }
    }
}
