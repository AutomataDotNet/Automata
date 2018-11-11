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
using System.Runtime.Serialization.Formatters.Binary;
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
            SerializeObjectToFile("dt.bin", dt);
            var dt_ = (DecisionTree)DeserializeObjectFromFile("dt.bin");
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
            SerializeObjectToFile("dt.bin", dt);
            var dt_ = (BooleanDecisionTree)DeserializeObjectFromFile("dt.bin");
            Assert.AreEqual<string>(dt.ToString(), dt_.ToString());
        }

        [TestMethod]
        public void TestSerialization_BV()
        {
            var bv = new BV(100, 1, 1);
            SerializeObjectToFile("bbv.bin", bv);
            var bv_ = (BV)DeserializeObjectFromFile("bbv.bin");
            Assert.AreEqual<string>(bv.ToString(), bv_.ToString());
        }

        [TestMethod]
        public void TestSerialization_BVAlgebra()
        {
            var css = new CharSetSolver();
            var regex = new Regex(@"[0-9]");
            var regex_bdd = regex.ConvertToSymbolicRegexBDD(css);
            var minterms = regex_bdd.ComputeMinterms();
            var bva = BVAlgebra.Create(css, minterms);
            SerializeObjectToFile("bva.bin", bva);
            var bva_ = (BVAlgebra)DeserializeObjectFromFile("bva.bin");
            Assert.AreEqual<int>(bva.atoms.Length, bva_.atoms.Length);
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
            var matcher = (SymbolicRegex<BV>)regex.Compile();
            matcher.Serialize("matcher.bin");
            var matcher_ = SymbolicRegex<BV>.Deserialize("matcher.bin");
        }

        [TestMethod]
        public void TestSerialization_StartAnchorBugFix()
        {
            var regex1 = new Regex(@"b|a{1,2}");
            var matcher1 = (SymbolicRegex<BV>)regex1.Compile();
            matcher1.Serialize("test1.bin");
            var matcher1_ = SymbolicRegex<BV>.Deserialize("test1.bin");
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




        static void SerializeObjectToFile(string file, object obj)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(file, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, obj);
            stream.Close();
        }

        static object DeserializeObjectFromFile(string file)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None);
            object obj = formatter.Deserialize(stream);
            stream.Close();
            return obj;
        }

    }
}
