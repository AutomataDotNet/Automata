using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using QUT.Gppg;

namespace Microsoft.Automata.MSO.Mona
{
    public enum MonaParseExceptionKind
    {
        SytaxError,
        InternalError,
        DuplicateDeclaration,
        UndeclaredIdentifier,
        InvalidIntegerFormat,
        TypeMismatch,
        UnknownArithmeticOperator,
        UnexpectedCharacter,
        UnexpectedToken,
        InvalidUniverseDeclaration,
        UndeclaredConstant,
        IdentifierIsNotDeclaredConstant,
        InvalidUseOfPredicateOrMacroName,
        UndeclaredPredicate,
        InvalidUseOfName,
        InvalidNrOfParameters,
        InvalidUniverseReference,
        DuplicateAllpos,
        UnexpectedDeclaration,
    }

    [Serializable]
    public class MonaParseException : Exception
    {
        internal LexLocationInFile location = null;
        MonaParseExceptionKind kind = MonaParseExceptionKind.SytaxError;

        public MonaParseExceptionKind Kind
        {
            get
            {
                return kind;
            }
        }

        public int StartLine
        {
            get { return (location == null ? 0 : location.StartLine); }
        }
        public int StartColumn
        {
            get { return (location == null ? 0 : location.StartColumn + 1); }
        }
        public int EndLine
        {
            get { return (location == null ? 0 : location.EndLine); }
        }
        public int EndColumn
        {
            get { return (location == null ? 0 : location.EndColumn + 1); }
        }

        public string File
        {
            get { return (location == null ? "" : location.File); }
        }

        public bool HasLocationInfo
        {
            get { return location != null; }
        }

        internal MonaParseException()
            : base()
        {
        }

        internal MonaParseException(string message, Exception innerexception) : base(message, innerexception)
        {
        }

        internal MonaParseException(MonaParseExceptionKind kind, LexLocationInFile location,  string message = "")
            : base(kind.ToString() + (message== "" ? "" : ": " + message))
        {
            this.location = location;
            this.kind = kind;
        }

        public override string ToString()
        {
            string res = "";
            if (location != null)
                res += string.Format("{5}({0},{1},{2},{3})", StartLine, StartColumn, EndLine, EndColumn, File);
            if (res != "")
                res += ": ";
            res += Message;
            return res;
        }
    }
}

