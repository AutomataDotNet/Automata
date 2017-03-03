using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

using Microsoft.Automata.Z3;
using Microsoft.Automata.Z3.Internal;
using Microsoft.Automata;
using Microsoft.Z3;
using System.Diagnostics;

namespace RunExperiments
{
    class Program
    {
        

        static internal string path = @"..\..\benchmark\";
        static internal int timeOut = 299999; // 5 minutes
        static internal int numTests = 2;

        static internal string timbukFile = "timbukResults.txt";
        static internal string timbukFileDM = "dtaminrestimbuk.txt";
        static internal string largeAlphabetFile = "largeAlphabetResults.txt";
        static internal string fastFile = "fastResults.txt";
        
        static internal string regexOutputFile = "nfaRegexMinResults.txt";
        static internal string verifNFAOutputFile = "nfaVerificationMinResults.txt";

        static internal string regexInputFile = "regexlib-clean.txt";

        static internal string timbukPrefix = "timbuk";
        static internal string largeAlphabetPrefix = "large";
        static internal string fastPrefix = "fast";

        static void Main(string[] args)
        {
            //Test generation for Fast
            // This takes hours
            //RecognizerGenerator.GenerateTAUsingFast(10, 12, 50);
            // These two takes minutes
            //RecognizerGenerator.GenerateTAUsingFast(6, 15, 1000);
            //RecognizerGenerator.GenerateTAUsingFast(5, 16, 1000);

            //DTAMINParsing.RunLargeGeneration();
            //DTAMINParsing.RunGeneration();

            // Rune experiments                    
            //LargeAlphabetExperiment.RunTest();
            //FastExperiment.RunTest();
            //TimbukExperiment.RunTest();

            ////Gather results in text files            
            //Util.GatherResults(largeAlphabetFile, largeAlphabetPrefix);
            //Util.GatherResults(fastFile, fastPrefix);
            //Util.GatherResultsTimbuk(timbukFile, timbukFileDM, timbukPrefix);


            //RegexExperiment.RunTest();

            //VerificationNFAExperiment.RunTest();

            /*
            Console.WriteLine("regex");
            RegexExperiment.RunTest();
            Console.WriteLine("nfa-verification");
            VerificationNFAExperiment.RunFinAlphTest();
             */

            //trie.PredicateTrieExperiments.RunRandom();         

            // Experimentation.HtmlEncode.CompareHtmlEncode.CompareHtmlEncodePerformance(true); 
            Console.WriteLine("CheckDeterminize");
            CheckDeterminize();
            //GenerateUnicodeCategories();
            //GenerateIgnoreCaseRelation();
        }

        static void CheckDeterminize()
        {
            var solver = new CharSetSolver();
            string regex = @"abd|abc";
            var fa = solver.Convert(regex);
            int t = System.Environment.TickCount;
            var fadet = fa.Determinize().Minimize();
            t = System.Environment.TickCount - t;
            Console.WriteLine("time = {0}ms, states = {1}", t, fadet.StateCount);
            var cs = fadet.Compile();
            //sanity check the generated cs 
            foreach (string passw in new string[] { "foordPa$$w0rdbar", "foordP$$w0rdbar", "marP@ssw0rd1gus" })
            {
                Console.WriteLine("{0}={1}", System.Text.RegularExpressions.Regex.IsMatch(passw, regex), cs.Automaton.IsFinalState(cs.Automaton.Transition(0,passw.ToCharArray())));
            }
            //Console.ReadLine();
            //fa.ShowGraph();
            //Console.WriteLine("Showgraph");
            fadet.ShowGraph();
        }

        static void GenerateUnicodeCategories()
        {
            Microsoft.Automata.Internal.Utilities.UnicodeCategoryRangesGenerator.Generate("Microsoft.Automata.Internal.Generated", "UnicodeCategoryRanges", "");
            Console.WriteLine("done");
            Console.ReadLine();
        }

        //IgnoreCaseRelation

        static void GenerateIgnoreCaseRelation()
        {
            Microsoft.Automata.Internal.Utilities.IgnoreCaseRelationGenerator.Generate("Microsoft.Automata.Internal.Generated", "IgnoreCaseRelation", "");
            Console.WriteLine("done");
            Console.ReadLine();
        }

    }
}
