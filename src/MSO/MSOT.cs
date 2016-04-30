using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Automata;

namespace Microsoft.Automata.MSO
{
    /// <summary>
    /// Base class for all MSO formulas
    /// </summary>
    public abstract class MSOFormula<T>
    {
        /// <summary>
        /// Constructs the equivalent WS1S formula.
        /// </summary>
        public abstract WS1SFormula<T> ToWS1S();

        internal abstract bool IsWellFormedFormula(List<string> fovars, List<string> sovars);

        /// <summary>
        /// Returns true iff the formula is closed and all its variables are used in a well-formed manner.
        /// </summary>
        public virtual bool IsWellFormedFormula()
        {
            return IsWellFormedFormula(new List<string>(), new List<string>());
        }

        /// <summary>
        /// Constructs the automaton. The formula must be closed.
        /// </summary>
        public Automaton<T> GetAutomaton(IBooleanAlgebra<T> elementAlgebra)
        {
            var css = elementAlgebra as CharSetSolver;
            if (css != null)
            {
                var ws1s = this.ToWS1S();
                var aut = ws1s.GetAutomatonBDD(css, (int)css.Encoding) as Automaton<T>;
                return aut;
            }
            else
            {
                var ws1s = this.ToWS1S();
                var aut = ws1s.GetAutomaton(new BDDAlgebra<T>(elementAlgebra));
                return BasicAutomata.Restrict(aut);
            }
        }

        /// <summary>
        /// Gets the automaton over the product algebra. All the free variables in the formula must occur among fv.
        /// </summary>
        public Automaton<IMonadicPredicate<BDD, T>> GetAutomaton(ICartesianAlgebraBDD<T> alg, params string[] fv) 
        {
            var fvs = Array.ConvertAll(fv, v => new WS1SVariable<T>(v));
            return ToWS1S().GetAutomaton(alg, fvs);
        }

        /// <summary>
        /// Gets the automaton over alg where BDDs are extended with new bit positions for the variables. All the free variables in the formula must occur among fv.
        /// The type T must be BDD.
        /// </summary>
        public Automaton<BDD> GetAutomaton(CharSetSolver alg, params string[] fv)
        {
            var fvs = Array.ConvertAll(fv, v => new WS1SVariable<T>(v));
            return ToWS1S().GetAutomatonBDD(alg, (int)alg.Encoding, fvs);
        }

        public abstract void ToString(StringBuilder sb);

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            ToString(sb);
            return sb.ToString();
        }
    }

    /// <summary>
    /// True formula.
    /// </summary>
    public class MSOTrue<T> : MSOFormula<T>
    {
        public MSOTrue() { }

        public override WS1SFormula<T> ToWS1S()
        {
            return new WS1STrue<T>();
        }

        internal override bool IsWellFormedFormula(List<string> fovars, List<string> sovars)
        {
            return true;
        }
        public override void ToString(StringBuilder sb)
        {
            sb.AppendFormat("True");
        }
    }

    /// <summary>
    /// False formula.
    /// </summary>
    public class MSOFalse<T> : MSOFormula<T>
    {
        public MSOFalse() { }

        public override WS1SFormula<T> ToWS1S()
        {
            return new WS1SNot<T>(new WS1STrue<T>());
        }

        internal override bool IsWellFormedFormula(List<string> fovars, List<string> sovars)
        {
            return true;
        }
        public override void ToString(StringBuilder sb)
        {
            sb.AppendFormat("False");
        }
    }

    /// <summary>
    /// Formulas with two variables
    /// </summary>
    public abstract class MSOBinaryFormula<T> : MSOFormula<T>
    {
        protected readonly string var1;
        protected readonly string var2;

        public MSOBinaryFormula(string var1, string var2)
        {
            this.var1 = var1;
            this.var2 = var2;
        }

        public WS1SVariable<T> Var1(bool asFirstOrderVariable = false)
        {
            return new WS1SVariable<T>(var1, asFirstOrderVariable); 
        }

        public WS1SVariable<T> Var2(bool asFirstOrderVariable = false)
        {
            return new WS1SVariable<T>(var2, asFirstOrderVariable);
        }
    }

    /// <summary>
    /// Equality of variables.
    /// </summary>
    public class MSOEq<T> : MSOBinaryFormula<T>
    {
        public MSOEq(string var1, string var2) : base(var1, var2) { }

        public override WS1SFormula<T> ToWS1S()
        {
            return new WS1SEq<T>(Var1(), Var2());
        }

        public override void ToString(StringBuilder sb)
        {
            sb.AppendFormat("({0}={1})", Var1(), Var2());
        }

        internal override bool IsWellFormedFormula(List<string> fovars, List<string> sovars)
        {
            return (fovars.Contains(var1) && fovars.Contains(var2)) || (sovars.Contains(var1) && sovars.Contains(var2));
        }
    }

    #region logical connectives

    /// <summary>
    /// Binary operators
    /// </summary>
    public abstract class MSOBinaryOp<T> : MSOFormula<T>
    {
        public readonly MSOFormula<T> phi1;
        public readonly MSOFormula<T> phi2;

        public MSOBinaryOp(MSOFormula<T> phi1, MSOFormula<T> phi2)
        {
            this.phi1 = phi1;
            this.phi2 = phi2;
        }

        internal override bool IsWellFormedFormula(List<string> fovars, List<string> sovars)
        {
            return phi1.IsWellFormedFormula(fovars, sovars) && phi2.IsWellFormedFormula(fovars, sovars);
        }
    }

    /// <summary>
    /// Conjuction of phi1 and phi2.
    /// </summary>
    public class MSOAnd<T> : MSOBinaryOp<T>
    {
        public MSOAnd(MSOFormula<T> phi1, MSOFormula<T> phi2) : base(phi1, phi2) { }

        public override WS1SFormula<T> ToWS1S()
        {
            return new WS1SAnd<T>(phi1.ToWS1S(), phi2.ToWS1S());
        }
        public override void ToString(StringBuilder sb)
        {
            sb.Append("(");
            phi1.ToString(sb);
            sb.Append("&");
            phi2.ToString(sb);
            sb.Append(")");
        }
    }

    /// <summary>
    /// Disjunction of phi1 and phi2.
    /// </summary>
    public class MSOOr<T> : MSOBinaryOp<T>
    {
        public MSOOr(MSOFormula<T> phi1, MSOFormula<T> phi2) : base(phi1, phi2) { }

        public override WS1SFormula<T> ToWS1S()
        {
            return new WS1SOr<T>(phi1.ToWS1S(), phi2.ToWS1S());
        }
        public override void ToString(StringBuilder sb)
        {
            sb.Append("(");
            phi1.ToString(sb);
            sb.Append("|");
            phi2.ToString(sb);
        }
    }

    /// <summary>
    /// Implication from phi1 to phi2.
    /// </summary>
    public class MSOIf<T> : MSOBinaryOp<T>
    {
        public MSOIf(MSOFormula<T> phi1, MSOFormula<T> phi2) : base(phi1, phi2) { }

        public override WS1SFormula<T> ToWS1S()
        {
            return new WS1SOr<T>(new WS1SNot<T>(phi1.ToWS1S()), phi2.ToWS1S());
        }
        public override void ToString(StringBuilder sb)
        {
            sb.Append("(");
            phi1.ToString(sb);
            sb.Append("=>");
            phi2.ToString(sb);
            sb.Append(")");
        }
    }

    /// <summary>
    /// Equivalence of phi1 and phi2.
    /// </summary>
    public class MSOIff<T> : MSOBinaryOp<T>
    {
        public MSOIff(MSOFormula<T> phi1, MSOFormula<T> phi2) : base(phi1, phi2) { }

        public override WS1SFormula<T> ToWS1S()
        {
            var psi1 = phi1.ToWS1S();
            var psi2 = phi2.ToWS1S();
            return new WS1SOr<T>(new WS1SAnd<T>(psi1, psi2), new WS1SAnd<T>(new WS1SNot<T>(psi1), new WS1SNot<T>(psi2)));
        }
        public override void ToString(StringBuilder sb)
        {
            sb.Append("(");
            phi1.ToString(sb);
            sb.Append("<=>");
            phi2.ToString(sb);
            sb.Append(")");
        }
    }

    /// <summary>
    /// Complement of phi
    /// </summary>
    public class MSONot<T> : MSOFormula<T>
    {
        public readonly MSOFormula<T> phi;

        public MSONot(MSOFormula<T> phi)
        {
            this.phi = phi;
        }

        public override WS1SFormula<T> ToWS1S()
        {
            return new WS1SNot<T>(phi.ToWS1S());
        }

        internal override bool IsWellFormedFormula(List<string> fovars, List<string> sovars)
        {
            return phi.IsWellFormedFormula(fovars, sovars);
        }
        public override void ToString(StringBuilder sb)
        {
            sb.Append("~");
            phi.ToString(sb);
        }
    }

    #endregion

    #region quantifiers

    /// <summary>
    /// Quantified formulas
    /// </summary>
    public abstract class MSOQuantifiedFormula<T> : MSOFormula<T>
    {
        /// <summary>
        /// quantified variable
        /// </summary>
        public readonly string var;
        public readonly MSOFormula<T> phi;

        public MSOQuantifiedFormula(string var, MSOFormula<T> phi)
        {
            this.var = var;
            this.phi = phi;
        }

        public WS1SVariable<T> Var(bool asFirstOrderVariable = false)
        {
            return new WS1SVariable<T>(var, asFirstOrderVariable);
        }
    }

    /// <summary>
    /// First-order quantifiers
    /// </summary>
    public abstract class MSOQuantifiedFOFormula<T> : MSOQuantifiedFormula<T>
    {
        public MSOQuantifiedFOFormula(string var, MSOFormula<T> phi) : base(var, phi) { }

        internal override bool IsWellFormedFormula(List<string> fovars, List<string> sovars)
        {
            if (sovars.Contains(var))
                return false;

            var fovarsNew = new List<string>(fovars);
            fovarsNew.Insert(0, var);
            return phi.IsWellFormedFormula(fovarsNew, sovars);
        }
    }

    /// <summary>
    /// Existential first-order quantifier
    /// </summary>
    public class MSOExistsFo<T> : MSOQuantifiedFOFormula<T>
    {
        public MSOExistsFo(string var, MSOFormula<T> phi) : base(var, phi) { }

        public override WS1SFormula<T> ToWS1S()
        {
            var X = new WS1SVariable<T>(var);
            return new WS1SExists<T>(X, new WS1SAnd<T>(new WS1SSingleton<T>(X), phi.ToWS1S()));
        }

        public override void ToString(StringBuilder sb)
        {
            sb.AppendFormat("Exists({0},",var);
            phi.ToString(sb);
            sb.Append(")");
        }
    }

    /// <summary>
    /// Universal first-order quantifier
    /// </summary>
    public class MSOForallFo<T> : MSOQuantifiedFOFormula<T>
    {
        public MSOForallFo(string var, MSOFormula<T> phi) : base(var, phi) { }

        public override WS1SFormula<T> ToWS1S()
        {
            var X = new WS1SVariable<T>(var);
            return new WS1SNot<T>(new WS1SExists<T>(X, new WS1SAnd<T>(new WS1SSingleton<T>(X), new WS1SNot<T>(phi.ToWS1S()))));
        }
        public override void ToString(StringBuilder sb)
        {
            sb.AppendFormat("Forall({0},", var);
            phi.ToString(sb);
            sb.Append(")");
        }
    }

    /// <summary>
    /// Second-order quantifiers
    /// </summary>
    public abstract class MSOQuantifiedSOFormula<T> : MSOQuantifiedFormula<T>
    {
        public MSOQuantifiedSOFormula(string var, MSOFormula<T> phi) : base(var, phi) { }

        internal override bool IsWellFormedFormula(List<string> fovars, List<string> sovars)
        {
            if (fovars.Contains(var))
                return false;

            var sovarsNew = new List<string>(sovars);
            sovarsNew.Insert(0, var);
            return phi.IsWellFormedFormula(fovars, sovarsNew);
        }
    }

    /// <summary>
    /// Existential second-order quantifier
    /// </summary>
    public class MSOExistsSo<T> : MSOQuantifiedSOFormula<T>
    {
        public MSOExistsSo(string var, MSOFormula<T> phi) : base(var, phi) { }

        public override WS1SFormula<T> ToWS1S()
        {
            var X = new WS1SVariable<T>(var);
            return new WS1SExists<T>(X, phi.ToWS1S());
        }
        public override void ToString(StringBuilder sb)
        {
            sb.AppendFormat("EXISTS({0},", var);
            phi.ToString(sb);
            sb.Append(")");
        }
    }

    /// <summary>
    /// Universal second-order quantifier
    /// </summary>
    public class MSOForallSo<T> : MSOQuantifiedSOFormula<T>
    {
        public MSOForallSo(string var, MSOFormula<T> phi) : base(var, phi) { }

        public override WS1SFormula<T> ToWS1S()
        {
            return new WS1SNot<T>(new WS1SExists<T>(Var(), new WS1SNot<T>(phi.ToWS1S())));
        }
        public override void ToString(StringBuilder sb)
        {
            sb.AppendFormat("FORALL({0},", var);
            phi.ToString(sb);
            sb.Append(")");
        }
    }

    #endregion

    #region 1 position variable
    /// <summary>
    /// Formulas over one position variable.
    /// </summary>
    public abstract class MSOUnaryPosFormula<T> : MSOFormula<T>
    {
        public readonly string var;

        public MSOUnaryPosFormula(string var)
        {
            this.var = var;
        }

        internal override bool IsWellFormedFormula(List<string> fovars, List<string> sovars)
        {
            return fovars.Contains(var);
        }

        public WS1SVariable<T> Var(bool asFirstOrderVariable = false)
        {
            return new WS1SVariable<T>(var, asFirstOrderVariable);
        }
    }

    /// <summary>
    /// Unary predicate [pred](var) where pred is a predicate in the Boolean algebra over T and var is a position variable.
    /// </summary>
    public class MSOPredicate<T> : MSOUnaryPosFormula<T>
    {
        public readonly T pred;

        public MSOPredicate(T pred, string var) : base(var)
        {
            this.pred = pred;
        }

        public override WS1SFormula<T> ToWS1S()
        {
            return new WS1SPred<T>(pred, Var());  
        }

        public override void ToString(StringBuilder sb)
        {
            sb.AppendFormat("[{0}]({1})", pred.ToString(), var);
        }
    }

    /// <summary>
    /// First occurrence of position variable var
    /// </summary>
    public class MSOFirst<T> : MSOUnaryPosFormula<T>
    {
        public MSOFirst(string var) : base(var) { }

        public override WS1SFormula<T> ToWS1S()
        {
            var Y = new WS1SVariable<T>(var + "_");
            return new WS1SNot<T>(new WS1SExists<T>(Y, Y < Var()));
        }

        public override void ToString(StringBuilder sb)
        {
            sb.AppendFormat("First({0})", var);
        }
    }

    /// <summary>
    /// Last occurrence of position variable var
    /// </summary>
    public class MSOLast<T> : MSOUnaryPosFormula<T>
    {
        public MSOLast(string var) : base(var) { }

        public override WS1SFormula<T> ToWS1S()
        {
            var Y = new WS1SVariable<T>(var + "_");
            return new WS1SNot<T>(new WS1SExists<T>(Y, Var() < Y));
        }
        public override void ToString(StringBuilder sb)
        {
            sb.AppendFormat("Last({0})", var);
        }
    }
    #endregion

    #region 2 position variables
    /// <summary>
    /// Binary formulas over position variables.
    /// </summary>
    public abstract class MSOBinaryPosFormula<T> : MSOBinaryFormula<T>
    {
        public MSOBinaryPosFormula(string var1, string var2)
            : base(var1, var2)
        {
        }

        internal override bool IsWellFormedFormula(List<string> fovars, List<string> sovars)
        {
            return fovars.Contains(var1) && fovars.Contains(var2);
        }
    }

    /// <summary>
    /// The position variable var2 is the immediate successor or position variable var1
    /// </summary>
    public class MSOSucc<T> : MSOBinaryPosFormula<T>
    {
        public MSOSucc(string var1, string var2) : base(var1, var2) { }

        public override WS1SFormula<T> ToWS1S()
        {
            return new WS1SSuccN<T>(Var1(), Var2(), 1);
        }
        public override void ToString(StringBuilder sb)
        {
            sb.AppendFormat("({0}={1}+1)", var2, var1);
        }
    }

    /// <summary>
    /// The position variable var2 is the n'th successor of position variable var1
    /// </summary>
    public class MSOSuccN<T> : MSOBinaryPosFormula<T>
    {
        public readonly int n;

        public MSOSuccN(string var1, string var2, int n)
            : base(var1, var2)
        {
            this.n = n;
        }

        public override WS1SFormula<T> ToWS1S()
        {
            return new WS1SSuccN<T>(Var1(), Var2(), n);
        }
        public override void ToString(StringBuilder sb)
        {
            sb.AppendFormat("({0}={1}+{2})", var2, var1, n);
        }
    }

    /// <summary>
    /// The position variable var1 is less than the position variable var2
    /// </summary>
    public class MSOLt<T> : MSOBinaryPosFormula<T>
    {
        public MSOLt(string var1, string var2) : base(var1, var2) { }

        public override WS1SFormula<T> ToWS1S()
        {
            return new WS1SLt<T>(Var1(), Var2());
        }
        public override void ToString(StringBuilder sb)
        {
            sb.AppendFormat("({0}<{1})", var1, var2);
        }
    }
    #endregion

    #region 2 set variables
    /// <summary>
    /// Formulas over two set variables
    /// </summary>
    public abstract class MSOBinarySetFormula<T> : MSOBinaryFormula<T>
    {

        public MSOBinarySetFormula(string var1, string var2) : base(var1,var2)
        {
        }

        internal override bool IsWellFormedFormula(List<string> fovars, List<string> sovars)
        {
            return sovars.Contains(var1) && sovars.Contains(var2);
        }
    }

    /// <summary>
    /// Set var1 is a subset of set var2.
    /// </summary>
    public class MSOSubset<T> : MSOBinarySetFormula<T>
    {
        public MSOSubset(string var1, string var2) : base(var1, var2) { }

        public override WS1SFormula<T> ToWS1S()
        {
            return new WS1SSubset<T>(new WS1SVariable<T>(var1), new WS1SVariable<T>(var2));
        }
        public override void ToString(StringBuilder sb)
        {
            sb.AppendFormat("Subset({0},{1})", var1, var2);
        }
    }

    ///// <summary>
    ///// Set var1 is a strict subset of set var2.
    ///// </summary
    //public class MSOSubsetStrict<T> : MSOBinarySetFormula<T>
    //{
    //    public MSOSubsetStrict(string var1, string var2) : base(var1, var2) { }

    //    public override WS1SFormula<T> ToWS1S()
    //    {
    //        return new WS1SAnd<T>(new WS1SSubset<T>(var1, var2), new WS1SNot<T>(new WS1SSubset<T>(var2, var1)));
    //    }
    //    public override void ToString(StringBuilder sb)
    //    {
    //        sb.AppendFormat("StrictSubset({0},{1})", var1, var2);
    //    }
    //}
    #endregion
}

