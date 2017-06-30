using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.Fast;
using Microsoft.Fast.AST;
using Microsoft.Automata.Z3;
using Microsoft.Z3;

namespace Fast.Tests
{
    [TestClass]
    public class FastTests
    {

        static string srcDirectory = "../../../../../src/";

        [TestMethod]
        public void TestConstants()
        {
            var pgm = Parser.ParseFromFile(srcDirectory + "Fast.Tests/FastSamples/constants.fast");

            FastTransducerInstance fti = FastTransducerInstance.MkFastTransducerInstance(pgm);

            FastTransducerInstance.DisposeZ3P();

            Assert.IsTrue(pgm.defs.Count == 8);

            StringBuilder sb = new StringBuilder();

            //fti.ToFast(sb);

            //pgm = Parser.ParseFromString(sb.ToString());

            //bool b = CsharpGenerator.GenerateCode(pgm, "../../../src/Fast.Tests/GeneratedCS/constants.cs", "Microsoft.Fast.Tests.Constants");            

        }


        [TestMethod]
        public void TestBodytree()
        {
            var pgm = Parser.ParseFromFile(srcDirectory + "Fast.Tests/FastSamples/bodytree.fast");

            FastTransducerInstance fti = FastTransducerInstance.MkFastTransducerInstance(pgm);

            FastTransducerInstance.DisposeZ3P();

        }

        [TestMethod]
        public void TestHtmlSanitizer()
        {
            var pgm = Parser.ParseFromFile(srcDirectory + "Fast.Tests/FastSamples/htmlSanitizer.fast");

            FastTransducerInstance fti = FastTransducerInstance.MkFastTransducerInstance(pgm);

            FastTransducerInstance.DisposeZ3P();

        }

        [TestMethod]
        public void TestSample()
        {
            var pgm = Parser.ParseFromFile(srcDirectory + "Fast.Tests/FastSamples/sample.fast");

            FastTransducerInstance fti = FastTransducerInstance.MkFastTransducerInstance(pgm);

            FastTransducerInstance.DisposeZ3P();

        }

        [TestMethod]
        public void TestRecognizers()
        {
            var pgm = Parser.ParseFromFile(srcDirectory + "Fast.Tests/FastSamples/recognizers.fast");

            FastTransducerInstance fti = FastTransducerInstance.MkFastTransducerInstance(pgm);

            FastTransducerInstance.DisposeZ3P();

        }

        [TestMethod]
        public void TestOperators()
        {
            var pgm = Parser.ParseFromFile(srcDirectory + "Fast.Tests/FastSamples/operators.fast");

            FastTransducerInstance fti = FastTransducerInstance.MkFastTransducerInstance(pgm);

            FastTransducerInstance.DisposeZ3P();
        }

        [TestMethod]
        public void TestFunctions()
        {
            var pgm = Parser.ParseFromFile(srcDirectory + "Fast.Tests/FastSamples/functions.fast");

            Assert.IsTrue(pgm.defs.Count == 6);

            FastTransducerInstance fti = FastTransducerInstance.MkFastTransducerInstance(pgm);

            StringBuilder sb = new StringBuilder();

            fti.ToFast(sb);

            //TBD: ToFast produces code in old prefix notation

            //pgm = Parser.ParseFromString(sb.ToString());

            //bool b = CsharpGenerator.GenerateCode(pgm, "../../../src/Fast.Tests/GeneratedCS/functions.cs", "Microsoft.Fast.Tests.Functions");

            Assert.IsTrue(pgm.defs.Count == 6);
        }

        //[TestMethod]
        //public void TestSlideEx()
        //{
        //    var pgm = FastParser.ParseFromFile("../../../src/Fast.Tests/FastSamples/exforslides.fast");
            
        //    FastTransducerInstance fti = FastTransducerInstance.MkFastTransducerInstance(pgm);

        //    StringBuilder sb = new StringBuilder();

        //    fti.ToFast(sb);

        //    pgm = FastParser.ParseFromString(sb.ToString());

        //    bool b = CsharpGenerator.GenerateCode(pgm, "../../../src/Fast.Tests/GeneratedCS/exforslides.cs", "Microsoft.Fast.Tests.Functions");
        //}

        [TestMethod]
        public void TestComposition()
        {
            var pgm = Parser.ParseFromFile(srcDirectory + "Fast.Tests/FastSamples/compose.fast");

            FastTransducerInstance fti = FastTransducerInstance.MkFastTransducerInstance(pgm);

            //Assert.IsTrue(fti.treeDefinitions["A"].Transducers.Count == 2);

            //StringBuilder sb = new StringBuilder();

            //fti.ToFast(sb);

            //pgm = Parser.ParseFromString(sb.ToString());

            //bool b = CsharpGenerator.GenerateCode(pgm, "../../../src/Fast.Tests/GeneratedCS/compose.cs", "Microsoft.Fast.Tests.Composition");

        }

        [TestMethod]
        public void TestCompositionAsserts()
        {
            var pgm = Parser.ParseFromFile(srcDirectory + "Fast.Tests/FastSamples/compose1.fast");

            FastTransducerInstance fti = FastTransducerInstance.MkFastTransducerInstance(pgm);

            Assert.IsTrue(fti.treeDefinitions["C"].trees["t_112"].SequenceEqual(fti.treeDefinitions["C"].trees["t_12"]));
            Assert.IsTrue(fti.treeDefinitions["C"].trees["t_212"].SequenceEqual(fti.treeDefinitions["C"].trees["t_22"]));

            //StringBuilder sb = new StringBuilder();

            //fti.ToFast(sb);

            //pgm = Parser.ParseFromString(sb.ToString());            

        }

        [TestMethod]
        public void TestWitGen()
        {
            var pgm = Parser.ParseFromFile(srcDirectory + "Fast.Tests/FastSamples/witness.fast");

            FastTransducerInstance fti = FastTransducerInstance.MkFastTransducerInstance(pgm);

            StringBuilder sb = new StringBuilder();

            fti.ToFast(sb);

            pgm = Parser.ParseFromString(sb.ToString());

            //bool b = CsharpGenerator.GenerateCode(pgm, "../../../src/Fast.Tests/GeneratedCS/trees.cs", "Microsoft.Fast.Tests.Trees");
        }

        [TestMethod]
        public void TestTypechecking()
        {
            var pgm = Parser.ParseFromFile(srcDirectory + "Fast.Tests/FastSamples/typechecking.fast");

            FastTransducerInstance fti = FastTransducerInstance.MkFastTransducerInstance(pgm);

            //StringBuilder sb = new StringBuilder();

            //fti.ToFast(sb);

            //pgm = Parser.ParseFromString(sb.ToString());
        }

        [TestMethod]
        public void TestPreimage()
        {
            var pgm = Parser.ParseFromFile(srcDirectory + "Fast.Tests/FastSamples/preimage.fast");

            FastTransducerInstance fti = FastTransducerInstance.MkFastTransducerInstance(pgm);

            //StringBuilder sb = new StringBuilder();

            //fti.ToFast(sb);

            //pgm = Parser.ParseFromString(sb.ToString());
        }

        [TestMethod]
        public void TestTrees()
        {
            var pgm = Parser.ParseFromFile(srcDirectory + "Fast.Tests/FastSamples/trees.fast");

            FastTransducerInstance fti = FastTransducerInstance.MkFastTransducerInstance(pgm);

            //StringBuilder sb = new StringBuilder();

            //fti.ToFast(sb);

            //pgm = Parser.ParseFromString(sb.ToString());

            //bool b = CsharpGenerator.GenerateCode(pgm, "../../../src/Fast.Tests/GeneratedCS/trees.cs", "Microsoft.Fast.Tests.Trees");

        }

        [TestMethod]
        public void TestIntersection()
        {
            var pgm = Parser.ParseFromFile(srcDirectory + "Fast.Tests/FastSamples/intersect.fast");

            FastTransducerInstance fti = FastTransducerInstance.MkFastTransducerInstance(pgm);

            //var t1 = fti.treeDefinitions["A"].trees["t_1"];
            //var t2 = fti.treeDefinitions["A"].trees["t_2"];
            //var t12 = fti.treeDefinitions["A"].trees["t_12"];
            //var tnot = fti.treeDefinitions["A"].trees["t_not"];

            //var q1 = fti.treeDefinitions["A"].acceptors["q1"];
            //var r1 = fti.treeDefinitions["A"].acceptors["r1"];
            //var inters = fti.treeDefinitions["A"].acceptors["intersect_q1r1"];

            //Assert.IsTrue(q1.Apply(t12).SequenceEqual(inters.Apply(t12)));
            //Assert.IsTrue(r1.Apply(t12).SequenceEqual(inters.Apply(t12)));
            //Assert.IsFalse(q1.Apply(t1).SequenceEqual(inters.Apply(t1)));
            //Assert.IsTrue(r1.Apply(t1).SequenceEqual(inters.Apply(t1)));
            //Assert.IsTrue(q1.Apply(t2).SequenceEqual(inters.Apply(t2)));
            //Assert.IsFalse(r1.Apply(t2).SequenceEqual(inters.Apply(t2)));
            //Assert.IsTrue(q1.Apply(tnot).SequenceEqual(inters.Apply(tnot)));
            //Assert.IsTrue(r1.Apply(tnot).SequenceEqual(inters.Apply(tnot)));

            //StringBuilder sb = new StringBuilder();

            //fti.ToFast(sb);

            //pgm = Parser.ParseFromString(sb.ToString());

            //bool b = CsharpGenerator.GenerateCode(pgm, "../../../src/Fast.Tests/GeneratedCS/intersect.cs", "Microsoft.Fast.Tests.Intersection");
        }

        [TestMethod]
        public void TestUnion()
        {
            var pgm = Parser.ParseFromFile(srcDirectory + "Fast.Tests/FastSamples/union.fast");

            FastTransducerInstance fti = FastTransducerInstance.MkFastTransducerInstance(pgm);

            var t1 = fti.treeDefinitions["A"].trees["t_1"];
            var t2 = fti.treeDefinitions["A"].trees["t_2"];
            var t12 = fti.treeDefinitions["A"].trees["t_12"];
            var tnot = fti.treeDefinitions["A"].trees["t_not"];

            var q1 = fti.treeDefinitions["A"].acceptors["q1"];
            var r1 = fti.treeDefinitions["A"].acceptors["r1"];
            var union = fti.treeDefinitions["A"].acceptors["union_q1r1"];

            

            //StringBuilder sb = new StringBuilder();

            //fti.ToFast(sb);
            //var pgm2 = Parser.ParseFromString(sb.ToString());


            //Assert.IsTrue(q1.Apply(t12).SequenceEqual(union.Apply(t12)));
           // Assert.IsTrue(r1.Apply(t12).SequenceEqual(union.Apply(t12)));
            //Assert.IsTrue(q1.Apply(t1).SequenceEqual(union.Apply(t1)));


            var res1 = new Microsoft.Automata.Sequence<Expr>(r1.Apply(t1));
            Assert.IsTrue(res1.Length == 0);

            var res2 = new Microsoft.Automata.Sequence<Expr>(union.Apply(t1));
            Assert.IsTrue(res2.Length == 1);




            Assert.IsFalse(q1.Apply(t2).SequenceEqual(union.Apply(t2)));


            Assert.IsTrue(r1.Apply(t2).SequenceEqual(union.Apply(t2)));
            Assert.IsTrue(q1.Apply(tnot).SequenceEqual(union.Apply(tnot)));
            Assert.IsTrue(r1.Apply(tnot).SequenceEqual(union.Apply(tnot)));


            //bool b = CsharpGenerator.GenerateCode(pgm, "../../../src/Fast.Tests/GeneratedCS/union.cs", "Microsoft.Fast.Tests.Intersection");
        }


        [TestMethod]
        public void TestRestriction()
        {
            var pgm = Parser.ParseFromFile(srcDirectory + "Fast.Tests/FastSamples/restrict.fast");

            FastTransducerInstance fti = FastTransducerInstance.MkFastTransducerInstance(pgm);

            Assert.IsTrue(fti.treeDefinitions["A"].trees["t_11"].SequenceEqual(fti.treeDefinitions["A"].trees["t_12"]));
            Assert.IsFalse(fti.treeDefinitions["A"].trees["t_21"].SequenceEqual(fti.treeDefinitions["A"].trees["t_22"]));
            Assert.IsTrue(fti.treeDefinitions["A"].trees["t_31"].SequenceEqual(fti.treeDefinitions["A"].trees["t_32"]));
            Assert.IsFalse(fti.treeDefinitions["A"].trees["t_41"].SequenceEqual(fti.treeDefinitions["A"].trees["t_42"]));

            //StringBuilder sb = new StringBuilder();

            //fti.ToFast(sb);

            //pgm = Parser.ParseFromString(sb.ToString());

            //bool b = CsharpGenerator.GenerateCode(pgm, "../../../src/Fast.Tests/GeneratedCS/restrict.cs", "Microsoft.Fast.Tests.Restriction");

        }



        [TestMethod]
        public void TestChars()
        {
            var pgm = Parser.ParseFromFile(srcDirectory + "Fast.Tests/FastSamples/shiftchars.fast");

            FastTransducerInstance fti = FastTransducerInstance.MkFastTransducerInstance(pgm);
            Assert.AreEqual<int>(fti.treeDefinitions["STR"].transducers.Count, 1);
            var ta = fti.treeDefinitions["STR"].transducers["caesar1"];
            Assert.AreEqual<int>(2, ta.StateCount);
            var da = ta.ComputeDomainAcceptor();
            var sfa = da.ConvertToSFA();
            //sfa.ShowGraph();
            var aut = sfa.Concretize(100);
            //sfa.Solver.CharSetProvider.ShowGraph(aut, "test");
        }

        [TestMethod]
        public void TestEquivalence()
        {
            var pgm = Parser.ParseFromFile(srcDirectory + "Fast.Tests/FastSamples/equivalence.fast");

            //Build transducers
            FastTransducerInstance fti = FastTransducerInstance.MkFastTransducerInstance(pgm);

            //Compile to fast and back
            //StringBuilder sb = new StringBuilder();
            //fti.ToFast(sb);
            //pgm = Parser.ParseFromString(sb.ToString());
        }

        //[TestMethod]
        public void TestMinimization()
        {
            var pgm = Parser.ParseFromFile("../../../src/Fast.Tests/FastSamples/minimization.fast");

            //Build transducers
            FastTransducerInstance fti = FastTransducerInstance.MkFastTransducerInstance(pgm);

            //Compile to fast and back
            //StringBuilder sb = new StringBuilder();
            //fti.ToFast(sb);
            //pgm = Parser.ParseFromString(sb.ToString());
        }

        [TestMethod]
        public void TestComplement()
        {
            var pgm = Parser.ParseFromFile(srcDirectory + "Fast.Tests/FastSamples/complement.fast");

            //Build transducers
            FastTransducerInstance fti = FastTransducerInstance.MkFastTransducerInstance(pgm);

            Assert.AreEqual<int>(1, fti.alphabets.Count);
            Assert.AreEqual<int>(1, fti.treeDefinitions.Count);
            Assert.AreEqual<int>(1, fti.queryRes.Count);



            //Compile to fast and back
            //StringBuilder sb = new StringBuilder();
            //fti.ToFast(sb);
            //pgm = Parser.ParseFromString(sb.ToString());
        }

        [TestMethod]
        public void TestCycletree()
        {
            var pgm = Parser.ParseFromFile(srcDirectory + "Fast.Tests/FastSamples/cycletree.fast");
            FastTransducerInstance fti = FastTransducerInstance.MkFastTransducerInstance(pgm);
            Assert.AreEqual<int>(1, fti.queryRes.Count);
        }

        [TestMethod]
        public void TestDomain()
        {
            var pgm = Parser.ParseFromFile(srcDirectory + "Fast.Tests/FastSamples/domain.fast");

            FastTransducerInstance fti = FastTransducerInstance.MkFastTransducerInstance(pgm);

            //Assert.IsTrue(fti.treeDefinitions["A"].trees["t_11"].SequenceEqual(fti.treeDefinitions["A"].trees["t_12"]));
            //Assert.IsTrue(fti.treeDefinitions["A"].trees["t_21"].SequenceEqual(fti.treeDefinitions["A"].trees["t_22"]));

            //StringBuilder sb = new StringBuilder();

            //fti.ToFast(sb);

            //pgm = Parser.ParseFromString(sb.ToString());

            //bool b = CsharpGenerator.GenerateCode(pgm, "../../../src/Fast.Tests/GeneratedCS/domain.cs", "Microsoft.Fast.Tests.Domain");

        }

        //[TestMethod]
        //public void TestCSharpGenerationFromFast()
        //{
        //    var pgm = FastParser.ParseFromFile("../../../src/Fast.Tests/FastSamples/bodytree.fast");

        //    bool b = CsharpGenerator.GenerateCode(pgm, "../../../src/Fast.Tests/GeneratedCS/bodytree.cs", "Microsoft.Fast.Tests.BodyTree");

        //    Assert.IsTrue(b);
        //}

        //[TestMethod]
        //public void TestTransducerGeneration()
        //{
        //    var pgm = FastParser.ParseFromFile("../../../src/Fast.Tests/FastSamples/bodytree.fast");
        //    int alphNumber = 0, TransNumber = 0, langNumber = 0, enumNumber = 0;
        //    foreach (var v in pgm.defs)
        //    {
        //        if (v.kind == DefKind.Lang && ((LangDef)v).isPublic)
        //            langNumber++;
        //        else if (v.kind == DefKind.Trans && ((TransDef)v).isPublic)
        //            TransNumber++;
        //        else if(v.kind==DefKind.Alphabet)
        //            alphNumber++;
        //        else if(v.kind==DefKind.Enum)
        //            enumNumber++;
        //        else if(v.kind==DefKind.Def && ((DefDef)v).ddkind == DefDefKind.Trans)
        //            TransNumber++;
        //        else if(v.kind==DefKind.Def && ((DefDef)v).ddkind == DefDefKind.Lang)
        //            langNumber++;
        //    }

        //    FastTransducerInstance fti = FastTransducerInstance.MkFastTransducerInstance(pgm);

        //    int computedTransNumber=0, computedLangNumber=0;

        //    Assert.IsTrue(fti.Alphabets.Count==alphNumber);
        //    Assert.IsTrue(fti.enums.Count==enumNumber);

        //    foreach (var v in fti.treeDefinitions)
        //    {
        //        computedLangNumber += v.Value.acceptors.Count;
        //        computedTransNumber += v.Value.Transducers.Count;
        //    }

        //    Assert.IsTrue(computedTransNumber == TransNumber);
        //    Assert.IsTrue(computedLangNumber == langNumber);    
        //}

        //[TestMethod]
        //public void TestRoundTripWithouthCSharpGen()
        //{
        //    String filename = "bodytree";
        //    var pgm = FastParser.ParseFromFile("../../../src/Fast.Tests/FastSamples/" + filename + ".fast");

        //    int alphNumber = 0, TransNumber = 0, langNumber = 0, enumNumber = 0;
        //    foreach (var v in pgm.defs)
        //    {
        //        if (v.kind == DefKind.Lang && ((LangDef)v).isPublic)
        //            langNumber++;
        //        else if (v.kind == DefKind.Trans && ((TransDef)v).isPublic)
        //            TransNumber++;
        //        else if (v.kind == DefKind.Alphabet)
        //            alphNumber++;
        //        else if (v.kind == DefKind.Enum)
        //            enumNumber++;
        //        else if (v.kind == DefKind.Def && ((DefDef)v).ddkind == DefDefKind.Trans)
        //            TransNumber++;
        //        else if (v.kind == DefKind.Def && ((DefDef)v).ddkind == DefDefKind.Lang)
        //            langNumber++;
        //    }

        //    FastTransducerInstance fti = FastTransducerInstance.MkFastTransducerInstance(pgm);

        //    StringBuilder sb = new StringBuilder();
        //    fti.ToFast(sb);

        //    pgm = FastParser.ParseFromString(sb.ToString());

        //    int newAlphNumber = 0, newTransNumber = 0, newLangNumber = 0, newEnumNumber = 0;
        //    foreach (var v in pgm.defs)
        //    {
        //        if (v.kind == DefKind.Lang && ((LangDef)v).isPublic)
        //            newLangNumber++;
        //        else if (v.kind == DefKind.Trans && ((TransDef)v).isPublic)
        //            newTransNumber++;
        //        else if (v.kind == DefKind.Alphabet)
        //            newAlphNumber++;
        //        else if (v.kind == DefKind.Enum)
        //            newEnumNumber++;
        //        else if (v.kind == DefKind.Def && ((DefDef)v).ddkind == DefDefKind.Trans)
        //            newTransNumber++;
        //        else if (v.kind == DefKind.Def && ((DefDef)v).ddkind == DefDefKind.Lang)
        //            newLangNumber++;
        //    }

        //    Assert.IsTrue(newTransNumber == TransNumber);
        //    Assert.IsTrue(newLangNumber == langNumber);
        //    Assert.IsTrue(newAlphNumber == alphNumber);
        //    Assert.IsTrue(newEnumNumber == enumNumber);  
            
        //}

        //[TestMethod]
        //public void TestRoundTrip()
        //{
        //    String filename = "bodytree";
        //    var pgm = FastParser.ParseFromFile("../../../src/Fast.Tests/FastSamples/" + filename + ".fast");

        //    FastTransducerInstance fti = FastTransducerInstance.MkFastTransducerInstance(pgm);

        //    StringBuilder sb = new StringBuilder();
        //    fti.ToFast(sb);

        //    System.IO.File.WriteAllText(@"C:\tmp\fast.fast", sb.ToString());

        //    pgm = FastParser.ParseFromString(sb.ToString());

        //    bool b = CsharpGenerator.GenerateCode(pgm, "../../../src/Fast.Tests/GeneratedCS/" + filename + ".cs", "Microsoft.Fast.Tests.BodyTree");

        //    Assert.IsTrue(true);

        //}

        [TestMethod]
        public void TestFastGeneration()
        {
            Z3Provider Z = new Z3Provider();

            Sort color = Z.MkEnumSort("Color", "blue", "green", "red");

            string enum_sort_name = color.Name.ToString();
            Assert.AreEqual<string>("Color", enum_sort_name);
            Assert.AreEqual<string>("green", Z.GetEnumElement("Color", "green").FuncDecl.Name.ToString());

            FuncDecl[] fields = new FuncDecl[5];
            FuncDecl mkTuple;
            Sort attrSort = Z.MkTupleSort("$", new string[] { "i", "b", "e", "s", "r" }, new Sort[] { Z.IntSort, Z.BoolSort, color, Z.StringSort, Z.RealSort }, out mkTuple, out fields);


            string tuple_sort_name = attrSort.Name.ToString();
            string tuple_contructor_name = mkTuple.Name.ToString();

            Assert.AreEqual<string>("$", tuple_sort_name);
            Assert.AreEqual<string>("$", tuple_contructor_name);

            Assert.AreEqual<string>("i", fields[0].Name.ToString());
            Assert.AreEqual<string>("b", fields[1].Name.ToString());
            Assert.AreEqual<string>("e", fields[2].Name.ToString());


            Assert.AreEqual<string>("Int", Z.GetRange(fields[0]).Name.ToString());
            Assert.AreEqual<string>("Bool", Z.GetRange(fields[1]).Name.ToString());
            Assert.AreEqual<string>("Color", Z.GetRange(fields[2]).Name.ToString());

            var A = (Z.TT.MkRankedAlphabet("A", attrSort, new string[] { "zero", "one", "two" }, new int[] { 0, 1, 2 }));


            Expr _i_plus_1 = Z.MkApp(mkTuple, Z.MkAdd(Z.MkProj(0, A.AttrVar), Z.MkInt(1)), Z.True,
                Z.MkIte(Z.MkGe(Z.MkProj(0, A.AttrVar), Z.MkInt(4)), Z.GetEnumElement("Color", "green"), Z.GetEnumElement("Color", "blue")), Z.MkProj(3, A.AttrVar), Z.MkAdd(Z.MkProj(4, A.AttrVar), Z.MkNumeral("9/3", Z.RealSort)));

            Expr _i_plus_1_foo = Z.MkApp(mkTuple, Z.MkAdd(Z.MkProj(0, A.AttrVar), Z.MkInt(1)), Z.True,
    Z.MkIte(Z.MkGe(Z.MkProj(0, A.AttrVar), Z.MkInt(4)), Z.GetEnumElement("Color", "green"), Z.GetEnumElement("Color", "blue")), Z.MkListFromString("foo", Z.CharacterSort), Z.MkNumeral("5.06", Z.RealSort));

            var proj = Z.GetTupleField(attrSort, 0);
            var proj_term = Z.MkApp(proj, _i_plus_1);

            var proj_term2 = Z.MkProj(0, _i_plus_1);


            var r1 = Z.TT.MkTreeRule(A, A, 0, "two", Z.MkGe(Z.MkProj(0, A.AttrVar), Z.MkInt(2)),
                A.MkTree("two", _i_plus_1, A.MkTree("one", _i_plus_1, A.MkTrans(A, 0, 1)),
                                           A.MkTree("two", _i_plus_1, A.MkTrans(A, 0, 2), A.MkTrans(A, 1, 2))));

            var r2 = Z.TT.MkTreeRule(A, A, 1, "two", Z.MkLe(Z.MkProj(0, A.AttrVar), Z.MkInt(5)),
               A.MkTree("two", _i_plus_1, A.MkTree("one", _i_plus_1, A.MkTrans(A, 0, 1)),
                                          A.MkTree("two", _i_plus_1, A.MkTrans(A, 0, 1), A.MkTrans(A, 1, 2))));

            var r3 = Z.TT.MkTreeRule(A, A, 1, "one", Z.True, A.MkTree("zero", _i_plus_1));

            var r4 = Z.TT.MkTreeRule(A, A, 0, "one", Z.True, A.MkTree("zero", _i_plus_1_foo));
            var r5 = Z.TT.MkTreeRule(A, A, 0, "zero", Z.True, A.MkTree("zero", _i_plus_1_foo));

            var T = Z.TT.MkTreeAutomaton(0, A, A, new TreeRule[] { r1, r2, r3, r4, r5 });

            var D = T.ComputeDomainAcceptor();

            var sb = new StringBuilder();
            var fastgen = new FastGen(Z);
            fastgen.ToFast(enum_sort_name, sb);
            fastgen.ToFast(A, sb);
            fastgen.ToFast("A", T, sb, false);
            fastgen.GetStateName = (x => "p_" + x);
            fastgen.ToFast("A", D, sb, true);

            Console.WriteLine(sb.ToString());
        }

        [TestMethod]
        public void TestApply()
        {
            var pgm = Parser.ParseFromFile(srcDirectory + "Fast.Tests/FastSamples/apply.fast");

            FastTransducerInstance fti = FastTransducerInstance.MkFastTransducerInstance(pgm);

            Assert.IsTrue(fti.treeDefinitions["A"].transducers.Count == 1);

            //StringBuilder sb = new StringBuilder();

            //fti.ToFast(sb);

            var q = new List<TreeTransducer>(fti.treeDefinitions["A"].transducers.Values)[0];

            var A = q.InputAlphabet;

            Func<Expr, Expr, Expr> two = (x, y) => A.MkTree("two", A.MkAttr(), x, y);
            Func<Expr, Expr> one = x => A.MkTree("one", A.MkAttr(), x);
            Expr zero = A.MkTree("zero", A.MkAttr());

            var t1 = two(one(one(one(two(zero, zero)))), one(one(one(zero))));
            var t1_out_expected = one(zero);

            var t2 = two(one(one(one(zero))), one(one(one(two(zero, zero)))));

            var t1_out = q[t1];
            var t2_out = q[t2];

            Assert.AreEqual<int>(1, t1_out.Length);
            Assert.AreEqual<Expr>(t1_out_expected, t1_out[0]);

            Assert.AreEqual<int>(0, t2_out.Length,"t2 is not accepted because the right subtree contains twos");
        }


        [TestMethod]
        public void TestNondet()
        {
            var pgm = Parser.ParseFromFile(srcDirectory + "Fast.Tests/FastSamples/nondet.fast");

            FastTransducerInstance fti = FastTransducerInstance.MkFastTransducerInstance(pgm);

            Assert.IsTrue(fti.treeDefinitions["A"].acceptors.Count == 1);

            //StringBuilder sb = new StringBuilder();

            //fti.ToFast(sb);

            //var q = new List<TreeAutomaton>(fti.treeDefinitions["A"].transducers.Values)[0];

            //var A = q.InputAlphabet;

            //var T = A.MkAttr(A.TT.Z.True);
            //Func<Expr, Expr> one = x => A.MkTree("one", T, x);
            //Expr zero = A.MkTree("zero", T);

            //var t1 = one(one(one(zero)));
            //var t1s = q[t1];
            //var t2 = one(one(one(one(zero))));
            //var t2s = q[t2];

            //Assert.AreEqual<int>(8, t1s.Length);
            //Assert.AreEqual<int>(16, t2s.Length);
        }

        //[TestMethod]
        public void TestNondetCS()
        {
            var pgm = Parser.ParseFromFile(srcDirectory + "Fast.Tests/FastSamples/nondetcs.fast");

            FastTransducerInstance fti = FastTransducerInstance.MkFastTransducerInstance(pgm);

            Assert.IsTrue(fti.treeDefinitions["A"].transducers.Count == 1);

            StringBuilder sb = new StringBuilder();

            fti.ToFast(sb);

            var q = new List<TreeTransducer>(fti.treeDefinitions["A"].transducers.Values)[0];

            var A = q.InputAlphabet;

            var T = A.MkAttr(A.TT.Z.True);
            Func<Expr, Expr> one = x => A.MkTree("one", T, x);
            Expr zero = A.MkTree("zero", T);

            var t1 = one(one(one(zero)));
            var t1s = q[t1];
            var t2 = one(one(one(one(zero))));
            var t2s = q[t2];

            Assert.AreEqual<int>(8, t1s.Length);
            Assert.AreEqual<int>(16, t2s.Length);
        }


        [TestMethod]
        public void AAAtest()
        {
            var pgm = Parser.ParseFromFile(srcDirectory + "Fast.Tests/FastSamples/aaa.fast");

            FastTransducerInstance fti = FastTransducerInstance.MkFastTransducerInstance(pgm);

            Assert.AreEqual<int>(5, fti.treeDefinitions["Node"].transducers.Count);
            Assert.AreEqual<int>(3, fti.treeDefinitions["Node"].acceptors.Count);
            Assert.AreEqual<int>(1, fti.alphabets.Count);
            Assert.AreEqual<int>(2, fti.queryRes.Count);
        }
    }
}
