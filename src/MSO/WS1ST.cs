using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Automata;

namespace Microsoft.Automata.MSO
{
    /// <summary>
    /// WS1S variable
    /// </summary>
    public class WS1SVariable<T>
    {
        public readonly string name;
        public readonly bool isFirstOrder;
        public WS1SVariable(string name, bool isFirstOrder = false)
        {
            this.name = name;
            this.isFirstOrder = isFirstOrder;
        }

        /// <summary>
        /// Create the formula v1 is less than v2.
        /// Either both v1 and v2 are first-order or both are second-order.
        /// In the case that v1 and v2 are second-order the meaning is that both are singletons.
        /// </summary>
        /// <param name="v1">first varible</param>
        /// <param name="v2">second variable</param>
        public static WS1SFormula<T> operator <(WS1SVariable<T> v1, WS1SVariable<T> v2)
        {
            return new WS1SLt<T>(v1, v2);
        }

        /// <summary>
        /// Create the formula v1 is greater than v2
        /// Either both v1 and v2 are first-order or both are second-order.
        /// In the case that v1 and v2 are second-order the meaning is that both are singletons.
        /// </summary>
        /// <param name="v1">first varible</param>
        /// <param name="v2">second variable</param>
        public static WS1SFormula<T> operator >(WS1SVariable<T> v1, WS1SVariable<T> v2)
        {
            return new WS1SLt<T>(v2, v1);
        }

        /// <summary>
        /// Create the formula that the second-order variable X is a singleton.
        /// </summary>
        /// <param name="X">second-order variable</param>
        public static WS1SFormula<T> operator !(WS1SVariable<T> X)
        {
            return new WS1SSingleton<T>(X);
        }

        /// <summary>
        /// x ^ psi constructs the formula Exists(x,psi)
        /// </summary>
        /// <param name="x">variable</param>
        /// <param name="psi">formula</param>
        public static WS1SFormula<T> operator ^(WS1SVariable<T> x, WS1SFormula<T> psi)
        {
            return new WS1SExists<T>(x, psi);
        }


        public static WS1SFormula<T> operator ==(WS1SVariable<T> x, WS1SVariable<T> y)
        {
            return new WS1SEq<T>(x, y);
        }

        /// <summary>
        /// ~(x == y)
        /// </summary>
        public static WS1SFormula<T> operator !=(WS1SVariable<T> x, WS1SVariable<T> y)
        {
            return new WS1SNot<T>(new WS1SEq<T>(x, y));
        }

        /// <summary>
        /// X subset Y
        /// </summary>
        public static WS1SFormula<T> operator <=(WS1SVariable<T> X, WS1SVariable<T> Y)
        {
            return new WS1SSubset<T>(X, Y);
        }

        /// <summary>
        /// X superset Y
        /// </summary>
        public static WS1SFormula<T> operator >=(WS1SVariable<T> X, WS1SVariable<T> Y)
        {
            return new WS1SSubset<T>(Y, X);
        }

        /// <summary>
        /// [pred](X)
        /// </summary>
        public static WS1SFormula<T> operator %(T pred, WS1SVariable<T> X)
        {
            return new WS1SPred<T>(pred, X);
        }

        public override bool Equals(object obj)
        {
            WS1SVariable<T> v = obj as WS1SVariable<T>;
            if (object.Equals(v,null))
                return false;
            return (name.Equals(v.name));
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }

        public override string ToString()
        {
            return name;
        }
    }

    /// <summary>
    /// Base class for symbolic WS1S formulas.
    /// </summary>
    public abstract class WS1SFormula<T>
    {
        public Automaton<IMonadicPredicate<BDD, T>> GetAutomaton(ICartesianAlgebraBDD<T> ca, params WS1SVariable<T>[] fv)
        {
            var fvs = SimpleList<WS1SVariable<T>>.Empty.Append(fv);
            return getAutomaton(fvs, ca);
        }

        internal abstract Automaton<IMonadicPredicate<BDD, T>> getAutomaton(SimpleList<WS1SVariable<T>> variables, ICartesianAlgebraBDD<T> ca);

        public Automaton<BDD> GetAutomatonBDD(IBDDAlgebra alg, int nrOfLabelBits, params WS1SVariable<T>[] fv)
        {
            if (!(typeof(T).Equals(typeof(BDD))))
                throw new ArgumentException("Method is not supported because T is not BDD");

            return getAutomatonBDD(SimpleList<WS1SVariable<T>>.Empty.Append(fv), alg, nrOfLabelBits); 
        }

        internal abstract Automaton<BDD> getAutomatonBDD(SimpleList<WS1SVariable<T>> variables, IBDDAlgebra alg, int nrOfLabelBits);

        internal abstract IEnumerable<WS1SVariable<T>> EnumerateFreeVariablesPossiblyWithDuplicates();

        public SimpleList<WS1SVariable<T>> GetFreeVariables(bool firstOrderOnly = false)
        {
            HashSet<WS1SVariable<T>> set = new HashSet<WS1SVariable<T>>();
            var fvs = SimpleList<WS1SVariable<T>>.Empty;
            foreach (var v in EnumerateFreeVariablesPossiblyWithDuplicates())
                if (!firstOrderOnly || v.isFirstOrder)
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

    #region Boolean operations 

    public class WS1SAnd<T> : WS1SFormula<T>
    {
        public readonly WS1SFormula<T> phi1;
        public readonly WS1SFormula<T> phi2;

        public WS1SAnd(WS1SFormula<T> phi1, WS1SFormula<T> phi2)
        {
            this.phi1 = phi1;
            this.phi2 = phi2;
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> getAutomaton(SimpleList<WS1SVariable<T>> variables, ICartesianAlgebraBDD<T> ca)
        {
            var aut1 = phi1.getAutomaton(variables, ca);
            var aut2 = phi2.getAutomaton(variables, ca);
            var aut3 = aut1.Intersect(aut2, ca);
            var aut = aut3.Minimize(ca);
            //var aut_old = aut.Determinize(ca).Minimize(ca);
            return aut;
        }

        internal override Automaton<BDD> getAutomatonBDD(SimpleList<WS1SVariable<T>> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            var aut1 = phi1.getAutomatonBDD(variables, alg, nrOfLabelBits);
            var aut2 = phi2.getAutomatonBDD(variables, alg, nrOfLabelBits);
            var aut = aut1.Intersect(aut2, alg);
            aut = aut.Minimize(alg);
            return aut;
        }

        internal override IEnumerable<WS1SVariable<T>> EnumerateFreeVariablesPossiblyWithDuplicates()
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

        internal override Automaton<IMonadicPredicate<BDD, T>> getAutomaton(SimpleList<WS1SVariable<T>> variables, ICartesianAlgebraBDD<T> alg)
        {
            var aut1 = phi1.getAutomaton(variables, alg);
            var aut2 = phi2.getAutomaton(variables, alg);
            //var res = aut1.Complement(alg).Intersect(aut2.Complement(alg), alg).Complement(alg);
            //res = res.Determinize(alg).Minimize(alg);
            var res = aut1.Union(aut2).RemoveEpsilons(alg.MkOr);
            var res1 = res.Minimize(alg);
            return res1;
        }

        internal override Automaton<BDD> getAutomatonBDD(SimpleList<WS1SVariable<T>> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            var aut1 = phi1.getAutomatonBDD(variables, alg, nrOfLabelBits);
            var aut2 = phi2.getAutomatonBDD(variables, alg, nrOfLabelBits);
            var res = aut1.Complement(alg).Intersect(aut2.Complement(alg), alg).Complement(alg);
            res = res.Determinize(alg).Minimize(alg);
            return res;
        }

        internal override IEnumerable<WS1SVariable<T>> EnumerateFreeVariablesPossiblyWithDuplicates()
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

        internal override Automaton<IMonadicPredicate<BDD, T>> getAutomaton(SimpleList<WS1SVariable<T>> variables, ICartesianAlgebraBDD<T> alg)
        {
            var aut = phi.getAutomaton(variables, alg);
            var res = aut.Determinize(alg).Complement(alg).Minimize(alg);
            foreach (var x in phi.GetFreeVariables(true))
            {
                var sing = new WS1SSingleton<T>(x).getAutomaton(variables, alg);
                res = res.Intersect(sing, alg).Determinize(alg).Minimize(alg);
            }
            return res;
        }

        internal override Automaton<BDD> getAutomatonBDD(SimpleList<WS1SVariable<T>> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            var aut = phi.getAutomatonBDD(variables, alg, nrOfLabelBits);
            var res = aut.Determinize(alg).Complement(alg).Minimize(alg);
            foreach (var x in phi.EnumerateFreeVariablesPossiblyWithDuplicates())
            {
                if (x.isFirstOrder)
                {
                    var sing = new WS1SSingleton<T>(x).getAutomatonBDD(variables, alg, nrOfLabelBits);
                    res = res.Intersect(sing, alg).Determinize(alg).Minimize(alg);
                }
            }
            return res;
        }

        internal override IEnumerable<WS1SVariable<T>> EnumerateFreeVariablesPossiblyWithDuplicates()
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

        internal override Automaton<IMonadicPredicate<BDD, T>> getAutomaton(SimpleList<WS1SVariable<T>> variables, ICartesianAlgebraBDD<T> ca)
        {
            return BasicAutomata.MkTrue(ca);
        }

        internal override Automaton<BDD> getAutomatonBDD(SimpleList<WS1SVariable<T>> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            return BasicAutomata.MkTrue(alg);
        }

        internal override IEnumerable<WS1SVariable<T>> EnumerateFreeVariablesPossiblyWithDuplicates()
        {
            yield break;
        }

        public override string ToString()
        {
            return "true";
        }
    }

    #endregion

    public class WS1SSubset<T> : WS1SFormula<T>
    {
        public readonly WS1SVariable<T> var1;
        public readonly WS1SVariable<T> var2;

        public WS1SSubset(WS1SVariable<T> var1, WS1SVariable<T> var2)
        {
            this.var1 = var1;
            this.var2 = var2;
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> getAutomaton(SimpleList<WS1SVariable<T>> variables, ICartesianAlgebraBDD<T> ca)
        {
            var pos1 = variables.IndexOf(var1);
            var pos2 = variables.IndexOf(var2);

            if (pos1 < 0 || pos2 < 0)
                throw new AutomataException(AutomataExceptionKind.InvalidWS1Sformula_UnknownVariable);

            return BasicAutomata.MkSubset(pos1, pos2, ca);
        }

        internal override Automaton<BDD> getAutomatonBDD(SimpleList<WS1SVariable<T>> variables, IBDDAlgebra alg, int nrOfLabelBits) 
        {
            var pos1 = variables.IndexOf(var1);
            var pos2 = variables.IndexOf(var2);

            if (pos1 < 0 || pos2 < 0)
                throw new AutomataException(AutomataExceptionKind.InvalidWS1Sformula_UnknownVariable);

            var aut = BasicAutomata.MkSubset(pos1 + nrOfLabelBits, pos2 + nrOfLabelBits, alg);
            return aut;
        }

        internal override IEnumerable<WS1SVariable<T>> EnumerateFreeVariablesPossiblyWithDuplicates()
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
        public readonly WS1SVariable<T> var;

        public WS1SSingleton(WS1SVariable<T> var)
        {
            this.var = var;
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> getAutomaton(SimpleList<WS1SVariable<T>> variables, ICartesianAlgebraBDD<T> ca)
        {
            var pos = variables.IndexOf(var);

            if (pos < 0)
                throw new AutomataException(AutomataExceptionKind.InvalidWS1Sformula_UnknownVariable);

            return BasicAutomata.MkSingleton(pos, ca);
        }

        internal override Automaton<BDD> getAutomatonBDD(SimpleList<WS1SVariable<T>> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            var pos = variables.IndexOf(var);

            if (pos < 0)
                throw new AutomataException(AutomataExceptionKind.InvalidWS1Sformula_UnknownVariable);

            return BasicAutomata.MkSingleton(pos + nrOfLabelBits, alg);
        }

        internal override IEnumerable<WS1SVariable<T>> EnumerateFreeVariablesPossiblyWithDuplicates()
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
        public readonly WS1SVariable<T> var;

        public WS1SPred(T pred, WS1SVariable<T> var)
        {
            this.pred = pred;
            this.var = var;
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> getAutomaton(SimpleList<WS1SVariable<T>> variables, ICartesianAlgebraBDD<T> ca)
        {
            var k = variables.IndexOf(var);

            if (k < 0)
                throw new AutomataException(AutomataExceptionKind.InvalidWS1Sformula_UnknownVariable);

            if (var.isFirstOrder)
            {
                return BasicAutomata.MkLabelOfPosition(k, pred, ca);
            }
            else
            {
                return BasicAutomata.MkLabelOfSet(k, pred, ca);
            }
        }

        internal override Automaton<BDD> getAutomatonBDD(SimpleList<WS1SVariable<T>> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            var k = variables.IndexOf(var);

            if (k < 0)
                throw new AutomataException(AutomataExceptionKind.InvalidWS1Sformula_UnknownVariable);

            k = k + nrOfLabelBits;

            if (var.isFirstOrder)
            {
                return BasicAutomata.MkLabelOfPosition(k, pred as BDD, alg);
            }
            else
            {
                return BasicAutomata.MkLabelOfSet(k, pred as BDD, alg);
            }
        }

        internal override IEnumerable<WS1SVariable<T>> EnumerateFreeVariablesPossiblyWithDuplicates()
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
        public readonly WS1SVariable<T> var1;
        public readonly WS1SVariable<T> var2;
        public readonly int n;

        public WS1SSuccN(WS1SVariable<T> var1, WS1SVariable<T> var2, int n)
        {
            if (n < 1)
                throw new ArgumentException("successor offset must be positive", "n");

            if (var1.isFirstOrder != var2.isFirstOrder)
                throw new AutomataException(AutomataExceptionKind.InvalidWS1Sformula);

            this.var1 = var1;
            this.var2 = var2;
            this.n = n;
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> getAutomaton(SimpleList<WS1SVariable<T>> variables, ICartesianAlgebraBDD<T> ca)
        {
            var pos1 = variables.IndexOf(var1);
            var pos2 = variables.IndexOf(var2);

            if (pos1 < 0 || pos2 < 0)
                throw new AutomataException(AutomataExceptionKind.InvalidWS1Sformula_UnknownVariable);

            return BasicAutomata.MkSuccN(pos1, pos2, n, ca);
        }

        internal override Automaton<BDD> getAutomatonBDD(SimpleList<WS1SVariable<T>> variables, IBDDAlgebra alg, int nrOfLabelBits) 
        {
            var pos1 = variables.IndexOf(var1);
            var pos2 = variables.IndexOf(var2);

            if (pos1 < 0 || pos2 < 0)
                throw new AutomataException(AutomataExceptionKind.InvalidWS1Sformula_UnknownVariable);

            pos1 = pos1 + nrOfLabelBits;
            pos2 = pos2 + nrOfLabelBits;

            return BasicAutomata.MkSuccN(pos1, pos2, n, alg);
        }

        internal override IEnumerable<WS1SVariable<T>> EnumerateFreeVariablesPossiblyWithDuplicates()
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
        public readonly WS1SVariable<T> var1;
        public readonly WS1SVariable<T> var2;

        public WS1SLt(WS1SVariable<T> var1, WS1SVariable<T> var2)
        {
            if (var1.isFirstOrder != var2.isFirstOrder)
                throw new AutomataException(AutomataExceptionKind.InvalidWS1Sformula);
            this.var1 = var1;
            this.var2 = var2;
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> getAutomaton(SimpleList<WS1SVariable<T>> variables, ICartesianAlgebraBDD<T> ca)
        {
            var pos1 = variables.IndexOf(var1);
            var pos2 = variables.IndexOf(var2);

            if (pos1 < 0 || pos2 < 0)
                throw new AutomataException(AutomataExceptionKind.InvalidWS1Sformula_UnknownVariable);

            return BasicAutomata.MkLess(pos1, pos2, ca);
        }

        internal override Automaton<BDD> getAutomatonBDD(SimpleList<WS1SVariable<T>> variables, IBDDAlgebra alg, int nrOfLabelBits) 
        {
            var pos1 = variables.IndexOf(var1);
            var pos2 = variables.IndexOf(var2);

            if (pos1 < 0 || pos2 < 0)
                throw new AutomataException(AutomataExceptionKind.InvalidWS1Sformula_UnknownVariable);

            pos1 = pos1 + nrOfLabelBits;
            pos2 = pos2 + nrOfLabelBits;

            return BasicAutomata.MkLess(pos1, pos2, alg);
        }

        internal override IEnumerable<WS1SVariable<T>> EnumerateFreeVariablesPossiblyWithDuplicates()
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
        public readonly WS1SVariable<T> var1;
        public readonly WS1SVariable<T> var2;

        public WS1SEq(WS1SVariable<T> var1, WS1SVariable<T> var2)
        {
            if (var1.isFirstOrder != var2.isFirstOrder)
                throw new AutomataException(AutomataExceptionKind.InvalidWS1Sformula);
            this.var1 = var1;
            this.var2 = var2;
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> getAutomaton(SimpleList<WS1SVariable<T>> variables, ICartesianAlgebraBDD<T> ca)
        {
            var pos1 = variables.IndexOf(var1);
            var pos2 = variables.IndexOf(var2);

            if (pos1 < 0 || pos2 < 0)
                throw new AutomataException(AutomataExceptionKind.InvalidWS1Sformula_UnknownVariable);

            if (var1.isFirstOrder)
            {
                return BasicAutomata.MkEqualPositions(pos1, pos2, ca);
            }
            else
            {
                return BasicAutomata.MkEqualSets(pos1, pos2, ca);
            }
        }

        internal override Automaton<BDD> getAutomatonBDD(SimpleList<WS1SVariable<T>> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            var pos1 = variables.IndexOf(var1);
            var pos2 = variables.IndexOf(var2);

            if (pos1 < 0 || pos2 < 0)
                throw new AutomataException(AutomataExceptionKind.InvalidWS1Sformula_UnknownVariable);

            pos1 = pos1 + nrOfLabelBits;
            pos2 = pos2 + nrOfLabelBits;

            if (var1.isFirstOrder)
            {
                return BasicAutomata.MkEqualPositions(pos1, pos2, alg);
            }
            else
            {
                return BasicAutomata.MkEqualSets(pos1, pos2, alg);
            }
        }

        internal override IEnumerable<WS1SVariable<T>> EnumerateFreeVariablesPossiblyWithDuplicates()
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
        public readonly WS1SVariable<T> var;
        public readonly WS1SFormula<T> phi;

        public WS1SExists(WS1SVariable<T> var, WS1SFormula<T> phi)
        {
            this.var = var;
            this.phi = phi;
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> getAutomaton(SimpleList<WS1SVariable<T>> variables, ICartesianAlgebraBDD<T> alg)
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

            var res = Automaton<IMonadicPredicate<BDD, T>>.Create(autPhi.InitialState, autPhi.GetFinalStates(), newMoves);
            //var res = res.Determinize(alg);
            var res_min = res.Minimize(alg);

            return res_min;
        }

        internal override Automaton<BDD> getAutomatonBDD(SimpleList<WS1SVariable<T>> variables, IBDDAlgebra alg, int nrOfLabelBits)
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

            var aut = Automaton<BDD>.Create(autPhi.InitialState, autPhi.GetFinalStates(), newMoves);
            var res = aut.Determinize(alg);
            res = res.Minimize(alg);

            return res;
        }

        internal override IEnumerable<WS1SVariable<T>> EnumerateFreeVariablesPossiblyWithDuplicates()
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

    public static class BasicAutomata
    {
        public static Automaton<BDD> MkSubset(int i, int j, IBDDAlgebra alg)
        {
            var bit_j_is1 = alg.MkBitTrue(j);
            var bit_i_is0 = alg.MkBitFalse(i);
            var subsetCond = alg.MkOr(bit_j_is1, bit_i_is0);
            var moves = new Move<BDD>[] { new Move<BDD>(0, 0, subsetCond) };
            var aut = Automaton<BDD>.Create(0, new int[] { 0 }, moves, false, false, true);
            return aut;
        }

        public static Automaton<IMonadicPredicate<BDD,T>> MkSubset<T>(int i, int j, ICartesianAlgebraBDD<T> alg)
        {
            var bit_j_is1 = alg.BDDAlgebra.MkBitTrue(j);
            var bit_i_is0 = alg.BDDAlgebra.MkBitFalse(i);
            var subsetCond = alg.BDDAlgebra.MkOr(bit_j_is1, bit_i_is0);
            var moves = new Move<IMonadicPredicate<BDD, T>>[] { new Move<IMonadicPredicate<BDD, T>>(0, 0, alg.MkCartesianProduct(subsetCond, alg.Second.True)) };
            var aut = Automaton<IMonadicPredicate<BDD, T>>.Create(0, new int[] { 0 }, moves, false, false, true);
            return aut;
        }

        public static Automaton<BDD> MkEqualSets(int i, int j, IBDDAlgebra alg)
        {
            var bit_j_is1 = alg.MkBitTrue(j);
            var bit_i_is1 = alg.MkBitTrue(i);
            var bit_i_is0 = alg.MkBitFalse(i);
            var bit_j_is0 = alg.MkBitFalse(j);
            var cond = alg.MkOr(alg.MkAnd(bit_i_is1,bit_j_is1),alg.MkAnd(bit_i_is0,bit_j_is0));
            var moves = new Move<BDD>[] { new Move<BDD>(0, 0, cond) };
            var aut = Automaton<BDD>.Create(0, new int[] { 0 }, moves, false, false, true);
            return aut;
        }

        public static Automaton<IMonadicPredicate<BDD, T>> MkEqualSets<T>(int i, int j, ICartesianAlgebraBDD<T> alg)
        {
            var bit_j_is1 = alg.BDDAlgebra.MkBitTrue(j);
            var bit_i_is1 = alg.BDDAlgebra.MkBitTrue(i);
            var bit_i_is0 = alg.BDDAlgebra.MkBitFalse(i);
            var bit_j_is0 = alg.BDDAlgebra.MkBitFalse(j);
            var cond = alg.MkCartesianProduct(alg.BDDAlgebra.MkOr(alg.BDDAlgebra.MkAnd(bit_i_is1, bit_j_is1), alg.BDDAlgebra.MkAnd(bit_i_is0, bit_j_is0)), alg.Second.True);
            var moves = new Move<IMonadicPredicate<BDD, T>>[] { new Move<IMonadicPredicate<BDD, T>>(0, 0, cond) };
            var aut = Automaton<IMonadicPredicate<BDD, T>>.Create(0, new int[] { 0 }, moves, false, false, true);
            return aut;
        }

        public static Automaton<BDD> MkEqualPositions(int i, int j, IBDDAlgebra alg)
        {
            var bit_j_is1 = alg.MkBitTrue(j);
            var bit_i_is1 = alg.MkBitTrue(i);
            var bit_i_is0 = alg.MkBitFalse(i);
            var bit_j_is0 = alg.MkBitFalse(j);
            var both0 = alg.MkAnd(bit_i_is0, bit_j_is0);
            var both1 = alg.MkAnd(bit_i_is1, bit_j_is1);
            var moves = new Move<BDD>[] { new Move<BDD>(0, 0, both0), 
                new Move<BDD>(0, 1, both1), new Move<BDD>(1, 1, both0) };
            var aut = Automaton<BDD>.Create(0, new int[] { 1 }, moves, false, false, true);
            return aut;
        }

        public static Automaton<IMonadicPredicate<BDD, T>> MkEqualPositions<T>(int i, int j, ICartesianAlgebraBDD<T> alg)
        {
            var bit_j_is1 = alg.BDDAlgebra.MkBitTrue(j);
            var bit_i_is1 = alg.BDDAlgebra.MkBitTrue(i);
            var bit_i_is0 = alg.BDDAlgebra.MkBitFalse(i);
            var bit_j_is0 = alg.BDDAlgebra.MkBitFalse(j);
            var both0 = alg.BDDAlgebra.MkAnd(bit_i_is0, bit_j_is0);
            var both1 = alg.BDDAlgebra.MkAnd(bit_i_is1, bit_j_is1);
            Func<BDD, IMonadicPredicate<BDD, T>> lift = 
                bdd => alg.MkCartesianProduct(bdd, alg.Second.True);
            var moves = new Move<IMonadicPredicate<BDD, T>>[] { 
                new Move<IMonadicPredicate<BDD, T>>(0, 0, lift(both0)), 
                new Move<IMonadicPredicate<BDD, T>>(0, 1, lift(both1)), 
                new Move<IMonadicPredicate<BDD, T>>(1, 1, lift(both0)) };
            var aut = Automaton<IMonadicPredicate<BDD, T>>.Create(0, new int[] { 1 }, moves, false, false, true);
            return aut;
        }

        public static Automaton<BDD> MkMember(int i, int j, IBDDAlgebra alg)
        {
            var bit_j_is1 = alg.MkBitTrue(j);
            var bit_i_is1 = alg.MkBitTrue(i);
            var bit_i_is0 = alg.MkBitFalse(i);
            var both1 = alg.MkAnd(bit_i_is1, bit_j_is1);
            var moves = new Move<BDD>[] { new Move<BDD>(0, 0, bit_i_is0), 
                new Move<BDD>(0, 1, both1), new Move<BDD>(1, 1, bit_i_is0) };
            var aut = Automaton<BDD>.Create(0, new int[] { 1 }, moves, false, false, true);
            return aut;
        }

        public static Automaton<IMonadicPredicate<BDD, T>> MkMember<T>(int i, int j, ICartesianAlgebraBDD<T> ca)
        {
            Func<BDD, IMonadicPredicate<BDD, T>> lift = bdd => ca.MkCartesianProduct(bdd, ca.Second.True);
            var alg = ca.BDDAlgebra;
            var bit_j_is1 = alg.MkBitTrue(j);
            var bit_i_is1 = alg.MkBitTrue(i);
            var bit_i_is0 = alg.MkBitFalse(i);
            var both1 = alg.MkAnd(bit_i_is1, bit_j_is1);
            var moves = new Move<IMonadicPredicate<BDD, T>>[] { 
                new Move<IMonadicPredicate<BDD, T>>(0, 0, lift(bit_i_is0)), 
                new Move<IMonadicPredicate<BDD, T>>(0, 1, lift(both1)), 
                new Move<IMonadicPredicate<BDD, T>>(1, 1, lift(bit_i_is0)) };
            var aut = Automaton<IMonadicPredicate<BDD, T>>.Create(0, new int[] { 1 }, moves, false, false, true);
            return aut;
        }

        public static Automaton<BDD> MkLabelOfSet(int i, BDD lab, IBDDAlgebra alg)
        {
            var bit_i_is0 = alg.MkBitFalse(i);
            var cond = alg.MkOr(lab, bit_i_is0);
            var moves = new Move<BDD>[] { new Move<BDD>(0, 0, cond) };
            var aut = Automaton<BDD>.Create(0, new int[] { 0 }, moves, false, false, true);
            return aut;
        }
        
        public static Automaton<IMonadicPredicate<BDD, T>> MkLabelOfSet<T>(int i, T lab, ICartesianAlgebraBDD<T> ca)
        {
            Func<BDD, IMonadicPredicate<BDD, T>> lift1 = bdd => ca.MkCartesianProduct(bdd, ca.Second.True);
            Func<T, IMonadicPredicate<BDD, T>> lift2 = psi => ca.MkCartesianProduct(ca.BDDAlgebra.True, psi);
            var bit_i_is0 = ca.BDDAlgebra.MkBitFalse(i);
            var cond = ca.MkOr(lift2(lab),lift1(bit_i_is0));
            var moves = new Move<IMonadicPredicate<BDD, T>>[] { new Move<IMonadicPredicate<BDD, T>>(0, 0, cond) };
            var aut = Automaton<IMonadicPredicate<BDD, T>>.Create(0, new int[] { 0 }, moves, false, false, true);
            return aut;
        }

        public static Automaton<BDD> MkLabelOfPosition(int i, BDD lab, IBDDAlgebra alg)
        {
            var bit_i_is0 = alg.MkBitFalse(i);
            var bit_i_is1 = alg.MkBitTrue(i);
            var cond = alg.MkAnd(bit_i_is1, lab);
            var moves = new Move<BDD>[] { 
                new Move<BDD>(0, 0, bit_i_is0),
                new Move<BDD>(0, 1, cond), 
                new Move<BDD>(1, 1, bit_i_is0)};
            var aut = Automaton<BDD>.Create(0, new int[] { 1 }, moves, false, false, true);
            return aut;
        }
        
        public static Automaton<IMonadicPredicate<BDD, T>> MkLabelOfPosition<T>(int i, T lab, ICartesianAlgebraBDD<T> ca)
        {
            Func<BDD, IMonadicPredicate<BDD, T>> lift1 = bdd => ca.MkCartesianProduct(bdd, ca.Second.True);
            Func<T, IMonadicPredicate<BDD, T>> lift2 = psi => ca.MkCartesianProduct(ca.BDDAlgebra.True, psi);
            var alg = ca.BDDAlgebra;
            var bit_i_is0 = alg.MkBitFalse(i);
            var bit_i_is1 = alg.MkBitTrue(i);
            var cond = ca.MkCartesianProduct(bit_i_is1, lab);
            var moves = new Move<IMonadicPredicate<BDD, T>>[] { 
                new Move<IMonadicPredicate<BDD, T>>(0, 0, lift1(bit_i_is0)),
                new Move<IMonadicPredicate<BDD, T>>(0, 1, cond), 
                new Move<IMonadicPredicate<BDD, T>>(1, 1, lift1(bit_i_is0))};
            var aut = Automaton<IMonadicPredicate<BDD, T>>.Create(0, new int[] { 1 }, moves, false, false, true);
            return aut;
        }

        public static Automaton<BDD> MkLess(int i, int j, IBDDAlgebra alg)
        {
            var bit_i_is0 = alg.MkBitFalse(i);
            var bit_i_is1 = alg.MkBitTrue(i);
            var bit_j_is0 = alg.MkBitFalse(j);
            var bit_j_is1 = alg.MkBitTrue(j);
            var both0 = alg.MkAnd(bit_i_is0, bit_j_is0);
            var cond1 = alg.MkAnd(bit_i_is1, bit_j_is0);
            var cond2 = alg.MkAnd(bit_j_is1, bit_i_is0);
            var moves = new Move<BDD>[] { new Move<BDD>(0, 0, both0),
                new Move<BDD>(0, 1, cond1), new Move<BDD>(1, 1, both0),
                new Move<BDD>(1, 2, cond2), new Move<BDD>(2, 2, both0)};
            var aut = Automaton<BDD>.Create(0, new int[] { 2 }, moves, false, false, true);
            return aut;
        }

        public static Automaton<IMonadicPredicate<BDD, T>> MkLess<T>(int i, int j, ICartesianAlgebraBDD<T> ca)
        {
            Func<BDD, IMonadicPredicate<BDD, T>> lift1 = bdd => ca.MkCartesianProduct(bdd, ca.Second.True);
            Func<T, IMonadicPredicate<BDD, T>> lift2 = psi => ca.MkCartesianProduct(ca.BDDAlgebra.True, psi);
            var alg = ca.BDDAlgebra;
            var bit_i_is0 = alg.MkBitFalse(i);
            var bit_i_is1 = alg.MkBitTrue(i);
            var bit_j_is0 = alg.MkBitFalse(j);
            var bit_j_is1 = alg.MkBitTrue(j);
            var both0 = alg.MkAnd(bit_i_is0, bit_j_is0);
            var cond1 = alg.MkAnd(bit_i_is1, bit_j_is0);
            var cond2 = alg.MkAnd(bit_j_is1, bit_i_is0);
            var moves = new Move<IMonadicPredicate<BDD, T>>[] { 
                new Move<IMonadicPredicate<BDD, T>>(0, 0, lift1(both0)),
                new Move<IMonadicPredicate<BDD, T>>(0, 1, lift1(cond1)), 
                new Move<IMonadicPredicate<BDD, T>>(1, 1, lift1(both0)),
                new Move<IMonadicPredicate<BDD, T>>(1, 2, lift1(cond2)), 
                new Move<IMonadicPredicate<BDD, T>>(2, 2, lift1(both0))};
            var aut = Automaton<IMonadicPredicate<BDD, T>>.Create(0, new int[] { 2 }, moves, false, false, true);
            return aut;
        }

        public static Automaton<BDD> MkSingleton(int i, IBDDAlgebra alg)
        {
            var bit_i_is0 = alg.MkBitFalse(i);
            var bit_i_is1 = alg.MkBitTrue(i);
            var moves = new Move<BDD>[] { new Move<BDD>(0, 0, bit_i_is0),
                new Move<BDD>(0, 1, bit_i_is1), new Move<BDD>(1, 1, bit_i_is0)};
            var aut = Automaton<BDD>.Create(0, new int[] { 1 }, moves, false, false, true);
            return aut;
        }

        public static Automaton<IMonadicPredicate<BDD, T>> MkSingleton<T>(int i, ICartesianAlgebraBDD<T> ca) 
        {
            Func<BDD, IMonadicPredicate<BDD, T>> lift1 = bdd => ca.MkCartesianProduct(bdd, ca.Second.True);
            Func<T, IMonadicPredicate<BDD, T>> lift2 = psi => ca.MkCartesianProduct(ca.BDDAlgebra.True, psi);
            var alg = ca.BDDAlgebra;
            var bit_i_is0 = alg.MkBitFalse(i);
            var bit_i_is1 = alg.MkBitTrue(i);
            var moves = new Move<IMonadicPredicate<BDD, T>>[] { 
                new Move<IMonadicPredicate<BDD, T>>(0, 0, lift1(bit_i_is0)),
                new Move<IMonadicPredicate<BDD, T>>(0, 1, lift1(bit_i_is1)), 
                new Move<IMonadicPredicate<BDD, T>>(1, 1, lift1(bit_i_is0))};
            var aut = Automaton<IMonadicPredicate<BDD, T>>.Create(0, new int[] { 1 }, moves, false, false, true);
            return aut;
        }

        public static Automaton<BDD> MkTrue(IBDDAlgebra alg)
        {
            var moves = new Move<BDD>[] { 
                new Move<BDD>(0, 0, alg.True) 
            };
            return Automaton<BDD>.Create(0, new int[] { 0 }, moves, false, false, true);
        }

        public static Automaton<IMonadicPredicate<BDD, T>> MkTrue<T>(ICartesianAlgebraBDD<T> ca)
        {
            var moves = new Move<IMonadicPredicate<BDD, T>>[] { 
                new Move<IMonadicPredicate<BDD, T>>(0, 0, ca.True) 
            };
            return Automaton<IMonadicPredicate<BDD, T>>.Create(0, new int[] { 0 }, moves, false, false, true);
        }

        public static Automaton<BDD> MkSuccN(int i1, int i2, int n, IBDDAlgebra bddAlg)
        {
            var alg = bddAlg;
            var ind10 = bddAlg.MkBitFalse(i1);
            var ind11 = bddAlg.MkBitTrue(i1);
            var ind20 = bddAlg.MkBitFalse(i2);
            var ind21 = bddAlg.MkBitTrue(i2);
            var both0 = bddAlg.MkAnd(ind10, ind20);
            var ind11ind20 = bddAlg.MkAnd(ind11, ind20);
            var ind10ind21 = bddAlg.MkAnd(ind10, ind21);

            //Create moves
            var moves = new List<Move<BDD>>();
            moves.Add(new Move<BDD>(0, 0, both0));
            moves.Add(new Move<BDD>(0, 1, ind11ind20));
            moves.Add(new Move<BDD>(n, n + 1, ind10ind21));
            moves.Add(new Move<BDD>(n + 1, n + 1, both0));

            for (int i = 1; i < n; i++)
                moves.Add(new Move<BDD>(i, i + 1, both0));

            var aut = Automaton<BDD>.Create(0, new int[] { n + 1 }, moves, false, false, true);
            return aut;
        }

        public static Automaton<IMonadicPredicate<BDD, T>> MkSuccN<T>(int i1, int i2, int n, ICartesianAlgebraBDD<T> ca)
        {

            var bddAlg = ca.BDDAlgebra;
            var ind10 = bddAlg.MkBitFalse(i1);
            var ind11 = bddAlg.MkBitTrue(i1);
            var ind20 = bddAlg.MkBitFalse(i2);
            var ind21 = bddAlg.MkBitTrue(i2);
            var both0 = bddAlg.MkAnd(ind10, ind20);
            var ind11ind20 = bddAlg.MkAnd(ind11, ind20);
            var ind10ind21 = bddAlg.MkAnd(ind10, ind21);

            var both0t = ca.MkCartesianProduct(both0, ca.Second.True);
            var ind11ind20t = ca.MkCartesianProduct(ind11ind20, ca.Second.True);
            var ind10ind21t = ca.MkCartesianProduct(ind10ind21, ca.Second.True);

            //Create moves
            var moves = new List<Move<IMonadicPredicate<BDD, T>>>();
            moves.Add(new Move<IMonadicPredicate<BDD, T>>(0, 0, both0t));
            moves.Add(new Move<IMonadicPredicate<BDD, T>>(0, 1, ind11ind20t));
            moves.Add(new Move<IMonadicPredicate<BDD, T>>(n, n + 1, ind10ind21t));
            moves.Add(new Move<IMonadicPredicate<BDD, T>>(n + 1, n + 1, both0t));

            for (int i = 1; i < n; i++)
                moves.Add(new Move<IMonadicPredicate<BDD, T>>(i, i + 1, both0t));

            var aut = Automaton<IMonadicPredicate<BDD, T>>.Create(0, new int[] { n + 1 }, moves, false, false, true);
            return aut;
        }

        public static Automaton<T> Restrict<T>(Automaton<IMonadicPredicate<BDD, T>> autom)
        {
            List<Move<T>> moves = new List<Move<T>>();
            foreach (var move in autom.GetMoves())
                moves.Add(new Move<T>(move.SourceState, move.TargetState, move.Label.ProjectSecond()));
            var res = Automaton<T>.Create(autom.InitialState, autom.GetFinalStates(), moves);
            return res;
        }
    }
}