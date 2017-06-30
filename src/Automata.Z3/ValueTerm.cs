using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Automata;
using Microsoft.Automata.Z3.Internal;
using Microsoft.Z3;

//using SP = System.Tuple<int, int>;

namespace Microsoft.Automata.Z3
{
  /// <summary>
  /// Represents a concrete Z3 value term. 
  /// Value terms represent concrete values that are returned 
  /// by GetModel.
  /// </summary>
  internal class ValueExpr : IValue<Expr>
  {
    Z3Provider z3p;
    Expr value;

    /// <summary>
    /// Concrete Z3 value term
    /// </summary>
    public Expr Value
    {
      get { return value; }
    }

    /// <summary>
    /// Returns true if Value is a numeral and outputs the numeral in n.
    /// Returns false otherwise and sets n to 0.
    /// </summary>
    public bool TryGetNumeralValue(out int n)
    {
      if (value.ASTKind == Z3_ast_kind.Z3_NUMERAL_AST)
      {
        n = (int)z3p.GetNumeralUInt(value);
        return true;
      }
      else
      {
        n = 0;
        return false;
      }
    }

    /// <summary>
    /// If Value is a list of concrete characters returns the corresponding string.
    /// </summary>
    public string GetStringValue(bool unicode)
    {
        if (unicode)
            return GetUnicodeString(value);
        else
            return GetString(value);
    }
    string GetString(Expr t)
    {
      Expr[] args = z3p.GetAppArgs(t);
      if (args.Length == 0)
        return ""; //TBD: either the empty string or uspecified rest
      char c = GetCharValue(args[0]);
      string rest = GetString(args[1]);
      return c + rest;
    }

     /// <summary>
     /// Returns the value as a list of values. Assumes that 
     /// the value term is a list of values.
     /// </summary>
    public List<Expr> GetList()
    {
        var res = new List<Expr>();
        var t = Value;
        Expr[] args = z3p.GetAppArgs(t); 
        while (args.Length > 0)
        {
            res.Add(args[0]);
            args = z3p.GetAppArgs(args[1]);
        }
        return res;
    }
    string GetUnicodeString(Expr t)
    {
        // just like GetStringValue, but extract
        // code points above 65536 to proper
        // UTF-16 rather than truncate to single
        // chars.
        Expr[] args = z3p.GetAppArgs(t);
        if (args.Length == 0)
            return "";
        if (args[0].ASTKind != Z3_ast_kind.Z3_NUMERAL_AST) {
            throw new ArgumentException();
            // return "[???]";
        }

        string im;
        if (z3p.GetSort(args[0]).Equals(z3p.IntSort))
        {
            // TODO: Hard-coded limit for POPL experiment;
            // just let it throw exception.
            var x = z3p.GetNumeralInt(args[0]);
            if (!(0 <= x && x <= 1100000)) {
                x = 97;
            }
            im = Char.ConvertFromUtf32(x);
        }
        else
        {
            im = Char.ConvertFromUtf32((int)z3p.GetNumeralUInt(args[0]));
        }
        return im + GetUnicodeString(args[1]);
    }

    char GetCharValue(Expr t)
    {
      if (t.ASTKind != Z3_ast_kind.Z3_NUMERAL_AST)
        return '?';          //? unsure case
      char b;
      if (z3p.GetSort(t).Equals(z3p.IntSort))
        b = (char)z3p.GetNumeralInt(t);
      else
        b = (char)z3p.GetNumeralUInt(t);
      return b;
    }

    internal ValueExpr(Z3Provider z3p, Expr t)
    {
      this.z3p = z3p;
      this.value = t;
    }

    public override string ToString()
    {
        return z3p.PrettyPrint(value);
        //return new ExprPrettyPrinter(z3p).DescribeExpr(value);
    }

    #region IValue<Expr> Members

    /// <summary>
    /// Gets the string value. Returns null when a string value does not exist.
    /// </summary>
    public string StringValue
    {
        get { return GetStringValue(false); }
    }

    #endregion
  }
}
