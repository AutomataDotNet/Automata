using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Automata;

namespace Microsoft.Automata.MSO
{
    /// <summary>
    /// Enumeration of the kind of formula
    /// </summary>
    public enum MSOFormulaKind
    {
        True, False, And, Or, Implies, Equiv, Not, Eq, In, Exists, Forall,
        Predicate, First, Last, EqN, SuccN, IsEmpty, Min, Max, Subset,
        Lt, Le, IsSingleton
    }

    /// <summary>
    /// Base class for all MSO formulas 
    /// </summary>
    public abstract partial class MSOFormula<T>
    {
        /// <summary>
        /// Idempotent operation that constructs an equivalent predicate in the core subset of MSO formulas.
        /// </summary>
        public abstract MSOFormula<T> ToCore();

        /// <summary>
        /// conjunction: pred1 &amp; pred2
        /// </summary>
        /// <param name="pred1">first predicate</param>
        /// <param name="pred2">second predicate</param>
        public static MSOFormula<T> operator &(MSOFormula<T> pred1, MSOFormula<T> pred2)
        {
            return new MSOAnd<T>(pred1, pred2);
        }

        /// <summary>
        /// disjunction: pred1 | pred2
        /// </summary>
        /// <param name="pred1">first predicate</param>
        /// <param name="pred2">second predicate</param>
        public static MSOFormula<T> operator |(MSOFormula<T> pred1, MSOFormula<T> pred2)
        {
            return new MSOOr<T>(pred1, pred2);
        }

        /// <summary>
        /// negation: ~pred
        /// </summary>
        /// <param name="pred">the predicate to be negated</param>
        public static MSOFormula<T> operator ~(MSOFormula<T> pred)
        {
            return new MSONot<T>(pred);
        }

        /// <summary>
        /// true iff the formula has no free variables
        /// </summary>
        public bool IsClosed
        {
            get
            {
                return FreeVariables.Length == 0;
            }
        }

        /// <summary>
        /// Prints the formula in Mona formula format. 
        /// Fresh first-order variables are denoted #i.
        /// Fresh second-order variables are denoted @i.
        /// </summary>
        public abstract void Print(StringBuilder sb);

        /// <summary>
        /// Displays the formula as a Mona program, 
        /// with declarations of all free variables.
        /// Fresh first-order variables are denoted #i.
        /// Fresh second-order variables are denoted @i.
        /// </summary>
        public string AsMonaProgram
        {
            get
            {
                StringBuilder pgm = new StringBuilder();
                foreach (var v in FreeVariables)
                    pgm.AppendLine(string.Format("var{1} {0};", v.Name, v.IsFirstOrder ? "1" : "2"));
                Print(pgm);
                pgm.Append(";");
                return pgm.ToString();
            }
        }

        /// <summary>
        /// Shows the formula in Mona formula format. 
        /// Fresh first-order variables are denoted #i.
        /// Fresh second-order variables are denoted @i.
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            Print(sb);
            return sb.ToString();
        }

        public abstract MSOFormulaKind Kind { get; }

        internal static int GetVarIndex(Variable x, SimpleList<Variable> xs)
        {
            var i = xs.IndexOf(x);
            if (i < 0)
                throw new ArgumentException(string.Format("list does not contain variable {0}", x), "xs");
            return i;
        }

        Variable[] fvs = null;
        /// <summary>
        /// Gets the list of all free variables sorted alphabetically.
        /// </summary>
        public Variable[] FreeVariables
        {
            get
            {
                if (fvs == null)
                {
                    var fvs_ = new List<Variable>(EnumerateFreeVariables(SimpleList<Variable>.Empty, new HashSet<Variable>()));
                    fvs_.Sort();
                    fvs = fvs_.ToArray();
                }
                return fvs;
            }
        }

        internal abstract IEnumerable<Variable> EnumerateFreeVariables(SimpleList<Variable> bound, HashSet<Variable> free);

        /// <summary>
        /// Returns true iff some subformula satisfies the predicate check.
        /// </summary>
        public abstract bool ExistsSubformula(Predicate<MSOFormula<T>> check); 
    }

    /// <summary>
    /// Atomic formulas with two variables
    /// </summary>
    public abstract partial class MSOBinaryAtom<T> : MSOFormula<T>
    {
        protected readonly Variable var1;
        protected readonly Variable var2;

        public MSOBinaryAtom(Variable var1, Variable var2)
        {
            this.var1 = var1;
            this.var2 = var2;
        }

        internal override IEnumerable<Variable> EnumerateFreeVariables(SimpleList<Variable> bound, HashSet<Variable> free)
        {
            if (!bound.Contains(var1))
                if (free.Add(var1))
                    yield return var1;
            if (!bound.Contains(var2))
                if (free.Add(var2))
                    yield return var2;
        }

        public override bool ExistsSubformula(Predicate<MSOFormula<T>> check)
        {
            return check(this);
        }
    }

    /// <summary>
    /// Atomic formulas with one variable
    /// </summary>
    public abstract partial class MSOUnaryAtom<T> : MSOFormula<T>
    {
        public readonly Variable var;

        public MSOUnaryAtom(Variable var)
        {
            this.var = var;
        }

        internal override IEnumerable<Variable> EnumerateFreeVariables(SimpleList<Variable> bound, HashSet<Variable> free)
        {
            if (!bound.Contains(var))
                if (free.Add(var))
                    yield return var;
        }

        public override bool ExistsSubformula(Predicate<MSOFormula<T>> check)
        {
            return check(this);
        }
    }

    /// <summary>
    /// Boolean formulas with binary operator
    /// </summary>
    public abstract partial class MSOBinaryOp<T> : MSOFormula<T>
    {
        public readonly MSOFormula<T> phi1;
        public readonly MSOFormula<T> phi2;

        public MSOBinaryOp(MSOFormula<T> phi1, MSOFormula<T> phi2)
        {
            this.phi1 = phi1;
            this.phi2 = phi2;
        }

        internal override IEnumerable<Variable> EnumerateFreeVariables(SimpleList<Variable> bound, HashSet<Variable> free)
        {
            foreach (var x in phi1.EnumerateFreeVariables(bound, free))
                yield return x;
            foreach (var x in phi2.EnumerateFreeVariables(bound, free))
                yield return x;
        }

        public override bool ExistsSubformula(Predicate<MSOFormula<T>> check)
        {
            return check(this) || phi1.ExistsSubformula(check) || phi1.ExistsSubformula(check);
        }
    }

    /// <summary>
    /// Quantified formulas
    /// </summary>
    public abstract partial class MSOQuantifiedFormula<T> : MSOFormula<T>
    {
        /// <summary>
        /// quantified variable
        /// </summary>
        public readonly Variable var;
        public readonly MSOFormula<T> phi;
        public abstract bool IsForall { get; }
        public bool IsFirstOrder
        {
            get { return var.IsFirstOrder; }
        }

        public MSOQuantifiedFormula(Variable var, MSOFormula<T> phi)
        {
            this.var = var;
            this.phi = phi;
        }

        public override MSOFormula<T> ToCore()
        {
            throw new NotImplementedException();
        }

        public override void Print(StringBuilder sb)
        {
            throw new NotImplementedException();
        }

        public override MSOFormulaKind Kind
        {
            get { throw new NotImplementedException(); }
        }

        internal override IEnumerable<Variable> EnumerateFreeVariables(SimpleList<Variable> bound, HashSet<Variable> free)
        {
            foreach (var x in phi.EnumerateFreeVariables(bound.Append(var), free))
                yield return x;
        }

        public override bool ExistsSubformula(Predicate<MSOFormula<T>> check)
        {
            return check(this) || phi.ExistsSubformula(check);
        }
    }

    #region Core formulas
    /// <summary>
    /// true
    /// </summary>
    public partial class MSOTrue<T> : MSOFormula<T>
    {
        public MSOTrue() { }

        public override void Print(StringBuilder sb)
        {
            sb.AppendFormat("true");
        }

        public override MSOFormula<T> ToCore()
        {
            return this;
        }

        public override MSOFormulaKind Kind
        {
            get { return MSOFormulaKind.True; }
        }

        internal override IEnumerable<Variable> EnumerateFreeVariables(SimpleList<Variable> bound, HashSet<Variable> free)
        {
            yield break;
        }

        public override bool ExistsSubformula(Predicate<MSOFormula<T>> check)
        {
            return check(this);
        }
    }

    /// <summary>
    /// false
    /// </summary>
    public partial class MSOFalse<T> : MSOFormula<T>
    {
        public MSOFalse() { }

        public override void Print(StringBuilder sb)
        {
            sb.AppendFormat("false");
        }

        public override MSOFormula<T> ToCore()
        {
            return this;
        }

        public override MSOFormulaKind Kind
        {
            get { return MSOFormulaKind.False; }
        }

        internal override IEnumerable<Variable> EnumerateFreeVariables(SimpleList<Variable> bound, HashSet<Variable> free)
        {
            yield break;
        }

        public override bool ExistsSubformula(Predicate<MSOFormula<T>> check)
        {
            return check(this);
        }
    }

    /// <summary>
    /// equality: var1 = var2
    /// </summary>
    public partial class MSOEq<T> : MSOBinaryAtom<T>
    {
        public MSOEq(Variable var1, Variable var2)
            : base(var1, var2)
        {
            if (var1.IsFirstOrder != var2.IsFirstOrder)
                throw new ArgumentException("variables do not have same type");
        }

        public override void Print(StringBuilder sb)
        {
            sb.AppendFormat("({0} = {1})", var1, var2);
        }

        public override MSOFormula<T> ToCore()
        {
            return this;
        }

        public override MSOFormulaKind Kind
        {
            get { return MSOFormulaKind.Eq; }
        }
    }

    /// <summary>
    /// element of: var1 in var2
    /// </summary>
    public partial class MSOIn<T> : MSOBinaryAtom<T>
    {
        public MSOIn(Variable var1, Variable var2)
            : base(var1, var2)
        {
            if (!var1.IsFirstOrder)
                throw new ArgumentException("variable type not first-order", "var1");
            if (var2.IsFirstOrder)
                throw new ArgumentException("variable type not second-order", "var2");
        }

        public override void Print(StringBuilder sb)
        {
            sb.AppendFormat("({0} in {1})", var1, var2);
        }

        public override MSOFormula<T> ToCore()
        {
            return this;
        }

        public override MSOFormulaKind Kind
        {
            get { return MSOFormulaKind.In; }
        }
    }

    /// <summary>
    /// minimum of a set: var1 = min var2
    /// </summary>
    public partial class MSOMin<T> : MSOBinaryAtom<T>
    {
        public MSOMin(Variable var1, Variable var2)
            : base(var1, var2)
        {
            if (!var1.IsFirstOrder)
                throw new ArgumentException("variable type not first-order", "var1");
            if (var2.IsFirstOrder)
                throw new ArgumentException("variable type not second-order", "var2");
        }

        public override void Print(StringBuilder sb)
        {
            sb.AppendFormat("({0} = min {1})", var1, var2);
        }

        public override MSOFormula<T> ToCore()
        {
            return this;
        }

        public override MSOFormulaKind Kind
        {
            get { return MSOFormulaKind.Min; }
        }
    }

    /// <summary>
    /// maximum of a set: var1 = max var2
    /// </summary>
    public partial class MSOMax<T> : MSOBinaryAtom<T>
    {
        public MSOMax(Variable var1, Variable var2)
            : base(var1, var2)
        {
            if (!var1.IsFirstOrder)
                throw new ArgumentException("variable type not first-order", "var1");
            if (var2.IsFirstOrder)
                throw new ArgumentException("variable type not second-order", "var2");
        }

        public override void Print(StringBuilder sb)
        {
            sb.AppendFormat("({0} = max {1})", var1, var2);
        }

        public override MSOFormula<T> ToCore()
        {
            return this;
        }

        public override MSOFormulaKind Kind
        {
            get { return MSOFormulaKind.Max; }
        }
    }

    /// <summary>
    /// conjunction: phi1 &amp; phi2
    /// </summary>
    public partial class MSOAnd<T> : MSOBinaryOp<T>
    {
        public MSOAnd(MSOFormula<T> phi1, MSOFormula<T> phi2) : base(phi1, phi2) { }

        public override void Print(StringBuilder sb)
        {
            sb.Append("(");
            phi1.Print(sb);
            sb.Append("&");
            phi2.Print(sb);
            sb.Append(")");
        }

        public override MSOFormula<T> ToCore()
        {
            var f1 = phi1.ToCore();
            var f2 = phi2.ToCore();
            if (phi1 == f1 && phi2 == f2)
                return this;
            else
                return new MSOAnd<T>(f1, f2);
        }

        public override MSOFormulaKind Kind
        {
            get { return MSOFormulaKind.And; }
        }
    }

    /// <summary>
    /// disjunction: phi1 | phi2
    /// </summary>
    public partial class MSOOr<T> : MSOBinaryOp<T>
    {
        public MSOOr(MSOFormula<T> phi1, MSOFormula<T> phi2) : base(phi1, phi2) { }

        public override void Print(StringBuilder sb)
        {
            sb.Append("(");
            phi1.Print(sb);
            sb.Append("|");
            phi2.Print(sb);
            sb.Append(")");
        }

        public override MSOFormula<T> ToCore()
        {
            var f1 = phi1.ToCore();
            var f2 = phi2.ToCore();
            if (phi1 == f1 && phi2 == f2)
                return this;
            else
                return new MSOOr<T>(f1, f2);
        }
        public override MSOFormulaKind Kind
        {
            get { return MSOFormulaKind.Or; }
        }
    }

    /// <summary>
    /// negation: ~phi
    /// </summary>
    public partial class MSONot<T> : MSOFormula<T>
    {
        public readonly MSOFormula<T> phi;

        public MSONot(MSOFormula<T> phi)
        {
            this.phi = phi;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("~");
            phi.Print(sb);
        }

        public override MSOFormula<T> ToCore()
        {
            var psi = phi.ToCore();
            if (phi == psi)
                return this;
            else
                return new MSONot<T>(psi);
        }

        public override MSOFormulaKind Kind
        {
            get { return MSOFormulaKind.Not; }
        }

        internal override IEnumerable<Variable> EnumerateFreeVariables(SimpleList<Variable> bound, HashSet<Variable> free)
        {
            foreach (var x in phi.EnumerateFreeVariables(bound, free))
                yield return x;
        }

        public override bool ExistsSubformula(Predicate<MSOFormula<T>> check)
        {
            return check(this) || phi.ExistsSubformula(check);
        }
    }

    /// <summary>
    /// [pred](var)
    /// </summary>
    public partial class MSOPredicate<T> : MSOUnaryAtom<T>
    {
        public readonly T pred;

        public MSOPredicate(T pred, Variable var)
            : base(var)
        {
            this.pred = pred;
        }

        public override void Print(StringBuilder sb)
        {
            sb.AppendFormat("[{0}]({1})", pred.ToString(), var);
        }

        public override MSOFormula<T> ToCore()
        {
            return this;
        }

        public override MSOFormulaKind Kind
        {
            get { return MSOFormulaKind.Predicate; }
        }
    }

    /// <summary>
    /// var is the last position
    /// </summary>
    public partial class MSOLast<T> : MSOUnaryAtom<T>
    {
        public MSOLast(Variable var)
            : base(var)
        {
            if (!var.IsFirstOrder)
                throw new ArgumentException("variable type not first-order", "var1");
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(ToCore().ToString());
        }
        public override MSOFormula<T> ToCore()
        {
            return this;
        }

        public override MSOFormulaKind Kind
        {
            get { return MSOFormulaKind.Last; }
        }
    }

    /// <summary>
    /// N'th successor: var2 = var1 + N
    /// </summary>
    public partial class MSOSuccN<T> : MSOBinaryAtom<T>
    {
        int n;
        public int N
        {
            get { return n; }
        }

        public MSOSuccN(Variable var1, Variable var2, int n)
            : base(var1, var2)
        {
            if (!var1.IsFirstOrder)
                throw new ArgumentException("variable type not first-order", "var1");
            if (!var2.IsFirstOrder)
                throw new ArgumentException("variable type not first-order", "var2");
            if (n <= 0)
                throw new ArgumentException("must be positive", "n");
            this.n = n;
        }

        public override void Print(StringBuilder sb)
        {
            sb.AppendFormat("({0} = {1} + {2})", var2, var1, N);
        }

        public override MSOFormula<T> ToCore()
        {
            return this;
        }

        public override MSOFormulaKind Kind
        {
            get { return MSOFormulaKind.SuccN; }
        }
    }

    /// <summary>
    /// equals N: var = N
    /// </summary>
    public partial class MSOeqN<T> : MSOUnaryAtom<T>
    {
        int n;
        public int N
        {
            get
            {
                return n;
            }
        }

        public MSOeqN(Variable x, int n)
            : base(x)
        {
            if (!x.IsFirstOrder)
                throw new ArgumentException("variable type not first-order", "x");
            if (n < 0)
                throw new ArgumentException("value must be nonnegative", "n");
            this.n = n;
        }

        public override void Print(StringBuilder sb)
        {
            sb.AppendFormat("({0} = {1})", var, n);
        }

        public override MSOFormula<T> ToCore()
        {
            return this;
        }

        public override MSOFormulaKind Kind
        {
            get { return MSOFormulaKind.EqN; }
        }
    }

    /// <summary>
    /// less than: var1 &lt; var2
    /// </summary>
    public partial class MSOLt<T> : MSOBinaryAtom<T>
    {
        public MSOLt(Variable var1, Variable var2)
            : base(var1, var2)
        {
            if (!var1.IsFirstOrder)
                throw new ArgumentException(string.Format("variable {0} is not first order", var1.Name), "var1");
            if (!var2.IsFirstOrder)
                throw new ArgumentException(string.Format("variable {0} is not first order", var2.Name), "var2");
        }

        public override void Print(StringBuilder sb)
        {
            sb.AppendFormat("({0} < {1})", var1, var2);
        }

        public override MSOFormula<T> ToCore()
        {
            return this;
        }

        public override MSOFormulaKind Kind
        {
            get { return MSOFormulaKind.Lt; }
        }
    }

    /// <summary>
    /// less than or equal: var1 &lt;= var2
    /// </summary>
    public partial class MSOLe<T> : MSOBinaryAtom<T>
    {
        public MSOLe(Variable var1, Variable var2)
            : base(var1, var2)
        {
            if (!var1.IsFirstOrder)
                throw new ArgumentException(string.Format("variable {0} is not first order", var1.Name), "var1");
            if (!var2.IsFirstOrder)
                throw new ArgumentException(string.Format("variable {0} is not first order", var2.Name), "var2");
        }

        public override void Print(StringBuilder sb)
        {
            sb.AppendFormat("({0} <= {1})", var1, var2);
        }

        public override MSOFormula<T> ToCore()
        {
            return this;
        }

        public override MSOFormulaKind Kind
        {
            get { return MSOFormulaKind.Le; }
        }
    }

    /// <summary>
    /// var1 is a subset of var2.
    /// </summary>
    public partial class MSOSubset<T> : MSOBinaryAtom<T>
    {
        public MSOSubset(Variable var1, Variable var2)
            : base(var1, var2)
        {
            if (var1.IsFirstOrder)
                throw new ArgumentException(string.Format("variable {0} is not second-order", var1.Name), "var1");
            if (var2.IsFirstOrder)
                throw new ArgumentException(string.Format("variable {0} is not second-order", var2.Name), "var2");
        }

        public override void Print(StringBuilder sb)
        {
            sb.AppendFormat("({0} sub {1})", var1, var2);
        }

        public override MSOFormula<T> ToCore()
        {
            return this;
        }

        public override MSOFormulaKind Kind
        {
            get { return MSOFormulaKind.Subset; }
        }
    }

    /// <summary>
    /// existential quantification: ex var : phi(var)
    /// </summary>
    public partial class MSOExists<T> : MSOQuantifiedFormula<T>
    {
        public MSOExists(Variable var, MSOFormula<T> phi) : base(var, phi) { }

        public override void Print(StringBuilder sb)
        {
            sb.AppendFormat("ex{0} {1} : ", var.IsFirstOrder ? "1" : "2", var.Name);
            phi.Print(sb);
        }

        public override bool IsForall
        {
            get { return false; }
        }

        public override MSOFormula<T> ToCore()
        {
            var psi = phi.ToCore();
            if (psi == phi)
                return this;
            else
                return new MSOExists<T>(var, psi);
        }

        public override MSOFormulaKind Kind
        {
            get { return MSOFormulaKind.Exists; }
        }
    }

    /// <summary>
    /// empty(var)
    /// </summary>
    public partial class MSOIsEmpty<T> : MSOUnaryAtom<T>
    {
        public MSOIsEmpty(Variable var)
            : base(var)
        {
            if (var.IsFirstOrder)
                throw new ArgumentException(string.Format("variable {0} is not second-order", var.Name), "var");
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("empty(" + var.Name + ")");
        }

        public override MSOFormula<T> ToCore()
        {
            return this;
        }

        public override MSOFormulaKind Kind
        {
            get { return MSOFormulaKind.IsEmpty; }
        }
    }

    /// <summary>
    /// singleton set: exists x: var = {x}
    /// </summary>
    public partial class MSOIsSingleton<T> : MSOUnaryAtom<T>
    {
        public MSOIsSingleton(Variable var)
            : base(var)
        {
            if (var.IsFirstOrder)
                throw new ArgumentException(string.Format("variable {0} is not second-order", var.Name), "var");
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("ex1 _" + var.Name + ":(" + var.Name + " = {_" + var.Name + "})");
        }

        public override MSOFormula<T> ToCore()
        {
            return this;
        }

        public override MSOFormulaKind Kind
        {
            get { return MSOFormulaKind.IsSingleton; }
        }
    }
    #endregion

    #region Formulas defined with core formulas
    /// <summary>
    /// logical implication: phi1 =&gt; phi2
    /// </summary>
    public partial class MSOImplies<T> : MSOBinaryOp<T>
    {
        public MSOImplies(MSOFormula<T> phi1, MSOFormula<T> phi2) : base(phi1, phi2) { }

        public override void Print(StringBuilder sb)
        {
            sb.Append("(");
            phi1.Print(sb);
            sb.Append("=>");
            phi2.Print(sb);
            sb.Append(")");
        }

        public override MSOFormula<T> ToCore()
        {
            return new MSOOr<T>(new MSONot<T>(phi1.ToCore()), phi2.ToCore());
        }

        public override MSOFormulaKind Kind
        {
            get { return MSOFormulaKind.Implies; }
        }
    }

    /// <summary>
    /// logical equivalence: phi1 &lt;=&gt; phi2
    /// </summary>
    public partial class MSOEquiv<T> : MSOBinaryOp<T>
    {
        public MSOEquiv(MSOFormula<T> phi1, MSOFormula<T> phi2) : base(phi1, phi2) { }

        public override void Print(StringBuilder sb)
        {
            sb.Append("(");
            phi1.Print(sb);
            sb.Append("<=>");
            phi2.Print(sb);
            sb.Append(")");
        }
        public override MSOFormula<T> ToCore()
        {
            var psi1 = phi1.ToCore();
            var psi2 = phi2.ToCore();
            return new MSOOr<T>(new MSOAnd<T>(psi1, psi2), new MSOAnd<T>(new MSONot<T>(psi1), new MSONot<T>(psi2)));
        }
        public override MSOFormulaKind Kind
        {
            get { return MSOFormulaKind.Equiv; }
        }
    }

    /// <summary>
    /// universal quantification: forall var: phi(var)
    /// </summary>
    public partial class MSOForall<T> : MSOQuantifiedFormula<T>
    {
        public MSOForall(Variable var, MSOFormula<T> phi) : base(var, phi) { }

        public override void Print(StringBuilder sb)
        {
            sb.AppendFormat("all{0} {1} : ", var.IsFirstOrder ? "1" : "2", var.Name);
            phi.Print(sb);
        }

        public override bool IsForall
        {
            get { return true; }
        }

        public override MSOFormula<T> ToCore()
        {
            return new MSONot<T>(new MSOExists<T>(var, new MSONot<T>(phi.ToCore())));
        }

        public override MSOFormulaKind Kind
        {
            get { return MSOFormulaKind.Forall; }
        }
    }
    #endregion
}



