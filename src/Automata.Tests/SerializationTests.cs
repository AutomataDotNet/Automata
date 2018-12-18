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
            var bv = new BV(100, 1, 1);
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
            var m1 = new Regex(@"\d+").Compile();
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
            var s1 = new FileStream("test.bin", FileMode.OpenOrCreate);
            new BinaryFormatter().Serialize(s1, m1);
            s1.Close();
            var s2 = new FileStream("test.bin", FileMode.Open);
            var m2 = (IMatcher)new BinaryFormatter().Deserialize(s2);
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
            var s1 = new FileStream("test.soap", FileMode.OpenOrCreate);
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

    }
}
