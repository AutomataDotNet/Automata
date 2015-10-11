
namespace Microsoft.Bek.Tests
{
    public class HtmlEncodeStrict
    {
        static int H(int _0) { return ((0 <= _0) && (_0 <= 9) ? _0 + 0x30 : _0 + 0x37); }

        public static string Apply(string input)
        {
            var output = new System.Text.StringBuilder();

            var chars = input.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                int c = (int)chars[i];
                switch (c)
                {
                    case (0x3C):
                        {
                            output.Append(new char[] { (char)0x26, (char)0x6C, (char)0x74, (char)0x3B });
                            break;
                        }
                    case (0x3E):
                        {
                            output.Append(new char[] { (char)0x26, (char)0x67, (char)0x74, (char)0x3B });
                            break;
                        }
                    case (0x22):
                        {
                            output.Append(new char[] { (char)0x26, (char)0x71, (char)0x75, (char)0x6F, (char)0x74, (char)0x3B });
                            break;
                        }
                    case (0x26):
                        {
                            output.Append(new char[] { (char)0x26, (char)0x61, (char)0x6D, (char)0x70, (char)0x3B });
                            break;
                        }
                    case (0x27):
                        {
                            output.Append(new char[] { (char)0x26, (char)0x61, (char)0x70, (char)0x6F, (char)0x73, (char)0x3B });
                            break;
                        }
                    default:
                        {
                            if (((c == 0) || ((0x20 <= c) && (((c >> 6) & 0x3FF) == 0) && ((c & 0x3F) <= 0x21)) || ((0x23 <= c) && (((c >> 6) & 0x3FF) == 0) && ((c & 0x3F) <= 0x25)) || ((0x28 <= c) && (((c >> 6) & 0x3FF) == 0) && ((c & 0x3F) <= 0x3B)) || (c == 0x3D) || ((0x3F <= c) && (((c >> 7) & 0x1FF) == 0) && ((c & 0x7F) <= 0x7E))))
                            {
                                output.Append((char)c);
                            }
                            else
                            {
                                if (((1 <= c) && (((c >> 4) & 0xFFF) == 0)))
                                {
                                    output.Append(new char[] { (char)0x26, (char)0x23, (char)0x58, (char)H(c), (char)0x3B });
                                }
                                else
                                {
                                    if ((((0x10 <= c) && (((c >> 5) & 0x7FF) == 0)) || ((0x7F <= c) && (((c >> 8) & 0xFF) == 0))))
                                    {
                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)0x58, (char)H((c >> 4) & 0xFFF), (char)H(c & 0xF), (char)0x3B });
                                    }
                                    else
                                    {
                                        if (((0x100 <= c) && (((c >> 12) & 0xF) == 0)))
                                        {
                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)0x58, (char)H((c >> 8) & 0xFF), (char)H((c >> 4) & 0xF), (char)H(c & 0xF), (char)0x3B });
                                        }
                                        else
                                        {
                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)0x58, (char)H((c >> 12) & 0xF), (char)H((c >> 8) & 0xF), (char)H((c >> 4) & 0xF), (char)H(c & 0xF), (char)0x3B });
                                        }
                                    }
                                }
                            }
                            break;
                        }
                }
            }
            return output.ToString();
        }
    }
}
