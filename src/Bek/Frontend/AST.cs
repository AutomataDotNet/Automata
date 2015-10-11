using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Bek.Frontend.TreeOps;
using System.Reflection;
using System.Globalization;
using Microsoft.Bek.Frontend.Meta;

namespace Microsoft.Bek.Frontend.AST
{
    /// <summary>
    /// Line number and character position information.
    /// </summary>
    public interface ILocation
    {
        /// <summary>
        /// Line number.
        /// </summary>
        int Line { get; }

        /// <summary>
        /// Character position number.
        /// </summary>
        int Pos { get; }
    }

    /// <summary>
    /// Interface for emitting code fragments.
    /// </summary>
    public interface IEmitCode
    {
        /// <summary>
        /// Append JavaScript code fragment to sb.
        /// </summary>
        void ToJS(StringBuilder sb);

        /// <summary>
        /// Append C# code fragment to sb.
        /// </summary>
        void ToCS(StringBuilder sb);

        /// <summary>
        /// Append C code fragment to sb.
        /// </summary>
        void ToC(StringBuilder sb);

        /// <summary>
        /// Generate 'C#', 'C', or 'JS' code fragment and append it to the string builder.
        /// </summary>
        /// <param name="language">language identifier must be one of 'C#', 'C', or 'JS'</param>
        /// <param name="sb">the code is appended here</param>
        void GenerateCode(string language, StringBuilder sb);

        /// <summary>
        /// Enumerate all function names.
        /// </summary>
        IEnumerable<ident> GetFunctionNames();

        /// <summary>
        /// Enumerate all bound variables (variables of the form #&lt;k&gt;) where k is some integer identifier.
        /// </summary>
        IEnumerable<ident> GetBoundVars();
    }

    public class BekPgm : IEmitCode, ILocation
    {
        public ident id;

        public int Line
        {
            get { return id.Line; }
        }

        public int Pos
        {
            get { return id.Pos; }
        }

        public List<BekLocalFunction> funcs = new List<BekLocalFunction>();

        public void AddLocalFunctions(IEnumerable<BekLocalFunction> localFuncs)
        {
            funcs.AddRange(localFuncs);
        }

        public BekPgm(ident id, ident inputvar, stmt body)
        {
            this.id = id;
            this.name = id.name;
            this.input = inputvar;
            this.body = body;
        }

        public string name;

        public ident input;

        public stmt body;

        public virtual void ToJS(StringBuilder sb)
        {
            sb.Append("function ");
            sb.Append(name);
            sb.Append("(");
            input.ToJS(sb);
            sb.AppendLine("){");

            foreach (var f in GetUniqueFunctionNames())
                DefineJavaScriptFunction(f, sb);

            //must be a return statement
            returnstmt ret = body as returnstmt;
            if (ret == null)
                throw new BekParseException(ret.val.Line, ret.val.Pos, "Illformed function body, expecting return statement.");

            var str = ret.val;
            iterexpr itr = str as iterexpr;
            if (itr != null)
            {
                itr.ToJS1(sb);
            }
            else //must be strconst -- uncommon case
            {
                strconst c = str as strconst;
                if (c == null)
                    throw new BekParseException(str.Line, str.Pos, "Illformed function body, unexpected string expression.");

                sb.AppendLine("var result = new Array();");
                if (c.val != "")
                {
                    sb.Append("result.push(String.fromCharCode(");
                    for (int i = 0; i < c.val.Length; i++)
                    {
                        if (i > 0)
                            sb.Append(", ");
                        sb.Append(string.Format("0x{0:X}", (int)(c.val[i])));
                    }
                    sb.AppendLine("));");
                }
                sb.AppendLine("return result.join('');");
            }

            sb.AppendLine("}");
        }
   
        protected void DefineJavaScriptFunction(ident f, StringBuilder sb)
        {
            var func = funcs.Find(fn => fn.id.name.Equals(f.name));
            if (func != null)
            {
                func.ToJS(sb);
                sb.AppendLine(";");
                return;
            }

            switch (f.name)
            {
                case "dec":
                    {
                        sb.AppendLine(@"
function _dec(){
var _dec_result = 0; 
var k = 1;
for (var i = _dec.arguments.length-1; i >= 0; i--)
{
_dec_result = _dec_result + ((_dec.arguments[i] & 0xF) * k);
k = k * 10;
}
return _dec_result;
};");
                        break;
                    }
                case "hex":
                    {
                        sb.AppendLine(@"
function _hex(){
var h = 0;
if (_hex.arguments.length == 1)
{
h = _hex.arguments[0] & 0xF;
h = (h <= 9 ? h + 48 : h + 55);
}
else if (_hex.arguments.length > 1)
{
h = (_hex.arguments[1] >> (4 * _hex.arguments[0])) & 0xF;
h = (h <= 9 ? h + 48 : h + 55);
}
return h;
};");
                        break;

                    }
                case "BitExtract":
                    {
                        sb.AppendLine(@"
function _BitExtract(m, n, c){
var mask = 0;
for (var i = 0; i <= (m - n); i++) { mask = (mask << 1) + 1; }
return (c >> n) & mask;
};");

                        break;

                    }
                case "Bits":
                    {
                        sb.AppendLine(@"
function _Bits(m, n, c){
var mask = 0;
for (var i = 0; i <= (m - n); i++) { mask = (mask << 1) + 1; }
return (c >> n) & mask;
};");

                        break;

                    }

                case "IsLowSurrogate":
                    {
                        sb.AppendLine(@"
function _IsLowSurrogate(c){
return ((0xdc00 <= c) && (c <= 0xdfff)) 
};");
                        break;
                    }
                case "IsHighSurrogate":
                    {
                        sb.AppendLine(@"
function _IsHighSurrogate(c){
return ((0xd800 <= c) && (c <= 0xdbff)) 
};");
                        break;

                    }
                case "IsWhitespace":
                    {
                        sb.AppendLine(@"
function _IsWhitespace(c){
if (String.fromCharCode(c).match(/^(\s)$/))
  return true;
else
  return false;
};");
                        break;
                    }
                case "IsWordletter":
                    {
                        sb.AppendLine(@"
function _IsWordletter(c){
if (String.fromCharCode(c).match(/^(\w)$/))
  return true;
else
  return false;
};");
                        break;
                    }
                case "IsDecimalDigitNumber":  //digit
                    {
                        sb.AppendLine(@"
function _IsDecimalDigitNumber(c){
if (String.fromCharCode(c).match(/^(\d)$/))
  return true;
else
  return false;
};");
                        break;
                    }

                default:
                    throw new BekParseException(f.line, f.pos, string.Format("Unknown function '{0}'", f));
            }
        }

        public IEnumerable<ident> GetFunctionNames()
        {
            foreach (var f in body.GetFunctionNames())
            {
                yield return f;
                var func = funcs.Find(fn => fn.id.Equals(f));
                if (func != null)
                    foreach (var f1 in func.body.GetFunctionNames())
                        yield return f1;
            }
        }

        public IEnumerable<ident> GetBoundVars()
        {
            foreach (var v in body.GetBoundVars())
            {
                yield return v;
            }
        }

        protected IEnumerable<ident> GetUniqueFunctionNames()
        {
            HashSet<string> names = new HashSet<string>();
            foreach (var f in GetFunctionNames())
                if (names.Add(f.name))
                    yield return f;
        }


        public void ToCS(StringBuilder sb)
        {
            throw new NotImplementedException();
        }

        public void ToC(StringBuilder sb)
        {
            throw new NotImplementedException();
        }

        public void GenerateCode(string language, StringBuilder sb)
        {
            if (language == "JS")
                ToJS(sb);
            else if (language == "C#")
                ToCS(sb);
            else
                ToC(sb);
        }
    }

    public class BekLocalFunction : IEmitCode
    {
        public ident id;
        public ident[] args;
        public expr body;

        public BekLocalFunction(ident id, ident[] args, expr body)
        {
            this.id = id;
            this.body = body;
            this.args = args;
        }

        public void GenerateCode(string language, StringBuilder sb)
        {
            if (language == "JS")
                ToJS(sb);
            else if (language == "C#")
                ToCS(sb);
            else
                ToC(sb);
        }

        public void ToJS(StringBuilder sb)
        {
            sb.Append("function ");
            id.ToJS(sb);
            sb.Append("(");
            for (int i = 0; i < args.Length; i++ )
            {
                if (i > 0)
                    sb.Append(",");
                args[i].ToJS(sb);
            }
            sb.Append("){return ");
            body.ToJS(sb);
            sb.Append(";}");
        }

        static int foo(int a, int b, int c)
        {
            return a + b + c;
        }

        public void ToCS(StringBuilder sb)
        {
            sb.Append("static int ");
            id.ToCS(sb);
            sb.Append("(");
            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                sb.Append("int ");
                args[i].ToCS(sb);
            }
            sb.Append("){return ");
            body.ToCS(sb);
            sb.Append(";}");
        }

        public void ToC(StringBuilder sb)
        {
            sb.Append("int ");
            id.ToC(sb);
            sb.Append("(");
            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                sb.Append("int ");
                args[i].ToC(sb);
            }
            sb.Append("){return ");
            body.ToC(sb);
            sb.Append(";}");
        }

        public void ToCS(StringBuilder sb, bool ispredicate)
        {
            sb.Append("static ");
            if (ispredicate)
                sb.Append("bool ");
            else
                sb.Append("int ");
            id.ToCS(sb);
            sb.Append("(");
            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                sb.Append("int ");
                args[i].ToCS(sb);
            }
            sb.Append("){return ");
            body.ToCS(sb);
            sb.AppendLine(";}");
        }

        public void ToC(StringBuilder sb, bool ispredicate)
        {
            //sb.Append("static ");
            if (ispredicate)
                sb.Append("bool ");
            else
                sb.Append("int ");
            id.ToC(sb);
            sb.Append("(");
            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                sb.Append("int ");
                args[i].ToC(sb);
            }
            sb.Append("){return ");
            body.ToC(sb);
            sb.AppendLine(";}");
        }

        public IEnumerable<ident> GetFunctionNames()
        {
            return body.GetFunctionNames();
        }

        public IEnumerable<ident> GetBoundVars()
        {
            return body.GetBoundVars();
        }
    }

    public interface expr : IEmitCode, ILocation
    {
    }

    public class ident : expr
    {
        public string name;
        public int line;
        public int pos;

        public ident(string name, int line, int pos)
        {
            this.name = name;
            this.line = line;
            this.pos = pos;
        }

        public override string ToString()
        {
            return name;
        }

        public override bool Equals(object obj)
        {
            var id = obj as ident;
            if (id == null)
                return false;
            return name.Equals(id.name);
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }

        public void ToJS(StringBuilder sb)
        {
            sb.Append("_"); //add an underscore as a prefix of every identifier
            string n = name.Replace("#", "_"); //replace # with _ in vars
            sb.Append(n);
        }

        public IEnumerable<ident> GetFunctionNames()
        {
            yield break;
        }

        public bool IsVar
        {
            get { return this.name.StartsWith("#"); }
        }

        public int GetVarId()
        {
            return int.Parse(name.Substring(1));
        }

        public IEnumerable<ident> GetBoundVars()
        {
            if (IsVar)
                yield return this;
        }

        public void ToCS(StringBuilder sb)
        {
            string n = name.Replace("#", "_"); //replace # with _ in vars
            sb.Append(n);
        }

        public void ToC(StringBuilder sb)
        {
            string n = name.Replace("#", "_"); //replace # with _ in vars
            sb.Append(n);
        }

        public void GenerateCode(string language, StringBuilder sb)
        {
            switch (language)
            {
                case "C#":
                    ToCS(sb);
                    break;
                case "C":
                    ToC(sb);
                    break;
                case "JS":
                    ToJS(sb);
                    break;
                default:
                    throw new BekException(string.Format("Unexpected language identifier '{0}', expected one of 'C#', 'C', or 'JS'.", language));
            };
        }

        public int Line
        {
            get { return line; }
        }

        public int Pos
        {
            get { return pos; }
        }
    }

    public class charconst : expr
    {
        int line;
        int pos;

        public int Line
        {
            get { return line; }
        }

        public int Pos
        {
            get { return pos; }
        }

        public charconst(string str, BekCharModes mode = BekCharModes.BV16) : this(0, 0, str)
        {
        }

        public charconst(int line, int pos, string str, BekCharModes mode = BekCharModes.BV16)
        {
            this.line = line;
            this.pos = pos;

            IFormatProvider prov = new System.Globalization.NumberFormatInfo();

            string s;
            if (str.StartsWith("'"))
            {
                var str1 = str.Substring(1, str.Length - 2);
                if (str1.Length == 1)
                    val = (int)str1[0];
                else
                {
                    var unesc = System.Text.RegularExpressions.Regex.Unescape(str1);
                    if (unesc.Length != 1)
                        throw new BekParseException(string.Format("Illeagal character constant {0}", str));
                    val = (int)unesc[0];
                }
            }
            else if (str.StartsWith("0x")) //must be hexadecimal number
            {
                s = str.Substring(2, str.Length - 2);
                if (!int.TryParse(s, NumberStyles.AllowHexSpecifier, prov, out val))
                    throw new BekException(string.Format("Invalid hexadecimal numeral {0}", str));
            }
            else
            {
                s = str;
                if (!int.TryParse(s, out val)) //first try to parse as decimal numeral
                {
                    //we know at this point that the character constant string starts and ends with a single quote
                    string buffer = s.Substring(1, s.Length - 2);

                    if (mode != BekCharModes.BV32)
                    {
                        string intermediate = "";
                        try
                        {
                            intermediate = System.Text.RegularExpressions.Regex.Unescape(buffer);
                        }
                        catch (Exception e)
                        {
                            throw new BekException(string.Format("Illeagal character constant {0}", s), e);
                        }
                        if (intermediate.Length > 1)
                        {
                            throw new BekException(string.Format("Illeagal character constant {0}", s));
                        }
                        this.val = (int)intermediate[0];
                    }
                    else
                    {
                        try
                        {
                            string intermediate = System.Text.RegularExpressions.Regex.Unescape(buffer);
                            // extract unicode code points as integers
                            var e = StringInfo.GetTextElementEnumerator(intermediate);
                            if (e.MoveNext())
                            {
                                this.val = char.ConvertToUtf32(e.GetTextElement(), 0);
                            }
                            else
                            {
                                throw new Exception();
                            }
                            if (e.MoveNext())
                            {
                                throw new Exception();
                            }
                        }
                        catch (Exception e)
                        {
                            throw new BekException(string.Format("Illeagal character constant {0}", s), e);
                        }
                    }
                }
            }
        }

        public int val;

        public override string ToString()
        {
            return string.Format("'{0}'", Escape(val));
        }

        /// <summary>
        /// Make an escaped string from a character
        /// </summary>
        internal static string Escape(int code)
        {
            if (code > 126)
                return ToUnicodeRepr(code);
            char c = (char)code;

            switch (c)
            {
                case '\0':
                    return @"\0";
                case '\a':
                    return @"\a";
                case '\b':
                    return @"\b";
                case '\t':
                    return @"\t";
                case '\r':
                    return @"\r";
                case '\v':
                    return @"\v";
                case '\f':
                    return @"\f";
                case '\n':
                    return @"\n";
                case '\u001B':
                    return @"\e";
                case '"':
                    return "\\\"";
                case '\'':
                    return "\\\'";
                case '\\':
                    return "\\\\";
                case ' ':
                    return @"\s";
                default:
                    if (code < 32)
                        return string.Format("\\x{0:X}", code);
                    else
                        return c.ToString();
            }
        }

        static string ToUnicodeRepr(int i)
        {
            string s = string.Format("{0:X}", i);
            if (s.Length == 1)
                s = "\\u000" + s;
            else if (s.Length == 2)
                s = "\\u00" + s;
            else if (s.Length == 3)
                s = "\\u0" + s;
            else
                s = "\\u" + s;
            return s;
        }

        public void GenerateCode(string language, StringBuilder sb)
        {
            //use hexadecimal representation of numbers
            string s = string.Format("0x{0:X}", val);
            sb.Append(s);
        }

        public void ToJS(StringBuilder sb)
        {
            //use hexadecimal representation of numbers
            string s = string.Format("0x{0:X}", val);
            sb.Append(s);
        }

        public void ToCS(StringBuilder sb)
        {
            //use hexadecimal representation of numbers
            string s = string.Format("0x{0:X}", val);
            sb.Append(s);
        }

        public void ToC(StringBuilder sb)
        {
            //use hexadecimal representation of numbers
            string s = string.Format("0x{0:X}", val);
            sb.Append(s);
        }

        public IEnumerable<ident> GetFunctionNames()
        {
            yield break;
        }

        public IEnumerable<ident> GetBoundVars()
        {
            yield break;
        }
    }

    public class functioncall : expr
    {
        public ident id;

        public int Line
        {
            get { return id.line; }
        }

        public int Pos
        {
            get { return id.pos; }
        }

        public functioncall(ident n, IEnumerable<expr> cont)
        {
            this.id = n;
            args = new List<expr>(cont);
        }

        public functioncall(ident n, params expr[] cont)
        {
            this.id = n;
            args = new List<expr>(cont);
        }

        [ChildList]
        [AllowBekTypes(BekTypes.CHAR, BekTypes.BOOL, BekTypes.ANY)]
        public List<expr> args;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(id);
            sb.Append("(");
            for (int i = 0; i < args.Count; i++)
            {
                if (i > 0)
                    sb.Append(",");
                sb.Append(args[i].ToString());
            }
            sb.Append(")");
            return sb.ToString();
        }

        public IEnumerable<ident> GetFunctionNames()
        {
            if (!id.name.Equals("ite"))
                yield return id;

            foreach (var arg in args)
                foreach (var f in arg.GetFunctionNames())
                    yield return f;
        }

        public IEnumerable<ident> GetBoundVars()
        {
            foreach (var arg in args)
                foreach (var f in arg.GetBoundVars())
                    yield return f;
        }

        public void GenerateCode(string language, StringBuilder sb)
        {
            switch (language)
            {
                case "C#":
                    ToCS(sb);
                    break;
                case "C":
                    ToC(sb);
                    break;
                case "JS":
                    ToJS(sb);
                    break;
                default:
                    throw new BekException(string.Format("Unexpected language identifier '{0}', expected one of 'C#', 'C', or 'JS'.", language));
            };
        }

        public void ToJS(StringBuilder sb)
        {
            if (id.name == "ite")
            {
                sb.Append("(");
                args[0].ToJS(sb);
                sb.Append(" ? ");
                args[1].ToJS(sb);
                sb.Append(" : ");
                args[2].ToJS(sb);
                sb.Append(")");
            }
            else
            {
                id.ToJS(sb);
                sb.Append("(");
                bool add_comma = false;
                foreach (var arg in args)
                {
                    if (add_comma)
                        sb.Append(",");
                    add_comma = true;
                    arg.ToJS(sb);
                }
                sb.Append(")");
            }
        }

        public void ToCS(StringBuilder sb)
        {
            if (id.name == "ite")
            {
                sb.Append("(");
                args[0].ToCS(sb);
                sb.Append(" ? ");
                args[1].ToCS(sb);
                sb.Append(" : ");
                args[2].ToCS(sb);
                sb.Append(")");
            }
            else
            {
                id.ToCS(sb);
                sb.Append("(");
                bool add_comma = false;
                foreach (var arg in args)
                {
                    if (add_comma)
                        sb.Append(",");
                    add_comma = true;
                    arg.ToCS(sb);
                }
                sb.Append(")");
            }
        }

        public void ToC(StringBuilder sb)
        {
            if (id.name == "ite")
            {
                sb.Append("(");
                args[0].ToC(sb);
                sb.Append(" ? ");
                args[1].ToC(sb);
                sb.Append(" : ");
                args[2].ToC(sb);
                sb.Append(")");
            }
            else
            {
                id.ToC(sb);
                sb.Append("(");
                bool add_comma = false;
                foreach (var arg in args)
                {
                    if (add_comma)
                        sb.Append(",");
                    add_comma = true;
                    arg.ToC(sb);
                }
                sb.Append(")");
            }
        }

        internal static bool IsDefined(string f)
        {
            //bool ok = (f == "ite" || f == "dec" || f == "hex" || f == "Bits" || f == "BitExtract" || f == "IsLowSurrogate"
            //    || f == "IsHighSurrogate" || f == "IsWhitespace" || f == "IsWordletter" || f == "IsDecimalDigitNumber" || f == "UnicodeCodePoint" || f == "long");
            //return ok;
            return true;
        }
    }

    public enum ElseReplaceHow { Delete, Keep, Error }

    public class replace : functioncall
    {
        public replace(ident id, params expr[] cases) : base(id, cases) { }
        public replace(ident id, IEnumerable<expr> cases) : base(id, cases) { }

        public ElseReplaceHow ElseKind
        {
            get
            {
                if (id.name.EndsWith("!"))
                    return ElseReplaceHow.Error;
                else if (id.name.EndsWith("*"))
                    return ElseReplaceHow.Delete;
                else
                    return ElseReplaceHow.Keep;

            }
        }

        /// <summary>
        /// Number of normal cases
        /// </summary>
        public int CaseCount
        {
            get
            {
                if (GetCase(args.Count - 1).IsElse)
                    return args.Count - 1;
                else
                    return args.Count;
            }
        }

        public bool HasElseCase
        {
            get
            {
                return GetCase(args.Count - 1).IsElse;
            }
        }

        /// <summary>
        /// Gets the else case output. 
        /// Only defined when there is an else case.
        /// </summary>
        public expr ElseOutput
        {
            get 
            {
                if (GetCase(args.Count - 1).IsElse)
                    return GetCase(args.Count - 1).Output;
                else
                    return null;
            }
        }

        /// <summary>
        /// Gets the i'th replace case.
        /// </summary>
        /// <param name="i">number between 0 and CaseCount-1</param>
        public replacecase GetCase(int i)
        {
            return (replacecase)args[i];
        }
    }

    public class replacecase : functioncall
    {
        public replacecase(ident id, params expr[] args) : base(id, args) { }

        public bool IsElse
        {
            get { return (args[0] == null); }
        }

        public strconst Pattern
        {
            get { return (strconst)args[0]; }
        }
        public expr Output
        {
            get { return args[1]; }
        }

        public override string ToString()
        {
            if (IsElse)
                return "else ==> " + args[1].ToString() + ";";
            else
                return Pattern.ToString() + " ==> " + args[1].ToString() + ";";
        }
    }

    public class boolconst : expr
    {
        int line;
        int pos;

        public int Line
        {
            get { return line; }
        }

        public int Pos
        {
            get { return pos; }
        }

        public boolconst(int line, int pos, bool val)
        {
            this.line = line;
            this.pos = pos;
            this.val = val;
        }

        public boolconst(bool val)
            : this(0, 0, val)
        {
        }

        public bool val;

        public override string ToString()
        {
            return (val ? "true" : "false");
        }

        public void GenerateCode(string language, StringBuilder sb)
        {
            sb.Append(val ? "true" : "false");
        }

        public void ToJS(StringBuilder sb)
        {
            sb.Append(val ? "true" : "false");
        }

        public void ToCS(StringBuilder sb)
        {
            sb.Append(val ? "true" : "false");
        }

        public void ToC(StringBuilder sb)
        {
            sb.Append(val ? "true" : "false");
        }

        public IEnumerable<ident> GetFunctionNames()
        {
            yield break;
        }

        public IEnumerable<ident> GetBoundVars()
        {
            yield break;
        }
    }

    public interface stmt : IEmitCode { }

    public class returnstmt : stmt
    {
        public returnstmt(expr val)
        {
            this.val = val;
        }

        [Child]
        [AllowBekTypes(BekTypes.STR, BekTypes.ANY)]
        public expr val;

        public override string ToString()
        {
            return "return " + val.ToString() + ";";
        }

        public void GenerateCode(string language, StringBuilder sb)
        {
            sb.Append("return ");
            val.GenerateCode(language, sb);
            sb.AppendLine(";");
        }

        public void ToJS(StringBuilder sb)
        {
            sb.Append("return ");
            val.ToJS(sb);
            sb.AppendLine(";");
        }

        public void ToCS(StringBuilder sb)
        {
            sb.Append("return ");
            val.ToCS(sb);
            sb.AppendLine(";");
        }

        public void ToC(StringBuilder sb)
        {
            sb.Append("return ");
            val.ToC(sb);
            sb.AppendLine(";");
        }

        public IEnumerable<ident> GetFunctionNames()
        {
            return val.GetFunctionNames();
        }

        public IEnumerable<ident> GetBoundVars()
        {
            return val.GetBoundVars();
        }

    }

    public class iterinit : IEmitCode 
    {
        public iterinit()
        {
            assgns = new List<iterassgn>();
        }

        [ChildList]
        public List<iterassgn> assgns;

        public void GenerateCode(string language, StringBuilder sb)
        {
            if (language == "C")
                ToC(sb);
            else if (language == "C#")
                ToCS(sb);
            else
                ToJS(sb);
        }

        public void ToJS(StringBuilder sb)
        {
            foreach (var i in assgns)
            {
                sb.Append("var ");
                i.ToJS(sb);
            }
        }

        public void ToCS(StringBuilder sb)
        {
            foreach (var i in assgns)
            {
                sb.Append("var ");
                i.ToCS(sb);
            }
        }

        public void ToC(StringBuilder sb)
        {
            foreach (var i in assgns)
            {
                sb.Append("int "); //TBD: could possibly also be bool
                i.ToC(sb);
            }
        }

        public IEnumerable<ident> GetFunctionNames()
        {
            foreach (iterassgn a in assgns)
                foreach (var f in a.GetFunctionNames())
                    yield return f;
        }

        public IEnumerable<ident> GetBoundVars()
        {
            foreach (iterassgn a in assgns)
                foreach (var f in a.GetBoundVars())
                    yield return f;
        }
    }

    public class strconst : expr
    {
        int pos;
        int line;

        public int Line
        {
            get { return line; }
        }

        public int Pos
        {
            get { return pos; }
        }

        public string val;
        string inputStr;

        public strconst(string instr) : this(0, 0, instr)
        {
        }

        public strconst(int line, int pos, string instr)
        {
            this.line = line;
            this.pos = pos;

            this.inputStr = instr;
            if (instr.StartsWith("@"))
                val = instr.Substring(2, instr.Length - 3); //remove '@"' from beginning and '"' from end, do not unescape
            else
                val = System.Text.RegularExpressions.Regex.Unescape(instr.Substring(1, instr.Length - 2)); //remove '"' from beginning and '"' from end

            this.content = new List<int>();
            foreach (char c in val)
            {
                this.content.Add((int)c);
            }
        }

        public List<int> content;

        public override string ToString()
        {
            return inputStr;
        }

        public void GenerateCode(string language, StringBuilder sb)
        {
            bool add_comma = false;
            foreach (int c in val)
            {
                if (add_comma)
                    sb.Append(", ");
                add_comma = true;
                string s = string.Format("0x{0:X}", c);
                sb.Append(s);
            }
        }

        public void ToJS(StringBuilder sb)
        {
            bool add_comma = false;
            foreach (int c in val)
            {
                if (add_comma)
                    sb.Append(", ");
                add_comma = true;
                string s = string.Format("0x{0:X}", c);
                sb.Append(s);
            }
        }

        public void ToCS(StringBuilder sb)
        {
            bool add_comma = false;
            foreach (int c in val)
            {
                if (add_comma)
                    sb.Append(", ");
                add_comma = true;
                string s = string.Format("0x{0:X}", c);
                sb.Append(s);
            }
        }

        public void ToC(StringBuilder sb)
        {
            bool add_comma = false;
            foreach (int c in val)
            {
                if (add_comma)
                    sb.Append(", ");
                add_comma = true;
                string s = string.Format("0x{0:X}", c);
                sb.Append(s);
            }
        }


        public IEnumerable<ident> GetFunctionNames()
        {
            yield break;
        }

        public IEnumerable<ident> GetBoundVars()
        {
            yield break;
        }
    }

    public class iterexpr : expr
    {

        public int Line
        {
            get { return binder.Line; }
        }

        public int Pos
        {
            get { return binder.Pos; }
        }

        public iterexpr(ident c0, expr src, iterinit init, params itercase[] cont)
        {
            this.binder = c0;
            this.source = src;
            this.initializer = init;
            this.body = new List<itercase>(cont);
        }

        public iterexpr(ident c0, expr src, iterinit init, IEnumerable<itercase> cont)
        {
            this.binder = c0;
            this.source = src;
            this.initializer = init;
            this.body = new List<itercase>(cont);
        }

        [Child]
        public ident binder;

        [Child]
        [AllowBekTypes(BekTypes.STR, BekTypes.ANY)]
        public expr source;

        [Child]
        public iterinit initializer;

        [ChildList]
        public List<itercase> body;

        public IEnumerable<itercase> GetEndCases()
        {
            foreach (var i in body)
                if (i.endcase)
                    yield return i;
        }

        public IEnumerable<itercase> GetNormalCases()
        {
            foreach (var i in body)
                if (!i.endcase)
                    yield return i;
        }

        public void GenerateCode(string language, StringBuilder sb)
        {
            if (language == "JS")
                ToJS(sb);
            else
                throw new NotImplementedException();
        }

        public void ToJS(StringBuilder sb)
        {
            //create an anomymous function with input argument $
            sb.AppendLine("function($){");
            sb.AppendLine("var result = new Array();");
            initializer.ToJS(sb);
            sb.AppendLine("for (var _i = 0; _i < $.length; _i++) {");
            sb.AppendFormat("var ");
            binder.ToJS(sb);
            sb.AppendLine(" = $.charCodeAt(_i);");
            bool else_if = false;
            foreach (var iter in GetNormalCases())
            {
                if (else_if)
                    sb.Append("else ");
                else_if = true;
                iter.ToJS(sb);
            }
            sb.AppendLine("}");
            else_if = false;
            foreach (var iter in GetEndCases())
            {
                if (else_if)
                    sb.Append("else ");
                else_if = true;
                iter.ToJS(sb);
            }
            sb.AppendLine(@"return result.join('');");
            sb.Append("}(");
            source.ToJS(sb);
            sb.Append(")");
        }

        /// <summary>
        /// Appends the iter expression as a statement that returns the final result
        /// </summary>
        internal void ToJS1(StringBuilder sb)
        {
            //create an anomymous function with input argument $
            sb.AppendLine("var result = new Array();");
            StringBuilder sb1 = new StringBuilder();
            source.ToJS(sb1);
            string inp = sb1.ToString();
            initializer.ToJS(sb);
            sb.AppendLine(string.Format("for (var _i = 0; _i < {0}.length; _i++) {{",inp));
            sb.Append("var ");
            binder.ToJS(sb);
            sb.AppendLine(string.Format(" = {0}.charCodeAt(_i);",inp));
            bool else_if = false;
            foreach (var iter in GetNormalCases())
            {
                if (else_if)
                    sb.Append("else ");
                else_if = true;
                iter.ToJS(sb);
            }
            sb.AppendLine("}");
            else_if = false;
            foreach (var iter in GetEndCases())
            {
                if (else_if)
                    sb.Append("else ");
                else_if = true;
                iter.ToJS(sb);
            }
            sb.AppendLine(@"return result.join('');");
        }

        public void ToCS(StringBuilder sb)
        {
            throw new NotImplementedException();
        }

        public void ToC(StringBuilder sb)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ident> GetFunctionNames()
        {
            foreach (var f in source.GetFunctionNames())
                yield return f;

            foreach (var f in initializer.GetFunctionNames())
                yield return f;

            foreach (var i in body)
                foreach (var f in i.GetFunctionNames())
                    yield return f;
        }

        public IEnumerable<ident> GetBoundVars()
        {
            foreach (var f in source.GetBoundVars())
                yield return f;

            foreach (var f in initializer.GetBoundVars())
                yield return f;

            foreach (var i in body)
                foreach (var f in i.GetBoundVars())
                    yield return f;
        }
    }

    public class elemlist : expr 
    {
        public int Line
        {
            get { if (elems.Count > 0) return elems[0].Line; else return 0; }
        }

        public int Pos
        {
            get { if (elems.Count > 0) return elems[0].Pos; else return 0; }
        }

        public List<expr> elems;
        public elemlist(List<expr> elems)
        {
            this.elems = elems;
        }

        public void GenerateCode(string language, StringBuilder sb)
        {
            throw new NotImplementedException();
        }

        public void ToJS(StringBuilder sb)
        {
            throw new NotImplementedException();
        }

        public void ToCS(StringBuilder sb)
        {
            throw new NotImplementedException();
        }

        public void ToC(StringBuilder sb)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ident> GetFunctionNames()
        {
            var seen = new HashSet<string>();
            foreach (var exp in elems)
                foreach (var i in exp.GetFunctionNames())
                {
                    if (seen.Add(i.name))
                        yield return i;
                }
        }

        public IEnumerable<ident> GetBoundVars()
        {
            var seen = new HashSet<string>();
            foreach (var exp in elems)
                foreach (var i in exp.GetBoundVars())
                {
                    if (seen.Add(i.name))
                        yield return i;
                }
        }
    }

    public class itercase : IEmitCode
    {
        public itercase(expr guard, params iterstmt[] cont)
        {
            this.cond = guard;
            this.body = new List<iterstmt>(cont);
        }

        public itercase(expr guard, IEnumerable<iterstmt> cont)
        {
            this.cond = guard;
            this.body = new List<iterstmt>(cont);
        }

        [Child]
        [AllowBekTypes(BekTypes.BOOL, BekTypes.ANY)]
        public expr cond;

        [ChildList]
        public List<iterstmt> body;

        public bool endcase;

        public void GenerateCode(string language, StringBuilder sb)
        {
            if (language == "JS")
                ToJS(sb);
            else
                throw new NotImplementedException();
        }

        public void ToJS(StringBuilder sb)
        {
            sb.Append("if (");
            cond.ToJS(sb);
            sb.AppendLine("){");
            foreach (var i in body)
            {
                i.ToJS(sb);
            }
            sb.AppendLine("}");
        }

        public void ToCS(StringBuilder sb)
        {
            throw new NotImplementedException();
        }

        public void ToC(StringBuilder sb)
        {
            throw new NotImplementedException();
        }


        public IEnumerable<ident> GetFunctionNames()
        {
            foreach (var f in cond.GetFunctionNames())
                yield return f;

            foreach (var b in body)
                foreach (var f in b.GetFunctionNames())
                    yield return f;
        }

        public IEnumerable<ident> GetBoundVars()
        {
            foreach (var f in cond.GetBoundVars())
                yield return f;

            foreach (var b in body)
                foreach (var f in b.GetBoundVars())
                    yield return f;
        }
    }

    public interface iterstmt : IEmitCode { }

    public class iterassgn : iterstmt {
        public iterassgn(ident l, expr r)
        {
            lhs = l;
            rhs = r;
        }

        [Child]
        public ident lhs;

        [Child]
        [AllowBekTypes(BekTypes.BOOL, BekTypes.CHAR, BekTypes.ANY)]
        public expr rhs;

        public override string ToString()
        {
            return string.Format("{0} := {1};", lhs.ToString(), rhs.ToString());
        }

        public IEnumerable<ident> GetFunctionNames()
        {
            return rhs.GetFunctionNames();
        }

        public IEnumerable<ident> GetBoundVars()
        {
            return rhs.GetBoundVars();
        }

        public void GenerateCode(string language, StringBuilder sb)
        {
            if (language == "CS")
                ToCS(sb);
            else if (language == "C")
                ToC(sb);
            else
                ToJS(sb);
        }

        public void ToCS(StringBuilder sb)
        {
            lhs.ToCS(sb);
            sb.Append(" = ");
            rhs.ToCS(sb);
            sb.AppendLine(";");
        }

        public void ToC(StringBuilder sb)
        {
            lhs.ToC(sb);
            sb.Append(" = ");
            rhs.ToC(sb);
            sb.AppendLine(";");
        }

        public void ToJS(StringBuilder sb)
        {
            lhs.ToJS(sb);
            sb.Append(" = ");
            rhs.ToJS(sb);
            sb.AppendLine(";");
        }
    }

    public class yieldstmt : iterstmt
    {
        public yieldstmt(params expr[] cont)
        {
            args = new List<expr>(cont);
        }

        public yieldstmt(IEnumerable<expr> cont)
        {
            args = new List<expr>(cont);
        }

        [ChildList]
        [AllowBekTypes(BekTypes.CHAR, BekTypes.STR, BekTypes.ANY)]
        public List<expr> args;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < args.Count; i++)
            {
                if (i > 0)
                    sb.Append(",");
                sb.Append(args[i].ToString());
            }
            return string.Format("yield({0});", sb.ToString());
        }

        public void ToJS(StringBuilder sb)
        {
            if (args.Count > 0)
            {
                bool put_comma = false;
                foreach (var exp in args)
                {
                    if (!(exp is strconst) || ((exp as strconst).val != ""))
                    {
                        if (!put_comma)
                        {
                            put_comma = true;
                            sb.Append("result.push(String.fromCharCode(");
                        }
                        else
                        {
                            sb.Append(", ");
                        }
                        exp.ToJS(sb);
                    }
                }
                if (put_comma)
                    sb.AppendLine("));");
            }
        }

        public void ToCS(StringBuilder sb)
        {
            if (args.Count > 0)
            {
                sb.Append("{");
                foreach (var exp in args)
                {
                    if (!(exp is strconst) || ((exp as strconst).val != ""))
                    {
                        sb.AppendLine("yield return (char)(");
                        exp.ToCS(sb);
                        sb.AppendLine("); ");
                    }
                }
                sb.Append("}");
            }
        }

        public void ToC(StringBuilder sb)
        {
            if (args.Count > 0)
            {
                sb.Append("{");
                foreach (var exp in args)
                {
                    if (!(exp is strconst) || ((exp as strconst).val != ""))
                    {
                        sb.AppendLine("output[pos++] = ((char)");
                        exp.ToCS(sb);
                        sb.AppendLine("); ");
                    }
                }
                sb.Append("}");
            }
        }

        public IEnumerable<ident> GetFunctionNames()
        {
            foreach (var e in args)
                foreach (var f in e.GetFunctionNames())
                    yield return f;
        }

        public IEnumerable<ident> GetBoundVars()
        {
            foreach (var e in args)
                foreach (var f in e.GetBoundVars())
                    yield return f;
        }


        public void GenerateCode(string language, StringBuilder sb)
        {
            switch (language)
            {
                case "C#": 
                    ToCS(sb); 
                    break;
                case "C": 
                    ToC(sb); 
                    break;
                case "JS": 
                    ToJS(sb);
                    break;
                default:
                    throw new BekException(string.Format("Unexpected language identifier '{0}', expected one of 'C#', 'C', or 'JS'.",language));
            };
        }
    }

    public class raisestmt : iterstmt
    {
        public string exc;
        public raisestmt(string exc)
        {
            this.exc = exc;
        }

        public override string ToString()
        {
            return string.Format("raise {0};", exc);
        }

        public void ToJS(StringBuilder sb)
        {
            sb.Append("throw {name:'");
            sb.Append(this.exc);
            sb.AppendLine("'};");
        }

        public void ToCS(StringBuilder sb)
        {
            sb.Append("throw new Exception(\"");
            sb.Append(this.exc);
            sb.AppendLine("\");");
        }

        public void ToC(StringBuilder sb)
        {
            sb.Append("throw (\"");
            sb.Append(this.exc);
            sb.AppendLine("\");");
        }

        public void GenerateCode(string language, StringBuilder sb)
        {
            switch (language)
            {
                case "C#":
                    ToCS(sb);
                    break;
                case "C":
                    ToC(sb);
                    break;
                case "JS":
                    ToJS(sb);
                    break;
                default:
                    throw new BekException(string.Format("Unexpected language identifier '{0}', expected one of 'C#', 'C', or 'JS'.", language));
            };
        }

        public IEnumerable<ident> GetFunctionNames()
        {
            yield break;
        }

        public IEnumerable<ident> GetBoundVars()
        {
            yield break;
        }
    }

    public class ifthenelse : iterstmt
    {
        public expr cond;
        public List<iterstmt> tcase;
        public List<iterstmt> fcase;

        public ifthenelse(expr cond, List<iterstmt> tcase, List<iterstmt> fcase)
        {
            this.cond = cond;
            this.tcase = tcase;
            this.fcase = fcase;
        }

        public override string ToString()
        {
            var tcaseStr = "";
            foreach (var i in tcase)
                tcaseStr += i.ToString();
            var fcaseStr = "";
            foreach (var i in fcase)
                fcaseStr += i.ToString();
            return string.Format("if (" + cond.ToString() + ")" + "{" + tcaseStr + "} else {" + fcaseStr + "};");
        }

        //the are guaranteed at least two elements the last key is null
        static public ifthenelse Mk(List<KeyValuePair<expr, List<iterstmt>>> pairs)
        {
            List<iterstmt> fcase = pairs[pairs.Count - 1].Value;
            for (int i = pairs.Count - 2; i > 0; i--)
            {
                var fcase_new = new List<iterstmt>();
                fcase_new.Add(new ifthenelse(pairs[i].Key, pairs[i].Value, fcase));
                fcase = fcase_new;
            }
            ifthenelse ite = new ifthenelse(pairs[0].Key, pairs[0].Value, fcase);
            return ite;
        }

        public void GenerateCode(string language, StringBuilder sb)
        {
            switch (language)
            {
                case "C#":
                    ToCS(sb);
                    break;
                case "C":
                    ToC(sb);
                    break;
                case "JS":
                    ToJS(sb);
                    break;
                default:
                    throw new BekException(string.Format("Unexpected language identifier '{0}', expected one of 'C#', 'C', or 'JS'.", language));
            };
        }

        public void ToJS(StringBuilder sb)
        {
            sb.Append("if (");
            cond.ToJS(sb);
            sb.AppendLine("){");
            foreach (var i in tcase)
            {
                i.ToJS(sb);
            }
            sb.AppendLine("}");
            if (fcase.Count > 0)
            {
                sb.AppendLine("else {");
                foreach (var i in fcase)
                {
                    i.ToJS(sb);
                }
                sb.AppendLine("}");
            }
        }

        public void ToCS(StringBuilder sb)
        {
            sb.Append("if (");
            cond.ToCS(sb);
            sb.AppendLine("){");
            foreach (var i in tcase)
            {
                i.ToCS(sb);
            }
            sb.AppendLine("}");
            if (fcase.Count > 0)
            {
                sb.AppendLine("else {");
                foreach (var i in fcase)
                {
                    i.ToCS(sb);
                }
                sb.AppendLine("}");
            }
        }

        public void ToC(StringBuilder sb)
        {
            sb.Append("if (");
            cond.ToC(sb);
            sb.AppendLine("){");
            foreach (var i in tcase)
            {
                i.ToC(sb);
            }
            sb.AppendLine("}");
            if (fcase.Count > 0)
            {
                sb.AppendLine("else {");
                foreach (var i in fcase)
                {
                    i.ToC(sb);
                }
                sb.AppendLine("}");
            }
        }

        public IEnumerable<ident> GetFunctionNames()
        {
            foreach (var f in cond.GetFunctionNames())
                yield return f;

            foreach (var i in tcase)
                foreach (var f in i.GetFunctionNames())
                    yield return f;

            foreach (var i in fcase)
                foreach (var f in i.GetFunctionNames())
                    yield return f;
        }

        public IEnumerable<ident> GetBoundVars()
        {
            foreach (var f in cond.GetBoundVars())
                yield return f;

            foreach (var i in tcase)
                foreach (var f in i.GetBoundVars())
                    yield return f;

            foreach (var i in fcase)
                foreach (var f in i.GetBoundVars())
                    yield return f;
        }
    }
}

