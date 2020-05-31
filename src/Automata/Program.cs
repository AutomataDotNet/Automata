using System;
using System.Text.RegularExpressions;
using Microsoft.Automata.Utilities;
using System.Collections.Generic;

namespace Microsoft.Automata
{
    class Program
    {
        static void Main(string[] args)
        {
            if (RegexExtensionMethods.context == null)
                RegexExtensionMethods.context = new CharSetSolver();

            Re2smtSettings settings;
            if (!ParseFromCommandlineArguments(args, out settings))
                return;

            BDD filter = RegexExtensionMethods.context.True;
            if (settings.bits == 7)
                filter = RegexExtensionMethods.context.CharSetProvider.MkCharSetFromRange('\0', '\x7F');
            else if (settings.bits == 8)
                filter = RegexExtensionMethods.context.CharSetProvider.MkCharSetFromRange('\0', '\xFF');
            else if (settings.bits != 16)
            {
                Console.WriteLine("/bits must be assigned either 7, 8 or 16");
                return;
            }

            if ((settings.regexes.Length == 0 && settings.input == null) || (settings.regexes.Length > 0 && settings.input != null))
            {
                Console.WriteLine("either an input file containing regexes or some regex must be specified, but not both");
                return;
            }

            List<string> regexes = new List<string>(settings.regexes);
            if (settings.input != null)
                regexes.AddRange(System.IO.File.ReadAllLines(settings.input));

            if ((settings.op == OP.subset || settings.op == OP.nequiv) && regexes.Count != 2)
            {
                Console.WriteLine("/op:subset or /op:equiv take two regexes as arguments");
                return;
            }

            string[] smt_regexes = new string[regexes.Count];

            for (int i = 0; i < regexes.Count; i++)
            {
                RegexTree rt = RegexParser.Parse(regexes[i], RegexOptions.Singleline);

                var sr = RegexExtensionMethods.context.RegexConverter.ConvertToSymbolicRegex(rt._root, !settings.anchors, false);
                smt_regexes[i] = sr.ToSMTlibFormat(filter);
            }

            if (settings.op == OP.none)
            {
                if (settings.output != null)
                    System.IO.File.WriteAllLines(settings.output, smt_regexes);
                else
                {
                    for (int i = 0; i < smt_regexes.Length; i++)
                        Console.WriteLine(smt_regexes[i]);
                }
            }
            else
            {
                string assertion = "";
                if (settings.op == OP.member)
                {
                    //create intersection of all regexes
                    string inter = smt_regexes[0];
                    for (int i = 1; i < smt_regexes.Length; i++)
                        inter = string.Format("(re.inter {0} {1})", inter, smt_regexes[i]);
                    assertion = string.Format("(declare-const x String)\n(assert(str.in_re x {0}))\n(check-sat)\n(get-model)", inter);
                }
                else if (settings.op == OP.nequiv)
                    assertion = string.Format("(assert (not (= {0} {1})))\n(check-sat)\n(get-model)", smt_regexes[0], smt_regexes[1]);
                else
                    assertion = string.Format("(assert (subset {0} {1}))\n(check-sat)\n(get-model)", smt_regexes[0], smt_regexes[1]);

                if (settings.output != null)
                    System.IO.File.WriteAllText(settings.output, assertion);
                else
                    Console.WriteLine(assertion);
            }
        }

        /// <summary>
        /// Parse the given commandline arguments and create the corresponding settings object.
        /// </summary>
        public static bool ParseFromCommandlineArguments(string[] args, out Re2smtSettings settings)
        {
            Re2smtSettings settings1 = new Re2smtSettings();
            if (CommandLineParser.ParseArgumentsWithUsage(args, settings1))
            {
                settings = settings1;
                return true;
            }
            settings = null;
            return false;
        }

        public enum OP
        {
            member,
            subset,
            nequiv,
            none
        }

        /// <summary>
        /// Provides settings for Rex
        /// </summary>
        public sealed class Re2smtSettings
        {
            /// <summary>
            /// Creates and instance of RexSettings with default values for all settings.
            /// </summary>
            internal Re2smtSettings() { }

            /// <summary>
            /// Parse the given commandline arguments and create the corresponding settings object.
            /// </summary>
            public static bool ParseFromCommandlineArguments(string[] args, out Re2smtSettings settings)
            {
                Re2smtSettings settings1 = new Re2smtSettings();
                if (CommandLineParser.ParseArgumentsWithUsage(args, settings1))
                {
                    settings = settings1;
                    return true;
                }
                settings = null;
                return false;
            }

            /// <summary>
            /// Explicit input regexes 
            /// </summary>
            [DefaultArgument(ArgumentType.Multiple, HelpText = "Explicit input regexes")]
            public string[] regexes = null;

            /// <summary>
            /// File where input regexes are stored one regex per line.
            /// This argument must be given if no regexes are given explicitly.
            /// </summary>
            [Argument(ArgumentType.AtMostOnce, ShortName = "i", HelpText = "File where input regexes are stored one regex per line. This argument must be given if no regexes are given explicitly.")]
            public string input = null;

            /// <summary>
            /// Number of bits in a character. Must be one of 7, 8 or 16.
            /// </summary>
            [Argument(ArgumentType.AtMostOnce, ShortName = "b", DefaultValue = 7, HelpText = "Number of bits in a character. Must be one of 7, 8 or 16")]
            public int bits = 7;

            /// <summary>
            /// File where the generated smtlib output is written, if omitted, the output it directed to the console.
            /// </summary>
            [Argument(ArgumentType.AtMostOnce, ShortName = "o", HelpText = "File where the generated smtlib output is written, if omitted, the output it directed to the console.")]
            public string output = null;

            /// <summary>
            /// If true then anchors are explicit
            /// </summary>
            [Argument(ArgumentType.AtMostOnce, ShortName = "a", DefaultValue = false, HelpText = "If true then anchors are explicit")]
            public bool anchors = false;

            /// <summary>
            /// Operation to be performed.
            /// </summary>
            [Argument(ArgumentType.AtMostOnce, DefaultValue = OP.none, HelpText = "Operation to be performed. For 'member' all regexes are intersected.")]
            public OP op = OP.none;
        }
    }
}

