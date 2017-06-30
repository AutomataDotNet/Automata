using System;
using System.Collections.Generic;

namespace Microsoft.Automata
{
    /// <summary>
    /// Context interface.
    /// </summary>
    /// <typeparam name="FUNC">function declarations, each function declaration has domain and range sorts</typeparam>
    /// <typeparam name="TERM">terms, each term has a fixed sort</typeparam>
    /// <typeparam name="SORT">sorts correspond to different subuniverses of elements</typeparam>
    public interface IContext<FUNC, TERM, SORT> : IContextCore<TERM>
    {
        #region Basic sorts
        /// <summary>
        /// Sort of Boolean values
        /// </summary>
        SORT BoolSort { get; }

        /// <summary>
        /// Sort of integers
        /// </summary>
        SORT IntSort { get; }

        /// <summary>
        /// Sort of a fixed singleton domain containing the element UnitConst.
        /// </summary>
        SORT UnitSort { get; }

        /// <summary>
        /// The single fixed element of sort UnitSort.
        /// </summary>
        TERM UnitConst { get; }

        /// <summary>
        /// Sort of bit-vectors.
        /// </summary>
        /// <param name="bits">nr of bits in the bit-vectors</param>
        SORT MkBitVecSort(uint bits);

        /// <summary>
        /// Get the sort of a given term.
        /// </summary>
        SORT GetSort(TERM term);

        /// <summary>
        /// Make a variable with the given index and sort
        /// </summary>
        TERM MkVar(uint index, SORT sort);

        /// <summary>
        /// Returns true if the given term is a variable.
        /// </summary>
        /// <param name="v">given term</param>
        bool IsVar(TERM v);

        /// <summary>
        /// Gets the index of the variable v.
        /// </summary>
        uint GetVarIndex(TERM v);

        /// <summary>
        /// Declares an uninterpreted constant with the given name and sort.
        /// </summary>
        TERM MkConst(string name, SORT sort);

        /// <summary>
        /// Make a fresh uninterpreted constant (nullary function symbol) with a given prefix of the name and sort.
        /// </summary>
        /// <param name="prefix">prefix of the name</param>
        /// <param name="sort">sort of the constant</param>
        /// <returns></returns>
        TERM MkFreshConst(string prefix, SORT sort);

        /// <summary>
        /// Enumerates all variables in term t without repetitions, 
        /// assumes that t is quantifier-free.
        /// </summary>
        IEnumerable<TERM> GetVars(TERM t);

        #endregion

        #region Numerals
        /// <summary>
        /// Make a concrete term corresponding to n.
        /// The sort must be integer or bitvector.
        /// </summary>
        TERM MkNumeral(int n, SORT sort);

        /// <summary>
        /// Make a concrete term corresponding to n.
        /// The sort must be integer or bitvector.
        /// </summary>
        TERM MkNumeral(uint n, SORT sort);

        /// <summary>
        /// Get the unsigned integer value represented by the concrete term t.
        /// </summary>
        /// <param name="t">must be a concrete numeric term</param>
        uint GetNumeralUInt(TERM t);


        /// <summary>
        /// Get the signed integer value represented by the concrete term t.
        /// </summary>
        /// <param name="t">must be a concrete numeric term</param>
        int GetNumeralInt(TERM t);


        TERM MkHexProj(int n, TERM c);

        TERM MkHexProj(int n, TERM c, SORT resSort);

        /// <summary>
        /// Convert bv to the given sort (by appending 0's at the beginning when the given sort has more bits).
        /// The sort of bv and s must be bit-vector sorts.
        /// </summary>
        TERM ConvertBitVector(TERM bv, SORT s);

        /// <summary>
        /// Extract the sub-bitvector from bv.
        /// </summary>
        TERM MkBvExtract(uint high, uint low, TERM bv);

        #endregion

        #region regex converter

        /// <summary>
        /// Gets the .NET regex converter.
        /// </summary>
        IRegexConverter<TERM> RegexConverter { get; }

        #endregion

        #region chooser
        Chooser Chooser { get; }
        #endregion

        #region bitvectors
        /// <summary>
        /// Bitwise and of bitvectors
        /// </summary>
        TERM MkBvAnd(TERM bv1, TERM bv2);

        /// <summary>
        /// Bitwise or of bitvectors
        /// </summary>
        TERM MkBvOr(TERM bv1, TERM bv2);

        /// <summary>
        /// Shift right
        /// </summary>
        TERM MkBvShiftRight(uint i, TERM bv);

        /// <summary>
        /// Shift left.
        /// </summary>
        TERM MkBvShiftLeft(uint i, TERM bv);

        TERM MkBvAdd(TERM bv1, TERM bv2);

        TERM MkBvMul(TERM bv1, TERM bv2);

        TERM MkBvSub(TERM bv1, TERM bv2);
        #endregion

        #region function symbols

        /// <summary>
        /// Get the argument sorts of a function symbol.
        /// </summary>
        /// <param name="f">function symbol</param>
        /// <returns>array of length n of argment sorts where n is the arity of f</returns>
        SORT[] GetDomain(FUNC f);

        /// <summary>
        /// Get the range sort of a function symbol.
        /// </summary>
        /// <param name="f">function symbol</param>
        /// <returns>result or range sort of f</returns>
        SORT GetRange(FUNC f);

        /// <summary>
        /// Declare a fresh uninterpreted function symbol with given prefix f of the name, arity domSorts.Length, and of given domain and range sorts.
        /// </summary>
        /// <param name="f">prefix of the name</param>
        /// <param name="domSorts">domain (argument) sorts</param>
        /// <param name="ranSort">range (value) sort</param>
        /// <returns>uninterpreted function symbol from domSorts to ranSort</returns>
        FUNC MkFreshFuncDecl(string f, SORT[] domSorts, SORT ranSort);

        /// <summary>
        /// Make uninterpreted function declaration f: domSorts -> ranSort
        /// </summary>
        /// <param name="f">prefix of the name</param>
        /// <param name="domSorts">domain (argument) sorts</param>
        /// <param name="ranSort">range (value) sort</param>
        /// <returns>uninterpreted function symbol from domSorts to ranSort</returns>
        FUNC MkFuncDecl(string f, SORT[] domSorts, SORT ranSort);

        /// <summary>
        /// Make uninterpreted function declaration f: domain -> range
        /// </summary>
        FUNC MkFuncDecl(string f, SORT domain, SORT range);

        /// <summary>
        /// Make a term with function symbol f and arguments args
        /// </summary>
        /// <param name="f">function symbol</param>
        /// <param name="args">arguments whose sorts must match the corresponding domain sort of f</param>
        /// <returns></returns>
        TERM MkApp(FUNC f, params TERM[] args);

        /// <summary>
        /// Gets the immediate subterms of a term. Returns the empty array if t is a constant.
        /// </summary>
        TERM[] GetAppArgs(TERM t);

        /// <summary>
        /// Gets the name of the function declaration.
        /// </summary>
        string GetDeclName(FUNC f);

        /// <summary>
        /// Gets the name of the function declaration by omitting the parameter suffix if there is one.
        /// </summary>
        string GetDeclShortName(FUNC f);

        /// <summary>
        /// Gets the function declaration parameters if the name has a parameter suffix [p_1:p_2:...:p_k].
        /// </summary>
        uint[] GetDeclParameters(FUNC f);

        #endregion

        #region lists

        /// <summary>
        /// Make a list sort with the given element sort
        /// </summary>
        SORT MkListSort(SORT elemSort);

        /// <summary>
        /// Returns true if the given sort is a listsort
        /// </summary>
        /// <param name="sort">given sort</param>
        /// <returns></returns>
        bool IsListSort(SORT sort);

        /// <summary>
        /// Get the empty list of the given list sort.
        /// </summary>
        /// <param name="listSort">must be a list sort</param>
        TERM GetNil(SORT listSort);

        /// <summary>
        /// Gets the element sort of a list sort.
        /// </summary>
        /// <param name="listSort">must be a list sort</param>
        SORT GetElemSort(SORT listSort);

        /// <summary>
        /// Makes a list of the terms. 
        /// Assumes that all terms in ts have the same sort and that ts is nonempty.
        /// </summary>
        TERM MkList(params TERM[] ts);

        /// <summary>
        /// Makes a list with the given numeric element sort. 
        /// Each character of the string is considered as its numeric code and converted 
        /// into corresponding numeric term.
        /// </summary>
        TERM MkListFromString(string inputElems, SORT elemSort);

        /// <summary>
        /// Makes a list of the terms in elems (may be null or empty), 
        /// with rest as the remaining list.
        /// Assumes that all terms in elems have the same sort 
        /// and that rest is a term of the corresponding list sort.
        /// </summary>
        TERM MkListWithRest(TERM[] elems, TERM rest);

        /// <summary>
        /// Makes a list cons(first,rest). The element sort of rest must equal to the sort of first.
        /// </summary>
        /// <param name="first">first element</param>
        /// <param name="rest">rest of the list</param>
        TERM MkListCons(TERM first, TERM rest);

        /// <summary>
        /// Make a term that accesses the first element of the list.
        /// </summary>
        /// <param name="list">must be a term of List sort</param>
        /// <returns>a term of element sort of the list representing the first element of the list</returns>
        TERM MkFirstOfList(TERM list);

        /// <summary>
        /// Make a term that test that the list is nonempty.
        /// </summary>
        TERM MkIsCons(TERM list);

        /// <summary>
        /// Make a term that test that the list is empty.
        /// </summary>
        TERM MkIsNil(TERM list);

        /// <summary>
        /// Make a term that accesses the rest of the list.
        /// </summary>
        /// <param name="list">must be a term of List sort</param>
        /// <returns>a term of the same list sort representing the rest of the list</returns>
        TERM MkRestOfList(TERM list);

        #endregion

        #region tuples

        /// <summary>
        /// Makes a tuple sort with a default sort name of the given element sorts.
        /// </summary>
        /// <param name="elemSorts">nonempty array of element sorts</param>
        /// <returns>tuple sort of the given element sorts</returns>
        SORT MkTupleSort(params SORT[] elemSorts);

        /// <summary>
        /// Returns true iff the sort is a tuple sort.
        /// </summary>
        bool IsTupleSort(SORT sort);

        /// <summary>
        /// Get the number of elements in a tuple of the given tuple sort.
        /// </summary>
        /// <param name="tupleSort">must be a tuple sort</param>
        /// <returns>a positive number</returns>
        int GetTupleLength(SORT tupleSort);

        /// <summary>
        /// Get the sort of the p'th element of the tuple sort.
        /// The index p must be between 0 and GetTupleLength(tupleSort)-1.
        /// </summary>
        SORT GetTupleElementSort(SORT tupleSort, int p);

        #endregion

        #region option values

        /// <summary>
        /// Makes an option sort with the given element sort
        /// </summary>
        /// <param name="s">element sort</param>
        SORT MkOptionSort(SORT s);

        /// <summary>
        /// Gets the None value of the given option sort.
        /// </summary>
        /// <param name="s">option sort</param>
        TERM MkNone(SORT s);

        /// <summary>
        /// Gets the value sort of an option sort.
        /// </summary>
        /// <param name="s">option sort</param>
        SORT GetOptionValueSort(SORT s);

        /// <summary>
        /// Lifts the term t to Some(t) of corresponding option sort.
        /// </summary>
        /// <param name="t">given term</param>
        TERM MkSome(TERM t);

        /// <summary>
        /// Makes a predicate that tests that t is not None.
        /// </summary>
        /// <param name="t">term of option sort</param>
        TERM MkIsSome(TERM t);

        /// <summary>
        /// Makes a predicate that tests that t is None.
        /// </summary>
        /// <param name="t">term of option sort</param>
        TERM MkIsNone(TERM t);

        /// <summary>
        /// Makes a term that returns v from Some(v).
        /// </summary>
        /// <param name="t">term of option sort</param>
        TERM MkGetSomeValue(TERM t);

        /// <summary>
        /// Returns true if the given sort is an option sort
        /// </summary>
        /// <param name="s">given sort</param>
        bool IsOption(SORT s);

        #endregion

        #region characters
        /// <summary>
        /// Character term x is lexicographically greater than or equal to character term y
        /// </summary>
        TERM MkCharGe(TERM x, TERM y);

        /// <summary>
        /// Character term x is lexicographically strictly greater than character term y
        /// </summary>
        TERM MkCharGt(TERM x, TERM y);

        /// <summary>
        /// Character term x is lexicographically smaller than or equal to character term y
        /// </summary>
        TERM MkCharLe(TERM x, TERM y);

        /// <summary>
        /// Character term x is lexicographically strictly smaller than character term y
        /// </summary>
        TERM MkCharLt(TERM x, TERM y);

        /// <summary>
        /// Get the Least Significant Half-Byte (LSHB) from the character.
        /// </summary>
        TERM MkCharLSHB(TERM c);

        /// <summary>
        /// Addition
        /// </summary>
        TERM MkCharAdd(TERM x, TERM y);

        /// <summary>
        /// Extract the character value from a ground charater term t.
        /// </summary>
        /// <param name="t">must be a concrete numeric term</param>
        char GetCharValue(TERM t);

        /// <summary>
        /// Make character term from the character c
        /// </summary>
        TERM MkCharExpr(char c);

        /// <summary>
        /// Make character variable with identifier id
        /// </summary>
        TERM MkCharVar(uint id);

        /// <summary>
        /// Remainder
        /// </summary>
        /// <returns></returns>
        TERM MkCharRem(TERM x, TERM y);

        /// <summary>
        /// Divison of x by y
        /// </summary>
        /// <returns></returns>
        TERM MkCharDiv(TERM x, TERM y);

        /// <summary>
        /// Subtraction x - y
        /// </summary>
        TERM MkCharSub(TERM x, TERM y);

        /// <summary>
        /// Mutiplication x * y
        /// </summary>
        TERM MkCharMul(TERM x, TERM y);

        /// <summary>
        /// Gets the character sort corresponding to the current character encoding.
        /// </summary>
        SORT CharSort { get; }
        #endregion

        ILibrary<TERM> Library { get; }

        FUNC GetTupleConstructor(SORT tupleSort);

        TERM MkExists(TERM body, params TERM[] vars);

        TERM MkZeroExt(uint i, TERM t);
    }

    /// <summary>
    /// Provides core context operations. 
    /// Operations that do not depend on function symbol or sort types.
    /// </summary>
    public interface IContextCore<TERM> :
        ICharAlgebra<TERM>,
        IDisposable
    {
        /// <summary>
        /// Makes a tuple with a default constructor name of the terms in ts, ts must me nonempty.
        /// </summary>
        /// <param name="ts">nonempty array of terms</param>
        /// <returns>tuple of the terms in ts</returns>
        TERM MkTuple(params TERM[] ts);

        /// <summary>
        /// Make the term that projects the p'th element from tuple z
        /// </summary>
        /// <param name="p">id of the element to be projected, must be between 0 and arity(tuple)-1</param>
        /// <param name="z">the given tuple term</param>
        /// <returns></returns>
        TERM MkProj(int p, TERM z);

        /// <summary>
        /// Make the if-then-else term Ite(cond,t,f) that equals t, if cond is true; equals f, otherwise, where t and f must have the same sort and cond must have Boolean sort.
        /// </summary>
        TERM MkIte(TERM cond, TERM t, TERM f);

        /// <summary>
        /// Make an integer term whose integer value is n.
        /// </summary>
        TERM MkInt(int n);

        /// <summary>
        /// Make a fresh constant of the same sort ast the sort of t.
        /// </summary>
        TERM MkFreshConst(string prefix, TERM t);

        /// <summary>
        /// Returns true iff t is a ground term (contains no free variables).
        /// </summary>
        bool IsGround(TERM t);

        #region Equalities and disequalities

        /// <summary>
        /// Make the equality constraint t1 = t2, where t1 and t2 must have the same sort.
        /// </summary>
        TERM MkEq(TERM t1, TERM t2);

        /// <summary>
        /// Make the disequality constraint t1 != t2, where t1 and t2 must have the same sort.
        /// </summary>
        TERM MkNeq(TERM t1, TERM t2);

        #endregion

        #region Substitutions

        /// <summary>
        /// Replace all occurrences of key in t by val, where key and val must have the same sort.
        /// </summary>
        TERM ApplySubstitution(TERM t, TERM key, TERM val);

        /// <summary>
        /// Simultaneously replace all occurrences of key_i in t by val_i for i=1,2, where key_i and val_i must have the same sort.
        /// </summary>
        TERM ApplySubstitution(TERM t, TERM key_1, TERM val_1, TERM key_2, TERM val_2);

        /// <summary>
        /// Simultaneously replace all occurrences of key in t by val for all (key,val) in the dictionary, where key and val must have the same sort.
        /// </summary>
        TERM ApplySubstitution(TERM t, IDictionary<TERM, TERM> substitution);

        #endregion

        #region Negation Normal Form

        /// <summary>
        /// Converts all the Boolean subterms in t to an equivalent predicates in Negation Normal Form.
        /// </summary>
        TERM ToNNF(TERM t);

        #endregion

        #region Axioms

        /// <summary>
        /// Makes the axiom 'Forall vars (lhs[vars] = rhs[vars])'.
        /// Assumes that lhs and rhs have the same sort.
        /// Assumes that all variables in rhs occur in vars.
        /// Assumes that the function symbol of lhs is an uninterpreted function symbol.
        /// </summary>
        /// <param name="lhs">term whose function symbol is uninterpreted</param>
        /// <param name="rhs">term that defines the meaning of lhs</param>
        /// <param name="vars">a sequence of distinct variables</param>
        TERM MkAxiom(TERM lhs, TERM rhs, params TERM[] vars);

        #endregion

        ISolver<TERM> MkSolver(string logic = null);

        ISolver<TERM> MainSolver { get; }

    }
}
