using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Automata.MSO.Mona;

namespace MSO.Tests
{
    [TestClass]
    public class MonaTests
    {
        [TestMethod]
        public void MonaTest1()
        {
            string input = @"
/*
header declares M2L-STR semantics 
(finite input strings)
*/
m2l-str;

var1 p, q, r;            # fo vars p, q and r
var2 $;                  # s0 var $
p = q + 1;               # p is the successor of q
p < r;                   # r is after p
p in $ & q in $;         # both p an q are in $
";
            Program pgm = MonaParser.Parse(input);
            Assert.AreEqual<int>(5, pgm.declarations.Count);
            Assert.AreEqual<Tokens>(Tokens.M2LSTR, pgm.header.Kind);
        }

        //[TestMethod]
        public void MonaTest2()
        {
            string input = @"
# Qe describes the valid indices of a queue
pred isWfQueue(var2 Qe) =
   all1 p: (p in Qe & p > 0  => p - 1 in Qe);

# isx holds if p contains an x
pred is0(var1 p, var2 Qe, Q1, Q2) = p in Qe & p notin Q1 & p notin Q2;
pred is1(var1 p, var2 Qe, Q1, Q2) = p in Qe & p notin Q1 & p in Q2;
pred is2(var1 p, var2 Qe, Q1, Q2) = p in Qe & p in Q1 & p notin Q2;
pred is3(var1 p, var2 Qe, Q1, Q2) = p in Qe & p in Q1 & p in Q2;
  
# lt compares the elements at positions p and q of a queue 
pred lt(var1 p, q, var2 Qe, Q1, Q2) =
    (is0(p, Qe, Q1, Q2) & ~is0(q, Qe, Q1, Q2)) 
  | (   is1(p, Qe, Q1, Q2)
     & (is2(q, Qe, Q1, Q2) | is3(q, Qe, Q1, Q2)))
  | (   is2(p, Qe, Q1, Q2)
     & (is3(q, Qe, Q1, Q2))); 

# isLast holds if p is the last element in the queue
pred isLast(var1 p, var2 Qe) =
  p in Qe & (all1 q': q' in Qe => q' <= p);  
  
# an ordered queue of length l 
pred Queue(var2 Qe, Q1, Q2, var1 l) =
   isLast(l - 1, Qe)
  &
   (all1 p, q: p<q & p in Qe & q in Qe => lt(p, q, Qe, Q1, Q2));

# eqQueue2 compares elements in two queues
pred eqQueue2(var1 p, q, var2 Q1, Q2, Q1', Q2') =
    (p in Q1 <=> q in Q1')
  & (p in Q2 <=> q in Q2');
 
# LooseOne holds about a queue Q and a queue Q' if
# queue Q' is the same as Q except that one
# element (denoted by p below) is removed
pred LooseOne(var2 Qe, Q1, Q2, Qe', Q1', Q2') =
  ex1 p: p in Qe
  &  (all1 q: (~isLast(q, Qe) => (q in Qe  <=> q in Qe'))
         & (isLast(q, Qe) => (q notin Qe')))
  &  (all1 q: q<p & q in Qe  => eqQueue2(q, q, Q1, Q2, Q1', Q2'))
  &  (all1 q: q > p & q in Qe  => eqQueue2(q, q - 1, Q1, Q2, Q1', Q2'));

var2 Qe, Q1, Q2;     # the queue Q
var2 Qe', Q1', Q2';  # the queue Q'

assert isWfQueue(Qe);

# the primed variables denote a queue of length 3 containing
# three of the elements 0, 1, 2, 3 in that order and the element 3
Queue(Qe, Q1, Q2, 4);   # the queue Q is a queue of length 3
                        # containing the elements 0, 1, 2, 3 
LooseOne(Qe, Q1, Q2, Qe', Q1', Q2'); # Q' is Q except for one element
ex1 p: is3(p, Qe', Q1', Q2'); # Q' does contain the element 3      
";
            Program pgm = MonaParser.Parse(input);
            Assert.IsTrue(pgm.declarations.Count > 0);
        }

        [TestMethod]
        public void MonaTestVar0decl()
        {
            string input = @"
var0 a, b ; # Boolean vars a and b
";
            Program pgm = MonaParser.Parse(input);
            Assert.AreEqual<int>(1, pgm.declarations.Count);
            Assert.AreEqual<string>("var0 a,b;", pgm.ToString().Trim());
        }

        [TestMethod]
        public void MonaTestVar0decl_error()
        {
            string input = @"
var1 a; # variable a 
var0 a; # variable a again
";
            try
            {
                Program pgm = MonaParser.Parse(input);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is MonaParseException);
                return;
            }
            Assert.IsTrue(false, "expected MonaParseException");
        }

        [TestMethod]
        public void MonaTestUnivDecl()
        {
            string input = @"
m2l-tree;
universe U1, U2:110101, U3:foo; # sample universes
";
            Program pgm = MonaParser.Parse(input);
            Assert.AreEqual<int>(1, pgm.declarations.Count);
            Assert.IsTrue(pgm.declarations[0] is UnivDecl);
            var univdecl = pgm.declarations[0] as UnivDecl;
            Assert.IsTrue(univdecl.args.Count == 3);
            Assert.IsTrue(univdecl.args[1] is UnivArgWithSucc);
            Assert.IsTrue(univdecl.args[2] is UnivArgWithType);
        }

        [TestMethod]
        public void MonaTestUnivDecl_error()
        {
            string input = @"
m2l-tree;
universe U1, U2:110201, U3:foo; # sample universes
";
            try
            {
                Program pgm = MonaParser.Parse(input);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is MonaParseException);
                return;
            }
            Assert.IsTrue(false, "expecting MonaParseException");
            return;
        }

        [TestMethod]
        public void MonaTestConstDecl()
        {
            string input = @"
const a = 23 + 2;
const b = a + (2 - a);
";
            Program pgm = MonaParser.Parse(input);
            Assert.AreEqual<int>(2, pgm.declarations.Count);
            Assert.IsTrue(pgm.declarations[0] is ConstDecl);
            Assert.IsTrue(pgm.declarations[1] is ConstDecl);
            var a = pgm.declarations[0] as ConstDecl;
            Assert.IsTrue(a.name.text == "a");
            var b = pgm.declarations[1] as ConstDecl;
            Assert.IsTrue(b.name.text == "b");
        }

        [TestMethod]
        public void MonaTestConstDecl_error()
        {
            string input = @"
const a = 23 + 2;
const b = a + (2 - c);
";
            try
            {
                Program pgm = MonaParser.Parse(input);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is MonaParseException);
                return;
            }
            Assert.IsTrue(false, "expecting MonaParseException");
            return;
        }

        [TestMethod]
        public void MonaTestDefaultWhereDecl()
        {
            string input = @"
defaultwhere1(p) = p < 10;
";
            Program pgm = MonaParser.Parse(input);
            Assert.AreEqual<int>(1, pgm.declarations.Count);
            Assert.IsTrue(pgm.declarations[0] is DefaultWhereDecl);
            Assert.IsFalse((pgm.declarations[0] as DefaultWhereDecl).isSecondOrder);
            Assert.IsTrue((pgm.declarations[0] as DefaultWhereDecl).kind == DeclKind.defaultwhere1);
        }

    }
}
