using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Fast.AST;

using System.Diagnostics;
using Microsoft.Automata.Z3;
using Microsoft.Automata.Z3.Internal;
using Microsoft.Automata;
using Microsoft.Z3;
using ExprSet = Microsoft.Automata.OrderedSet<Microsoft.Z3.Expr>;

namespace Microsoft.Fast
{
    public class FastTransducerInstance
    {
        internal FastLog fastLog;

        private static Z3Provider z3p;

        public List<EnumSort> enums;
        public List<Const> consts;
        public List<Function> functions;
        public Dictionary<string,RankedAlphabetSort> alphabets;

        public Dictionary<String, TreeClassDef> treeDefinitions;

        public List<QueryResult> queryRes;

        private FastTransducerInstance()
        {
            enums = new List<EnumSort>();
            consts = new List<Const>();
            functions = new List<Function>();
            alphabets = new Dictionary<string,RankedAlphabetSort>();
            treeDefinitions = new Dictionary<string, TreeClassDef>();
            queryRes = new List<QueryResult>();
            fastLog = new FastLog();
        }

        private FastTransducerInstance(TextWriter tw)
        {
            enums = new List<EnumSort>();
            consts = new List<Const>();
            functions = new List<Function>();
            alphabets = new Dictionary<string, RankedAlphabetSort>();
            treeDefinitions = new Dictionary<string, TreeClassDef>();
            queryRes = new List<QueryResult>();
            fastLog = new FastLog(tw);
        }

        /// <summary>
        /// Dispose the context
        /// </summary>
        public static void DisposeZ3P()
        {
            z3p.Dispose();
            z3p = null;
        }

        /// <summary>
        /// Generates the corresponding Fast code
        /// </summary>
        /// <param name="sb">string builder to append the generated code</param>
        public void ToFast(StringBuilder sb)
        {
            FastGen fastgen = new FastGen(z3p);
            foreach (var en in enums)
                fastgen.ToFast(en.name, sb);
            foreach (var td in treeDefinitions)
            {
                fastgen.ToFast(td.Value.alphabet.alph, sb);
                foreach (var ac in td.Value.acceptors)
                    fastgen.ToFast(ac.Key, ac.Value, sb,true);
                foreach (var tr in td.Value.transducers)                 
                    fastgen.ToFast(tr.Key, tr.Value, sb,false);
                foreach (var tree in td.Value.trees)
                    fastgen.ToFastTree(tree.Key, td.Value.alphabet, tree.Value, sb);
            }
        }

        /// <summary>
        /// Generate a FastTransducerInstance from a Fast program 
        /// </summary>
        /// <param name="fpg">the fast program</param>
        public static FastTransducerInstance MkFastTransducerInstance(FastPgm fpg)
        {
            return MkFastTransducerInstance(fpg, Console.Out, LogLevel.Normal);
        }

        /// <summary>
        /// Generate a FastTransducerInstance from a Fast program 
        /// </summary>
        /// <param name="fpg">the fast program</param>
        public static FastTransducerInstance MkFastTransducerInstance(FastPgm fpg,TextWriter tw, LogLevel lv)
        {
            FastTransducerInstance fti = new FastTransducerInstance(tw);
            fti.fastLog.setLogLevel(lv);
            z3p = new Z3Provider();

            Dictionary<string, Def> definitions = new Dictionary<string, Def>();
            List<EnumDef> enumDefs = new List<EnumDef>();
            List<Def> constFunDefs = new List<Def>();
            List<AlphabetDef> alphabetDefs = new List<AlphabetDef>();
            List<QueryDef> queryDefs = new List<QueryDef>();

            foreach (var def in fpg.defs)
            {
                switch(def.kind){
                    case DefKind.Query: {
                        queryDefs.Add(def as QueryDef);
                        break;
                    }
                    case DefKind.Alphabet: {
                        alphabetDefs.Add(def as AlphabetDef);
                        break;
                    }
                    case DefKind.Enum: {
                        enumDefs.Add(def as EnumDef);                        
                        break;
                    }
                    case DefKind.Const: case DefKind.Function:{
                        constFunDefs.Add(def);                        
                        break;
                    }     
                }
                if(def.kind!=DefKind.Query)
                    definitions[def.id.text] = def;
            }

            if (!GenerateEnumSorts(enumDefs, fti))
                return null;
            if (!GenerateConstsAndFunctions(constFunDefs, fti))
                return null;
            if (!GenerateAlphabetSorts(alphabetDefs, fti))
                return null;
            if (!GenerateTreeClasses(definitions, fti))
                return null;
            if (!GenerateQueryResults(queryDefs, definitions, fti))
                return null;

            return fti;
        }


        #region enum generation
        //Generate Z3 enums from enum definitions
        private static bool GenerateEnumSorts(List<EnumDef> enumDefs, FastTransducerInstance fti)
        {
            foreach (var def in enumDefs)
            {
                if (!GenerateEnumSort(def, fti))
                     return false;
                break;
            }
            return true;
        }

        //Generate Z3 sorts from enum definition
        private static bool GenerateEnumSort(EnumDef def, FastTransducerInstance fti)
        {
            fti.enums.Add(new EnumSort(def, z3p));
            return true;

        }
        #endregion

        #region consts and functions generation
        //Generate Z3 enums from enum definitions
        private static bool GenerateConstsAndFunctions(List<Def> constFunDefs, FastTransducerInstance fti)
        {
            foreach (var def in constFunDefs)
            {
                switch (def.kind)
                {
                    case (DefKind.Const):
                        {
                            if (!GenerateConst((ConstDef)def, fti))
                                return false;
                            break;
                        }
                    case (DefKind.Function):
                        {
                            if (!GenerateFunction((FunctionDef)def, fti))
                                return false;
                            break;
                        }
                }
            }
            return true;
        }

        //Generate Z3 term for const def
        private static bool GenerateConst(ConstDef def, FastTransducerInstance fti)
        {
            fti.consts.Add(new Const(def, fti, z3p));
            return true;
        }

        //Generate Z3 term for fun def
        private static bool GenerateFunction(FunctionDef def, FastTransducerInstance fti)
        {
            fti.functions.Add(new Function(def, fti, z3p));
            return true;
        }
        #endregion

        #region alphabets generation
        //Generate Z3 sorts from alphabet definitions
        private static bool GenerateAlphabetSorts(List<AlphabetDef> defs, FastTransducerInstance fti)
        {
            //Generate the set of alphabets in fti
            foreach (var def in defs)
            {
                AlphabetDef alphDef = def as AlphabetDef;
                fti.alphabets[alphDef.id.text] = new RankedAlphabetSort(alphDef, z3p, fti);
                fti.fastLog.WriteLog(LogLevel.Maximal, string.Format("alphabet '{0}' correctly added", alphDef.id));                           
            }
            return true;
        }
        #endregion

        #region GenerateQueryResults
        //Generate resultsfor queries
        private static bool GenerateQueryResults(List<QueryDef> queryDefs, Dictionary<string, Def> defs, FastTransducerInstance fti)
        {
            for (int i = 0; i < queryDefs.Count; i++ )
            {
                var def = queryDefs[i];
                if (!GenerateQueryResult(def, defs, fti))
                    return false;
            }
            return true;
        }
        //Generate result from query
        private static bool GenerateQueryResult(QueryDef query, Dictionary<string, Def> defs, FastTransducerInstance fti)
        {
            string msg = "";
            switch (query.id.Kind)
            {
                case (Tokens.CONTAINS):
                    {
                        ContainsQueryDef ct = query as ContainsQueryDef;

                        IEnumerable<Expr> t = null;
                        if (ct.expr.kind == FExpKind.Var)
                        {
                            foreach (var treeDef in fti.treeDefinitions)
                                if (treeDef.Value.trees.TryGetValue(ct.expr.token.text, out t))
                                    break;
                        }
                        else
                        {
                            throw new FastException(FastExceptionKind.InternalError);
                        }

                        TreeTransducer lang = OperationTranGen.getTreeAutomatonFromExpr(ct.language, fti, defs);

                        var results = lang.Apply(t).ToList();
                        var containsRes = results.Count>0;
                        
                        msg = string.Format("'{0}' is{1} a member of the language '{2}'", ct.expr, containsRes ? "" : " not", ct.language);                        
                        if (ct.isAssert)
                        {
                            if (containsRes == ct.assertTrue)
                                return true;
                            else
                                throw new FastAssertException(msg,ct.func.name.line, ct.func.name.position);
                        }                        
                        break;
                    }
                case (Tokens.TYPECHECK):
                    {
                        TypecheckQueryDef ct = query as TypecheckQueryDef;                        

                        TreeTransducer input = OperationTranGen.getTreeAutomatonFromExpr(ct.input, fti, defs);
                        TreeTransducer output = OperationTranGen.getTreeAutomatonFromExpr(ct.output, fti, defs);
                        TreeTransducer trans = OperationTranGen.getTreeAutomatonFromExpr(ct.trans, fti, defs);

                        //Preimage of outputcomplement doesn't interesect input
                        var ocomp = output.Complement();
                        var preim = trans.RestrictRange(ocomp).ComputeDomainAcceptor();
                        var badinp = preim.Intersect(input);
                        bool typechecks = badinp.IsEmpty;

                        //For assertions
                        var sb = new StringBuilder();
                        if (!typechecks)
                        {
                            var badInput = badinp.GenerateWitness();
                            FastGen fg = new FastGen(z3p);
                            sb.Append("\n ---> input '");
                            fg.ToFastExpr(badInput, sb, false);
                            sb.Append("' produces output '");
                            // take first bad output
                            foreach (var v in trans.Apply(new Expr[] { badInput }))                            
                                if(output.Apply(new Expr[] { v })!=null)
                                {
                                    fg.ToFastExpr(v, sb, false);
                                    break;
                                }                           
                            sb.Append("'");
                        }
                        msg = string.Format("'{0}' has{3} type '{1}' -> '{2}'{4}", 
                            ct.trans, ct.input, ct.output, typechecks ? "" : " not",sb.ToString());
                        if (ct.isAssert)
                        {
                            if (typechecks == ct.assertTrue)
                                return true;
                            else
                                throw new FastAssertException(msg, ct.func.name.line, ct.func.name.position);
                        }  
                        break;
                    }
                case (Tokens.ID):
                case (Tokens.PRINT):
                    {
                        DisplayQueryDef dt = query as DisplayQueryDef;
                        StringBuilder sb = new StringBuilder();
                        FastGen fastgen = new FastGen(z3p);

                        var toPrintDef = defs[dt.toPrintVar];
                        switch (toPrintDef.kind)
                        {
                            case DefKind.Alphabet:
                            case DefKind.Const:
                            case DefKind.Function:
                            case DefKind.Enum:
                                {
                                    toPrintDef.PrettyPrint(sb);
                                    msg = string.Format("'{0}': {1}", dt.toPrintVar, sb);
                                    break;
                                }
                            case DefKind.Lang:
                                {
                                    LangDef td = toPrintDef as LangDef;
                                    var trans = fti.treeDefinitions[td.domain.name.text].acceptors[dt.toPrintVar];                                    
                                    fastgen.ToFast(dt.toPrintVar,trans, sb,true);
                                    msg = string.Format("{0}", sb);
                                    break;
                                }
                            case DefKind.Trans:
                                {
                                    TransDef td = toPrintDef as TransDef;
                                    var trans = fti.treeDefinitions[td.domain.name.text].transducers[dt.toPrintVar];                                    
                                    fastgen.ToFast(dt.toPrintVar,trans, sb,false);
                                    msg = string.Format("{0}", sb);
                                    break;
                                }
                            case DefKind.Def:
                                {
                                    DefDef df = toPrintDef as DefDef;
                                    switch(df.ddkind){
                                        case DefDefKind.Lang:
                                            {
                                                LangDefDef td = df as LangDefDef;
                                                var trans = fti.treeDefinitions[td.domain.name.text].acceptors[dt.toPrintVar];
                                                fastgen.ToFast(dt.toPrintVar, trans, sb, true);
                                                msg = string.Format("{0}", sb);
                                                break;
                                            }
                                        case DefDefKind.Trans:
                                            {
                                                TransDefDef td = df as TransDefDef;
                                                var trans = fti.treeDefinitions[td.domain.name.text].transducers[dt.toPrintVar];
                                                fastgen.ToFast(dt.toPrintVar, trans, sb, false);
                                                msg = string.Format("{0}", sb);
                                                break;
                                            }
                                        case DefDefKind.Tree:
                                            {
                                                TreeDef td = df as TreeDef;
                                                var trees = new List<Expr>(fti.treeDefinitions[td.domain.name.text].trees[dt.toPrintVar]);
                                                var counter = 1;  
                                                if(trees.Count==0){
                                                    msg = string.Format("'{0}' does not contain any tree", dt.toPrintVar);
                                                    break;
                                                }
                                                sb.AppendLine(string.Format("'{0}': [", dt.toPrintVar));
                                                foreach (var tree in trees)
                                                {
                                                    sb.AppendFormat("\t{0}) ",counter);
                                                    fastgen.ToFastExpr(tree, sb, false);
                                                    sb.AppendLine();
                                                    counter++;
                                                }
                                                sb.Append("]");
                                                msg += sb.ToString();
                                                break;
                                            }
                                    }
                                    break;
                                }
                        } 
                        break;
                    }
                case (Tokens.STRING):
                    {
                        StringQueryDef ie = query as StringQueryDef;
                        msg = ie.message;
                        break;
                    }
                case (Tokens.IS_EMPTY_TRANS):
                case (Tokens.IS_EMPTY_LANG):
                    {
                        IsEmptyQueryDef ie = query as IsEmptyQueryDef;

                        if (ie.isTrans)
                        {
                            IsEmptyTransQueryDef te = ie as IsEmptyTransQueryDef;
                            TreeTransducer trans = OperationTranGen.getTreeAutomatonFromExpr(te.trans, fti, defs);

                            if (trans.IsEmpty)                            
                                msg = string.Format("The transformation '{0}' is empty", te.trans);                            
                            else
                            {
                                FastGen fg = new FastGen(z3p);
                                StringBuilder sb = new StringBuilder();
                                var tre = trans.ComputeDomainAcceptor().GenerateWitness();
                                fg.ToFastExpr(tre, sb, false);
                                msg = string.Format("The transformation '{0}' is not empty\n ---> '{0}' accepts the tree '{1}'", te.trans, sb.ToString());
                            }
                            //For assertions
                            if (te.isAssert)
                            {
                                if (trans.IsEmpty == te.assertTrue)
                                    return true;
                                else
                                    throw new FastAssertException(msg, te.func.name.line, te.func.name.position);
                            }
                            
                        }
                        else
                        {
                            IsEmptyLangQueryDef te = ie as IsEmptyLangQueryDef;
                            TreeTransducer lang = OperationTranGen.getTreeAutomatonFromExpr(te.lang, fti, defs);
                           
                            if (lang.IsEmpty)
                            {
                                msg = string.Format("The transformation '{0}' is empty", te.lang);
                            }
                            else
                            {
                                FastGen fg = new FastGen(z3p);
                                StringBuilder sb = new StringBuilder();
                                var tre = lang.GenerateWitness();
                                fg.ToFastExpr(tre, sb, false);
                                msg = string.Format("The language '{0}' is not empty\n ---> '{0}' accepts the tree '{1}'", te.lang, sb.ToString());
                            }

                            if (te.isAssert)
                            {
                                if (lang.IsEmpty == te.assertTrue)
                                    return true;
                                else
                                    throw new FastAssertException(msg, te.func.name.line, te.func.name.position);
                            }
                        }
                        break;
                    }
                case (Tokens.EQ_TRANS):
                case (Tokens.EQ_LANG):
                    {
                        EquivQueryDef ie = query as EquivQueryDef;

                        if (ie.isTransEquiv)
                        {
                            TransEquivQueryDef te = ie as TransEquivQueryDef;
                            TreeTransducer trans1 = OperationTranGen.getTreeAutomatonFromExpr(te.trans1, fti, defs);
                            TreeTransducer trans2 = OperationTranGen.getTreeAutomatonFromExpr(te.trans2, fti, defs);

                            throw new FastException(FastExceptionKind.NotImplemented);

                            //if (te.isAssert)
                            //{
                            //    if (lang.IsEmpty == te.assertTrue)
                            //        return true;
                            //    else
                            //        throw new FastAssertException(ct.func.name.line, ct.func.name.position);
                            //}
                            //msg = string.Format("The transformation '{0}' is{1} empty", te.trans, trans.IsEmpty ? "" : " not");
                        }
                        else
                        {
                            StringBuilder sb = new StringBuilder();
                            FastGen fg = new FastGen(z3p);
                            StringBuilder msgBd = new StringBuilder();
                            LangEquivQueryDef te = ie as LangEquivQueryDef;
                            TreeTransducer lang1 = OperationTranGen.getTreeAutomatonFromExpr(te.lang1, fti, defs);
                            TreeTransducer lang2 = OperationTranGen.getTreeAutomatonFromExpr(te.lang2, fti, defs);

                            var areEquiv = false;
                            string counterex ="";
                            //var lang1m = lang1.MinimizeMoore();
                            TreeTransducer lang1_compl = lang1.Complement();
                            TreeTransducer lang = lang1_compl.Intersect(lang2);
                            if (lang.IsEmpty)
                            {
                                lang = lang2.Complement().Intersect(lang1);
                                areEquiv = lang.IsEmpty;
                                if (!areEquiv)
                                {
                                    fg.ToFastExpr(lang.GenerateWitness(), sb, false);
                                    counterex = string.Format("---> the language '{0}' contains the tree '{1}' while '{2}' doesn't.", te.lang1, sb.ToString(), te.lang2);
                                }
                            }
                            else
                            {
                                var tre = lang.GenerateWitness();
                                fg.ToFastExpr(tre, sb, false);
                                counterex = string.Format("---> the language '{0}' contains the tree '{1}' while '{2}' doesn't.", te.lang2, sb.ToString(),te.lang1);
                            }
                            
                            msgBd.AppendFormat("The language '{0}' is{1} equivalent to language '{2}':", te.lang1, lang.IsEmpty ? "" : " not",te.lang2);
                            if (!areEquiv)
                            {
                                msgBd.AppendLine();
                                msgBd.Append(counterex);                                
                            }
                            msg = msgBd.ToString();

                            //Assertion case
                            if (te.isAssert)
                            {
                                if (areEquiv == te.assertTrue)
                                    return true;
                                else
                                    throw new FastAssertException(msg, te.func.name.line, te.func.name.position);
                            }
                        }
                        break;
                    }
                case (Tokens.GEN_CSHARP):
                    {
                        GenCodeQueryDef cge = query as GenCodeQueryDef;
                        StringBuilder sb = new StringBuilder();

                        switch (cge.language)
                        {
                            case PLang.CSharp:
                                fti.ToFast(sb);
                                FastPgm pgm = Parser.ParseFromString(sb.ToString());
                                sb = new StringBuilder();
                                sb.AppendLine("generated C# [");
                                CsharpGenerator.GenerateCode(pgm, sb);
                                sb.AppendLine("]");
                                msg = sb.ToString();
                                break;
                            case PLang.Javascript:
                                throw new FastException(FastExceptionKind.NotImplemented);
                        }
                        break;
                    }
                default:
                    throw new FastException(FastExceptionKind.InternalError);
            }            
            fti.fastLog.WriteLog(LogLevel.Minimal, msg);
            fti.queryRes.Add(new QueryResult(msg));
            return true;
        } 
        #endregion


        #region tree languages and transductions
        //Generate the trees definition for each alphabet
        private static bool GenerateTreeClasses(Dictionary<string, Def> defs, FastTransducerInstance fti)
        {
            //Generate one tree with the corresponding transductions and languages for each ranked alphabet
            foreach (var ras in fti.alphabets.Values)
            {
                if (!GenerateTreeClass(defs, fti, ras))
                    return false;
            }
            //Generate Languages And Transductions
            if (!GenerateLanguagesAndTransductions(defs, fti))
                return false;
            return GenerateDefinitions(defs, fti);
        }

        //Generate the tree definition corresponding to a ranked alphabet
        private static bool GenerateTreeClass(Dictionary<string, Def> defs, FastTransducerInstance fti, RankedAlphabetSort ras)
        {
            //Create a new tree definition
            TreeClassDef treeDef = new TreeClassDef(ras, z3p);
            fti.treeDefinitions.Add(ras.alphName, treeDef);            
            return true;
        }

        //Generates all the languages and transductions of a particular tree definitions class
        private static bool GenerateLanguagesAndTransductions(Dictionary<string, Def> defs, FastTransducerInstance fti)
        {
            foreach (var def in defs.Values)
            {
                if (def.kind == DefKind.Lang){
                    LangDef langDef= def as LangDef;
                    if (langDef.isPublic)
                        if (!fti.treeDefinitions[langDef.domain.name.text].AddTransducer(def, fti, defs))
                            return false;
                        else
                            fti.fastLog.WriteLog(LogLevel.Maximal, string.Format("language '{0}:{1}' correctly added", langDef.func.name.text, langDef.domain.name.text));
                }
                if (def.kind == DefKind.Trans)
                {
                    TransDef transDef = def as TransDef;
                    if (transDef.isPublic)
                        if (!fti.treeDefinitions[transDef.domain.name.text].AddTransducer(def, fti, defs))
                            return false;
                        else
                            fti.fastLog.WriteLog(LogLevel.Maximal,
                                string.Format("transformation '{0}: {1} -> {2}' correctly added", 
                                    transDef.func.name.text, 
                                    transDef.domain.name.text, 
                                    transDef.range.name.text)
                                );
                }
            }
      
            return true;
        }

        //Generates all the languages and transductions of a particular tree definitions class
        private static bool GenerateDefinitions(Dictionary<string, Def> defs, FastTransducerInstance fti)
        {
            foreach (var def in defs.Values)
            {
                if (def.kind == DefKind.Def)
                {
                    switch (((DefDef)def).ddkind)
                    {
                        case DefDefKind.Trans:
                            {
                                TransDefDef transDef = def as TransDefDef;
                                if (!(fti.treeDefinitions[transDef.domain.name.text].AddTransducer(transDef, fti, defs)))
                                    return false;
                                else
                                    fti.fastLog.WriteLog(LogLevel.Maximal,
                                        string.Format("transformation '{0}: {1} -> {2}' correctly added",
                                            transDef.func.name.text,
                                            transDef.domain.name.text,
                                            transDef.range.name.text)
                                        );
                                break;
                            }
                        case DefDefKind.Lang:
                            {
                                LangDefDef langDef = def as LangDefDef;
                                if (!(fti.treeDefinitions[langDef.domain.name.text].AddAcceptor(langDef, fti, defs)))
                                    return false;
                                else
                                    fti.fastLog.WriteLog(LogLevel.Maximal, string.Format("language '{0}:{1}' correctly added", langDef.func.name.text, langDef.domain.name.text));
                                break;
                            }
                        case DefDefKind.Tree:
                            {
                                TreeDef treeDef = def as TreeDef;
                                if (!fti.treeDefinitions[treeDef.domain.name.text].AddTree(treeDef, fti, defs))
                                    return false;
                                else
                                    fti.fastLog.WriteLog(LogLevel.Maximal, string.Format("tree '{0}:{1}' correctly added", treeDef.func.name.text, treeDef.domain.name.text));
                                break;
                            }
                    }
                }
            }
            return true;
        }
        #endregion

    }

    public class QueryResult
    {
        string message;
        public QueryResult(string message)
        {
            this.message=message;
        }
    }

    //Classes for representation of a program
    public class EnumSort
    {
        public Z3Provider z3p;

        public Sort sort;
        public String name;

        public EnumSort(EnumDef def, Z3Provider z3p)
        {
            this.z3p = z3p;
            this.name = def.id.text;
            this.sort = z3p.MkEnumSort(def.id.text, def.elems.ToArray());
        }
    }

    public class Const
    {
        public Z3Provider z3p;

        public Sort sort;
        public String name;
        public Expr value;

        public Const(ConstDef def, FastTransducerInstance fti, Z3Provider z3p)
        {
            this.z3p = z3p;
            this.name = def.id.text;
            switch (def.sort.kind)
            {
                case (FastSortKind.Real):
                    {
                        sort = z3p.RealSort;
                        break;
                    }
                case (FastSortKind.Bool):
                    {
                        sort = z3p.BoolSort;
                        break;
                    }
                case (FastSortKind.Int):
                    {
                        sort = z3p.IntSort;
                        break;
                    }
                case (FastSortKind.String):
                    {
                        sort = z3p.MkListSort(z3p.CharSort);
                        break;
                    }
                case (FastSortKind.Tree):
                    {
                        foreach (var enumSort in fti.enums)
                        {
                            if (enumSort.name == def.sort.name.text)
                            {
                                sort = enumSort.sort;
                                break;
                            }
                        }
                        break;
                    }
            }
            this.value = GenerateZ3ExprFromExpr(def.expr, fti).Simplify();
        }


        #region Z3 Expr Generation for guard expressions
        public Expr GenerateZ3ExprFromExpr(FExp expr, FastTransducerInstance fti)
        {
            switch (expr.kind)
            {
                case (FExpKind.App):
                    return GenerateZ3Expr((AppExp)expr, fti);
                case (FExpKind.Value):
                    return GenerateZ3Expr((Value)expr, fti);
                case (FExpKind.Var):
                    return GenerateZ3Expr((Variable)expr, fti);
            }
            return null;
        }

        private Expr GenerateZ3Expr(AppExp expr, FastTransducerInstance fti)
        {
            List<Expr> termList = new List<Expr>();
            switch (expr.func.name.text)
            {
                case ("="):
                    {
                        return z3p.MkEq(GenerateZ3ExprFromExpr(expr.args[0], fti), GenerateZ3ExprFromExpr(expr.args[1], fti));
                    }
                case ("and"):
                    {
                        foreach (var arg in expr.args)
                            termList.Add(GenerateZ3ExprFromExpr(arg, fti));
                        return z3p.MkAnd(termList.ToArray());
                    }
                case ("xor"):
                    {
                        if (expr.args.Count > 2)
                            throw new Exception("Too many arguments");
                        return z3p.Z3.MkXor((BoolExpr)GenerateZ3ExprFromExpr(expr.args[0], fti), (BoolExpr)GenerateZ3ExprFromExpr(expr.args[1], fti));
                    }
                case ("not"):
                    {
                        return z3p.MkNot(GenerateZ3ExprFromExpr(expr.args[0], fti));
                    }
                case ("or"):
                    {
                        foreach (var arg in expr.args)
                            termList.Add(GenerateZ3ExprFromExpr(arg, fti));
                        return z3p.MkOr(termList.ToArray());
                    }
                case ("=>"):
                    {
                        return z3p.MkOr(z3p.MkNot(GenerateZ3ExprFromExpr(expr.args[0], fti)), GenerateZ3ExprFromExpr(expr.args[1], fti));
                    }
                case ("+"):
                    {
                        foreach (var arg in expr.args)
                            termList.Add(GenerateZ3ExprFromExpr(arg, fti));
                        if (z3p.GetSort(termList[0]).SortKind == Z3_sort_kind.Z3_BV_SORT)
                            return z3p.MkBvAddMany(termList.ToArray());
                        return z3p.MkAdd(termList.ToArray());
                    }
                case ("/"):
                    {
                        return z3p.MkDiv(GenerateZ3ExprFromExpr(expr.args[0], fti), GenerateZ3ExprFromExpr(expr.args[1], fti));
                    }
                case ("-"):
                    {
                        return z3p.MkSub(GenerateZ3ExprFromExpr(expr.args[0], fti), GenerateZ3ExprFromExpr(expr.args[1], fti));
                    }
                case ("*"):
                    {
                        return z3p.MkMul(GenerateZ3ExprFromExpr(expr.args[0], fti), GenerateZ3ExprFromExpr(expr.args[1], fti));
                    }
                case ("<"):
                    {
                        return z3p.MkLt(GenerateZ3ExprFromExpr(expr.args[0], fti), GenerateZ3ExprFromExpr(expr.args[1], fti));
                    }
                case ("<="):
                    {
                        return z3p.MkLe(GenerateZ3ExprFromExpr(expr.args[0], fti), GenerateZ3ExprFromExpr(expr.args[1], fti));
                    }
                case (">"):
                    {
                        return z3p.MkGt(GenerateZ3ExprFromExpr(expr.args[0], fti), GenerateZ3ExprFromExpr(expr.args[1], fti));
                    }

                case (">="):
                    {
                        return z3p.MkGe(GenerateZ3ExprFromExpr(expr.args[0], fti), GenerateZ3ExprFromExpr(expr.args[1], fti));
                    }
                case ("if"):
                    {
                        return z3p.MkIte(GenerateZ3ExprFromExpr(expr.args[0], fti), GenerateZ3ExprFromExpr(expr.args[1], fti), GenerateZ3ExprFromExpr(expr.args[2], fti));
                    }
                default:
                    {
                        foreach (var f in fti.functions)
                        {
                            if (expr.func.name.text == f.name)
                            {
                                List<Expr> argsExprs = new List<Expr>();
                                foreach (var a in expr.args)
                                {
                                    argsExprs.Add(GenerateZ3ExprFromExpr(a, fti));
                                }
                                return z3p.ApplySubstitution(f.functionDef, f.variableExprs.Values.ToArray(), argsExprs.ToArray<Expr>());
                                //return z3p.MkApp(f.funcDecl, argsExprs.ToArray<Expr>());
                            }
                        }
                        throw new Exception("Function not defined");
                    }
            }
        }

        private Expr GenerateZ3Expr(Value expr, FastTransducerInstance fti)
        {
            switch (expr.sort.kind)
            {
                case (FastSortKind.Bool):
                    {
                        if (expr.token.Kind == Tokens.TRUE)
                            return z3p.True;
                        if (expr.token.Kind == Tokens.FALSE)
                            return z3p.False;
                        break;
                    }
                case (FastSortKind.Int):
                    {
                        return z3p.MkInt(Convert.ToInt32(expr.token.text));
                    }
                case (FastSortKind.Real):
                    {
                        String[] parts = expr.token.text.Split('.');
                        if (parts.Length == 2 && parts[1] == "0")
                        {
                            return z3p.Z3.MkReal(parts[0]);
                        }
                        return z3p.Z3.MkReal(expr.token.text);
                    }
                case (FastSortKind.String):
                    {
                        return z3p.MkListFromString(expr.token.text.Substring(1, expr.token.text.Length - 2), z3p.CharSort);
                    }
                case (FastSortKind.Tree):
                    {
                        String[] lr = expr.token.text.Split('.');
                        return z3p.GetEnumElement(lr[0], lr[1]);
                    }
            }
            throw new Exception("Unexpected Sort");
        }

        private Expr GenerateZ3Expr(Variable expr, FastTransducerInstance fti)
        {
            foreach (var c in fti.consts)
            {
                if (c.name == expr.token.text)
                    return c.value;
            }
            throw new Exception("The constant " + expr.token.text + " wasn't defined before");
        }
        #endregion

    }

    public class Function
    {
        public Z3Provider z3p;

        public String name;
        public FuncDecl funcDecl;
        public Dictionary<string, Sort> inputSorts;
        public Sort outputSort;
        public int arity;
        public Expr functionDef;
        public Dictionary<string, Expr> variableExprs;

        public Function(FunctionDef def, FastTransducerInstance fti, Z3Provider z3p)
        {
            this.z3p = z3p;
            this.arity = def.inputVariables.Count;
            this.name = def.id.text;
            this.outputSort = getSort(def.outputSort, fti);
            this.inputSorts = new Dictionary<string, Sort>();
            this.variableExprs = new Dictionary<string, Expr>(); //contains the terms for the variables of the function

            #region Compute input and outputs sort
            Sort[] inputs = new Sort[this.arity];
            uint i = 0;
            foreach (var va in def.inputVariables)
            {
                inputs[i] = getSort(va.Value, fti);
                inputSorts.Add(va.Key.text, inputs[i]);
                variableExprs.Add(va.Key.text, z3p.MkVar(i, inputs[i]));
                i++;
            }
            Expr[] varExprs = variableExprs.Values.ToArray();
            #endregion

            this.funcDecl = z3p.MkFreshFuncDecl(this.name, inputs, this.outputSort);
            var e = GenerateZ3ExprFromExpr(def.expr, fti);
            this.functionDef = e.Simplify(); //z3p.MkEqForall(z3p.MkApp(funcDecl, varExprs), GenerateZ3ExprFromExpr(def.expr, fti), varExprs);
        }

        private Sort getSort(FastSort fs, FastTransducerInstance fti)
        {
            switch (fs.kind)
            {
                case (FastSortKind.Real):
                    return z3p.RealSort;
                case (FastSortKind.Bool):
                    return z3p.BoolSort;
                case (FastSortKind.Int):
                    return z3p.IntSort;
                case (FastSortKind.Char):
                    return z3p.CharSort;
                case (FastSortKind.String):
                    return z3p.MkListSort(z3p.CharSort);
                case (FastSortKind.Tree):
                    {
                        foreach (var enumSort in fti.enums)
                        {
                            if (enumSort.name == fs.name.text)
                            {
                                return enumSort.sort;
                            }
                        }
                        break;
                    }
            }
            return null;
        }


        #region Z3 Expr Generation for guard expressions
        public Expr GenerateZ3ExprFromExpr(FExp expr, FastTransducerInstance fti)
        {
            switch (expr.kind)
            {
                case (FExpKind.App):
                    return GenerateZ3Expr((AppExp)expr, fti);
                case (FExpKind.Value):
                    return GenerateZ3Expr((Value)expr, fti);
                case (FExpKind.Var):
                    return GenerateZ3Expr((Variable)expr, fti);
            }
            return null;
        }

        private Expr GenerateZ3Expr(AppExp expr, FastTransducerInstance fti)
        {
            List<Expr> termList = new List<Expr>();
            switch (expr.func.name.Kind)
            {
                case (Tokens.EQ):
                    {
                        return z3p.MkEq(GenerateZ3ExprFromExpr(expr.args[0], fti), GenerateZ3ExprFromExpr(expr.args[1], fti));
                    }
                case (Tokens.AND):
                    {
                        foreach (var arg in expr.args)
                            termList.Add(GenerateZ3ExprFromExpr(arg, fti));
                        return z3p.MkAnd(termList.ToArray());
                    }
                //case ("xor"):
                //    {
                //        return z3p.Z3.MkXor((BoolExpr)GenerateZ3ExprFromExpr(expr.args[0], fti), (BoolExpr)GenerateZ3ExprFromExpr(expr.args[1], fti));
                //    }
                case (Tokens.NOT):
                    {
                        return z3p.MkNot(GenerateZ3ExprFromExpr(expr.args[0], fti));
                    }
                case (Tokens.OR):
                    {
                        foreach (var arg in expr.args)
                            termList.Add(GenerateZ3ExprFromExpr(arg, fti));
                        return z3p.MkOr(termList.ToArray());
                    }
                case (Tokens.IMPLIES):
                    {
                        return z3p.MkOr(z3p.MkNot(GenerateZ3ExprFromExpr(expr.args[0], fti)), GenerateZ3ExprFromExpr(expr.args[1], fti));
                    }
                case (Tokens.PLUS):
                    {
                        foreach (var arg in expr.args)
                            termList.Add(GenerateZ3ExprFromExpr(arg, fti));
                        if (z3p.GetSort(termList[0]).SortKind == Z3_sort_kind.Z3_BV_SORT)
                            return z3p.MkBvAddMany(termList.ToArray());
                        return z3p.MkAdd(termList.ToArray());
                    }
                case (Tokens.DIV):
                    {
                        return z3p.MkCharDiv(GenerateZ3ExprFromExpr(expr.args[0], fti), GenerateZ3ExprFromExpr(expr.args[1], fti));
                    }
                case (Tokens.MINUS):
                    {
                        return z3p.MkCharSub(GenerateZ3ExprFromExpr(expr.args[0], fti), GenerateZ3ExprFromExpr(expr.args[1], fti));
                    }
                case (Tokens.TIMES):
                    {
                        return z3p.MkCharMul(GenerateZ3ExprFromExpr(expr.args[0], fti), GenerateZ3ExprFromExpr(expr.args[1], fti));
                    }
                case (Tokens.LT):
                    {
                        return z3p.MkCharLt(GenerateZ3ExprFromExpr(expr.args[0], fti), GenerateZ3ExprFromExpr(expr.args[1], fti));
                    }
                case (Tokens.LE):
                    {
                        return z3p.MkCharLe(GenerateZ3ExprFromExpr(expr.args[0], fti), GenerateZ3ExprFromExpr(expr.args[1], fti));
                    }
                case (Tokens.GT):
                    {
                        return z3p.MkCharGt(GenerateZ3ExprFromExpr(expr.args[0], fti), GenerateZ3ExprFromExpr(expr.args[1], fti));
                    }
                case (Tokens.GE):
                    {
                        return z3p.MkCharGe(GenerateZ3ExprFromExpr(expr.args[0], fti), GenerateZ3ExprFromExpr(expr.args[1], fti));
                    }
                case (Tokens.MOD):
                    {
                        return z3p.MkMod(GenerateZ3ExprFromExpr(expr.args[0], fti), GenerateZ3ExprFromExpr(expr.args[1], fti));
                    }
                //case ("concat"):
                //    {
                //        throw new Exception("concat not defined yet");
                //        //return z3p.MkL(GenerateZ3ExprFromExpr(expr.args[0], fti), GenerateZ3ExprFromExpr(expr.args[1], fti));
                //    }
                case (Tokens.ITE):
                    {
                        return z3p.MkIte(GenerateZ3ExprFromExpr(expr.args[0], fti), GenerateZ3ExprFromExpr(expr.args[1], fti), GenerateZ3ExprFromExpr(expr.args[2], fti));
                    }
                case (Tokens.ID):
                    {
                        foreach (var f in fti.functions)
                        {
                            if (this.name == f.name)
                                throw new Exception("cannot define recursive functions");

                            if (expr.func.name.text == f.name)
                            {
                                List<Expr> argsExprs = new List<Expr>();
                                foreach (var a in expr.args)
                                {
                                    argsExprs.Add(GenerateZ3ExprFromExpr(a, fti));
                                }
                                return z3p.ApplySubstitution(f.functionDef, f.variableExprs.Values.ToArray(), argsExprs.ToArray<Expr>());
                                //return z3p.MkApp(f.funcDecl, argsExprs.ToArray<Expr>());
                            }
                        }
                        throw new FastException(FastExceptionKind.UnknownFunction);
                    }
                default:
                    throw new FastException(FastExceptionKind.UnknownFunction);
            }
        }

        private Expr GenerateZ3Expr(Value expr, FastTransducerInstance fti)
        {
            switch (expr.sort.kind)
            {
                case (FastSortKind.Bool):
                    {
                        if (expr.token.Kind == Tokens.TRUE)
                            return z3p.True;
                        if (expr.token.Kind == Tokens.FALSE)
                            return z3p.False;
                        break;
                    }
                case (FastSortKind.Int):
                    {
                        return z3p.MkInt(expr.token.ToInt());
                    }
                case (FastSortKind.Char):
                    {
                        return z3p.MkNumeral(expr.token.ToInt(), z3p.CharSort);
                    }
                case (FastSortKind.Real):
                    {
                        String[] parts = expr.token.text.Split('.');
                        if (parts.Length == 2 && parts[1] == "0")
                        {
                            return z3p.Z3.MkReal(parts[0]);
                        }
                        return z3p.Z3.MkReal(expr.token.text);
                    }
                case (FastSortKind.String):
                    {
                        return z3p.MkListFromString(expr.token.text.Substring(1, expr.token.text.Length - 2), z3p.CharSort);
                    }
                case (FastSortKind.Tree):
                    {
                        String[] lr = expr.token.text.Split('.');
                        return z3p.GetEnumElement(lr[0], lr[1]);
                    }
                case (FastSortKind.Record):
                    {
                        throw new NotImplementedException();
                    }
            }
            throw new Exception("Unexpected Sort");
        }

        private Expr GenerateZ3Expr(Variable expr, FastTransducerInstance fti)
        {
            foreach (var v in this.variableExprs)
            {
                if (v.Key == expr.token.text)
                    return v.Value;
            }
            foreach (var c in fti.consts)
            {
                if (c.name == expr.token.text)
                    return c.value;
            }
            throw new Exception("The variable " + expr.token.text + " wasn't defined before");
        }
        #endregion

    }

    public class RankedAlphabetSort
    {
        public Z3Provider z3p;

        public RankedAlphabet alph;
        public Sort alphSort;
        public String alphName;

        public Sort tupleSort;
        public String tupleName;
        public List<String> tupleKeys;
        public List<Sort> alphFieldsSorts;
        public FuncDecl tupleFuncDec;
        public FuncDecl[] tupleTests;

        public RankedAlphabetSort(AlphabetDef def, Z3Provider z3p, FastTransducerInstance fti)
        {
            this.z3p = z3p;

            List<string> alphSymbols = new List<string>();
            List<int> alphArities = new List<int>();

            // Create list of symbols with corresponding arities
            foreach (var sym in def.symbols)
            {
                alphSymbols.Add(sym.name.text);
                alphArities.Add(sym.arity - 1);
            }

            alphFieldsSorts = new List<Sort>();
            tupleKeys = new List<String>();

            foreach (var field in def.attrSort.fieldSorts)
            {
                tupleKeys.Add(field.Key);
                switch (field.Value.kind)
                {
                    case (FastSortKind.Char):
                        {
                            alphFieldsSorts.Add(z3p.CharSort);
                            break;
                        }
                    case (FastSortKind.Real):
                        {
                            alphFieldsSorts.Add(z3p.RealSort);
                            break;
                        }
                    case (FastSortKind.Bool):
                        {
                            alphFieldsSorts.Add(z3p.BoolSort);
                            break;
                        }
                    case (FastSortKind.Int):
                        {
                            alphFieldsSorts.Add(z3p.IntSort);
                            break;
                        }
                    case (FastSortKind.String):
                        {
                            alphFieldsSorts.Add(z3p.MkListSort(z3p.CharSort));
                            break;
                        }
                    case (FastSortKind.Tree):
                        {
                            foreach (var enumSort in fti.enums)
                            {
                                if (enumSort.name == field.Value.name.text)
                                {
                                    alphFieldsSorts.Add(enumSort.sort);
                                    break;
                                }
                            }
                            break;
                        }
                }
            }

            this.tupleName = "$" + def.id.text;
            this.tupleTests = new FuncDecl[alphFieldsSorts.Count];
            var tupsymbs = new Symbol[tupleKeys.Count];
            int j = 0;
            foreach(var v in tupleKeys){
                tupsymbs[j]= z3p.Z3.MkSymbol(v);
                j++;
            }
            var tup = z3p.Z3.MkTupleSort(z3p.Z3.MkSymbol(tupleName), tupsymbs, alphFieldsSorts.ToArray());
            this.tupleSort = tup;
            this.tupleFuncDec = tup.MkDecl;
            this.tupleTests = tup.FieldDecls;

            this.alph = z3p.TT.MkRankedAlphabet(def.id.text, this.tupleSort, alphSymbols.ToArray(), alphArities.ToArray());
            this.alphName = def.id.text;
            this.alphSort = this.alph.AlphabetSort;

        }
    }

    public class TreeClassDef
    {
        public Z3Provider z3p;

        public RankedAlphabetSort alphabet;

        public Dictionary<string, IEnumerable<Expr>> trees;
        public Dictionary<string, TreeTransducer> acceptors;
        public Dictionary<string, TreeTransducer> transducers;

        public TreeClassDef(RankedAlphabetSort ras, Z3Provider z3p)
        {
            this.alphabet = ras;
            this.z3p = z3p;
            this.trees = new Dictionary<string, IEnumerable<Expr>>();
            this.acceptors = new Dictionary<string, TreeTransducer>();
            this.transducers = new Dictionary<string, TreeTransducer>();
        }        

        /// <summary>
        /// Generate an acceptor rooted in def from a compound language definition and adds it to the list of acceptors
        /// </summary>
        public bool AddAcceptor(LangDefDef def, FastTransducerInstance fti, Dictionary<string, Def> defs)
        {
            var acceptor = OperationTranGen.getTreeAutomatonFromExpr(def.expr, fti, defs);
            acceptors.Add(def.func.name.text, acceptor);
            if (acceptor.IsEmpty)
                fti.fastLog.WriteLog(LogLevel.Normal, string.Format("the transducer '{0}' is empty", def.func.name.text));
            return true;
        }

        /// <summary>
        /// Generate a transducer rooted in def from a transducer definition
        /// </summary>
        public bool AddTransducer(Def def, FastTransducerInstance fti, Dictionary<string, Def> defs)
        {
            #region Accessory variables
            int ruleNumb;
            int[][] givenExpr;
            Expr whereExpr;
            Expr toExpr;
            int currentState = 0;
            Def untdef;
            TransDef currentDef;
            LangDef currentLangDef;
            bool isTrans = true;

            //The alphabet of the transducer
            List<GuardedExp> cases = new List<GuardedExp>();
            RankedAlphabet currentAlphabet = alphabet.alph;
            List<TreeRule> transducerRules = new List<TreeRule>();
            List<string> exploredStates = new List<string>();
            List<string> reachedStates = new List<string>();

            RankedAlphabetSort outputAlphabet = null;

            int childPosition;
            String name = "";
            String defName = "";
            List<FastToken> children;

            int[][] nextStates;
            List<int>[] nextStatesL;
            #endregion

            List<Def> definitionQueue = new List<Def>();

            //Find the output alphabet

            #region pick proper outputAlph
            if (def.kind == DefKind.Trans)
            {
                TransDef tdef = def as TransDef;
                defName = tdef.func.name.text;
                outputAlphabet = fti.alphabets[tdef.range.name.text];
            }
            if (def.kind == DefKind.Lang)
            {
                LangDef ldef = def as LangDef;
                defName = ldef.func.name.text;
                outputAlphabet = fti.alphabets[ldef.domain.name.text];
            }
            #endregion

            reachedStates.Add(defName);
            definitionQueue.Add(def);

            while (definitionQueue.Count > 0)
            {
                //remove state from the queue and mark it as explored                
                untdef = definitionQueue.ElementAt<Def>(0);
                definitionQueue.RemoveAt(0);
                #region pick proper type for def
                if (untdef.kind == DefKind.Trans)
                {
                    isTrans = true;
                    currentDef = (TransDef)untdef;
                    exploredStates.Add(currentDef.func.name.text);
                    cases = currentDef.cases;
                }
                if (untdef.kind == DefKind.Lang)
                {
                    isTrans = false;
                    currentLangDef = (LangDef)untdef;
                    exploredStates.Add(currentLangDef.func.name.text);
                    cases = currentLangDef.cases;
                }
                #endregion

                ruleNumb = 0;
                List<int> unsatRules = new List<int>();
                foreach (var defCase in cases)
                {
                    ruleNumb++;
                    children = defCase.pat.children;

                    //Compute the Z3 term for the where condition
                    whereExpr = GenerateZ3ExprFromWhereExpr(defCase.where, fti).Simplify();

                    if (z3p.IsSatisfiable(whereExpr))
                    {
                        //Compute the termSet array corresponding to the given
                        givenExpr = new int[children.Count][];

                        #region Find next states and add them to the queue and add the corresponding number to next states
                        nextStates = new int[children.Count][];
                        nextStatesL = new List<int>[children.Count];
                        for (int i = 0; i < children.Count; i++)
                            nextStatesL[i] = new List<int>();
                        foreach (var expr in defCase.given)
                        {
                            if (expr.kind == FExpKind.App)
                            {
                                name = ((AppExp)expr).func.name.text;
                                //Add the state to the queue if it has not been reached yet
                                if (!reachedStates.Contains(name))
                                {
                                    reachedStates.Add(name);
                                    var langDef = defs[name] as LangDef;
                                    definitionQueue.Add(langDef);
                                      
                                }
                                childPosition = 0;
                                foreach (var child in children)
                                {
                                    if (child.text == ((AppExp)expr).args[0].token.text)
                                    {
                                        break;
                                    }
                                    childPosition++;
                                }
                                nextStatesL[childPosition].Add(reachedStates.IndexOf(name));
                            }
                        }

                        #endregion

                        //Compute the Z3 term for the to condition
                        if (isTrans)
                            toExpr = GenerateZ3ExprFromToExpr(defCase.to, outputAlphabet, children, currentState, reachedStates, definitionQueue, defs, fti, nextStatesL).Simplify();
                        else
                            toExpr = null;

                        //Next states will contain the outgoing states of the automata
                        for (int i = 0; i < children.Count; i++)
                            nextStates[i] = nextStatesL[i].ToArray();
                        transducerRules.Add(z3p.TT.MkTreeRule(currentAlphabet, outputAlphabet.alph, currentState, defCase.pat.symbol.text, whereExpr, toExpr, nextStates));
                    }
                    else
                    {
                        unsatRules.Add(ruleNumb - 1);
                        //Console.WriteLine("Rule " + ruleNumb + " is unsat in " + untdef.id.text);
                        fti.fastLog.WriteLog(LogLevel.Normal, string.Format("Rule {0} in '{1}' is not satisfiable", +ruleNumb, untdef.id.text));
                    }

                }
                int removed = 0;
                foreach (int ind in unsatRules)
                {
                    cases.RemoveAt(ind-removed);
                    removed++;
                }
                currentState++;
            }

            //Generate transducer
            try
            {
                var automaton = z3p.TT.MkTreeAutomaton(0, currentAlphabet, outputAlphabet.alph, transducerRules);
                automaton = automaton.RemoveMultipleInitialStates();
                if (def.kind == DefKind.Trans)
                {
                    transducers.Add(defName, automaton);
                    if (automaton.IsEmpty)
                        fti.fastLog.WriteLog(LogLevel.Normal, string.Format("the transducer '{0}' is empty", defName));
                }
                else
                {
                    acceptors.Add(defName, automaton);
                    if (automaton.IsEmpty)
                        fti.fastLog.WriteLog(LogLevel.Normal, string.Format("the language '{0}' is empty", defName));
                }
            }
            catch (AutomataException e)
            {
                Console.WriteLine(e);
                throw e;
            }
            return true;
        }

        /// <summary>
        /// Generate a tree from a tree definition
        /// </summary>
        public bool AddTree(TreeDef def, FastTransducerInstance fti, Dictionary<string, Def> defs)
        {
            switch (def.tdkind)
            {
                case TreeDefKind.Witness:
                    {
                        TreeWitnessDef ce = def as TreeWitnessDef;
                        TreeTransducer language = OperationTranGen.getTreeAutomatonFromExpr(ce.language, fti, defs);
                        var res = language.GenerateWitness();
                        if(res!=null)
                            this.trees.Add(def.func.name.text, new Expr[] { res });
                        else
                            this.trees.Add(def.func.name.text, new Expr[] { });
                        break;
                    }
                case TreeDefKind.Apply:
                    {
                        TreeAppDef ce = def as TreeAppDef;
                        IEnumerable<Expr> t = null;
                        if (ce.expr.kind == FExpKind.Var)
                        {
                            foreach (var treeDef in fti.treeDefinitions)
                                if (treeDef.Value.trees.TryGetValue(ce.expr.token.text, out t))
                                    break;
                            if (t == null)
                            {
                                this.trees.Add(def.func.name.text, null);
                                return true;
                            }
                        }
                        else
                        {
                            Expr t1 = GenerateZ3ExprFromToExpr(ce.expr, alphabet, null, 0, null, null, defs, fti, null).Simplify();
                            List<Expr> tl = new List<Expr>();
                            tl.Add(t1);
                            t = tl;
                        }

                        TreeTransducer a = OperationTranGen.getTreeAutomatonFromExpr(ce.transducer, fti, defs);

                        var results = a.Apply(t);
                        this.trees.Add(def.func.name.text, results.ToList());
                        break;
                    }
                case TreeDefKind.Tree:
                    {
                        TreeExpDef ce = def as TreeExpDef;
                        IEnumerable<Expr> t = null;
                        if (ce.expr.kind == FExpKind.Var)
                        {
                            foreach (var treeDef in fti.treeDefinitions)
                                if (treeDef.Value.trees.TryGetValue(ce.expr.token.text, out t))
                                    break;
                            if (t == null)
                            {
                                this.trees.Add(def.func.name.text, null);
                                return true;
                            }
                        }
                        else
                        {
                            Expr t1 = GenerateZ3ExprFromToExpr(ce.expr, alphabet, null, 0, null, null, defs, fti, null).Simplify();
                            List<Expr> tl = new List<Expr>();
                            tl.Add(t1);
                            t = tl;
                        }
                        this.trees.Add(def.func.name.text, t);
                        break;
                    }
            }
            return true;
        }

        /// <summary>
        /// Generate a transducer rooted in def from a compound transducer definition and adds it to the list of transducers
        /// </summary>
        public bool AddTransducer(TransDefDef def, FastTransducerInstance fti, Dictionary<string, Def> defs)
        {
            var transducer = OperationTranGen.getTreeAutomatonFromExpr(def.expr, fti, defs);
            transducers.Add(def.func.name.text, transducer);
            if (transducer.IsEmpty)
                fti.fastLog.WriteLog(LogLevel.Normal, string.Format("the transducer '{0}' is empty", def.func.name.text));
            return true;
        }

        


        /// <summary>
        /// Transform a fast where (also used for the record's values) expression into the corresponding Z3 term
        /// </summary>
        #region Z3 Expr Generation for guard expressions
        public Expr GenerateZ3ExprFromWhereExpr(FExp expr, FastTransducerInstance fti)
        {
            switch (expr.kind)
            {
                case (FExpKind.App):
                    return GenerateZ3WhereExpr((AppExp)expr, fti);
                case (FExpKind.Value):
                    return GenerateZ3WhereExpr((Value)expr, fti);
                case (FExpKind.Var):
                    return GenerateZ3WhereExpr((Variable)expr, fti);
            }
            return null;
        }

        private Expr GenerateZ3WhereExpr(AppExp expr, FastTransducerInstance fti)
        {
            List<Expr> termList = new List<Expr>();
            switch (expr.func.name.Kind)
            {
                case (Tokens.EQ):
                    {
                        return z3p.MkEq(GenerateZ3ExprFromWhereExpr(expr.args[0], fti), GenerateZ3ExprFromWhereExpr(expr.args[1], fti));
                    }
                case (Tokens.AND):
                    {
                        foreach (var arg in expr.args)
                            termList.Add(GenerateZ3ExprFromWhereExpr(arg, fti));
                        return z3p.MkAnd(termList.ToArray());
                    }
                //case (Tokens.XOR):
                //    {
                //        return z3p.Z3.MkXor((BoolExpr)GenerateZ3ExprFromWhereExpr(expr.args[0], fti), (BoolExpr)GenerateZ3ExprFromWhereExpr(expr.args[1], fti));
                //    }
                case (Tokens.NOT):
                    {
                        return z3p.MkNot(GenerateZ3ExprFromWhereExpr(expr.args[0], fti));
                    }
                case (Tokens.OR):
                    {
                        foreach (var arg in expr.args)
                            termList.Add(GenerateZ3ExprFromWhereExpr(arg, fti));
                        return z3p.MkOr(termList.ToArray());
                    }
                case (Tokens.IMPLIES):
                    {
                        return z3p.MkOr(z3p.MkNot(GenerateZ3ExprFromWhereExpr(expr.args[0], fti)), GenerateZ3ExprFromWhereExpr(expr.args[1], fti));
                    }
                case (Tokens.PLUS):
                    {
                        foreach (var arg in expr.args)
                            termList.Add(GenerateZ3ExprFromWhereExpr(arg, fti));
                        if (z3p.GetSort(termList[0]).SortKind == Z3_sort_kind.Z3_BV_SORT)
                            return z3p.MkBvAddMany(termList.ToArray());
                        return z3p.MkAdd(termList.ToArray());
                    }
                case (Tokens.DIV):
                    {
                        return z3p.MkCharDiv(GenerateZ3ExprFromWhereExpr(expr.args[0], fti), GenerateZ3ExprFromWhereExpr(expr.args[1], fti));
                    }
                case (Tokens.MINUS):
                    {
                        return z3p.MkCharSub(GenerateZ3ExprFromWhereExpr(expr.args[0], fti), GenerateZ3ExprFromWhereExpr(expr.args[1], fti));
                    }
                case (Tokens.TIMES):
                    {
                        return z3p.MkCharMul(GenerateZ3ExprFromWhereExpr(expr.args[0], fti), GenerateZ3ExprFromWhereExpr(expr.args[1], fti));
                    }
                case (Tokens.LT):
                    {
                        return z3p.MkCharLt(GenerateZ3ExprFromWhereExpr(expr.args[0], fti), GenerateZ3ExprFromWhereExpr(expr.args[1], fti));
                    }
                case (Tokens.LE):
                    {
                        return z3p.MkCharLe(GenerateZ3ExprFromWhereExpr(expr.args[0], fti), GenerateZ3ExprFromWhereExpr(expr.args[1], fti));
                    }
                case (Tokens.GT):
                    {
                        return z3p.MkCharGt(GenerateZ3ExprFromWhereExpr(expr.args[0], fti), GenerateZ3ExprFromWhereExpr(expr.args[1], fti));
                    }
                case (Tokens.GE):
                    {
                        return z3p.MkCharGe(GenerateZ3ExprFromWhereExpr(expr.args[0], fti), GenerateZ3ExprFromWhereExpr(expr.args[1], fti));
                    }
                case (Tokens.MOD):
                    {
                        return z3p.MkMod(GenerateZ3ExprFromWhereExpr(expr.args[0], fti), GenerateZ3ExprFromWhereExpr(expr.args[1], fti));
                    }
                case (Tokens.ITE):
                    {
                        return z3p.MkIte(GenerateZ3ExprFromWhereExpr(expr.args[0], fti), GenerateZ3ExprFromWhereExpr(expr.args[1], fti), GenerateZ3ExprFromWhereExpr(expr.args[2], fti));
                    }
                default:
                    {
                        foreach (var f in fti.functions)
                        {
                            if (expr.func.name.text == f.name)
                            {
                                List<Expr> argsExprs = new List<Expr>();
                                foreach (var a in expr.args)
                                {
                                    argsExprs.Add(GenerateZ3ExprFromWhereExpr(a, fti));
                                }
                                return z3p.ApplySubstitution(f.functionDef, f.variableExprs.Values.ToArray(), argsExprs.ToArray<Expr>());
                                //return z3p.MkApp(f.funcDecl, argsExprs.ToArray<Expr>());
                            }
                        }
                        return null;
                    }
            }
        }

        private Expr GenerateZ3WhereExpr(Value expr, FastTransducerInstance fti)
        {
            switch (expr.sort.kind)
            {
                case (FastSortKind.Char):
                    {
                        return z3p.MkNumeral(expr.token.ToInt(), z3p.CharSort);
                    }
                case (FastSortKind.Bool):
                    {
                        if (expr.token.Kind == Tokens.TRUE)
                            return z3p.True;
                        if (expr.token.Kind == Tokens.FALSE)
                            return z3p.False;
                        break;
                    }
                case (FastSortKind.Int):
                    {
                        return z3p.MkInt(expr.token.ToInt());
                    }
                case (FastSortKind.Real):
                    {
                        String[] parts = expr.token.text.Split('.');
                        if (parts.Length == 2 && Regex.IsMatch(parts[1],"[0]+"))
                        {
                            return z3p.Z3.MkReal(parts[0]);
                        }
                        return z3p.Z3.MkReal(expr.token.text);
                    }
                case (FastSortKind.String):
                    {
                        return z3p.MkListFromString(expr.token.text.Substring(1, expr.token.text.Length - 2), z3p.CharSort);
                    }
                case (FastSortKind.Tree):
                    {
                        String[] lr = expr.token.text.Split('.');
                        return z3p.GetEnumElement(lr[0], lr[1]);
                    }
            }
            throw new Exception("Unexpected Sort");
        }

        private Expr GenerateZ3WhereExpr(Variable expr, FastTransducerInstance fti)
        {
            foreach (var c in fti.consts)
            {
                if (c.name == expr.token.text)
                    return c.value;
            }
            return z3p.MkProj(alphabet.tupleKeys.IndexOf(expr.token.text), alphabet.alph.AttrVar);
        }
        #endregion

        /// <summary>
        /// Transform a fast To expression into the corresponding Z3 term
        /// </summary>
        #region Z3 Expr Generation for output expressions
        public Expr GenerateZ3ExprFromToExpr(FExp expr, RankedAlphabetSort outputAlph, List<FastToken> children, int from, List<string> reachedStates, List<Def> queue, Dictionary<string, Def> defs, FastTransducerInstance fti, List<int>[] nextStatesL)
        {
            switch (expr.kind)
            {
                case (FExpKind.App):
                    return GenerateZ3ToExpr((AppExp)expr, outputAlph, children, from, reachedStates, queue, defs, fti, nextStatesL);
                case (FExpKind.Value):
                    return GenerateZ3ToExpr((Value)expr, outputAlph);
                case (FExpKind.Var):
                    return GenerateZ3ToExpr((Variable)expr, outputAlph, children);
            }
            return null;
        }

        private Expr GenerateZ3ToExpr(AppExp expr, RankedAlphabetSort outputAlph, List<FastToken> children, int from, List<string> reachedStates, List<Def> queue, Dictionary<string, Def> defs, FastTransducerInstance fti, List<int>[] nextStatesL)
        {
            List<Expr> termList = new List<Expr>();
            switch (expr.func.name.Kind)
            {
                #region predefined functions
                case (Tokens.EQ):
                    {
                        return z3p.MkEq(GenerateZ3ExprFromToExpr(expr.args[0], outputAlph, children, from, reachedStates, queue, defs, fti, nextStatesL), GenerateZ3ExprFromToExpr(expr.args[1], outputAlph, children, from, reachedStates, queue, defs, fti, nextStatesL));
                    }
                case (Tokens.AND):
                    {
                        foreach (var arg in expr.args)
                            termList.Add(GenerateZ3ExprFromToExpr(arg, outputAlph, children, from, reachedStates, queue, defs, fti, nextStatesL));
                        return z3p.MkAnd(termList.ToArray());
                    }
                //case ("xor"):
                //    {
                //        if (expr.args.Count > 2)
                //            throw new Exception("Too many arguments");
                //        return z3p.Z3.MkXor((BoolExpr)GenerateZ3ExprFromToExpr(expr.args[0], outputAlph, children, from, reachedStates, queue, defs, fti, nextStatesL), (BoolExpr)GenerateZ3ExprFromToExpr(expr.args[1], outputAlph, children, from, reachedStates, queue, defs, fti, nextStatesL));
                //    }
                case (Tokens.NOT):
                    {
                        return z3p.MkNot(GenerateZ3ExprFromToExpr(expr.args[0], outputAlph, children, from, reachedStates, queue, defs, fti, nextStatesL));
                    }
                case (Tokens.OR):
                    {
                        foreach (var arg in expr.args)
                            termList.Add(GenerateZ3ExprFromToExpr(arg, outputAlph, children, from, reachedStates, queue, defs, fti, nextStatesL));
                        return z3p.MkOr(termList.ToArray());
                    }
                case (Tokens.IMPLIES):
                    {
                        return z3p.MkOr(z3p.MkNot(GenerateZ3ExprFromToExpr(expr.args[0], outputAlph, children, from, reachedStates, queue, defs, fti, nextStatesL)), GenerateZ3ExprFromToExpr(expr.args[1], outputAlph, children, from, reachedStates, queue, defs, fti, nextStatesL));
                    }
                case (Tokens.PLUS):
                    {
                        foreach (var arg in expr.args)
                            termList.Add(GenerateZ3ExprFromToExpr(arg, outputAlph, children, from, reachedStates, queue, defs, fti, nextStatesL));
                        if (z3p.GetSort(termList[0]).SortKind == Z3_sort_kind.Z3_BV_SORT)
                            return z3p.MkBvAddMany(termList.ToArray());
                        return z3p.MkAdd(termList.ToArray());
                    }
                case (Tokens.DIV):
                    {
                        return z3p.MkCharDiv(GenerateZ3ExprFromToExpr(expr.args[0], outputAlph, children, from, reachedStates, queue, defs, fti, nextStatesL), GenerateZ3ExprFromToExpr(expr.args[1], outputAlph, children, from, reachedStates, queue, defs, fti, nextStatesL));
                    }
                case (Tokens.MINUS):
                    {
                        return z3p.MkCharSub(GenerateZ3ExprFromToExpr(expr.args[0], outputAlph, children, from, reachedStates, queue, defs, fti, nextStatesL), GenerateZ3ExprFromToExpr(expr.args[1], outputAlph, children, from, reachedStates, queue, defs, fti, nextStatesL));
                    }
                case (Tokens.TIMES):
                    {
                        return z3p.MkCharMul(GenerateZ3ExprFromToExpr(expr.args[0], outputAlph, children, from, reachedStates, queue, defs, fti, nextStatesL), GenerateZ3ExprFromToExpr(expr.args[1], outputAlph, children, from, reachedStates, queue, defs, fti, nextStatesL));
                    }
                case (Tokens.LT):
                    {
                        return z3p.MkCharLt(GenerateZ3ExprFromToExpr(expr.args[0], outputAlph, children, from, reachedStates, queue, defs, fti, nextStatesL), GenerateZ3ExprFromToExpr(expr.args[1], outputAlph, children, from, reachedStates, queue, defs, fti, nextStatesL));
                    }
                case (Tokens.LE):
                    {
                        return z3p.MkCharLe(GenerateZ3ExprFromToExpr(expr.args[0], outputAlph, children, from, reachedStates, queue, defs, fti, nextStatesL), GenerateZ3ExprFromToExpr(expr.args[1], outputAlph, children, from, reachedStates, queue, defs, fti, nextStatesL));
                    }
                case (Tokens.GT):
                    {
                        return z3p.MkCharGt(GenerateZ3ExprFromToExpr(expr.args[0], outputAlph, children, from, reachedStates, queue, defs, fti, nextStatesL), GenerateZ3ExprFromToExpr(expr.args[1], outputAlph, children, from, reachedStates, queue, defs, fti, nextStatesL));
                    }
                case (Tokens.GE):
                    {
                        return z3p.MkCharGe(GenerateZ3ExprFromToExpr(expr.args[0], outputAlph, children, from, reachedStates, queue, defs, fti, nextStatesL), GenerateZ3ExprFromToExpr(expr.args[1], outputAlph, children, from, reachedStates, queue, defs, fti, nextStatesL));
                    }
                case (Tokens.MOD):
                    {
                        return z3p.MkMod(GenerateZ3ExprFromToExpr(expr.args[0], outputAlph, children, from, reachedStates, queue, defs, fti, nextStatesL), GenerateZ3ExprFromToExpr(expr.args[1], outputAlph, children, from, reachedStates, queue, defs, fti, nextStatesL));
                    }
                case (Tokens.ITE):
                    {
                        return z3p.MkIte(GenerateZ3ExprFromToExpr(expr.args[0], outputAlph, children, from, reachedStates, queue, defs, fti, nextStatesL), GenerateZ3ExprFromToExpr(expr.args[1], outputAlph, children, from, reachedStates, queue, defs, fti, nextStatesL), GenerateZ3ExprFromToExpr(expr.args[2], outputAlph, children, from, reachedStates, queue, defs, fti, nextStatesL));
                    }
                #endregion
                default:
                    {
                        if (expr.IsTranDef)
                        {
                            //The application is a transduction applied to a subtree
                            //Check if the current trans has been processed, if not add it to the queue
                            String name = ((AppExp)expr).func.name.text;
                            int pos = reachedStates.IndexOf(name);
                            if (pos == -1)
                            {
                                reachedStates.Add(name);
                                var transDef = defs[name];
                                queue.Add((TransDef)transDef);
                                pos = reachedStates.Count-1;
                            }
                            
                            
                            //Find the child number and return the transition
                            int childPosition = 1;
                            foreach (var child in children)
                            {
                                if (child.text == ((AppExp)expr).args[0].token.text)
                                {
                                    break;
                                }
                                childPosition++;
                            }
                            if (!nextStatesL[childPosition - 1].Contains(pos))
                                nextStatesL[childPosition - 1].Add(pos);

                            return alphabet.alph.MkTrans(outputAlph.alph, pos, childPosition);
                        }

                        //It means the app is a constructor
                        Expr[] terms = new Expr[expr.args.Count-1];
                        var terms_0 = GenerateZ3ToExpr((RecordExp)expr.args.ElementAt<FExp>(0), outputAlph, fti);
                        for (int i = 1; i < expr.args.Count; i++)
                        {
                            terms[i-1] = GenerateZ3ExprFromToExpr(expr.args.ElementAt<FExp>(i), outputAlph, children, from, reachedStates, queue, defs, fti, nextStatesL);
                        }
                        return outputAlph.alph.MkTree(expr.func.name.text, terms_0, terms);

                    }
            }
        }

        private Expr GenerateZ3ToExpr(RecordExp expr, RankedAlphabetSort outputAlph, FastTransducerInstance fti)
        {
            Expr[] terms = new Expr[expr.args.Count];
            for (int i = 0; i < expr.args.Count; i++)
            {
                terms[i] = GenerateZ3ExprFromWhereExpr(expr.args.ElementAt<FExp>(i), fti);
            }
            return z3p.MkApp(outputAlph.tupleFuncDec, terms);
        }

        private Expr GenerateZ3ToExpr(Value expr, RankedAlphabetSort outputAlph)
        {
            switch (expr.sort.kind)
            {
                case (FastSortKind.Bool):
                    {
                        if (expr.token.Kind == Tokens.TRUE)
                            return z3p.True;
                        if (expr.token.Kind == Tokens.FALSE)
                            return z3p.False;
                        break;
                    }
                case (FastSortKind.Int):
                    {
                        return z3p.MkInt(Convert.ToInt32(expr.token.text));
                    }
                case (FastSortKind.Real):
                    {
                        return z3p.Z3.MkReal(expr.token.text);
                    }
                case (FastSortKind.String):
                    {
                        return z3p.MkListFromString(expr.token.text.Substring(1, expr.token.text.Length - 2), z3p.CharSort);
                    }
                case (FastSortKind.Tree):
                    {
                        String[] lr = expr.token.text.Split('.');
                        return z3p.GetEnumElement(lr[0], lr[1]);
                    }
            }
            throw new Exception("Unexpected Sort");
        }

        private Expr GenerateZ3ToExpr(Variable expr, RankedAlphabetSort outputAlph, List<FastToken> children)
        {
            //Find the child number and return the transition
            int childPosition = 1;
            foreach (var child in children)
            {
                if (child.text == ((Variable)expr).token.text)
                {
                    break;
                }
                childPosition++;
            }
            return alphabet.alph.ChildVar(childPosition);
        }
        #endregion

    }

    public static class OperationTranGen
    {
        internal static TreeTransducer getTreeAutomatonFromExpr(BuiltinTransExp expr, FastTransducerInstance fti, Dictionary<string, Def> defs)
        {
            Stopwatch stopwatch = new Stopwatch();
            switch (expr.kind)
            {
                case (BuiltinTransExpKind.Composition):
                    {
                        var castExpr = expr as CompositionExp;
                        var a = getTreeAutomatonFromExpr(castExpr.arg1, fti, defs);
                        var b = getTreeAutomatonFromExpr(castExpr.arg2, fti, defs);
                        stopwatch.Start();
                        var comp = TreeTransducer.ComposeR(a, b).RemoveMultipleInitialStates();
                        stopwatch.Stop();
                        fti.fastLog.WriteLog(LogLevel.Normal, string.Format("compose of {0} and {1}: {2} ms", castExpr.arg1.ToString(), castExpr.arg2.ToString(), stopwatch.ElapsedMilliseconds));
                        return comp;
                    }
                case (BuiltinTransExpKind.RestrictionInp):
                    {
                        var castExpr = expr as RestrictionInpExp;
                        var a = getTreeAutomatonFromExpr(castExpr.arg1, fti, defs);
                        var b = getTreeAutomatonFromExpr(castExpr.arg2, fti, defs);
                        stopwatch.Start();
                        var res = a.RestrictDomain(b).RemoveMultipleInitialStates();
                        stopwatch.Stop();
                        fti.fastLog.WriteLog(LogLevel.Normal, string.Format("compose of {0} and {1}: {2} ms", castExpr.arg1.ToString(), castExpr.arg2.ToString(), stopwatch.ElapsedMilliseconds));
                        return res;
                    }
                case (BuiltinTransExpKind.RestrictionOut):
                    {
                        var castExpr = expr as RestrictionOutExp;
                        var a = getTreeAutomatonFromExpr(castExpr.arg1, fti, defs);
                        var b = getTreeAutomatonFromExpr(castExpr.arg2, fti, defs);
                        stopwatch.Start();
                        var res = a.RestrictRange(b).RemoveMultipleInitialStates();
                        stopwatch.Stop();
                        fti.fastLog.WriteLog(LogLevel.Normal, string.Format("compose of {0} and {1}: {2} ms", castExpr.arg1.ToString(), castExpr.arg2.ToString(), stopwatch.ElapsedMilliseconds));
                        return res;
                    }
                case (BuiltinTransExpKind.Var):
                    {
                        var castExpr = expr as TransNameExp;
                        return fti.treeDefinitions[castExpr.domain.name.text].transducers[castExpr.func.name.text];
                    }
                default:
                    {
                        throw new FastException(FastExceptionKind.NotImplemented);
                    }
            }
            throw new FastException(FastExceptionKind.InternalError);
        }

        internal static TreeTransducer getTreeAutomatonFromExpr(BuiltinLangExp expr, FastTransducerInstance fti, Dictionary<string, Def> defs)
        {
            Stopwatch stopwatch = new Stopwatch();
            switch (expr.kind)
            {
                case (BuiltinLangExpKind.Intersection):
                    {
                        var castExpr = expr as IntersectionExp;
                        var a = getTreeAutomatonFromExpr(castExpr.arg1, fti, defs);
                        var b = getTreeAutomatonFromExpr(castExpr.arg2, fti, defs);
                        stopwatch.Start();
                        var inters = a.Intersect(b).RemoveMultipleInitialStates();
                        stopwatch.Stop();
                        fti.fastLog.WriteLog(LogLevel.Normal, string.Format("intersection of {0} and {1}: {2} ms", castExpr.arg1.ToString(), castExpr.arg2.ToString(), stopwatch.ElapsedMilliseconds));
                        return inters;
                    }
                case (BuiltinLangExpKind.Difference):
                    {
                        var castExpr = expr as DifferenceExp;
                        var a = getTreeAutomatonFromExpr(castExpr.arg1, fti, defs);
                        var b = getTreeAutomatonFromExpr(castExpr.arg2, fti, defs);
                        stopwatch.Start();
                        var inters = a.Intersect(b.Complement()).RemoveMultipleInitialStates();
                        stopwatch.Stop();
                        fti.fastLog.WriteLog(LogLevel.Normal, string.Format("difference of {0} and {1}: {2} ms", castExpr.arg1.ToString(), castExpr.arg2.ToString(), stopwatch.ElapsedMilliseconds));
                        return inters;
                    }
                case (BuiltinLangExpKind.Union):
                    {
                        var castExpr = expr as UnionExp;
                        var a = getTreeAutomatonFromExpr(castExpr.arg1, fti, defs);
                        var b = getTreeAutomatonFromExpr(castExpr.arg2, fti, defs);
                        stopwatch.Start();
                        var res = a.Union(b).RemoveMultipleInitialStates();
                        stopwatch.Stop();
                        fti.fastLog.WriteLog(LogLevel.Normal, string.Format("union of {0} and {1}: {2} ms", castExpr.arg1.ToString(), castExpr.arg2.ToString(), stopwatch.ElapsedMilliseconds));
                        return res;
                    }
                case (BuiltinLangExpKind.Domain):
                    {
                        var castExpr = expr as DomainExp;
                        var a = getTreeAutomatonFromExpr(castExpr.arg1, fti, defs);
                        stopwatch.Start();
                        var res = a.ComputeDomainAcceptor().RemoveMultipleInitialStates();
                        stopwatch.Stop();
                        fti.fastLog.WriteLog(LogLevel.Normal, string.Format("domain of {0}: {1} ms", castExpr.arg1.ToString(), stopwatch.ElapsedMilliseconds));
                        return res;
                    }
                case (BuiltinLangExpKind.Preimage):
                    {
                        var castExpr = expr as PreimageExp;
                        var t = getTreeAutomatonFromExpr(castExpr.arg1, fti, defs);
                        var output = getTreeAutomatonFromExpr(castExpr.arg2, fti, defs);
                        stopwatch.Start();
                        var res = t.RestrictRange(output).ComputeDomainAcceptor().RemoveMultipleInitialStates();
                        stopwatch.Stop();
                        fti.fastLog.WriteLog(LogLevel.Normal, string.Format("preimage of {0}: {1} ms", castExpr.arg1.ToString(), stopwatch.ElapsedMilliseconds));
                        return res;
                    }
                case (BuiltinLangExpKind.Complement):
                    {
                        var castExpr = expr as ComplementExp;
                        var a =  getTreeAutomatonFromExpr(castExpr.arg1, fti, defs);
                        stopwatch.Start();
                        var res = a.Complement().RemoveMultipleInitialStates();
                        stopwatch.Stop();
                        fti.fastLog.WriteLog(LogLevel.Normal, string.Format("complement of {0}: {1} ms", castExpr.arg1.ToString(), stopwatch.ElapsedMilliseconds));
                        return res;
                    }
                case (BuiltinLangExpKind.Minimization):
                    {
                        var castExpr = expr as MinimizeExp;
                        var a = getTreeAutomatonFromExpr(castExpr.arg1, fti, defs);
                        stopwatch.Start();
                        var res = a.Minimize().RemoveMultipleInitialStates();
                        stopwatch.Stop();
                        fti.fastLog.WriteLog(LogLevel.Normal, string.Format("minimization of {0}: {1} ms", castExpr.arg1.ToString(), stopwatch.ElapsedMilliseconds));
                        return res;
                    }
                case (BuiltinLangExpKind.Var):
                    {
                        var castExpr = expr as LangNameExp;
                        return fti.treeDefinitions[castExpr.domain.name.text].acceptors[castExpr.func.name.text];
                    }
                default:
                    {
                        throw new FastException(FastExceptionKind.NotImplemented);
                    }
            }
            throw new FastException(FastExceptionKind.InternalError);
        }    
    }

}
