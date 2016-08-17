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
        static internal int numTests = 11;

        static internal string timbukFile = "timbukResults.txt";
        static internal string timbukFileDM = "dtaminrestimbuk.txt";
        static internal string largeAlphabetFile = "largeAlphabetResults.txt";
        static internal string fastFile = "fastResults.txt";
        
        static internal string regexOutputFile = "nfaRegexMinResults.txt";
        static internal string verifNFAOutputFile = "nfaVerificationMinResults.txt";

        static internal string regexInputFile = "regexes.txt";

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
            //VerificationNFAExperiment.RunTest();

            RegexExperiment.RunTest();
        }

    }
}
