using System;
using System.Collections.Generic;


namespace Microsoft.Automata
{
    /// <summary>
    /// Represents a symbolic partition refinement of a set of elements. 
    /// Sets are not required to be finite but the set type must be associated with a Boolean algebra.
    /// </summary>
    /// <typeparam name="S">set type</typeparam>
    internal class SymbolicPartitionRefinement<S>
    {
        PartTree partitions;
        IBooleanAlgebra<S> solver;

        /// <summary>
        /// Construct a symbolic partition refinement for a given Boolean algebra over S and initial set of elements.
        /// </summary>
        /// <param name="solver">given Boolean algebra</param>
        /// <param name="initialSet">initial set of elements will be one part</param>
        public SymbolicPartitionRefinement(IBooleanAlgebra<S> solver, S initialSet)
        {
            this.solver = solver;
            this.partitions = new PartTree(initialSet, null, null);
        }

        /// <summary>
        /// Enumerates all individual parts of the partition. 
        /// </summary>
        public IEnumerable<S> GetRegions()
        {
            return partitions.GetRegions();
        }

        /// <summary>
        /// Refines the current partition with respect to the given set.
        /// The given set is not required be a subset of the initial set.
        /// </summary>
        /// <param name="B">given set</param>
        public void Refine(S B)
        {
            var A = partitions.set;
            if (!solver.AreEquivalent(A, B))
            {
                var B_minus_A = solver.MkAnd(B, solver.MkNot(A));

                if (solver.IsSatisfiable(B_minus_A)) //new elements are added to the total set
                {
                    var A_union_B = solver.MkOr(A, B);
                    var A_intersect_B = solver.MkAnd(A, B);
                    var A_minus_B = solver.MkAnd(A, solver.MkNot(B));

                    var left = new PartTree(B_minus_A, null, null);
                    this.partitions = new PartTree(A_union_B, left, this.partitions);
                    if (solver.IsSatisfiable(A_intersect_B) && solver.IsSatisfiable(A_minus_B))
                        this.partitions.right.Refine(solver, A_minus_B);
                }
                else // B is a subset of A
                {
                    partitions.Refine(solver, B);
                }
            }
        }

        class PartTree
        {
            internal PartTree left;
            internal PartTree right;
            internal S set;

            internal PartTree(S set, PartTree left, PartTree right)
            {
                this.set = set;
                this.left = left;
                this.right = right;
            }

            internal void Refine(IBooleanAlgebra<S> solver, S newSet)
            {
                var set_cap_newSet = solver.MkAnd(set, newSet);
                if (!solver.IsSatisfiable(set_cap_newSet))
                    return; //set is disjoint from newSet

                if (solver.AreEquivalent(set, set_cap_newSet))
                    return; //set is a subset of newSet

                var set_minus_newSet = solver.MkAnd(set, solver.MkNot(newSet));


                if (left == null) //leaf
                {
                    left = new PartTree(set_cap_newSet, null, null);
                    right = new PartTree(set_minus_newSet, null, null);
                }
                else
                {
                    left.Refine(solver, newSet);
                    right.Refine(solver, newSet);
                }
            }

            internal IEnumerable<S> GetRegions()
            {
                if (left == null)
                    yield return set;
                else
                {
                    foreach (var region in left.GetRegions())
                        yield return region;
                    foreach (var region in right.GetRegions())
                        yield return region;
                }
            }
        }
    }

    /// <summary>
    /// Used by the Miminization algorithm to compute partitions of the state space.
    /// </summary>
    internal class PartitionRefinement
    {
        BDDAlgebra solver;
        Dictionary<int, Part> elemToPart = new Dictionary<int, Part>();
        HashSet<Part> parts = new HashSet<Part>();
        Part initialPart;
        int maxbit = 0;

        internal IEnumerable<Part> GetParts()
        {
            return parts;
        }

        //public Part MkPart(IEnumerable<int> domain)
        //{
        //    return new Part(domain, solver, maxbit);
        //}

        /// <summary>
        /// Gets the initial part.
        /// </summary>
        public Part InitialPart
        {
            get { return initialPart; }
        }

        /// <summary>
        /// Creates a new partition refinement.
        /// </summary>
        /// <param name="domain">nonempty collection of nonnegative integers</param>
        /// <param name="maxState">must be the largest element in the domain</param>
        public PartitionRefinement(IEnumerable<int> domain, int maxState)
        {
            this.solver = new BDDAlgebra();
            int elemsSetSize = maxState;
            while (elemsSetSize > 1)
            {
                elemsSetSize = (elemsSetSize >> 1);
                maxbit += 1;
            }
            initialPart = new Part(domain, solver, maxbit);
            foreach (var elem in domain)
            {
                elemToPart[elem] = initialPart;
            }
            parts.Add(initialPart);
        }

        /// <summary>
        /// Refines the partition with respect to the given set of elements that must be 
        /// a subset of the original partition.
        /// Returns pairs [R0,R1] where R0 is the modified original partition and R1 is a nonempty subset..
        /// </summary>
        /// <param name="set">given set of elements, the enumeration must not have repetitions</param>
        /// <returns></returns>
        public IEnumerable<Part[]> Refine(IEnumerable<int> set)
        {
            var splits = new Dictionary<Part, Part>();
            foreach (var elem in set)
            {
                var part = elemToPart[elem];
                part.Remove(elem);
                Part split;
                if (!splits.TryGetValue(part, out split))
                {
                    split = new Part(elem, solver, maxbit);
                    splits[part] = split;
                }
                else
                    split.Add(elem);
            }
            foreach (var elem in set)
            {
                var part = elemToPart[elem];          
                if (part.IsEmpty)
                    part.Update(splits[part]);
                else 
                {
                    var split = splits[part];
                    if (!part.IsEquivalentTo(split))
                    {
                        elemToPart[elem] = split;
                        if (parts.Add(split))
                            yield return
                                new Part[] { part, split };
                    }
                }
            }
        }


        //public ICollection<Part> Parts
        //{
        //    get { return parts; }
        //}

        /// <summary>
        /// Returns the part that the given element belongs to.
        /// </summary>
        /// <param name="elem">given element</param>
        public Part  GetPart(int elem)
        {
            return elemToPart[elem];
        }

        public override string ToString()
        {
            string res = "{";
            foreach (var part in parts)
            {
                if (res != "{")
                    res += ",";
                res += "[" + part + "]";
            }
            res += "}";
            return res;
        }
    }

    /// <summary>
    /// Represents a single part of a partition of a finite set of integers. Used by the PartitionRefinement class.
    /// </summary>
    internal class Part
    {
        BDD elems;
        BDDAlgebra solver;
        int maxbit;

        /// <summary>
        /// Enumerates all the elements in this part.
        /// </summary>
        public IEnumerable<int> GetElements()
        {
            var ranges = solver.ToRanges(elems, maxbit);
            foreach (var range in ranges)
                for (uint i = range.Item1; i <= range.Item2; i += 1 )
                    yield return (int)i;
        }

        /// <summary>
        /// Constructs a new part with the given elements.
        /// </summary>
        internal Part(IEnumerable<int> elements, BDDAlgebra solver, int maxbit)
        {
            this.solver = solver;
            this.maxbit = maxbit;
            elems = solver.MkSetFromElements(elements, maxbit);
        }

        /// <summary>
        /// Constructs a new part with the given single element.
        /// </summary>
        internal Part(int element, BDDAlgebra solver, int maxbit)
        {
            this.solver = solver;
            this.maxbit = maxbit;
            elems = solver.MkSetFrom((uint)element, maxbit);
        }

        //Part(BvSet set, BvSetSolver solver, int maxbit)
        //{
        //    this.elems = set;
        //    this.solver = solver;
        //    this.maxbit = maxbit;
        //}

        /// <summary>
        /// Add the element to this part.
        /// </summary>
        /// <param name="elem">element to be added</param>
        public void Add(int elem)
        {
            elems = solver.MkOr(elems, solver.MkSetFrom((uint)elem, maxbit));
        }

        /// <summary>
        /// Remove the element from this part.
        /// </summary>
        /// <param name="elem">element to be removed</param>
        public void Remove(int elem)
        {
            elems =  solver.MkAnd(elems, solver.MkNot(solver.MkSetFrom((uint)elem, maxbit)));
        }

        /// <summary>
        /// Gets a fixed member of the part.
        /// </summary>
        public int Representative
        {
            get
            {
                return (int)solver.GetMin(elems);
            }
        }

        /// <summary>
        /// Gets the number of elements in this part.
        /// </summary>
        public int Size
        {
            get
            {
                return (int)solver.ComputeDomainSize(elems, maxbit);
            }
        }

        /// <summary>
        /// Returns true iff the part is empty.
        /// </summary>
        public bool IsEmpty
        {
            get { return elems.IsEmpty; }
        }

        public override string ToString()
        {
           string res = "";
           var ranges = solver.ToRanges(elems, maxbit);
           foreach (var range in ranges)
           {
               if (res != "")
                   res += ",";
               if (range.Item2 == range.Item1 + 1)
                   res += string.Format("{0},{1}", (int)range.Item1, (int)range.Item2);
               else if (range.Item2 != range.Item1)
                   res += string.Format("{0}..{1}", (int)range.Item1, (int)range.Item2);
               else 
                   res += string.Format("{0}", (int)range.Item1);
           }
           return res;
        }

        /// <summary>
        /// Updates the set of elements in this part to the set of the given part.
        /// </summary>
        /// <param name="part">given part</param>
        public void Update(Part part)
        {
            this.elems = part.elems;
        }

        /// <summary>
        /// Returns true iff this part and the given part contain the same elements.
        /// </summary>
        /// <param name="part">given part</param>
        public bool IsEquivalentTo(Part part)
        {
            return solver.AreEquivalent(elems, part.elems);
        }
    }
}
