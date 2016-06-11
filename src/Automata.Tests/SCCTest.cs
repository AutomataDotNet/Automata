using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Automata;

namespace Microsoft.Automata.Tests
{
    [TestClass]
    public class SCCTest
    {
        [TestMethod]
        public void SCCTest1()
        {
            CharSetSolver solver = new CharSetSolver();

            var aut = solver.Convert("^a(ab)*$").Determinize().Minimize();            

            var sccs = GraphAlgorithms.GetStronglyConnectedComponents(aut);
            List<int> total = new List<int>();
            foreach (var scc in sccs)
            {
                Console.WriteLine();
                foreach (var st in scc)
                {
                    total.Add(st);
                    Console.Write(st + ",");
                }
            }

            Assert.IsTrue(sccs.ToArray().Length == 2);
            Assert.IsTrue(total.Count == aut.StateCount);
        }

        
    }
}
