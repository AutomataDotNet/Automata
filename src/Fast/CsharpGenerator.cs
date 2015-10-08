using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Fast.AST;

namespace Microsoft.Fast
{
    public class CsharpGenerator
    {

        /// <summary>
        /// Generates C# code into the file "namefile" from a fast program with the namespace "ns"
        /// </summary>
        /// <param name="fpg">fast program</param> 
        /// <param name="namefile">path of the output file</param> 
        /// <param name="ns">namespace for the generated file</param> 
        public static bool GenerateCode(FastPgm fpg, StringBuilder sb)
        {
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("namespace Microsoft.Fast.Sample{");
            sb.AppendLine();

            GenerateEnums(fpg.defs, sb);
            GenerateAlphabets(fpg.defs, sb);
            GenerateTreeClasses(fpg.defs, sb);

            sb.AppendLine("}");
            return true;
        }

        /// <summary>
        /// Generates C# code into the file "namefile" from a fast program with the namespace "ns"
        /// </summary>
        /// <param name="fpg">fast program</param> 
        /// <param name="namefile">path of the output file</param> 
        /// <param name="ns">namespace for the generated file</param> 
        public static bool GenerateCode(FastPgm fpg, StringBuilder sb, string ns)
        {
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("namespace " + ns + "{");
            sb.AppendLine();

            GenerateEnums(fpg.defs, sb);
            GenerateAlphabets(fpg.defs, sb);
            GenerateTreeClasses(fpg.defs, sb);

            sb.AppendLine("}");
            return true;
        }

        /// <summary>
        /// Generates C# code into the file "namefile" from a fast program with the namespace "ns"
        /// </summary>
        /// <param name="fpg">fast program</param> 
        /// <param name="namefile">path of the output file</param> 
        /// <param name="ns">namespace for the generated file</param> 
        public static bool GenerateCode(FastPgm fpg, String namefile, String ns)
        {
            StreamWriter file = new System.IO.StreamWriter(namefile, false);
            StringBuilder sb = new StringBuilder();
            GenerateCode(fpg, sb, ns);
            file.Write(sb.ToString());
            file.Close();
            return true;
        }

        /// <summary>
        /// Generates C# code into the file "namefile" from a fast program 
        /// </summary>
        /// <param name="fpg">fast program</param> 
        /// <param name="namefile">path of the output file</param> 
        public static bool GenerateCode(FastPgm fpg, String namefile)
        {
            StreamWriter file = new System.IO.StreamWriter(namefile, false);
            StringBuilder sb = new StringBuilder();
            GenerateCode(fpg, sb);
            file.Write(sb.ToString());
            file.Close();
            return true;
        }


        //PRIVATE GENERATORS
        //Generate C# for all the enums in the program
        private static bool GenerateEnums(List<Def> defs, StringBuilder sb)
        {
            foreach (var def in defs)
            {
                switch (def.kind)
                {
                    case (DefKind.Enum):
                        {
                            if (!GenerateEnum((EnumDef)def, sb))
                                return false;
                            break;
                        }
                }
            }
            return true;
        }

        //Generate C# for all the consts in the program
        private static bool GenerateConsts(List<Def> defs, StringBuilder sb)
        {
            foreach (var def in defs)
            {
                switch (def.kind)
                {
                    case (DefKind.Const):
                        {
                            if (!GenerateConst((ConstDef)def, sb))
                                return false;
                            break;
                        }
                }
            }
            return true;
        }

        //Generate C# for all the functions in the program
        private static bool GenerateFuns(List<Def> defs, StringBuilder sb)
        {
            foreach (var def in defs)
            {
                switch (def.kind)
                {
                    case (DefKind.Function):
                        {
                            if (!GenerateFun((FunctionDef)def, sb))
                                return false;
                            break;
                        }
                }
            }
            return true;
        }

        //Generate C# for all the consts in the program
        private static bool GenerateTrees(List<Def> defs, StringBuilder sb, AlphabetDef aDef)
        {
            foreach (var def in defs)            
                switch (def.kind)
                {
                    case (DefKind.Def):
                        {
                            if(((DefDef)def).ddkind == DefDefKind.Tree)
                                if (((TreeDef)def).domain.name.text.Equals(aDef.id.text))
                                    if (!GenerateTree((TreeDef)def, sb))
                                        return false;
                            break;
                        }
                }
            
            return true;
        }

        //Generate the C# corresponding to one enum
        private static bool GenerateEnum(EnumDef def, StringBuilder sb)
        {
            sb.Append("public enum " + def.id.text + " {");
            foreach (var elem in def.elems)
            {
                if (def.elems.First().Equals(elem))
                {
                    sb.Append(elem);
                }
                else
                {
                    sb.Append(", " + elem);
                }
            }
            sb.AppendLine("}");
            sb.AppendLine();
            return true;
        }

        //Generate the C# corresponding to one constant
        private static bool GenerateConst(ConstDef def, StringBuilder sb)
        {
            sb.Append("public const " + ((def.sort.kind == FastSortKind.Real) ? "double" : def.sort.name.text) + " " +def.id.text + " = ");
            PrintExpr(new List<FastToken>(), def.expr, sb);
            sb.AppendLine(";");
            sb.AppendLine();
            return true;
        }

        //Generate the C# corresponding to one function
        private static bool GenerateFun(FunctionDef def, StringBuilder sb)
        {
            sb.Append("public " + ((def.outputSort.kind == FastSortKind.Real) ? "double" : def.outputSort.name.text) + " " + def.id.text+"(");
            bool needComma=false;
            foreach (var e in def.inputVariables)
            {
                if (needComma)
                    sb.Append(",");
                else
                    needComma = true;
                sb.Append(((e.Value.name.text == "real") ? "double" : e.Value.name.text) + " " + e.Key.text);
            }
            sb.AppendLine("){");
            sb.Append("return ");
            PrintExpr(new List<FastToken>(), def.expr, sb);
            sb.AppendLine(";");
            sb.Append("}");
            sb.AppendLine();
            return true;
        }

        //Generate the C# corresponding to one constant
        private static bool GenerateTree(TreeDef def, StringBuilder sb)
        {
            sb.Append("public static Tree"+def.domain.name.text+" "+def.func.name.text +" = ");

            switch (def.tdkind)
            {
                case TreeDefKind.Apply: throw new FastException(FastExceptionKind.NotImplemented);
                case TreeDefKind.Tree: PrintOutputTree(null, (def as TreeExpDef).expr, def.domain.name.text, sb, false); break;
                case TreeDefKind.Witness: throw new FastException(FastExceptionKind.NotImplemented);
            }
            sb.AppendLine(";");
            sb.AppendLine();
            return true;
        }

        //Generate the C# corresponding to the alphabets
        private static bool GenerateAlphabets(List<Def> defs, StringBuilder sb)
        {
            foreach (var def in defs)
            {
                switch (def.kind)
                {
                    case (DefKind.Alphabet):
                        {
                            if (!GenerateAlphabet((AlphabetDef)def, sb))
                                return false;
                            break;
                        }
                }
            }
            return true;
        }

        //Generate the C# corresponding to a particular alphabet (using enum)
        private static bool GenerateAlphabet(AlphabetDef def, StringBuilder sb)
        {
            //Generate enum for alphabet
            sb.Append("public enum " + def.id.text + " {");
            foreach (var sym in def.symbols)
            {
                if (def.symbols.First().Equals(sym))
                {
                    sb.Append(sym.name);
                }
                else
                {
                    sb.Append(", " + sym.name);
                }
            }
            sb.AppendLine("}");
            sb.AppendLine();

            sb.AppendLine("class " + def.id.text + "Util {");
            sb.AppendLine("public static int getRank(" + def.id.text + " el){");
            sb.AppendLine("switch(el){");
            foreach (var sym in def.symbols)
            {
                sb.AppendLine("case(" + def.id.text + "." + sym.name + "):{");
                sb.AppendLine("return " + (sym.arity - 1) + ";");
                sb.AppendLine("}");
            }
            sb.AppendLine("}");
            sb.AppendLine("return -1;");
            sb.AppendLine("}");
            sb.AppendLine("}");
            sb.AppendLine();

            return true;
        }

        //Generate the C# corresponding to the tree classes (each of them will contain the languages and transductions)
        private static bool GenerateTreeClasses(List<Def> defs, StringBuilder sb)
        {
            foreach (var def in defs)
            {
                switch (def.kind)
                {
                    case (DefKind.Alphabet):
                        {
                            if (!GenerateTreeClass(defs, sb, ((AlphabetDef)def)))
                                return false;
                            break;
                        }
                }
            }
            return true;
        }

        //Generate the C# corresponding to a particular tree class (inferred from the alphabets)
        private static bool GenerateTreeClass(List<Def> defs, StringBuilder sb, AlphabetDef aDef)
        {
            sb.AppendLine("partial class Tree" + aDef.id.text + "{");
            foreach (var s in aDef.attrSort.fieldSorts)
                if (s.Value.kind == FastSortKind.Real)
                    sb.AppendLine("double " + s.Key + ";");
                else
                    sb.AppendLine(s.Value.name + " " + s.Key + ";");
            sb.AppendLine();
            sb.AppendLine(aDef.id.text + " symbol;");
            sb.AppendLine();
            sb.AppendLine("HashSet<string> langsLabel;");
            sb.AppendLine("Tree" + aDef.id.text + "[] children;");
            sb.AppendLine();

            GenerateConsts(defs, sb);
            GenerateFuns(defs, sb);
            GenerateTrees(defs, sb, aDef);
            GenerateConstructor(sb, aDef);           
            GenerateLookahead(defs, sb, aDef);
            GenerateLanguagesAndTransductions(defs, sb, aDef);

            sb.AppendLine("}");
            sb.AppendLine();

            return true;
        }

        //Generate the C# corresponding to the constructor of a Tree class, the MakeTree method and an accessor for the children
        private static bool GenerateConstructor(StringBuilder sb, AlphabetDef aDef)
        {
            sb.Append("private Tree" + aDef.id.text + "(" + aDef.id.text + " symbol");
            foreach (var s in aDef.attrSort.fieldSorts)
            {
                if (s.Value.kind == FastSortKind.Real)
                {
                    sb.Append(", double " + s.Key);
                }
                else
                {
                    sb.Append(", " + s.Value.name + " " + s.Key);
                }
            }
            sb.AppendLine(", Tree" + aDef.id.text + "[] children){");
            sb.AppendLine("this.symbol=symbol;");
            foreach (var s in aDef.attrSort.fieldSorts)
            {
                sb.AppendLine("this." + s.Key + "=" + s.Key + ";");
            }
            sb.AppendLine("this.children=children;");
            sb.AppendLine("this.langsLabel = new HashSet<string>();");
            sb.AppendLine("ComputeLabel();");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("public Tree" + aDef.id.text + " this [int _i]{get { return children[_i]; }}");
            sb.AppendLine();

            sb.Append("public static Tree" + aDef.id.text + " MakeTree(" + aDef.id.text + " symbol");
            foreach (var s in aDef.attrSort.fieldSorts)
            {
                if (s.Value.kind == FastSortKind.Real)
                {
                    sb.Append(", double " + s.Key);
                }
                else
                {
                    sb.Append(", " + s.Value.name + " " + s.Key);
                }
            }
            sb.AppendLine(", Tree" + aDef.id.text + "[] children){");
            sb.AppendLine("if(children.Length==" + aDef.id.text + "Util.getRank(symbol)){");
            sb.Append("Tree" + aDef.id.text + " t = new Tree" + aDef.id.text + "(symbol");
            foreach (var s in aDef.attrSort.fieldSorts)
            {
                sb.Append(", " + s.Key);
            }
            sb.AppendLine(", children);");
            sb.AppendLine("return t;");
            sb.AppendLine("}");
            sb.AppendLine("return null;");
            sb.AppendLine("}");
            sb.AppendLine();            
            sb.AppendLine();

            return true;
        }

        //Generate the Lookahed computation for the tree
        private static bool GenerateLookahead(List<Def> defs, StringBuilder sb, AlphabetDef aDef)
        {
            sb.AppendLine("private void ComputeLabel(){");
            foreach (var def in defs)
            {
                switch (def.kind)
                {
                    case (DefKind.Lang):
                        {
                            if ((((LangDef)def).domain.name.text).Equals(aDef.id.text))
                                if (!GenerateLookaheadExpr((LangDef)def, sb, aDef))
                                    return false;
                            break;
                        }
                }
            }
            sb.AppendLine("}");
            return true;
        }

        //Generate the Lookahed computation for a particular language definition
        private static bool GenerateLookaheadExpr(LangDef def, StringBuilder sb, AlphabetDef aDef)
        {
            List<FastToken> children;
            foreach (var ruleCase in def.cases)
            {
                children = ruleCase.pat.children;
                sb.AppendLine("if(this.symbol==" + aDef.id.text + "." + ruleCase.pat.symbol + "){");
                sb.Append("if(");
                PrintExpr(children, ruleCase.where, sb);
                sb.AppendLine("){");

                #region Compute the given subsets
                int childPos = 0;
                string childName, langName;
                HashSet<string>[] givenElelements = new HashSet<string>[children.Count];
                for (int i = 0; i < givenElelements.Length; i++)
                    givenElelements[i] = new HashSet<string>();
                #endregion

                #region Populate given elements
                foreach (var giv in ruleCase.given)
                {
                    if (giv.kind == FExpKind.App)
                    {
                        langName = ((AppExp)giv).func.name.text;
                        childName = ((AppExp)giv).args[0].token.text;
                        childPos = 0;
                        foreach (var child in children)
                        {
                            if (child.text == childName)
                                break;
                            childPos++;
                        }
                        //Array of sets of langauges
                        if (!givenElelements[childPos].Contains(langName))
                            givenElelements[childPos].Add(langName);
                    }
                }
                #endregion

                #region Compute lookahead
                bool needsComma;
                int givenUsed = 0;
                for (int i = 0; i < givenElelements.Length; i++)
                {
                    if (givenElelements[i].Count != 0)
                    {
                        sb.Append("if(this.children[" + i + "].langsLabel.IsSupersetOf(new string[] {");
                        needsComma = false;
                        foreach (var lang in givenElelements[i])
                        {
                            if (needsComma)
                                sb.Append(", ");
                            sb.Append("\"" + lang + "\"");
                            needsComma = true;
                        }
                        sb.Append("})){");
                        sb.AppendLine();
                        givenUsed++;
                    }
                }
                sb.AppendLine("this.langsLabel.Add(\"" + def.func.name.text + "\");");
                #endregion

                for (int i = 0; i < givenUsed; i++)
                    sb.AppendLine("}");
                sb.AppendLine("}");
                sb.AppendLine("}");
            }
            sb.AppendLine();
            return true;
        }

        //Generates all the languages and transductions of a particular tree class (correposnding to aDef)
        private static bool GenerateLanguagesAndTransductions(List<Def> defs, StringBuilder sb, AlphabetDef aDef)
        {
            foreach (var def in defs)
            {
                switch (def.kind)
                {
                    case (DefKind.Lang):
                        {
                            if (((LangDef)def).isPublic)
                                if (((LangDef)def).domain.name.text.Equals(aDef.id.text))
                                    if (!GenerateLanguage((LangDef)def, sb))                                
                                        return false;                             
                            break;
                        }
                    case (DefKind.Trans):
                        {
                            if (((TransDef)def).domain.name.text.Equals(aDef.id.text))
                                if (!GenerateTransduction((TransDef)def, sb, aDef))
                                    return false;                            
                            break;
                        }
                    case (DefKind.Def):
                        {
                            if (((DefDef)def).ddkind == DefDefKind.Trans)
                            {
                                if (((TransDefDef)def).domain.name.text.Equals(aDef.id.text))
                                    if (!GenerateTransductionDefinition((TransDefDef)def, sb, aDef))
                                        return false;                               
                            }
                            else
                            {
                                if (((DefDef)def).ddkind == DefDefKind.Lang)
                                    if (((LangDefDef)def).domain.name.text.Equals(aDef.id.text))                                    
                                        if (!GenerateLanguageDefinition((LangDefDef)def, sb))
                                            return false;                                
                            }
                            break;
                        }
                }
            }

            return true;
        }     

        //Generates the code for a particular Transduction (def) in a particular class (aDef)
        private static bool GenerateTransduction(TransDef def, StringBuilder sb, AlphabetDef aDef)
        {
            sb.AppendLine((def.isPublic ? "public" : "private") + " IEnumerable<Tree" + def.range.name + "> " + def.func.name + "(){");
            List<FastToken> children;
            foreach (var ruleCase in def.cases)
            {
                children = ruleCase.pat.children;
                //Check that the constructor is the one of the rule
                sb.AppendLine("if(this.symbol==" + aDef.id + "." + ruleCase.pat.symbol + "){");

                //Check the where condition
                sb.Append("if(");
                PrintExpr(children, ruleCase.where, sb);
                sb.AppendLine("){");

                //Compute the given subsets
                int childPos = 0;
                string childName, langName;
                HashSet<string>[] givenElelements = new HashSet<string>[children.Count];
                for (int i = 0; i < givenElelements.Length; i++)
                    givenElelements[i] = new HashSet<string>();
                //Populate given elements
                foreach (var giv in ruleCase.given)
                {
                    if (giv.kind == FExpKind.App)
                    {
                        langName = ((AppExp)giv).func.name.text;
                        childName = ((AppExp)giv).args[0].token.text;
                        childPos = 0;
                        foreach (var child in children)
                        {
                            if (child.text == childName)
                                break;
                            childPos++;
                        }
                        //Array of sets of langauges
                        if (!givenElelements[childPos].Contains(langName))
                            givenElelements[childPos].Add(langName);
                    }
                }
                //Check given subsets are included in labeling  
                bool needsComma;
                int givenUsed = 0;
                for (int i = 0; i < givenElelements.Length; i++)
                {
                    if (givenElelements[i].Count != 0)
                    {
                        sb.Append("if(this.children[" + i + "].langsLabel.IsSupersetOf(new string[] {");
                        needsComma = false;
                        foreach (var lang in givenElelements[i])
                        {
                            if (needsComma)
                                sb.Append(", ");
                            sb.Append("\"" + lang + "\"");
                            needsComma = true;
                        }
                        sb.Append("})){");
                        sb.AppendLine();
                        givenUsed++;
                    }
                }

                iterCases = new List<List<string>>();

                ComputeIterators((AppExp)ruleCase.to, def.range.name.text, sb, false);
                RemoveDuplicates();

                PrintIterators(children, def.range.name.text, sb);
                sb.Append("yield return ");
                PrintOutputTree(children, (AppExp)ruleCase.to, def.range.name.text, sb, false);
                sb.Append(";");

                //Close all parenthesis
                for (int j = 0; j < iterCases.Count; j++)
                    sb.AppendLine("}");
                for (int i = 0; i < givenUsed; i++)
                    sb.AppendLine("}");
                sb.AppendLine("}");                
                sb.AppendLine("}");
            }

            //FOR EMPTY RULES CASES
            sb.AppendLine("if(false) yield return null;");            
            sb.AppendLine("}");
            sb.AppendLine();
            return true;
        }

        //Generate CSharp for a definition of type TransDefDef
        private static bool GenerateTransductionDefinition(TransDefDef def, StringBuilder sb, AlphabetDef aDef)
        {
            int x;
            sb.AppendLine("public IEnumerable<Tree" + def.range.name + "> " + def.func.name + "(){");
            GenerateDefExpr(def.expr, sb, "this", 0, out x);
            sb.AppendLine("}");
            return true;
        }

        //Generate CSharp for languages
        private static bool GenerateLanguage(LangDef def, StringBuilder sb)
        {
            sb.AppendLine("public bool " + def.func.name + "(){");
            sb.AppendLine("return this.langsLabel.Contains(\""+def.func.name+"\");");
            sb.AppendLine("}");
            return true;
        }

        //Generate CSharp for a definition of type LangDefDef
        private static bool GenerateLanguageDefinition(LangDefDef def, StringBuilder sb)
        {
            sb.AppendLine("public bool " + def.func.name + "(){");
            int x;
            GenerateDefExpr(def.expr, sb, "this", 0, out x);
            sb.AppendLine("}");
            return true;
        }

        //Generate CSharp for lang expr inside a definition
        private static bool GenerateDefExpr(BuiltinLangExp expr, StringBuilder sb, string tree, int varIndex, out int nextVar)
        {
            switch (expr.kind)
            {
                case (BuiltinLangExpKind.Union):
                    {
                        var ce = expr as UnionExp;
                        int x, y;
                        sb.AppendLine(string.Format("bool b{0};", varIndex + 1));
                        GenerateDefExpr(ce.arg1, sb, tree, varIndex + 1, out x);
                        sb.AppendLine(string.Format("bool b{0};", x));
                        GenerateDefExpr(ce.arg2, sb, tree, x, out y);
                        if (varIndex != 0)
                            sb.AppendLine(string.Format("b{0}=b{1}||b{2};", varIndex));
                        else
                            sb.AppendLine(string.Format("return b{0}||b{1};", varIndex + 1, x));

                        nextVar = y;
                        break;
                    }
                case (BuiltinLangExpKind.Intersection):
                    {
                        var ce = expr as IntersectionExp;
                        int x, y;
                        sb.AppendLine(string.Format("bool b{0};", varIndex + 1));
                        GenerateDefExpr(ce.arg1, sb, tree, varIndex + 1, out x);
                        sb.AppendLine(string.Format("bool b{0};", x));
                        GenerateDefExpr(ce.arg2, sb, tree, x, out y);
                        if(varIndex!=0)
                            sb.AppendLine(string.Format("b{0}=b{1}&&b{2};", varIndex,varIndex + 1, x));
                        else
                            sb.AppendLine(string.Format("return b{0}&&b{1};", varIndex + 1, x));

                        nextVar = y;
                        break;
                    }
                case (BuiltinLangExpKind.Domain):
                    {
                        var ce = expr as DomainExp;
                        int x;
                        sb.AppendLine(string.Format("List<Tree{0}> o{1} = new List<Tree{0}>();", ce.arg1.range.name, varIndex + 1));
                        GenerateDefExpr(ce.arg1, sb, tree, varIndex + 1, out x);
                        if (varIndex != 0)
                            sb.AppendLine(string.Format("b{0}=o{1}.Count>0;", varIndex, varIndex + 1));
                        else
                            sb.AppendLine(string.Format("return o{0}.Count>0;", varIndex + 1));

                        nextVar = x;
                        break;
                    }
                case (BuiltinLangExpKind.Var):
                    {
                        var ce = expr as LangNameExp;
                        if(varIndex==0)
                            sb.AppendLine(string.Format("return {0}.{1}();", tree, ce.func.name.text));
                        else
                            sb.AppendLine(string.Format("b{0} = {1}.{2}();", varIndex, tree, ce.func.name.text));

                        nextVar = varIndex + 1; ;
                        break;
                    }
                default:
                    {
                        throw new FastException(FastExceptionKind.NotImplemented);
                    }

            }
            return true;
        }

        //Generate CSharp for trans expr inside a definition
        private static bool GenerateDefExpr(BuiltinTransExp expr, StringBuilder sb, string tree, int varIndex, out int nextVar)
        {
            switch (expr.kind)
            {
                case (BuiltinTransExpKind.Composition):
                    {
                        var ce = expr as CompositionExp;

                        int x, y;
                        sb.AppendLine(string.Format("List<Tree{0}> o{1} = new List<Tree{0}>();", ce.arg1.range.name, varIndex+1));
                        GenerateDefExpr(ce.arg1, sb, tree, varIndex+1, out x);

                        sb.AppendLine(string.Format("List<Tree{0}> o{1} = new List<Tree{0}>();", ce.arg2.range.name, x));
                        sb.Append(string.Format("foreach(var v1 in o{0})",varIndex+1));
                        sb.AppendLine("{");
                        GenerateDefExpr(ce.arg2, sb, "v1",x, out y);
                        sb.AppendLine("}");

                        if (varIndex == 0)
                            sb.AppendLine(string.Format("return o{0};", x));
                        else
                            sb.AppendLine(string.Format("o{0}=o{1};",varIndex, x));

                        nextVar = y;
                        break;
                    }
                case (BuiltinTransExpKind.RestrictionInp):
                    {
                        var ce = expr as RestrictionInpExp;

                        int x, y;
                        
                        sb.AppendLine(string.Format("bool b{0};", varIndex + 1));
                        GenerateDefExpr(ce.arg2, sb, tree, varIndex + 1, out x);

                        sb.AppendLine(string.Format("List<Tree{0}> o{1} = new List<Tree{0}>();", ce.arg1.range.name, x));
                        sb.AppendLine(string.Format("if(b{0})", varIndex + 1));
                        sb.AppendLine("{");
                        GenerateDefExpr(ce.arg1, sb, tree, x, out y);
                        sb.AppendLine("}");

                        if (varIndex == 0)
                            sb.AppendLine(string.Format("return o{0};", x));
                        else
                            sb.AppendLine(string.Format("o{0}=o{1};", varIndex, x));

                        nextVar = y;
                        break;
                    }
                case (BuiltinTransExpKind.RestrictionOut):
                    {
                        var ce = expr as RestrictionOutExp;

                        int x, y;

                        sb.AppendLine(string.Format("List<Tree{0}> o{1} = new List<Tree{0}>();", ce.arg1.range.name, varIndex + 1));
                        GenerateDefExpr(ce.arg1, sb, tree, varIndex + 1, out x);

                        sb.Append(string.Format("foreach(var v1 in o{0})", varIndex + 1));
                        sb.AppendLine("{");
                        sb.AppendLine(string.Format("bool b{0};", x));
                        GenerateDefExpr(ce.arg2, sb, "v1", x, out y);
                        sb.AppendLine(string.Format("if(b{0})", x));
                        if (varIndex == 0)
                            sb.AppendLine(string.Format("yield return v1;"));
                        else
                            sb.AppendLine(string.Format("o{0}.Add(v1);", varIndex));
                        sb.AppendLine("}");

                        nextVar = y;
                        break;
                    }
                case (BuiltinTransExpKind.Var):
                    {
                        if(varIndex==0)
                            sb.AppendLine(string.Format("return {0}.{1}();", tree, expr.func.name.text));                         
                        else
                            sb.AppendLine(string.Format("o{0}.AddRange({1}.{2}());", varIndex, tree, expr.func.name.text));
                         
                        nextVar = varIndex + 1;
                        break;
                    }
                default:
                    {
                        throw new FastException(FastExceptionKind.NotImplemented);
                    }

            }
            return true;
        }


        //BUILD TREES FOR THE OUTPUT
        #region transduction results
        //Generates the code for an output of a transduction
        private static bool PrintOutputTree(List<FastToken> children, FExp ex, String range, StringBuilder sb, bool isapplied)
        {
            switch (ex.kind)
            {
                case (FExpKind.App):
                    {
                        PrintOutputTree(children, (AppExp)ex, range, sb, isapplied);
                        break;
                    }
                case (FExpKind.Var):
                    {
                        PrintOutputTree(children, (Variable)ex, range, sb, isapplied);
                        break;
                    }
            }
            return true;
        }

        //Generates the code for an output of a transduction when the expression is an Expr
        private static bool PrintOutputTree(List<FastToken> children, AppExp ex, String range, StringBuilder sb, bool isapplied)
        {
            if (ex.IsTranDef)
            {
                //We hit the new function invocation 
                sb.Append(ex.func.name);
                PrintOutputTree(children, ex.args[0], range, sb, true);
                return true;
            }
            else
            {
                //The function symbol is a constructor in the output alphabet
                //Create a new Tree of the result kind based on the current function arity
                sb.Append("Tree" + range + ".MakeTree(" + range + "." + ex.func.name);


                foreach (var att in ((RecordExp)ex.args[0]).args)
                {
                    sb.Append(", ");
                    PrintExpr(children, att, sb);
                }


                sb.Append(", new Tree" + range + "[" + (ex.func.arity - 1) + "]{");

                int i = 0;
                foreach (var node in ex.args)
                {
                    if (i == 0) { }
                    else
                    {
                        if (i == 1)
                        {
                            PrintOutputTree(children, node, range, sb, false);
                        }
                        else
                        {
                            sb.Append(", ");
                            PrintOutputTree(children, node, range, sb, false);
                        }
                    }
                    i++;
                }
                sb.Append("}");
                sb.Append(")");

                return true;
            }
        }
        //Generates the code for an output of a transduction when the expression is a Variable
        private static bool PrintOutputTree(List<FastToken> children, Variable ex, String range, StringBuilder sb, bool isapplied)
        {
            if (isapplied)
            {
                //this case is used when a child appear in the output applied to a transduction
                sb.Append(ex.token.text);
                return true;
            }
            //the child is not applied to any transduction
            int i = 0;
            foreach (var c in children)
            {
                if (c.text == ex.token.text)
                {
                    sb.Append(" children[" + i + "] ");
                    return true;
                }
                i++;
            }
            return true;
        }
        #endregion

        //ITERATORS FOR TRANSDUCTIONS
        #region iterators computation

        //Compute iterators for transductions
        private static List<List<String>> iterCases;

        //Compute all the iterators necessary to generate a particular output of a transduction
        //The iterators are saved inside iterCases ([[x,f],[y,g]] means we will have to precompute f(x) and g(x))
        private static bool ComputeIterators(FExp ex, String range, StringBuilder sb, bool isApplied)
        {
            switch (ex.kind)
            {
                case (FExpKind.App):
                    return ComputeIterators((AppExp)ex, range, sb, isApplied);

                case (FExpKind.Var):
                    return ComputeIterators((Variable)ex, range, sb, isApplied);
            }
            return false;
        }

        //Compute all the iterators necessary to generate a particular output of a transduction when expr is AppExpr
        private static bool ComputeIterators(AppExp ex, String range, StringBuilder sb, bool isApplied)
        {
            if (ex.IsTranDef)
            {
                List<String> t = new List<String>();
                t.Add(ex.func.name.text);
                iterCases.Insert(0, t);
                return ComputeIterators((Variable)ex.args[0], range, sb, true);
            }
            else
            {
                int i = 0;
                foreach (var node in ex.args)
                {
                    if (i != 0)
                        ComputeIterators(node, range, sb, false);
                    i++;
                }
                return true;
            }

        }
        //Compute all the iterators necessary to generate a particular output of a transduction when expr is Variable
        private static bool ComputeIterators(Variable ex, String range, StringBuilder sb, bool isApplied)
        {
            if (isApplied)
                iterCases[0].Insert(0, ex.token.text);
            return true;
        }

        //Print all the iterators of the output a particular transduction, and also the corresponding for each loop
        private static bool PrintIterators(List<FastToken> children, String range, StringBuilder sb)
        {
            foreach (var it in iterCases)
                PrintIterator(children, range, sb, it);

            foreach (var it in iterCases)
                PrintIteration(children, range, sb, it);

            return true;

        }

        //Print a single iterator
        private static bool PrintIterator(List<FastToken> children, String range, StringBuilder sb, List<String> it)
        {
            String nameVar = "";
            foreach (var s in it)
                nameVar = "_" + s + nameVar;

            if (it.Count == 2)
            {
                int i = 0;
                foreach (var c in children)
                {
                    if (c.text == it[0])
                        break;
                    i++;
                }
                sb.AppendLine("IEnumerable<Tree" + range + "> " + nameVar + " = children[" + i + "]." + it[1] + "();");
            }

            return true;
        }

        //Print a single for each iteration
        private static bool PrintIteration(List<FastToken> children, String range, StringBuilder sb, List<String> it)
        {
            String nameVar = "";
            foreach (var s in it)
                nameVar = "_" + s + nameVar;

            if (it.Count == 2)
                sb.AppendLine("foreach(var " + it[1] + it[0] + " in " + nameVar + "){");

            return true;
        }

        //Remove  duplicates from the iterCases list
        public static void RemoveDuplicates()
        {
            List<List<String>> temp = new List<List<String>>();
            bool found;
            foreach (var it in iterCases)
            {
                found = false;
                foreach (var t in temp)
                {
                    if (t.Count == it.Count)
                    {
                        found = true;
                        for (int i = 0; i < t.Count; i++)
                            found = found && (it[i] == t[i]);
                    }
                    if (found)
                        break;
                }
                if (!found)
                    temp.Add(it);
            }
            iterCases = temp;
        }
        #endregion

        //EXPRESSIONS
        #region conditional expresssions

        //Generates the code for an expression
        private static bool PrintExpr(List<FastToken> children, FExp ex, StringBuilder sb)
        {
            switch (ex.kind)
            {
                case (FExpKind.App):
                    {
                        PrintExpr(children, (AppExp)ex, sb);
                        break;
                    }
                case (FExpKind.Value):
                    {
                        PrintExpr(children, (Value)ex, sb);
                        break;
                    }
                case (FExpKind.Var):
                    {
                        PrintExpr(children, (Variable)ex, sb);
                        break;
                    }
            }
            return true;
        }

        //Generates the code for an AppExpr expression
        private static bool PrintExpr(List<FastToken> children, AppExp ex, StringBuilder sb)
        {
            sb.Append("(");

            switch (ex.func.name.text)
            {
                case ("="):
                    {
                        PrintExpr(children, ex.args[0], sb);
                        sb.Append(" == ");
                        PrintExpr(children, ex.args[1], sb);
                        break;
                    }
                case ("and"):
                    {
                        for (int i = 0; i < ex.func.arity; i++)
                        {
                            if (i == 0)
                            {
                                PrintExpr(children, ex.args[i], sb);
                            }
                            else
                            {
                                sb.Append(" && ");
                                PrintExpr(children, ex.args[i], sb);
                            }
                        }
                        break;
                    }
                case ("xor"):
                    {
                        for (int i = 0; i < ex.func.arity; i++)
                        {
                            if (i == 0)
                            {
                                PrintExpr(children, ex.args[i], sb);
                            }
                            else
                            {
                                sb.Append(" ^ ");
                                PrintExpr(children, ex.args[i], sb);
                            }
                        }
                        break;
                    }
                case ("not"):
                    {
                        sb.Append("!");
                        PrintExpr(children, ex.args[0], sb);
                        break;
                    }
                case ("or"):
                    {
                        for (int i = 0; i < ex.func.arity; i++)
                        {
                            if (i == 0)
                            {
                                PrintExpr(children, ex.args[i], sb);
                            }
                            else
                            {
                                sb.Append(" || ");
                                PrintExpr(children, ex.args[i], sb);
                            }
                        }
                        break;
                    }
                case ("=>"):
                    {
                        sb.Append("(!");
                        PrintExpr(children, ex.args[0], sb);
                        sb.Append(")");
                        sb.Append(" || ");
                        PrintExpr(children, ex.args[1], sb);
                        break;
                    }
                case ("+"):
                    {
                        for (int i = 0; i < ex.func.arity; i++)
                        {
                            if (i == 0)
                            {
                                PrintExpr(children, ex.args[i], sb);
                            }
                            else
                            {
                                sb.Append(" + ");
                                PrintExpr(children, ex.args[i], sb);
                            }
                        }
                        break;
                    }
                case ("/"):
                    {
                        for (int i = 0; i < ex.func.arity; i++)
                        {
                            if (i == 0)
                            {
                                PrintExpr(children, ex.args[i], sb);
                            }
                            else
                            {
                                sb.Append(" / ");
                                PrintExpr(children, ex.args[i], sb);
                            }
                        }
                        break;
                    }
                case ("-"):
                    {
                        for (int i = 0; i < ex.func.arity; i++)
                        {
                            if (i == 0)
                            {
                                PrintExpr(children, ex.args[i], sb);
                            }
                            else
                            {
                                sb.Append(" - ");
                                PrintExpr(children, ex.args[i], sb);
                            }
                        }
                        break;
                    }
                case ("*"):
                    {
                        for (int i = 0; i < ex.func.arity; i++)
                        {
                            if (i == 0)
                            {
                                PrintExpr(children, ex.args[i], sb);
                            }
                            else
                            {
                                sb.Append(" * ");
                                PrintExpr(children, ex.args[i], sb);
                            }
                        }
                        break;
                    }
                case ("<"):
                    {
                        PrintExpr(children, ex.args[0], sb);
                        sb.Append(" < ");
                        PrintExpr(children, ex.args[1], sb);
                        break;
                    }
                case ("<="):
                    {
                        PrintExpr(children, ex.args[0], sb);
                        sb.Append(" <= ");
                        PrintExpr(children, ex.args[1], sb);
                        break;
                    }
                case (">"):
                    {
                        PrintExpr(children, ex.args[0], sb);
                        sb.Append(" > ");
                        PrintExpr(children, ex.args[1], sb);
                        break;
                    }
                case (">="):
                    {
                        PrintExpr(children, ex.args[0], sb);
                        sb.Append(" >= ");
                        PrintExpr(children, ex.args[1], sb);
                        break;
                    }
                case ("mod"):
                    {
                        PrintExpr(children, ex.args[0], sb);
                        sb.Append(" % ");
                        PrintExpr(children, ex.args[1], sb);
                        break;
                    }
                case ("if"):
                    {
                        PrintExpr(children, ex.args[0], sb);
                        sb.Append(" ? ");
                        PrintExpr(children, ex.args[1], sb);
                        sb.Append(" : ");
                        PrintExpr(children, ex.args[2], sb);
                        break;
                    }

                default:
                    {
                        if (ex.IsLangDef)
                        {
                            PrintExpr(children, ex.args[0], sb);
                            sb.Append("." + ex.func.name.text + "()");
                        }
                        else{
                            sb.Append(ex.func.name.text + "(");
                            PrintExpr(children, ex.args[0], sb);
                            sb.Append(")");
                        }
                        break;
                    }

            }
            sb.Append(")");
            return true;

        }

        //Generates the code for a Variable expression
        private static bool PrintExpr(List<FastToken> children, Variable ex, StringBuilder sb)
        {
            int i = 0;
            foreach (var c in children)
            {
                if (c.text == ex.token.text)
                {
                    sb.Append(" children[" + i + "] ");
                    return true;
                }
                i++;
            }
            sb.Append(ex.token.text);
            return true;
        }

        //Generates the code for a Value expression
        private static bool PrintExpr(List<FastToken> children, Value ex, StringBuilder sb)
        {
            if (ex.token.text.Split('/').Length==2)
                sb.Append(" ((double)" + ex.token.text + ") ");
            else
                sb.Append(" " + ex.token.text + " ");
            return true;
        }
        #endregion


    }

}

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.IO;
//using Microsoft.Fast.AST;

//namespace Microsoft.Fast
//{
//    public class CsharpGenerator
//    {

//        static List<string> states;

//        /// <summary>
//        /// Generates C# code into the file "namefile" from a fast program with the namespace "ns"
//        /// </summary>
//        /// <param name="fpg">fast program</param> 
//        /// <param name="namefile">path of the output file</param> 
//        /// <param name="ns">namespace for the generated file</param> 
//        public static bool GenerateCode(FastPgm fpg, String namefile, String ns)
//        {
//            states = new List<string>();
//            StreamWriter file = new System.IO.StreamWriter(namefile, false);
//            sb.AppendLine("using System;");
//            sb.AppendLine("using System.Collections;");
//            sb.AppendLine("using System.Collections.Generic;");
//            sb.AppendLine("using Microsoft.Automata;");            
//            sb.AppendLine("namespace " + ns + "{");
//            sb.AppendLine();

//            GenerateEnums(fpg.defs, sb);
//            GenerateAlphabets(fpg.defs, sb);
//            GenerateTreeClasses(fpg.defs, sb);

//            sb.AppendLine("}");
//            file.Close();
//            return true;
//        }

//        /// <summary>
//        /// Generates C# code into the file "namefile" from a fast program 
//        /// </summary>
//        /// <param name="fpg">fast program</param> 
//        /// <param name="namefile">path of the output file</param> 
//        public static bool GenerateCode(FastPgm fpg, String namefile)
//        {
//            StreamWriter file = new System.IO.StreamWriter(@"../../" + namefile, false);
//            sb.AppendLine("using System;");
//            sb.AppendLine("using System.Collections;");
//            sb.AppendLine("using System.Collections.Generic;");
//            sb.AppendLine("namespace Microsoft.Fast.Sample{");
//            sb.AppendLine();

//            GenerateEnums(fpg.defs, sb);
//            GenerateAlphabets(fpg.defs, sb);
//            GenerateTreeClasses(fpg.defs, sb);

//            sb.AppendLine("}");
//            file.Close();
//            return true;
//        }


//        //PRIVATE GENERATORS
//        //Generate C# for all the enums in the program
//        private static bool GenerateEnums(List<Def> defs, StringBuilder sb)
//        {
//            foreach (var def in defs)
//            {
//                switch (def.kind)
//                {
//                    case (DefKind.Enum):
//                        {
//                            if (!GenerateEnum((EnumDef)def, sb))
//                                return false;
//                            break;
//                        }
//                }
//            }
//            return true;
//        }

//        //Generate C# for all the consts in the program
//        private static bool GenerateConsts(List<Def> defs, StringBuilder sb)
//        {
//            foreach (var def in defs)
//            {
//                switch (def.kind)
//                {
//                    case (DefKind.Const):
//                        {
//                            if (!GenerateConst((ConstDef)def, sb))
//                                return false;
//                            break;
//                        }
//                }
//            }
//            return true;
//        }

//        //Generate C# for all the functions in the program
//        private static bool GenerateFuns(List<Def> defs, StringBuilder sb)
//        {
//            foreach (var def in defs)
//            {
//                switch (def.kind)
//                {
//                    case (DefKind.Function):
//                        {
//                            if (!GenerateFun((FunctionDef)def, sb))
//                                return false;
//                            break;
//                        }
//                }
//            }
//            return true;
//        }

//        //Generate C# for all the consts in the program
//        private static bool GenerateTrees(List<Def> defs, StringBuilder sb, AlphabetDef aDef)
//        {
//            foreach (var def in defs)            
//                switch (def.kind)
//                {
//                    case (DefKind.Def):
//                        {
//                            if(((DefDef)def).ddkind == DefDefKind.Tree)
//                                if (((TreeDef)def).domain.name.text.Equals(aDef.id.text))
//                                    if (!GenerateTree((TreeDef)def, sb))
//                                        return false;
//                            break;
//                        }
//                }

//            return true;
//        }

//        //Generate the C# corresponding to one enum
//        private static bool GenerateEnum(EnumDef def, StringBuilder sb)
//        {
//            sb.Append("public enum " + def.id.text + " {");
//            foreach (var elem in def.elems)
//            {
//                if (def.elems.First().Equals(elem))
//                {
//                    sb.Append(elem);
//                }
//                else
//                {
//                    sb.Append(", " + elem);
//                }
//            }
//            sb.AppendLine("}");
//            sb.AppendLine();
//            return true;
//        }

//        //Generate the C# corresponding to one constant
//        private static bool GenerateConst(ConstDef def, StringBuilder sb)
//        {
//            sb.Append("public const " + ((def.sort.kind == FastSortKind.Real) ? "double" : def.sort.name.text) + " " +def.id.text + " = ");
//            PrintExpr(new List<FastToken>(), def.expr, sb);
//            sb.AppendLine(";");
//            sb.AppendLine();
//            return true;
//        }

//        //Generate the C# corresponding to one function
//        private static bool GenerateFun(FunctionDef def, StringBuilder sb)
//        {
//            sb.Append("public " + ((def.outputSort.kind == FastSortKind.Real) ? "double" : def.outputSort.name.text) + " " + def.id.text+"(");
//            bool needComma=false;
//            foreach (var e in def.inputVariables)
//            {
//                if (needComma)
//                    sb.Append(",");
//                else
//                    needComma = true;
//                sb.Append(((e.Value.name.text == "real") ? "double" : e.Value.name.text) + " " + e.Key.text);
//            }
//            sb.AppendLine("){");
//            sb.Append("return ");
//            PrintExpr(new List<FastToken>(), def.expr, sb);
//            sb.AppendLine(";");
//            sb.Append("}");
//            sb.AppendLine();
//            return true;
//        }

//        //Generate the C# corresponding to one constant
//        private static bool GenerateTree(TreeDef def, StringBuilder sb)
//        {
//            sb.Append("public static Tree"+def.domain.name.text+" "+def.func.name.text +" = ");
//            if(def.func!=null)
//                PrintOutputTree(null, def.expr, def.domain.name.text, sb, false);
//            else // AT some point add output gen in C#
//                sb.AppendLine("null");
//            sb.AppendLine(";");
//            sb.AppendLine();
//            return true;
//        }

//        //Generate the C# corresponding to the alphabets
//        private static bool GenerateAlphabets(List<Def> defs, StringBuilder sb)
//        {
//            foreach (var def in defs)
//            {
//                switch (def.kind)
//                {
//                    case (DefKind.Alphabet):
//                        {
//                            if (!GenerateAlphabet((AlphabetDef)def, sb))
//                                return false;
//                            break;
//                        }
//                }
//            }
//            return true;
//        }

//        //Generate the C# corresponding to a particular alphabet (using enum)
//        private static bool GenerateAlphabet(AlphabetDef def, StringBuilder sb)
//        {
//            //Generate enum for alphabet
//            sb.Append("public enum " + def.id.text + " {");
//            foreach (var sym in def.symbols)
//            {
//                if (def.symbols.First().Equals(sym))
//                {
//                    sb.Append(sym.name);
//                }
//                else
//                {
//                    sb.Append(", " + sym.name);
//                }
//            }
//            sb.AppendLine("}");
//            sb.AppendLine();

//            sb.AppendLine("class " + def.id.text + "Util {");
//            sb.AppendLine("public static int getRank(" + def.id.text + " el){");
//            sb.AppendLine("switch(el){");
//            foreach (var sym in def.symbols)
//            {
//                sb.AppendLine("case(" + def.id.text + "." + sym.name + "):{");
//                sb.AppendLine("return " + (sym.arity - 1) + ";");
//                sb.AppendLine("}");
//            }
//            sb.AppendLine("}");
//            sb.AppendLine("return -1;");
//            sb.AppendLine("}");
//            sb.AppendLine("}");
//            sb.AppendLine();

//            return true;
//        }

//        //Generate the C# corresponding to the tree classes (each of them will contain the languages and transductions)
//        private static bool GenerateTreeClasses(List<Def> defs, StringBuilder sb)
//        {
//            foreach (var def in defs)
//            {
//                switch (def.kind)
//                {
//                    case (DefKind.Alphabet):
//                        {
//                            if (!GenerateTreeClass(defs, sb, ((AlphabetDef)def)))
//                                return false;
//                            break;
//                        }
//                }
//            }
//            return true;
//        }

//        //Generate the C# corresponding to a particular tree class (inferred from the alphabets)
//        private static bool GenerateTreeClass(List<Def> defs, StringBuilder sb, AlphabetDef aDef)
//        {
//            sb.AppendLine("partial class Tree" + aDef.id.text + "{");
//            foreach (var s in aDef.attrSort.fieldSorts)
//                if (s.Value.kind == FastSortKind.Real)
//                    sb.AppendLine("double " + s.Key + ";");
//                else
//                    sb.AppendLine(s.Value.name + " " + s.Key + ";");
//            sb.AppendLine();
//            sb.AppendLine(aDef.id.text + " symbol;");
//            sb.AppendLine();                        

//            sb.AppendLine("BvSetSolver provider;");
//            sb.AppendLine("BvSet langsLabel;");
//            sb.AppendLine("Tree" + aDef.id.text + "[] children;");
//            sb.AppendLine();

//            GenerateConsts(defs, sb);
//            GenerateFuns(defs, sb);
//            GenerateTrees(defs, sb, aDef);
//            GenerateConstructor(file, aDef);           
//            GenerateLookahead(defs, sb, aDef);
//            GenerateLanguagesAndTransductions(defs, sb, aDef);

//            sb.AppendLine("}");
//            sb.AppendLine();

//            return true;
//        }

//        //Generate the C# corresponding to the constructor of a Tree class, the MakeTree method and an accessor for the children
//        private static bool GenerateConstructor(StreamWriter file, AlphabetDef aDef)
//        {
//            sb.Append("private Tree" + aDef.id.text + "(" + aDef.id.text + " symbol");
//            foreach (var s in aDef.attrSort.fieldSorts)
//            {
//                if (s.Value.kind == FastSortKind.Real)
//                {
//                    sb.Append(", double " + s.Key);
//                }
//                else
//                {
//                    sb.Append(", " + s.Value.name + " " + s.Key);
//                }
//            }
//            sb.AppendLine(", Tree" + aDef.id.text + "[] children){");
//            sb.AppendLine("this.symbol=symbol;");
//            foreach (var s in aDef.attrSort.fieldSorts)
//            {
//                sb.AppendLine("this." + s.Key + "=" + s.Key + ";");
//            }
//            sb.AppendLine("this.children=children;");                        
//            sb.AppendLine("this.provider = new BvSetSolver();");
//            sb.AppendLine("this.langsLabel = provider.False;");
//            sb.AppendLine("ComputeLabel();");
//            sb.AppendLine("}");
//            sb.AppendLine();
//            sb.AppendLine("public Tree" + aDef.id.text + " this [int _i]{get { return children[_i]; }}");
//            sb.AppendLine();

//            sb.Append("public static Tree" + aDef.id.text + " MakeTree(" + aDef.id.text + " symbol");
//            foreach (var s in aDef.attrSort.fieldSorts)
//            {
//                if (s.Value.kind == FastSortKind.Real)
//                {
//                    sb.Append(", double " + s.Key);
//                }
//                else
//                {
//                    sb.Append(", " + s.Value.name + " " + s.Key);
//                }
//            }
//            sb.AppendLine(", Tree" + aDef.id.text + "[] children){");
//            sb.AppendLine("if(children.Length==" + aDef.id.text + "Util.getRank(symbol)){");
//            sb.Append("Tree" + aDef.id.text + " t = new Tree" + aDef.id.text + "(symbol");
//            foreach (var s in aDef.attrSort.fieldSorts)
//            {
//                sb.Append(", " + s.Key);
//            }
//            sb.AppendLine(", children);");
//            sb.AppendLine("return t;");
//            sb.AppendLine("}");
//            sb.AppendLine("return null;");
//            sb.AppendLine("}");
//            sb.AppendLine();            
//            sb.AppendLine();

//            return true;
//        }

//        //Generate the Lookahed computation for the tree
//        private static bool GenerateLookahead(List<Def> defs, StringBuilder sb, AlphabetDef aDef)
//        {
//            sb.AppendLine("private void ComputeLabel(){");
//            foreach (var def in defs)
//            {
//                switch (def.kind)
//                {
//                    case (DefKind.Lang):
//                        {
//                            if ((((LangDef)def).domain.name.text).Equals(aDef.id.text))
//                                if (!GenerateLookaheadExpr((LangDef)def, sb, aDef))
//                                    return false;
//                            break;
//                        }
//                }
//            }
//            sb.AppendLine("}");
//            return true;
//        }

//        //Generate the Lookahed computation for a particular language definition
//        private static bool GenerateLookaheadExpr(LangDef def, StringBuilder sb, AlphabetDef aDef)
//        {
//            List<FastToken> children;
//            foreach (var ruleCase in def.cases)
//            {
//                children = ruleCase.pat.children;
//                sb.AppendLine("if(this.symbol==" + aDef.id.text + "." + ruleCase.pat.symbol + "){");
//                sb.Append("if(");
//                PrintExpr(children, ruleCase.where, sb);
//                sb.AppendLine("){");

//                #region Compute the given subsets
//                int childPos = 0;
//                string childName, langName;
//                HashSet<string>[] givenElelements = new HashSet<string>[children.Count];
//                for (int i = 0; i < givenElelements.Length; i++)
//                    givenElelements[i] = new HashSet<string>();
//                #endregion

//                #region Populate given elements
//                foreach (var giv in ruleCase.given)
//                {
//                    if (giv.kind == Z3_ast_kind.Z3_APP_AST)
//                    {
//                        langName = ((AppExpr)giv).func.name.text;
//                        childName = ((AppExpr)giv).args[0].token.text;
//                        childPos = 0;
//                        foreach (var child in children)
//                        {
//                            if (child.text == childName)
//                                break;
//                            childPos++;
//                        }
//                        //Array of sets of langauges
//                        if (!givenElelements[childPos].Contains(langName))
//                            givenElelements[childPos].Add(langName);
//                    }
//                }
//                #endregion

//                #region Compute lookahead
//                bool needsComma;
//                int givenUsed = 0;
//                if (!states.Contains(def.func.name.text))
//                    states.Add(def.func.name.text);
//                for (int i = 0; i < givenElelements.Length; i++)
//                {
//                    if (givenElelements[i].Count != 0)
//                    {
//                        sb.Append("if(this.provider.IsSatisfiable(this.provider.MkDiff(this.provider.MkSetFromElements(new uint[] {");
//                        needsComma = false;
//                        foreach (var lang in givenElelements[i])
//                        {
//                            if (!states.Contains(lang))
//                                states.Add(lang);
//                            if (needsComma)
//                                sb.Append(", ");
//                            sb.Append(states.IndexOf(lang));
//                            needsComma = true;
//                        }
//                        sb.Append("}, 31), langsLabel))){");
//                        sb.AppendLine();
//                        givenUsed++;
//                    }
//                }
//                sb.AppendLine("this.langsLabel = this.provider.MkOr(langsLabel, this.provider.MkSetFromElements(new uint[] {" + states.IndexOf(def.func.name.text) + "}, 31));");
//                #endregion

//                for (int i = 0; i < givenUsed; i++)
//                    sb.AppendLine("}");
//                sb.AppendLine("}");
//                sb.AppendLine("}");
//            }
//            sb.AppendLine();
//            return true;
//        }

//        //Generates all the languages and transductions of a particular tree class (correposnding to aDef)
//        private static bool GenerateLanguagesAndTransductions(List<Def> defs, StringBuilder sb, AlphabetDef aDef)
//        {
//            foreach (var def in defs)
//            {
//                switch (def.kind)
//                {
//                    case (DefKind.Lang):
//                        {
//                            if (((LangDef)def).isPublic)
//                                if (((LangDef)def).domain.name.text.Equals(aDef.id.text))
//                                    if (!GenerateLanguage((LangDef)def, sb, aDef))                                
//                                        return false;                             
//                            break;
//                        }
//                    case (DefKind.Trans):
//                        {
//                            if (((TransDef)def).domain.name.text.Equals(aDef.id.text))
//                                if (!GenerateTransduction((TransDef)def, sb, aDef))
//                                    return false;                            
//                            break;
//                        }
//                    case (DefKind.Def):
//                        {
//                            if (((DefDef)def).ddkind == DefDefKind.Trans)
//                            {
//                                if (((TransDefDef)def).domain.name.text.Equals(aDef.id.text))
//                                    if (!GenerateTransductionDefinition((TransDefDef)def, sb, aDef))
//                                        return false;                               
//                            }
//                            else
//                            {
//                                if (((DefDef)def).ddkind == DefDefKind.Lang)
//                                    if (((LangDefDef)def).domain.name.text.Equals(aDef.id.text))                                    
//                                        if (!GenerateLanguageDefinition((LangDefDef)def, sb, aDef))
//                                            return false;                                
//                            }
//                            break;
//                        }
//                }
//            }

//            return true;
//        }     

//        //Generates the code for a particular Transduction (def) in a particular class (aDef)
//        private static bool GenerateTransduction(TransDef def, StringBuilder sb, AlphabetDef aDef)
//        {
//            sb.AppendLine((def.isPublic ? "public" : "private") + " IEnumerable<Tree" + def.range.name + "> " + def.func.name + "(){");
//            List<FastToken> children;
//            foreach (var ruleCase in def.cases)
//            {
//                children = ruleCase.pat.children;
//                //Check that the constructor is the one of the rule
//                sb.AppendLine("if(this.symbol==" + aDef.id + "." + ruleCase.pat.symbol + "){");

//                //Check the where condition
//                sb.Append("if(");
//                PrintExpr(children, ruleCase.where, sb);
//                sb.AppendLine("){");

//                //Compute the given subsets
//                int childPos = 0;
//                string childName, langName;
//                HashSet<string>[] givenElelements = new HashSet<string>[children.Count];
//                for (int i = 0; i < givenElelements.Length; i++)
//                    givenElelements[i] = new HashSet<string>();
//                //Populate given elements
//                foreach (var giv in ruleCase.given)
//                {
//                    if (giv.kind == Z3_ast_kind.Z3_APP_AST)
//                    {
//                        langName = ((AppExpr)giv).func.name.text;
//                        childName = ((AppExpr)giv).args[0].token.text;
//                        childPos = 0;
//                        foreach (var child in children)
//                        {
//                            if (child.text == childName)
//                                break;
//                            childPos++;
//                        }
//                        //Array of sets of langauges
//                        if (!givenElelements[childPos].Contains(langName))
//                            givenElelements[childPos].Add(langName);
//                    }
//                }
//                //Check given subsets are included in labeling  
//                bool needsComma;
//                int givenUsed = 0;
//                for (int i = 0; i < givenElelements.Length; i++)
//                {
//                    if (givenElelements[i].Count != 0)
//                    {
//                        sb.Append("if(this.provider.IsSatisfiable(this.provider.MkDiff(this.provider.MkSetFromElements(new uint[] {");
//                        needsComma = false;
//                        foreach (var lang in givenElelements[i])
//                        {
//                            if (!states.Contains(lang))
//                                states.Add(lang);
//                            if (needsComma)
//                                sb.Append(", ");
//                            sb.Append(states.IndexOf(lang));
//                            needsComma = true;
//                        }
//                        sb.Append("}, 31), langsLabel))){");
//                        sb.AppendLine();
//                        givenUsed++;
//                    }
//                }                                

//                iterCases = new List<List<string>>();

//                ComputeIterators((AppExpr)ruleCase.to, def.range.name.text, sb, false);
//                RemoveDuplicates();

//                PrintIterators(children, def.range.name.text, sb);
//                sb.Append("yield return ");
//                PrintOutputTree(children, (AppExpr)ruleCase.to, def.range.name.text, sb, false);
//                sb.Append(";");

//                //Close all parenthesis
//                for (int j = 0; j < iterCases.Count; j++)
//                    sb.AppendLine("}");
//                for (int i = 0; i < givenUsed; i++)
//                    sb.AppendLine("}");
//                sb.AppendLine("}");                
//                sb.AppendLine("}");
//            }

//            //FOR EMPTY RULES CASES
//            sb.AppendLine("if(false) yield return null;");            
//            sb.AppendLine("}");
//            sb.AppendLine();
//            return true;
//        }

//        //Generate CSharp for a definition of type TransDefDef
//        private static bool GenerateTransductionDefinition(TransDefDef def, StringBuilder sb, AlphabetDef aDef)
//        {
//            sb.AppendLine("public IEnumerable<Tree" + def.range.name + "> " + def.func.name + "(){");
//            GenerateDefExpr(def.expr, sb, aDef, 0);
//            sb.AppendLine("}");
//            return true;
//        }

//        //Generate CSharp for languages
//        private static bool GenerateLanguage(LangDef def, StringBuilder sb, AlphabetDef aDef)
//        {
//            sb.AppendLine("public bool " + def.func.name + "(){");
//            sb.AppendLine("return this.langsLabel.Contains(\""+def.func.name+"\");");
//            sb.AppendLine("}");
//            return true;
//        }

//        //Generate CSharp for a definition of type LangDefDef
//        private static bool GenerateLanguageDefinition(LangDefDef def, StringBuilder sb, AlphabetDef aDef)
//        {
//            sb.AppendLine("public bool " + def.func.name + "(){");
//            GenerateDefExpr(def.expr, sb, aDef, 0);
//            sb.AppendLine("}");
//            return true;
//        }

//        //Generate CSharp for an expression inside a definition
//        private static bool GenerateDefExpr(Expr expr, StringBuilder sb, AlphabetDef aDef, int i)
//        {
//            switch (expr.kind)
//            {
//                case (ExprKind.Composition):
//                    {
//                        sb.AppendLine("foreach(var v" + i + " in this." + ((CompositionExpr)expr).arg1.token + "())");
//                        sb.AppendLine("foreach(var v" + (i + 1) + " in v" + i + "." + ((CompositionExpr)expr).arg2.token + "())");
//                        sb.AppendLine("yield return v" + (i+1) + ";");
//                        break;
//                    }
//                case (ExprKind.RestrictionInp):
//                    {
//                        sb.AppendLine("if(this.langsLabel.Contains(\""+((RestrictionInpExpr)expr).arg2.token+"\")");
//                        sb.AppendLine("foreach(var v" + i + " in this." + ((RestrictionInpExpr)expr).arg1.token + "())");
//                        sb.AppendLine("yield return v" + i + ";");
//                        break;
//                    }
//                case (ExprKind.RestrictionOut):
//                    {
//                        sb.AppendLine("foreach(var v" + i + " in this." + ((RestrictionOutExpr)expr).arg1.token + "())");
//                        sb.AppendLine("if(v" + i + ".langsLabel.Contains(\"" + ((RestrictionInpExpr)expr).arg2.token + "\")");
//                        sb.AppendLine("yield return v" + i + ";");
//                        break;
//                    }
//                case (ExprKind.Union):
//                    {
//                        sb.AppendLine("return (this." + ((UnionExpr)expr).arg1.token + "() || this." + ((UnionExpr)expr).arg2.token + "());");
//                        break;
//                    }
//                case (ExprKind.Intersection):
//                    {
//                        sb.AppendLine("return (this." + ((IntersectionExpr)expr).arg1.token + "() && this." + ((IntersectionExpr)expr).arg2.token + "());");
//                        break;
//                    }
//                case (ExprKind.Domain):
//                    {
//                        sb.AppendLine("return (this." + ((DomainExpr)expr).arg1.token + "().GetEnumerator().MoveNext());");
//                        break;
//                    }

//            }
//            return true;
//        }


//        //BUILD TREES FOR THE OUTPUT
//        #region transduction results
//        //Generates the code for an output of a transduction
//        private static bool PrintOutputTree(List<FastToken> children, Expr ex, String range, StringBuilder sb, bool isapplied)
//        {
//            switch (ex.kind)
//            {
//                case (Z3_ast_kind.Z3_APP_AST):
//                    {
//                        PrintOutputTree(children, (AppExpr)ex, range, sb, isapplied);
//                        break;
//                    }
//                case (Z3_ast_kind.Z3_VAR_AST):
//                    {
//                        PrintOutputTree(children, (Variable)ex, range, sb, isapplied);
//                        break;
//                    }
//            }
//            return true;
//        }

//        //Generates the code for an output of a transduction when the expression is an Expr
//        private static bool PrintOutputTree(List<FastToken> children, AppExpr ex, String range, StringBuilder sb, bool isapplied)
//        {
//            if (ex.IsTranDef)
//            {
//                //We hit the new function invocation 
//                sb.Append(ex.func.name);
//                PrintOutputTree(children, ex.args[0], range, sb, true);
//                return true;
//            }
//            else
//            {
//                //The function symbol is a constructor in the output alphabet
//                //Create a new Tree of the result kind based on the current function arity
//                sb.Append("Tree" + range + ".MakeTree(" + range + "." + ex.func.name);


//                foreach (var att in ((RecordExpr)ex.args[0]).args)
//                {
//                    sb.Append(", ");
//                    PrintExpr(children, att, sb);
//                }


//                sb.Append(",new Tree" + range + "[" + (ex.func.arity - 1) + "]{");

//                int i = 0;
//                foreach (var node in ex.args)
//                {
//                    if (i == 0) { }
//                    else
//                    {
//                        if (i == 1)
//                        {
//                            PrintOutputTree(children, node, range, sb, false);
//                        }
//                        else
//                        {
//                            sb.Append(", ");
//                            PrintOutputTree(children, node, range, sb, false);
//                        }
//                    }
//                    i++;
//                }
//                sb.Append("}");
//                sb.Append(")");

//                return true;
//            }
//        }
//        //Generates the code for an output of a transduction when the expression is a Variable
//        private static bool PrintOutputTree(List<FastToken> children, Variable ex, String range, StringBuilder sb, bool isapplied)
//        {
//            if (isapplied)
//            {
//                //this case is used when a child appear in the output applied to a transduction
//                sb.Append(ex.token.text);
//                return true;
//            }
//            //the child is not applied to any transduction
//            int i = 0;
//            foreach (var c in children)
//            {
//                if (c.text == ex.token.text)
//                {
//                    sb.Append(" children[" + i + "] ");
//                    return true;
//                }
//                i++;
//            }
//            return true;
//        }
//        #endregion

//        //ITERATORS FOR TRANSDUCTIONS
//        #region iterators computation

//        //Compute iterators for transductions
//        private static List<List<String>> iterCases;

//        //Compute all the iterators necessary to generate a particular output of a transduction
//        //The iterators are saved inside iterCases ([[x,f],[y,g]] means we will have to precompute f(x) and g(x))
//        private static bool ComputeIterators(Expr ex, String range, StringBuilder sb, bool isApplied)
//        {
//            switch (ex.kind)
//            {
//                case (Z3_ast_kind.Z3_APP_AST):
//                    return ComputeIterators((AppExpr)ex, range, sb, isApplied);

//                case (Z3_ast_kind.Z3_VAR_AST):
//                    return ComputeIterators((Variable)ex, range, sb, isApplied);
//            }
//            return false;
//        }

//        //Compute all the iterators necessary to generate a particular output of a transduction when expr is AppExpr
//        private static bool ComputeIterators(AppExpr ex, String range, StringBuilder sb, bool isApplied)
//        {
//            if (ex.IsTranDef)
//            {
//                List<String> t = new List<String>();
//                t.Add(ex.func.name.text);
//                iterCases.Insert(0, t);
//                return ComputeIterators((Variable)ex.args[0], range, sb, true);
//            }
//            else
//            {
//                int i = 0;
//                foreach (var node in ex.args)
//                {
//                    if (i != 0)
//                        ComputeIterators(node, range, sb, false);
//                    i++;
//                }
//                return true;
//            }

//        }
//        //Compute all the iterators necessary to generate a particular output of a transduction when expr is Variable
//        private static bool ComputeIterators(Variable ex, String range, StringBuilder sb, bool isApplied)
//        {
//            if (isApplied)
//                iterCases[0].Insert(0, ex.token.text);
//            return true;
//        }

//        //Print all the iterators of the output a particular transduction, and also the corresponding for each loop
//        private static bool PrintIterators(List<FastToken> children, String range, StringBuilder sb)
//        {
//            foreach (var it in iterCases)
//                PrintIterator(children, range, sb, it);

//            foreach (var it in iterCases)
//                PrintIteration(children, range, sb, it);

//            return true;

//        }

//        //Print a single iterator
//        private static bool PrintIterator(List<FastToken> children, String range, StringBuilder sb, List<String> it)
//        {
//            String nameVar = "";
//            foreach (var s in it)
//                nameVar = "_" + s + nameVar;

//            if (it.Count == 2)
//            {
//                int i = 0;
//                foreach (var c in children)
//                {
//                    if (c.text == it[0])
//                        break;
//                    i++;
//                }
//                sb.AppendLine("IEnumerable<Tree" + range + "> " + nameVar + " = children[" + i + "]." + it[1] + "();");
//            }

//            return true;
//        }

//        //Print a single for each iteration
//        private static bool PrintIteration(List<FastToken> children, String range, StringBuilder sb, List<String> it)
//        {
//            String nameVar = "";
//            foreach (var s in it)
//                nameVar = "_" + s + nameVar;

//            if (it.Count == 2)
//                sb.AppendLine("foreach(var " + it[1] + it[0] + " in " + nameVar + "){");

//            return true;
//        }

//        //Remove  duplicates from the iterCases list
//        public static void RemoveDuplicates()
//        {
//            List<List<String>> temp = new List<List<String>>();
//            bool found;
//            foreach (var it in iterCases)
//            {
//                found = false;
//                foreach (var t in temp)
//                {
//                    if (t.Count == it.Count)
//                    {
//                        found = true;
//                        for (int i = 0; i < t.Count; i++)
//                            found = found && (it[i] == t[i]);
//                    }
//                    if (found)
//                        break;
//                }
//                if (!found)
//                    temp.Add(it);
//            }
//            iterCases = temp;
//        }
//        #endregion

//        //EXPRESSIONS
//        #region conditional expresssions

//        //Generates the code for an expression
//        private static bool PrintExpr(List<FastToken> children, Expr ex, StringBuilder sb)
//        {
//            switch (ex.kind)
//            {
//                case (Z3_ast_kind.Z3_APP_AST):
//                    {
//                        PrintExpr(children, (AppExpr)ex, sb);
//                        break;
//                    }
//                case (ExprKind.Value):
//                    {
//                        PrintExpr(children, (Value)ex, sb);
//                        break;
//                    }
//                case (Z3_ast_kind.Z3_VAR_AST):
//                    {
//                        PrintExpr(children, (Variable)ex, sb);
//                        break;
//                    }
//            }
//            return true;
//        }

//        //Generates the code for an AppExpr expression
//        private static bool PrintExpr(List<FastToken> children, AppExpr ex, StringBuilder sb)
//        {
//            sb.Append("(");

//            switch (ex.func.name.text)
//            {
//                case ("="):
//                    {
//                        PrintExpr(children, ex.args[0], sb);
//                        sb.Append(" == ");
//                        PrintExpr(children, ex.args[1], sb);
//                        break;
//                    }
//                case ("and"):
//                    {
//                        for (int i = 0; i < ex.func.arity; i++)
//                        {
//                            if (i == 0)
//                            {
//                                PrintExpr(children, ex.args[i], sb);
//                            }
//                            else
//                            {
//                                sb.Append(" && ");
//                                PrintExpr(children, ex.args[i], sb);
//                            }
//                        }
//                        break;
//                    }
//                case ("xor"):
//                    {
//                        for (int i = 0; i < ex.func.arity; i++)
//                        {
//                            if (i == 0)
//                            {
//                                PrintExpr(children, ex.args[i], sb);
//                            }
//                            else
//                            {
//                                sb.Append(" ^ ");
//                                PrintExpr(children, ex.args[i], sb);
//                            }
//                        }
//                        break;
//                    }
//                case ("not"):
//                    {
//                        sb.Append("!");
//                        PrintExpr(children, ex.args[0], sb);
//                        break;
//                    }
//                case ("or"):
//                    {
//                        for (int i = 0; i < ex.func.arity; i++)
//                        {
//                            if (i == 0)
//                            {
//                                PrintExpr(children, ex.args[i], sb);
//                            }
//                            else
//                            {
//                                sb.Append(" || ");
//                                PrintExpr(children, ex.args[i], sb);
//                            }
//                        }
//                        break;
//                    }
//                case ("=>"):
//                    {
//                        sb.Append("(!");
//                        PrintExpr(children, ex.args[0], sb);
//                        sb.Append(")");
//                        sb.Append(" || ");
//                        PrintExpr(children, ex.args[1], sb);
//                        break;
//                    }
//                case ("+"):
//                    {
//                        for (int i = 0; i < ex.func.arity; i++)
//                        {
//                            if (i == 0)
//                            {
//                                PrintExpr(children, ex.args[i], sb);
//                            }
//                            else
//                            {
//                                sb.Append(" + ");
//                                PrintExpr(children, ex.args[i], sb);
//                            }
//                        }
//                        break;
//                    }
//                case ("/"):
//                    {
//                        for (int i = 0; i < ex.func.arity; i++)
//                        {
//                            if (i == 0)
//                            {
//                                PrintExpr(children, ex.args[i], sb);
//                            }
//                            else
//                            {
//                                sb.Append(" / ");
//                                PrintExpr(children, ex.args[i], sb);
//                            }
//                        }
//                        break;
//                    }
//                case ("-"):
//                    {
//                        for (int i = 0; i < ex.func.arity; i++)
//                        {
//                            if (i == 0)
//                            {
//                                PrintExpr(children, ex.args[i], sb);
//                            }
//                            else
//                            {
//                                sb.Append(" - ");
//                                PrintExpr(children, ex.args[i], sb);
//                            }
//                        }
//                        break;
//                    }
//                case ("*"):
//                    {
//                        for (int i = 0; i < ex.func.arity; i++)
//                        {
//                            if (i == 0)
//                            {
//                                PrintExpr(children, ex.args[i], sb);
//                            }
//                            else
//                            {
//                                sb.Append(" * ");
//                                PrintExpr(children, ex.args[i], sb);
//                            }
//                        }
//                        break;
//                    }
//                case ("<"):
//                    {
//                        PrintExpr(children, ex.args[0], sb);
//                        sb.Append(" < ");
//                        PrintExpr(children, ex.args[1], sb);
//                        break;
//                    }
//                case ("<="):
//                    {
//                        PrintExpr(children, ex.args[0], sb);
//                        sb.Append(" <= ");
//                        PrintExpr(children, ex.args[1], sb);
//                        break;
//                    }
//                case (">"):
//                    {
//                        PrintExpr(children, ex.args[0], sb);
//                        sb.Append(" > ");
//                        PrintExpr(children, ex.args[1], sb);
//                        break;
//                    }
//                case (">="):
//                    {
//                        PrintExpr(children, ex.args[0], sb);
//                        sb.Append(" >= ");
//                        PrintExpr(children, ex.args[1], sb);
//                        break;
//                    }
//                case ("mod"):
//                    {
//                        PrintExpr(children, ex.args[0], sb);
//                        sb.Append(" % ");
//                        PrintExpr(children, ex.args[1], sb);
//                        break;
//                    }
//                case ("if"):
//                    {
//                        PrintExpr(children, ex.args[0], sb);
//                        sb.Append(" ? ");
//                        PrintExpr(children, ex.args[1], sb);
//                        sb.Append(" : ");
//                        PrintExpr(children, ex.args[2], sb);
//                        break;
//                    }

//                default:
//                    {
//                        if (ex.IsLangDef)
//                        {
//                            PrintExpr(children, ex.args[0], sb);
//                            sb.Append("." + ex.func.name.text + "()");
//                        }
//                        else{
//                            sb.Append(ex.func.name.text + "(");
//                            PrintExpr(children, ex.args[0], sb);
//                            sb.Append(")");
//                        }
//                        break;
//                    }

//            }
//            sb.Append(")");
//            return true;

//        }

//        //Generates the code for a Variable expression
//        private static bool PrintExpr(List<FastToken> children, Variable ex, StringBuilder sb)
//        {
//            int i = 0;
//            foreach (var c in children)
//            {
//                if (c.text == ex.token.text)
//                {
//                    sb.Append(" children[" + i + "] ");
//                    return true;
//                }
//                i++;
//            }
//            sb.Append(ex.token.text);
//            return true;
//        }

//        //Generates the code for a Value expression
//        private static bool PrintExpr(List<FastToken> children, Value ex, StringBuilder sb)
//        {
//            if (ex.token.text.Split('/').Length==2)
//                sb.Append(" ((double)" + ex.token.text + ") ");
//            else
//                sb.Append(" " + ex.token.text + " ");
//            return true;
//        }
//        #endregion


//    }



//}

