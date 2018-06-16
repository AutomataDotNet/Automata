using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Microsoft.Automata.Utilities
{
    /// <summary>
    /// Methods for decoding UTF8 encoded strings.
    /// </summary>
    public static class UTF8Decoder
    {
        /// <summary>
        /// Incremental UTF8 to UTF16 decoder. Outputs the next UTF-16 character and advances the current input position.
        /// Returns true if the decoding succeeds, returns false otherwise.
        /// </summary>
        /// <param name="input">UFT8 encoded input</param>
        /// <param name="i">current position in the input</param>
        /// <param name="partial_low_surrogate">low surrogate leftover from previous high surrogate decoding</param>
        /// <param name="c">decoded UTF16 character code or surrogate (0 if return value is false)</param>
        /// <returns>true iff the decoding succeeds</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool UTF8toUTF16(byte[] input, ref int i, ref ushort partial_low_surrogate, out ushort c)
        {
            int input_length = input.Length;
            if (partial_low_surrogate == 0) 
            {   //0 means that we are not in the middle of a 4byte encoding
                ushort b1 = input[i];
                if (b1 <= 0x7F)
                {
                    c = b1;
                    i += 1;
                    return true;
                }
                else if (0xC2 <= b1 && b1 <= 0xDF) //two byte encoding
                {
                    i += 1;
                    if (i == input_length)
                    {
                        c = 0;
                        return false;
                    }
                    else
                    {
                        ushort b2 = input[i];
                        if (0x80 <= b2 && b2 <= 0xBF)
                        {
                            c = (ushort)(((b1 & 0x3F) << 6) | (b2 & 0x3F));
                            i += 1;
                            return true;
                        }
                        else
                        {
                            c = 0;
                            return false;
                        }
                    }
                }
                else if (0xE0 <= b1 && b1 <= 0xEF)  //three byte encoding
                {
                    i += 1;
                    if (i + 1 >= input_length)
                    {
                        c = 0;
                        return false;
                    }
                    else
                    {
                        ushort b2 = input[i];
                        if ((b1 == 0xE0 && 0xA0 <= b2 && b2 <= 0xBF) ||
                            (b1 == 0xED && 0x80 <= b2 && b2 <= 0x9F) ||
                            (0x80 <= b2 && b2 <= 0xBF))
                        {
                            i += 1;
                            ushort b3 = input[i];
                            if (0x80 <= b3 && b3 <= 0xBF)
                            {
                                c = (ushort)(((b1 & 0xF) << 12) | ((b2 & 0x3F) << 6) | (b3 & 0x3F)); //utf8decode the bytes
                                i += 1;
                                return true;
                            }
                            else
                            {
                                c = 0;
                                return false; //invalid third byte
                            }
                        }
                        else
                        {
                            c = 0;
                            return false; //invalid second byte
                        }
                    }
                }
                else if (0xF0 <= b1 && b1 <= 0xF4) //4 byte encoding decoded and reencoded into UTF16 surrogate pair (high, low)
                {
                    i += 1;
                    if (i + 2 >= input_length) //(4 byte check)
                    {
                        c = 0;
                        return false;  //second byte, third byte or fourth byte is missing
                    }
                    else
                    {
                        ushort b2 = input[i];
                        if ((b1 == 0xF0 && (0x90 <= b2 && b2 <= 0xBF)) ||
                            (b1 == 0xF4 && (0x80 <= b2 && b2 <= 0x8F)) ||
                            (0x80 <= b2 && b2 <= 0xBF))
                        {
                            i += 1;
                            ushort b3 = input[i];
                            if (0x80 <= b3 && b3 <= 0xBF)
                            {
                                //set *c to high surrogate
                                c = (ushort)(0xD800 | (((((b1 & 7) << 2) | ((b2 & 0x30) >> 4)) - 1) << 6) | ((b2 & 0x0F) << 2) | ((b3 >> 4) & 3));
                                partial_low_surrogate = (ushort)(0xDC00 | ((b3 & 0xF) << 6)); //set the low surrogate register
                                i += 1;
                                return true;
                            }
                            else
                            {
                                c = 0;
                                return false; //incorrect third byte
                            }
                        }
                        else
                        {
                            c = 0;
                            return false; //incorrect second byte
                        }
                    }
                }
                else
                {
                    c = 0;
                    return false; //incorrect first byte
                }
            }
            else //compute the low surrogate
            {
                ushort b4 = input[i]; //we know i < size due to the above check (4 byte check)
                if (0x80 <= b4 && b4 <= 0xBF)
                {
                    i += 1;
                    c = (ushort)(partial_low_surrogate | (b4 & 0x3F)); //set *c to low surrogate
                    partial_low_surrogate = 0;                  //reset the low surrogate register
                    return true;
                }
                else
                {
                    c = 0;
                    return false; //incorrect fourth byte
                }
            }
        }

        /// <summary>
        /// Assuming correctly encoded input.
        /// Called only if input[i] is non-ASCII
        /// </summary>
        internal static void UTF8toUTF16_backwards(byte[] input, ref int i, ref ushort partial_low_surrogate, out ushort c)
        {
            throw new NotImplementedException("UTF8toUTF16_backwards");
        }


        /// <summary>
        /// Decode the next codepoint in the input.
        /// Here input[i] is assumed to be non-ASCII.
        /// The input byte array is asssumed to be valid UTF8 encoded Unicode text.
        /// </summary>
        /// <param name="input">UTF8 encoded Unicode text</param>
        /// <param name="i">position of the current start byte</param>
        /// <param name="step">how many bytes were consumed</param>
        /// <param name="codepoint">computed Unicode codepoint</param>
        /// <returns></returns>
        internal static void DecodeNextNonASCII(byte[] input, int i, out int step, out int codepoint)
        {
            int b = input[i];
            // (b & 1110.0000 == 1100.0000)
            // so b has the form 110x.xxxx
            // startbyte of two byte encoding
            if ((b & 0xE0) == 0xC0)
            {
                codepoint = ((b & 0x1F) << 6) | (input[i + 1] & 0x3F);
                step = 2;
            }
            // (b & 1111.0000 == 1110.0000)
            // so b has the form 1110.xxxx
            // startbyte of three byte encoding
            else if ((b & 0xF0) == 0xE0)
            {
                codepoint = ((b & 0x0F) << 12) | ((input[i + 1] & 0x3F) << 6) | (input[i + 2] & 0x3F);
                step = 3;
            }
            // (b & 1111.1000 == 1111.0000)
            // so b has the form 1111.0xxx
            // must be startbyte of four byte encoding
            else
            {
                codepoint = ((b & 0x07) << 18) | ((input[i + 1] & 0x3F) << 12) | ((input[i + 2] & 0x3F) << 6) | (input[i + 3] & 0x3F);
                step = 4;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ushort HighSurrogate(int codepoint)
        {
            //given codepoint = ((H - 0xD800) * 0x400) + (L - 0xDC00) + 0x10000
            // compute H 
            return (ushort)(((codepoint - 0x10000) >> 10) | 0xD800);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ushort LowSurrogate(int codepoint)
        {
            //given codepoint = ((H - 0xD800) * 0x400) + (L - 0xDC00) + 0x10000
            //compute L 
            var cp = (ushort)(((codepoint - 0x10000) & 0x3FF) | 0xDC00);
            return cp;
        }
    }
}
