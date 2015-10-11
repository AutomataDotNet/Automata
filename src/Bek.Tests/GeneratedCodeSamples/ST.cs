using System;
using System.Text;

namespace Microsoft.Bek.Tests
{
    /*
    * 
    *  The following are automatically generated using the above unit test: TestCssEncodeCSgen
    * 
    */

    //no exploration
    public static class CssEncode
    {

        static int Bits(int m, int n, int c) { return ((c >> n) & ~(-2 << (m - n))); }

        public static string Apply(string input)
        {
            var output = new char[(input.Length * 7) + 0];
            bool r0 = false; int r1 = 0;
            int pos = 0;
            var chars = input.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                int c = (int)chars[i];
                if (((c == 0xFFFE) || (c == 0xFFFF)))
                {
                    throw new Exception("CssEncode");
                }
                else
                {
                    if (r0) { if (((0xDC00 > c) || (c > 0xDFFF))) { throw new Exception("CssEncode"); } else { output[pos++] = ((char)((((Bits(13, 0, r1) << 2) | Bits(9, 8, c)) & 0xF) + ((((Bits(13, 0, r1) << 2) | Bits(9, 8, c)) & 0xF) <= 9 ? 48 : 55))); output[pos++] = ((char)(((c >> 4) & 0xF) + (((c >> 4) & 0xF) <= 9 ? 48 : 55))); output[pos++] = ((char)((c & 0xF) + ((c & 0xF) <= 9 ? 48 : 55))); r0 = false; r1 = 0; } } else { if (((0xDC00 <= c) && (c <= 0xDFFF))) { throw new Exception("CssEncode"); } else { if (((0xD800 <= c) && (c <= 0xDBFF))) { output[pos++] = ((char)0x5C); output[pos++] = ((char)(Bits(9, 6, c) == 0xF ? 0x31 : 0x30)); output[pos++] = ((char)(((1 + Bits(15, 6, c)) & 0xF) + (((1 + Bits(15, 6, c)) & 0xF) <= 9 ? 48 : 55))); output[pos++] = ((char)((Bits(15, 2, c) & 0xF) + ((Bits(15, 2, c) & 0xF) <= 9 ? 48 : 55))); r0 = true; r1 = Bits(1, 0, c); } else { if (!(Bits(15, 8, c) == 0)) { output[pos++] = ((char)0x5C); output[pos++] = ((char)0x30); output[pos++] = ((char)0x30); output[pos++] = ((char)(((c >> 12) & 0xF) + (((c >> 12) & 0xF) <= 9 ? 48 : 55))); output[pos++] = ((char)(((c >> 8) & 0xF) + (((c >> 8) & 0xF) <= 9 ? 48 : 55))); output[pos++] = ((char)(((c >> 4) & 0xF) + (((c >> 4) & 0xF) <= 9 ? 48 : 55))); output[pos++] = ((char)((c & 0xF) + ((c & 0xF) <= 9 ? 48 : 55))); } else { if ((((0x30 <= c) && (Bits(15, 6, c) == 0) && (Bits(5, 0, c) <= 0x39)) || ((0x41 <= c) && (Bits(15, 7, c) == 0) && (Bits(6, 0, c) <= 0x5A)) || ((0x61 <= c) && (Bits(15, 7, c) == 0) && (Bits(6, 0, c) <= 0x7A)))) { output[pos++] = ((char)c); } else { output[pos++] = ((char)0x5C); output[pos++] = ((char)0x30); output[pos++] = ((char)0x30); output[pos++] = ((char)0x30); output[pos++] = ((char)0x30); output[pos++] = ((char)(((c >> 4) & 0xF) + (((c >> 4) & 0xF) <= 9 ? 48 : 55))); output[pos++] = ((char)((c & 0xF) + ((c & 0xF) <= 9 ? 48 : 55))); } } } } }
                }
            }
            if (r0)
            {
                throw new Exception("CssEncode");
            }
            else
            {

            }
            return new String(output, 0, pos);
        }
    }

    public static class CssEncode_F
    {

        static int Bits(int m, int n, int c) { return ((c >> n) & ~(-2 << (m - n))); }

        public static string Apply(string input)
        {
            var output = new char[(input.Length * 7) + 0];
            int state = 0;
            int pos = 0;
            var chars = input.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                int c = (int)chars[i];
                switch (state)
                {
                    case (0):
                        {
                            if (((c == 0xFFFE) || (c == 0xFFFF)))
                            {
                                throw new Exception("CssEncode_F");
                            }
                            else
                            {
                                if (((0xDC00 <= c) && (c <= 0xDFFF))) { throw new Exception("CssEncode_F"); } else { if (((0xD800 <= c) && (c <= 0xDBFF))) { if ((Bits(1, 0, c) == 0)) { output[pos++] = ((char)0x5C); output[pos++] = ((char)(Bits(9, 6, c) == 0xF ? 0x31 : 0x30)); output[pos++] = ((char)(((1 + Bits(15, 6, c)) & 0xF) + (((1 + Bits(15, 6, c)) & 0xF) <= 9 ? 48 : 55))); output[pos++] = ((char)((Bits(15, 2, c) & 0xF) + ((Bits(15, 2, c) & 0xF) <= 9 ? 48 : 55))); state = 4; } else { if ((Bits(1, 0, c) == 2)) { output[pos++] = ((char)0x5C); output[pos++] = ((char)(Bits(9, 6, c) == 0xF ? 0x31 : 0x30)); output[pos++] = ((char)(((1 + Bits(15, 6, c)) & 0xF) + (((1 + Bits(15, 6, c)) & 0xF) <= 9 ? 48 : 55))); output[pos++] = ((char)((Bits(15, 2, c) & 0xF) + ((Bits(15, 2, c) & 0xF) <= 9 ? 48 : 55))); state = 3; } else { if ((Bits(1, 0, c) == 3)) { output[pos++] = ((char)0x5C); output[pos++] = ((char)(Bits(9, 6, c) == 0xF ? 0x31 : 0x30)); output[pos++] = ((char)(((1 + Bits(15, 6, c)) & 0xF) + (((1 + Bits(15, 6, c)) & 0xF) <= 9 ? 48 : 55))); output[pos++] = ((char)((Bits(15, 2, c) & 0xF) + ((Bits(15, 2, c) & 0xF) <= 9 ? 48 : 55))); state = 2; } else { output[pos++] = ((char)0x5C); output[pos++] = ((char)(Bits(9, 6, c) == 0xF ? 0x31 : 0x30)); output[pos++] = ((char)(((1 + Bits(15, 6, c)) & 0xF) + (((1 + Bits(15, 6, c)) & 0xF) <= 9 ? 48 : 55))); output[pos++] = ((char)((Bits(15, 2, c) & 0xF) + ((Bits(15, 2, c) & 0xF) <= 9 ? 48 : 55))); state = 1; } } } } else { if (!(Bits(15, 8, c) == 0)) { output[pos++] = ((char)0x5C); output[pos++] = ((char)0x30); output[pos++] = ((char)0x30); output[pos++] = ((char)(((c >> 12) & 0xF) + (((c >> 12) & 0xF) <= 9 ? 48 : 55))); output[pos++] = ((char)(((c >> 8) & 0xF) + (((c >> 8) & 0xF) <= 9 ? 48 : 55))); output[pos++] = ((char)(((c >> 4) & 0xF) + (((c >> 4) & 0xF) <= 9 ? 48 : 55))); output[pos++] = ((char)((c & 0xF) + ((c & 0xF) <= 9 ? 48 : 55))); state = 0; } else { if ((((0x30 <= c) && (Bits(15, 6, c) == 0) && (Bits(5, 0, c) <= 0x39)) || ((0x41 <= c) && (Bits(15, 7, c) == 0) && (Bits(6, 0, c) <= 0x5A)) || ((0x61 <= c) && (Bits(15, 7, c) == 0) && (Bits(6, 0, c) <= 0x7A)))) { output[pos++] = ((char)c); state = 0; } else { output[pos++] = ((char)0x5C); output[pos++] = ((char)0x30); output[pos++] = ((char)0x30); output[pos++] = ((char)0x30); output[pos++] = ((char)0x30); output[pos++] = ((char)(((c >> 4) & 0xF) + (((c >> 4) & 0xF) <= 9 ? 48 : 55))); output[pos++] = ((char)((c & 0xF) + ((c & 0xF) <= 9 ? 48 : 55))); state = 0; } } } }
                            }
                            break;
                        }
                    case (4):
                        {
                            if (((c == 0xFFFE) || (c == 0xFFFF)))
                            {
                                throw new Exception("CssEncode_F");
                            }
                            else
                            {
                                if (((0xDC00 > c) || (c > 0xDFFF))) { throw new Exception("CssEncode_F"); } else { output[pos++] = ((char)((Bits(9, 8, c) & 0xF) + ((Bits(9, 8, c) & 0xF) <= 9 ? 48 : 55))); output[pos++] = ((char)(((c >> 4) & 0xF) + (((c >> 4) & 0xF) <= 9 ? 48 : 55))); output[pos++] = ((char)((c & 0xF) + ((c & 0xF) <= 9 ? 48 : 55))); state = 0; }
                            }
                            break;
                        }
                    case (3):
                        {
                            if (((c == 0xFFFE) || (c == 0xFFFF)))
                            {
                                throw new Exception("CssEncode_F");
                            }
                            else
                            {
                                if (((0xDC00 > c) || (c > 0xDFFF))) { throw new Exception("CssEncode_F"); } else { output[pos++] = ((char)(((8 | Bits(9, 8, c)) & 0xF) + (((8 | Bits(9, 8, c)) & 0xF) <= 9 ? 48 : 55))); output[pos++] = ((char)(((c >> 4) & 0xF) + (((c >> 4) & 0xF) <= 9 ? 48 : 55))); output[pos++] = ((char)((c & 0xF) + ((c & 0xF) <= 9 ? 48 : 55))); state = 0; }
                            }
                            break;
                        }
                    case (2):
                        {
                            if (((c == 0xFFFE) || (c == 0xFFFF)))
                            {
                                throw new Exception("CssEncode_F");
                            }
                            else
                            {
                                if (((0xDC00 > c) || (c > 0xDFFF))) { throw new Exception("CssEncode_F"); } else { output[pos++] = ((char)(((0xC | Bits(9, 8, c)) & 0xF) + (((0xC | Bits(9, 8, c)) & 0xF) <= 9 ? 48 : 55))); output[pos++] = ((char)(((c >> 4) & 0xF) + (((c >> 4) & 0xF) <= 9 ? 48 : 55))); output[pos++] = ((char)((c & 0xF) + ((c & 0xF) <= 9 ? 48 : 55))); state = 0; }
                            }
                            break;
                        }
                    case (1):
                        {
                            if (((c == 0xFFFE) || (c == 0xFFFF)))
                            {
                                throw new Exception("CssEncode_F");
                            }
                            else
                            {
                                if (((0xDC00 > c) || (c > 0xDFFF))) { throw new Exception("CssEncode_F"); } else { output[pos++] = ((char)(((4 | Bits(9, 8, c)) & 0xF) + (((4 | Bits(9, 8, c)) & 0xF) <= 9 ? 48 : 55))); output[pos++] = ((char)(((c >> 4) & 0xF) + (((c >> 4) & 0xF) <= 9 ? 48 : 55))); output[pos++] = ((char)((c & 0xF) + ((c & 0xF) <= 9 ? 48 : 55))); state = 0; }
                            }
                            break;
                        }
                }
            }
            if (state == 0)
            {

                state = 0;
            }
            else if (state == 4)
            {
                throw new Exception("CssEncode_F");
            }
            else if (state == 3)
            {
                throw new Exception("CssEncode_F");
            }
            else if (state == 2)
            {
                throw new Exception("CssEncode_F");
            }
            else if (state == 1)
            {
                throw new Exception("CssEncode_F");
            }
            return new String(output, 0, pos);
        }
    }


    //boolean exploration
    public static class CssEncode_B
    {

        static int Bits(int m, int n, int c) { return ((c >> n) & ~(-2 << (m - n))); }

        public static string Apply(string input)
        {
            var output = new char[(input.Length * 7) + 0];
            int r0 = 0; int state = 0;
            int pos = 0;
            var chars = input.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                int c = (int)chars[i];
                switch (state)
                {
                    case (0):
                        {
                            if (((c == 0xFFFE) || (c == 0xFFFF)))
                            {
                                throw new Exception("CssEncode_B");
                            }
                            else
                            {
                                if (((0xDC00 <= c) && (c <= 0xDFFF))) { throw new Exception("CssEncode_B"); } else { if (((0xD800 <= c) && (c <= 0xDBFF))) { output[pos++] = ((char)0x5C); output[pos++] = ((char)(Bits(9, 6, c) == 0xF ? 0x31 : 0x30)); output[pos++] = ((char)(((1 + Bits(15, 6, c)) & 0xF) + (((1 + Bits(15, 6, c)) & 0xF) <= 9 ? 48 : 55))); output[pos++] = ((char)((Bits(15, 2, c) & 0xF) + ((Bits(15, 2, c) & 0xF) <= 9 ? 48 : 55))); r0 = Bits(1, 0, c); state = 1; } else { if (!(Bits(15, 8, c) == 0)) { output[pos++] = ((char)0x5C); output[pos++] = ((char)0x30); output[pos++] = ((char)0x30); output[pos++] = ((char)(((c >> 12) & 0xF) + (((c >> 12) & 0xF) <= 9 ? 48 : 55))); output[pos++] = ((char)(((c >> 8) & 0xF) + (((c >> 8) & 0xF) <= 9 ? 48 : 55))); output[pos++] = ((char)(((c >> 4) & 0xF) + (((c >> 4) & 0xF) <= 9 ? 48 : 55))); output[pos++] = ((char)((c & 0xF) + ((c & 0xF) <= 9 ? 48 : 55))); state = 0; } else { if ((((0x30 <= c) && (Bits(15, 6, c) == 0) && (Bits(5, 0, c) <= 0x39)) || ((0x41 <= c) && (Bits(15, 7, c) == 0) && (Bits(6, 0, c) <= 0x5A)) || ((0x61 <= c) && (Bits(15, 7, c) == 0) && (Bits(6, 0, c) <= 0x7A)))) { output[pos++] = ((char)c); state = 0; } else { output[pos++] = ((char)0x5C); output[pos++] = ((char)0x30); output[pos++] = ((char)0x30); output[pos++] = ((char)0x30); output[pos++] = ((char)0x30); output[pos++] = ((char)(((c >> 4) & 0xF) + (((c >> 4) & 0xF) <= 9 ? 48 : 55))); output[pos++] = ((char)((c & 0xF) + ((c & 0xF) <= 9 ? 48 : 55))); state = 0; } } } }
                            }
                            break;
                        }
                    case (1):
                        {
                            if (((c == 0xFFFE) || (c == 0xFFFF)))
                            {
                                throw new Exception("CssEncode_B");
                            }
                            else
                            {
                                if (((0xDC00 > c) || (c > 0xDFFF))) { throw new Exception("CssEncode_B"); } else { output[pos++] = ((char)((((Bits(13, 0, r0) << 2) | Bits(9, 8, c)) & 0xF) + ((((Bits(13, 0, r0) << 2) | Bits(9, 8, c)) & 0xF) <= 9 ? 48 : 55))); output[pos++] = ((char)(((c >> 4) & 0xF) + (((c >> 4) & 0xF) <= 9 ? 48 : 55))); output[pos++] = ((char)((c & 0xF) + ((c & 0xF) <= 9 ? 48 : 55))); r0 = 0; state = 0; }
                            }
                            break;
                        }
                }
            }
            if (state == 0)
            {

                state = 0;
            }
            else if (state == 1)
            {
                throw new Exception("CssEncode_B");
            }
            return new String(output, 0, pos);
        }
    }

    public static class UTF8
    {
        public static string Apply(string input)
        {
            byte[] encoding = Encoding.UTF8.GetBytes(input);
            char[] chars = Array.ConvertAll(encoding, b => (char)b);
            string output_expected = new String(chars);
            return output_expected;
        }
    }

    public static class UTF8Encode_F
    {
        public static string Apply(string input)
        {
            var output = new char[(input.Length * 3) + 0];
            int state = 0;
            int pos = 0;
            var chars = input.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                int c = (int)chars[i];
                //switch (state)
                {
                    if (state == 0)
                    {
                        if ((c <= 127))
                        {
                            output[pos++] = ((char)c);
                            state = 0;
                        }
                        else
                        {
                            if ((c <= 2047))
                            {
                                output[pos++] = ((char)(((c >> 6) & 31) | 192));
                                output[pos++] = ((char)((c & 63) | 128));
                                state = 0;
                            }
                            else
                            {
                                if (((55296 > c) || (c > 56319)))
                                {
                                    if (((56320 <= c) && (c <= 57343)))
                                    {
                                        throw new Exception("InvalidSurrogatePairException");
                                    }
                                    else
                                    {
                                        output[pos++] = ((char)(((c >> 12) & 15) | 224));
                                        output[pos++] = ((char)(((c >> 6) & 63) | 128));
                                        output[pos++] = ((char)((c & 63) | 128));
                                        state = 0;
                                    }
                                }
                                else
                                {
                                    if ((!(1 == ((c >> 0) & 1)) && !(1 == ((c >> 1) & 1))))
                                    {
                                        output[pos++] = ((char)((((1 + ((c >> 6) & 15)) >> 2) & 7) | 240));
                                        output[pos++] = ((char)(((c >> 2) & 15) | ((((1 + ((c >> 6) & 15)) & 3) << 4) | 128)));
                                        state = 4;
                                    }
                                    else
                                    {
                                        if (((1 == ((c >> 1) & 1)) && !(1 == ((c >> 0) & 1))))
                                        {
                                            output[pos++] = ((char)((((1 + ((c >> 6) & 15)) >> 2) & 7) | 240));
                                            output[pos++] = ((char)(((c >> 2) & 15) | ((((1 + ((c >> 6) & 15)) & 3) << 4) | 128)));
                                            state = 3;
                                        }
                                        else
                                        {
                                            if (((1 == ((c >> 0) & 1)) && (1 == ((c >> 1) & 1))))
                                            {
                                                output[pos++] = ((char)((((1 + ((c >> 6) & 15)) >> 2) & 7) | 240));
                                                output[pos++] = ((char)(((c >> 2) & 15) | ((((1 + ((c >> 6) & 15)) & 3) << 4) | 128)));
                                                state = 2;
                                            }
                                            else
                                            {
                                                output[pos++] = ((char)((((1 + ((c >> 6) & 15)) >> 2) & 7) | 240));
                                                output[pos++] = ((char)(((c >> 2) & 15) | ((((1 + ((c >> 6) & 15)) & 3) << 4) | 128)));
                                                state = 1;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (state == 4)
                    {
                        if (((56320 > c) || (c > 57343)))
                        {
                            throw new Exception("InvalidSurrogatePairException");
                        }
                        else
                        {
                            output[pos++] = ((char)(((c >> 6) & 15) | 128));
                            output[pos++] = ((char)((c & 63) | 128));
                            state = 0;
                        }
                    }
                    else if (state == 3)
                    {
                        if (((56320 > c) || (c > 57343)))
                        {
                            throw new Exception("InvalidSurrogatePairException");
                        }
                        else
                        {
                            output[pos++] = ((char)(((c >> 6) & 15) | 160));
                            output[pos++] = ((char)((c & 63) | 128));
                            state = 0;
                        }
                    }
                    else if (state == 2)
                    {
                        if (((56320 > c) || (c > 57343)))
                        {
                            throw new Exception("InvalidSurrogatePairException");
                        }
                        else
                        {
                            output[pos++] = ((char)(((c >> 6) & 15) | 176));
                            output[pos++] = ((char)((c & 63) | 128));
                            state = 0;
                        }
                    }
                    else
                    {
                        if (((56320 > c) || (c > 57343)))
                        {
                            throw new Exception("InvalidSurrogatePairException");
                        }
                        else
                        {
                            output[pos++] = ((char)(((c >> 6) & 15) | 144));
                            output[pos++] = ((char)((c & 63) | 128));
                            state = 0;
                        }
                    }
                }
            }
            if (state == 0)
            {

                state = 0;
            }
            else if (state == 4)
            {
                throw new Exception("InvalidSurrogatePairException");
            }
            else if (state == 3)
            {
                throw new Exception("InvalidSurrogatePairException");
            }
            else if (state == 2)
            {
                throw new Exception("InvalidSurrogatePairException");
            }
            else if (state == 1)
            {
                throw new Exception("InvalidSurrogatePairException");
            }
            return new String(output, 0, pos);
        }
    }
}
