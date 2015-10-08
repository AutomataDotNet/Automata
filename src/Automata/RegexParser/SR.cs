using System;
using System.Collections.Generic;
using System.Text;

namespace System.Text.RegularExpressions  
{
    static class SR
    {
        public static string GetString(string s, params object[] o)
        {
            return s;
        }

        public const string ReplacementError = "ReplacementError";
        public const string UnexpectedOpcode = "UnexpectedOpcode";
        public const string TooManyParens = "TooManyParens";
        public const string NestedQuantify = "NestedQuantify";
        public const string QuantifyAfterNothing = "QuantifyAfterNothing";
        public const string InternalError = "InternalError";
        public const string IllegalRange = "IllegalRange";
        public const string NotEnoughParens = "NotEnoughParens";
        public const string BadClassInCharRange = "BadClassInCharRange";
        public const string SubtractionMustBeLast = "SubtractionMustBeLast";
        public const string ReversedCharRange = "ReversedCharRange";
        public const string UnterminatedBracket = "UnterminatedBracket";
        public const string InvalidGroupName = "InvalidGroupName";
        public const string CapnumNotZero = "CapnumNotZero";
        public const string UndefinedBackref = "UndefinedBackref";
        public const string MalformedReference = "MalformedReference";
        public const string AlternationCantHaveComment = "AlternationCantHaveComment";
        public const string AlternationCantCapture = "AlternationCantCapture";
        public const string UnrecognizedGrouping = "UnrecognizedGrouping";
        public const string IllegalEndEscape = "IllegalEndEscape";
        public const string CaptureGroupOutOfRange = "CaptureGroupOutOfRange";
        public const string TooFewHex = "TooFewHex";
        public const string MissingControl = "MissingControl";
        public const string UnrecognizedControl = "UnrecognizedControl";
        public const string UnrecognizedEscape = "UnrecognizedEscape";
        public const string IncompleteSlashP = "IncompleteSlashP";
        public const string MalformedSlashP = "MalformedSlashP";
        public const string IllegalCondition = "IllegalCondition";
        public const string TooManyAlternates = "TooManyAlternates";
        public const string MakeException = "MakeException";
        public const string UndefinedNameRef = "UndefinedNameRef";
        public const string UndefinedReference = "UndefinedReference";
        public const string UnterminatedComment = "UnterminatedComment";
        public const string MalformedNameRef = "MalformedNameRef";
        public const string UnknownProperty = "UnknownProperty";
   }
}
