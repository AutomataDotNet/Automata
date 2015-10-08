using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Fast
{
    public class FastProvider
    {
        /// <summary>
        /// Check if the input file is a correct Fast program: returns true if correct, returns false and 
        /// prints an exception if it is not.
        /// </summary>
        /// <param name="inputPath">the Fast file to be checked</param>
        public static bool CheckSyntax(String inputPath)
        {
            try
            {
                var pgm = Parser.ParseFromFile(inputPath);
            }
            catch (FastParseException fpe)
            {
                Console.WriteLine(fpe.ToString());
                return false;
            }
            return true;
        }


        /// <summary>
        /// Generates C# code from a Fast program
        /// </summary>
        /// <param name="inputPath">the Fast file from which it generates the CSharp code</param>
        /// <param name="outputPath">the C# file into which the CSharp code is generated</param>
        /// <param name="ns">the package the generated file will belong to</param>
        /// <param name="flag">true to generate the transducer's optimized version</param>
        public static bool GenerateCSharp(String inputPath, String outputPath, String ns, bool flag){

            var pgm = Parser.ParseFromFile(inputPath);
            FastTransducerInstance fti = FastTransducerInstance.MkFastTransducerInstance(pgm);
            StringBuilder sb = new StringBuilder();
            fti.ToFast(sb);
            pgm = Parser.ParseFromString(sb.ToString());
            return CsharpGenerator.GenerateCode(pgm, outputPath, ns);
        }


    }
}
