using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Z3;
using Microsoft.Automata.Z3.Internal;
using Microsoft.Bek.Frontend.Meta;
using Microsoft.Bek.Frontend.AST;
using Microsoft.Automata;
using Microsoft.Automata.Z3;

using STBuilderZ3 = Microsoft.Automata.STBuilder<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;


namespace Microsoft.Bek.Model.Converters.Z3
{
    internal class ValueExpr : AValueExpr<Expr>
    {

        public ValueExpr(STBuilderZ3 stb, Sort charsort, List<Bek.Frontend.AST.BekLocalFunction> funcs)
            : base()
        {
            this.stb = stb;
            this.solver = stb.Solver;
            this.charsort = charsort;
            this.funcs = funcs;
            //this.library = new Library(stb.Solver, charsort, true);
            //this.library.AddLocalFunctions(this, funcs);
        }

        List<Bek.Frontend.AST.BekLocalFunction> funcs;
        //internal Library library;
        internal STBuilderZ3 stb;
        internal IContext<FuncDecl, Expr, Sort> solver;
        internal Sort charsort;

        //public override Library Library
        //{
        //    get { return library; }
        //}

        override protected Expr MkChar(int val)
        {
            return solver.MkNumeral(val, charsort);
        }

        override protected Expr MkBool(bool val)
        {
            return (val ? solver.True : solver.False);
        }

        string GetString(Expr t)
        {
            Expr[] args = solver.GetAppArgs(t);
            if (args.Length == 0)
                return "";
            char c = solver.GetCharValue(args[0]);
            string rest = GetString(args[1]);
            return c + rest;
        }

        protected override Expr MkFunctionCall(ident id, params Expr[] args)
        {
            if (id.name == "string")  //construct a string from fixed characters
                return  (args.Length > 0 ? solver.MkList(args) : solver.MkListFromString("", solver.CharSort));

            if (id.name == "in") //RHS is a regular expression
            {
                if (args.Length != 2)
                    throw new BekParseException(id.Line, id.Pos, "Wrong number of arguments");

                string regex = "^(" + GetString(args[1]) + ")$";
                Automaton<Expr> aut = solver.RegexConverter.Convert(regex).Determinize().Minimize();
                if (aut.StateCount != 2 || aut.MoveCount != 1)
                    throw new BekParseException(id.Line, id.Pos, "The rhs must be a regex character class or regex matching strings of length 1 (anchors are implicit)");

                Expr pred = aut.GetMoveFrom(aut.InitialState).Label;

                Expr predInst = solver.ApplySubstitution(pred, solver.MkCharVar(0), args[0]);
                return predInst;
            }


            return solver.Library.ApplyFunction(id.name, args);

            //var func = funcs.Find(fn => fn.id.name.Equals(name.name));
            //if (func != null)
            //    return library.LocalFunction(name, args);

            //try
            //{
            //    switch (name.name)
            //    {
            //        case ("ite"):
            //            return MkIte(name, args);
            //        case ("dec"):
            //        case ("FromDecimals"):
            //            return FromDecimals(args);
            //        case ("IsHighSurrogate"):
            //            return library.IsHighSurrogate(args);
            //        case ("IsLowSurrogate"):
            //            return library.IsLowSurrogate(args);
            //        case ("hex"):
            //        case ("HexEnc"):
            //            return library.HexEnc(args);
            //        case ("Bits"):
            //        case ("BitExtract"):
            //            return library.BitExtr(args);
            //        case ("InCssSafeList"):
            //            return library.InCssSafeList(args);
            //        case ("UnicodeCodePoint"):
            //        case ("CssCombinedCodePoint"):
            //            return library.UnicodeCodePoint(args);
            //        case ("long"):
            //            return MkLong(args);
            //        case ("string"):
            //            if (args.Length == 0)
            //                return solver.MkListFromString("", solver.CharSort);
            //            else
            //                return solver.MkList(args);
            //        default:
            //            if (name.name.StartsWith("hex"))
            //            {
            //                string rest = name.name.Substring(3);
            //                int k;
            //                if (!int.TryParse(rest, out k) || args.Length != 1)
            //                    throw new BekParseException(name.line, name.pos, string.Format("Unknown function '{0}'", name.name));
            //                return library.HexEnc(k, args[0]);
            //            }
            //            else
            //            {
            //                throw new BekParseException(name.line, name.pos, string.Format("Unknown function '{0}'", name.name));
            //            }
            //    }
            //}
            //catch (BekParseException e)
            //{
            //    throw e;
            //}
            //catch (Exception e)
            //{
            //    throw new BekParseException(name.line, name.pos, e.Message);
            //}
        }

        //Expr FromDecimals(Expr[] args)
        //{
        //    if (args.Length == 0)
        //        throw new BekException(string.Format("Invalid nr of arguments"));

        //    if (Array.Exists(args, a => !solver.GetSort(a).Equals(charsort)))
        //        throw new BekException("Invalid argument types");

        //    Expr res = solver.MkCharLSHB(args[0]);
        //    Expr _10 = solver.MkNumeral(10, charsort);
        //    for (int i = 1; i < args.Length; i++)
        //    {
        //        res = solver.MkCharAdd(solver.MkCharMul(_10, res), solver.MkCharLSHB(args[i]));
        //    }
        //    return res;
        //}
    }
}