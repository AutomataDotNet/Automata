using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Automata;

namespace Microsoft.Automata.MSO
{
    public abstract class MSOFormula
    {
        public abstract WS1SFormula toWS1S();

        internal abstract bool IsWellFormedFormula(List<string> fovars, List<string> sovars);

        public virtual bool IsWellFormedFormula()
        {
            return IsWellFormedFormula(new List<string>(), new List<string>());
        }

        public Automaton<BDD> GetAutomaton(CharSetSolver solver)
        {
            return toWS1S().getAutomaton(solver);
        }
    }

    public abstract class MSOBinaryPred : MSOFormula
    {
        internal MSOFormula phi1;
        internal MSOFormula phi2;

        public MSOBinaryPred(MSOFormula phi1, MSOFormula phi2)
        {
            this.phi1 = phi1;
            this.phi2 = phi2;
        }

        internal override bool IsWellFormedFormula(List<string> fovars, List<string> sovars)
        {
            return phi1.IsWellFormedFormula(fovars, sovars) && phi2.IsWellFormedFormula(fovars, sovars);
        }
    }

    public class MSOAnd : MSOBinaryPred
    {
        public MSOAnd(MSOFormula phi1, MSOFormula phi2) : base(phi1, phi2) { }

        public override WS1SFormula toWS1S()
        {
            return new WS1SAnd(phi1.toWS1S(), phi2.toWS1S());
        }
    }

    public class MSOOr : MSOBinaryPred
    {
        public MSOOr(MSOFormula phi1, MSOFormula phi2) : base(phi1, phi2) { }

        public override WS1SFormula toWS1S()
        {
            return new WS1SOr(phi1.toWS1S(), phi2.toWS1S());
        }
    }

    public class MSOIf : MSOBinaryPred
    {
        public MSOIf(MSOFormula phi1, MSOFormula phi2) : base(phi1, phi2) { }

        public override WS1SFormula toWS1S()
        {
            return new WS1SOr(new WS1SNot(phi1.toWS1S()), phi2.toWS1S());
        }
    }

    public class MSOIff : MSOBinaryPred
    {
        public MSOIff(MSOFormula phi1, MSOFormula phi2) : base(phi1, phi2) { }

        public override WS1SFormula toWS1S()
        {
            return new MSOAnd(new MSOIf(phi1, phi2), new MSOIf(phi2, phi1)).toWS1S();
        }
    }

    //Negation
    public class MSONot : MSOFormula
    {
        internal MSOFormula phi;

        public MSONot(MSOFormula phi)
        {
            this.phi = phi;
        }

        public override WS1SFormula toWS1S()
        {
            return new WS1SNot(phi.toWS1S());
        }

        internal override bool IsWellFormedFormula(List<string> fovars, List<string> sovars)
        {
            return phi.IsWellFormedFormula(fovars, sovars);
        }
    }

    //Formula with quantifiers
    public abstract class MSOQuantifiedFormula : MSOFormula
    {
        internal string var;
        internal MSOFormula phi;

        public MSOQuantifiedFormula(string var, MSOFormula phi)
        {
            this.var = var;
            this.phi = phi;
        }

        internal override abstract bool IsWellFormedFormula(List<string> fovars, List<string> sovars);
    }

    //Formula with first order quantifiers
    public abstract class MSOQuantifiedFOFormula : MSOQuantifiedFormula
    {
        public MSOQuantifiedFOFormula(string var, MSOFormula phi) : base(var, phi) { }

        internal override bool IsWellFormedFormula(List<string> fovars, List<string> sovars)
        {
            if (sovars.Contains(var))
                return false;

            var fovarsNew = new List<string>(fovars);
            fovarsNew.Insert(0, var);
            return phi.IsWellFormedFormula(fovarsNew, sovars);
        }
    }

    public class MSOExistsFo : MSOQuantifiedFOFormula
    {
        public MSOExistsFo(string var, MSOFormula phi) : base(var, phi) { }

        public override WS1SFormula toWS1S()
        {
            return new WS1SExists(var, new WS1SAnd(new WS1SSingleton(var), phi.toWS1S()));
        }
    }

    public class MSOForallFo : MSOQuantifiedFOFormula
    {
        public MSOForallFo(string var, MSOFormula phi) : base(var, phi) { }

        public override WS1SFormula toWS1S()
        {
            return new WS1SNot(new WS1SExists(var, new WS1SAnd(new WS1SSingleton(var), new WS1SNot(phi.toWS1S()))));
        }
    }

    //Formula with second order quantifiers
    public abstract class MSOQuantifiedSOFormula : MSOQuantifiedFormula
    {
        public MSOQuantifiedSOFormula(string var, MSOFormula phi) : base(var, phi) { }

        internal override bool IsWellFormedFormula(List<string> fovars, List<string> sovars)
        {
            if (fovars.Contains(var))
                return false;

            var sovarsNew = new List<string>(sovars);
            sovarsNew.Insert(0, var);
            return phi.IsWellFormedFormula(fovars, sovarsNew);
        }
    }

    public class MSOExistsSo : MSOQuantifiedSOFormula
    {
        public MSOExistsSo(string var, MSOFormula phi) : base(var, phi) { }

        public override WS1SFormula toWS1S()
        {
            return new WS1SExists(var, phi.toWS1S());
        }
    }

    public class MSOForallSo : MSOQuantifiedSOFormula
    {
        public MSOForallSo(string var, MSOFormula phi) : base(var, phi) { }

        public override WS1SFormula toWS1S()
        {
            return new WS1SNot(new WS1SExists(var, new WS1SNot(phi.toWS1S())));
        }
    }

    //Formulas over 2 positions
    public abstract class MSOBinaryPosFormula : MSOFormula
    {
        internal string var1;
        internal string var2;

        public MSOBinaryPosFormula(string var1, string var2)
        {
            this.var1 = var1;
            this.var2 = var2;
        }

        internal override bool IsWellFormedFormula(List<string> fovars, List<string> sovars)
        {
            return fovars.Contains(var1) && fovars.Contains(var2);
        }
    }

    public class MSOSucc : MSOBinaryPosFormula
    {
        public MSOSucc(string var1, string var2) : base(var1, var2) { }

        public override WS1SFormula toWS1S()
        {
            return new WS1SSucc(var1, var2);
        }
    }

    public class MSOEq : MSOBinaryPosFormula
    {
        public MSOEq(string var1, string var2) : base(var1, var2) { }

        public override WS1SFormula toWS1S()
        {
            return new WS1SEq(var2, var1);
        }
    }

    public class MSOLt : MSOBinaryPosFormula
    {
        public MSOLt(string var1, string var2) : base(var1, var2) { }

        public override WS1SFormula toWS1S()
        {
            return new WS1SLt(var1, var2);
        }
    }

    public class MSOLe : MSOBinaryPosFormula
    {
        public MSOLe(string var1, string var2) : base(var1, var2) { }

        public override WS1SFormula toWS1S()
        {
            return new WS1SNot(new WS1SLt(var2, var1));
        }
    }

    //Formulas over 1 position
    public abstract class MSOUnaryPosFormula : MSOFormula
    {
        internal string var;

        public MSOUnaryPosFormula(string var)
        {
            this.var = var;
        }

        internal override bool IsWellFormedFormula(List<string> fovars, List<string> sovars)
        {
            return fovars.Contains(var);
        }
    }

    public class MSOFirst : MSOUnaryPosFormula
    {
        public MSOFirst(string var) : base(var) { }

        public override WS1SFormula toWS1S()
        {
            return new MSOForallFo("_x_", new MSOLe(var, "_x_")).toWS1S();
        }
    }

    public class MSOLast : MSOUnaryPosFormula
    {
        public MSOLast(string var) : base(var) { }

        public override WS1SFormula toWS1S()
        {
            return new MSOForallFo("_x_", new MSOLe("_x_", var)).toWS1S();
        }
    }

    public abstract class MSOBinarySetFormula : MSOFormula
    {
        internal string var1;
        internal string var2;

        public MSOBinarySetFormula(string var1, string var2)
        {
            this.var1 = var1;
            this.var2 = var2;
        }

        internal override bool IsWellFormedFormula(List<string> fovars, List<string> sovars)
        {
            return sovars.Contains(var1) && sovars.Contains(var2);
        }
    }

    public class MSOSetEq : MSOBinarySetFormula
    {
        public MSOSetEq(string var1, string var2) : base(var1, var2) { }

        public override WS1SFormula toWS1S()
        {
            return new WS1SEq(var2, var1);
        }
    }

    public class MSOSubset : MSOBinarySetFormula
    {
        public MSOSubset(string var1, string var2) : base(var1, var2) { }

        public override WS1SFormula toWS1S()
        {
            return new WS1SSubset(var1, var2);
        }
    }

    public class MSOSubsetStrict : MSOBinarySetFormula
    {
        public MSOSubsetStrict(string var1, string var2) : base(var1, var2) { }

        public override WS1SFormula toWS1S()
        {
            return new WS1SAnd(new WS1SSubset(var1, var2), new WS1SNot(new WS1SSubset(var2, var1)));
        }
    }

    public class MSOBelong : MSOFormula
    {
        internal string pos;
        internal string set;

        public MSOBelong(string pos, string set)
        {
            this.pos = pos;
            this.set = set;
        }

        public override WS1SFormula toWS1S()
        {
            return new WS1SAnd(new WS1SSingleton(pos), new WS1SSubset(pos, set));
        }

        internal override bool IsWellFormedFormula(List<string> fovars, List<string> sovars)
        {
            return fovars.Contains(pos) && sovars.Contains(set);
        }
    }

    public class MSOPredicate : MSOFormula
    {
        internal BDD pred;
        internal string var;

        public MSOPredicate(BDD pred, string var)
        {
            this.pred = pred;
            this.var = var;
        }

        public override WS1SFormula toWS1S()
        {
            return new WS1SPred(pred, var);
        }

        internal override bool IsWellFormedFormula(List<string> fovars, List<string> sovars)
        {
            return fovars.Contains(var);
        }
    }

    public class MSOTrue : MSOFormula
    {
        public MSOTrue() { }

        public override WS1SFormula toWS1S()
        {
            return new WS1STrue();
        }

        internal override bool IsWellFormedFormula(List<string> fovars, List<string> sovars)
        {
            return true;
        }
    }

    public class MSOFalse : MSOFormula
    {
        public MSOFalse() { }

        public override WS1SFormula toWS1S()
        {
            return new WS1SNot(new WS1STrue());
        }

        internal override bool IsWellFormedFormula(List<string> fovars, List<string> sovars)
        {
            return true;
        }
    }
}

