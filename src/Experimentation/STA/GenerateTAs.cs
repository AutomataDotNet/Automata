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
    class GenerateTimbukTAs
    {
        static string path = @"C:\MinBench\";
        static string pathDet = @"..\..\benchmark\timbukdeterminized\";
        static string pathComp = @"..\..\benchmark\timbukcomplete\";
        static string[] suffs = new string[] { @"moderate_artmc_timbuk", @"small_timbuk", @"yun_yun_mata" };

        public static void GenerateTests()
        {
            HashSet<Tuple<int, int>> alreadyFound = new HashSet<Tuple<int, int>>();

            DirectoryInfo detDir = new DirectoryInfo(pathDet);
            int fileName = detDir.GetFiles().Length + 1;

            foreach (var suff in suffs)
            {
                DirectoryInfo d = new DirectoryInfo(path + suff + @"\");
                FileInfo[] Files = d.GetFiles("*");

                foreach (FileInfo file in Files)
                {
                    Console.WriteLine(suff + @"\" + file.Name);
                    var aut = VataParsing.ParseVataFile(path + suff + @"\" + file.Name);
                    if (aut != null)
                    {
                        Console.WriteLine(aut.StateCount + " " + aut.RuleCount);
                        if (aut.StateCount < 301 || aut.RuleCount < 3407)
                        {

                            var stTr = new Tuple<int, int>(aut.StateCount, aut.RuleCount);
                            if (!alreadyFound.Contains(stTr))
                            {
                                alreadyFound.Add(stTr);

                                var tokenSource = new CancellationTokenSource();
                                CancellationToken token = tokenSource.Token;

                                TreeTransducer detAut = null;

                                var task = Task.Factory.StartNew(() =>
                                {
                                    aut.IsDeterminstic();
                                    detAut = aut.DeterminizeWithoutCompletion().RemoveUselessStates();
                                }, token);

                                if (!task.Wait(Program.timeOut, token))
                                    Console.WriteLine("The determinization Task timed out!");
                                else
                                {


                                    var outstring = VataParsing.GetVataFormatString(detAut);
                                    VataParsing.ParseVataFormat(outstring);
                                    using (System.IO.StreamWriter outfile = new System.IO.StreamWriter(pathDet + fileName))
                                    {
                                        outfile.Write(outstring);
                                        fileName++;
                                    }

                                    if (detAut.InputAlphabet.MaxRank<6 && detAut.RuleCount < 4000)
                                    {
                                        TreeTransducer completeAut = null;
                                        CancellationToken token2 = tokenSource.Token;
                                        var task2 = Task.Factory.StartNew(() =>
                                        {
                                            completeAut = aut.Determinize();
                                        }, token2);
                                        if (!task2.Wait(Program.timeOut, token2))
                                            Console.WriteLine("The completion Task timed out!");
                                        else
                                        {
                                            outstring = VataParsing.GetVataFormatString(completeAut);
                                            VataParsing.ParseVataFormat(outstring);
                                            using (System.IO.StreamWriter outfile = new System.IO.StreamWriter(pathComp + (fileName - 1)))
                                            {                                               
                                                outfile.Write(outstring);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine(detAut.RuleCount + " rules, skip completion");
                                    }
                                }
                            }
                        }
                    }

                }
            }
        }
    }
}
