using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Automata;

namespace Microsoft.Automata.MSO
{
    /// <summary>
    /// depricated
    /// </summary>
    public abstract class WS1SFormula<T>
    {
        public Automaton<IMonadicPredicate<BDD, T>> GetAutomaton(ICartesianAlgebraBDD<T> ca, params Variable[] fv)
        {
            var fvs = SimpleList<Variable>.Empty.Append(fv);
            return getAutomaton(fvs, ca);
        }

        internal abstract Automaton<IMonadicPredicate<BDD, T>> getAutomaton(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> ca);

        public Automaton<BDD> GetAutomatonBDD(IBDDAlgebra alg, int nrOfLabelBits, params Variable[] fv)
        {
            if (!(typeof(T).Equals(typeof(BDD))))
                throw new ArgumentException("Method is not supported because T is not BDD");

            return getAutomatonBDD(SimpleList<Variable>.Empty.Append(fv), alg, nrOfLabelBits);
        }

        internal abstract Automaton<BDD> getAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits);

        internal abstract IEnumerable<Variable> EnumerateFreeVariablesPossiblyWithDuplicates();

        public SimpleList<Variable> GetFreeVariables(bool firstOrderOnly = false)
        {
            HashSet<Variable> set = new HashSet<Variable>();
            var fvs = SimpleList<Variable>.Empty;
            foreach (var v in EnumerateFreeVariablesPossiblyWithDuplicates())
                if (!firstOrderOnly || v.IsFirstOrder)
                    if (set.Add(v))
                        fvs = fvs.Append(v);
            return fvs;
        }

        public bool IsClosed
        {
            get
            {
                var e = EnumerateFreeVariablesPossiblyWithDuplicates().GetEnumerator();
                var nonempty = e.MoveNext();
                return !nonempty;
            }
        }

        /// <summary>
        /// Create the conjunction of two predicates
        /// </summary>
        /// <param name="pred1">first predicate</param>
        /// <param name="pred2">second predicate</param>
        public static WS1SFormula<T> operator &(WS1SFormula<T> pred1, WS1SFormula<T> pred2)
        {
            return new WS1SAnd<T>(pred1, pred2);
        }


        /// <summary>
        /// Create the disjunction of two predicates
        /// </summary>
        /// <param name="pred1">first predicate</param>
        /// <param name="pred2">second predicate</param>
        public static WS1SFormula<T> operator |(WS1SFormula<T> pred1, WS1SFormula<T> pred2)
        {
            return new WS1SOr<T>(pred1, pred2);
        }

        /// <summary>
        /// Create the negation of the predicate
        /// </summary>
        /// <param name="pred">the predicate to be negated</param>
        public static WS1SFormula<T> operator ~(WS1SFormula<T> pred)
        {
            return new WS1SNot<T>(pred);
        }
    }

    public class WS1SAnd<T> : WS1SFormula<T>
    {
        public readonly WS1SFormula<T> phi1;
        public readonly WS1SFormula<T> phi2;

        public WS1SAnd(WS1SFormula<T> phi1, WS1SFormula<T> phi2)
        {
            this.phi1 = phi1;
            this.phi2 = phi2;
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> getAutomaton(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> ca)
        {
            var aut1 = phi1.getAutomaton(variables, ca);
            var aut2 = phi2.getAutomaton(variables, ca);
            var aut3 = aut1.Intersect(aut2);
            var aut = aut3.Minimize();
            //var aut_old = aut.Determinize(ca).Minimize(ca);
            return aut;
        }

        internal override Automaton<BDD> getAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            var aut1 = phi1.getAutomatonBDD(variables, alg, nrOfLabelBits);
            var aut2 = phi2.getAutomatonBDD(variables, alg, nrOfLabelBits);
            var aut = aut1.Intersect(aut2);
            aut = aut.Minimize();
            return aut;
        }

        internal override IEnumerable<Variable> EnumerateFreeVariablesPossiblyWithDuplicates()
        {
            foreach (var v in phi1.EnumerateFreeVariablesPossiblyWithDuplicates())
                yield return v;
            foreach (var v in phi2.EnumerateFreeVariablesPossiblyWithDuplicates())
                yield return v;
        }

        public override string ToString()
        {
            return "(" + phi1.ToString() + "&" + phi2.ToString() + ")";
        }
    }

    public class WS1SOr<T> : WS1SFormula<T>
    {
        public readonly WS1SFormula<T> phi1;
        public readonly WS1SFormula<T> phi2;

        public WS1SOr(WS1SFormula<T> phi1, WS1SFormula<T> phi2)
        {
            this.phi1 = phi1;
            this.phi2 = phi2;
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> getAutomaton(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> alg)
        {
            var aut1 = phi1.getAutomaton(variables, alg);
            var aut2 = phi2.getAutomaton(variables, alg);
            //var res = aut1.Complement(alg).Intersect(aut2.Complement(alg), alg).Complement(alg);
            //res = res.Determinize(alg).Minimize(alg);
            var res = aut1.Union(aut2).RemoveEpsilons();
            var res1 = res.Minimize();
            return res1;
        }

        internal override Automaton<BDD> getAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            var aut1 = phi1.getAutomatonBDD(variables, alg, nrOfLabelBits);
            var aut2 = phi2.getAutomatonBDD(variables, alg, nrOfLabelBits);
            var res = aut1.Complement().Intersect(aut2.Complement()).Complement();
            res = res.Determinize().Minimize();
            return res;
        }

        internal override IEnumerable<Variable> EnumerateFreeVariablesPossiblyWithDuplicates()
        {
            foreach (var v in phi1.EnumerateFreeVariablesPossiblyWithDuplicates())
                yield return v;
            foreach (var v in phi2.EnumerateFreeVariablesPossiblyWithDuplicates())
                yield return v;
        }

        public override string ToString()
        {
            return "(" + phi1.ToString() + "|" + phi2.ToString() + ")";
        }
    }

    public class WS1SNot<T> : WS1SFormula<T>
    {
        public readonly WS1SFormula<T> phi;

        public WS1SNot(WS1SFormula<T> phi)
        {
            this.phi = phi;
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> getAutomaton(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> alg)
        {
            var aut = phi.getAutomaton(variables, alg);
            var res = aut.Determinize().Complement().Minimize();
            foreach (var x in phi.GetFreeVariables(true))
            {
                var sing = new WS1SSingleton<T>(x).getAutomaton(variables, alg);
                res = res.Intersect(sing).Determinize().Minimize();
            }
            return res;
        }

        internal override Automaton<BDD> getAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            var aut = phi.getAutomatonBDD(variables, alg, nrOfLabelBits);
            var res = aut.Determinize().Complement().Minimize();
            foreach (var x in phi.EnumerateFreeVariablesPossiblyWithDuplicates())
            {
                if (x.IsFirstOrder)
                {
                    var sing = new WS1SSingleton<T>(x).getAutomatonBDD(variables, alg, nrOfLabelBits);
                    res = res.Intersect(sing).Determinize().Minimize();
                }
            }
            return res;
        }

        internal override IEnumerable<Variable> EnumerateFreeVariablesPossiblyWithDuplicates()
        {
            return phi.EnumerateFreeVariablesPossiblyWithDuplicates();
        }

        public override string ToString()
        {
            return "~" + phi.ToString();
        }
    }

    public class WS1STrue<T> : WS1SFormula<T>
    {
        public WS1STrue() { }

        internal override Automaton<IMonadicPredicate<BDD, T>> getAutomaton(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> ca)
        {
            return BasicAutomata.MkTrue(ca);
        }

        internal override Automaton<BDD> getAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            return BasicAutomata.MkTrue(alg);
        }

        internal override IEnumerable<Variable> EnumerateFreeVariablesPossiblyWithDuplicates()
        {
            yield break;
        }

        public override string ToString()
        {
            return "true";
        }
    }

    public class WS1SSubset<T> : WS1SFormula<T>
    {
        public readonly Variable var1;
        public readonly Variable var2;

        public WS1SSubset(Variable var1, Variable var2)
        {
            this.var1 = var1;
            this.var2 = var2;
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> getAutomaton(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> ca)
        {
            var pos1 = variables.IndexOf(var1);
            var pos2 = variables.IndexOf(var2);

            if (pos1 < 0 || pos2 < 0)
                throw new AutomataException(AutomataExceptionKind.InvalidWS1Sformula_UnknownVariable);

            return BasicAutomata.MkSubset(pos1, pos2, ca);
        }

        internal override Automaton<BDD> getAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            var pos1 = variables.IndexOf(var1);
            var pos2 = variables.IndexOf(var2);

            if (pos1 < 0 || pos2 < 0)
                throw new AutomataException(AutomataExceptionKind.InvalidWS1Sformula_UnknownVariable);

            var aut = BasicAutomata.MkSubset(pos1 + nrOfLabelBits, pos2 + nrOfLabelBits, alg);
            return aut;
        }

        internal override IEnumerable<Variable> EnumerateFreeVariablesPossiblyWithDuplicates()
        {
            yield return var1;
            yield return var2;
        }

        public override string ToString()
        {
            return "Subset(" + var1 + "," + var2 + ")";
        }
    }

    public class WS1SSingleton<T> : WS1SFormula<T>
    {
        public readonly Variable var;

        public WS1SSingleton(Variable var)
        {
            this.var = var;
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> getAutomaton(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> ca)
        {
            var pos = variables.IndexOf(var);

            if (pos < 0)
                throw new AutomataException(AutomataExceptionKind.InvalidWS1Sformula_UnknownVariable);

            return BasicAutomata.MkSingleton(pos, ca);
        }

        internal override Automaton<BDD> getAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            var pos = variables.IndexOf(var);

            if (pos < 0)
                throw new AutomataException(AutomataExceptionKind.InvalidWS1Sformula_UnknownVariable);

            return BasicAutomata.MkSingleton(pos + nrOfLabelBits, alg);
        }

        internal override IEnumerable<Variable> EnumerateFreeVariablesPossiblyWithDuplicates()
        {
            yield return var;
        }

        public override string ToString()
        {
            return "!" + var;
        }
    }

    public class WS1SPred<T> : WS1SFormula<T>
    {
        public readonly T pred;
        public readonly Variable var;

        public WS1SPred(T pred, Variable var)
        {
            this.pred = pred;
            this.var = var;
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> getAutomaton(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> ca)
        {
            var k = variables.IndexOf(var);

            if (k < 0)
                throw new AutomataException(AutomataExceptionKind.InvalidWS1Sformula_UnknownVariable);

            if (var.IsFirstOrder)
            {
                return BasicAutomata.MkLabelOfPosition(k, pred, ca);
            }
            else
            {
                return BasicAutomata.MkLabelOfSet(k, pred, ca);
            }
        }

        internal override Automaton<BDD> getAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            var k = variables.IndexOf(var);

            if (k < 0)
                throw new AutomataException(AutomataExceptionKind.InvalidWS1Sformula_UnknownVariable);

            k = k + nrOfLabelBits;

            if (var.IsFirstOrder)
            {
                return BasicAutomata.MkLabelOfPosition1(k, pred as BDD, alg);
            }
            else
            {
                return BasicAutomata.MkLabelOfSet(k, pred as BDD, alg);
            }
        }

        internal override IEnumerable<Variable> EnumerateFreeVariablesPossiblyWithDuplicates()
        {
            yield return var;
        }

        public override string ToString()
        {
            return "[" + pred.ToString() + "](" + var + ")";
        }
    }

    public class WS1SSuccN<T> : WS1SFormula<T>
    {
        public readonly Variable var1;
        public readonly Variable var2;
        public readonly int n;

        public WS1SSuccN(Variable var1, Variable var2, int n)
        {
            if (n < 1)
                throw new ArgumentException("successor offset must be positive", "n");

            if (var1.IsFirstOrder != var2.IsFirstOrder)
                throw new AutomataException(AutomataExceptionKind.InvalidWS1Sformula);

            this.var1 = var1;
            this.var2 = var2;
            this.n = n;
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> getAutomaton(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> ca)
        {
            var pos1 = variables.IndexOf(var1);
            var pos2 = variables.IndexOf(var2);

            if (pos1 < 0 || pos2 < 0)
                throw new AutomataException(AutomataExceptionKind.InvalidWS1Sformula_UnknownVariable);

            return BasicAutomata.MkSuccN(pos1, pos2, n, ca);
        }

        internal override Automaton<BDD> getAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            var pos1 = variables.IndexOf(var1);
            var pos2 = variables.IndexOf(var2);

            if (pos1 < 0 || pos2 < 0)
                throw new AutomataException(AutomataExceptionKind.InvalidWS1Sformula_UnknownVariable);

            pos1 = pos1 + nrOfLabelBits;
            pos2 = pos2 + nrOfLabelBits;

            return BasicAutomata.MkSuccN1(pos1, pos2, n, alg);
        }

        internal override IEnumerable<Variable> EnumerateFreeVariablesPossiblyWithDuplicates()
        {
            yield return var1;
            yield return var2;
        }

        public override string ToString()
        {
            return "(" + var2 + "=" + var1 + "+" + n.ToString() + ")";
        }
    }

    public class WS1SLt<T> : WS1SFormula<T>
    {
        public readonly Variable var1;
        public readonly Variable var2;

        public WS1SLt(Variable var1, Variable var2)
        {
            if (var1.IsFirstOrder != var2.IsFirstOrder)
                throw new AutomataException(AutomataExceptionKind.InvalidWS1Sformula);
            this.var1 = var1;
            this.var2 = var2;
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> getAutomaton(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> ca)
        {
            var pos1 = variables.IndexOf(var1);
            var pos2 = variables.IndexOf(var2);

            if (pos1 < 0 || pos2 < 0)
                throw new AutomataException(AutomataExceptionKind.InvalidWS1Sformula_UnknownVariable);

            return BasicAutomata.MkLess(pos1, pos2, ca);
        }

        internal override Automaton<BDD> getAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            var pos1 = variables.IndexOf(var1);
            var pos2 = variables.IndexOf(var2);

            if (pos1 < 0 || pos2 < 0)
                throw new AutomataException(AutomataExceptionKind.InvalidWS1Sformula_UnknownVariable);

            pos1 = pos1 + nrOfLabelBits;
            pos2 = pos2 + nrOfLabelBits;

            return BasicAutomata.MkLt1(pos1, pos2, alg);
        }

        internal override IEnumerable<Variable> EnumerateFreeVariablesPossiblyWithDuplicates()
        {
            yield return var1;
            yield return var2;
        }

        public override string ToString()
        {
            return "(" + var1 + "<" + var2 + ")";
        }
    }

    public class WS1SEq<T> : WS1SFormula<T>
    {
        public readonly Variable var1;
        public readonly Variable var2;

        public WS1SEq(Variable var1, Variable var2)
        {
            if (var1.IsFirstOrder != var2.IsFirstOrder)
                throw new AutomataException(AutomataExceptionKind.InvalidWS1Sformula);
            this.var1 = var1;
            this.var2 = var2;
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> getAutomaton(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> ca)
        {
            var pos1 = variables.IndexOf(var1);
            var pos2 = variables.IndexOf(var2);

            if (pos1 < 0 || pos2 < 0)
                throw new AutomataException(AutomataExceptionKind.InvalidWS1Sformula_UnknownVariable);

            if (var1.IsFirstOrder)
            {
                return BasicAutomata.MkEqualPositions(pos1, pos2, ca);
            }
            else
            {
                return BasicAutomata.MkEqualSets(pos1, pos2, ca);
            }
        }

        internal override Automaton<BDD> getAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            var pos1 = variables.IndexOf(var1);
            var pos2 = variables.IndexOf(var2);

            if (pos1 < 0 || pos2 < 0)
                throw new AutomataException(AutomataExceptionKind.InvalidWS1Sformula_UnknownVariable);

            pos1 = pos1 + nrOfLabelBits;
            pos2 = pos2 + nrOfLabelBits;

            if (var1.IsFirstOrder)
            {
                return BasicAutomata.MkEqualPositions1(pos1, pos2, alg);
            }
            else
            {
                return BasicAutomata.MkEqualSets(pos1, pos2, alg);
            }
        }

        internal override IEnumerable<Variable> EnumerateFreeVariablesPossiblyWithDuplicates()
        {
            yield return var1;
            yield return var2;
        }

        public override string ToString()
        {
            return "(" + var1 + "=" + var2 + ")";
        }
    }

    public class WS1SExists<T> : WS1SFormula<T>
    {
        public readonly Variable var;
        public readonly WS1SFormula<T> phi;

        public WS1SExists(Variable var, WS1SFormula<T> phi)
        {
            this.var = var;
            this.phi = phi;
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> getAutomaton(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> alg)
        {
            //the existential variable will shadow any previous occurrence with the same name
            //because when looking up the index of var it will be the last occurrence
            var varIndex = variables.Count;
            var variablesExt = variables.Append(var);
            var autPhi = phi.getAutomaton(variablesExt, alg);

            //Project away the the existential variable
            var newMoves = new List<Move<IMonadicPredicate<BDD, T>>>();
            foreach (var move in autPhi.GetMoves())
            {
                var newPred = alg.Omit(varIndex, move.Label);
                newMoves.Add(new Move<IMonadicPredicate<BDD, T>>(move.SourceState, move.TargetState, newPred));
            }

            var res = Automaton<IMonadicPredicate<BDD, T>>.Create(alg, autPhi.InitialState, autPhi.GetFinalStates(), newMoves);
            //var res = res.Determinize(alg);
            var res_min = res.Minimize();

            return res_min;
        }

        internal override Automaton<BDD> getAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            //the existential variable will shadow any previous occurrence with the same name
            //because when looking up the index of var it will be the last occurrence
            var varIndex = variables.Count + nrOfLabelBits;
            var variablesExt = variables.Append(var);
            var autPhi = phi.getAutomatonBDD(variablesExt, alg, nrOfLabelBits);

            //Project away the the existential variable
            var newMoves = new List<Move<BDD>>();
            foreach (var move in autPhi.GetMoves())
                newMoves.Add(new Move<BDD>(move.SourceState, move.TargetState, alg.OmitBit(move.Label, varIndex)));

            var aut = Automaton<BDD>.Create(alg, autPhi.InitialState, autPhi.GetFinalStates(), newMoves);
            var res = aut.Determinize();
            res = res.Minimize();

            return res;
        }

        internal override IEnumerable<Variable> EnumerateFreeVariablesPossiblyWithDuplicates()
        {
            foreach (var v in phi.EnumerateFreeVariablesPossiblyWithDuplicates())
                if (!var.Equals(v))
                    yield return v;
        }

        public override string ToString()
        {
            return "Exists(" + var + "," + phi.ToString() + ")";
        }
    }
}