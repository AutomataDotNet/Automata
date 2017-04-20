using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Experiments
{
    class Program
    {
        static void Main(string[] args)
        {
            //HtmlEncode.Test.Run();
            //Trie.Test.Run();
            TestEvilRegex();
            Console.ReadLine();
        }

        public static void TestEvilRegex()
        {
            //@"^(([a-z0-9])+.)+[A-Z]([a-z0-9])+$"
            //Regex EvilRegex = new Regex(@"^(([^\0])+.)+[\0]([^\0])+$", RegexOptions.Compiled | (RegexOptions.Singleline));
            Regex EvilRegex = new Regex(@"(.+.)+\n.+");
            //Regex EvilRegex = new Regex("^(_?a?_?a?_?)+$");
            //string b = "some     arbitrary   input string";
            string b = "bb";
            //takes time exponential in the length of a
            int t = 0;
            for (int i = 0; i < 50; i++)
            {
                t = System.Environment.TickCount;
                //bool res = new Regex("^(b?a?b?)+$").IsMatch("a" + b + "c");
                //bool res = Regex.IsMatch("^(b?a?b?)+$", "a" + b + "c");
                bool res = EvilRegex.IsMatch(b);
                t = System.Environment.TickCount - t;
                Console.WriteLine("{0}, {1}", b.Length, t);
                b += "b";
            }
        }
    }
}
