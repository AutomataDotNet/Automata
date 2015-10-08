using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Automata
{
    /// <summary>
    /// Nonnegative big integers with restricted functionality.
    /// </summary>
    public class BigInt
    {
        internal static readonly BigInt Zero = new BigInt(System.Numerics.BigInteger.Zero);
        internal static readonly BigInt One = new BigInt(System.Numerics.BigInteger.One);

        internal static int NrOfDecimals = 10; //use 10 decimal precision in double
        static readonly System.Numerics.BigInteger K_bigint = System.Numerics.BigInteger.Parse("1000000000000");
        static readonly double K_double = (double)System.Numerics.BigInteger.Parse("1000000000000");

        System.Numerics.BigInteger bigint;

        BigInt(System.Numerics.BigInteger other)
        {
            bigint = other;
        }

        internal BigInt(long m)
        {
            bigint = new System.Numerics.BigInteger(m);
        }

        internal BigInt(ulong m)
        {
            bigint = new System.Numerics.BigInteger((long)m);
        }

        internal BigInt Times(BigInt other)
        {
            return new BigInt(bigint * other.bigint);
        }

        internal BigInt Plus(BigInt other)
        {
            return new BigInt(bigint + other.bigint);
        }

        internal double DivideAsDouble(BigInt divider)
        {
            double d = (((double)((K_bigint * bigint) / divider.bigint)) / K_double);
            double d_rounded = Math.Round(d, NrOfDecimals); 
            return d_rounded;
        }

        public override bool Equals(object obj)
        {
            BigInt b = obj as BigInt;
            if (obj == null)
                return false;
            return bigint.Equals(b.bigint);
        }

        public override int GetHashCode()
        {
            return bigint.GetHashCode();
        }

        public override string ToString()
        {
            return bigint.ToString();
        }
    }
}
