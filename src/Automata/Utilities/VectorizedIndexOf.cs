using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Microsoft.Automata;

namespace Microsoft.Automata
{
    public static class VectorizedIndexOf
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe static int UnsafeIndexOf(this string input, int start, BooleanDecisionTree toMatch, Vector<ushort>[] toMatchVecs)
        {
            //System.Diagnostics.Debug.Assert(Vector.IsHardwareAccelerated);
            fixed (char* chars = input)
            {
                var vecSize = Vector<ushort>.Count;
                var length = input.Length;
                int i = start;
                int lastVec = length - vecSize;
                for (; i <= lastVec; i += vecSize)
                {
                    var vec = Unsafe.Read<Vector<ushort>>(chars + i);
                    for (int k = 0; k < toMatchVecs.Length; k++)
                    {
                        var searchVec = toMatchVecs[k];
                        if (Vector.EqualsAny(vec, searchVec))
                        {
                            for (int j = 0; j < vecSize; ++j)
                            {
                                if (toMatch.Contains(chars[i + j])) return i + j;
                            }
                        }
                    }
                }
                for (; i < input.Length; ++i)
                {
                    if (toMatch.Contains(chars[i])) return i;
                }
                return -1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe static int UnsafeIndexOf1(this string input, int start, ushort toMatch, Vector<ushort> toMatchVec)
        {
            System.Diagnostics.Debug.Assert(Vector.IsHardwareAccelerated);
            fixed (char* chars = input)
            {
                var vecSize = Vector<ushort>.Count;
                var length = input.Length;
                int i = start;
                int lastVec = length - vecSize;
                for (; i <= lastVec; i += vecSize)
                {
                    var vec = Unsafe.Read<Vector<ushort>>(chars + i);
                    if (Vector.EqualsAny(vec, toMatchVec))
                    {
                        for (int j = 0; j < vecSize; ++j)
                        {
                            if (toMatch == chars[i + j]) return i + j;
                        }
                    }
                }
                for (; i < input.Length; ++i)
                {
                    if (toMatch == chars[i]) return i;
                }
                return -1;
            }
        }

        /// <summary>
        /// here substring.Length > 1, toMatchVec1 is first character and toMatchVec2 is second character
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe static int UnsafeIndexOf2(this string input, int start, string substring, Vector<ushort> toMatchVec1, Vector<ushort> toMatchVec2)
        {
            //uint matchBoth = (((uint)toMatch) << 16) | toMatch2;
            //var matchBothVec = new Vector<uint>(matchBoth);
            System.Diagnostics.Debug.Assert(Vector.IsHardwareAccelerated);
            fixed (char* chars = input)
            fixed (char* substrp = substring)
            {
                var stepSize = Vector<ushort>.Count - 1;
                var length = input.Length;
                int i = start;
                int lastVec = length - stepSize;
                int k = substring.Length;
                ushort firstChar = substrp[0];
                ushort secondChar = substrp[1];
                for (; i <= lastVec; i += stepSize)
                {
                    var vec = Unsafe.Read<Vector<ushort>>(chars + i);
                    if (Vector.EqualsAny(vec, toMatchVec1) && Vector.EqualsAny(vec, toMatchVec2))
                    {
                        for (int j = 0; j < stepSize; ++j)
                        {
                            int ij = i + j;
                            if (firstChar == chars[ij])
                            {
                                //check the rest of the input also 
                                if (ij + k < length)
                                {
                                    int l = 1;
                                    while (l < k && substrp[l] == chars[ij + l])
                                        l += 1;
                                    if (l == k)
                                        return ij;
                                }
                            }
                        }
                    }
                }
                for (; i < input.Length - k + 1; ++i)
                {
                    if (firstChar == chars[i])
                    {
                        int l = 1;
                        while (l < k && substrp[l] == chars[i + l])
                            l += 1;
                        if (l == k)
                            return i;
                    }
                }
                return -1;
            }
        }

        static string Show(Vector<ushort> vec)
        {
            //string s = "";
            //for (int i = 0; i < Vector<ushort>.Count; i++)
            //    s += ((char)vec[i]).ToString();
            //return s;
            return new string(new char[] { (char)vec[0],(char)vec[1],(char)vec[2],(char)vec[3], (char)vec[4],(char)vec[5],(char)vec[6],(char)vec[7],(char)vec[8],(char)vec[9],(char)vec[10],(char)vec[11],(char)vec[12],(char)vec[13],(char)vec[14],(char)vec[15]});
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static int UnsafeIndexOfByte(byte[] input, int start, byte[] toMatch)
        {
            var toMatchVecs = toMatch.Select(x => new Vector<byte>(x)).ToArray();
            fixed (byte* bytes = input)
            {
                var vecSize = Vector<byte>.Count;
                var length = input.Length;
                int i = start;
                int lastVec = length - vecSize;
                for (; i <= lastVec; i += vecSize)
                {
                    var vec = Unsafe.Read<Vector<byte>>(bytes + i);
                    foreach (var searchVec in toMatchVecs)
                    {
                        if (Vector.EqualsAny(vec, searchVec))
                        {
                            for (int j = 0; j < vecSize; ++j)
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
            var vecSize = Vector<byte>.Count;
            var length = input.Length;
            int i = start;
            int lastVec = length - vecSize;
            for (; i <= lastVec; i += vecSize)
            {
                var vec = new Vector<byte>(input, i);
                foreach (var searchVec in toMatchVecs)
                {
                    if (Vector.EqualsAny(vec, searchVec))
                    {
                        for (int j = 0; j < vecSize; ++j)
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
