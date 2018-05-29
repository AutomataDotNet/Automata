using System;
using System.Text;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Reflection;

namespace Microsoft.Automata.Utilities
{
    internal class AutomataCSharpCompiler
    {
        string namespacename;
        string classname;
        Automaton<BDD> automaton;
        CharSetSolver solver
        {
            get
            {
                return automaton.Algebra as CharSetSolver;
            }
        }
        string source;

        internal IMatcher Compile()
        {
            var csc = new CSharpCodeProvider();
            var parameters = new CompilerParameters();  
            parameters.GenerateInMemory = true;
            parameters.CompilerOptions = "/o";  //optimize the code
            parameters.ReferencedAssemblies.Add("System.dll");
            //parameters.ReferencedAssemblies.Add("mscorlib.dll");
            parameters.ReferencedAssemblies.Add(typeof(IFiniteAutomaton).GetTypeInfo().Assembly.Location);
            CompilerResults results = csc.CompileAssemblyFromSource(parameters, source);
            if (results.Errors.HasErrors)
            {
                throw new AutomataException("C# compilation error: " + results.Errors[0].ErrorText);
            }
            else
            {
                var cls = results.CompiledAssembly.GetType(string.Format("{0}.{1}",namespacename,classname));
                var cons = cls.GetConstructor(new Type[] { });
                IDeterministicFiniteAutomaton compiledAutomaton = cons.Invoke(new object[] { }) as IDeterministicFiniteAutomaton;
                return new CompiledFiniteAutomaton(this.source, compiledAutomaton);
            }
        }

        internal AutomataCSharpCompiler(Automaton<BDD> automaton, string classname, string namespacename, bool OptimzeForAsciiInput)
        {
            this.automaton = automaton;
            this.classname = (String.IsNullOrEmpty(classname) ? "RegexMatcher" : classname);
            this.namespacename = (String.IsNullOrEmpty(namespacename) ? "GeneratedRegexMatchers" : namespacename);
            this.source = new CSharpGenerator(automaton, solver, this.classname, this.namespacename, OptimzeForAsciiInput).GenerateCS();
        }

        private class CSharpGenerator
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
                this.automaton = automaton.Normalize();
                this.namespacename = namespacename;
                this.classname = classname;
                ASCII = solver.MkCharSetFromRange('\0', '\x7F');
                helper_predicates = new HelperPredicates(solver, OptimzeForAsciiInput);
            }

            public string GenerateCS()
            {
                StringBuilder code = new StringBuilder();

                BDD finalStateBDD = solver.False;
                BDD sinkStateBDD = solver.False;
                foreach (int q in automaton.GetStates())
                {
                    if (automaton.IsFinalState(q))
                    {
                        finalStateBDD = finalStateBDD.Or(solver.MkCharConstraint((char)q));
                    }
                    if (automaton.IsLoopState(q) && automaton.GetMovesCountFrom(q) == 1)
                    {
                        sinkStateBDD = sinkStateBDD.Or(solver.MkCharConstraint((char)q));
                    }
                }
                var ranges = finalStateBDD.ToRanges();
                string finalPred = this.helper_predicates.GeneratePredicate(finalStateBDD);
                string sinkPred = this.helper_predicates.GeneratePredicate(sinkStateBDD);

                code.Append(String.Format(@"
namespace {0}
{{
    public class {1} : Microsoft.Automata.IDeterministicFiniteAutomaton
    {{

        System.Func<char, int>[] delta = new System.Func<char, int>[{2}];

        int prevStartIndex = 0;

        int currIndex = 0;

        public System.Func<char, int>[] Delta {{ get {{return delta; }} }}

        public bool IsFinalState(int x) {{ return {3}; }}

        public bool IsSinkState(int x) {{ return {4}; }}
        
        //return the state from the given state after reading the input
        public int Transition(int state, params char[] input)
        {{
            int x = state;
            for (int i = 0; i < input.Length; i++)
            {{
                x = delta[x](input[i]);
                if ({4})
                    return x;
            }}
            return x;
        }}

        public {1}()
        {{", namespacename, classname, automaton.StateCount, finalPred, sinkPred));

                for (int q = 0; q < automaton.StateCount; q++)
                {
                    code.Append(String.Format(@"
            delta[{0}] = x => 
            {{", q));
                    var path = solver.True;
                    foreach (var move in automaton.GetMovesFrom(q))
                    {
                        path = solver.MkDiff(path, move.Label);
                        if (path == solver.False) //this is the last else case
                        {
                            code.Append(String.Format(@"
                return {0};", move.TargetState));
                        }
                        else
                            code.Append(String.Format(@" 
                if ({0})
                    return {1};", helper_predicates.GeneratePredicate(move.Label), move.TargetState));
                    }
                    code.Append(String.Format(@"
            }};"));
                }
                code.Append(String.Format(@"
        }}
"));
                //adds a static IsMatch method
                code.Append(String.Format(@"
        public bool IsMatch(string input)
        {{
            var cs = input.ToCharArray();
            int k = input.Length;
            int x = 0;
            int i = -1;", namespacename, classname));

                Predicate<int> IsFinalSink = (q =>
                    (automaton.GetMovesCountFrom(q) == 1 && automaton.IsLoopState(q)
                    && automaton.IsFinalState(q) && automaton.GetMoveFrom(q).Label.IsFull));

                Predicate<int> IsNoninalSink = (q =>
                    (automaton.GetMovesCountFrom(q) == 1 && automaton.IsLoopState(q)
                    && !automaton.IsFinalState(q) && automaton.GetMoveFrom(q).Label.IsFull));

                Predicate<int> IsInitialSource = (q => q == automaton.InitialState && automaton.GetMovesCountTo(q) == 0);

                for (int q = 0; q < automaton.StateCount; q++)
                {
                    if (IsFinalSink(q))
                        code.Append(String.Format(@"
        State{0}:
            return true;", q));
                    else if (IsNoninalSink(q))
                        code.Append(String.Format(@"
        State{0}:
            return false;", q));
                    else
                    {
                        if (!IsInitialSource(q))
                            code.Append(String.Format(@"
        State{0}:", q));
                        code.Append(String.Format(@"
            i += 1;
            if (i == k)
                return {0};
            x = (int)cs[i];", automaton.IsFinalState(q).ToString().ToLower()));
                        //---------------------------------------------------------------------
                        //many potential optimizations can be made in generating the conditions
                        //---------------------------------------------------------------------
                        var path = solver.True;
                        foreach (var move in automaton.GetMovesFrom(q))
                        {
                            path = solver.MkDiff(path, move.Label);
                            if (path == solver.False) //this is the last else case
                            {
                                BDD qBDD = solver.MkCharConstraint((char)q);
                                code.Append(String.Format(@"
            goto State{0};", move.TargetState));
                            }
                            else
                                code.Append(String.Format(@" 
            if ({0})
                goto State{1};", helper_predicates.GeneratePredicate(move.Label), move.TargetState));
                        }
                    }
                }
                code.Append(@"
        }");

                //adds a static GenerateMatches method
                code.Append(String.Format(@"
        public int GenerateMatches(string input, System.Tuple<int,int>[] matches)
        {{
            var cs = input.ToCharArray();
            int i0 = 0;
            int q = 0;
            int j = 0;
            for (int i = 0; i < cs.Length; i++)
            {{
                if (j == matches.Length)
                    return j;
                if (q == 0) 
                    i0 = i;
                q = this.delta[q](cs[i]);
                if (this.IsFinalState(q))
                {{ 
                    matches[j] = new System.Tuple<int,int>(i0,i);
                    j += 1;
                    q = 0;
                }}
            }}
            return j;
        }}", classname));

                code.Append(helper_predicates.ToString());
                code.Append(@"
    }
}");
                return code.ToString();
            }
        }
    }
}