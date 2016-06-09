using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Automata;

namespace Microsoft.Automata.MSO
{
    public enum MSOFormulaKind
    {
        True, False, And, Or, Implies, Equiv, Not, Eq, In, Exists, Forall,
        Predicate, First, Last, EqN, SuccN, IsEmpty, Min, Max, Subset,
        Lt, Le
    }

    /// <summary>
    /// Base class for all MSO formulas
    /// </summary>
    public abstract class MSOFormula<T>
    {
        /// <summary>
        /// Constructs the equivalent WS1S formula.
        /// </summary>
        public abstract WS1SFormula<T> ToWS1S();

        /// <summary>
        /// Idempotent operation that constructs an equivalent predicate in the core subset of MSO formulas.
        /// </summary>
        public abstract MSOFormula<T> ToCore(); 

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
        public Automaton<IMonadicPredicate<BDD, T>> GetAutomaton(ICartesianAlgebraBDD<T> alg, params Variable[] fvs)
        {
            return ToWS1S().GetAutomaton(alg, fvs);
        }

        /// <summary>
        /// Gets the automaton over alg where BDDs are extended with new bit positions for the variables. 
        /// All the free variables in the formula must occur among fv.
        /// The type T must be BDD.
        public Automaton<BDD> GetAutomaton(IBDDAlgebra alg, int nrOfReservedBits, Variable[] fvs)
        {
            return GetAutomatonBDD(SimpleList<Variable>.Empty.Append(fvs), alg, nrOfReservedBits);
        }

        /// <summary>
        /// Gets the automaton over alg where BDDs are extended with new bit positions for the variables. 
        /// The type T must be BDD.
        public Automaton<BDD> GetAutomaton(IBDDAlgebra alg, int nrOfReservedBits)
        {
            return GetAutomatonBDD(SimpleList<Variable>.Empty.Append(FreeVariables.ToArray()), alg, nrOfReservedBits);
        }

        /// <summary>
        /// Prints the formula in Mona format
        /// </summary>
        public abstract void Print(StringBuilder sb);

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            Print(sb);
            return sb.ToString();
        }

        public abstract MSOFormulaKind Kind { get; }

        internal abstract Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfFreeBits);

        internal static int GetVarIndex(Variable x, SimpleList<Variable> xs)
        {
            var i = xs.IndexOf(x);
            if (i < 0)
                throw new ArgumentException(string.Format("list does not contain variable {0}", x), "xs");
            return i;
        }

        List<Variable> fvs = null;
        /// <summary>
        /// Gets the list of all free variables sorted alphabetically.
        /// </summary>
        public List<Variable> FreeVariables
        {
            get
            {
                if (fvs == null)
                {
                    fvs = new List<Variable>(EnumerateFreeVariables(SimpleList<Variable>.Empty, new HashSet<Variable>()));
                    fvs.Sort();
                }
                return fvs;
            }
        }

        internal abstract IEnumerable<Variable> EnumerateFreeVariables(SimpleList<Variable> bound, HashSet<Variable> free);  
    }

    /// <summary>
    /// Atomic formulas with two variables
    /// </summary>
    public abstract class MSOBinaryAtom<T> : MSOFormula<T>
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
    }

    /// <summary>
    /// Atomic formulas with one variable
    /// </summary>
    public abstract class MSOUnaryAtom<T> : MSOFormula<T>
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
    }

    /// <summary>
    /// Boolean formulas with binary operator
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

        internal override IEnumerable<Variable> EnumerateFreeVariables(SimpleList<Variable> bound, HashSet<Variable> free)
        {
            foreach (var x in phi1.EnumerateFreeVariables(bound, free))
                yield return x;
            foreach (var x in phi2.EnumerateFreeVariables(bound, free))
                yield return x;
        }

    }

    /// <summary>
    /// Quantified formulas
    /// </summary>
    public abstract class MSOQuantifiedFormula<T> : MSOFormula<T>
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


        public override WS1SFormula<T> ToWS1S()
        {
            throw new NotImplementedException();
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

        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfFreeBits)
        {
            throw new NotImplementedException();
        }

        internal override IEnumerable<Variable> EnumerateFreeVariables(SimpleList<Variable> bound, HashSet<Variable> free)
        {
            foreach (var x in phi.EnumerateFreeVariables(bound.Append(var), free))
                yield return x;
        }

    }

    /// <summary>
    /// First-order or second-order variable
    /// </summary>
    public class Variable : IComparable
    {
        bool isfo;
        string name;
        public bool IsFirstOrder
        {
            get
            {
                return isfo;
            }
        }
        public string Name
        {
            get
            {
                return name;
            }
        }
        public Variable(string name, bool isFirstOrder)
        {
            this.name = name;
            this.isfo = isFirstOrder;
        }
        public override string ToString()
        {
            return name;
        }
        public override bool Equals(object obj)
        {
            var v = obj as Variable;
            if (v == null)
                return false;
            return (name.Equals(v.name));
        }
        public override int GetHashCode()
        {
            return name.GetHashCode();
        }

        public int CompareTo(object obj)
        {
            var v = obj as Variable;
            if (v == null)
                return -1;
            return name.CompareTo(v.name);
        }
    }


    #region Core formulas
    /// <summary>
    /// true
    /// </summary>
    public class MSOTrue<T> : MSOFormula<T>
    {
        public MSOTrue() { }

        public override WS1SFormula<T> ToWS1S()
        {
            return new WS1STrue<T>();
        }

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

        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            return BasicAutomata.MkTrue(alg);
        }

        internal override IEnumerable<Variable> EnumerateFreeVariables(SimpleList<Variable> bound, HashSet<Variable> free)
        {
            yield break;
        }
    }

    /// <summary>
    /// false
    /// </summary>
    public class MSOFalse<T> : MSOFormula<T>
    {
        public MSOFalse() { }

        public override WS1SFormula<T> ToWS1S()
        {
            return new WS1SNot<T>(new WS1STrue<T>());
        }
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

        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            return BasicAutomata.MkFalse(alg);
        }

        internal override IEnumerable<Variable> EnumerateFreeVariables(SimpleList<Variable> bound, HashSet<Variable> free)
        {
            yield break;
        }
    }

    /// <summary>
    /// var1 = var2
    /// </summary>
    public class MSOEq<T> : MSOBinaryAtom<T>
    {
        public MSOEq(Variable var1, Variable var2)
            : base(var1, var2)
        {
            if (var1.IsFirstOrder != var2.IsFirstOrder)
                throw new ArgumentException("variables do not have same type");
        }

        public override WS1SFormula<T> ToWS1S()
        {
            return new WS1SEq<T>(var1, var2);
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

        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            var pos1 = GetVarIndex(var1, variables) + nrOfLabelBits;
            var pos2 = GetVarIndex(var2, variables) + nrOfLabelBits;

            if (var1.IsFirstOrder)
                return BasicAutomata.MkEqualPositions2(pos1, pos2, alg);
            else
                return BasicAutomata.MkEqualSets(pos1, pos2, alg);
        }
    }

    /// <summary>
    /// var1 in var2
    /// </summary>
    public class MSOIn<T> : MSOBinaryAtom<T>
    {
        public MSOIn(Variable var1, Variable var2)
            : base(var1, var2)
        {
            if (!var1.IsFirstOrder)
                throw new ArgumentException("variable type not first-order", "var1");
            if (var2.IsFirstOrder)
                throw new ArgumentException("variable type not second-order", "var2");
        }

        public override WS1SFormula<T> ToWS1S()
        {
            throw new NotImplementedException();
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

        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            var pos1 = variables.IndexOf(var1);
            var pos2 = variables.IndexOf(var2);

            if (pos1 < 0)
                throw new ArgumentOutOfRangeException("variables",string.Format("does not contain {0}",var1));
            if (pos2 < 0)
                throw new ArgumentOutOfRangeException("variables", string.Format("does not contain {0}", var2));

            var aut = BasicAutomata.MkIn2(pos1 + nrOfLabelBits, pos2 + nrOfLabelBits, alg);
            return aut;
        }
    }

    /// <summary>
    /// var1 = min var2
    /// </summary>
    public class MSOMin<T> : MSOBinaryAtom<T>
    {
        public MSOMin(Variable var1, Variable var2)
            : base(var1, var2)
        {
            if (!var1.IsFirstOrder)
                throw new ArgumentException("variable type not first-order", "var1");
            if (var2.IsFirstOrder)
                throw new ArgumentException("variable type not second-order", "var2");
        }

        public override WS1SFormula<T> ToWS1S()
        {
            throw new NotImplementedException();
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

        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            var pos1 = variables.IndexOf(var1);
            var pos2 = variables.IndexOf(var2);

            if (pos1 < 0)
                throw new ArgumentOutOfRangeException("variables", string.Format("does not contain {0}", var1));
            if (pos2 < 0)
                throw new ArgumentOutOfRangeException("variables", string.Format("does not contain {0}", var2));

            var aut = BasicAutomata.MkMin2(pos1 + nrOfLabelBits, pos2 + nrOfLabelBits, alg);
            return aut;
        }
    }

    /// <summary>
    /// var1 = max var2
    /// </summary>
    public class MSOMax<T> : MSOBinaryAtom<T>
    {
        public MSOMax(Variable var1, Variable var2)
            : base(var1, var2)
        {
            if (!var1.IsFirstOrder)
                throw new ArgumentException("variable type not first-order", "var1");
            if (var2.IsFirstOrder)
                throw new ArgumentException("variable type not second-order", "var2");
        }

        public override WS1SFormula<T> ToWS1S()
        {
            throw new NotImplementedException();
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

        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            var pos1 = variables.IndexOf(var1);
            var pos2 = variables.IndexOf(var2);

            if (pos1 < 0)
                throw new ArgumentOutOfRangeException("variables", string.Format("does not contain {0}", var1));
            if (pos2 < 0)
                throw new ArgumentOutOfRangeException("variables", string.Format("does not contain {0}", var2));

            var aut = BasicAutomata.MkMax2(pos1 + nrOfLabelBits, pos2 + nrOfLabelBits, alg);
            return aut;
        }
    }

    /// <summary>
    /// phi1 &amp; phi2
    /// </summary>
    public class MSOAnd<T> : MSOBinaryOp<T>
    {
        public MSOAnd(MSOFormula<T> phi1, MSOFormula<T> phi2) : base(phi1, phi2) { }

        public override WS1SFormula<T> ToWS1S()
        {
            return new WS1SAnd<T>(phi1.ToWS1S(), phi2.ToWS1S());
        }
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

        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            var aut1 = phi1.GetAutomatonBDD(variables, alg, nrOfLabelBits);
            var aut2 = phi2.GetAutomatonBDD(variables, alg, nrOfLabelBits);
            var aut = aut1.Intersect(aut2, alg);
            aut = aut.Minimize(alg);
            return aut;
        }
    }

    /// <summary>
    /// phi1 | phi2
    /// </summary>
    public class MSOOr<T> : MSOBinaryOp<T>
    {
        public MSOOr(MSOFormula<T> phi1, MSOFormula<T> phi2) : base(phi1, phi2) { }

        public override WS1SFormula<T> ToWS1S()
        {
            return new WS1SOr<T>(phi1.ToWS1S(), phi2.ToWS1S());
        }
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

        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            var aut1 = phi1.GetAutomatonBDD(variables, alg, nrOfLabelBits);
            var aut2 = phi2.GetAutomatonBDD(variables, alg, nrOfLabelBits);
            var res = aut1.Complement(alg).Intersect(aut2.Complement(alg), alg).Complement(alg);
            res = res.Determinize(alg).Minimize(alg);
            return res;
        }
    }

    /// <summary>
    /// ~phi
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

        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits) 
        {
            var aut = phi.GetAutomatonBDD(variables, alg, nrOfLabelBits);
            var res = aut.Determinize(alg).Complement(alg).Minimize(alg);
            return res;
        }

        internal override IEnumerable<Variable> EnumerateFreeVariables(SimpleList<Variable> bound, HashSet<Variable> free)
        {
            foreach (var x in phi.EnumerateFreeVariables(bound, free))
                yield return x;
        }
    }

    /// <summary>
    /// [pred](var)
    /// </summary>
    public class MSOPredicate<T> : MSOUnaryAtom<T>
    {
        public readonly T pred;

        public MSOPredicate(T pred, Variable var)
            : base(var)
        {
            this.pred = pred;
        }

        public override WS1SFormula<T> ToWS1S()
        {
            return new WS1SPred<T>(pred, var);
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

        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            var k = variables.IndexOf(var);

            if (k < 0)
                throw new ArgumentOutOfRangeException("variables", string.Format("does not contain variable: {0}", var));

            k = k + nrOfLabelBits;

            if (var.IsFirstOrder)
            {
                return BasicAutomata.MkLabelOfPosition2(k, pred as BDD, alg);
            }
            else
            {
                return BasicAutomata.MkLabelOfSet(k, pred as BDD, alg);
            }
        }
    }

    /// <summary>
    /// var is the last position
    /// </summary>
    public class MSOLast<T> : MSOUnaryAtom<T>
    {
        public MSOLast(Variable var)
            : base(var)
        {
            if (!var.IsFirstOrder)
                throw new ArgumentException("variable type not first-order", "var1");
        }

        public override WS1SFormula<T> ToWS1S()
        {
            var y = new Variable(var.Name == "y" ? "x" : "y", true);
            return new WS1SNot<T>(new WS1SExists<T>(y, new WS1SLt<T>(var, y)));
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


        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            var pos = variables.IndexOf(var);
            if (pos < 0)
                throw new ArgumentOutOfRangeException("variables", string.Format("does not contain {0}", var));

            Automaton<BDD> aut = BasicAutomata.MkLast(pos + nrOfLabelBits, alg);
            return aut;
        }
    }

    /// <summary>
    /// The position variable var2 is the n'th successor of position variable var1
    /// </summary>
    public class MSOSuccN<T> : MSOBinaryAtom<T>
    {
        public readonly int n;

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

        public override WS1SFormula<T> ToWS1S()
        {
            return new WS1SSuccN<T>(var1, var2, n);
        }
        public override void Print(StringBuilder sb)
        {
            sb.AppendFormat("({0} = {1} + {2})", var2, var1, n);
        }

        public override MSOFormula<T> ToCore()
        {
            return this;
        }

        public override MSOFormulaKind Kind
        {
            get { return MSOFormulaKind.SuccN; }
        }

        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            var pos1 = variables.IndexOf(var1);
            var pos2 = variables.IndexOf(var2);

            if (pos1 < 0)
                throw new ArgumentException("variables", string.Format("does not contain {0}", var1));
            if (pos2 < 0)
                throw new ArgumentException("variables", string.Format("does not contain {0}", var2));

            pos1 = pos1 + nrOfLabelBits;
            pos2 = pos2 + nrOfLabelBits;

            return BasicAutomata.MkSuccN2(pos1, pos2, n, alg);
        }
    }

    /// <summary>
    /// var = N
    /// </summary>
    public class MSOeqN<T> : MSOUnaryAtom<T>
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
        }

        public override WS1SFormula<T> ToWS1S()
        {
            if (n == 0)
            {
                var y = (var.Name == "y" ? new Variable("y1", true) : new Variable("y", true));
                var var_is_0 = new WS1SNot<T>(new WS1SExists<T>(y, new WS1SLt<T>(y, var)));
                return var_is_0;
            }
            else
            {
                var y = new Variable("y", true);
                var z = (var.Name == "z" ? new Variable("z1", true) : new Variable("z", true));
                var z_is_zero = new WS1SNot<T>(new WS1SExists<T>(y, new WS1SLt<T>(y, z)));
                var var_is_N = new WS1SExists<T>(z, new WS1SAnd<T>(z_is_zero, new WS1SSuccN<T>(z, var, n)));
                return var_is_N;
            }
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

        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            var k = variables.IndexOf(var);

            if (k < 0)
                throw new ArgumentOutOfRangeException("variables", string.Format("does not contain variable: {0}", var));

            k = k + nrOfLabelBits;

            return BasicAutomata.MkEqN2(k, n, alg);
        }
    }

    /// <summary>
    /// var1 &lt; var2
    /// </summary>
    public class MSOLt<T> : MSOBinaryAtom<T>
    {
        public MSOLt(Variable var1, Variable var2)
            : base(var1, var2)
        {
            if (!var1.IsFirstOrder)
                throw new ArgumentException(string.Format("variable {0} is not first order", var1.Name), "var1");
            if (!var2.IsFirstOrder)
                throw new ArgumentException(string.Format("variable {0} is not first order", var2.Name), "var2");
        }

        public override WS1SFormula<T> ToWS1S()
        {
            return new WS1SLt<T>(var1, var2);
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

        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            var pos1 = variables.IndexOf(var1);
            var pos2 = variables.IndexOf(var2);

            if (pos1 < 0)
                throw new InvalidOperationException(string.Format("unkown variable {0}", var1));
            if (pos2 < 0)
                throw new InvalidOperationException(string.Format("unkown variable {0}", var2));

            pos1 = pos1 + nrOfLabelBits;
            pos2 = pos2 + nrOfLabelBits;

            return BasicAutomata.MkLt2(pos1, pos2, alg);
        }
    }

    /// <summary>
    /// var1 &lt;= var2
    /// </summary>
    public class MSOLe<T> : MSOBinaryAtom<T>
    {
        public MSOLe(Variable var1, Variable var2)
            : base(var1, var2)
        {
            if (!var1.IsFirstOrder)
                throw new ArgumentException(string.Format("variable {0} is not first order", var1.Name), "var1");
            if (!var2.IsFirstOrder)
                throw new ArgumentException(string.Format("variable {0} is not first order", var2.Name), "var2");
        }

        public override WS1SFormula<T> ToWS1S()
        {
            return new WS1SOr<T>(new WS1SLt<T>(var1, var2), new WS1SEq<T>(var1, var2));
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

        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            var pos1 = variables.IndexOf(var1);
            var pos2 = variables.IndexOf(var2);

            if (pos1 < 0)
                throw new InvalidOperationException(string.Format("unkown variable {0}", var1));
            if (pos2 < 0)
                throw new InvalidOperationException(string.Format("unkown variable {0}", var2));

            pos1 = pos1 + nrOfLabelBits;
            pos2 = pos2 + nrOfLabelBits;

            return BasicAutomata.MkLe2(pos1, pos2, alg);
        }
    }

    /// <summary>
    /// var1 sub var2.
    /// </summary>
    public class MSOSubset<T> : MSOBinaryAtom<T>
    {
        public MSOSubset(Variable var1, Variable var2)
            : base(var1, var2)
        {
            if (var1.IsFirstOrder)
                throw new ArgumentException(string.Format("variable {0} is not second-order", var1.Name), "var1");
            if (var2.IsFirstOrder)
                throw new ArgumentException(string.Format("variable {0} is not second-order", var2.Name), "var2");
        }

        public override WS1SFormula<T> ToWS1S()
        {
            return new WS1SSubset<T>(var1, var2);
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

        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            var pos1 = variables.IndexOf(var1);
            var pos2 = variables.IndexOf(var2);

            if (pos1 < 0)
                throw new ArgumentException("variables", string.Format("does not contain {0}", var1));
            if (pos2 < 0)
                throw new ArgumentException("variables", string.Format("does not contain {0}", var2));

            var aut = BasicAutomata.MkSubset(pos1 + nrOfLabelBits, pos2 + nrOfLabelBits, alg);
            return aut;
        }
    }

    /// <summary>
    /// ex(1|2) var : phi
    /// </summary>
    public class MSOExists<T> : MSOQuantifiedFormula<T>
    {
        public MSOExists(Variable var, MSOFormula<T> phi) : base(var, phi) { }

        public override WS1SFormula<T> ToWS1S()
        {
            return new WS1SExists<T>(var, new WS1SAnd<T>(new WS1SSingleton<T>(var), phi.ToWS1S()));
        }

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

        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            //the existential variable will shadow any previous occurrence with the same name
            //because when looking up the index of var it will be the last occurrence
            var varIndex = variables.Count + nrOfLabelBits;
            var variablesExt = variables.Append(var);
            var autPhi = phi.GetAutomatonBDD(variablesExt, alg, nrOfLabelBits);

            //Project away the the existential variable
            var newMoves = new List<Move<BDD>>();
            foreach (var move in autPhi.GetMoves())
                newMoves.Add(new Move<BDD>(move.SourceState, move.TargetState, alg.OmitBit(move.Label, varIndex)));

            var aut = Automaton<BDD>.Create(alg, autPhi.InitialState, autPhi.GetFinalStates(), newMoves);
            var res = aut.Determinize(alg);
            res = res.Minimize(alg);

            return res;
        }
    }

    public class MSOIsEmpty<T> : MSOUnaryAtom<T>
    {
        public MSOIsEmpty(Variable var)
            : base(var)
        {
            if (var.IsFirstOrder)
                throw new ArgumentException(string.Format("variable {0} is not second-order", var.Name), "var");
        }

        public override WS1SFormula<T> ToWS1S()
        {
            throw new NotImplementedException("emptyset");
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

        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfLabelBits)
        {
            var pos = GetVarIndex(var, variables) + nrOfLabelBits;
            return BasicAutomata.MkIsEmpty(pos, alg);
        }
    }
    #endregion

    #region Extended formulas
    /// <summary>
    /// phi1 =&gt; phi2
    /// </summary>
    public class MSOImplies<T> : MSOBinaryOp<T>
    {
        public MSOImplies(MSOFormula<T> phi1, MSOFormula<T> phi2) : base(phi1, phi2) { }

        public override WS1SFormula<T> ToWS1S()
        {
            return new WS1SOr<T>(new WS1SNot<T>(phi1.ToWS1S()), phi2.ToWS1S());
        }
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

        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfFreeBits)
        {
            return ToCore().GetAutomatonBDD(variables, alg, nrOfFreeBits);
        }
    }

    /// <summary>
    /// phi1 &lt;=&gt; phi2
    /// </summary>
    public class MSOEquiv<T> : MSOBinaryOp<T>
    {
        public MSOEquiv(MSOFormula<T> phi1, MSOFormula<T> phi2) : base(phi1, phi2) { }

        public override WS1SFormula<T> ToWS1S()
        {
            var psi1 = phi1.ToWS1S();
            var psi2 = phi2.ToWS1S();
            return new WS1SOr<T>(new WS1SAnd<T>(psi1, psi2), new WS1SAnd<T>(new WS1SNot<T>(psi1), new WS1SNot<T>(psi2)));
        }
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

        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfFreeBits)
        {
            return ToCore().GetAutomatonBDD(variables, alg, nrOfFreeBits);
        }
    }

    /// <summary>
    /// all(1|2) var : phi
    /// </summary>
    public class MSOForall<T> : MSOQuantifiedFormula<T>
    {
        public MSOForall(Variable var, MSOFormula<T> phi) : base(var, phi) { }

        public override WS1SFormula<T> ToWS1S()
        {
            return new WS1SNot<T>(new WS1SExists<T>(var, new WS1SAnd<T>(new WS1SSingleton<T>(var), new WS1SNot<T>(phi.ToWS1S()))));
        }
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

        internal override Automaton<BDD> GetAutomatonBDD(SimpleList<Variable> variables, IBDDAlgebra alg, int nrOfFreeBits)
        {
            return ToCore().GetAutomatonBDD(variables, alg, nrOfFreeBits);
        }
    }
    #endregion

}



