//-------------------------------------
// Copyright (c) Microsoft Corporation
//-------------------------------------
//////////////////////////////////////////////////////////////////////////////
//    Command Line Argument Parser
//    ----------------------------
//
//    Usage
//    -----
//
//    Parsing command line arguments to a console application is a common problem. 
//    This library handles the common task of reading arguments from a command line 
//    and filling in the values in a type.
//
//    To use this library, define a class whose fields represent the data that your 
//    application wants to receive from arguments on the command line. Then call 
//    CommandLine.ParseArguments() to fill the object with the data 
//    from the command line. Each field in the class defines a command line argument. 
//    The type of the field is used to validate the data read from the command line. 
//    The name of the field defines the name of the command line option.
//
//    The parser can handle fields of the following types:
//
//    - string
//    - int
//    - uint
//    - bool
//    - enum
//    - array of the above type
//
//    For example, suppose you want to read in the argument list for wc (word count). 
//    wc takes three optional boolean arguments: -l, -w, and -c and a list of files.
//
//    You could parse these arguments using the following code:
//
//    class WCArguments
//    {
//        public bool lines;
//        public bool words;
//        public bool chars;
//        public string[] files;
//    }
//
//    class WC
//    {
//        static void Main(string[] args)
//        {
//            if (CommandLine.ParseArgumentsWithUsage(args, parsedArgs))
//            {
//            //     insert application code here
//            }
//        }
//    }
//
//    So you could call this aplication with the following command line to count 
//    lines in the foo and bar files:
//
//        wc.exe /lines /files:foo /files:bar
//
//    The program will display the following usage message when bad command line 
//    arguments are used:
//
//        wc.exe -x
//
//    Unrecognized command line argument '-x'
//        /lines[+|-]                         short form /l
//        /words[+|-]                         short form /w
//        /chars[+|-]                         short form /c
//        /files:<string>                     short form /f
//        @<file>                             Read response file for more options
//
//    That was pretty easy. However, you realy want to omit the "/files:" for the 
//    list of files. The details of field parsing can be controled using custom 
//    attributes. The attributes which control parsing behaviour are:
//
//    ArgumentAttribute 
//        - controls short name, long name, required, allow duplicates, default value
//        and help text
//    DefaultArgumentAttribute 
//        - allows omition of the "/name".
//        - This attribute is allowed on only one field in the argument class.
//
//    So for the wc.exe program we want this:
//
//    using System;
//    using Utilities;
//
//    class WCArguments
//    {
//        [Argument(ArgumentType.AtMostOnce, HelpText="Count number of lines in the input text.")]
//        public bool lines;
//        [Argument(ArgumentType.AtMostOnce, HelpText="Count number of words in the input text.")]
//        public bool words;
//        [Argument(ArgumentType.AtMostOnce, HelpText="Count number of chars in the input text.")]
//        public bool chars;
//        [DefaultArgument(ArgumentType.MultipleUnique, HelpText="Input files to count.")]
//        public string[] files;
//    }
//
//    class WC
//    {
//        static void Main(string[] args)
//        {
//            WCArguments parsedArgs = new WCArguments();
//            if (CommandLine.ParseArgumentsWithUsage(args, parsedArgs))
//            {
//            //     insert application code here
//            }
//        }
//    }
//
//
//
//    So now we have the command line we want:
//
//        wc.exe /lines foo bar
//
//    This will set lines to true and will set files to an array containing the 
//    strings "foo" and "bar".
//
//    The new usage message becomes:
//
//        wc.exe -x
//
//    Unrecognized command line argument '-x'
//    /lines[+|-]  Count number of lines in the input text. (short form /l)
//    /words[+|-]  Count number of words in the input text. (short form /w)
//    /chars[+|-]  Count number of chars in the input text. (short form /c)
//    @<file>      Read response file for more options
//    <files>      Input files to count. (short form /f)
//
//    If you want more control over how error messages are reported, how /help is 
//    dealt with, etc you can instantiate the CommandLine.Parser class.
//
//////////////////////////////////////////////////////////////////////////////
namespace Microsoft.Automata.Utilities
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Collections;
    using System.IO;
    using System.Text;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Used to control parsing of command line arguments.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue"), Flags]
    internal enum ArgumentType
    {
        /// <summary>
        /// Indicates that this field is required. An error will be displayed
        /// if it is not present when parsing arguments.
        /// </summary>
        Required = 0x01,

        /// <summary>
        /// Only valid in conjunction with Multiple.
        /// Duplicate values will result in an error.
        /// </summary>
        Unique = 0x02,

        /// <summary>
        /// Inidicates that the argument may be specified more than once.
        /// Only valid if the argument is a collection
        /// </summary>
        Multiple = 0x04,

        /// <summary>
        /// The default type for non-collection arguments.
        /// The argument is not required, but an error will be reported if it is specified more than once.
        /// </summary>
        AtMostOnce = 0x00,

        /// <summary>
        /// For non-collection arguments, when the argument is specified more than
        /// once no error is reported and the value of the argument is the last
        /// value which occurs in the argument list.
        /// </summary>
        LastOccurenceWins = Multiple,

        /// <summary>
        /// The default type for collection arguments.
        /// The argument is permitted to occur multiple times, but duplicate 
        /// values will cause an error to be reported.
        /// </summary>
        MultipleUnique = Multiple | Unique,

        /// <summary>
        /// The default type for non-collection arguments.
        /// The argument is not required, but an error will be reported if it is specified more than once.
        /// </summary>
        AtLeastOnce = Multiple | Required,
    }

    /// <summary>
    /// Allows control of command line parsing.
    /// Attach this attribute to instance fields of types used
    /// as the destination of command line argument parsing.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes"), AttributeUsage(AttributeTargets.Field)]
    internal class ArgumentAttribute : Attribute
    {
        /// <summary>
        /// Allows control of command line parsing.
        /// </summary>
        /// <param name="type"> Specifies the error checking to be done on the argument. </param>
        public ArgumentAttribute(ArgumentType type)
        {
            this.type = type;
        }

        /// <summary>
        /// The error checking to be done on the argument.
        /// </summary>
        public ArgumentType Type
        {
            get
            {
                return this.type;
            }
        }
        /// <summary>
        /// Returns true if the argument did not have an explicit short name specified.
        /// </summary>
        public bool DefaultShortName
        {
            get
            {
                return null == this.shortName;
            }
        }

        /// <summary>
        /// The short name of the argument.
        /// Set to null means use the default short name if it does not
        /// conflict with any other parameter name.
        /// Set to String.Empty for no short name.
        /// This property should not be set for DefaultArgumentAttributes.
        /// </summary>
        public string ShortName
        {
            get
            {
                return this.shortName;
            }
            set
            {
                Debug.Assert(value == null || !(this is DefaultArgumentAttribute));
                this.shortName = value;
            }
        }

        /// <summary>
        /// Returns true if the argument did not have an explicit long name specified.
        /// </summary>
        public bool DefaultLongName
        {
            get
            {
                return null == this.longName;
            }
        }

        /// <summary>
        /// The long name of the argument.
        /// Set to null means use the default long name.
        /// The long name for every argument must be unique.
        /// It is an error to specify a long name of String.Empty.
        /// </summary>
        public string LongName
        {
            get
            {
                Debug.Assert(!this.DefaultLongName);
                return this.longName;
            }
            set
            {
                Debug.Assert(!String.IsNullOrEmpty(value));
                this.longName = value;
            }
        }

        /// <summary>
        /// The default value of the argument.
        /// </summary>
        public object DefaultValue
        {
            get
            {
                return this.defaultValue;
            }
            set
            {
                this.defaultValue = value;
            }
        }

        /// <summary>
        /// Returns true if the argument has a default value.
        /// </summary>
        public bool HasDefaultValue
        {
            get
            {
                return null != this.defaultValue;
            }
        }

        /// <summary>
        /// Returns true if the argument has help text specified.
        /// </summary>
        public bool HasHelpText
        {
            get
            {
                return null != this.helpText;
            }
        }

        /// <summary>
        /// The help text for the argument.
        /// </summary>
        public string HelpText
        {
            get
            {
                return this.helpText;
            }
            set
            {
                this.helpText = value;
            }
        }

        private string shortName;
        private string longName;
        private string helpText;
        private object defaultValue;
        private ArgumentType type;
    }

    /// <summary>
    /// Indicates that this argument is the default argument.
    /// '/' or '-' prefix only the argument value is specified.
    /// The ShortName property should not be set for DefaultArgumentAttribute
    /// instances. The LongName property is used for usage text only and
    /// does not affect the usage of the argument.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes"), AttributeUsage(AttributeTargets.Field)]
    internal class DefaultArgumentAttribute : ArgumentAttribute
    {
        /// <summary>
        /// Indicates that this argument is the default argument.
        /// </summary>
        /// <param name="type"> Specifies the error checking to be done on the argument. </param>
        public DefaultArgumentAttribute(ArgumentType type)
            : base(type)
        {
        }
    }

    /// <summary>
    /// A delegate used in error reporting.
    /// </summary>
    internal delegate void ErrorReporter(string message);

    /// <summary>
    /// Parser for command line arguments.
    ///
    /// The parser specification is infered from the instance fields of the object
    /// specified as the destination of the parse.
    /// Valid argument types are: int, uint, string, bool, enums
    /// Also argument types of Array of the above types are also valid.
    /// 
    /// Error checking options can be controlled by adding a ArgumentAttribute
    /// to the instance fields of the destination object.
    ///
    /// At most one field may be marked with the DefaultArgumentAttribute
    /// indicating that arguments without a '-' or '/' prefix will be parsed as that argument.
    ///
    /// If not specified then the parser will infer default options for parsing each
    /// instance field. The default long name of the argument is the field name. The
    /// default short name is the first character of the long name. Long names and explicitly
    /// specified short names must be unique. Default short names will be used provided that
    /// the default short name does not conflict with a long name or an explicitly
    /// specified short name.
    ///
    /// Arguments which are array types are collection arguments. Collection
    /// arguments can be specified multiple times.
    /// </summary>
    internal sealed class CommandLineParser
    {
        /// <summary>
        /// The System Defined new line string.
        /// </summary>
        public const string NewLine = "\r\n";

        /// <summary>
        /// Parses Command Line Arguments. Displays usage message to Console.Out
        /// if /?, /help or invalid arguments are encounterd.
        /// Errors are output on Console.Error.
        /// Use ArgumentAttributes to control parsing behaviour.
        /// </summary>
        /// <param name="arguments"> The actual arguments. </param>
        /// <param name="destination"> The resulting parsed arguments. </param>
        /// <returns> true if no errors were detected. </returns>
        public static bool ParseArgumentsWithUsage(string[] arguments, object destination)
        {
            if (destination == null)
                throw new ArgumentNullException("destination");

            if (CommandLineParser.ParseHelp(arguments))
            {
                // Display usage message
                System.Console.Write(CommandLineParser.ArgumentsUsage(destination.GetType()));
                return false;
            }
            if (!CommandLineParser.ParseArguments(arguments, destination))
            {
                // error encountered in arguments.
                return false;
            }

            return true;
        }

        /// <summary>
        /// Parses Command Line Arguments. Displays usage message to Console.Out
        /// if /?, /help or invalid arguments are encounterd.
        /// Errors are output on Console.Error.
        /// Use ArgumentAttributes to control parsing behaviour.
        /// </summary>
        /// <param name="arguments"> The actual arguments. </param>
        /// <param name="destination"> The resulting parsed arguments. </param>
        /// <returns> true if no errors were detected. </returns>
        public static bool ParseArgumentsWithUsage(string[] arguments, Type destination)
        {
            if (CommandLineParser.ParseHelp(arguments) || !CommandLineParser.ParseArguments(arguments, destination))
            {
                // error encountered in arguments. Display usage message
                //System.Console.Write(Parser.ArgumentsUsage(destination));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Parses Command Line Arguments. 
        /// Errors are output on Console.Error.
        /// Use ArgumentAttributes to control parsing behaviour.
        /// </summary>
        /// <param name="arguments"> The actual arguments. </param>
        /// <param name="destination"> The resulting parsed arguments. </param>
        /// <returns> true if no errors were detected. </returns>
        public static bool ParseArguments(string[] arguments, object destination)
        {
            return CommandLineParser.ParseArguments(arguments, destination, new ErrorReporter(Console.Error.WriteLine));
        }

        /// <summary>
        /// Parses Command Line Arguments. 
        /// Errors are output on Console.Error.
        /// Use ArgumentAttributes to control parsing behaviour.
        /// </summary>
        /// <param name="arguments"> The actual arguments. </param>
        /// <param name="destination"> The resulting parsed arguments. </param>
        /// <returns> true if no errors were detected. </returns>
        public static bool ParseArguments(string[] arguments, Type destination)
        {
            return CommandLineParser.ParseArguments(arguments, destination, new ErrorReporter(Console.Error.WriteLine));
        }

        /// <summary>
        /// Parses Command Line Arguments. 
        /// Use ArgumentAttributes to control parsing behaviour.
        /// </summary>
        /// <param name="arguments"> The actual arguments. </param>
        /// <param name="destination"> The resulting parsed arguments. </param>
        /// <param name="reporter"> The destination for parse errors. </param>
        /// <returns> true if no errors were detected. </returns>
        public static bool ParseArguments(string[] arguments, object destination, ErrorReporter reporter)
        {
            if (destination == null)
                throw new ArgumentNullException("destination");

            CommandLineParser parser = new CommandLineParser(destination.GetType(), reporter);
            return parser.Parse(arguments, destination);
        }

        /// <summary>
        /// Parses Command Line Arguments. 
        /// Use ArgumentAttributes to control parsing behaviour.
        /// </summary>
        /// <param name="arguments"> The actual arguments. </param>
        /// <param name="destination"> The resulting parsed arguments. </param>
        /// <param name="reporter"> The destination for parse errors. </param>
        /// <returns> true if no errors were detected. </returns>
        public static bool ParseArguments(string[] arguments, Type destination, ErrorReporter reporter)
        {
            CommandLineParser parser = new CommandLineParser(destination, reporter);
            return parser.Parse(arguments, null);
        }

        private static void NullErrorReporter(string message)
        {
        }

        private class HelpArgument
        {
            [ArgumentAttribute(ArgumentType.AtMostOnce, ShortName = "?")]
            public bool help = false;
        }

        /// <summary>
        /// Checks if a set of arguments asks for help.
        /// </summary>
        /// <param name="args"> Args to check for help. </param>
        /// <returns> Returns true if args contains /? or /help. </returns>
        public static bool ParseHelp(string[] args)
        {
            CommandLineParser helpParser = new CommandLineParser(typeof(HelpArgument), new ErrorReporter(NullErrorReporter));
            HelpArgument helpArgument = new HelpArgument();
            helpParser.Parse(args, helpArgument);
            return helpArgument.help;
        }

        /// <summary>
        /// Returns a Usage string for command line argument parsing.
        /// Use ArgumentAttributes to control parsing behaviour.
        /// Formats the output to the width of the current console window.
        /// </summary>
        /// <param name="argumentType"> The type of the arguments to display usage for. </param>
        /// <returns> Printable string containing a user friendly description of command line arguments. </returns>
        public static string ArgumentsUsage(Type argumentType)
        {
            //int screenWidth = Parser.GetConsoleWindowWidth();
            int screenWidth = Console.WindowWidth;
            if (screenWidth == 0)
                screenWidth = 80;
            return ArgumentsUsage(argumentType, screenWidth);
        }

        /// <summary>
        /// Returns a Usage string for command line argument parsing.
        /// Use ArgumentAttributes to control parsing behaviour.
        /// </summary>
        /// <param name="argumentType"> The type of the arguments to display usage for. </param>
        /// <param name="columns"> The number of columns to format the output to. </param>
        /// <returns> Printable string containing a user friendly description of command line arguments. </returns>
        public static string ArgumentsUsage(Type argumentType, int columns)
        {
            return (new CommandLineParser(argumentType, null)).GetUsageString(columns);
        }

        /*        private const int STD_OUTPUT_HANDLE = -11;

                private struct COORD
                {
                    internal Int16 x;
                    internal Int16 y;
                }

                private struct SMALL_RECT
                {
                    internal Int16 Left;
                    internal Int16 Top;
                    internal Int16 Right;
                    internal Int16 Bottom;
                }

                private struct CONSOLE_SCREEN_BUFFER_INFO
                {
                    internal COORD dwSize;
                    internal COORD dwCursorPosition;
                    internal Int16 wAttributes;
                    internal SMALL_RECT srWindow;
                    internal COORD dwMaximumWindowSize;
                }

                [DllImport("kernel32.dll", EntryPoint = "GetStdHandle", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
                private static extern int GetStdHandle(int nStdHandle);

                [DllImport("kernel32.dll", EntryPoint = "GetConsoleScreenBufferInfo", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
                private static extern int GetConsoleScreenBufferInfo(int hConsoleOutput, ref CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo);

                /// <summary>
                /// Returns the number of columns in the current console window
                /// </summary>
                /// <returns>Returns the number of columns in the current console window</returns>
                public static int GetConsoleWindowWidth()
                {
                    int screenWidth;
                    CONSOLE_SCREEN_BUFFER_INFO csbi = new CONSOLE_SCREEN_BUFFER_INFO();

                    int rc;
                    rc = GetConsoleScreenBufferInfo(GetStdHandle(STD_OUTPUT_HANDLE), ref csbi);
                    screenWidth = csbi.dwSize.x;
                    return screenWidth;
                }
        */
        /// <summary>
        /// Searches a StringBuilder for a character
        /// </summary>
        /// <param name="text"> The text to search. </param>
        /// <param name="value"> The character value to search for. </param>
        /// <param name="startIndex"> The index to stat searching at. </param>
        /// <returns> The index of the first occurence of value or -1 if it is not found. </returns>
        public static int IndexOf(StringBuilder text, char value, int startIndex)
        {
            if (text == null)
                throw new ArgumentNullException("text");

            for (int index = startIndex; index < text.Length; index++)
            {
                if (text[index] == value)
                    return index;
            }

            return -1;
        }

        /// <summary>
        /// Searches a StringBuilder for a character in reverse
        /// </summary>
        /// <param name="text"> The text to search. </param>
        /// <param name="value"> The character to search for. </param>
        /// <param name="startIndex"> The index to start the search at. </param>
        /// <returns>The index of the last occurence of value in text or -1 if it is not found. </returns>
        public static int LastIndexOf(StringBuilder text, char value, int startIndex)
        {
            if (text == null)
                throw new ArgumentNullException("text");

            for (int index = Math.Min(startIndex, text.Length - 1); index >= 0; index--)
            {
                if (text[index] == value)
                    return index;
            }

            return -1;
        }

        private const int spaceBeforeParam = 2;

        /// <summary>
        /// Creates a new command line argument parser.
        /// </summary>
        /// <param name="argumentSpecification"> The type of object to  parse. </param>
        /// <param name="reporter"> The destination for parse errors. </param>
        public CommandLineParser(Type argumentSpecification, ErrorReporter reporter)
        {
            if (argumentSpecification == null)
                throw new ArgumentNullException("argumentSpecification");

            this.reporter = reporter;
            this.arguments = new ArrayList();
            this.argumentMap = new Hashtable();

            foreach (FieldInfo field in argumentSpecification.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!field.IsInitOnly && !field.IsLiteral)
                {
                    ArgumentAttribute attribute = GetAttribute(field);
                    if (attribute is DefaultArgumentAttribute)
                    {
                        Debug.Assert(this.defaultArgument == null);
                        this.defaultArgument = new Argument(attribute, field, reporter);
                    }
                    else if (attribute != null)
                    {
                        this.arguments.Add(new Argument(attribute, field, reporter));
                    }
                }
            }

            // add explicit names to map
            foreach (Argument argument in this.arguments)
            {
                //Debug.Assert(!argumentMap.ContainsKey(argument.LongName));
                this.argumentMap[argument.LongName] = argument;
                if (argument.ExplicitShortName)
                {
                    if (argument.ShortName != null && argument.ShortName.Length > 0)
                    {
                        //Debug.Assert(!argumentMap.ContainsKey(argument.ShortName));
                        this.argumentMap[argument.ShortName] = argument;
                    }
                    else
                    {
                        argument.ClearShortName();
                    }
                }
            }

            // add implicit names which don't collide to map
            foreach (Argument argument in this.arguments)
            {
                if (!argument.ExplicitShortName)
                {
                    if (argument.ShortName != null && argument.ShortName.Length > 0 && !argumentMap.ContainsKey(argument.ShortName))
                        this.argumentMap[argument.ShortName] = argument;
                    else
                        argument.ClearShortName();
                }
            }
        }

        private static ArgumentAttribute GetAttribute(FieldInfo field)
        {
            object[] attributes = field.GetCustomAttributes(typeof(ArgumentAttribute), false);
            if (attributes.Length == 1)
                return (ArgumentAttribute)attributes[0];

            Debug.Assert(attributes.Length == 0);
            return null;
        }

        private void ReportUnrecognizedArgument(string argument)
        {
            this.reporter(string.Format("Unrecognized command line argument '{0}'", argument));
        }

        /// <summary>
        /// Parses an argument list into an object
        /// </summary>
        /// <param name="args"></param>
        /// <param name="destination"></param>
        /// <returns> true if an error occurred </returns>
        private bool ParseArgumentList(string[] args, object destination)
        {
            bool hadError = false;
            if (args != null)
            {
                foreach (string argument in args)
                {
                    if (argument.Length > 0)
                    {
                        switch (argument[0])
                        {
                            case '-':
                            case '/':
                                int endIndex = argument.IndexOfAny(new char[] { ':', '+', '-' }, 1);
                                string option = argument.Substring(1, endIndex == -1 ? argument.Length - 1 : endIndex - 1);
                                string optionArgument;
                                if (option.Length + 1 == argument.Length)
                                {
                                    optionArgument = null;
                                }
                                else if (argument.Length > 1 + option.Length && argument[1 + option.Length] == ':')
                                {
                                    optionArgument = argument.Substring(option.Length + 2);
                                }
                                else
                                {
                                    optionArgument = argument.Substring(option.Length + 1);
                                }

                                Argument arg = (Argument)this.argumentMap[option];
                                if (arg == null)
                                {
                                    ReportUnrecognizedArgument(argument);
                                    hadError = true;
                                }
                                else
                                {
                                    hadError |= !arg.SetValue(optionArgument, destination);
                                }
                                break;
                            case '@':
                                string[] nestedArguments;
                                hadError |= LexFileArguments(argument.Substring(1), out nestedArguments);
                                hadError |= ParseArgumentList(nestedArguments, destination);
                                break;
                            default:
                                if (this.defaultArgument != null)
                                {
                                    hadError |= !this.defaultArgument.SetValue(argument, destination);
                                }
                                else
                                {
                                    ReportUnrecognizedArgument(argument);
                                    hadError = true;
                                }
                                break;
                        }
                    }
                }
            }

            return hadError;
        }

        /// <summary>
        /// Parses an argument list.
        /// </summary>
        /// <param name="args"> The arguments to parse. </param>
        /// <param name="destination"> The destination of the parsed arguments. </param>
        /// <returns> true if no parse errors were encountered. </returns>
        public bool Parse(string[] args, object destination)
        {
            bool hadError = ParseArgumentList(args, destination);

            // check for missing required arguments
            foreach (Argument arg in this.arguments)
            {
                hadError |= arg.Finish(destination);
            }
            if (this.defaultArgument != null)
            {
                hadError |= this.defaultArgument.Finish(destination);
            }

            return !hadError;
        }

        private struct ArgumentHelpStrings
        {
            public ArgumentHelpStrings(string syntax, string help)
            {
                this.syntax = syntax;
                this.help = help;
            }

            public string syntax;
            public string help;
        }

        /// <summary>
        /// A user firendly usage string describing the command line argument syntax.
        /// </summary>
        public string GetUsageString(int screenWidth)
        {
            ArgumentHelpStrings[] strings = GetAllHelpStrings();

            int maxParamLen = 0;
            foreach (ArgumentHelpStrings helpString in strings)
            {
                maxParamLen = Math.Max(maxParamLen, helpString.syntax.Length);
            }

            const int minimumNumberOfCharsForHelpText = 10;
            const int minimumHelpTextColumn = 5;
            const int minimumScreenWidth = minimumHelpTextColumn + minimumNumberOfCharsForHelpText;

            int helpTextColumn;
            int idealMinimumHelpTextColumn = maxParamLen + spaceBeforeParam;
            screenWidth = Math.Max(screenWidth, minimumScreenWidth);
            if (screenWidth < (idealMinimumHelpTextColumn + minimumNumberOfCharsForHelpText))
                helpTextColumn = minimumHelpTextColumn;
            else
                helpTextColumn = idealMinimumHelpTextColumn;

            const string newLine = "\n";
            StringBuilder builder = new StringBuilder();

            builder.AppendLine();

            Assembly assembly = Assembly.GetEntryAssembly();
            if (assembly != null)
            {
                builder.AppendFormat("{0} version {1}", GetTitle(assembly), assembly.GetName().Version);
                builder.AppendLine();

                AssemblyCopyrightAttribute copyright = GetAssemblyAttribute<AssemblyCopyrightAttribute>(assembly);
                if (copyright != null && !string.IsNullOrEmpty(copyright.Copyright))
                    builder.AppendLine(copyright.Copyright.Replace("©", "(C)"));
                builder.AppendLine();

                builder.AppendFormat("Usage: {0}", Path.GetFileNameWithoutExtension(assembly.Location));
                foreach (ArgumentHelpStrings helpStrings in strings)
                    builder.AppendFormat(" {0}", helpStrings.syntax);
                builder.AppendLine();
                builder.AppendLine();
            }

            foreach (ArgumentHelpStrings helpStrings in strings)
            {
                // add syntax string
                int syntaxLength = helpStrings.syntax.Length;
                builder.Append(helpStrings.syntax);

                // start help text on new line if syntax string is too long
                int currentColumn = syntaxLength;
                if (syntaxLength >= helpTextColumn)
                {
                    builder.Append(newLine);
                    currentColumn = 0;
                }

                // add help text broken on spaces
                int charsPerLine = screenWidth - helpTextColumn;
                int index = 0;
                while (index < helpStrings.help.Length)
                {
                    // tab to start column
                    builder.Append(' ', helpTextColumn - currentColumn);
                    currentColumn = helpTextColumn;

                    // find number of chars to display on this line
                    int endIndex = index + charsPerLine;
                    if (endIndex >= helpStrings.help.Length)
                    {
                        // rest of text fits on this line
                        endIndex = helpStrings.help.Length;
                    }
                    else
                    {
                        endIndex = helpStrings.help.LastIndexOf(' ', endIndex - 1, Math.Min(endIndex - index, charsPerLine));
                        if (endIndex <= index)
                        {
                            // no spaces on this line, append full set of chars
                            endIndex = index + charsPerLine;
                        }
                    }

                    // add chars
                    builder.Append(helpStrings.help, index, endIndex - index);
                    index = endIndex;

                    // do new line
                    AddNewLine(newLine, builder, ref currentColumn);

                    // don't start a new line with spaces
                    while (index < helpStrings.help.Length && helpStrings.help[index] == ' ')
                        index++;
                }

                // add newline if there's no help text                
                if (helpStrings.help.Length == 0)
                {
                    builder.Append(newLine);
                }
            }

            return builder.ToString();
        }

        private static string GetTitle(Assembly assembly)
        {
            string prefix = "";
            AssemblyCompanyAttribute company = GetAssemblyAttribute<AssemblyCompanyAttribute>(assembly);
            if (company != null && !string.IsNullOrEmpty(company.Company))
                prefix = company.Company + " ";

            AssemblyTitleAttribute title = GetAssemblyAttribute<AssemblyTitleAttribute>(assembly);
            if (title != null && !string.IsNullOrEmpty(title.Title))
                return title.Title.StartsWith(prefix) ? title.Title : prefix + title.Title;

            AssemblyProductAttribute product = GetAssemblyAttribute<AssemblyProductAttribute>(assembly);
            if (title != null && !string.IsNullOrEmpty(product.Product))
                return product.Product.StartsWith(prefix) ? product.Product : prefix + product.Product;

            AssemblyDescriptionAttribute description = GetAssemblyAttribute<AssemblyDescriptionAttribute>(assembly);
            if (title != null && !string.IsNullOrEmpty(description.Description))
                return description.Description.StartsWith(prefix) ? description.Description : prefix + description.Description;

            return prefix + assembly.GetName();
        }

        private static T GetAssemblyAttribute<T>(Assembly assembly) where T : Attribute
        {
            object[] attributes = assembly.GetCustomAttributes(typeof(T), true);
            if (attributes != null && attributes.Length > 0)
                return (T)attributes[0];
            return null;
        }

        private static void AddNewLine(string newLine, StringBuilder builder, ref int currentColumn)
        {
            builder.Append(newLine);
            currentColumn = 0;
        }

        private ArgumentHelpStrings[] GetAllHelpStrings()
        {
            ArgumentHelpStrings[] strings = new ArgumentHelpStrings[NumberOfParametersToDisplay()];

            int index = 0;
            foreach (Argument arg in this.arguments)
                strings[index++] = GetHelpStrings(arg);
            if (this.defaultArgument != null)
                strings[index++] = GetHelpStrings(this.defaultArgument);
            strings[index++] = new ArgumentHelpStrings("@<file>", "Read response file for more options.");

            return strings;
        }

        private static ArgumentHelpStrings GetHelpStrings(Argument arg)
        {
            return new ArgumentHelpStrings(arg.SyntaxHelp, arg.FullHelpText);
        }

        private int NumberOfParametersToDisplay()
        {
            int numberOfParameters = this.arguments.Count + 1;
            if (HasDefaultArgument)
                numberOfParameters += 1;
            return numberOfParameters;
        }

        /// <summary>
        /// Does this parser have a default argument.
        /// </summary>
        /// <value> Does this parser have a default argument. </value>
        public bool HasDefaultArgument
        {
            get
            {
                return this.defaultArgument != null;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private bool LexFileArguments(string fileName, out string[] arguments1)
        {
            string args = null;

            try
            {
                using (FileStream file = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    StreamReader sreader = new StreamReader(file);
                    args = sreader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                this.reporter(string.Format("Error: Can't open command line argument file '{0}' : '{1}'", fileName, e.Message));
                arguments1 = null;
                return true;
            }

            bool hadError = false;
            ArrayList argArray = new ArrayList();
            StringBuilder currentArg = new StringBuilder();
            bool inQuotes = false;
            int index = 0;

            // while (index < args.Length)
            try
            {
                while (true)
                {
                    // skip whitespace
                    while (char.IsWhiteSpace(args[index]))
                    {
                        index += 1;
                    }

                    // # - comment to end of line
                    if (args[index] == '#')
                    {
                        index += 1;
                        while (args[index] != '\n')
                        {
                            index += 1;
                        }
                        continue;
                    }

                    // do one argument
                    do
                    {
                        if (args[index] == '\\')
                        {
                            int cSlashes = 1;
                            index += 1;
                            while (index == args.Length && args[index] == '\\')
                            {
                                cSlashes += 1;
                            }

                            if (index == args.Length || args[index] != '"')
                            {
                                currentArg.Append('\\', cSlashes);
                            }
                            else
                            {
                                currentArg.Append('\\', (cSlashes >> 1));
                                if (0 != (cSlashes & 1))
                                {
                                    currentArg.Append('"');
                                }
                                else
                                {
                                    inQuotes = !inQuotes;
                                }
                            }
                        }
                        else if (args[index] == '"')
                        {
                            inQuotes = !inQuotes;
                            index += 1;
                        }
                        else
                        {
                            currentArg.Append(args[index]);
                            index += 1;
                        }
                    } while (!char.IsWhiteSpace(args[index]) || inQuotes);
                    argArray.Add(currentArg.ToString());
                    currentArg.Length = 0;
                }
            }
            catch (System.IndexOutOfRangeException)
            {
                // got EOF 
                if (inQuotes)
                {
                    this.reporter(string.Format("Error: Unbalanced '\"' in command line argument file '{0}'", fileName));
                    hadError = true;
                }
                else if (currentArg.Length > 0)
                {
                    // valid argument can be terminated by EOF
                    argArray.Add(currentArg.ToString());
                }
            }

            arguments1 = (string[])argArray.ToArray(typeof(string));
            return hadError;
        }

        private static string LongName(ArgumentAttribute attribute, FieldInfo field)
        {
            return (attribute == null || attribute.DefaultLongName) ? field.Name : attribute.LongName;
        }

        private static string ShortName(ArgumentAttribute attribute, FieldInfo field)
        {
            if (attribute is DefaultArgumentAttribute)
                return null;
            if (!ExplicitShortName(attribute))
                return LongName(attribute, field).Substring(0, 1);
            return attribute.ShortName;
        }

        private static string HelpText(ArgumentAttribute attribute /*, FieldInfo field */)
        {
            if (attribute == null)
                return null;
            else
                return attribute.HelpText;
        }

        private static bool HasHelpText(ArgumentAttribute attribute)
        {
            return (attribute != null && attribute.HasHelpText);
        }

        private static bool ExplicitShortName(ArgumentAttribute attribute)
        {
            return (attribute != null && !attribute.DefaultShortName);
        }

        private static object DefaultValue(ArgumentAttribute attribute /*, FieldInfo field */)
        {
            return (attribute == null || !attribute.HasDefaultValue) ? null : attribute.DefaultValue;
        }

        private static Type ElementType(FieldInfo field)
        {
            if (IsCollectionType(field.FieldType))
                return field.FieldType.GetElementType();
            else
                return null;
        }

        private static ArgumentType Flags(ArgumentAttribute attribute, FieldInfo field)
        {
            if (attribute != null)
                return attribute.Type;
            else if (IsCollectionType(field.FieldType))
                return ArgumentType.MultipleUnique;
            else
                return ArgumentType.AtMostOnce;
        }

        private static bool IsCollectionType(Type type)
        {
            return type.IsArray;
        }

        private static bool IsValidElementType(Type type)
        {
            return type != null && (
                type == typeof(int) ||
                type == typeof(uint) ||
                type == typeof(string) ||
                type == typeof(bool) ||
                type.IsEnum);
        }

        private class Argument
        {
            public Argument(ArgumentAttribute attribute, FieldInfo field, ErrorReporter reporter)
            {
                this.longName = CommandLineParser.LongName(attribute, field);
                this.explicitShortName = CommandLineParser.ExplicitShortName(attribute);
                this.shortName = CommandLineParser.ShortName(attribute, field);
                this.hasHelpText = CommandLineParser.HasHelpText(attribute);
                this.helpText = CommandLineParser.HelpText(attribute/* , field */);
                this.defaultValue = CommandLineParser.DefaultValue(attribute /* , field */);
                this.elementType = ElementType(field);
                this.flags = Flags(attribute, field);
                this.field = field;
                this.seenValue = false;
                this.reporter = reporter;
                this.isDefault = attribute != null && attribute is DefaultArgumentAttribute;

                if (IsCollection)
                {
                    this.collectionValues = new ArrayList();
                }

                Debug.Assert(!String.IsNullOrEmpty(this.longName));
                Debug.Assert(!this.isDefault || !this.ExplicitShortName);
                Debug.Assert(!IsCollection || AllowMultiple, "Collection arguments must have allow multiple");
                Debug.Assert(!Unique || IsCollection, "Unique only applicable to collection arguments");
                Debug.Assert(IsValidElementType(Type) ||
                    IsCollectionType(Type));
                Debug.Assert((IsCollection && IsValidElementType(elementType)) ||
                    (!IsCollection && elementType == null));
                Debug.Assert(!(this.IsRequired && this.HasDefaultValue), "Required arguments cannot have default value");
                Debug.Assert(!this.HasDefaultValue || (this.defaultValue.GetType() == field.FieldType), "Type of default value must match field type");
            }

            public bool Finish(object destination)
            {
                if (!this.SeenValue && this.HasDefaultValue)
                {
                    this.field.SetValue(destination, this.DefaultValue);
                }
                if (this.IsCollection)
                {
                    this.field.SetValue(destination, this.collectionValues.ToArray(this.elementType));
                }

                return ReportMissingRequiredArgument();
            }

            private bool ReportMissingRequiredArgument()
            {
                if (this.IsRequired && !this.SeenValue)
                {
                    if (this.IsDefault)
                        reporter(string.Format("Missing required argument '<{0}>'.", this.LongName));
                    else
                        reporter(string.Format("Missing required argument '/{0}'.", this.LongName));
                    return true;
                }
                return false;
            }

            private void ReportDuplicateArgumentValue(string value)
            {
                this.reporter(string.Format("Duplicate '{0}' argument '{1}'", this.LongName, value));
            }

            public bool SetValue(string value, object destination)
            {
                if (SeenValue && !AllowMultiple)
                {
                    this.reporter(string.Format("Duplicate '{0}' argument", this.LongName));
                    return false;
                }
                this.seenValue = true;

                object newValue;
                if (!ParseValue(this.ValueType, value, out newValue))
                    return false;
                if (this.IsCollection)
                {
                    if (this.Unique && this.collectionValues.Contains(newValue))
                    {
                        ReportDuplicateArgumentValue(value);
                        return false;
                    }
                    else
                    {
                        this.collectionValues.Add(newValue);
                    }
                }
                else
                {
                    this.field.SetValue(destination, newValue);
                }

                return true;
            }

            public Type ValueType
            {
                get
                {
                    return this.IsCollection ? this.elementType : this.Type;
                }
            }

            private void ReportBadArgumentValue(string value)
            {
                this.reporter(string.Format("'{0}' is not a valid value for the '{1}' command line option", value, this.LongName));
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
            private bool ParseValue(Type type, string stringData, out object value)
            {
                // null is only valid for bool variables
                // empty string is never valid
                if ((stringData != null || type == typeof(bool)) && (stringData == null || stringData.Length > 0))
                {
                    try
                    {
                        if (type == typeof(string))
                        {
                            value = stringData;
                            return true;
                        }
                        else if (type == typeof(bool))
                        {
                            if (stringData == null || stringData == "+")
                            {
                                value = true;
                                return true;
                            }
                            else if (stringData == "-")
                            {
                                value = false;
                                return true;
                            }
                        }
                        else if (type == typeof(int))
                        {
                            value = int.Parse(stringData);
                            return true;
                        }
                        else if (type == typeof(uint))
                        {
                            value = uint.Parse(stringData);
                            return true;
                        }
                        else
                        {
                            Debug.Assert(type.IsEnum);
                            value = Enum.Parse(type, stringData, true);
                            return true;
                        }
                    }
                    catch
                    {
                        // catch parse errors
                    }
                }

                ReportBadArgumentValue(stringData);
                value = null;
                return false;
            }

            private void AppendValue(StringBuilder builder, object value)
            {
                if (value is string || value is int || value is uint || value.GetType().IsEnum)
                {
                    builder.Append(value.ToString());
                }
                else if (value is bool)
                {
                    builder.Append((bool)value ? "+" : "-");
                }
                else
                {
                    bool first = true;
                    foreach (object o in (System.Array)value)
                    {
                        if (!first)
                        {
                            builder.Append(", ");
                        }
                        AppendValue(builder, o);
                        first = false;
                    }
                }
            }

            public string LongName
            {
                get
                {
                    return this.longName;
                }
            }

            public bool ExplicitShortName
            {
                get
                {
                    return this.explicitShortName;
                }
            }

            public string ShortName
            {
                get
                {
                    return this.shortName;
                }
            }

            public bool HasShortName
            {
                get
                {
                    return this.shortName != null;
                }
            }

            public void ClearShortName()
            {
                this.shortName = null;
            }

            public bool HasHelpText
            {
                get
                {
                    return this.hasHelpText;
                }
            }

            public string HelpText
            {
                get
                {
                    return this.helpText;
                }
            }

            public object DefaultValue
            {
                get
                {
                    return this.defaultValue;
                }
            }

            public bool HasDefaultValue
            {
                get
                {
                    return null != this.defaultValue;
                }
            }

            public string FullHelpText
            {
                get
                {
                    StringBuilder builder = new StringBuilder();
                    if (this.HasHelpText)
                    {
                        builder.Append(this.HelpText);
                    }
                    if (this.HasDefaultValue)
                    {
                        if (builder.Length > 0)
                            builder.Append(" ");
                        builder.Append("Default value: '");
                        AppendValue(builder, this.DefaultValue);
                        builder.Append('\'');
                    }
                    if (this.HasShortName)
                    {
                        if (builder.Length > 0)
                            builder.Append(" ");
                        builder.Append("(Short form: /");
                        builder.Append(this.ShortName);
                        builder.Append(")");
                    }
                    return builder.ToString();
                }
            }

            public string SyntaxHelp
            {
                get
                {
                    StringBuilder builder = new StringBuilder();

                    if (this.IsDefault)
                    {
                        builder.Append("<");
                        builder.Append(this.LongName);
                        builder.Append(">");
                    }
                    else
                    {
                        if (!this.IsRequired)
                            builder.Append("[");

                        builder.Append("/");
                        builder.Append(this.LongName);
                        Type valueType = this.ValueType;
                        if (valueType == typeof(int))
                        {
                            builder.Append(":<int>");
                        }
                        else if (valueType == typeof(uint))
                        {
                            builder.Append(":<uint>");
                        }
                        else if (valueType == typeof(bool))
                        {
                            builder.Append("[+|-]");
                        }
                        else if (valueType == typeof(string))
                        {
                            builder.Append(":<string>");
                        }
                        else
                        {
                            Debug.Assert(valueType.IsEnum);

                            builder.Append(":{");
                            bool first = true;
                            foreach (FieldInfo fd in valueType.GetFields())
                            {
                                if (fd.IsStatic)
                                {
                                    if (first)
                                        first = false;
                                    else
                                        builder.Append('|');
                                    builder.Append(fd.Name);
                                }
                            }
                            builder.Append('}');
                        }

                        if (!this.IsRequired)
                            builder.Append("]");
                    }

                    if (this.AllowMultiple)
                        if (this.IsRequired)
                            builder.Append("+");
                        else
                            builder.Append("*");

                    return builder.ToString();
                }
            }

            public bool IsRequired
            {
                get
                {
                    return 0 != (this.flags & ArgumentType.Required);
                }
            }

            public bool SeenValue
            {
                get
                {
                    return this.seenValue;
                }
            }

            public bool AllowMultiple
            {
                get
                {
                    return 0 != (this.flags & ArgumentType.Multiple);
                }
            }

            public bool Unique
            {
                get
                {
                    return 0 != (this.flags & ArgumentType.Unique);
                }
            }

            public Type Type
            {
                get
                {
                    return field.FieldType;
                }
            }

            public bool IsCollection
            {
                get
                {
                    return IsCollectionType(Type);
                }
            }

            public bool IsDefault
            {
                get
                {
                    return this.isDefault;
                }
            }

            private string longName;
            private string shortName;
            private string helpText;
            private bool hasHelpText;
            private bool explicitShortName;
            private object defaultValue;
            private bool seenValue;
            private FieldInfo field;
            private Type elementType;
            private ArgumentType flags;
            private ArrayList collectionValues;
            private ErrorReporter reporter;
            private bool isDefault;
        }

        private ArrayList arguments;
        private Hashtable argumentMap;
        private Argument defaultArgument;
        private ErrorReporter reporter;
    }
}
