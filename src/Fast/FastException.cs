using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Fast
{
    public enum FastExceptionKind
    {
        NotImplemented,
        UnexpectedArgument,
        InvalidSort,
        EmptyEnumeration,
        InvalidArity,
        DuplicateRecordField,
        InvalidRecordField,
        InternalError,
        NotCharacter,
        UnknownFunction
    }

    public class FastException : Exception
    {
        public readonly FastExceptionKind kind;
        public FastException(FastExceptionKind kind)
        {
            this.kind = kind;
        }
    }
}
