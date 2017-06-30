using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Automata.Utilities;

namespace Microsoft.Automata.Rex
{
    /// <summary>
    /// Provides settings for Rex
    /// </summary>
    public sealed class RexSettings
    {
        /// <summary>
        /// Creates and instance of RexSettings with default values for all settings.
        /// </summary>
        RexSettings()
        {
            k = 1;
            encoding = BitWidth.BV16;
            //seed = -1;
            intersect = false;
            game = false;
        }

        /// <summary>
        /// Parse the given commandline arguments and create the corresponding settings object.
        /// </summary>
        public static bool ParseFromCommandlineArguments(string[] args, out RexSettings settings)
        {
            RexSettings settings1 = new RexSettings();
            if (CommandLineParser.ParseArgumentsWithUsage(args, settings1))
            {
                settings = settings1;
                return true;
            }
            settings = null;
            return false;
        }

        /// <summary>
        /// Creates and instance of RexSettings for the given regexes using default values for other settings.
        /// </summary>
        public RexSettings(string[] regexes)
        {
            if (regexes.Length < 1)
                throw new AutomataException("At least one regex must be specified");
 
            this.regexes = regexes;
            k = 1;
            encoding = BitWidth.BV16;
            //seed = -1;
            intersect = false;
        }

        /// <summary>
        /// Explicit input regexes, must be a nonempty collection of regexes if no regexfile is given. 
        /// </summary>
        [DefaultArgument(ArgumentType.Multiple, HelpText = "Explicit input regexes, must be a nonempty collection of regexes if no regexfile is given. ")]
        public string[] regexes;

        /// <summary>
        /// File where input regexes are stored one regex per line.
        /// This argument must be given if no regexes are given explicitly.
        /// </summary>
        [Argument(ArgumentType.AtMostOnce, ShortName = "r", HelpText = "File where input regexes are stored one regex per line. This argument must be given if no regexes are given explicitly.")]
        public string regexfile; 

        /// <summary>
        /// Zero or more regular expression options. Default is no options or equivalently RegexOptions.None.
        /// </summary>
        [Argument(ArgumentType.Multiple, ShortName = "o", HelpText = "Zero or more regular expression options")]
        public RegexOptions[] options;

        /// <summary>
        /// Number of members to generate. Default is 1.
        /// </summary>
        [Argument(ArgumentType.AtMostOnce, ShortName = "k", DefaultValue = 1, HelpText = "Number of members to generate")]
        public int k;

        /// <summary>
        /// File where the generated strings are stored, if omitted, the output it directed to the console.
        /// </summary>
        [Argument(ArgumentType.AtMostOnce, ShortName = "f", HelpText = "File where the generated strings are stored, if omitted, the output it directed to the console")]
        public string file;

        /// <summary>
        /// The character encoding to be used; determines the number of bits, ASCII:7, CP437:8, Unicode:16. 
        /// Default is Unicode.
        /// </summary>
        [Argument(ArgumentType.AtMostOnce, ShortName = "e", DefaultValue = BitWidth.BV16, HelpText = "The character encoding to be used; determines the number of bits, ASCII:7, CP437:8, Unicode:16")]
        public BitWidth encoding;

        ///// <summary>
        ///// Random seed for the generation, -1 (which is the default) means that a seed is chosen randomly.
        ///// </summary>
        //[Argument(ArgumentType.AtMostOnce, ShortName = "s", DefaultValue = -1, HelpText = "Random seed for the generation, -1 means that no seed is specified")]
        //public int seed;

        /// <summary>
        /// If true then the elements are generated uniformly at random (default is false) and the automaton may not have loops.
        /// </summary>
        [Argument(ArgumentType.AtMostOnce, ShortName = "u", DefaultValue = false, HelpText = "If true then the elements are generated uniformly at random and the input length must be bounded (default is false).")]
        public bool uniform;

        /// <summary>
        /// Name of output dot file of the finite automaton for the regex(es).
        /// </summary>
        [Argument(ArgumentType.AtMostOnce, ShortName = "d", HelpText = "Name of output dot file of the finite automaton for the regex(es)")]
        public string dot;

        /// <summary>
        /// If set, intersect the regexes; otherwise treat the regexes independently and generate k members for each. Default is false.
        /// </summary>
        [Argument(ArgumentType.AtMostOnce, ShortName = "i", DefaultValue = false, HelpText = "If set, intersect the regexes; otherwise treat the regexes independently and generate k members for each")]
        public bool intersect;

        /// <summary>
        /// If set, other options are ignored and there must be two given regexes r1 and r2. Generate 2 witnesses in each region of the Venn diagram for L(r1), L(r2).  Default is false.
        /// </summary>
        [Argument(ArgumentType.AtMostOnce, ShortName = "g", DefaultValue = false, HelpText = "If set, other options are ignored and there must be two given regexes r1 and r2. Generate 2 witnesses in each nonempty region of the Venn diagram for L(r1), L(r2).")]
        public bool game;
    }
}
