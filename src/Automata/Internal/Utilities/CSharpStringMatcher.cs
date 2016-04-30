using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Reflection;

using Microsoft.Automata;

namespace Microsoft.Automata.Internal.Utilities
{
    internal class CSharpStringMatcher : ICompiledStringMatcher
    {
        string namespacename;
        string classname;
        Automaton<BDD> automaton;
        CharSetSolver solver;
        string source;
        MethodInfo matcher;

        internal CSharpStringMatcher(Automaton<BDD> automaton, CharSetSolver solver, string classname, string namespacename, bool OptimzeForAsciiInput)
        {
            this.automaton = automaton;
            this.solver = solver;
            this.classname = classname;
            this.namespacename = namespacename;
            this.source = new CSharpGenerator(automaton, solver, classname, namespacename, OptimzeForAsciiInput).GenerateCS();
            this.matcher = CompiledStringMatcher();
        }

        public string SourceCode
        {
            get { return source; }
        }

        MethodInfo CompiledStringMatcher()
        {
            var csc = new CSharpCodeProvider();
            var parameters = new CompilerParameters();
            parameters.GenerateInMemory = true;
            parameters.CompilerOptions = "/o";  //optimize the code
            parameters.ReferencedAssemblies.Add("System.dll");
            CompilerResults results = csc.CompileAssemblyFromSource(parameters, source);
            if (results.Errors.HasErrors)
            {
                throw new AutomataException("C# compilation error: " + results.Errors[0].ErrorText);
            }
            else
            {
                var cls = results.CompiledAssembly.GetTypes()[0];
                var mi = cls.GetMethod("IsMatch", BindingFlags.Static | BindingFlags.Public);
                return mi;
            }
        }

        public bool IsMatch(string input)
        {
            if (matcher == null)
                throw new AutomataException("Source is not compiled");
            try
            {
                bool res = (bool)matcher.Invoke(null, new object[] { input });
                return res;
            }
            catch (TargetInvocationException e)
            {
                throw e.InnerException;
            }
        }
    }

    internal class CSharpGenerator
    {
        CharSetSolver solver;
        Automaton<BDD> automaton;
        string namespacename;
        string classname;
        HelperPredicates helper_predicates;
        BDD ASCII;

        public CSharpGenerator(Automaton<BDD> automaton, CharSetSolver solver, string classname, string namespacename, bool OptimzeForAsciiInput = true)
        {
            this.solver = solver;
            this.automaton = automaton;
            this.namespacename = namespacename;
            this.classname = classname;
            ASCII = solver.MkCharSetFromRange('\0', '\x7F');
            helper_predicates = new HelperPredicates(solver, OptimzeForAsciiInput);
        }

        public string GenerateCS()
        {
            StringBuilder code = new StringBuilder();
            var stack = new SimpleStack<int>();
            var set = new HashSet<int>();
            stack.Push(automaton.InitialState);
            set.Add(automaton.InitialState);
            code.Append(String.Format(@"
namespace {0}
{{
    public class {1}
    {{
        public static bool IsMatch(string input)
        {{
            var cs = input.ToCharArray();
            int k = input.Length;
            int c = 0;
            int i = -1;", namespacename, classname));

            Predicate<int> IsFinalSink = (q =>
                (automaton.GetMovesCountFrom(q) == 1 && automaton.IsLoopState(q)
                && automaton.IsFinalState(q) && automaton.GetMoveFrom(q).Label.IsFull));

            Predicate<int> IsNonfinalSink = (q => (automaton.GetMovesCountFrom(q) == 0));

            Predicate<int> IsInitialSource = (q => q == automaton.InitialState && automaton.GetMovesCountTo(q) == 0);

            while (stack.IsNonempty)
            {
                int q = stack.Pop();
                bool q_is_complete = false;
                if (IsFinalSink(q))
                    code.Append(String.Format(@"
        State{0}:
            return true;", q));
                else
                {
                    if (!IsInitialSource(q))
                        code.Append(String.Format(@"
        State{0}:", q));
                    code.Append(String.Format(@"
            i += 1;
            if (i == k)
                return {0};
            c = (int)cs[i];", automaton.IsFinalState(q).ToString().ToLower()));
                    //---------------------------------------------------------------------
                    //many potential optimizations can be made in generating the conditions
                    //---------------------------------------------------------------------
                    var path = solver.True;
                    foreach (var move in automaton.GetMovesFrom(q))
                    {
                        path = solver.MkDiff(path, move.Label);
                        if (path == solver.False) //this is the last else case
                        {
                            code.Append(String.Format(@"
            goto State{0};", move.TargetState));
                            q_is_complete = true;
                        }
                        else
                            code.Append(String.Format(@" 
            if ({0})
                goto State{1};", helper_predicates.GeneratePredicate(move.Label), move.TargetState));
                        if (set.Add(move.TargetState))
                            stack.Push(move.TargetState);
                    }
                    if (!q_is_complete)
                        //reject the input, this corresponds to q being a partial state 
                        //the implicit transition is to a deadend sink state
                        code.Append(@"
            return false;");
                }
            }
            code.Append(@"
        }");
            code.Append(helper_predicates.ToString());
            code.Append(@"
    }
}");
            return code.ToString();
        }
    }
}