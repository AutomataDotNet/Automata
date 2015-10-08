using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Z3;

namespace Microsoft.Automata.Z3.Internal
{
    /// <summary>
    /// Encapsulates the Z3 declarations related to the algebraic datatype of parse trees
    /// </summary>
    internal class UnrankedTreeInfo
    {
        public readonly Sort TreeListSort;
        public readonly FuncDecl GetNodeLabel, GetNodeSubtrees, MkNode, MkLeaf, GetLeafValue, IsNode, IsLeaf;
        public readonly Expr EmptyTreeList;
        public readonly FuncDecl GetFirst, GetRest, MkCons, IsEmpty, IsCons;

        public UnrankedTreeInfo(Sort treeListSort, FuncDecl getNodeValue, FuncDecl getSubtrees, FuncDecl mkNode,
            FuncDecl mkLeaf, FuncDecl getLeafValue, FuncDecl isNode, FuncDecl isLeaf,
            Expr empty, FuncDecl first, FuncDecl rest,
            FuncDecl cons, FuncDecl isEmpty, FuncDecl isCons)
        {
            this.TreeListSort = treeListSort;
            this.GetNodeLabel = getNodeValue;
            this.GetNodeSubtrees = getSubtrees;
            this.MkNode = mkNode;
            this.EmptyTreeList = empty;
            this.GetFirst = first;
            this.GetRest = rest;
            this.MkCons = cons;
            this.IsEmpty = isEmpty;
            this.IsCons = isCons;
            this.MkLeaf = mkLeaf;
            this.GetLeafValue = getLeafValue;
            this.IsNode = isNode;
            this.IsLeaf = isLeaf;
        }
    }

    /// <summary>
    /// Encapsulates the Z3 declarations related to the algebraic datatype of binary trees without node labels
    /// </summary>
    internal class BinaryTreeInfo
    {
        public readonly FuncDecl MkTree, MkLeaf, GetLeafValue, IsTree, IsLeaf;
        public readonly FuncDecl GetLeft, GetRight;

        public BinaryTreeInfo(FuncDecl mkTree, FuncDecl mkLeaf, 
            FuncDecl getLeafValue, FuncDecl isTree, FuncDecl isLeaf,
            FuncDecl getLeft, FuncDecl getRight)
        {
            this.MkTree = mkTree;
            this.GetLeft = getLeft;
            this.GetRight = getRight;
            this.MkLeaf = mkLeaf;
            this.GetLeafValue = getLeafValue;
            this.IsTree = isTree;
            this.IsLeaf = isLeaf;
        }
    }

}
