using System;
using System.Security.Cryptography;

namespace Microsoft.Automata
{
    /// <summary>
    /// Random number chooser.
    /// </summary>
    public class Chooser
    {
        /// <summary>
        /// The RNGCryptoServiceProvider object the Chooser uses.
        /// </summary>
        private RNGCryptoServiceProvider randomNumberGenerator = new RNGCryptoServiceProvider();

        /// <summary>
        /// Initializes a new instance of the <see cref="Chooser" /> class.
        /// </summary>
        public Chooser()
        {
        }

        ///// <summary>
        ///// Initializes a new instance of the <see cref="Chooser" /> class.
        ///// </summary>
        ///// <param name="randomSeed">This value is ignored. It is needed for backward compatibility.</param>
        //[Obsolete("This constructor is obsolete. The value of randomSeed is ignored. Use the parameterless constructor instead.")]
        //public Chooser(int randomSeed)
        //    : this()
        //{
        //    this.RandomSeed = randomSeed;
        //}

        /// <summary>
        /// Gets or sets the random seed of the chooser. The value of this property is ignored.
        /// The property is needed for backward compatibility.
        /// </summary>
        //[Obsolete("This property is The value of this property is ignored. The property is needed for backward compatibility.")]
        //public int RandomSeed { get; set; }

        /// <summary>
        /// Returns a nonnegative random number less than the specified maximum.
        /// </summary>
        /// <param name="n">The exclusive upper bound of the random number to be generated. n must be
        /// greater than or equal to zero.</param>
        /// <returns>An integer greater than or equal to zero, and less than n.</returns>
        public int Choose(int n)
        {
            if (n < 0)
            {
                throw new ArgumentOutOfRangeException("n", "n must be non-negative.");
            }

            if (n <= 1)
            {
                return 0;
            }

            uint result;
            uint msb = this.GetMostSignificantBit((uint)n);
            uint mask = msb - 1;

            double probabilityOfMsbBeingOne = ((double)n - msb) / n;

            if (this.ChooseDouble() < probabilityOfMsbBeingOne)
            {
                result = msb + (uint)this.Choose(n - (int)msb);
            }
            else
            {
                byte[] randomData = new byte[sizeof(uint)];
                this.randomNumberGenerator.GetBytes(randomData);
                uint randomInt = BitConverter.ToUInt32(randomData, 0);

                result = randomInt & mask;
            }

            return (int)result;
        }

        /// <summary>
        /// Choose a random uint.
        /// </summary>
        /// <returns>A random uint equal to or greater than 0 and less than or equal to uint.MaxValue.</returns>
        public uint ChooseBV32()
        {
            byte[] randomData = new byte[sizeof(uint)];

            uint result;

            this.randomNumberGenerator.GetBytes(randomData);
            result = BitConverter.ToUInt32(randomData, 0);

            return result;
        }

        /// <summary>
        /// Choose a random ulong.
        /// </summary>
        /// <returns>A random ulong equal to or greater than 0 and less than or equal to ulong.MaxValue.</returns>
        public ulong ChooseBV64()
        {
            byte[] randomData = new byte[sizeof(ulong)];

            ulong result;

            this.randomNumberGenerator.GetBytes(randomData);
            result = BitConverter.ToUInt64(randomData, 0);

            return result;
        }

        /// <summary>
        /// Choose true or false randomly.
        /// </summary>
        /// <returns>True or false.</returns>
        public bool ChooseTrueOrFalse()
        {
            byte[] randomData = new byte[1];
            this.randomNumberGenerator.GetBytes(randomData);
            return (randomData[0] & 0x1) == 0;
        }

        /// <summary>
        /// Returns a random number between 0.0 and 1.0.
        /// </summary>
        /// <returns>A double-precision floating point number greater than or equal to 0.0, and less
        /// than 1.0.</returns>
        public double ChooseDouble()
        {
            byte[] randomData = new byte[sizeof(int)];
            this.randomNumberGenerator.GetBytes(randomData);

            return (double)BitConverter.ToUInt32(randomData, 0) / ((ulong)uint.MaxValue + 1);
        }

        /// <summary>
        /// Calculates the value of the most significant bit in a positive integer.
        /// </summary>
        /// <param name="n">The positive integer of which the 
        /// value of the most significant bit is to be calculated.</param>
        /// <returns>The value of the most significant bit.</returns>
        private uint GetMostSignificantBit(uint n)
        {
            // Do from (2^x) =1 to (2^x) =(number of bits in data type of n)/2
            n |= n >> 1;
            n |= n >> 2;
            n |= n >> 4;
            n |= n >> 8;
            n |= n >> 16;

            return n - (n >> 1);
        }
    }
}