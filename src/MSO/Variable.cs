using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Automata;

namespace Microsoft.Automata.MSO
{
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
}
