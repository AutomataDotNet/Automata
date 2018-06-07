using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Automata.Utilities
{
    static class VectorizedIndexOf
    {
        public unsafe static int UnsafeIndexOf(this string input, int start, char[] toMatch)
        {
            var toMatchVecs = toMatch.Select(x => new Vector<ushort>(x)).ToArray();
            fixed (char* chars = input)
            {
                var vecSize = Vector<ushort>.Count;
                var length = input.Length;
                int i = start;
                int lastVec = length - vecSize;
                for (; i <= lastVec; i += vecSize)
                {
                    var vec = Unsafe.Read<Vector<ushort>>(chars + i);
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
