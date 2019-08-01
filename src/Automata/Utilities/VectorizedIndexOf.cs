﻿using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Microsoft.Automata;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq.Expressions;

namespace Microsoft.Automata
{
    public static class VectorizedIndexOf
    {
        static int vecUshortSize = Vector<ushort>.Count;
        static int vecUintSize = Vector<uint>.Count;    
        static int vecByteSize = Vector<byte>.Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe static int UnsafeIndexOf(char* chars, int length, int start, string toMatch)
        {
            if (toMatch.Length == 0)
            {
                return start;
            }
            if (toMatch.Length == 1)
            {
                return UnsafeIndexOf1(chars, length, start, toMatch[0], new Vector<ushort>(toMatch[0]));
            }

            fixed (char* toMatchp = toMatch)
            {
                var first = new Vector<ushort>((ushort)toMatchp[0]);
                int lastOffset = toMatch.Length - 1;
                var last = new Vector<ushort>((ushort)toMatchp[lastOffset]);

                int i = start;
                int lastVec = length - vecUshortSize - lastOffset;
                for (; i <= lastVec; i += vecUshortSize)
                {
                    var vecFirst = Unsafe.Read<Vector<ushort>>(chars + i);
                    var vecLast = Unsafe.Read<Vector<ushort>>(chars + i + lastOffset);

                    var eqFirst = Vector.Equals(vecFirst, first);
                    var eqLast = Vector.Equals(vecLast, last);

                    var mask = Vector.BitwiseAnd(eqFirst, eqLast);

                    if (!Vector.EqualsAll(mask, Vector<ushort>.Zero))
                    {
                        for (int j = 0; j < vecUshortSize; ++j)
                        {
                            if (mask[j] != 0)
                            {
                                var ij = i + j;
                                for (int k = 0; k <= lastOffset; ++k)
                                {
                                    if (chars[ij + k] != toMatchp[k])
                                        goto MATCH_FAIL;
                                }
                                return ij;
                            }
                        MATCH_FAIL:;
                        }
                    }
                }
                for (; i < length; ++i)
                {
                    for (int k = 0; k <= lastOffset; ++k)
                    {
                        if (chars[i + k] != toMatchp[k])
                            goto REMAINDER_MATCH_FAIL;
                    }
                    return i;
                REMAINDER_MATCH_FAIL:;
                }
                return -1;
            }
        }

        internal static Func<Vector<ushort>, Vector<ushort>> CompileBooleanDecisionTree(BooleanDecisionTree toMatch)
        {
            var ushortArgList = new[] { typeof(ushort) };
            int nextVarIdx = 0;
            Func<ParameterExpression> getVecVar = () => Expression.Parameter(typeof(Vector<ushort>), "var" + (nextVarIdx++));

            var input = getVecVar();
            var accumulator = getVecVar();

            Func<DecisionTree.BST, Expression, Expression> nodeToExpression = null;
            nodeToExpression = (expr, current) =>
            {
                if (expr.IsLeaf)
                {
                    if (expr.Node == 1)
                    {
                        return Expression.Block(
                            Expression.AddAssign(accumulator, current));
                    }
                    else
                    {
                        return Expression.Empty();
                    }
                } else
                {
                    var compared = getVecVar();
                    var left = getVecVar();
                    var right = getVecVar();
                    var stmts = new List<Expression>();
                    stmts.Add(Expression.Assign(compared, Expression.Call(typeof(Vector), "LessThan", ushortArgList, input, Expression.Constant(new Vector<ushort>((ushort)expr.Node)))));
                    if (!expr.Left.IsLeaf || expr.Left.Node == 1)
                    {
                        stmts.Add(Expression.Assign(left, Expression.And(current, compared)));
                        stmts.Add(Expression.IfThen(Expression.Not(Expression.Call(typeof(Vector), "EqualsAll", ushortArgList, left, Expression.Constant(Vector<ushort>.Zero))),
                                nodeToExpression(expr.Left, left)));
                    }
                    if (!expr.Right.IsLeaf || expr.Right.Node == 1)
                    {
                        stmts.Add(Expression.Assign(right, Expression.And(current, Expression.OnesComplement(compared))));
                        stmts.Add(Expression.IfThen(Expression.Not(Expression.Call(typeof(Vector), "EqualsAll", ushortArgList, right, Expression.Constant(Vector<ushort>.Zero))),
                                nodeToExpression(expr.Right, right)));
                    }
                    return Expression.Block(
                        new[] { compared, left, right },
                        stmts.ToArray()
                        ); // TODO: remove unnecessary branches
                }
            };

            LabelTarget returnTarget = Expression.Label(typeof(Vector<ushort>));
            GotoExpression returnExpression = Expression.Return(returnTarget,
                accumulator, typeof(Vector<ushort>));
            LabelExpression returnLabel = Expression.Label(returnTarget, accumulator);

            var func = Expression.Lambda<Func<Vector<ushort>, Vector<ushort>>>(Expression.Block(
                new ParameterExpression[] { accumulator },
                nodeToExpression(toMatch.bst, Expression.Constant(Vector<ushort>.One)),
                returnExpression,
                returnLabel
                ), input);
            return func.Compile();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe static int UnsafeIndexOf(char* chars, int length, int start, BooleanDecisionTree toMatch, Func<Vector<ushort>, Vector<ushort>> toMatchCompiled)
        {
            //System.Diagnostics.Debug.Assert(Vector.IsHardwareAccelerated);
            int i = start;
            int lastVec = length - vecUshortSize;
            for (; i <= lastVec; i += vecUshortSize)
            {
                var vec = Unsafe.Read<Vector<ushort>>(chars + i);
                var matching = toMatchCompiled(vec);
                if (!Vector.EqualsAll(matching, Vector<ushort>.Zero))
                {
                    for (int j = 0; j < vecUshortSize; ++j)
                    {
                        if (matching[j] != 0) return i + j;
                    }
                }
            }
            for (; i < length; ++i)
            {
                if (toMatch.Contains(chars[i])) return i;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe static int UnsafeIndexOf(char* chars, int length, int start, BooleanDecisionTree toMatch, Vector<ushort>[] toMatchVecs)
        {
            //System.Diagnostics.Debug.Assert(Vector.IsHardwareAccelerated);
            fixed (bool* toMatch_precomputed = toMatch.precomputed)
            {
                int i = start;
                int lastVec = length - vecUshortSize;
                int toMatch_precomputed_length = toMatch.precomputed.Length;
                int toMatchVecs_Length = toMatchVecs.Length;
                for (; i <= lastVec; i += vecUshortSize)
                {
                    var vec = Unsafe.Read<Vector<ushort>>(chars + i);
                    for (int k = 0; k < toMatchVecs_Length; k++)
                    {
                        var searchVec = toMatchVecs[k];
                        if (Vector.EqualsAny(vec, searchVec))
                        {
                            for (int j = 0; j < vecUshortSize; ++j)
                            {
                                int ij = i + j;
                                var c = chars[ij];
                                if (c < toMatch_precomputed_length ? toMatch_precomputed[c] : toMatch.bst.Find(c) == 1)
                                    return ij;
                            }
                        }
                    }
                }
                for (; i < length; ++i)
                {
                    if (toMatch.Contains(chars[i])) return i;
                }
                return -1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe static int UnsafeIndexOf(char* chars, int length, int start, BooleanDecisionTree toMatch, Vector<ushort> toMatchVec)
        {
            //System.Diagnostics.Debug.Assert(Vector.IsHardwareAccelerated);
            fixed (bool* toMatch_precomputed = toMatch.precomputed)
            {
                int i = start;
                int lastVec = length - vecUshortSize;
                int toMatch_precomputed_length = toMatch.precomputed.Length;
                for (; i <= lastVec; i += vecUshortSize)
                {
                    var vec = Unsafe.Read<Vector<ushort>>(chars + i);
                    var searchVec = toMatchVec;
                    if (Vector.EqualsAny(vec, searchVec))
                    {
                        for (int j = 0; j < vecUshortSize; ++j)
                        {
                            int ij = i + j;
                            var c = chars[ij];
                            if (c < toMatch_precomputed_length ? toMatch_precomputed[c] : toMatch.bst.Find(c) == 1)
                                return ij;
                        }
                    }
                }
                for (; i < length; ++i)
                {
                    if (toMatch.Contains(chars[i])) return i;
                }
                return -1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe static int UnsafeIndexOf1(char* chars, int length, int start, ushort toMatch, Vector<ushort> toMatchVec)
        {
            //System.Diagnostics.Debug.Assert(Vector.IsHardwareAccelerated);
            int i = start;
            int lastVec = length - vecUshortSize;
            for (; i <= lastVec; i += vecUshortSize)
            {
                var vec = Unsafe.Read<Vector<ushort>>(chars + i);
                if (Vector.EqualsAny(vec, toMatchVec))
                {
                    for (int j = 0; j < vecUshortSize; ++j)
                    {
                        int ij = i + j;
                        if (toMatch == chars[ij]) return ij;
                    }
                }
            }
            for (; i < length; ++i)
            {
                if (toMatch == chars[i]) return i;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe static int UnsafeIndexOfByte(byte[] input, int start, byte[] toMatch)
        {
            var toMatchVecs = toMatch.Select(x => new Vector<byte>(x)).ToArray();
            fixed (byte* bytes = input)
            {
                var length = input.Length;
                int i = start;
                int lastVec = length - vecByteSize;
                for (; i <= lastVec; i += vecByteSize)
                {
                    var vec = Unsafe.Read<Vector<byte>>(bytes + i);
                    foreach (var searchVec in toMatchVecs)
                    {
                        if (Vector.EqualsAny(vec, searchVec))
                        {
                            for (int j = 0; j < vecUshortSize; ++j)
                            {
                                if (toMatch.Contains(input[i + j])) return i + j;
                            }
                        }
                    }
                }
                for (; i < input.Length; ++i)
                {
                    if (toMatch.Contains(input[i])) return i;
                }
                return -1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfByte(byte[] input, int i, byte toMatch, Vector<byte> toMatchVec)
        {
            int lastVec = input.Length - vecByteSize;
            while (i <= lastVec && !Vector.EqualsAny(new Vector<byte>(input, i), toMatchVec))
                i += vecByteSize;
            return Array.IndexOf<byte>(input, toMatch, i);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfByteSeq(byte[] input, int i, byte[] seqToMatch, Vector<byte> firstToMatchVec)
        {
            int length = input.Length;
            int lastVec = length - vecByteSize;
            byte firstToMatch = seqToMatch[0];
            int seqToMatch_length = seqToMatch.Length;
            while (i <= lastVec)
            {
                if (Vector.EqualsAny(new Vector<byte>(input, i), firstToMatchVec))
                {
                    i = Array.IndexOf<byte>(input, firstToMatch, i);
                    if (i + seqToMatch_length > length)
                        return -1;
                    int j = 1;
                    while (j < seqToMatch_length && input[i + j] == seqToMatch[j])
                        j += 1;
                    if (j == seqToMatch_length)
                        return i;
                    else
                    {
                        i += 1;
                    }
                }
                else
                {
                    i += vecByteSize;
                }
            }
            i = Array.IndexOf<byte>(input, firstToMatch, i);
            if (i + seqToMatch_length > length)
                return -1;
            int j1 = 1;
            while (j1 < seqToMatch_length && input[i + j1] == seqToMatch[j1])
                j1 += 1;
            if (j1 == seqToMatch_length)
                return i;
            else
                return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe static int UnsafeIndexOfByte(byte[] input, int i, byte toMatch, Vector<byte> toMatchVec)
        {
            var length = input.Length;
            int lastVec = length - vecByteSize;
            fixed (byte* bytes = input)
            {
                for (; i <= lastVec; i += vecByteSize)
                {
                    var vec = Unsafe.Read<Vector<byte>>(bytes + i);
                    if (Vector.EqualsAny(vec, toMatchVec))
                    {
                        return Array.IndexOf<byte>(input, toMatch, i);
                    }
                }
                return Array.IndexOf<byte>(input, toMatch, i);
            }
        }
    }
}
