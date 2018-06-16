﻿using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Microsoft.Automata;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Microsoft.Automata
{
    public static class VectorizedIndexOf
    {
        static int vecUshortSize = Vector<ushort>.Count;
        static int vecUintSize = Vector<uint>.Count;    
        static int vecByteSize = Vector<byte>.Count;

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
