using System;
using System.Collections.Generic;

using Microsoft.Automata;

namespace Microsoft.Automata.MSO
{
    public abstract partial class MSOFormula<T>
    {
        public MSOFormula<T> SubstituteVariables(Dictionary<string, Variable> substitution)
        {
            return SubstituteVariables1(substitution, SimpleList<string>.Empty);
        }

        internal abstract MSOFormula<T> SubstituteVariables1(Dictionary<string, Variable> subst, SimpleList<string> bound);
    }

    public partial class MSOTrue<T>
    {
        internal override MSOFormula<T> SubstituteVariables1(Dictionary<string, Variable> subst, SimpleList<string> bound)
        {
            return this;
        }
    }

    public partial class MSOFalse<T>
    {
        internal override MSOFormula<T> SubstituteVariables1(Dictionary<string, Variable> subst, SimpleList<string> bound)
        {
            return this;
        }
    }

    public partial class MSOEq<T>
    {
        internal override MSOFormula<T> SubstituteVariables1(Dictionary<string, Variable> subst, SimpleList<string> bound)
        {
            var v1 = (bound.Contains(var1.Name) ? var1 : (subst.ContainsKey(var1.Name) ? subst[var1.Name] : var1));
            var v2 = (bound.Contains(var2.Name) ? var2 : (subst.ContainsKey(var2.Name) ? subst[var2.Name] : var2));
            if (v1 == var1 && v2 == var2)
                return this;
            else
            {
                if (v1.IsFirstOrder != var1.IsFirstOrder || v2.IsFirstOrder != var2.IsFirstOrder)
                    throw new ArgumentException("invalid substitution");
                return new MSOEq<T>(v1, v2);
            }
        }
    }

    public partial class MSOIn<T>
    {
        internal override MSOFormula<T> SubstituteVariables1(Dictionary<string, Variable> subst, SimpleList<string> bound)
        {
            var v1 = (bound.Contains(var1.Name) ? var1 : (subst.ContainsKey(var1.Name) ? subst[var1.Name] : var1));
            var v2 = (bound.Contains(var2.Name) ? var2 : (subst.ContainsKey(var2.Name) ? subst[var2.Name] : var2));
            if (v1 == var1 && v2 == var2)
                return this;
            else
            {
                if (v1.IsFirstOrder != var1.IsFirstOrder || v2.IsFirstOrder != var2.IsFirstOrder)
                    throw new ArgumentException("invalid substitution");
                return new MSOIn<T>(v1, v2);
            }
        }
    }

    public partial class MSOMin<T>
    {
        internal override MSOFormula<T> SubstituteVariables1(Dictionary<string, Variable> subst, SimpleList<string> bound)
        {
            var v1 = (bound.Contains(var1.Name) ? var1 : (subst.ContainsKey(var1.Name) ? subst[var1.Name] : var1));
            var v2 = (bound.Contains(var2.Name) ? var2 : (subst.ContainsKey(var2.Name) ? subst[var2.Name] : var2));
            if (v1 == var1 && v2 == var2)
                return this;
            else
            {
                if (v1.IsFirstOrder != var1.IsFirstOrder || v2.IsFirstOrder != var2.IsFirstOrder)
                    throw new ArgumentException("invalid substitution");
                return new MSOMin<T>(v1, v2);
            }
        }
    }

    public partial class MSOMax<T> 
    {
        internal override MSOFormula<T> SubstituteVariables1(Dictionary<string, Variable> subst, SimpleList<string> bound)
        {
            var v1 = (bound.Contains(var1.Name) ? var1 : (subst.ContainsKey(var1.Name) ? subst[var1.Name] : var1));
            var v2 = (bound.Contains(var2.Name) ? var2 : (subst.ContainsKey(var2.Name) ? subst[var2.Name] : var2));
            if (v1 == var1 && v2 == var2)
                return this;
            else
            {
                if (v1.IsFirstOrder != var1.IsFirstOrder || v2.IsFirstOrder != var2.IsFirstOrder)
                    throw new ArgumentException("invalid substitution");
                return new MSOMax<T>(v1, v2);
            }
        }
    }

    public partial class MSOAnd<T> 
    {
        internal override MSOFormula<T> SubstituteVariables1(Dictionary<string, Variable> subst, SimpleList<string> bound)
        {
            var psi1 = phi1.SubstituteVariables1(subst, bound);
            var psi2 = phi2.SubstituteVariables1(subst, bound);
            if (psi1 == phi1 && psi2 == phi2)
                return this;
            else
                return new MSOAnd<T>(psi1, psi2);
        }
    }

    public partial class MSOOr<T>
    {
        internal override MSOFormula<T> SubstituteVariables1(Dictionary<string, Variable> subst, SimpleList<string> bound)
        {
            var psi1 = phi1.SubstituteVariables1(subst, bound);
            var psi2 = phi2.SubstituteVariables1(subst, bound);
            if (psi1 == phi1 && psi2 == phi2)
                return this;
            else
                return new MSOOr<T>(psi1, psi2);
        }
    }

    public partial class MSONot<T>
    {
        internal override MSOFormula<T> SubstituteVariables1(Dictionary<string, Variable> subst, SimpleList<string> bound)
        {
            var psi = phi.SubstituteVariables1(subst, bound);
            if (psi == phi)
                return this;
            else
                return new MSONot<T>(psi);
        }
    }

    public partial class MSOPredicate<T> 
    {
        internal override MSOFormula<T> SubstituteVariables1(Dictionary<string, Variable> subst, SimpleList<string> bound)
        {
            var v = (bound.Contains(var.Name) ? var : (subst.ContainsKey(var.Name) ? subst[var.Name] : var));
            if (v == var)
                return this;
            else
                return new MSOPredicate<T>(pred, v);
        }
    }

    public partial class MSOLast<T> 
    {
        internal override MSOFormula<T> SubstituteVariables1(Dictionary<string, Variable> subst, SimpleList<string> bound)
        {
            var v = (bound.Contains(var.Name) ? var : (subst.ContainsKey(var.Name) ? subst[var.Name] : var));
            if (v == var)
                return this;
            else
                return new MSOLast<T>(v);
        }
    }

    public partial class MSOSuccN<T> 
    {
        internal override MSOFormula<T> SubstituteVariables1(Dictionary<string, Variable> subst, SimpleList<string> bound)
        {
            var v1 = (bound.Contains(var1.Name) ? var1 : (subst.ContainsKey(var1.Name) ? subst[var1.Name] : var1));
            var v2 = (bound.Contains(var2.Name) ? var2 : (subst.ContainsKey(var2.Name) ? subst[var2.Name] : var2));
            if (v1 == var1 && v2 == var2)
                return this;
            else
            {
                if (v1.IsFirstOrder != var1.IsFirstOrder || v2.IsFirstOrder != var2.IsFirstOrder)
                    throw new ArgumentException("invalid substitution");
                return new MSOSuccN<T>(v1, v2, N);
            }
        }
    }

    public partial class MSOeqN<T> 
    {
        internal override MSOFormula<T> SubstituteVariables1(Dictionary<string, Variable> subst, SimpleList<string> bound)
        {
            var v = (bound.Contains(var.Name) ? var : (subst.ContainsKey(var.Name) ? subst[var.Name] : var));
            if (v == var)
                return this;
            else
            {
                if (v.IsFirstOrder != var.IsFirstOrder)
                    throw new ArgumentException("invalid substitution");
                return new MSOeqN<T>(v, n);
            }
        }
    }

    public partial class MSOLt<T> 
    {
        internal override MSOFormula<T> SubstituteVariables1(Dictionary<string, Variable> subst, SimpleList<string> bound)
        {
            var v1 = (bound.Contains(var1.Name) ? var1 : (subst.ContainsKey(var1.Name) ? subst[var1.Name] : var1));
            var v2 = (bound.Contains(var2.Name) ? var2 : (subst.ContainsKey(var2.Name) ? subst[var2.Name] : var2));
            if (v1 == var1 && v2 == var2)
                return this;
            else
            {
                if (v1.IsFirstOrder != var1.IsFirstOrder || v2.IsFirstOrder != var2.IsFirstOrder)
                    throw new ArgumentException("invalid substitution");
                return new MSOLt<T>(v1, v2);
            }
        }
    }

    public partial class MSOLe<T> 
    {
        internal override MSOFormula<T> SubstituteVariables1(Dictionary<string, Variable> subst, SimpleList<string> bound)
        {
            var v1 = (bound.Contains(var1.Name) ? var1 : (subst.ContainsKey(var1.Name) ? subst[var1.Name] : var1));
            var v2 = (bound.Contains(var2.Name) ? var2 : (subst.ContainsKey(var2.Name) ? subst[var2.Name] : var2));
            if (v1 == var1 && v2 == var2)
                return this;
            else
            {
                if (v1.IsFirstOrder != var1.IsFirstOrder || v2.IsFirstOrder != var2.IsFirstOrder)
                    throw new ArgumentException("invalid substitution");
                return new MSOLe<T>(v1, v2);
            }
        }
    }

    public partial class MSOSubset<T> 
    {
        internal override MSOFormula<T> SubstituteVariables1(Dictionary<string, Variable> subst, SimpleList<string> bound)
        {
            var v1 = (bound.Contains(var1.Name) ? var1 : (subst.ContainsKey(var1.Name) ? subst[var1.Name] : var1));
            var v2 = (bound.Contains(var2.Name) ? var2 : (subst.ContainsKey(var2.Name) ? subst[var2.Name] : var2));
            if (v1 == var1 && v2 == var2)
                return this;
            else
            {
                if (v1.IsFirstOrder != var1.IsFirstOrder || v2.IsFirstOrder != var2.IsFirstOrder)
                    throw new ArgumentException("invalid substitution");
                return new MSOSubset<T>(v1, v2);
            }
        }
    }

    public partial class MSOExists<T> 
    {
        internal override MSOFormula<T> SubstituteVariables1(Dictionary<string, Variable> subst, SimpleList<string> bound)
        {
            var psi = phi.SubstituteVariables1(subst, bound.Append(var.Name));
            if (psi == phi)
                return this;
            else
                return new MSOExists<T>(var, psi);
        }
    }

    public partial class MSOIsEmpty<T> 
    {
        internal override MSOFormula<T> SubstituteVariables1(Dictionary<string, Variable> subst, SimpleList<string> bound)
        {
            var v = (bound.Contains(var.Name) ? var : (subst.ContainsKey(var.Name) ? subst[var.Name] : var));
            if (v == var)
                return this;
            else
            {
                if (v.IsFirstOrder != var.IsFirstOrder)
                    throw new ArgumentException("invalid substitution");
                return new MSOIsEmpty<T>(v);
            }
        }
    }

    public partial class MSOIsSingleton<T> 
    {
        internal override MSOFormula<T> SubstituteVariables1(Dictionary<string, Variable> subst, SimpleList<string> bound)
        {
            var v = (bound.Contains(var.Name) ? var : (subst.ContainsKey(var.Name) ? subst[var.Name] : var));
            if (v == var)
                return this;
            else
            {
                if (v.IsFirstOrder != var.IsFirstOrder)
                    throw new ArgumentException("invalid substitution");
                return new MSOIsSingleton<T>(v);
            }
        }
    }

    public partial class MSOImplies<T> 
    {
        internal override MSOFormula<T> SubstituteVariables1(Dictionary<string, Variable> subst, SimpleList<string> bound)
        {
            var psi1 = phi1.SubstituteVariables1(subst, bound);
            var psi2 = phi2.SubstituteVariables1(subst, bound);
            if (psi1 == phi1 && psi2 == phi2)
                return this;
            else
                return new MSOImplies<T>(psi1, psi2);
        }
    }

    public partial class MSOEquiv<T> 
    {
        internal override MSOFormula<T> SubstituteVariables1(Dictionary<string, Variable> subst, SimpleList<string> bound)
        {
            var psi1 = phi1.SubstituteVariables1(subst, bound);
            var psi2 = phi2.SubstituteVariables1(subst, bound);
            if (psi1 == phi1 && psi2 == phi2)
                return this;
            else
                return new MSOEquiv<T>(psi1, psi2);
        }
    }

    public partial class MSOForall<T> 
    {
        internal override MSOFormula<T> SubstituteVariables1(Dictionary<string, Variable> subst, SimpleList<string> bound)
        {
            var psi = phi.SubstituteVariables1(subst, bound.Append(var.Name));
            if (psi == phi)
                return this;
            else
                return new MSOForall<T>(var, psi);
        }
    }
}




