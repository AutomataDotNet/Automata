using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Fast.AST;

namespace Microsoft.Fast
{
    public static class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1 || args.Length > 2)
            {
                Console.WriteLine("Usage: tool.exe <file.fast> [<cs_master_file>]");
                return;
            }

            string cs_master_file = (args.Length > 1 ? args[1] : "");

            Run(File.ReadAllText(args[0]), null, cs_master_file, args[0]);
        }

        public static void Run(string source, TextWriter tw = null, string cs_master_file = "", string filename = "pgm.fast")
        {
            if (tw == null)
                tw = Console.Out;
            try
            {
                var pgm = FastPgmParser.Parse(source, filename);
                FastTransducerInstance fti = FastTransducerInstance.MkFastTransducerInstance(pgm, tw,LogLevel.Minimal);
                FastTransducerInstance.DisposeZ3P();
            }
            catch (FastParseException e)
            {
                tw.WriteLine(e.ToString());
                return;
            }
            catch (FastAssertException e)
            {
                tw.WriteLine("{3}({0},{1}): error : AssertionViolated, {2}", min1(e.line), min1(e.pos), e.Message, filename);                
                return;
            }
            catch (FastException e)
            {
                tw.WriteLine("{3}({0},{1}): error : FastError, {2}", 1, 1, e.Message, filename);
                return;
            }
            catch (Exception e)
            {
                tw.WriteLine("{3}({0},{1}): error : InternalError, {2}", 1, 1, filename);
                return;
            }
        }

        static int min1(int i)
        {
            if (i < 1)
                return 1;
            else return i;
        }
    }
}
