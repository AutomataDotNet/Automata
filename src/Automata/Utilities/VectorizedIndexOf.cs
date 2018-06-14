using System.Linq;
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
        static int vecSizeUshort = Vector<ushort>.Count;
        static int vecSizeUint = Vector<uint>.Count;
        static int vecSizeByte = Vector<byte>.Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe static int UnsafeIndexOf(char* chars, int length, int start, BooleanDecisionTree toMatch, Vector<ushort>[] toMatchVecs)
        {
            //System.Diagnostics.Debug.Assert(Vector.IsHardwareAccelerated);
            fixed (bool* toMatch_precomputed = toMatch.precomputed)
            {
                int i = start;
                int lastVec = length - vecSizeUshort;
                int toMatch_precomputed_length = toMatch.precomputed.Length;
                int toMatchVecs_Length = toMatchVecs.Length;
                for (; i <= lastVec; i += vecSizeUshort)
                {
                    var vec = Unsafe.Read<Vector<ushort>>(chars + i);
                    for (int k = 0; k < toMatchVecs_Length; k++)
                    {
                        var searchVec = toMatchVecs[k];
                        if (Vector.EqualsAny(vec, searchVec))
                        {
                            for (int j = 0; j < vecSizeUshort; ++j)
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
                int lastVec = length - vecSizeUshort;
                int toMatch_precomputed_length = toMatch.precomputed.Length;
                for (; i <= lastVec; i += vecSizeUshort)
                {
                    var vec = Unsafe.Read<Vector<ushort>>(chars + i);
                    var searchVec = toMatchVec;
                    if (Vector.EqualsAny(vec, searchVec))
                    {
                        for (int j = 0; j < vecSizeUshort; ++j)
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
            int lastVec = length - vecSizeUshort;
            for (; i <= lastVec; i += vecSizeUshort)
            {
                var vec = Unsafe.Read<Vector<ushort>>(chars + i);
                if (Vector.EqualsAny(vec, toMatchVec))
                {
                    for (int j = 0; j < vecSizeUshort; ++j)
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

        /// <summary>
        /// here substring.Length > 1, toMatchVec1 is first character and toMatchVec2 is second character
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe static int UnsafeIndexOf2(char* input, int length, int start, char first, char second)
        {
            uint toMatch = (((uint)first) << 16) | second;
            Vector<uint> toMatchVec = new Vector<uint>(toMatch);
            int i = start;

            int lastVec = length - vecSizeUint;

            uint[] elems = new uint[vecSizeUint];

            //stop vecsize+1 before the end because of +1 lookahead
            for (; i < lastVec; i += vecSizeUint)
            {
                for (int j = 0; j < vecSizeUint; j++)
                {
                    elems[j] = (((uint)(input[i + j])) << 16) | input[i + j + 1];
                }
                var vec = new Vector<uint>(elems);
                if (Vector.EqualsAny(vec, toMatchVec))
                {
                    for (int j = 0; j < vecSizeUshort; ++j)
                    {
                        int ij = i + j;
                        if (first == input[ij] && second == input[ij + 1])
                        {
                            return ij;
                        }
                    }
                }
            }
            for (; i < length - 1; ++i)
            {
                if (first == input[i] && second == input[i + 1])
                {
                    return i;
                }
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static int UnsafeIndexOfByte(byte[] input, int start, byte[] toMatch)
        {
            var toMatchVecs = toMatch.Select(x => new Vector<byte>(x)).ToArray();
            fixed (byte* bytes = input)
            {
                var length = input.Length;
                int i = start;
                int lastVec = length - vecSizeUshort;
                for (; i <= lastVec; i += vecSizeUshort)
                {
                    var vec = Unsafe.Read<Vector<byte>>(bytes + i);
                    foreach (var searchVec in toMatchVecs)
                    {
                        if (Vector.EqualsAny(vec, searchVec))
                        {
                            for (int j = 0; j < vecSizeUshort; ++j)
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
        public static int IndexOfByte(byte[] input, int start, byte[] toMatch)
        {
            var toMatchVecs = toMatch.Select(x => new Vector<byte>(x)).ToArray();
            var length = input.Length;
            int i = start;
            int lastVec = length - vecSizeUshort;
            for (; i <= lastVec; i += vecSizeUshort)
            {
                var vec = new Vector<byte>(input, i);
                foreach (var searchVec in toMatchVecs)
                {
                    if (Vector.EqualsAny(vec, searchVec))
                    {
                        for (int j = 0; j < vecSizeUshort; ++j)
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
}
