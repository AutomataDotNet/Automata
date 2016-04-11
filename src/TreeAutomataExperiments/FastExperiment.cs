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
using Microsoft.Fast;

namespace RunExperiments
{
    class FastExperiment
    {
        static string pathDet = @"..\..\benchmark\fastProgramsDet\";
        static string pathComp = @"..\..\benchmark\fastProgramsComp\";

        static int startFile = 1;
        static int numFiles;        


        public static void RunTest()
        {
            DirectoryInfo detDir = new DirectoryInfo(pathDet);
            FileInfo[] detFiles = detDir.GetFiles("*");
            numFiles = detFiles.Length;
            HashSet<int> detFileNames = new HashSet<int>();
            foreach (var f in detFiles)
                detFileNames.Add(int.Parse(f.Name));

            DirectoryInfo compDir = new DirectoryInfo(pathComp);
            FileInfo[] compFiles = compDir.GetFiles("*");
            HashSet<int> compFileNames = new HashSet<int>();
            foreach (var f in compFiles)
                compFileNames.Add(int.Parse(f.Name));

            if (startFile == 1)
                using (System.IO.StreamWriter outfile = new System.IO.StreamWriter(Program.path + Program.fastFile))
                {
                    outfile.WriteLine("ID, StateCount, RuleCount, CompleteRuleCount, MinStateCount, MinRuleCount, Algo1, Algo2, SFABased");
                }

            for (int fileNum = startFile; fileNum <= numFiles; fileNum++)
            {
                
                TreeTransducer detAut = null;
                if (detFileNames.Contains(fileNum))
                {
                    Console.WriteLine(fileNum);
                    detAut = GetFastProg(pathDet + fileNum);


                    detAut = detAut.Clean();


                    TreeTransducer compAut = null;
                    if (compFileNames.Contains(fileNum))
                        compAut = detAut.Complete();


                    Util.RunAllAlgorithms(detAut, compAut, fileNum.ToString(), Program.fastFile);
                }
            }
        }


        public static TreeTransducer GetFastProg(
            string path)
        {
            var pgm = Microsoft.Fast.Parser.ParseFromFile(path);
            var fti = FastTransducerInstance.MkFastTransducerInstance(pgm);
            foreach (var td in fti.treeDefinitions)
            {
                var tcd = td.Value;

                foreach (var pair in tcd.acceptors)
                {
                    return pair.Value;
                }
            }
            return null;
        }


        public static void SanityCheck()
        {
            DirectoryInfo detDir = new DirectoryInfo(pathDet);
            FileInfo[] detFiles = detDir.GetFiles("*");
            numFiles = detFiles.Length;
            HashSet<int> detFileNames = new HashSet<int>();
            foreach (var f in detFiles)
                detFileNames.Add(int.Parse(f.Name));

            DirectoryInfo compDir = new DirectoryInfo(pathComp);
            FileInfo[] compFiles = compDir.GetFiles("*");
            HashSet<int> compFileNames = new HashSet<int>();
            foreach (var f in compFiles)
                compFileNames.Add(int.Parse(f.Name));

            if (startFile == 1)
                using (System.IO.StreamWriter outfile = new System.IO.StreamWriter(Program.path + Program.fastFile))
                {
                    outfile.WriteLine("ID, StateCount, RuleCount, CompleteRuleCount, MinStateCount, MinRuleCount, Algo1, Algo2, SFABased");
                }

            for (int fileNum = startFile; fileNum <= numFiles; fileNum++)
            {
                Console.WriteLine(fileNum);
                TreeTransducer detAut = null;
                if (detFileNames.Contains(fileNum))
                    detAut = GetFastProg(pathDet + fileNum);
                else
                    break;
                TreeTransducer compAut = null;
                if (compFileNames.Contains(fileNum))
                    compAut = detAut.Complete();

                var min1 =detAut.Minimize();
                var min2 =detAut.MinimizeViaSFA();

                Console.WriteLine("StatesMin:");
                Console.WriteLine(detAut.StateCount+" "+min1.StateCount + " " + min2.StateCount);
                Console.WriteLine("TransMin:");
                Console.WriteLine(detAut.RuleCount + " " + min1.RuleCount + " " + min2.RuleCount);

                if(!min1.Complement().Intersect(min2).IsEmpty)
                    Console.WriteLine("Should be empty!");
                if (!min2.Complement().Intersect(min1).IsEmpty)
                    Console.WriteLine("Should be empty!");

                //Util.RunAllAlgorithms(detAut, compAut, fileNum.ToString(), Program.fastFile);
            }
        }
    }
}
