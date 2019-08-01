using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Automata;

namespace Microsoft.Automata.Grammars
{
    /// <summary>
    /// Symbolic Context Free Grammar
    /// </summary>
    public class ContextFreeGrammar
    {
        List<Nonterminal> variables;
        List<GrammarSymbol> terminals;
        Nonterminal startSymbol;
        Dictionary<Nonterminal, List<Production>> productionMap;
        List<Production> allProductions;

        /// <summary>
        /// Returns the underlying builtin character set solver if it was used for parsing terminals, returns null othewise.
        /// </summary>
        public CharSetSolver BuiltinTerminalAlgebra
        {
            get
            {
                if (terminals.Count == 0)
                    return null;
                Terminal<BDD> t = terminals[0] as Terminal<BDD>;
                if (t == null)
                    return null;
                var solver = t.term.algebra as CharSetSolver;
                return solver;
            }
        }

        public int ProductionCount
        {
            get
            {
                return allProductions.Count;
            }
        }

        public int NonterminalCount
        {
            get
            {
                return variables.Count;
            }
        }

        public int TerminalCount
        {
            get
            {
                return terminals.Count;
            }
        }

        public List<Nonterminal> Nonterminals
        {
            get { return variables; }
        }

        public List<GrammarSymbol> Terminals
        {
            get { return terminals; }
        }

        public Nonterminal StartSymbol
        {
            get { return startSymbol; }
        }

        public bool IsValidNonterminal(Nonterminal v)
        {
            return productionMap.ContainsKey(v);
        }

        public IEnumerable<GrammarSymbol> GetTerminals()
        {
            var set = new HashSet<GrammarSymbol>();
            foreach (Production p in GetProductions())
                foreach (GrammarSymbol s in p.Rhs)
                    if (! (s is Nonterminal) && set.Add(s))
                        yield return s;
        }

        public IList<Production> GetProductions(Nonterminal v)
        {
            return productionMap[v];
        }

        public ContextFreeGrammar(Nonterminal startSymbol, IEnumerable<Production> productions)
        {
            HashSet<Nonterminal> vars = new HashSet<Nonterminal>();
            terminals = new List<GrammarSymbol>();
            HashSet<GrammarSymbol> terms = new HashSet<GrammarSymbol>();
            List<Nonterminal> varsList = new List<Nonterminal>();
            bool startSymbolExisted = false;
            allProductions = new List<Production>(productions);
            foreach (Production p in allProductions)
            {
                if (p.Lhs.Equals(startSymbol))
                    startSymbolExisted = true;
                if (vars.Add(p.Lhs))
                    varsList.Add(p.Lhs);
                foreach (GrammarSymbol s in p.Rhs)
                {
                    Nonterminal v = s as Nonterminal;
                    if (v != null)
                    {
                        if (vars.Add(v))
                            varsList.Add(v);
                    }
                    else
                    {
                        if (terms.Add(s))
                            terminals.Add(s);
                    }
                }
            }
            if (!startSymbolExisted)
                throw new AutomataException(AutomataExceptionKind.StartSymbolOfContextFreeGrammarIsUnreachable);

            this.variables = varsList;
            this.startSymbol = startSymbol;
            var prodMap = new Dictionary<Nonterminal, List<Production>>();
            foreach (Nonterminal v in varsList)
                prodMap.Add(v, new List<Production>());
            foreach (Production p in productions)
                prodMap[p.Lhs].Add(p);
            this.productionMap = prodMap;
        }

        internal ContextFreeGrammar(List<Nonterminal> variables, Nonterminal startSymbol, Dictionary<Nonterminal, List<Production>> productionMap, List<GrammarSymbol> terminals)
        {
            this.variables = variables;
            this.startSymbol = startSymbol;
            this.productionMap = productionMap;
            this.terminals = terminals;
            this.allProductions = new List<Production>();
            foreach (var entry in productionMap)
                this.allProductions.AddRange(entry.Value);
        }

        public IList<Production> GetProductions()
        {
            return allProductions;
        }

        /// <summary>
        /// Restrict the grammar to the given variables.
        /// </summary>
        public ContextFreeGrammar RestrictToVariables(HashSet<Nonterminal> varSet)
        {
            Dictionary<Nonterminal, List<Production>> productions = new Dictionary<Nonterminal, List<Production>>();
            foreach (Nonterminal v in varSet)
                productions[v] = new List<Production>();

            List<Nonterminal> varList = new List<Nonterminal>();
            foreach (Nonterminal v in variables)
                if (varSet.Contains(v))
                {
                    varList.Add(v);
                    foreach (Production p in productionMap[v])
                        if (p.AreVariablesContainedIn(varSet))
                            productions[v].Add(p);
                }
            if (!productions.ContainsKey(startSymbol) || productions[startSymbol].Count == 0)
                throw new ArgumentException("Start symbol is not the LHS of any production.");

            ContextFreeGrammar g = new ContextFreeGrammar(varList, startSymbol, productions, terminals);
            return g;
        }

        /// <summary>
        /// Removes useless symbols from the grammar.
        /// Assumes that the language is nonempty.
        /// </summary>
        public ContextFreeGrammar RemoveUselessSymbols()
        {
            HashSet<Nonterminal> useful_backwards = new HashSet<Nonterminal>();

            //Lemma 4.1, p. 88, Hopcroft-Ullman
            #region backward reachability
            var variableNodeMap = new Dictionary<Nonterminal, VariableNode>();
            foreach (Nonterminal v in this.variables)
                variableNodeMap[v] = new VariableNode();


            List<ProductionNode> productionLeaves = new List<ProductionNode>();

            foreach (Nonterminal v in this.variables)
            {
                VariableNode parent = variableNodeMap[v];
                foreach (Production p in this.productionMap[v])
                {
                    var children = Array.ConvertAll(new List<Nonterminal>(p.GetNonterminals()).ToArray(), w => variableNodeMap[w]);
                    ProductionNode pn = new ProductionNode(parent, children);
                    if (children.Length == 0)
                        productionLeaves.Add(pn);
                    else
                        foreach (VariableNode child in children)
                            child.parents.Add(pn);
                }
            }

            foreach (ProductionNode leaf in productionLeaves)
                leaf.PropagateMark();

            foreach (Nonterminal v in this.variables)
                if (variableNodeMap[v].isMarked)
                    useful_backwards.Add(v);
            #endregion

            if (!useful_backwards.Contains(this.startSymbol))
                throw new AutomataException(AutomataExceptionKind.GrammarNotInGNF);

            ContextFreeGrammar g1 = this.RestrictToVariables(useful_backwards);

            HashSet<Nonterminal> useful_forwards = new HashSet<Nonterminal>();

            //Lemma 4.2, p. 89, Hopcroft-Ullman
            #region forward reachability
            Stack<Nonterminal> stack = new Stack<Nonterminal>();
            stack.Push(g1.StartSymbol);
            useful_forwards.Add(g1.StartSymbol);

            while (stack.Count > 0)
            {
                Nonterminal v = stack.Pop();
                foreach (Production p in g1.GetProductions(v))
                    foreach (Nonterminal u in p.GetNonterminals())
                        if (!useful_forwards.Contains(u))
                        {
                            useful_forwards.Add(u);
                            stack.Push(u);
                        }
            }

            #endregion

            ContextFreeGrammar g2 = g1.RestrictToVariables(useful_forwards);

            return g2;
        }


        /// <summary>
        /// Return all useful nonterminal symbols.
        /// </summary>
        public HashSet<string> GetUsefulNonterminals()
        {
            return GetUsefulNonterminals(false);
        }

        /// <summary>
        /// Return all useful nonterminal symbols. If checkBackwardsOnly is true, assume that all symbols are reachable from the start symbol.
        /// </summary>
        public HashSet<string> GetUsefulNonterminals(bool checkBackwardsOnly)
        {
            HashSet<Nonterminal> useful_backwards = new HashSet<Nonterminal>();

            //Lemma 4.1, p. 88, Hopcroft-Ullman
            #region backward reachability
            var variableNodeMap = new Dictionary<Nonterminal, VariableNode>();
            foreach (Nonterminal v in this.variables)
                variableNodeMap[v] = new VariableNode();


            List<ProductionNode> productionLeaves = new List<ProductionNode>();

            foreach (Nonterminal v in this.variables)
            {
                VariableNode parent = variableNodeMap[v];
                foreach (Production p in this.productionMap[v])
                {
                    var children = Array.ConvertAll(new List<Nonterminal>(p.GetNonterminals()).ToArray(), w => variableNodeMap[w]);
                    ProductionNode pn = new ProductionNode(parent, children);
                    if (children.Length == 0)
                        productionLeaves.Add(pn);
                    else
                        foreach (VariableNode child in children)
                            child.parents.Add(pn);
                }
            }

            foreach (ProductionNode leaf in productionLeaves)
                leaf.PropagateMark();

            foreach (Nonterminal v in this.variables)
                if (variableNodeMap[v].isMarked)
                    useful_backwards.Add(v);
            #endregion

            //returns the empty set because the language is empty
            if (!useful_backwards.Contains(this.startSymbol))
                return new HashSet<string>();

            //don't bother to check forward
            if (checkBackwardsOnly)
            {
                var res = new HashSet<string>();
                foreach (var nt in useful_backwards)
                    res.Add(nt.Name);
                return res;
            }

            ContextFreeGrammar g1 = this.RestrictToVariables(useful_backwards);

            HashSet<Nonterminal> useful_forwards = new HashSet<Nonterminal>();

            //Lemma 4.2, p. 89, Hopcroft-Ullman
            #region forward reachability
            Stack<Nonterminal> stack = new Stack<Nonterminal>();
            stack.Push(g1.StartSymbol);
            useful_forwards.Add(g1.StartSymbol);

            while (stack.Count > 0)
            {
                Nonterminal v = stack.Pop();
                foreach (Production p in g1.GetProductions(v))
                    foreach (Nonterminal u in p.GetNonterminals())
                        if (!useful_forwards.Contains(u))
                        {
                            useful_forwards.Add(u);
                            stack.Push(u);
                        }
            }

            #endregion

            HashSet<string> usefulSymbols = new HashSet<string>();
            foreach (var nt in useful_forwards)
                if (useful_backwards.Contains(nt))
                    usefulSymbols.Add(nt.Name);
            return usefulSymbols;
        }

        /// <summary>
        /// Removes all productions of the form A->B where A and B are variables.
        /// Removes also all the useless symbols after the unit production elimination.
        /// Assumes that the grammar has no epsilon productions. 
        /// </summary>
        public ContextFreeGrammar RemoveUnitProductions()
        {
            var newProductions = new Dictionary<Nonterminal, List<Production>>();
            foreach (Nonterminal v in variables)
                newProductions[v] = new List<Production>();

            foreach (Nonterminal v in variables)
                foreach (Nonterminal u in GetUnitClosure(v))
                    foreach (Production p in productionMap[u])
                        if (!p.IsUnit)
                            newProductions[v].Add(new Production(v, p.Rhs));

            ContextFreeGrammar g = new ContextFreeGrammar(variables, startSymbol, newProductions, terminals);
            ContextFreeGrammar g1 = g.RemoveUselessSymbols();
            return g1;
        }

        HashSet<Nonterminal> GetUnitClosure(Nonterminal v)
        {
            HashSet<Nonterminal> res = new HashSet<Nonterminal>();
            res.Add(v);
            Stack<Nonterminal> stack = new Stack<Nonterminal>();
            stack.Push(v);
            while (stack.Count > 0)
                foreach (Production p in productionMap[stack.Pop()])
                        if (p.IsUnit)
                            if (res.Add((Nonterminal)p.First)) //p.First is a new variable that is added to res
                                stack.Push((Nonterminal)p.First);
            return res;
        }

        /// <summary>
        /// Removes epsilon productions and then removes useless symbols.
        /// Assumes that the grammar does not accept the empty string and that the language is nonempty.
        /// </summary>
        public ContextFreeGrammar RemoveEpsilonsAndUselessSymbols()
        {
            //--- eliminate epsilon productions
            //based on algo in Theorem 4.3, p. 90-91, Hopcroft-Ullman
            HashSet<Nonterminal> nullables = GetNullables();

            Dictionary<Nonterminal, List<Production>> prodMap = new Dictionary<Nonterminal, List<Production>>();
            foreach (Nonterminal v in this.variables)
                prodMap[v] = new List<Production>(EliminateNullables(v, nullables));
            ContextFreeGrammar g1 = new ContextFreeGrammar(this.variables, this.StartSymbol, prodMap, terminals);

            ContextFreeGrammar g2 = g1.RemoveUselessSymbols();

            return g2;
        }

        #region Helper functions in RemoveEpsilonsAndUselessSymbols
        HashSet<Nonterminal> GetNullables()
        {
            HashSet<Nonterminal> nullables = new HashSet<Nonterminal>();

            var variableNodeMap = new Dictionary<Nonterminal, VariableNode>();
            foreach (Nonterminal v in this.variables)
                variableNodeMap[v] = new VariableNode();


            List<ProductionNode> productionLeaves = new List<ProductionNode>();

            foreach (Nonterminal v in this.variables)
            {
                VariableNode parent = variableNodeMap[v];
                foreach (Production p in this.productionMap[v])
                {
                    if (p.ContainsOnlyNonterminals)
                    {
                        VariableNode[] children =
                            Array.ConvertAll(p.Rhs, s => variableNodeMap[(Nonterminal)s]);

                        ProductionNode productionNode = new ProductionNode(parent, children);
                        if (children.Length == 0)
                            productionLeaves.Add(productionNode);
                        foreach (VariableNode child in children)
                            child.parents.Add(productionNode);
                    }
                }
            }

            foreach (ProductionNode leaf in productionLeaves)
                leaf.PropagateMark();

            foreach (Nonterminal v in this.variables)
                if (variableNodeMap[v].isMarked)
                    nullables.Add(v);
            return nullables;
        }

        IEnumerable<Production> EliminateNullables(Nonterminal v, HashSet<Nonterminal> nullables)
        {
            foreach (var p in this.productionMap[v])
            {
                if (p.Rhs.Length == 0)
                    yield break;

                foreach (var symbols in EnumerateNullableFreeVariations(ConsList<GrammarSymbol>.Create(p.Rhs), nullables))
                    if (symbols != null) //ignore the case when all nullables were replaced 
                        yield return new Production(v, symbols.ToArray());
            }
        }

        static IEnumerable<ConsList<GrammarSymbol>> EnumerateNullableFreeVariations(ConsList<GrammarSymbol> symbols, HashSet<Nonterminal> nullables)
        {
            if (symbols == null)
                yield return null;
            else
                foreach (var rest in EnumerateNullableFreeVariations(symbols.Rest, nullables))
                {
                    GrammarSymbol first = symbols.First;
                    Nonterminal variable = first as Nonterminal;
                    if (variable == null || !nullables.Contains(variable))
                        yield return new ConsList<GrammarSymbol>(first, rest);
                    else
                    {
                        yield return rest;
                        yield return new ConsList<GrammarSymbol>(first, rest);
                    }
                }
        }
        #endregion

        /// <summary>
        /// Returns MkCNF(g, true)
        /// </summary>
        public static ContextFreeGrammar MkCNF(ContextFreeGrammar g)
        {
            return MkCNF(g, true);
        }
        /// <summary>
        /// Produces the CNF (Chomsky Normal Form) for the grammar g.
        /// It first eliminates epsilons, useless symbols, and unit productions.
        /// If Assumes that there are no epsilons, useless symbols or unit productions
        /// </summary>
        public static ContextFreeGrammar MkCNF(ContextFreeGrammar g, bool removeEpsilonsUselessSymbolsUnitsProductions)
        {
            //if (removeEpsilonsUselessSymbolsUnitsProductions)
                g = g.RemoveEpsilonsAndUselessSymbols().RemoveUnitProductions();
            var productions = new Dictionary<Nonterminal, List<Production>>();
            List<Nonterminal> variables = new List<Nonterminal>(g.variables);
            foreach (Nonterminal v in g.variables)
                productions[v] = new List<Production>();

            int nonterminalID = 0;

            //Implements algo in Theorem 4.5, page 92-93, in Hopcroft-Ullman

            #region make productions of the form V --> V0...Vn or V --> a
            var freshVarMap = new Dictionary<GrammarSymbol, Nonterminal>();
            foreach (Nonterminal v in g.variables)
                foreach (Production p in g.productionMap[v])
                    if (p.ContainsOnlyNonterminals || p.IsCNF)
                        productions[v].Add(p);
                    else
                    {
                        GrammarSymbol[] rhs = new GrammarSymbol[p.Rhs.Length];
                        for (int i = 0; i < rhs.Length; i++)
                        {
                            if (p.Rhs[i] is Nonterminal)
                                rhs[i] = p.Rhs[i];
                            else
                            {
                                Nonterminal u;
                                if (!freshVarMap.TryGetValue(p.Rhs[i], out u))
                                {
                                    u = Nonterminal.MkNonterminalForId(nonterminalID++);
                                    freshVarMap[p.Rhs[i]] = u;
                                    variables.Add(u);
                                    var prods = new List<Production>();
                                    prods.Add(new Production(u, p.Rhs[i]));
                                    productions[u] = prods;
                                }
                                rhs[i] = u;
                            }
                        }
                        productions[v].Add(new Production(v, rhs));
                    }
            #endregion


            var productionsCNF = new Dictionary<Nonterminal, List<Production>>();
            List<Nonterminal> variablesCNF = new List<Nonterminal>(variables);
            foreach (Nonterminal v in variablesCNF)
                productionsCNF[v] = new List<Production>();

            #region replace V --> V0V1...Vn (n > 2), by V --> V0U0, U0 --> V1U1, ..., Un-2 --> Vn-1Vn
            foreach (Nonterminal v in variables)
                foreach (Production p in productions[v])
                    if (p.IsCNF)
                        productionsCNF[v].Add(p);
                    else
                    {
                        Nonterminal x = v;
                        Nonterminal y = Nonterminal.MkNonterminalForId(nonterminalID++);
                        variablesCNF.Add(y);
                        productionsCNF[y] = new List<Production>();
                        for (int i = 0; i < p.Rhs.Length - 2; i++)
                        {
                            productionsCNF[x].Add(new Production(x, p.Rhs[i], y));
                            if (i < p.Rhs.Length - 3)
                            {
                                x = y;
                                y = Nonterminal.MkNonterminalForId(nonterminalID++);
                                variablesCNF.Add(y);
                                productionsCNF[y] = new List<Production>();
                            }
                        }
                        productionsCNF[y].Add(new Production(y, p.Rhs[p.Rhs.Length - 2], p.Rhs[p.Rhs.Length - 1]));
                    }
            #endregion

            ContextFreeGrammar cnf = new ContextFreeGrammar(variablesCNF, g.startSymbol, productionsCNF, g.terminals);
            return cnf;
        }

        /// <summary>
        /// Produces the GNF (Greibach Normal Form) for the grammar g.
        /// If g is not already in GNF, first makes CNF.
        /// Implements a variation of the Koch-Blum algorithm. (STACS 97, pp. 47-54)
        /// </summary>
        /// <param name="g"></param>
        /// <param name="removeEpsilonsUselessSymbolsUnitsProductions"></param>
        /// <returns></returns>
        public static ContextFreeGrammar MkGNF(ContextFreeGrammar g, bool removeEpsilonsUselessSymbolsUnitsProductions = true)
        {
            if (removeEpsilonsUselessSymbolsUnitsProductions)
               g = g.RemoveEpsilonsAndUselessSymbols().RemoveUnitProductions();
            if (g.IsInGNF())
                return g;

            ContextFreeGrammar cnf = MkCNF(g,false);
            var Vars = cnf.variables;

            int nonterminalID = 0;

            var M = new Dictionary<Nonterminal, Automaton<GrammarSymbol>>();

            #region construct the automata M[B] for all variables B
            int id = 0;
            var initStateMap = new Dictionary<Nonterminal, int>();
            var finalStateMap = new Dictionary<Nonterminal, int>();
            foreach (Nonterminal B in Vars)
            {
                initStateMap[B] = id++;
                finalStateMap[B] = id++;
            }

            var movesOfM = new Dictionary<Nonterminal, List<Move<GrammarSymbol>>>();

            foreach (Nonterminal B in Vars)
                movesOfM[B] = new List<Move<GrammarSymbol>>();

            #region construct the moves of the automata
            foreach (Nonterminal B in Vars)
            {
                var variableToStateMap = new Dictionary<Nonterminal, int>();
                Stack<Nonterminal> stack = new Stack<Nonterminal>();
                stack.Push(B); 
                int initState = initStateMap[B];
                variableToStateMap[B] = finalStateMap[B];
                while (stack.Count > 0)
                {
                    Nonterminal C = stack.Pop();
                    foreach (Production p in cnf.GetProductions(C))
                    {
                        if (p.IsSingleTerminal)
                            movesOfM[B].Add(Move<GrammarSymbol>.Create(initState, variableToStateMap[C], p.First));
                        else
                        {
                            Nonterminal D = (Nonterminal)p.First; //using the fact that the grammar is in CNF
                            if (!variableToStateMap.ContainsKey(D))
                            {
                                //visit all variables reachable that have not already been visited
                                variableToStateMap.Add(D,id++);
                                stack.Push(D);
                            }
                            GrammarSymbol E = p.Rhs[1];
                            movesOfM[B].Add(Move<GrammarSymbol>.Create(variableToStateMap[D], variableToStateMap[C], E));
    
                        }
                    }
                }
            }
            #endregion

            foreach (Nonterminal B in Vars)
                M[B] = Automaton<GrammarSymbol>.Create(null, initStateMap[B], new int[] {finalStateMap[B]}, movesOfM[B]);
            #endregion

            var G_ = new Dictionary<Nonterminal, ContextFreeGrammar>();

            #region construct corresponding intermediate grammars G_[B] corresponding to M[B] 
            foreach (Nonterminal B in Vars)
            {
                var MB = M[B];
                bool MBfinalStateHasVariableMoves = FinalStateHasVariableMoves(MB);
                var productions = new Dictionary<Nonterminal, List<Production>>();
                Nonterminal startSymbol = Nonterminal.MkNonterminalForId(nonterminalID++);
                var vars = new List<Nonterminal>();
                vars.Add(startSymbol);
                productions[startSymbol] = new List<Production>();

                foreach (var move in MB.GetMovesFrom(MB.InitialState))
                {
                    if (move.TargetState == MB.FinalState)
                        productions[startSymbol].Add(new Production(startSymbol, move.Label));
                    if (move.TargetState != MB.FinalState || MBfinalStateHasVariableMoves)
                    {
                        var C = Nonterminal.MkNonterminalForStateId(move.TargetState);
                        productions[startSymbol].Add(new Production(startSymbol, move.Label, C));
                        if (!productions.ContainsKey(C))
                        {
                            productions[C] = new List<Production>();
                            vars.Add(C);
                        }
                    }
                }

                foreach (int state in MB.States)
                    if (state != MB.InitialState)
                        foreach (Move<GrammarSymbol> move in MB.GetMovesFrom(state))
                        {
                            Nonterminal D = Nonterminal.MkNonterminalForStateId(state);
                            Nonterminal C = Nonterminal.MkNonterminalForStateId(move.TargetState);
                            if (!productions.ContainsKey(D))
                            {
                                productions[D] = new List<Production>();
                                vars.Add(D);
                            }
                            Nonterminal E = (Nonterminal)move.Label;
                            if (move.TargetState == MB.FinalState)
                                productions[D].Add(new Production(D, E));
                            if (move.TargetState != MB.FinalState || MBfinalStateHasVariableMoves)
                            {
                                productions[D].Add(new Production(D, E, C));
                                //we pretend here that E is a terminal
                                if (!productions.ContainsKey(C))
                                {
                                    productions[C] = new List<Production>();
                                    vars.Add(C);
                                }
                            }
                        }
                G_[B] = new ContextFreeGrammar(vars, startSymbol, productions, g.terminals);
            }
            #endregion

            var G = new Dictionary<Nonterminal, ContextFreeGrammar>();

            #region construct the corresponding temporary G[B]'s
            foreach (Nonterminal B in Vars)
            {
                var G_B = G_[B];
                var productions = new Dictionary<Nonterminal, List<Production>>();
                //var vars = new List<Variable>();
                Nonterminal startSymbol = G_B.startSymbol;
                productions[startSymbol] = G_B.productionMap[startSymbol];
                foreach (Nonterminal D in G_B.variables)
                    if (!D.Equals(startSymbol))
                    {
                        var productions_D = new List<Production>();
                        productions[D] = productions_D;
                        foreach (Production p in G_B.productionMap[D])
                        {
                            Nonterminal E = (Nonterminal)p.First;
                            var G_E = G_[E];
                            if (p.IsUnit)
                                foreach (Production q in G_E.productionMap[G_E.startSymbol])
                                    productions_D.Add(new Production(D, q.Rhs));
                            else
                                foreach (Production q in G_E.productionMap[G_E.startSymbol])
                                {
                                    GrammarSymbol[] symbols = new GrammarSymbol[q.Rhs.Length + 1];
                                    Array.Copy(q.Rhs, symbols, q.Rhs.Length);
                                    symbols[q.Rhs.Length] = p.Rhs[1];
                                    productions_D.Add(new Production(D, symbols));
                                }
                        }
                    }
                //ignore the variable list, it is not used
                G[B] = new ContextFreeGrammar(null, startSymbol, productions, g.terminals);
            }
            #endregion

            #region construct the final GNF from the G[B]'s
            var productionsGNF = new List<Production>();
            foreach (Nonterminal A in cnf.variables)
            {
                foreach (Production p in cnf.productionMap[A])
                {
                    if (p.IsSingleTerminal)
                        productionsGNF.Add(p);
                    else
                    {
                        Nonterminal B = (Nonterminal)p.Rhs[0];
                        Nonterminal C = (Nonterminal)p.Rhs[1];
                        var GB = G[B];
                        foreach (Production q in GB.productionMap[GB.startSymbol])
                        {
                            GrammarSymbol[] symbols = new GrammarSymbol[q.Rhs.Length + 1];
                            Array.Copy(q.Rhs, symbols, q.Rhs.Length);
                            symbols[q.Rhs.Length] = C;
                            productionsGNF.Add(new Production(A, symbols));
                        }
                    }
                }
            }
            foreach (Nonterminal B in Vars)
            {
                var GB = G[B];
                foreach (var kv in GB.productionMap)
                    if (!kv.Key.Equals(GB.startSymbol))
                        productionsGNF.AddRange(kv.Value);
            }
            #endregion

            ContextFreeGrammar gnf = new ContextFreeGrammar(cnf.startSymbol, productionsGNF);
            if (removeEpsilonsUselessSymbolsUnitsProductions)
                gnf = gnf.RemoveEpsilonsAndUselessSymbols().RemoveUnitProductions();
            return gnf;
        }

        /// <summary>
        /// Returns true if the grammar is in GNF (Greibach Normal Form)
        /// </summary>
        public bool IsInGNF()
        {
            foreach (Production p in GetProductions())
                if (!p.IsGNF)
                    return false;
            return true;
        }

        /// <summary>
        /// Returns true if the grammar is in CNF (Chomsky Normal Form)
        /// </summary>
        public bool IsInCNF()
        {
            foreach (Production p in GetProductions())
                if (!p.IsCNF)
                    return false;
            return true;
        }

        /// <summary>
        /// Produces the EGNF (Extended Greibach Normal Form) for the grammar g. 
        /// The grammar g can be arbitrary. First removes epsilons and useless symbols from g.
        /// Implements a variation of the Blum-Koch algorithm. 
        /// (Inf. and Comp. vol.150, pp.112-118, 1999)
        /// </summary>
        /// <param name="g">the grammar to be normalized</param>
        /// <returns>Extended Greibach Normal Form of g</returns>
        public static ContextFreeGrammar MkEGNF(ContextFreeGrammar g)
        {
            return MkEGNF(g, true);
        }


        /// <summary>
        /// Produces the EGNF (Extended Greibach Normal Form) for the grammar g. 
        /// Implements a variation of the Blum-Koch algorithm. 
        /// (Inf. and Comp. vol.150, pp.112-118, 1999)
        /// </summary>
        /// <param name="g">the grammar to be normalized</param>
        /// <param name="removeEpsilonsAndUselessSymbols">if true, first removes epsilons and useless symbols, otherwise assumes that epsilons do not occur</param>
        /// <returns>Extended Greibach Normal Form of g</returns>
        public static ContextFreeGrammar MkEGNF(ContextFreeGrammar g, bool removeEpsilonsAndUselessSymbols)
        {
            if (removeEpsilonsAndUselessSymbols)
                g = g.RemoveEpsilonsAndUselessSymbols();

            if (g.IsInGNF())
                return g;

            var leavesP = new List<Production>();
            var revP = new Dictionary<Nonterminal, List<Tuple<GrammarSymbol[], Nonterminal>>>();

            int nonterminalID = 0;

            #region compute leavesP and revP
            foreach (Nonterminal v in g.variables)
                revP[v] = new List<Tuple<GrammarSymbol[], Nonterminal>>();

            foreach (Production p in g.GetProductions())
                if (!(p.First is Nonterminal))
                    leavesP.Add(p);
                else
                    revP[(Nonterminal)p.First].Add(new Tuple<GrammarSymbol[], Nonterminal>(p.Rest, p.Lhs));
            #endregion

            var W = new Dictionary<Nonterminal, HashSet<Nonterminal>>();
            var startSymbol = new Dictionary<Nonterminal, Nonterminal>();

            #region create new start symbols and compute unit closures
            foreach (Nonterminal v in g.variables)
            {
                W[v] = g.GetUnitClosure(v);
                startSymbol[v] = Nonterminal.MkNonterminalForId(nonterminalID++);
            }
            #endregion

            var P = new Dictionary<Nonterminal, List<Production>>();

            #region construct intermediate productions in P for each variable B
            foreach (Nonterminal B in g.variables)
            {
                var S_B = startSymbol[B];
                var W_B = W[B]; //unit closure of B
                var Bvar = new Dictionary<Nonterminal, Nonterminal>();
                Stack<Nonterminal> stack = new Stack<Nonterminal>();
                HashSet<Nonterminal> visited = new HashSet<Nonterminal>();
                var S_B_list = new List<Production>();
                P[S_B] = S_B_list;
                foreach (Production p in leavesP)
                {
                    S_B_list.Add(new Production(S_B, p.Rhs, Lookup(Bvar, p.Lhs, ref nonterminalID)));
                    if (visited.Add(p.Lhs))
                        stack.Push(p.Lhs);
                    if (W_B.Contains(p.Lhs))
                        S_B_list.Add(new Production(S_B, p.Rhs));
                }

                while (stack.Count > 0)
                {
                    Nonterminal C = stack.Pop();
                    Nonterminal C_B = Lookup(Bvar, C, ref nonterminalID);
                    List<Production> C_B_list;
                    if (!P.TryGetValue(C_B, out C_B_list))
                    {
                        C_B_list = new List<Production>();
                        P[C_B] = C_B_list;
                    }
                    foreach (var t in revP[C])
                    {
                        Nonterminal D = t.Item2;
                        Nonterminal D_B = Lookup(Bvar, D, ref nonterminalID);
                        C_B_list.Add(new Production(C_B, t.Item1, D_B));
                        if (t.Item1.Length > 0 && W_B.Contains(D))
                            C_B_list.Add(new Production(C_B, t.Item1));
                        if (visited.Add(D))
                            stack.Push(D);
                    }
                }
            }
            #endregion


            //produce the union of P and g.productionMap in H
            //and replace each production 'A ::= B alpha' by 'A ::= S_B alpha"

            var Hprods = new Dictionary<Nonterminal, List<Production>>();
            #region compute Hprods
            foreach (Nonterminal A in g.variables)
            {
                var A_prods = new List<Production>();
                Hprods[A] = A_prods;
                foreach (Production p in g.productionMap[A])
                {
                    if (p.First is Nonterminal && !p.IsUnit)
                    {
                        GrammarSymbol[] rhs = new GrammarSymbol[p.Rhs.Length];
                        rhs[0] = startSymbol[(Nonterminal)p.First];
                        Array.Copy(p.Rhs, 1, rhs, 1, rhs.Length - 1);
                        Production q = new Production(p.Lhs, rhs);
                        A_prods.Add(q);
                    }
                    else
                        A_prods.Add(p);
                }
            }
            foreach (Nonterminal A in P.Keys)
            {
                var A_prods = new List<Production>();
                Hprods[A] = A_prods;
                foreach (Production p in P[A])
                {
                    if (p.First is Nonterminal && !p.IsUnit)
                    {
                        GrammarSymbol[] rhs = new GrammarSymbol[p.Rhs.Length];
                        rhs[0] = startSymbol[(Nonterminal)p.First];
                        Array.Copy(p.Rhs, 1, rhs, 1, rhs.Length - 1);
                        Production q = new Production(p.Lhs, rhs);
                        A_prods.Add(q);
                    }
                    else
                        A_prods.Add(p);
                }
            }
            #endregion
            ContextFreeGrammar H = new ContextFreeGrammar(new List<Nonterminal>(Hprods.Keys), g.startSymbol, Hprods, g.terminals);

            //Console.WriteLine("--------- H:");
            //H.Display(Console.Out);

            //eliminate useless symbols from H
            //this may dramatically decrease the number of productions
            ContextFreeGrammar H1 = H.RemoveUselessSymbols();

            //Console.WriteLine("---------- H1:");
            //H1.Display(Console.Out);


            List<Nonterminal> egnfVars = new List<Nonterminal>();
            Dictionary<Nonterminal, List<Production>> egnfProds = new Dictionary<Nonterminal, List<Production>>();
            Stack<Nonterminal> egnfStack = new Stack<Nonterminal>();
            HashSet<Nonterminal> egnfVisited = new HashSet<Nonterminal>();
            egnfStack.Push(H1.startSymbol);
            egnfVisited.Add(H1.startSymbol);
            egnfVars.Add(H1.startSymbol);
            egnfProds[H1.startSymbol] = new List<Production>();

            #region eliminate temp start symbols and produce the EGNF form
            while (egnfStack.Count > 0)
            {
                var A = egnfStack.Pop();
                List<Production> A_prods = egnfProds[A];
                foreach (Production p in H1.productionMap[A])
                {
                    if (!(p.First is Nonterminal) || p.IsUnit)
                    {
                        A_prods.Add(p);
                        foreach (Nonterminal x in p.GetNonterminals())
                            if (egnfVisited.Add(x))
                            {
                                egnfStack.Push(x);
                                egnfVars.Add(x);
                                egnfProds[x] = new List<Production>();
                            }
                    }
                    else
                    {
                        Nonterminal S_B = (Nonterminal)p.First; //here we know that S_B is a temp start symbol 
                        foreach (Production t in H1.productionMap[S_B])
                        {
                            int k = t.Rhs.Length;
                            GrammarSymbol[] rhs = new GrammarSymbol[k + p.Rhs.Length - 1];
                            for (int i = 0; i < k; i++)
                                rhs[i] = t.Rhs[i];
                            for (int i = 1; i < p.Rhs.Length; i++)
                                rhs[k + i - 1] = p.Rhs[i];
                            Production q = new Production(A, rhs);
                            A_prods.Add(q);
                            foreach (Nonterminal x in q.GetNonterminals())
                                if (egnfVisited.Add(x))
                                {
                                    egnfStack.Push(x);
                                    egnfVars.Add(x);
                                    egnfProds[x] = new List<Production>();
                                }
                        }
                    }
                }
            }
            #endregion

            ContextFreeGrammar egnf = new ContextFreeGrammar(egnfVars, H1.startSymbol, egnfProds, g.terminals);
            return egnf;
        }

        static Nonterminal Lookup(Dictionary<Nonterminal, Nonterminal> vars, Nonterminal key, ref int nonterminalID)
        {
            Nonterminal v;
            if (vars.TryGetValue(key, out v))
                return v;
            v = Nonterminal.MkNonterminalForId(nonterminalID++);
            vars[key] = v;
            return v;
        }

        bool StartSymbolAppearsInRhs()
        {
            foreach (Production p in GetProductions())
                if (p.RhsContainsSymbol(startSymbol))
                    return true;
            return false;
        }

        private static bool FinalStateHasVariableMoves(Automaton<GrammarSymbol> MB)
        {
            foreach (Move<GrammarSymbol> move in MB.GetMovesFrom(MB.FinalState))
                if (move.Label is Nonterminal)
                    return true;
            return false;
        }

        IEnumerable<Nonterminal> AllVariablesExceptTheStartSymbol()
        {
            foreach (Nonterminal v in variables)
                if (!v.Equals(startSymbol))
                    yield return v;
        }

        #region Pretty Printing
        /// <summary>
        /// Writes the productions 'E -> alpha_1 | alpha_2 | ... | alpha_n' to tw 
        /// with the set of productions for each nonterminal E (variable) per line.
        /// </summary>
        public void Display(TextWriter tw)
        {
            foreach (Nonterminal v in variables)
                if (productionMap[v].Count > 0)
                    tw.WriteLine(DescribeProductions(v));
        }

        /// <summary>
        /// Returns the productions 'E -> rhs_1 | rhs_2 | ... | rhs_n' 
        /// for each nonterminal E as a single string separated by newlines.
        /// </summary>
        public string Description
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(DescribeProductions(startSymbol));
                foreach (Nonterminal v in variables)
                    if (!startSymbol.Equals(v))
                        sb.Append(DescribeProductions(v));
                return sb.ToString();
            }
        }

        /// <summary>
        /// Returns the value of Description
        /// </summary>
        public override string ToString()
        {
            return Description;
        }

        private string DescribeProductions(Nonterminal v)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(v.Name);
            List<Production> prods = productionMap[v];
            if (prods.Count > 0)
            {
                sb.Append(" -> ");
                sb.Append(prods[0].DescriptionOfRhs);
                for (int i = 1; i < prods.Count; i++ )
                {
                    sb.Append("| ");
                    sb.Append(prods[i].DescriptionOfRhs);
                }
            }
            sb.AppendLine();
            return sb.ToString();
        }
        #endregion

        /// <summary>
        /// Returns true if the language of the grammar intersects with the 
        /// language of the automaton, if true then a witness is produced
        /// in terms of a sequence of predicates over input characters.
        /// Assumes that the grammar is in GNF and terminals have type T.
        /// </summary>
        public bool Overlaps<T>(Automaton<T> automaton, out T[] witness)
        {
            var pda = this.ToPDA<T>();
            var product = pda.Intersect(automaton);
            return product.IsNonempty_(out witness);
        }

        /// <summary>
        /// Convert the CFG to an equivalent PDA.
        /// The stack symbols are the grammar symbols of the grammar 
        /// and two extra symbols are added for start and end.
        /// It is assumed that the terminal labels of the grammar have type T.
        /// </summary>
        public PushdownAutomaton<GrammarSymbol, T> ToPDA<T>()
        {
            var moves = new List<Move<PushdownLabel<GrammarSymbol, T>>>();
            int q0 = 0;
            int q = 1;
            int qF = 2;
            var stackSymbols = new List<GrammarSymbol>();
            stackSymbols.Add(Nonterminal.StartStackSymbol);
            stackSymbols.Add(Nonterminal.EndStackSymbol);
            stackSymbols.AddRange(this.variables);

            //terminals that occur in Rest in some production are also going to be used as stack symbols
            HashSet<GrammarSymbol> newStackSymbols = new HashSet<GrammarSymbol>();

            //the first transition pushes the actual start symbol and the final symbol to kickstart the PDA
            var startLabel = new PushdownLabel<GrammarSymbol, T>(Nonterminal.StartStackSymbol, this.StartSymbol, Nonterminal.EndStackSymbol);
            moves.Add(Move<PushdownLabel<GrammarSymbol, T>>.Create(q0, q, startLabel));

            //the only transition to final state is from endStackSymbol
            var finalLabel = new PushdownLabel<GrammarSymbol, T>(Nonterminal.EndStackSymbol);
            moves.Add(Move<PushdownLabel<GrammarSymbol, T>>.Create(q, qF, finalLabel));

            foreach (var entry in this.productionMap)
                foreach (var prod in entry.Value)
                {
                    var pop = prod.Lhs;
                    if (prod.IsEpsilon)
                    {
                        moves.Add(Move<PushdownLabel<GrammarSymbol, T>>.Create(q, q, new PushdownLabel<GrammarSymbol, T>(pop)));
                    }
                    else
                    {
                        var rest = prod.Rest;
                        if (prod.FirstIsTerminal)
                        {
                            Terminal<T> terminal = prod.First as Terminal<T>;
                            if (terminal == null)
                                throw new AutomataException(AutomataExceptionKind.IncompatibleGrammarTerminalType);
                            moves.Add(Move<PushdownLabel<GrammarSymbol, T>>.Create(q, q, new PushdownLabel<GrammarSymbol, T>(terminal.term, pop, rest)));
                        }
                        else
                        {
                            //this is an input-epsilon transition in the PDA
                            moves.Add(Move<PushdownLabel<GrammarSymbol, T>>.Create(q, q, new PushdownLabel<GrammarSymbol, T>(pop, prod.Rhs)));
                        }
                        //also add new moves for terminals in rest
                        foreach (var s in rest)
                        {
                            if (!(s is Nonterminal))
                            {
                                Terminal<T> terminal = s as Terminal<T>;
                                if (terminal == null)
                                    throw new AutomataException(AutomataExceptionKind.IncompatibleGrammarTerminalType);

                                var newMove = Move<PushdownLabel<GrammarSymbol, T>>.Create(q, q, new PushdownLabel<GrammarSymbol, T>(terminal.term, s));
                                moves.Add(newMove);
                                //add s as a new stack symbol also
                                if (newStackSymbols.Add(s))
                                    stackSymbols.Add(s);
                            }
                        }
                    }
                }

            var pda = new PushdownAutomaton<GrammarSymbol, T>((IBooleanAlgebra<T>)this.BuiltinTerminalAlgebra, q0, 
                new List<int>(new int[] { q0, q, qF }), new List<int>(new int[] { qF }),
                Nonterminal.StartStackSymbol, stackSymbols, moves);
            return pda;
        }


        /// <summary>
        /// Parse a CFG from the input string. Use the custom regex parser to parse terminals as regexes into automata.
        /// </summary>
        public static ContextFreeGrammar Parse<T>(Func<string, Automaton<T>> regexParser, string input)
        {
            return GrammarParser<T>.Parse(regexParser, input);
        }


        private static CharSetSolver context = null;
        public static CharSetSolver GetContext()
        {
            if (context == null)
                context = new CharSetSolver();
            return context;
        }
        public static void ResetContext()
        {
            context = null;
        }

        /// <summary>
        /// Parse a CFG from the input string. Use the builting regex parser to parse terminals as regexes into automata.
        /// All terminal regexes are parsed in RegexOptions.Singleline mode so '.' means any character.
        /// Stores an instance of CharSetSolver in context.
        /// </summary>
        /// <param name="removeEpsilonsFromAutomata">if true then remove epsilons from the automata contructed from terminals</param>
        /// <param name="determinizeAutomata">if true then determinize the automata contructed from terminals (also removes epsilons)</param>
        /// <param name="minimizeAutomata">if true then minimize the automata contructed from terminals (also removes epsilons, if determinize is false then uses bisimulation based minimization)</param>
        public static ContextFreeGrammar Parse(string input, bool removeEpsilonsFromAutomata = false, bool determinizeAutomata = false, bool minimizeAutomata = false)
        {
            Func<string, Automaton<BDD>> regexParser = null;
            var context = GetContext();
            //add anchors around x when using GetContext().Convert, 
            //because no start/end anchors mean .* at start/end
            if (!removeEpsilonsFromAutomata && !determinizeAutomata && !minimizeAutomata)
            {
                regexParser = x => context.Convert("^(" + x + ")$", System.Text.RegularExpressions.RegexOptions.Singleline).RemoveEpsilonLoops();
            }
            else if (determinizeAutomata && minimizeAutomata)
            {
                regexParser = x => context.Convert("^(" + x + ")$", System.Text.RegularExpressions.RegexOptions.Singleline).RemoveEpsilons().Determinize().Minimize();
            }
            else if (minimizeAutomata)
            {
                regexParser = x => context.Convert("^(" + x + ")$", System.Text.RegularExpressions.RegexOptions.Singleline).RemoveEpsilons().Minimize();
            }
            else
            {
                regexParser = x => context.Convert("^(" + x + ")$", System.Text.RegularExpressions.RegexOptions.Singleline).RemoveEpsilons();
            }

            return GrammarParser<BDD>.Parse(regexParser, input);
        }

        /// <summary>
        /// Checks if the context free language of this grammar intersects with the regular language defined by the regex.
        /// If the languages intersect then outputs a witness and returns true, otherwise returns false and sets witness to null.
        /// Regex is parsed in RegexOptions.Singleline mode so '.' means any character.
        /// Anchors are not implicit, eg, regex 'ab+c' is the same as '.*ab+c.*.'
        /// Use explicit anchors, as in ^ab+c$, if anchors are intended.
        /// </summary>
        /// <param name="regex">given regex</param>
        /// <param name="determinizeAutomaton">if true then determinize the automaton constructed from the regex</param>
        /// <param name="minimizeAutomaton">if true then minimize the automaton constructed from the regex, if determinise is false then uses bisimulation to minimize</param>
        public bool IntersectsWith(string regex, out string witness, bool determinizeAutomaton = false, bool minimizeAutomaton = false)
        {
            Automaton<BDD> aut = null;
            var context = GetContext();
            aut = context.Convert(regex, System.Text.RegularExpressions.RegexOptions.Singleline).RemoveEpsilons();
            if (determinizeAutomaton && minimizeAutomaton)
            {
                aut = aut.Determinize().Minimize();
            }
            else if (minimizeAutomaton)
            {
                aut = aut.Minimize();
            }
            else if (determinizeAutomaton)
            {
                aut = aut.Determinize();
            }
            BDD[] witness_bdd;
            var pda = this.ToPDA<BDD>();
            var pda2 = pda.Intersect(aut);
            if (pda2.IsNonempty(out witness_bdd))
            {
                witness = this.BuiltinTerminalAlgebra.ChooseString(witness_bdd);
                return true;
            }
            else
            {
                witness = null;
                return false;
            }
        }

        /// <summary>
        /// Checks if the context free language of this grammar intersects with all the regular languages defined by the regexes.
        /// If the languages intersect then outputs a witness and returns true, otherwise returns false and sets witness to null.
        /// Regexes are parsed in RegexOptions.Singleline mode so '.' means any character.
        /// Anchors are not implicit, eg, regex 'ab+c' is the same as '.*ab+c.*.'
        /// Use explicit anchors, as in ^ab+c$, if anchors are intended.
        /// </summary>
        /// <param name="regexes">given regexes to intersect with</param>
        /// <param name="determinizeAutomaton">if true then determinize the automaton constructed from the regex</param>
        /// <param name="minimizeAutomaton">if true then minimize the automaton constructed from the regex, if determinise is false then uses bisimulation to minimize</param>
        public bool IntersectsWith(string[] regexes, out string witness, bool determinizeAutomaton = false, bool minimizeAutomaton = false)
        {
            var context = GetContext();
            var aut = Array.ConvertAll(regexes, regex => context.Convert(regex, System.Text.RegularExpressions.RegexOptions.Singleline).RemoveEpsilons());
            if (determinizeAutomaton && minimizeAutomaton)
            {
                aut = Array.ConvertAll(aut, x => x.Determinize().Minimize());
            }
            else if (minimizeAutomaton)
            {
                aut = Array.ConvertAll(aut, x => x.Minimize());
            }
            else if (determinizeAutomaton)
            {
                aut = Array.ConvertAll(aut, x => x.Determinize());
            }
            BDD[] witness_bdd;
            var pda = this.ToPDA<BDD>();
            var pda2 = pda.Intersect(aut);
            if (pda2.IsNonempty(out witness_bdd))
            {
                witness = this.BuiltinTerminalAlgebra.ChooseString(witness_bdd);
                return true;
            }
            else
            {
                witness = null;
                return false;
            }
        }
    }


    #region Used in backward reachability algorithms
    internal class VariableNode
    {
        internal List<ProductionNode> parents = new List<ProductionNode>();
        internal bool isMarked = false;
        //internal Variable variable;

        internal VariableNode() { }
        //internal VariableNode(Variable variable)
        //{
        //    this.variable = variable;
        //}

        internal void PropagateMark()
        {
            if (!isMarked)
            {
                isMarked = true;
                foreach (ProductionNode parent in parents)
                    parent.children.Remove(this);

                foreach (ProductionNode parent in parents)
                    parent.PropagateMark();
            }
        }
    }

    internal class ProductionNode
    {
        internal VariableNode parent;
        internal HashSet<VariableNode> children;

        internal ProductionNode(VariableNode parent, params VariableNode[] children)
        {
            this.parent = parent;
            this.children = new HashSet<VariableNode>(children);
        }

        //internal ProductionNode(VariableNode parent, IEnumerable<VariableNode> children)
        //{
        //    this.parent = parent;
        //    this.children = new HashSet<VariableNode>(children);
        //}

        internal void PropagateMark()
        {
            if (children.Count == 0)
                parent.PropagateMark();
        }
    }
    #endregion

}
