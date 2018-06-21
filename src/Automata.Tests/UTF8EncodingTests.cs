using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

using Microsoft.Automata;

using System.Text.RegularExpressions;

namespace Automata.Tests
{
    [TestClass]
    public class UTF8EncodingTests
    {
        [TestMethod]
        public void ConvertUTF16BDDtoUTF8Test()
        {
            ConvertUTF16BDDtoUTF8Test_Helper(@"[\d-[\u0800-\uFFFF]]");
            ConvertUTF16BDDtoUTF8Test_Helper(@"\d");
            ConvertUTF16BDDtoUTF8Test_Helper(@"\w");
            ConvertUTF16BDDtoUTF8Test_Helper(@"\D");
            ConvertUTF16BDDtoUTF8Test_Helper(@"\W");
            ConvertUTF16BDDtoUTF8Test_Helper(@"\s");
            ConvertUTF16BDDtoUTF8Test_Helper(@"\S");
            ConvertUTF16BDDtoUTF8Test_Helper(@"[\x80-\u07FF]");
            ConvertUTF16BDDtoUTF8Test_Helper(@"[\0-\uFFFF]");
        }

        public void ConvertUTF16BDDtoUTF8Test_Helper(string testClass)
        {
            var css = new CharSetSolver();

            var bdd = css.MkCharSetFromRegexCharClass(testClass);

            var ascii = bdd & css.MkCharSetFromRange('\0', '\x7F');

            var onebyte_encodings = bdd & ascii;

            var threebyte_encodings = Microsoft.Automata.Utilities.UTF8Encoding.Extract3ByteUTF8Encodings(bdd);

            var twobyte_encodings = Microsoft.Automata.Utilities.UTF8Encoding.Extract2ByteUTF8Encodings(bdd);

            HashSet<Sequence<byte>> utf8_encoding_actual = new HashSet<Sequence<byte>>();

            foreach (var c in css.GenerateAllCharacters(onebyte_encodings))
                utf8_encoding_actual.Add(new Sequence<byte>((byte)c));

            List<Move<BDD>> moves = new List<Move<BDD>>();
            int q = 2;
            moves.Add(Move<BDD>.Create(0, 1, onebyte_encodings));
            for (int i = 0; i < twobyte_encodings.Length; i += 1)
            {
                moves.Add(Move<BDD>.Create(0, q, twobyte_encodings[i].Item1));
                moves.Add(Move<BDD>.Create(q, 1, twobyte_encodings[i].Item2));
                q += 1;
                foreach (var first_byte in css.GenerateAllCharacters(twobyte_encodings[i].Item1))
                {
                    foreach (var second_byte in css.GenerateAllCharacters(twobyte_encodings[i].Item2))
                    {
                        utf8_encoding_actual.Add(new Sequence<byte>((byte)first_byte, (byte)second_byte));
                    }
                }
            }

            foreach (var triple in threebyte_encodings)
            {
                foreach (var pair in triple.Item2)
                {
                    moves.Add(Move<BDD>.Create(0, q, triple.Item1));
                    moves.Add(Move<BDD>.Create(q, q + 1, pair.Item1));
                    moves.Add(Move<BDD>.Create(q + 1, 1, pair.Item2));
                    q += 2;
                    foreach (var first_byte in css.GenerateAllCharacters(triple.Item1))
                    {
                        foreach (var second_byte in css.GenerateAllCharacters(pair.Item1))
                        {
                            foreach (var third_byte in css.GenerateAllCharacters(pair.Item2))
                            {
                                utf8_encoding_actual.Add(new Sequence<byte>((byte)first_byte, (byte)second_byte, (byte)third_byte));
                            }
                        }
                    }
                }
            }

            HashSet<Sequence<byte>> utf8_encoding_expected = new HashSet<Sequence<byte>>();
            for (int i = 0; i <= 0xFFFF; i++)
            {
                char c = (char)i;
                if (!char.IsSurrogate(c))
                    if (Regex.IsMatch(c.ToString(), "^" + testClass + "$"))
                    {
                        var bytes = new Sequence<byte>(System.Text.UnicodeEncoding.UTF8.GetBytes(new char[] { c }));
                        utf8_encoding_expected.Add(bytes);
                    }
            }

            //Automaton<BDD> aut = Automaton<BDD>.Create(css, 0, new int[] { 1 }, moves).Determinize().Minimize();
            //aut.ShowGraph();


            bool encoding_ok = utf8_encoding_expected.IsSubsetOf(utf8_encoding_actual) &&
                utf8_encoding_actual.IsSubsetOf(utf8_encoding_expected);

            Assert.IsTrue(encoding_ok, "incorrectly ecoded character class: " + testClass);
        }
    }
}
