using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Automata
{
    /// <summary>
    /// Counting-set Automaton
    /// </summary>
    public class CsA<S> : IAutomaton<CsLabel<S>>
    {
        Automaton<CsLabel<S>> aut;
        public Automaton<CsLabel<S>> Automaton
        {
            get { return aut; }
        }

        public int InitialState
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IBooleanAlgebra<CsLabel<S>> Algebra
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public CsA(Automaton<CsLabel<S>> aut)
        {
            this.aut = aut;
        }

        public void ShowGraph()
        {
            //TBD
        }

        #region TBD

        public IEnumerable<Move<CsLabel<S>>> GetMoves()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<int> GetStates()
        {
            throw new NotImplementedException();
        }

        public string DescribeState(int state)
        {
            throw new NotImplementedException();
        }

        public string DescribeLabel(CsLabel<S> lab)
        {
            throw new NotImplementedException();
        }

        public string DescribeStartLabel()
        {
            throw new NotImplementedException();
        }

        public bool IsFinalState(int state)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Move<CsLabel<S>>> GetMovesFrom(int state)
        {
            throw new NotImplementedException();
        }
        #endregion
    }

    public class CsLabel<S>
    {
        S input;
        bool isFinalCondition;
        CsCondition[] conditions;
        CsUpdate[] updates;

        public bool IsFinalCondition
        {
            get { return isFinalCondition; }
        }

        public S InputGuard
        {
            get
            {
                if (isFinalCondition)
                    throw new AutomataException(AutomataExceptionKind.InvalidCall);

                return input;
            }
        }

        public CsCondition GetCondition(int counterid)
        {
            return this.conditions[counterid];
        }

        public CsUpdate GetUpdate(int counterid)
        {
            if (isFinalCondition)
                throw new AutomataException(AutomataExceptionKind.InvalidCall);

            return this.updates[counterid];
        }

        public int CsCount
        {
            get
            {
                return conditions.Length;
            }
        }

        CsLabel(bool isFinalCondition, S input, CsCondition[] conditions, CsUpdate[] updates)
        {
            this.input = input;
            this.isFinalCondition = isFinalCondition;
            this.conditions = conditions;
            this.updates = updates;
        }

        public static CsLabel<S> MkFinalCondition(CsCondition[] conditions)
        {
            return new CsLabel<S>(true, default(S), conditions, null);
        }

        public static CsLabel<S> MkTransitionLabel(S input, CsCondition[] conditions, CsUpdate[] updates)
        {
            if (conditions.Length != updates.Length)
                throw new AutomataException(AutomataExceptionKind.InvalidArguments);

            return new CsLabel<S>(false, input, conditions, updates);
        }

        public override string ToString()
        {
            if (isFinalCondition)
            {
                string s = "";
                for (int i=0; i < conditions.Length; i++)
                {
                    if (conditions[i] == CsCondition.True)
                        return "true";
                    else if (conditions[i] != CsCondition.False)
                        s += "c" + i + ":" + conditions[i].ToString();
                }
                return s;
            }
            else
            {
                string s = input.ToString() + "/" ;
                for (int i = 0; i < conditions.Length; i++)
                {
                    if (conditions[i] != CsCondition.False)
                        s += "c" + i + ":" + conditions[i].ToString() + "/" + updates[i].ToString();
                }
                return s;
            }
        }
    }

    public enum CsUpdate { Set0, Set1, Set01, Incr, IncrAdd0, IncrAdd1, IncrAdd01}

    public enum CsConditionKind { IsEmpty, IsSingleton, MaxGE, MaxLT, MaxEQ, NOT, AND, OR, TRUE, FALSE}

    /// <summary>
    /// Represents a Boolean combination of counter conditions of a single counter
    /// </summary>
    public class CsCondition
    {
        CsConditionKind kind;
        CsCondition first;
        CsCondition second;
        int bound;

        public static readonly CsCondition IsEmpty = new CsCondition(CsConditionKind.IsEmpty, null, null, -1);
        public static readonly CsCondition IsSingleton = new CsCondition(CsConditionKind.IsSingleton, null, null, -1);
        public static readonly CsCondition True = new CsCondition(CsConditionKind.TRUE, null, null, -1);
        public static readonly CsCondition False = new CsCondition(CsConditionKind.FALSE, null, null, -1);

        CsCondition(CsConditionKind kind, CsCondition first, CsCondition second, int bound)
        {
            this.kind = kind;
            this.first = first;
            this.second = second;
            this.bound = bound;
        }

        public static CsCondition MkNot(CsCondition cond)
        {
            return new CsCondition(CsConditionKind.NOT, cond, null, -1);
        }

        public static CsCondition MkAnd(CsCondition cond1, CsCondition cond2)
        {
            return new CsCondition(CsConditionKind.AND, cond1, cond2, -1);
        }

        public static CsCondition MkOr(CsCondition cond1, CsCondition cond2)
        {
            return new CsCondition(CsConditionKind.OR, cond1, cond2, -1);
        }

        public static CsCondition MkMaxEQ(int bound)
        {
            return new CsCondition(CsConditionKind.MaxEQ, null, null, bound);
        }

        public static CsCondition MkMaxLT(int bound)
        {
            return new CsCondition(CsConditionKind.MaxLT, null, null, bound);
        }

        public static CsCondition MkMaxGE(int bound)
        {
            return new CsCondition(CsConditionKind.MaxGE, null, null, bound);
        }

        public override bool Equals(object obj)
        {
            CsCondition that = obj as CsCondition;
            if (obj == null)
                return false;
            else
                return this.kind == that.kind &&
                    object.Equals(this.first, that.first) &&
                    object.Equals(this.second, that.second) &&
                    this.bound == that.bound;
        }

        public override int GetHashCode()
        {
            if (this.kind == CsConditionKind.IsEmpty || this.kind == CsConditionKind.IsSingleton || 
                this.kind == CsConditionKind.TRUE || this.kind == CsConditionKind.FALSE)
                return this.kind.GetHashCode();
            else if (this.kind == CsConditionKind.MaxEQ || this.kind == CsConditionKind.MaxLT || this.kind == CsConditionKind.MaxGE)
                return this.kind.GetHashCode() ^ this.bound;
            else if (this.kind == CsConditionKind.NOT)
                return this.kind.GetHashCode() ^ this.first.GetHashCode();
            else
                return this.kind.GetHashCode() ^ (this.first.GetHashCode() << 1) ^ (this.second.GetHashCode() << 2);
        }

        public override string ToString()
        {
            switch (kind)
            {
                case CsConditionKind.IsEmpty:
                    return "IsEmpty";
                case CsConditionKind.IsSingleton:
                    return "IsSingleton";
                case CsConditionKind.MaxEQ:
                    return "MaxEQ(" + bound + ")";
                case CsConditionKind.MaxLT:
                    return "MaxLT(" + bound + ")";
                case CsConditionKind.MaxGE:
                    return "MaxGE(" + bound + ")";
                case CsConditionKind.NOT:
                    return "~" + first.ToString();
                case CsConditionKind.AND:
                    return "(" + first.ToString() + "&" + second.ToString() + ")";
                case CsConditionKind.TRUE:
                    return "true";
                case CsConditionKind.FALSE:
                    return "false";
                default: //CsAConditionKind.OR
                    return "(" + first.ToString() + "|" + second.ToString() + ")";
            }
        }
    }
}
