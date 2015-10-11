namespace Microsoft.Bek.Tests
{
    public class base64encode_F
    {
        static int Bits(int m, int n, int c) { return ((c >> n) & ~(-2 << (m - n))); }
        static int E(int x) { return ((x <= 0x19) ? (x + 0x41) : ((x <= 0x33) ? (x + 0x47) : ((x <= 0x3D) ? (x - 0x4) : ((x == 0x3E) ? 0x2B : 0x2F)))); }
        static int D(int x) { return ((x == 0x2F) ? 0x3F : ((x == 0x2B) ? 0x3E : ((x <= 0x39) ? (x + 0x4) : ((x <= 0x5A) ? (x - 0x41) : (x - 0x47))))); }


        public static System.Collections.Generic.IEnumerable<char> Apply(System.Collections.Generic.IEnumerable<char> input)
        {
            int state = q0;
            foreach (char c in input)
            {
                foreach (char d in Psi(state, (int)c))
                    yield return d;
                state = Delta(state, (int)c);
            }
            if (F.Contains(state))
            {
                foreach (char d in Psi(state, -1))
                    yield return d;
            }
            else
                throw new System.Exception("base64encode_F");
        }


        public static System.Collections.Generic.ICollection<int> Q { get { return new int[] { 0, 4, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 3, 2, 1, -1 }; } }

        public static int q0 { get { return 0; } }

        public static System.Collections.Generic.ICollection<int> F { get { return new int[] { 0, 4, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 3, 2, 1, }; } }

        public static int Delta(int state, int c)
        {
            if (c < 0) return state;
            switch (state)
            {
                case (0):
                    {
                        if (!(Bits(7, 0, c) == c))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(1, 0, c) == 0)) { state = 4; } else { if ((Bits(1, 0, c) == 2)) { state = 3; } else { if ((Bits(1, 0, c) == 1)) { state = 2; } else { state = 1; } } }
                        }
                        break;
                    }
                case (4):
                    {
                        if (!(Bits(7, 0, c) == c))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, c) == 0)) { state = 20; } else { if ((Bits(3, 0, c) == 2)) { state = 19; } else { if ((Bits(3, 0, c) == 4)) { state = 18; } else { if ((Bits(3, 0, c) == 1)) { state = 17; } else { if ((Bits(3, 0, c) == 3)) { state = 16; } else { if ((Bits(3, 0, c) == 7)) { state = 15; } else { if ((Bits(3, 0, c) == 5)) { state = 14; } else { if ((Bits(3, 0, c) == 8)) { state = 13; } else { if ((Bits(3, 0, c) == 0xA)) { state = 12; } else { if ((Bits(3, 0, c) == 0xB)) { state = 11; } else { if ((Bits(3, 0, c) == 9)) { state = 10; } else { if ((Bits(3, 0, c) == 0xC)) { state = 9; } else { if ((Bits(3, 0, c) == 0xE)) { state = 8; } else { if ((Bits(3, 0, c) == 0xF)) { state = 7; } else { if ((Bits(3, 0, c) == 0xD)) { state = 6; } else { state = 5; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (20):
                    {
                        if (!(Bits(7, 0, c) == c))
                        {
                            state = -1;
                        }
                        else
                        {
                            state = 0;
                        }
                        break;
                    }
                case (19):
                    {
                        if (!(Bits(7, 0, c) == c))
                        {
                            state = -1;
                        }
                        else
                        {
                            state = 0;
                        }
                        break;
                    }
                case (18):
                    {
                        if (!(Bits(7, 0, c) == c))
                        {
                            state = -1;
                        }
                        else
                        {
                            state = 0;
                        }
                        break;
                    }
                case (17):
                    {
                        if (!(Bits(7, 0, c) == c))
                        {
                            state = -1;
                        }
                        else
                        {
                            state = 0;
                        }
                        break;
                    }
                case (16):
                    {
                        if (!(Bits(7, 0, c) == c))
                        {
                            state = -1;
                        }
                        else
                        {
                            state = 0;
                        }
                        break;
                    }
                case (15):
                    {
                        if (!(Bits(7, 0, c) == c))
                        {
                            state = -1;
                        }
                        else
                        {
                            state = 0;
                        }
                        break;
                    }
                case (14):
                    {
                        if (!(Bits(7, 0, c) == c))
                        {
                            state = -1;
                        }
                        else
                        {
                            state = 0;
                        }
                        break;
                    }
                case (13):
                    {
                        if (!(Bits(7, 0, c) == c))
                        {
                            state = -1;
                        }
                        else
                        {
                            state = 0;
                        }
                        break;
                    }
                case (12):
                    {
                        if (!(Bits(7, 0, c) == c))
                        {
                            state = -1;
                        }
                        else
                        {
                            state = 0;
                        }
                        break;
                    }
                case (11):
                    {
                        if (!(Bits(7, 0, c) == c))
                        {
                            state = -1;
                        }
                        else
                        {
                            state = 0;
                        }
                        break;
                    }
                case (10):
                    {
                        if (!(Bits(7, 0, c) == c))
                        {
                            state = -1;
                        }
                        else
                        {
                            state = 0;
                        }
                        break;
                    }
                case (9):
                    {
                        if (!(Bits(7, 0, c) == c))
                        {
                            state = -1;
                        }
                        else
                        {
                            state = 0;
                        }
                        break;
                    }
                case (8):
                    {
                        if (!(Bits(7, 0, c) == c))
                        {
                            state = -1;
                        }
                        else
                        {
                            state = 0;
                        }
                        break;
                    }
                case (7):
                    {
                        if (!(Bits(7, 0, c) == c))
                        {
                            state = -1;
                        }
                        else
                        {
                            state = 0;
                        }
                        break;
                    }
                case (6):
                    {
                        if (!(Bits(7, 0, c) == c))
                        {
                            state = -1;
                        }
                        else
                        {
                            state = 0;
                        }
                        break;
                    }
                case (5):
                    {
                        if (!(Bits(7, 0, c) == c))
                        {
                            state = -1;
                        }
                        else
                        {
                            state = 0;
                        }
                        break;
                    }
                case (3):
                    {
                        if (!(Bits(7, 0, c) == c))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, c) == 0)) { state = 20; } else { if ((Bits(3, 0, c) == 2)) { state = 19; } else { if ((Bits(3, 0, c) == 4)) { state = 18; } else { if ((Bits(3, 0, c) == 1)) { state = 17; } else { if ((Bits(3, 0, c) == 3)) { state = 16; } else { if ((Bits(3, 0, c) == 7)) { state = 15; } else { if ((Bits(3, 0, c) == 5)) { state = 14; } else { if ((Bits(3, 0, c) == 8)) { state = 13; } else { if ((Bits(3, 0, c) == 0xA)) { state = 12; } else { if ((Bits(3, 0, c) == 0xB)) { state = 11; } else { if ((Bits(3, 0, c) == 9)) { state = 10; } else { if ((Bits(3, 0, c) == 0xC)) { state = 9; } else { if ((Bits(3, 0, c) == 0xE)) { state = 8; } else { if ((Bits(3, 0, c) == 0xF)) { state = 7; } else { if ((Bits(3, 0, c) == 0xD)) { state = 6; } else { state = 5; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (2):
                    {
                        if (!(Bits(7, 0, c) == c))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, c) == 0)) { state = 20; } else { if ((Bits(3, 0, c) == 2)) { state = 19; } else { if ((Bits(3, 0, c) == 4)) { state = 18; } else { if ((Bits(3, 0, c) == 1)) { state = 17; } else { if ((Bits(3, 0, c) == 3)) { state = 16; } else { if ((Bits(3, 0, c) == 7)) { state = 15; } else { if ((Bits(3, 0, c) == 5)) { state = 14; } else { if ((Bits(3, 0, c) == 8)) { state = 13; } else { if ((Bits(3, 0, c) == 0xA)) { state = 12; } else { if ((Bits(3, 0, c) == 0xB)) { state = 11; } else { if ((Bits(3, 0, c) == 9)) { state = 10; } else { if ((Bits(3, 0, c) == 0xC)) { state = 9; } else { if ((Bits(3, 0, c) == 0xE)) { state = 8; } else { if ((Bits(3, 0, c) == 0xF)) { state = 7; } else { if ((Bits(3, 0, c) == 0xD)) { state = 6; } else { state = 5; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (1):
                    {
                        if (!(Bits(7, 0, c) == c))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, c) == 0)) { state = 20; } else { if ((Bits(3, 0, c) == 2)) { state = 19; } else { if ((Bits(3, 0, c) == 4)) { state = 18; } else { if ((Bits(3, 0, c) == 1)) { state = 17; } else { if ((Bits(3, 0, c) == 3)) { state = 16; } else { if ((Bits(3, 0, c) == 7)) { state = 15; } else { if ((Bits(3, 0, c) == 5)) { state = 14; } else { if ((Bits(3, 0, c) == 8)) { state = 13; } else { if ((Bits(3, 0, c) == 0xA)) { state = 12; } else { if ((Bits(3, 0, c) == 0xB)) { state = 11; } else { if ((Bits(3, 0, c) == 9)) { state = 10; } else { if ((Bits(3, 0, c) == 0xC)) { state = 9; } else { if ((Bits(3, 0, c) == 0xE)) { state = 8; } else { if ((Bits(3, 0, c) == 0xF)) { state = 7; } else { if ((Bits(3, 0, c) == 0xD)) { state = 6; } else { state = 5; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                default:
                    {
                        state = -1;
                        break;
                    }
            }
            return state;
        }

        public static System.Collections.Generic.IEnumerable<char> Psi(int state, int c)
        {
            if (c >= 0)
            {
                switch (state)
                {
                    case (0):
                        {
                            if (!(Bits(7, 0, c) == c))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)E(Bits(15, 2, c)));
                            }
                            break;
                        }
                    case (4):
                        {
                            if (!(Bits(7, 0, c) == c))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)E(Bits(15, 4, c)));
                            }
                            break;
                        }
                    case (20):
                        {
                            if (!(Bits(7, 0, c) == c))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)E(Bits(15, 6, c))); yield return ((char)E(Bits(5, 0, c)));
                            }
                            break;
                        }
                    case (19):
                        {
                            if (!(Bits(7, 0, c) == c))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)E((((Bits(15, 10, c) << 1) | 1) << 3) | Bits(8, 6, c))); yield return ((char)E(Bits(5, 0, c)));
                            }
                            break;
                        }
                    case (18):
                        {
                            if (!(Bits(7, 0, c) == c))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)E((((Bits(15, 11, c) << 1) | 1) << 4) | Bits(9, 6, c))); yield return ((char)E(Bits(5, 0, c)));
                            }
                            break;
                        }
                    case (17):
                        {
                            if (!(Bits(7, 0, c) == c))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)E((((Bits(15, 9, c) << 1) | 1) << 2) | Bits(7, 6, c))); yield return ((char)E(Bits(5, 0, c)));
                            }
                            break;
                        }
                    case (16):
                        {
                            if (!(Bits(7, 0, c) == c))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)E((((Bits(15, 10, c) << 2) | 3) << 2) | Bits(7, 6, c))); yield return ((char)E(Bits(5, 0, c)));
                            }
                            break;
                        }
                    case (15):
                        {
                            if (!(Bits(7, 0, c) == c))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)E((((Bits(15, 11, c) << 3) | 7) << 2) | Bits(7, 6, c))); yield return ((char)E(Bits(5, 0, c)));
                            }
                            break;
                        }
                    case (14):
                        {
                            if (!(Bits(7, 0, c) == c))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)E((((((((Bits(15, 11, c) << 1) | 1) << 1) | ((c >> 9) & 1)) << 1) | 1) << 2) | Bits(7, 6, c))); yield return ((char)E(Bits(5, 0, c)));
                            }
                            break;
                        }
                    case (13):
                        {
                            if (!(Bits(7, 0, c) == c))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)E((((Bits(15, 12, c) << 1) | 1) << 5) | Bits(10, 6, c))); yield return ((char)E(Bits(5, 0, c)));
                            }
                            break;
                        }
                    case (12):
                        {
                            if (!(Bits(7, 0, c) == c))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)E((((((((Bits(15, 12, c) << 1) | 1) << 1) | ((c >> 10) & 1)) << 1) | 1) << 3) | Bits(8, 6, c))); yield return ((char)E(Bits(5, 0, c)));
                            }
                            break;
                        }
                    case (11):
                        {
                            if (!(Bits(7, 0, c) == c))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)E((((((((Bits(15, 12, c) << 1) | 1) << 1) | ((c >> 10) & 1)) << 2) | 3) << 2) | Bits(7, 6, c))); yield return ((char)E(Bits(5, 0, c)));
                            }
                            break;
                        }
                    case (10):
                        {
                            if (!(Bits(7, 0, c) == c))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)E((((((((Bits(15, 12, c) << 1) | 1) << 2) | Bits(10, 9, c)) << 1) | 1) << 2) | Bits(7, 6, c))); yield return ((char)E(Bits(5, 0, c)));
                            }
                            break;
                        }
                    case (9):
                        {
                            if (!(Bits(7, 0, c) == c))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)E((((Bits(15, 12, c) << 2) | 3) << 4) | Bits(9, 6, c))); yield return ((char)E(Bits(5, 0, c)));
                            }
                            break;
                        }
                    case (8):
                        {
                            if (!(Bits(7, 0, c) == c))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)E((((Bits(15, 12, c) << 3) | 7) << 3) | Bits(8, 6, c))); yield return ((char)E(Bits(5, 0, c)));
                            }
                            break;
                        }
                    case (7):
                        {
                            if (!(Bits(7, 0, c) == c))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)E((((Bits(15, 12, c) << 4) | 0xF) << 2) | Bits(7, 6, c))); yield return ((char)E(Bits(5, 0, c)));
                            }
                            break;
                        }
                    case (6):
                        {
                            if (!(Bits(7, 0, c) == c))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)E((((((((Bits(15, 12, c) << 2) | 3) << 1) | ((c >> 9) & 1)) << 1) | 1) << 2) | Bits(7, 6, c))); yield return ((char)E(Bits(5, 0, c)));
                            }
                            break;
                        }
                    case (5):
                        {
                            if (!(Bits(7, 0, c) == c))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)E((((Bits(15, 11, c) << 2) | 3) << 3) | Bits(8, 6, c))); yield return ((char)E(Bits(5, 0, c)));
                            }
                            break;
                        }
                    case (3):
                        {
                            if (!(Bits(7, 0, c) == c))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)E((((Bits(15, 10, c) << 1) | 1) << 5) | Bits(8, 4, c)));
                            }
                            break;
                        }
                    case (2):
                        {
                            if (!(Bits(7, 0, c) == c))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)E((((Bits(15, 9, c) << 1) | 1) << 4) | Bits(7, 4, c)));
                            }
                            break;
                        }
                    case (1):
                        {
                            if (!(Bits(7, 0, c) == c))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)E((((Bits(15, 10, c) << 2) | 3) << 4) | Bits(7, 4, c)));
                            }
                            break;
                        }
                    default:
                        {
                            yield break;
                        }
                }
            }
            else
            {
                switch (state)
                {
                    case (0):
                        {
                            yield break;
                            break;
                        }
                    case (4):
                        {
                            yield return ((char)E(0)); yield return ((char)0x3D); yield return ((char)0x3D);
                            break;
                        }
                    case (20):
                        {
                            yield return ((char)E(0)); yield return ((char)0x3D);
                            break;
                        }
                    case (19):
                        {
                            yield return ((char)E(8)); yield return ((char)0x3D);
                            break;
                        }
                    case (18):
                        {
                            yield return ((char)E(0x10)); yield return ((char)0x3D);
                            break;
                        }
                    case (17):
                        {
                            yield return ((char)E(4)); yield return ((char)0x3D);
                            break;
                        }
                    case (16):
                        {
                            yield return ((char)E(0xC)); yield return ((char)0x3D);
                            break;
                        }
                    case (15):
                        {
                            yield return ((char)E(0x1C)); yield return ((char)0x3D);
                            break;
                        }
                    case (14):
                        {
                            yield return ((char)E(0x14)); yield return ((char)0x3D);
                            break;
                        }
                    case (13):
                        {
                            yield return ((char)E(0x20)); yield return ((char)0x3D);
                            break;
                        }
                    case (12):
                        {
                            yield return ((char)E(0x28)); yield return ((char)0x3D);
                            break;
                        }
                    case (11):
                        {
                            yield return ((char)E(0x2C)); yield return ((char)0x3D);
                            break;
                        }
                    case (10):
                        {
                            yield return ((char)E(0x24)); yield return ((char)0x3D);
                            break;
                        }
                    case (9):
                        {
                            yield return ((char)E(0x30)); yield return ((char)0x3D);
                            break;
                        }
                    case (8):
                        {
                            yield return ((char)E(0x38)); yield return ((char)0x3D);
                            break;
                        }
                    case (7):
                        {
                            yield return ((char)E(0x3C)); yield return ((char)0x3D);
                            break;
                        }
                    case (6):
                        {
                            yield return ((char)E(0x34)); yield return ((char)0x3D);
                            break;
                        }
                    case (5):
                        {
                            yield return ((char)E(0x18)); yield return ((char)0x3D);
                            break;
                        }
                    case (3):
                        {
                            yield return ((char)E(0x20)); yield return ((char)0x3D); yield return ((char)0x3D);
                            break;
                        }
                    case (2):
                        {
                            yield return ((char)E(0x10)); yield return ((char)0x3D); yield return ((char)0x3D);
                            break;
                        }
                    case (1):
                        {
                            yield return ((char)E(0x30)); yield return ((char)0x3D); yield return ((char)0x3D);
                            break;
                        }
                    default:
                        {
                            yield break;
                        }
                }
            }
        }

    }

    public class base64decode_F
    {
        static int Bits(int m, int n, int c) { return ((c >> n) & ~(-2 << (m - n))); }
        static int E(int x) { return ((x <= 0x19) ? (x + 0x41) : ((x <= 0x33) ? (x + 0x47) : ((x <= 0x3D) ? (x - 0x4) : ((x == 0x3E) ? 0x2B : 0x2F)))); }
        static int D(int x) { return ((x == 0x2F) ? 0x3F : ((x == 0x2B) ? 0x3E : ((x <= 0x39) ? (x + 0x4) : ((x <= 0x5A) ? (x - 0x41) : (x - 0x47))))); }


        public static System.Collections.Generic.IEnumerable<char> Apply(System.Collections.Generic.IEnumerable<char> input)
        {
            int state = q0;
            foreach (char c in input)
            {
                foreach (char d in Psi(state, (int)c))
                    yield return d;
                state = Delta(state, (int)c);
            }
            if (F.Contains(state))
            {
                foreach (char d in Psi(state, -1))
                    yield return d;
            }
            else
                throw new System.Exception("base64decode_F");
        }


        public static System.Collections.Generic.ICollection<int> Q { get { return new int[] { 0, 64, 80, 84, 85, 83, 82, 81, 79, 78, 77, 76, 75, 74, 73, 72, 71, 70, 69, 68, 86, 67, 66, 65, 63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, -1 }; } }

        public static int q0 { get { return 0; } }

        public static System.Collections.Generic.ICollection<int> F { get { return new int[] { 0, 85, }; } }

        public static int Delta(int state, int c)
        {
            if (c < 0) return state;
            switch (state)
            {
                case (0):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(13, 0, D(c)) == 0x34)) { state = 64; } else { if ((Bits(13, 0, D(c)) == 0x38)) { state = 63; } else { if ((Bits(13, 0, D(c)) == 0x3A)) { state = 62; } else { if ((Bits(13, 0, D(c)) == 0x36)) { state = 61; } else { if ((Bits(13, 0, D(c)) == 0x3C)) { state = 60; } else { if ((Bits(13, 0, D(c)) == 0x35)) { state = 59; } else { if ((Bits(13, 0, D(c)) == 0x37)) { state = 58; } else { if ((Bits(13, 0, D(c)) == 0x3D)) { state = 57; } else { if ((Bits(13, 0, D(c)) == 0x39)) { state = 56; } else { if ((Bits(13, 0, D(c)) == 0x3B)) { state = 55; } else { if ((Bits(13, 0, D(c)) == 0x3F)) { state = 54; } else { if ((Bits(13, 0, D(c)) == 0x3E)) { state = 53; } else { if ((Bits(13, 0, D(c)) == 0)) { state = 52; } else { if ((Bits(13, 0, D(c)) == 1)) { state = 51; } else { if ((Bits(13, 0, D(c)) == 2)) { state = 50; } else { if ((Bits(13, 0, D(c)) == 3)) { state = 49; } else { if ((Bits(13, 0, D(c)) == 0xB)) { state = 48; } else { if ((Bits(13, 0, D(c)) == 0x13)) { state = 47; } else { if ((Bits(13, 0, D(c)) == 9)) { state = 46; } else { if ((Bits(13, 0, D(c)) == 8)) { state = 45; } else { if ((Bits(13, 0, D(c)) == 0xA)) { state = 44; } else { if ((Bits(13, 0, D(c)) == 0x2B)) { state = 43; } else { if ((Bits(13, 0, D(c)) == 0x2F)) { state = 42; } else { if ((Bits(13, 0, D(c)) == 7)) { state = 41; } else { if ((Bits(13, 0, D(c)) == 0x17)) { state = 40; } else { if ((Bits(13, 0, D(c)) == 4)) { state = 39; } else { if ((Bits(13, 0, D(c)) == 0xF)) { state = 38; } else { if ((Bits(13, 0, D(c)) == 5)) { state = 37; } else { if ((Bits(13, 0, D(c)) == 0x20)) { state = 36; } else { if ((Bits(13, 0, D(c)) == 0xD)) { state = 35; } else { if ((Bits(13, 0, D(c)) == 0x21)) { state = 34; } else { if ((Bits(13, 0, D(c)) == 0x11)) { state = 33; } else { if ((Bits(13, 0, D(c)) == 0x29)) { state = 32; } else { if ((Bits(13, 0, D(c)) == 0x1B)) { state = 31; } else { if ((Bits(13, 0, D(c)) == 0x1D)) { state = 30; } else { if ((Bits(13, 0, D(c)) == 0x1F)) { state = 29; } else { if ((Bits(13, 0, D(c)) == 0x33)) { state = 28; } else { if ((Bits(13, 0, D(c)) == 0x31)) { state = 27; } else { if ((Bits(13, 0, D(c)) == 0x30)) { state = 26; } else { if ((Bits(13, 0, D(c)) == 0x32)) { state = 25; } else { if ((Bits(13, 0, D(c)) == 0x1C)) { state = 24; } else { if ((Bits(13, 0, D(c)) == 0x1A)) { state = 23; } else { if ((Bits(13, 0, D(c)) == 0x28)) { state = 22; } else { if ((Bits(13, 0, D(c)) == 0x24)) { state = 21; } else { if ((Bits(13, 0, D(c)) == 0x25)) { state = 20; } else { if ((Bits(13, 0, D(c)) == 0x2D)) { state = 19; } else { if ((Bits(13, 0, D(c)) == 0x15)) { state = 18; } else { if ((Bits(13, 0, D(c)) == 0x19)) { state = 17; } else { if ((Bits(13, 0, D(c)) == 0xC)) { state = 16; } else { if ((Bits(13, 0, D(c)) == 0x10)) { state = 15; } else { if ((Bits(13, 0, D(c)) == 0x18)) { state = 14; } else { if ((Bits(13, 0, D(c)) == 0x14)) { state = 13; } else { if ((Bits(13, 0, D(c)) == 6)) { state = 12; } else { if ((Bits(13, 0, D(c)) == 0xE)) { state = 11; } else { if ((Bits(13, 0, D(c)) == 0x12)) { state = 10; } else { if ((Bits(13, 0, D(c)) == 0x16)) { state = 9; } else { if ((Bits(13, 0, D(c)) == 0x2C)) { state = 8; } else { if ((Bits(13, 0, D(c)) == 0x1E)) { state = 7; } else { if ((Bits(13, 0, D(c)) == 0x22)) { state = 6; } else { if ((Bits(13, 0, D(c)) == 0x2A)) { state = 5; } else { if ((Bits(13, 0, D(c)) == 0x26)) { state = 4; } else { if ((Bits(13, 0, D(c)) == 0x2E)) { state = 3; } else { if ((Bits(13, 0, D(c)) == 0x23)) { state = 2; } else { state = 1; } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (64):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { state = 65; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (80):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(1, 0, D(c)) == 0)) { state = 84; } else { if ((Bits(1, 0, D(c)) == 2)) { state = 83; } else { if ((Bits(1, 0, D(c)) == 1)) { state = 82; } else { state = 81; } } }
                        }
                        break;
                    }
                case (84):
                    {
                        if ((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((c == 0x3D)) { state = 85; } else { state = 0; }
                        }
                        break;
                    }
                case (85):
                    {
                        state = -1;
                        break;
                    }
                case (83):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            state = 0;
                        }
                        break;
                    }
                case (82):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            state = 0;
                        }
                        break;
                    }
                case (81):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            state = 0;
                        }
                        break;
                    }
                case (79):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(1, 0, D(c)) == 0)) { state = 84; } else { if ((Bits(1, 0, D(c)) == 2)) { state = 83; } else { if ((Bits(1, 0, D(c)) == 1)) { state = 82; } else { state = 81; } } }
                        }
                        break;
                    }
                case (78):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(1, 0, D(c)) == 0)) { state = 84; } else { if ((Bits(1, 0, D(c)) == 2)) { state = 83; } else { if ((Bits(1, 0, D(c)) == 3)) { state = 81; } else { state = 82; } } }
                        }
                        break;
                    }
                case (77):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(1, 0, D(c)) == 0)) { state = 84; } else { if ((Bits(1, 0, D(c)) == 2)) { state = 83; } else { if ((Bits(1, 0, D(c)) == 1)) { state = 82; } else { state = 81; } } }
                        }
                        break;
                    }
                case (76):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(1, 0, D(c)) == 0)) { state = 84; } else { if ((Bits(1, 0, D(c)) == 2)) { state = 83; } else { if ((Bits(1, 0, D(c)) == 1)) { state = 82; } else { state = 81; } } }
                        }
                        break;
                    }
                case (75):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(1, 0, D(c)) == 0)) { state = 84; } else { if ((Bits(1, 0, D(c)) == 2)) { state = 83; } else { if ((Bits(1, 0, D(c)) == 1)) { state = 82; } else { state = 81; } } }
                        }
                        break;
                    }
                case (74):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(1, 0, D(c)) == 0)) { state = 84; } else { if ((Bits(1, 0, D(c)) == 2)) { state = 83; } else { if ((Bits(1, 0, D(c)) == 1)) { state = 82; } else { state = 81; } } }
                        }
                        break;
                    }
                case (73):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(1, 0, D(c)) == 0)) { state = 84; } else { if ((Bits(1, 0, D(c)) == 2)) { state = 83; } else { if ((Bits(1, 0, D(c)) == 1)) { state = 82; } else { state = 81; } } }
                        }
                        break;
                    }
                case (72):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(1, 0, D(c)) == 0)) { state = 84; } else { if ((Bits(1, 0, D(c)) == 2)) { state = 83; } else { if ((Bits(1, 0, D(c)) == 1)) { state = 82; } else { state = 81; } } }
                        }
                        break;
                    }
                case (71):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(1, 0, D(c)) == 0)) { state = 84; } else { if ((Bits(1, 0, D(c)) == 2)) { state = 83; } else { if ((Bits(1, 0, D(c)) == 1)) { state = 82; } else { state = 81; } } }
                        }
                        break;
                    }
                case (70):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(1, 0, D(c)) == 0)) { state = 84; } else { if ((Bits(1, 0, D(c)) == 2)) { state = 83; } else { if ((Bits(1, 0, D(c)) == 1)) { state = 82; } else { state = 81; } } }
                        }
                        break;
                    }
                case (69):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(1, 0, D(c)) == 0)) { state = 84; } else { if ((Bits(1, 0, D(c)) == 2)) { state = 83; } else { if ((Bits(1, 0, D(c)) == 1)) { state = 82; } else { state = 81; } } }
                        }
                        break;
                    }
                case (68):
                    {
                        if ((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((c == 0x3D)) { state = 86; } else { if ((Bits(1, 0, D(c)) == 0)) { state = 84; } else { if ((Bits(1, 0, D(c)) == 2)) { state = 83; } else { if ((Bits(1, 0, D(c)) == 1)) { state = 82; } else { state = 81; } } } }
                        }
                        break;
                    }
                case (86):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || !(c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            state = 85;
                        }
                        break;
                    }
                case (67):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(1, 0, D(c)) == 0)) { state = 84; } else { if ((Bits(1, 0, D(c)) == 2)) { state = 83; } else { if ((Bits(1, 0, D(c)) == 1)) { state = 82; } else { state = 81; } } }
                        }
                        break;
                    }
                case (66):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(1, 0, D(c)) == 0)) { state = 84; } else { if ((Bits(1, 0, D(c)) == 2)) { state = 83; } else { if ((Bits(1, 0, D(c)) == 1)) { state = 82; } else { state = 81; } } }
                        }
                        break;
                    }
                case (65):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(1, 0, D(c)) == 0)) { state = 84; } else { if ((Bits(1, 0, D(c)) == 2)) { state = 83; } else { if ((Bits(1, 0, D(c)) == 1)) { state = 82; } else { state = 81; } } }
                        }
                        break;
                    }
                case (63):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { state = 71; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (62):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { state = 65; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (61):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { state = 65; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (60):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { state = 71; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (59):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { state = 67; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (58):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { state = 65; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (57):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { state = 65; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (56):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { state = 69; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (55):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { state = 70; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (54):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { state = 65; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (53):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { state = 69; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (52):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { state = 65; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (51):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { state = 69; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (50):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { state = 65; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (49):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { state = 65; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (48):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { state = 67; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (47):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { state = 69; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (46):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { state = 65; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (45):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { state = 67; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (44):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { state = 70; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (43):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { state = 69; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (42):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { state = 65; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (41):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { state = 65; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (40):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { state = 69; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (39):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { state = 69; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (38):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { state = 65; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (37):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { state = 70; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (36):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { state = 70; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (35):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { state = 69; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (34):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { state = 69; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (33):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { state = 65; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (32):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { state = 69; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (31):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { state = 67; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (30):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { state = 69; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (29):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { state = 70; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (28):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { state = 77; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (27):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { state = 65; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (26):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { state = 67; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (25):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { state = 69; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (24):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { state = 67; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (23):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { state = 65; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (22):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { state = 74; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (21):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { state = 69; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (20):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { state = 70; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (19):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { state = 70; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (18):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { state = 65; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (17):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { state = 70; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (16):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { state = 72; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (15):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { state = 65; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (14):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { state = 78; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (13):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { state = 69; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (12):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { state = 65; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (11):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { state = 69; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (10):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { state = 69; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (9):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { state = 65; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (8):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { state = 65; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (7):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { state = 78; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (6):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { state = 67; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (5):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { state = 68; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (4):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { state = 65; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (3):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { state = 67; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (2):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 3)) { state = 65; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { state = 69; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                case (1):
                    {
                        if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                        {
                            state = -1;
                        }
                        else
                        {
                            if ((Bits(3, 0, D(c)) == 4)) { state = 80; } else { if ((Bits(3, 0, D(c)) == 6)) { state = 78; } else { if ((Bits(3, 0, D(c)) == 8)) { state = 76; } else { if ((Bits(3, 0, D(c)) == 0xC)) { state = 79; } else { if ((Bits(3, 0, D(c)) == 0xA)) { state = 77; } else { if ((Bits(3, 0, D(c)) == 0xF)) { state = 70; } else { if ((Bits(3, 0, D(c)) == 0xE)) { state = 69; } else { if ((Bits(3, 0, D(c)) == 7)) { state = 72; } else { if ((Bits(3, 0, D(c)) == 5)) { state = 73; } else { if ((Bits(3, 0, D(c)) == 9)) { state = 75; } else { if ((Bits(3, 0, D(c)) == 0xB)) { state = 74; } else { if ((Bits(3, 0, D(c)) == 0xD)) { state = 71; } else { if ((Bits(3, 0, D(c)) == 0)) { state = 68; } else { if ((Bits(3, 0, D(c)) == 2)) { state = 67; } else { if ((Bits(3, 0, D(c)) == 1)) { state = 66; } else { state = 65; } } } } } } } } } } } } } } }
                        }
                        break;
                    }
                default:
                    {
                        state = -1;
                        break;
                    }
            }
            return state;
        }

        public static System.Collections.Generic.IEnumerable<char> Psi(int state, int c)
        {
            if (c >= 0)
            {
                switch (state)
                {
                    case (0):
                        {
                            yield break;
                            break;
                        }
                    case (64):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0xD0 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (80):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x40 | Bits(5, 2, D(c))));
                            }
                            break;
                        }
                    case (84):
                        {
                            if ((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))))
                            {
                                yield break;
                            }
                            else
                            {
                                if ((c == 0x3D)) { yield break; } else { yield return ((char)D(c)); }
                            }
                            break;
                        }
                    case (85):
                        {
                            yield break;
                            break;
                        }
                    case (83):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)((((Bits(15, 8, D(c)) << 1) | 1) << 7) | Bits(6, 0, D(c))));
                            }
                            break;
                        }
                    case (82):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)((((Bits(15, 7, D(c)) << 1) | 1) << 6) | Bits(5, 0, D(c))));
                            }
                            break;
                        }
                    case (81):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)((((Bits(15, 8, D(c)) << 2) | 3) << 6) | Bits(5, 0, D(c))));
                            }
                            break;
                        }
                    case (79):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0xC0 | Bits(5, 2, D(c))));
                            }
                            break;
                        }
                    case (78):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x60 | Bits(5, 2, D(c))));
                            }
                            break;
                        }
                    case (77):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0xA0 | Bits(5, 2, D(c))));
                            }
                            break;
                        }
                    case (76):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x80 | Bits(5, 2, D(c))));
                            }
                            break;
                        }
                    case (75):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x90 | Bits(5, 2, D(c))));
                            }
                            break;
                        }
                    case (74):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0xB0 | Bits(5, 2, D(c))));
                            }
                            break;
                        }
                    case (73):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x50 | Bits(5, 2, D(c))));
                            }
                            break;
                        }
                    case (72):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x70 | Bits(5, 2, D(c))));
                            }
                            break;
                        }
                    case (71):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0xD0 | Bits(5, 2, D(c))));
                            }
                            break;
                        }
                    case (70):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0xF0 | Bits(5, 2, D(c))));
                            }
                            break;
                        }
                    case (69):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0xE0 | Bits(5, 2, D(c))));
                            }
                            break;
                        }
                    case (68):
                        {
                            if ((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))))
                            {
                                yield break;
                            }
                            else
                            {
                                if ((c == 0x3D)) { yield break; } else { yield return ((char)Bits(5, 2, D(c))); }
                            }
                            break;
                        }
                    case (86):
                        {
                            yield break;
                            break;
                        }
                    case (67):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x20 | Bits(5, 2, D(c))));
                            }
                            break;
                        }
                    case (66):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x10 | Bits(5, 2, D(c))));
                            }
                            break;
                        }
                    case (65):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x30 | Bits(5, 2, D(c))));
                            }
                            break;
                        }
                    case (63):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0xE0 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (62):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0xE8 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (61):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0xD8 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (60):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0xF0 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (59):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0xD4 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (58):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0xDC | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (57):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0xF4 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (56):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0xE4 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (55):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0xEC | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (54):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0xFC | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (53):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0xF8 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (52):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)Bits(5, 4, D(c)));
                            }
                            break;
                        }
                    case (51):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(4 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (50):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(8 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (49):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0xC | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (48):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x2C | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (47):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x4C | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (46):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x24 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (45):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x20 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (44):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x28 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (43):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0xAC | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (42):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0xBC | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (41):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x1C | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (40):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x5C | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (39):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x10 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (38):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x3C | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (37):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x14 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (36):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x80 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (35):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x34 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (34):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x84 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (33):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x44 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (32):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0xA4 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (31):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x6C | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (30):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x74 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (29):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x7C | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (28):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0xCC | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (27):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0xC4 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (26):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0xC0 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (25):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0xC8 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (24):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x70 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (23):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x68 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (22):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0xA0 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (21):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x90 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (20):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x94 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (19):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0xB4 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (18):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x54 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (17):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x64 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (16):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x30 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (15):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x40 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (14):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x60 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (13):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x50 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (12):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x18 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (11):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x38 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (10):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x48 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (9):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x58 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (8):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0xB0 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (7):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x78 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (6):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x88 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (5):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0xA8 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (4):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x98 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (3):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0xB8 | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (2):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x8C | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    case (1):
                        {
                            if (((!(c == 0x2B) && ((0x2F > c) || !(Bits(15, 6, c) == 0) || (Bits(5, 0, c) > 0x39)) && !(c == 0x3D) && ((0x41 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x5A)) && ((0x61 > c) || !(Bits(15, 7, c) == 0) || (Bits(6, 0, c) > 0x7A))) || (c == 0x3D)))
                            {
                                yield break;
                            }
                            else
                            {
                                yield return ((char)(0x9C | Bits(5, 4, D(c))));
                            }
                            break;
                        }
                    default:
                        {
                            yield break;
                        }
                }
            }
            else
            {
                switch (state)
                {
                    case (0):
                        {
                            yield break;
                            break;
                        }
                    case (64):
                        {
                            yield break;
                            break;
                        }
                    case (80):
                        {
                            yield break;
                            break;
                        }
                    case (84):
                        {
                            yield break;
                            break;
                        }
                    case (85):
                        {
                            yield break;
                            break;
                        }
                    case (83):
                        {
                            yield break;
                            break;
                        }
                    case (82):
                        {
                            yield break;
                            break;
                        }
                    case (81):
                        {
                            yield break;
                            break;
                        }
                    case (79):
                        {
                            yield break;
                            break;
                        }
                    case (78):
                        {
                            yield break;
                            break;
                        }
                    case (77):
                        {
                            yield break;
                            break;
                        }
                    case (76):
                        {
                            yield break;
                            break;
                        }
                    case (75):
                        {
                            yield break;
                            break;
                        }
                    case (74):
                        {
                            yield break;
                            break;
                        }
                    case (73):
                        {
                            yield break;
                            break;
                        }
                    case (72):
                        {
                            yield break;
                            break;
                        }
                    case (71):
                        {
                            yield break;
                            break;
                        }
                    case (70):
                        {
                            yield break;
                            break;
                        }
                    case (69):
                        {
                            yield break;
                            break;
                        }
                    case (68):
                        {
                            yield break;
                            break;
                        }
                    case (86):
                        {
                            yield break;
                            break;
                        }
                    case (67):
                        {
                            yield break;
                            break;
                        }
                    case (66):
                        {
                            yield break;
                            break;
                        }
                    case (65):
                        {
                            yield break;
                            break;
                        }
                    case (63):
                        {
                            yield break;
                            break;
                        }
                    case (62):
                        {
                            yield break;
                            break;
                        }
                    case (61):
                        {
                            yield break;
                            break;
                        }
                    case (60):
                        {
                            yield break;
                            break;
                        }
                    case (59):
                        {
                            yield break;
                            break;
                        }
                    case (58):
                        {
                            yield break;
                            break;
                        }
                    case (57):
                        {
                            yield break;
                            break;
                        }
                    case (56):
                        {
                            yield break;
                            break;
                        }
                    case (55):
                        {
                            yield break;
                            break;
                        }
                    case (54):
                        {
                            yield break;
                            break;
                        }
                    case (53):
                        {
                            yield break;
                            break;
                        }
                    case (52):
                        {
                            yield break;
                            break;
                        }
                    case (51):
                        {
                            yield break;
                            break;
                        }
                    case (50):
                        {
                            yield break;
                            break;
                        }
                    case (49):
                        {
                            yield break;
                            break;
                        }
                    case (48):
                        {
                            yield break;
                            break;
                        }
                    case (47):
                        {
                            yield break;
                            break;
                        }
                    case (46):
                        {
                            yield break;
                            break;
                        }
                    case (45):
                        {
                            yield break;
                            break;
                        }
                    case (44):
                        {
                            yield break;
                            break;
                        }
                    case (43):
                        {
                            yield break;
                            break;
                        }
                    case (42):
                        {
                            yield break;
                            break;
                        }
                    case (41):
                        {
                            yield break;
                            break;
                        }
                    case (40):
                        {
                            yield break;
                            break;
                        }
                    case (39):
                        {
                            yield break;
                            break;
                        }
                    case (38):
                        {
                            yield break;
                            break;
                        }
                    case (37):
                        {
                            yield break;
                            break;
                        }
                    case (36):
                        {
                            yield break;
                            break;
                        }
                    case (35):
                        {
                            yield break;
                            break;
                        }
                    case (34):
                        {
                            yield break;
                            break;
                        }
                    case (33):
                        {
                            yield break;
                            break;
                        }
                    case (32):
                        {
                            yield break;
                            break;
                        }
                    case (31):
                        {
                            yield break;
                            break;
                        }
                    case (30):
                        {
                            yield break;
                            break;
                        }
                    case (29):
                        {
                            yield break;
                            break;
                        }
                    case (28):
                        {
                            yield break;
                            break;
                        }
                    case (27):
                        {
                            yield break;
                            break;
                        }
                    case (26):
                        {
                            yield break;
                            break;
                        }
                    case (25):
                        {
                            yield break;
                            break;
                        }
                    case (24):
                        {
                            yield break;
                            break;
                        }
                    case (23):
                        {
                            yield break;
                            break;
                        }
                    case (22):
                        {
                            yield break;
                            break;
                        }
                    case (21):
                        {
                            yield break;
                            break;
                        }
                    case (20):
                        {
                            yield break;
                            break;
                        }
                    case (19):
                        {
                            yield break;
                            break;
                        }
                    case (18):
                        {
                            yield break;
                            break;
                        }
                    case (17):
                        {
                            yield break;
                            break;
                        }
                    case (16):
                        {
                            yield break;
                            break;
                        }
                    case (15):
                        {
                            yield break;
                            break;
                        }
                    case (14):
                        {
                            yield break;
                            break;
                        }
                    case (13):
                        {
                            yield break;
                            break;
                        }
                    case (12):
                        {
                            yield break;
                            break;
                        }
                    case (11):
                        {
                            yield break;
                            break;
                        }
                    case (10):
                        {
                            yield break;
                            break;
                        }
                    case (9):
                        {
                            yield break;
                            break;
                        }
                    case (8):
                        {
                            yield break;
                            break;
                        }
                    case (7):
                        {
                            yield break;
                            break;
                        }
                    case (6):
                        {
                            yield break;
                            break;
                        }
                    case (5):
                        {
                            yield break;
                            break;
                        }
                    case (4):
                        {
                            yield break;
                            break;
                        }
                    case (3):
                        {
                            yield break;
                            break;
                        }
                    case (2):
                        {
                            yield break;
                            break;
                        }
                    case (1):
                        {
                            yield break;
                            break;
                        }
                    default:
                        {
                            yield break;
                        }
                }
            }
        }

    }
}
