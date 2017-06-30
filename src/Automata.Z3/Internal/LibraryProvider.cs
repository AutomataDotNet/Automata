using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Z3;

namespace Microsoft.Automata.Z3.Internal
{

    internal class LibraryProvider : ILibrary<Expr>
    {
        Z3Provider z3p;
        Dictionary<string, Tuple<FuncDecl, Expr>> library;
        HashSet<string> reserved = new HashSet<string>(new string[] { "ite", "Bits", "bits" });

        internal LibraryProvider(Z3Provider z3p)
        {
            this.z3p = z3p;
            library = new Dictionary<string, Tuple<FuncDecl, Expr>>();
        }

        Expr MkApp(string f, params Expr[] args)
        {
            switch (f)
            {
                //arithmetic
                case "+": return z3p.MkCharAdd(args[0], args[1]);
                case "-": return z3p.MkCharSub(args[0], args[1]);
                case "*": return z3p.MkCharMul(args[0], args[1]);
                case "<": return z3p.MkCharLt(args[0], args[1]);
                case ">": return z3p.MkCharGt(args[0], args[1]);
                case "<=": return z3p.MkCharLe(args[0], args[1]);
                case ">=": return z3p.MkCharGe(args[0], args[1]);
                case "/": return z3p.MkCharDiv(args[0], args[1]);
                case "%": return z3p.MkCharRem(args[0], args[1]);
                case "|": return z3p.MkBvOr(args[0], args[1]);
                case "&": return z3p.MkBvAnd(args[0], args[1]);
                case "~": return z3p.z3.MkBVNot((BitVecExpr)args[0]);
                case "^": return z3p.z3.MkBVXOR((BitVecExpr)args[0], (BitVecExpr)args[1]);
                case ">>": return z3p.z3.MkBVLSHR((BitVecExpr)args[0], (BitVecExpr)args[1]);
                case "<<": return z3p.z3.MkBVSHL((BitVecExpr)args[0], (BitVecExpr)args[1]);
                //logical
                case "==": return z3p.MkEq(args[0], args[1]);
                case "!=": return z3p.MkNot(z3p.MkEq(args[0], args[1]));
                case "&&": return z3p.MkAnd(args[0], args[1]);
                case "||": return z3p.MkOr(args[0], args[1]);
                case "!": return z3p.MkNot(args[0]);
                //ite
                case "ite": return z3p.MkIte(args[0], args[1], args[2]);
                //misc
                case "bits":
                case "Bits": return MkBits((int)z3p.GetNumeralUInt(args[0]), (int)z3p.GetNumeralUInt(args[1]), args[2]);
                default:
                    {
                        Tuple<FuncDecl, Expr> func;
                        if (!library.TryGetValue(f, out func))
                            throw new ArgumentException(string.Format("undefined function '{0}'", f));
                        return z3p.MkApp(func.Item1, args);
                    }
            }
        }

        private Expr MkBits(int u, int l, Expr term)
        {
            string f = string.Format("Bits_{0}_{1}", u, l);
            Tuple<FuncDecl, Expr> def;
            if (library.TryGetValue(f, out def))
                return z3p.MkApp(def.Item1, term);

            var x = z3p.MkVar(0, z3p.CharSort);
            var body = z3p.MkBvAnd(z3p.MkBvShiftRight((uint)l, x), z3p.MkNumeral(MkMask(u, l), z3p.CharSort));
            DefineFunction(f, body, x);

            var func = library[f].Item1;
            var res = z3p.MkApp(func, term);
            return res;
        }

        static int Bits(int m, int n, int c) { return ((c >> n) & MkMask(m, n)); }

        internal static int MkMask(int upper, int lower)
        {
            return ~(-2 << (upper - lower));
        }

        public void DefineFunction(string name, Expr body, params Expr[] vars)
        {
            if (reserved.Contains(name) || library.ContainsKey(name))
                throw new ArgumentException(string.Format("'{0}' is already defined", name));

            foreach (var v in z3p.GetVars(body))
                if (!Array.Exists(vars, v.Equals))
                    throw new ArgumentException(string.Format("variable '{0}' in the definition of '{1}' is unbound", v, name));

            try
            {
                Sort[] domain = Array.ConvertAll(vars, z3p.GetSort);
                Sort range = z3p.GetSort(body);
                FuncDecl func = z3p.MkFuncDecl(name, domain, range);
                library[name] = new Tuple<FuncDecl, Expr>(func, body);
                z3p.AssertAxiom(z3p.MkApp(func, vars), body, vars);
            }
            catch (Exception e)
            {
                throw new ArgumentException(string.Format("definition of '{0}' failed", name), e);
            }
        }

        public Expr ApplyFunction(string name, params Expr[] args)
        {
            try
            {
                return MkApp(name, args);
            }
            catch (Exception e)
            {
                throw new ArgumentException(string.Format("application of '{0}' failed", name), e);
            }
        }

        /// <summary>
        /// Generate static methods for all of the user defined functions in the library.
        /// The generated definitions are arithmetic expressions.
        /// </summary>
        /// <param name="language">target language must be one of 'C#', 'C', or 'JS'</param>
        /// <param name="sb">generated code goes here</param>
        public void GenerateCode(string language, StringBuilder sb)
        {
            if (!(language == "C#" || language == "C" || language == "JS"))
                throw new ArgumentException(string.Format("{0} code generation is undefined, language must me one of 'C#', 'C', or 'JS'.", language), "language");
            try
            {
                foreach (var entry in library)
                {
                    var dom = z3p.GetDomain(entry.Value.Item1);

                    if (language == "JS")
                    {
                        sb.Append("function ");
                        sb.Append(entry.Key);
                        sb.Append("(");
                        for (int i = 0; i < dom.Length; i++)
                        {
                            if (i > 0)
                                sb.Append(",");
                            sb.Append("_");
                            sb.Append(i);
                        }
                    }
                    else
                    {
                        if (language == "C#")
                            sb.Append("static ");

                        if (z3p.GetRange(entry.Value.Item1).Equals(z3p.BoolSort))
                            sb.Append("bool ");
                        else
                            sb.Append("int ");

                        sb.Append(entry.Key);


                        sb.Append("(");
                        for (int i = 0; i < dom.Length; i++)
                        {
                            if (i > 0)
                                sb.Append(", ");

                            sb.Append(dom[i].Equals(z3p.BoolSort) ? "bool " : "int ");
                            sb.Append("_" + i);
                        }
                    }

                    sb.Append("){return ");
                    string body = z3p.PrettyPrintCS(entry.Value.Item2, x => ("_" + z3p.GetVarIndex(x)));
                    sb.Append(body);
                    sb.Append(";");
                    sb.AppendLine("}");
                }
            }
            catch (Exception e)
            {
                throw new ArgumentException(string.Format("{0} code generation failed", language), e);
            }
        }
    }
}
