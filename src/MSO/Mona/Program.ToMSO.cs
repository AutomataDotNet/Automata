using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Automata.MSO.Mona
{
    public partial class MonaProgram
    {
        private Variable V1(string name)
        {
            return new Variable(name, true);
        }

        private Variable V2(string name)
        {
            return new Variable(name, false);
        }

        Dictionary<string, MSOFormula<BDD>> predMap = new Dictionary<string, MSOFormula<BDD>>();

        public MSOFormula<BDD> ToMSO()
        {
            if (header == MonaHeader.M2LTREE || header == MonaHeader.WS2S)
                throw new NotSupportedException("unsopprted header " + header);

            foreach (var d in declarations)
            {
                if (d.kind != Mona.MonaDeclKind.formula
                    && d.kind != Mona.MonaDeclKind.var1
                    && d.kind != Mona.MonaDeclKind.var2
                    && d.kind != Mona.MonaDeclKind.constant
                    && d.kind != Mona.MonaDeclKind.pred
                    && d.kind != Mona.MonaDeclKind.macro)
                    throw new NotImplementedException("declaration: " + d.ToString());
                MonaVarDecl vd = d as MonaVarDecl;
                if (vd != null && (vd.univs != null || vd.varwhere.where != null))
                    throw new NotImplementedException("var declaration (with universe or where condition): " + vd.ToString());
            }

            int fvcount = vars1.Count + vars2.Count;

            MSOFormula<BDD> psi = null;

            foreach (var decl in declarations)
                if (decl.kind == MonaDeclKind.formula)
                {
                    var phi = ConvertFormula(((MonaFormulaDecl)decl).formula, MapStack<string,MonaParam>.Empty);
                    if (psi == null)
                        psi = phi;
                    else
                        psi = psi & phi;
                }
                else if (decl.kind == MonaDeclKind.pred || decl.kind == MonaDeclKind.macro)
                {
                    var pd = decl as MonaPredDecl;
                    predMap[pd.name.text] = ConvertFormula(pd.formula, MapStack<string, MonaParam>.Empty.Push(pd.pmap));
                }

            if (psi == null)
                throw new ArgumentException("formula is missing from the mona program");

            return psi;
        }

        private MSOFormula<BDD> ConvertFormula(MonaExpr expr, MapStack<string,MonaParam> locals)
        {
            switch (expr.symbol.Kind)
            {
                case Tokens.TRUE:
                    return new MSOTrue<BDD>();
                case Tokens.FALSE:
                    return new MSOFalse<BDD>();
                case Tokens.NOT:
                    return new MSONot<BDD>(ConvertFormula(expr[0], locals));
                case Tokens.AND:
                    return new MSOAnd<BDD>(ConvertFormula(expr[0], locals), ConvertFormula(expr[1], locals));
                case Tokens.OR:
                    return new MSOOr<BDD>(ConvertFormula(expr[0], locals), ConvertFormula(expr[1], locals));
                case Tokens.IMPLIES:
                    return new MSOImplies<BDD>(ConvertFormula(expr[0], locals), ConvertFormula(expr[1], locals));
                case Tokens.EQUIV:
                    return new MSOEquiv<BDD>(ConvertFormula(expr[0], locals), ConvertFormula(expr[1], locals));
                case Tokens.EQ:
                    return ConvertEq(expr[0], expr[1], locals);
                case Tokens.NE:
                    return new MSONot<BDD>(ConvertEq(expr[0], expr[1], locals));
                case Tokens.LT:
                    return ConvertLt(expr[0], expr[1], locals);
                case Tokens.GT:
                    return ConvertLt(expr[1], expr[0], locals);
                case Tokens.LE:
                    return ConvertLe(expr[0], expr[1], locals);
                case Tokens.GE:
                    return ConvertLe(expr[1], expr[0], locals);
                case Tokens.SUBSET:
                    return ConvertSubset(expr[1], expr[0], locals);
                case Tokens.IN:
                    return ConvertIn(expr[0], expr[1], locals);
                case Tokens.NOTIN:
                    return new MSONot<BDD>(ConvertIn(expr[0], expr[1], locals));
                case Tokens.EMPTY:
                    return ConvertIsEmpty(expr[0], locals);
                case Tokens.EX1:
                case Tokens.EX2:
                    {
                        MonaQFormula phi = (MonaQFormula)expr;
                        if ((phi.universes != null && phi.universes.Count > 1) ||
                            phi.vars.Exists(vw => vw.where != null))
                            throw new NotImplementedException(expr.ToString());
                        MSOFormula<BDD> psi = ConvertFormula(phi.formula, locals.Push(phi.varmap));
                        foreach (var vw in phi.vars)
                            psi = new MSOExists<BDD>(new Variable(vw.name, phi.varmap[vw.name].type == MonaExprType.INT), psi);
                        return psi;
                    }
                case Tokens.ALL1:
                case Tokens.ALL2:
                    {
                        MonaQFormula phi = (MonaQFormula)expr;
                        if ((phi.universes != null && phi.universes.Count > 1) ||
                            phi.vars.Exists(vw => vw.where != null))
                            throw new NotImplementedException(expr.ToString());
                        MSOFormula<BDD> psi = ConvertFormula(phi.formula, locals.Push(phi.varmap));
                        foreach (var vw in phi.vars)
                            psi = new MSOForall<BDD>(new Variable(vw.name, phi.varmap[vw.name].type == MonaExprType.INT), psi);
                        return psi;
                    }
                case Tokens.NAME:
                    {
                        var name = expr as MonaName;
                        if (name != null)
                        {
                            //must be a nullary predicate application (var0 is not supported)
                            var tmpPredApp = new MonaPredApp(name.symbol, Cons<MonaExpr>.Empty);
                            return ConvertPredApp(tmpPredApp, locals);
                        }
              
                        var predApp = expr as MonaPredApp;
                        if (predApp != null)
                        {
                            return ConvertPredApp(predApp, locals);
                        }

                        throw new NotImplementedException(expr.ToString());
                    }
                default:
                    throw new NotImplementedException(expr.ToString());
            }
        }

        private MSOFormula<BDD> ConvertPredApp(MonaPredApp predApp, MapStack<string, MonaParam> locals)
        {
            var predDef = predMap[predApp.symbol.text];
            var predDecl = (MonaPredDecl)globals[predApp.symbol.text];

            int k = predDecl.parameters.Count;
            if (k != predApp.NrOfSubexprs)
                throw new ArgumentException("invalid call of " + predDecl.name);

            if (k == 0)
                return predDef;

            var newVars = new Variable[k];
            Dictionary<string, Variable> substitution = new Dictionary<string, Variable>();

            var argPreds = new MSOFormula<BDD>[k];
            var argVars = new Variable[k];

            for (int i = 0; i < k; i++)
            {
                if (predDecl.parameters[i].kind != MonaParamKind.var1 && predDecl.parameters[i].kind != MonaParamKind.var2)
                    throw new NotImplementedException("parameter kind " + predDecl.parameters[i].kind.ToString());

                MSOFormula<BDD> argPreds_i;
                Variable argVars_i;
                if (predDecl.parameters[i].kind == MonaParamKind.var1)
                {
                    argVars_i = ConvertTerm1(predApp[i], locals, out argPreds_i);
                    if (argPreds_i == null)
                    {
                        var tmp1 = MkNewVar1();
                        argPreds_i = new MSOEq<BDD>(tmp1, argVars_i);
                        argVars_i = tmp1;
                    }
                }
                else
                {
                    argVars_i = ConvertTerm2(predApp[i], locals, out argPreds_i);
                    if (argPreds_i == null)
                    {
                        var tmp2 = MkNewVar2();
                        argPreds_i = new MSOEq<BDD>(tmp2, argVars_i);
                        argVars_i = tmp2;
                    }
                }
                argPreds[i] = argPreds_i;
                argVars[i] = argVars_i;
                substitution[predDecl.parameters[i].Token.text] = argVars_i;
            }

            MSOFormula<BDD> psi = predDef.SubstituteVariables(substitution);
            for (int i = k - 1; i >= 0; i--)
                psi = new MSOExists<BDD>(argVars[i], argPreds[i] & psi);

            return psi;
        }

        private MSOFormula<BDD> ConvertIsEmpty(MonaExpr set, MapStack<string, MonaParam> locals)
        {
            MSOFormula<BDD> psi_X;
            Variable X = ConvertTerm2(set, locals, out psi_X);
            MSOFormula<BDD> isempty = new MSOIsEmpty<BDD>(X);
            if (psi_X != null)
                isempty = new MSOExists<BDD>(X, new MSOAnd<BDD>(psi_X, isempty));
            return isempty;
        }

        private MSOFormula<BDD> ConvertEq(MonaExpr t1, MonaExpr t2, MapStack<string, MonaParam> locals)
        {
            if (t1.type == MonaExprType.INT)
            {
                MSOFormula<BDD> psi_x;
                Variable x = ConvertTerm1(t1, locals, out psi_x);
                MSOFormula<BDD> psi_y;
                Variable y = ConvertTerm1(t2, locals, out psi_y);
                return AddConstraints(psi_x, x, psi_y, y, new MSOEq<BDD>(x, y));
            }
            else
            {
                MSOFormula<BDD> psi_x;
                Variable x = ConvertTerm2(t1, locals, out psi_x);
                MSOFormula<BDD> psi_y;
                Variable y = ConvertTerm2(t2, locals, out psi_y);
                return AddConstraints(psi_x, x, psi_y, y, new MSOEq<BDD>(x, y));
            }
        }

        private MSOFormula<BDD> ConvertSubset(MonaExpr t1, MonaExpr t2, MapStack<string, MonaParam> locals)
        {
            MSOFormula<BDD> psi_x;
            Variable x = ConvertTerm2(t1, locals, out psi_x);
            MSOFormula<BDD> psi_y; 
            Variable y = ConvertTerm2(t2, locals, out psi_y);
            return AddConstraints(psi_x, x, psi_y, y, new MSOSubset<BDD>(x, y));
        }

        private static MSOFormula<BDD> AddConstraints(MSOFormula<BDD> psi_x, Variable x, MSOFormula<BDD> psi_y, Variable y, MSOFormula<BDD> res)
        {
            if (psi_y != null)
                res = new MSOExists<BDD>(y, new MSOAnd<BDD>(psi_y, res));
            if (psi_x != null)
                res = new MSOExists<BDD>(x, new MSOAnd<BDD>(psi_x, res));
            return res;
        }

        private MSOFormula<BDD> ConvertEq(bool fo, MonaExpr t1, MonaExpr t2, MapStack<string, MonaParam> locals)
        {
            MSOFormula<BDD> psi_x;
            Variable x = (fo ? ConvertTerm1(t1, locals, out psi_x) : ConvertTerm2(t1, locals, out psi_x));
            MSOFormula<BDD> psi_y;
            Variable y = (fo ? ConvertTerm1(t2, locals, out psi_y) : ConvertTerm2(t1, locals, out psi_y));
            return AddConstraints(psi_x, x, psi_y, y, new MSOEq<BDD>(x, y));
        }

        private MSOFormula<BDD> ConvertLt(MonaExpr t1, MonaExpr t2, MapStack<string, MonaParam> locals)
        {
            MSOFormula<BDD> psi_x;
            Variable x = ConvertTerm1(t1, locals, out psi_x);
            MSOFormula<BDD> psi_y;
            Variable y = ConvertTerm1(t2, locals, out psi_y);
            return AddConstraints(psi_x, x, psi_y, y, new MSOLt<BDD>(x, y));
        }

        private MSOFormula<BDD> ConvertLe(MonaExpr t1, MonaExpr t2, MapStack<string, MonaParam> locals)
        {
            MSOFormula<BDD> psi_x;
            Variable x = ConvertTerm1(t1, locals, out psi_x);
            MSOFormula<BDD> psi_y;
            Variable y = ConvertTerm1(t2, locals, out psi_y);
            return AddConstraints(psi_x, x, psi_y, y, new MSOLe<BDD>(x, y));
        }

        int newVarId1 = 1;
        int newVarId2 = 1;

        Variable MkNewVar1()
        {
            string v = "#" + newVarId1++;
            return new Variable(v,true);
        }

        Variable MkNewVar2()
        {
            string v = "@" + newVarId2++;
            return new Variable(v, false);
        }

        private MSOFormula<BDD> ConvertIn(MonaExpr t1, MonaExpr t2, MapStack<string, MonaParam> locals)
        {
            MSOFormula<BDD> psi_x;
            Variable x = ConvertTerm1(t1, locals, out psi_x);
            MSOFormula<BDD> psi_y;
            Variable y = ConvertTerm2(t2, locals, out psi_y);
            return AddConstraints(psi_x, x, psi_y, y, new MSOIn<BDD>(x, y));
        }

        /// <summary>
        /// If t is a variable name then returns t as a f-o variable and sets psi=null.
        /// Else returns a fresh f-o variable x and outputs psi(x) s.t. psi(x) iff x=t.
        /// </summary>
        Variable ConvertTerm1(MonaExpr t, MapStack<string, MonaParam> locals, out MSOFormula<BDD> psi) 
        {
            switch (t.symbol.Kind)
            {
                case Tokens.NAME:
                    {
                        MonaParam p;
                        if (locals.TryGetValue(t.symbol.text, out p))
                        {
                            if (p.kind == MonaParamKind.var1)
                            {
                                psi = null;
                                return new Variable(t.symbol.text, true);
                            }
                            else
                                throw new NotImplementedException(t.ToString());
                        }
                        else
                        {
                            MonaDecl d;
                            if (globals.TryGetValue(t.symbol.text, out d))
                            {
                                if (d.kind == MonaDeclKind.constant)
                                {
                                    int n = ((MonaConstDecl)d).def.ToInt(globals);
                                    Variable x = MkNewVar1();
                                    var pred = new MSOeqN<BDD>(x, n);
                                    psi = pred;
                                    return x;
                                }
                                else if (d.kind == MonaDeclKind.var1)
                                {
                                    psi = null;
                                    return new Variable(t.symbol.text, true);
                                }
                                else
                                    throw new NotImplementedException(t.ToString());
                            }
                            else
                                throw new NotImplementedException(t.ToString());
                        }
                    }
                case Tokens.PLUS:
                    {
                        Variable y = MkNewVar1();
                        if (t[0].symbol.Kind == Tokens.NAME)
                        {
                            int n = t[1].ToInt(globals);
                            Variable x = new Variable(t[0].symbol.text, true);
                            psi = new MSOSuccN<BDD>(x, y, n); // y = x + n
                        }
                        else
                        {
                            int n = t.ToInt(globals);
                            psi = new MSOeqN<BDD>(y, n); // y = n
                        }
                        return y;
                    }
                case Tokens.MIN:
                    {
                        MSOFormula<BDD> X_psi;
                        Variable X = ConvertTerm2(t[0], locals, out X_psi);
                        Variable x = MkNewVar1();
                        MSOFormula<BDD> min = new MSOMin<BDD>(x, X);
                        if (X_psi != null)
                            min = new MSOExists<BDD>(X, new MSOAnd<BDD>(X_psi, min));
                        psi = min;
                        return x;
                    }
                case Tokens.MAX:
                    {
                        MSOFormula<BDD> X_psi;
                        Variable X = ConvertTerm2(t[0], locals, out X_psi);
                        Variable x = MkNewVar1();
                        MSOFormula<BDD> max = new MSOMax<BDD>(x, X);
                        if (X_psi != null)
                            max = new MSOExists<BDD>(X, new MSOAnd<BDD>(X_psi, max));
                        psi = max;
                        return x;
                    }

                case Tokens.NUMBER:
                    {
                        Variable x = MkNewVar1();
                        int num = t.symbol.ToInt();
                        psi = new MSOeqN<BDD>(x,num);
                        return x;
                    }
                default:
                    throw new NotImplementedException(t.ToString());
            }
        }

        /// <summary>
        /// If t is a variable name then returns t as a s-o variable and sets psi=null.
        /// Else returns a fresh s-o variable X and outputs psi(X) s.t. psi(X) iff X=t.
        /// </summary>
        private Variable ConvertTerm2(MonaExpr t, MapStack<string, MonaParam> locals, out MSOFormula<BDD> psi)
        {
            switch (t.symbol.Kind)
            {
                case Tokens.NAME:
                    {
                        psi = null;
                        return new Variable(t.symbol.text, false);
                    }
                case Tokens.INTER:
                    return ConvertInter(t[0], t[1], locals, out psi);
                case Tokens.UNION:
                    return ConvertUnion(t[0], t[1], locals, out psi);
                case Tokens.SETMINUS:
                    return ConvertSetminus(t[0], t[1], locals, out psi);
                case Tokens.EMPTY:
                    return ConvertEmptyset(out psi);
                case Tokens.LBRACE:
                    return ConvertSet(t, locals, out psi);
                default:
                    throw new NotImplementedException(t.ToString());
            }
        }

        private Variable ConvertEmptyset(out MSOFormula<BDD> psi)
        {
            var X = MkNewVar2();
            psi = new MSOIsEmpty<BDD>(X);
            return X;
        }

        private Variable ConvertSet(MonaExpr set, MapStack<string, MonaParam> locals, out MSOFormula<BDD> psi)
        {
            if (set.NrOfSubexprs == 0)
                return ConvertEmptyset(out psi);

            MSOFormula<BDD> disj = null;
            Variable x = MkNewVar1();
            Variable X = MkNewVar2();
            for (int i=0; i < set.NrOfSubexprs; i++)
            {
                MonaExpr t = set[i];
                if (t.symbol.Kind == Tokens.RANGE)
                {
                    MSOFormula<BDD> from_psi;
                    Variable from = ConvertTerm1(t[0], locals, out from_psi);
                    MSOFormula<BDD> to_psi;
                    Variable to = ConvertTerm1(t[1], locals, out to_psi);

                    MSOFormula<BDD> range = AddConstraints(from_psi, from, to_psi, to,
                        new MSOAnd<BDD>(new MSOLe<BDD>(from, x), new MSOLe<BDD>(x, to)));

                    if (disj == null)
                        disj = range;
                    else
                        disj = new MSOOr<BDD>(disj, range);
                }
                else
                {
                    MSOFormula<BDD> y_psi;
                    Variable y = ConvertTerm1(t, locals, out y_psi);

                    MSOFormula<BDD> elem = new MSOEq<BDD>(x, y);
                    if (y_psi != null)
                        elem = new MSOExists<BDD>(y, new MSOAnd<BDD>(y_psi, elem));

                    if (disj == null)
                        disj = elem;
                    else
                        disj = new MSOOr<BDD>(disj, elem);
                }
            }

            var pred = new MSOForall<BDD>(x, new MSOEquiv<BDD>(new MSOIn<BDD>(x, X), disj));
            psi = pred;
            return X;
        }

        private Variable ConvertInter(MonaExpr set1, MonaExpr set2, MapStack<string, MonaParam> locals, out MSOFormula<BDD> pred)
        {
            MSOFormula<BDD> psi1; 
            var X1 = ConvertTerm2(set1, locals, out psi1);
            MSOFormula<BDD> psi2;
            var X2 = ConvertTerm2(set2, locals, out psi2);

            var X = MkNewVar2();
            var x = MkNewVar1();
            MSOFormula<BDD> inter = new MSOForall<BDD>(x, 
                                     new MSOEquiv<BDD>(new MSOIn<BDD>(x, X),
                                                     new MSOAnd<BDD>(new MSOIn<BDD>(x, X1), 
                                                                     new MSOIn<BDD>(x, X2))));
            pred = AddConstraints(psi2, X2, psi1, X1, inter);
            return X;
        }

        private Variable ConvertUnion(MonaExpr set1, MonaExpr set2, MapStack<string, MonaParam> locals, out MSOFormula<BDD> pred)
        {
            MSOFormula<BDD> psi1;
            var X1 = ConvertTerm2(set1, locals, out psi1);
            MSOFormula<BDD> psi2;
            var X2 = ConvertTerm2(set2, locals, out psi2);

            var X = MkNewVar2();
            var x = MkNewVar1();
            MSOFormula<BDD> union = new MSOForall<BDD>(x,
                                     new MSOEquiv<BDD>(new MSOIn<BDD>(x, X),
                                                     new MSOOr<BDD>(new MSOIn<BDD>(x, X1),
                                                                    new MSOIn<BDD>(x, X2))));
            pred = AddConstraints(psi2, X2, psi1, X1, union);
            return X;
        }

        private Variable ConvertSetminus(MonaExpr set1, MonaExpr set2, MapStack<string, MonaParam> locals, out MSOFormula<BDD> pred)
        {
            MSOFormula<BDD> psi1;
            var X1 = ConvertTerm2(set1, locals, out psi1); 
            MSOFormula<BDD> psi2;
            var X2 = ConvertTerm2(set2, locals, out psi2);

            var X = MkNewVar2();
            var x = MkNewVar1();
            MSOFormula<BDD> diff = new MSOForall<BDD>(x,
                                     new MSOEquiv<BDD>(new MSOIn<BDD>(x, X),
                                                     new MSOAnd<BDD>(new MSOIn<BDD>(x, X1),
                                                                    new MSONot<BDD>(new MSOIn<BDD>(x, X2)))));
            pred = AddConstraints(psi2, X2, psi1, X1, diff);
            return X;
        }
    }
}
