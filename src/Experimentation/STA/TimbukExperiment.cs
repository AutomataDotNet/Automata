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
    class TimbukExperiment
    {
     
        static string pathDet = @"..\..\benchmark\timbukdeterminized\";
        static string pathComp = @"..\..\benchmark\timbukcomplete\";

        static int startFile = 2655;
        static int maxFile = 2669;

        public static void RunTest()
        {
            DirectoryInfo detDir = new DirectoryInfo(pathDet);
            FileInfo[] detFiles = detDir.GetFiles("*");
            int numFiles = detFiles.Length;            
            HashSet<int> detFileNames = new HashSet<int>();
            foreach (var f in detFiles)
                detFileNames.Add(int.Parse(f.Name));

            DirectoryInfo compDir = new DirectoryInfo(pathComp);
            FileInfo[] compFiles = compDir.GetFiles("*");
            HashSet<int> compFileNames = new HashSet<int>();
            foreach (var f in compFiles)
                compFileNames.Add(int.Parse(f.Name));

            if (startFile == 1)
                using (System.IO.StreamWriter outfile = new System.IO.StreamWriter(Program.path + Program.timbukFile))
                {
                    outfile.WriteLine("ID, StateCount, RuleCount, CompleteRuleCount, MinStateCount, MinRuleCount, Algo1, Algo2, SFABased");
                }

            for (int fileNum = startFile; fileNum <= maxFile; fileNum++)
            {
                Console.WriteLine(fileNum);
                TreeTransducer detAut = null;
                if (detFileNames.Contains(fileNum))
                    detAut = VataParsing.ParseVataFile(pathDet + fileNum);
                TreeTransducer compAut = null;
                if (compFileNames.Contains(fileNum))
                    compAut = VataParsing.ParseVataFile(pathComp + fileNum);

                Util.RunAllAlgorithms(detAut, compAut, fileNum.ToString(), Program.timbukFile);
            }
        }

        public static void SanityCheck()
        {
            DirectoryInfo detDir = new DirectoryInfo(pathDet);
            FileInfo[] detFiles = detDir.GetFiles("*");
            int numFiles = detFiles.Length;
            HashSet<int> detFileNames = new HashSet<int>();
            foreach (var f in detFiles)
                detFileNames.Add(int.Parse(f.Name));

            DirectoryInfo compDir = new DirectoryInfo(pathComp);
            FileInfo[] compFiles = compDir.GetFiles("*");
            HashSet<int> compFileNames = new HashSet<int>();
            foreach (var f in compFiles)
                compFileNames.Add(int.Parse(f.Name));


            for (int fileNum = startFile; fileNum <= numFiles; fileNum++)
            {
                Console.WriteLine(fileNum);
                TreeTransducer detAut = null;
                if (detFileNames.Contains(fileNum))
                    detAut = VataParsing.ParseVataFile(pathDet + fileNum);

                var min1 = detAut.Minimize();
                var min2 = detAut.MinimizeViaSFA();

                Console.WriteLine("StatesMin:");
                Console.WriteLine(detAut.StateCount + " " + min1.StateCount + " " + min2.StateCount);
                Console.WriteLine("TransMin:");
                Console.WriteLine(detAut.RuleCount + " " + min1.RuleCount + " " + min2.RuleCount);

                //if (!min1.Complement().Intersect(min2).IsEmpty)
                //    Console.WriteLine("Should be empty!");
                //if (!min2.Complement().Intersect(min1).IsEmpty)
                //    Console.WriteLine("Should be empty!");
            }
        }
    }
}
