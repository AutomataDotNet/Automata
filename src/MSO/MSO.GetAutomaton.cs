using System;
using System.Collections.Generic;

using Microsoft.Automata.BooleanAlgebras;

namespace Microsoft.Automata.MSO
{
    public abstract partial class MSOFormula<T>
    {
        /// <summary>
        ///  Constructs the automaton assuming the given list fvs of free variables.
        /// </summary>
        /// <param name="alg">label algebra</param>
        /// <param name="nrOfLabelBits">nr of labels bits, only relevant if alg is not CharSetSolver but is BDDAlgebra</param>
        /// <param name="singletonSetSemantics">if true uses singleton-set-semantics for f-o variables else uses min-nonempty-set-semantics</param>
        /// <param name="fvs">free variables, if null uses this.FreeVariables</param>
        /// <returns></returns>
        public Automaton<T> GetAutomaton(IBooleanAlgebra<T> alg, int nrOfLabelBits = 0, bool singletonSetSemantics = false, Variable[] fvs = null)
        {
            if (fvs == null)
                fvs = this.FreeVariables;
            Automaton<T> res;
            var A = alg as CharSetSolver;
            if (A != null)
            {
                res = this.GetAutomatonBDD1(fvs, A, (int)A.Encoding, singletonSetSemantics) as Automaton<T>;
            }
            else
            {
                var B = alg as BDDAlgebra;
                if (B != null)
                {
                    if (nrOfLabelBits == 0 && this.ExistsSubformula(f => f.Kind == MSOFormulaKind.Predicate))
                        throw new ArgumentException("BDD predicates are not allowed without any reserved label bits");
                    res = this.GetAutomatonBDD1(fvs, B, nrOfLabelBits, singletonSetSemantics) as Automaton<T>;
                }
                else
                {
                    //create temporary cartesian product algebra
                    var C = new CartesianAlgebraBDD<T>(alg);
                    //keep only the original algebra
                    res = Automaton<T>.ProjectSecond<BDD>(this.GetAutomatonX1(fvs, C, singletonSetSemantics)).Determinize().Minimize();
                }
            }
            return res;
        }

        /// <summary>
        /// Constructs the automaton assuming the given list fvs of free variables.
        /// </summary>
        /// <param name="alg">cartesian product algebra of label algebra and BDD algebra</param>
        /// <param name="singletonSetSemantics">if true uses singleton-set-semantics for f-o variables else uses min-nonempty-set-semantics</param>
        /// <param name="fvs">free variables, if null uses this.FreeVariables</param>
        /// <returns></returns>
        public Automaton<IMonadicPredicate<BDD, T>> GetAutomaton(ICartesianAlgebraBDD<T> alg, bool singletonSetSemantics = false, Variable[] fvs = null)
        {
            if (fvs == null)
                fvs = this.FreeVariables;
            return GetAutomatonX1(fvs, alg, singletonSetSemantics);
        }


        Automaton<BDD> GetAutomatonBDD1(Variable[] variables, IBDDAlgebra alg, int nrOfFreeBits, bool singletonSetSemantics)
        {
            var res = GetAutomatonBDD(SimpleList<Variable>.Empty.Append(variables), alg, nrOfFreeBits, singletonSetSemantics);
            for (int i = 0; i < variables.Length; i++)
            {
                if (variables[i].IsFirstOrder)
                    if (singletonSetSemantics)
                        res = res.Intersect(BasicAutomata.MkSingleton(i, alg)).Determinize().Minimize();
                    else
                        res = res.Intersect(BasicAutomata.MkIsNonempty(i, alg)).Determinize().Minimize();
            }
            return res;
        }

        internal abstract Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfFreeBits, bool singletonSetSemantics);

        Automaton<IMonadicPredicate<BDD, T>> GetAutomatonX1(Variable[] variables, ICartesianAlgebraBDD<T> alg, bool singletonSetSemantics)
        {
            var res = GetAutomatonX(SimpleList<Variable>.Empty.Append(variables), alg, singletonSetSemantics);
            for (int i = 0; i < variables.Length; i++)
            {
                if (variables[i].IsFirstOrder)
                    if (singletonSetSemantics)
                        res = res.Intersect(BasicAutomata.MkSingleton<T>(i, alg)).Determinize().Minimize();
                    else
                        res = res.Intersect(BasicAutomata.MkIsNonempty<T>(i, alg)).Determinize().Minimize();
            }
            return res;
        }

        internal abstract Automaton<IMonadicPredicate<BDD, T>> GetAutomatonX(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> alg, bool singletonSetSemantics); 

    }

    public partial class MSOTrue<T>
    {
        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits, bool singletonSetSemantics)
        {
            return BasicAutomata.MkTrue(alg);
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> GetAutomatonX(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> alg, bool singletonSetSemantics)
        {
            return BasicAutomata.MkTrue<T>(alg);
        }
    }

    public partial class MSOFalse<T>
    {
        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits, bool singletonSetSemantics)
        {
            return BasicAutomata.MkFalse(alg);
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> GetAutomatonX(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> alg, bool singletonSetSemantics)
        {
            return BasicAutomata.MkFalse<T>(alg);
        }

    }

    public partial class MSOEq<T>
    {
        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits, bool singletonSetSemantics)
        {
            var pos1 = GetVarIndex(var1, variables) + nrOfLabelBits;
            var pos2 = GetVarIndex(var2, variables) + nrOfLabelBits;

            if (var1.IsFirstOrder)
                if (singletonSetSemantics)
                    return BasicAutomata.MkEqualPositions1(pos1, pos2, alg);
                else
                    return BasicAutomata.MkEqualPositions2(pos1, pos2, alg);
            else
                return BasicAutomata.MkEqualSets(pos1, pos2, alg);
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> GetAutomatonX(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> alg, bool singletonSetSemantics)
        {
            var pos1 = GetVarIndex(var1, variables);
            var pos2 = GetVarIndex(var2, variables);

            if (var1.IsFirstOrder)
                if (singletonSetSemantics)
                    return BasicAutomata.MkEqualPositions1<T>(pos1, pos2, alg);
                else
                    return BasicAutomata.MkEqualPositions2<T>(pos1, pos2, alg);
            else
                return BasicAutomata.MkEqualSets<T>(pos1, pos2, alg);
        }
    }

    public partial class MSOIn<T>
    {
        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits, bool singletonSetSemantics)
        {
            var pos1 = GetVarIndex(var1, variables);
            var pos2 = GetVarIndex(var2, variables);
            if (singletonSetSemantics)
                return BasicAutomata.MkIn1(pos1 + nrOfLabelBits, pos2 + nrOfLabelBits, alg);
            else
                return BasicAutomata.MkIn2(pos1 + nrOfLabelBits, pos2 + nrOfLabelBits, alg);
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> GetAutomatonX(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> alg, bool singletonSetSemantics)
        {
            var pos1 = GetVarIndex(var1, variables);
            var pos2 = GetVarIndex(var2, variables);
            if (singletonSetSemantics)
                return BasicAutomata.MkIn1<T>(pos1, pos2, alg);
            else
                return BasicAutomata.MkIn2<T>(pos1, pos2, alg);
        }
    }

    public partial class MSOMin<T>
    {
        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits, bool singletonSetSemantics)
        {
            var pos1 = GetVarIndex(var1, variables);
            var pos2 = GetVarIndex(var2, variables);
            if (singletonSetSemantics)
                return BasicAutomata.MkMin1(pos1 + nrOfLabelBits, pos2 + nrOfLabelBits, alg);
            else
                return BasicAutomata.MkMin2(pos1 + nrOfLabelBits, pos2 + nrOfLabelBits, alg);
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> GetAutomatonX(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> alg, bool singletonSetSemantics)
        {
            var pos1 = GetVarIndex(var1, variables);
            var pos2 = GetVarIndex(var2, variables);
            if (singletonSetSemantics)
                return BasicAutomata.MkMin1<T>(pos1, pos2, alg);
            else
                return BasicAutomata.MkMin2<T>(pos1, pos2, alg);
        }
    }

    public partial class MSOMax<T>
    {
        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits, bool singletonSetSemantics)
        {
            var pos1 = GetVarIndex(var1, variables);
            var pos2 = GetVarIndex(var2, variables);
            if (singletonSetSemantics)
                return BasicAutomata.MkMax1(pos1 + nrOfLabelBits, pos2 + nrOfLabelBits, alg);
            else
                return BasicAutomata.MkMax2(pos1 + nrOfLabelBits, pos2 + nrOfLabelBits, alg);
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> GetAutomatonX(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> alg, bool singletonSetSemantics)
        {
            var pos1 = GetVarIndex(var1, variables);
            var pos2 = GetVarIndex(var2, variables);
            if (singletonSetSemantics)
                return BasicAutomata.MkMax1<T>(pos1, pos2, alg);
            else
                return BasicAutomata.MkMax2<T>(pos1, pos2, alg);
        }
    }

    public partial class MSOAnd<T>
    {
        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits, bool singletonSetSemantics)
        {
            var aut1 = phi1.GetAutomatonBDD(variables, alg, nrOfLabelBits, singletonSetSemantics);
            var aut2 = phi2.GetAutomatonBDD(variables, alg, nrOfLabelBits, singletonSetSemantics);
            var aut = aut1.Intersect(aut2);
            aut = aut.Minimize();
            return aut;
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> GetAutomatonX(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> alg, bool singletonSetSemantics)
        {
            var aut1 = phi1.GetAutomatonX(variables, alg, singletonSetSemantics);
            var aut2 = phi2.GetAutomatonX(variables, alg, singletonSetSemantics);
            var aut = aut1.Intersect(aut2);
            aut = aut.Minimize();
            return aut;
        }
    }

    public partial class MSOOr<T>
    {
        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits, bool singletonSetSemantics)
        {
            var aut1 = phi1.GetAutomatonBDD(variables, alg, nrOfLabelBits, singletonSetSemantics);
            var aut2 = phi2.GetAutomatonBDD(variables, alg, nrOfLabelBits, singletonSetSemantics);
            var res = aut1.Complement().Intersect(aut2.Complement()).Complement();
            res = res.Determinize().Minimize();
            return res;
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> GetAutomatonX(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> alg, bool singletonSetSemantics)
        {
            var aut1 = phi1.GetAutomatonX(variables, alg, singletonSetSemantics);
            var aut2 = phi2.GetAutomatonX(variables, alg, singletonSetSemantics);
            var res = aut1.Complement().Intersect(aut2.Complement()).Complement();
            res = res.Determinize().Minimize();
            return res;
        }
    }

    public partial class MSONot<T>
    {
        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits, bool singletonSetSemantics)
        {
            var aut = phi.GetAutomatonBDD(variables, alg, nrOfLabelBits, singletonSetSemantics);
            var res = aut.Determinize().Complement().Minimize();
            return res;
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> GetAutomatonX(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> alg, bool singletonSetSemantics)
        {
            var aut = phi.GetAutomatonX(variables, alg, singletonSetSemantics);
            var res = aut.Determinize().Complement().Minimize();
            return res;
        }
    }

    public partial class MSOPredicate<T>
    {
        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits, bool singletonSetSemantics)
        {
            var k = GetVarIndex(var, variables);

            k = k + nrOfLabelBits;

            if (var.IsFirstOrder)
            {
                if (singletonSetSemantics)
                    return BasicAutomata.MkLabelOfPosition1(k, pred as BDD, alg);
                else
                    return BasicAutomata.MkLabelOfPosition2(k, pred as BDD, alg);
            }
            else
                return BasicAutomata.MkLabelOfSet(k, pred as BDD, alg);
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> GetAutomatonX(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> alg, bool singletonSetSemantics)
        {
            var k = GetVarIndex(var, variables);
            if (var.IsFirstOrder)
                if (singletonSetSemantics)
                    return BasicAutomata.MkLabelOfPosition1<T>(k, pred, alg);
                else
                    return BasicAutomata.MkLabelOfPosition2<T>(k, pred, alg);
            else
                return BasicAutomata.MkLabelOfSet<T>(k, pred, alg);
        }
    }

    public partial class MSOLast<T>
    {
        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits, bool singletonSetSemantics)
        {
            return BasicAutomata.MkLast(GetVarIndex(var, variables) + nrOfLabelBits, alg);
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> GetAutomatonX(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> alg, bool singletonSetSemantics)
        {
            return BasicAutomata.MkLast<T>(GetVarIndex(var, variables), alg);
        }
    }

    public partial class MSOSuccN<T>
    {
        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits, bool singletonSetSemantics)
        {
            var pos1 = GetVarIndex(var1, variables);
            var pos2 = GetVarIndex(var2, variables);

            pos1 = pos1 + nrOfLabelBits;
            pos2 = pos2 + nrOfLabelBits;

            if (singletonSetSemantics)
                return BasicAutomata.MkSuccN1(pos1, pos2, N, alg);
            else
                return BasicAutomata.MkSuccN2(pos1, pos2, N, alg);
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> GetAutomatonX(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> alg, bool singletonSetSemantics)
        {
            var pos1 = GetVarIndex(var1, variables);
            var pos2 = GetVarIndex(var2, variables);

            if (singletonSetSemantics)
                return BasicAutomata.MkSuccN1<T>(pos1, pos2, N, alg);
            else 
                return BasicAutomata.MkSuccN2<T>(pos1, pos2, N, alg);
        }
    }

    public partial class MSOeqN<T>
    {
        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits, bool singletonSetSemantics)
        {
            var k = GetVarIndex(var, variables) + nrOfLabelBits;
            if (singletonSetSemantics)
                return BasicAutomata.MkEqN1(k, n, alg);
            else
                return BasicAutomata.MkEqN2(k, n, alg);
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> GetAutomatonX(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> alg, bool singletonSetSemantics)
        {
            var k = GetVarIndex(var, variables);
            if (singletonSetSemantics)
                return BasicAutomata.MkEqN1<T>(k, n, alg);
            else
                return BasicAutomata.MkEqN2<T>(k, n, alg);
        }
    }

    public partial class MSOLt<T>
    {
        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits, bool singletonSetSemantics)
        {
            var pos1 = GetVarIndex(var1, variables) + nrOfLabelBits;
            var pos2 = GetVarIndex(var2, variables) + nrOfLabelBits;
            if (singletonSetSemantics)
                return BasicAutomata.MkLt1(pos1, pos2, alg);
            else
                return BasicAutomata.MkLt2(pos1, pos2, alg);
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> GetAutomatonX(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> alg, bool singletonSetSemantics)
        {
            var pos1 = GetVarIndex(var1, variables);
            var pos2 = GetVarIndex(var2, variables);
            if (singletonSetSemantics)
                return BasicAutomata.MkLt1<T>(pos1, pos2, alg);
            else
                return BasicAutomata.MkLt2<T>(pos1, pos2, alg);
        }
    }

    public partial class MSOLe<T>
    {
        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits, bool singletonSetSemantics)
        {
            if (singletonSetSemantics)
                return BasicAutomata.MkLe1(GetVarIndex(var1, variables) + nrOfLabelBits, GetVarIndex(var2, variables) + nrOfLabelBits, alg);
            else
                return BasicAutomata.MkLe2(GetVarIndex(var1, variables) + nrOfLabelBits, GetVarIndex(var2, variables) + nrOfLabelBits, alg);
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> GetAutomatonX(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> alg, bool singletonSetSemantics)
        {
            if (singletonSetSemantics)
                return BasicAutomata.MkLe1<T>(GetVarIndex(var1, variables), GetVarIndex(var2, variables), alg);
            else
                return BasicAutomata.MkLe2<T>(GetVarIndex(var1, variables), GetVarIndex(var2, variables), alg);
        }
    }

    public partial class MSOSubset<T>
    {
        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits, bool singletonSetSemantics)
        {
            var pos1 = GetVarIndex(var1, variables);
            var pos2 = GetVarIndex(var2, variables);

            var aut = BasicAutomata.MkSubset(pos1 + nrOfLabelBits, pos2 + nrOfLabelBits, alg);
            return aut;
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> GetAutomatonX(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> alg, bool singletonSetSemantics)
        {
            var pos1 = GetVarIndex(var1, variables);
            var pos2 = GetVarIndex(var2, variables);

            var aut = BasicAutomata.MkSubset<T>(pos1, pos2, alg);
            return aut;
        }
    }

    public partial class MSOExists<T>
    {
        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits, bool singletonSetSemantics)
        {
            //the existential variable will shadow any previous occurrence with the same name
            //because when looking up the index of var it will be the last occurrence
            var varIndex = variables.Count + nrOfLabelBits;
            var variablesExt = variables.Append(var);
            var autPhi = phi.GetAutomatonBDD(variablesExt, alg, nrOfLabelBits, singletonSetSemantics);
            Automaton<BDD> autEx;
            if (this.IsFirstOrder)
            {
                if (singletonSetSemantics)
                    autEx = autPhi.Intersect(BasicAutomata.MkSingleton(varIndex, alg)).Minimize();
                else
                    autEx = autPhi.Intersect(BasicAutomata.MkIsNonempty(varIndex, alg)).Minimize();
            }
            else
                autEx = autPhi;

            //Project away the the existential variable
            var newMoves = new List<Move<BDD>>();
            foreach (var move in autEx.GetMoves())
                newMoves.Add(new Move<BDD>(move.SourceState, move.TargetState, alg.OmitBit(move.Label, varIndex)));

            var aut = Automaton<BDD>.Create(alg, autEx.InitialState, autEx.GetFinalStates(), newMoves);
            var res = aut.Determinize();
            res = res.Minimize();

            return res;
        }

        //TBD: enable singleton-set-semantics
        internal override Automaton<IMonadicPredicate<BDD, T>> GetAutomatonX(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> alg, bool singletonSetSemantics)
        {
            //the existential variable will shadow any previous occurrence with the same name
            //because when looking up the index of var it will be the last occurrence
            var varIndex = variables.Count;
            var variablesExt = variables.Append(var);
            var autPhi = phi.GetAutomatonX(variablesExt, alg, singletonSetSemantics);
            Automaton<IMonadicPredicate<BDD, T>> autEx;
            if (this.IsFirstOrder)
            {
                if (singletonSetSemantics)
                    autEx = autPhi.Intersect(BasicAutomata.MkSingleton<T>(varIndex, alg)).Minimize();
                else
                    autEx = autPhi.Intersect(BasicAutomata.MkIsNonempty<T>(varIndex, alg)).Minimize();
            }
            else
                autEx = autPhi;

            //Project away the the existential variable
            var newMoves = new List<Move<IMonadicPredicate<BDD, T>>>();
            foreach (var move in autEx.GetMoves())
                newMoves.Add(new Move<IMonadicPredicate<BDD, T>>(move.SourceState, move.TargetState, alg.Omit(varIndex, move.Label)));

            var aut = Automaton<IMonadicPredicate<BDD, T>>.Create(alg, autEx.InitialState, autEx.GetFinalStates(), newMoves);
            var res = aut.Determinize();
            res = res.Minimize();

            return res;
        }
    }

    public partial class MSOIsEmpty<T>
    {
        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits, bool singletonSetSemantics)
        {
            var pos = GetVarIndex(var, variables) + nrOfLabelBits;
            return BasicAutomata.MkIsEmpty(pos, alg);
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> GetAutomatonX(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> alg, bool singletonSetSemantics)
        {
            var pos = GetVarIndex(var, variables);
            return BasicAutomata.MkIsEmpty<T>(pos, alg);
        }
    }

    public partial class MSOIsSingleton<T>
    {
        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits, bool singletonSetSemantics)
        {
            var pos = GetVarIndex(var, variables) + nrOfLabelBits;
            return BasicAutomata.MkSingleton(pos, alg);
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> GetAutomatonX(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> alg, bool singletonSetSemantics)
        {
            var pos = GetVarIndex(var, variables);
            return BasicAutomata.MkSingleton<T>(pos, alg);
        }
    }

    public partial class MSOImplies<T>
    {
        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfFreeBits, bool singletonSetSemantics)
        {
            return ToCore().GetAutomatonBDD(variables, alg, nrOfFreeBits, singletonSetSemantics);
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> GetAutomatonX(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> alg, bool singletonSetSemantics)
        {
            return ToCore().GetAutomatonX(variables, alg, singletonSetSemantics);
        }
    }

    public partial class MSOEquiv<T>
    {
        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfFreeBits, bool singletonSetSemantics)
        {
            return ToCore().GetAutomatonBDD(variables, alg, nrOfFreeBits, singletonSetSemantics);
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> GetAutomatonX(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> alg, bool singletonSetSemantics)
        {
            return ToCore().GetAutomatonX(variables, alg, singletonSetSemantics);
        }
    }

    public partial class MSOForall<T>
    {
        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfFreeBits, bool singletonSetSemantics)
        {
            return ToCore().GetAutomatonBDD(variables, alg, nrOfFreeBits, singletonSetSemantics);
        }

        internal override Automaton<IMonadicPredicate<BDD, T>> GetAutomatonX(SimpleList<Variable> variables, ICartesianAlgebraBDD<T> alg, bool singletonSetSemantics)
        {
            return ToCore().GetAutomatonX(variables, alg, singletonSetSemantics);
        }
    }
}





