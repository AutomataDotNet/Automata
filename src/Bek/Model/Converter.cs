using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Bek.Frontend;

using Microsoft.Bek.Model.Converters;
using Microsoft.Automata.Z3;
using Microsoft.Bek.Frontend.Meta;
using Microsoft.Automata;
using Microsoft.Z3;

using Microsoft.Bek.Frontend.AST;

using SFAModel = Microsoft.Automata.SFA<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;
using STBuilderZ3 = Microsoft.Automata.STBuilder<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;
using STModel = Microsoft.Automata.ST<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;
using STbModel = Microsoft.Automata.STb<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;


namespace Microsoft.Bek.Model
{
    /// <summary>
    /// Interface for converting a bek program to model MODEL.
    /// </summary>
    /// <typeparam name="MODEL">type corresponding to some representation of a bek program</typeparam>
    public interface IConverter<MODEL> : IDisposable
    {
        MODEL Convert(BekProgram p);
        ILibrary<Expr> Library { get; }
    }

    /// <summary>
    /// Implements methods for converting from bek programs to STs.
    /// </summary>
    public static class BekConverter
    {
        /// <summary>
        /// Makes a converter from Bek programs to STbs
        /// </summary>
        /// <param name="solver">given solver</param>
        public static IConverter<STbModel> MkBekToSTbConverter(IContext<FuncDecl, Expr, Sort> solver, List<Bek.Frontend.AST.BekLocalFunction> funcs, string name)
        {
            //if (solver.Encoding != BitWidth.BV16)
            //{
            //    throw new ModelException("Modes other than UTF16 are not supported.");
            //}

            var conv = new exprConverter(solver);

            STBuilderZ3 stb = new STBuilderZ3(solver);
            foreach (var bekfunc in funcs)
            {
                var varInfo = new Dictionary<string, Expr>();
                Expr[] vars = new Expr[bekfunc.args.Length];
                for (int i = 0; i < bekfunc.args.Length; i++)
                {
                    var v = solver.MkVar((uint)i, solver.CharSort);
                    varInfo[bekfunc.args[i].name] = solver.MkVar((uint)i, solver.CharSort);
                    vars[i] = v;
                }

                Expr body = conv.ConvertToExpr(bekfunc.body, varInfo);

                solver.Library.DefineFunction(bekfunc.id.name, body, vars);
            }

            var charsort = solver.CharSort;

            var ire = new Converters.Z3.ValueExpr(stb, charsort, funcs);
            var ie = new Converters.Z3.IterExpr2STb(ire, stb, charsort);
            var se = new Converters.Z3.StrExpr2STb(ie, stb, charsort);
            var bp = new Converters.Z3.BekProg2STb(se, stb, charsort, name);

            return bp;
        }

        internal class exprConverter
        {
            IContext<FuncDecl,Expr,Sort> smt;
            internal exprConverter(IContext<FuncDecl, Expr, Sort> smt)
            {
                this.smt = smt;
            }

            internal Expr ConvertToExpr(expr e, Dictionary<string, Expr> varInfo)
            {
                if (e is boolconst)
                    return (((boolconst)e).val ? smt.True : smt.False);
                else if (e is charconst)
                    return smt.MkNumeral(((charconst)e).val, smt.CharSort);
                else if (e is ident)
                {
                    ident i = (ident)e;
                    if (!varInfo.ContainsKey(i.name))
                        throw new BekParseException(i.line, i.pos, string.Format("undefined '{0}'", i.name));
                    return varInfo[i.name];
                }
                else if (e is strconst)
                    return smt.MkListFromString(((strconst)e).val, smt.CharSort);
                else
                {
                    functioncall f = e as functioncall; //must be != null
                    if (f == null)
                        throw new BekException("Internal error, unexpected bek expression '{0}'");
                    try
                    {
                        Expr[] args = Array.ConvertAll(f.args.ToArray(), t => ConvertToExpr(t, varInfo));
                        var res = smt.Library.ApplyFunction(f.id.name, args);
                        return res;
                    }
                    catch (BekParseException bek_error)
                    {
                        throw bek_error;
                    }
                    catch (Exception inner)
                    {
                        //add the line info
                        throw new BekParseException(f.id.line, f.id.pos, inner);
                    }
                }
            }
        }

        /// <summary>
        /// Converts a given bek program (given as a string) to an ST by using the given solver.
        /// </summary>
        /// <param name="solver">given solver</param>
        /// <param name="bek">given bek program</param>
        /// <returns>ST encoding of the bek program</returns>
        public static STModel BekToST(IContext<FuncDecl, Expr, Sort> solver, string bek)
        {
            var pgm = BekParser.BekFromString(bek);
            //use the STb converter so that exceptions and ite rules are supported

            var conv = BekConverter.MkBekToSTbConverter(solver, pgm.ast.funcs, pgm.ast.name);
            var stb = conv.Convert(pgm);
            return stb.ToST();
        }

        /// <summary>
        /// Converts a given bek program (given as a string) to an STb by using the given solver.
        /// </summary>
        /// <param name="solver">given solver</param>
        /// <param name="bek">given bek program as a string</param>
        /// <returns>STb encoding of the bek program</returns>
        public static STbModel BekToSTb(IContext<FuncDecl, Expr, Sort> solver, string bek)
        {
            var pgm = BekParser.BekFromString(bek);
            var conv = BekConverter.MkBekToSTbConverter(solver, pgm.ast.funcs, pgm.ast.name);
            var st = conv.Convert(pgm);
            return st;
        }


        /// <summary>
        /// Converts a given bek program to an STb by using the given solver.
        /// </summary>
        /// <param name="solver">given solver</param>
        /// <param name="pgm">given bek program</param>
        /// <returns>STb encoding of the bek program</returns>
        public static STbModel BekToSTb(IContext<FuncDecl, Expr, Sort> solver, BekProgram pgm)
        {
            var conv = BekConverter.MkBekToSTbConverter(solver, pgm.ast.funcs, pgm.ast.name);
            var st = conv.Convert(pgm);
            return st;
        }

        /// <summary>
        /// Converts a given bek program (from the given file) to an ST by using the given solver.
        /// </summary>
        /// <param name="solver">given solver</param>
        /// <param name="file">given bek program file</param>
        /// <returns>ST encoding of the bek program</returns>
        public static STModel BekFileToST(IContext<FuncDecl, Expr, Sort> solver, string file)
        {
            var sw = new System.IO.StreamReader(file);
            string bek = sw.ReadToEnd();
            sw.Close();
            return BekToST(solver, bek);
        }

        /// <summary>
        /// Converts a given bek program (from the given file) to an STb by using the given solver.
        /// </summary>
        /// <param name="solver">given solver</param>
        /// <param name="file">given bek program file</param>
        /// <returns>STb encoding of the bek program</returns>
        public static STbModel BekFileToSTb(IContext<FuncDecl, Expr, Sort> solver, string file)
        {
            var sw = new System.IO.StreamReader(file);
            string bek = sw.ReadToEnd();
            sw.Close();
            return BekToSTb(solver, bek);
        }

        public static BekProgram BekFileToBekProgram(string file)
        {
            var sw = new System.IO.StreamReader(file);
            string bek = sw.ReadToEnd();
            sw.Close();       
            return BekParser.BekFromString(bek); 
        }

        /// <summary>
        /// Converts a given bek program to an ST by using the given solver.
        /// </summary>
        /// <param name="solver">given solver</param>
        /// <param name="bek">given bek program</param>
        /// <returns>ST encoding of the bek program</returns>
        public static STModel BekToST(IContext<FuncDecl, Expr, Sort> solver, BekProgram bek)
        {
            //use the STb converter to support ite and exception rules
            var conv = BekConverter.MkBekToSTbConverter(solver, bek.ast.funcs, bek.ast.name);
            var st = conv.Convert(bek);
            return st.ToST(); //flatten the STb rules
        }
    }
}
