using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Automata
{
    public class ProductBoolAlg<S, T> : IBoolAlgMinterm<Table<S, T>>
    {
        IBooleanAlgebra<S> first;
        IBooleanAlgebra<T> second;
        Table<S, T> bot;
        Table<S, T> top;
        MintermGenerator<Table<S, T>> mtg;

        public IBooleanAlgebra<S> First
        {
            get { return first; }
        }

        public IBooleanAlgebra<T> Second
        {
            get { return second; }
        }

        public ProductBoolAlg(IBooleanAlgebra<S> first, IBooleanAlgebra<T> second)
        {
            this.first = first;
            this.second = second;
            this.mtg = new MintermGenerator<Table<S, T>>(this);
            this.bot = new Table<S, T>(this);
            this.top = new Table<S, T>(this, new Pair<S, T>(first.True, second.True));
        }

        public Table<S, T> True
        {
            get { return top; }
        }

        public Table<S, T> False
        {
            get { return bot; }
        }

        public IEnumerable<Pair<bool[], Table<S, T>>> GenerateMinterms(params Table<S, T>[] predicates)
        {
            return mtg.GenerateMinterms(predicates);
        }

        public Table<S, T> MkOr(IEnumerable<Table<S, T>> tables)
        {
            var rows = new HashSet<Pair<S,T>>();
            foreach (var table in tables)
                rows.UnionWith(table.Rows);

            var newRows = new HashSet<Pair<S,T>>();
            bool fstTrue = false;
            bool sndTrue = false;
            var newRowFirstTrue = new Pair<S, T>(first.True, second.False);
            var newRowSecondTrue = new Pair<S, T>(first.False,second.True);
            foreach(var row in rows)
                if (row.First.Equals(first.True))
                {
                    newRowFirstTrue = new Pair<S, T>(first.True, second.MkOr(row.Second, newRowFirstTrue.Second));
                    fstTrue = true;
                }
                else
                {
                    if (row.Second.Equals(second.True))
                    {
                        newRowSecondTrue = new Pair<S, T>(first.MkOr(row.First, newRowSecondTrue.First), second.True);
                        sndTrue = true;
                    }
                    else
                        newRows.Add(row);
                }
            if (fstTrue)
                newRows.Add(newRowFirstTrue);
            if (sndTrue)
                newRows.Add(newRowSecondTrue);

            return MkTable_(newRows);
        }

        public Table<S, T> MkAnd(IEnumerable<Table<S, T>> tables)
        {
            var res = True;
            foreach (var table in tables)
                res = MkAnd(res, table);
            return res;
        }

        public Table<S, T> MkAnd(params Table<S, T>[] tables)
        {
            var res = True;
            foreach (var table in tables)
                res = MkAnd(res, table);
            return res;
        }

        public Table<S, T> MkNot(Table<S, T> table)
        {
            var res = True;
            foreach (var row in table.Rows)
            {
                HashSet<Pair<S, T>> rows = new HashSet<Pair<S, T>>();
                var phi = first.MkNot(row.First);
                if (!phi.Equals(first.False))
                    rows.Add(new Pair<S, T>(phi, second.True));
                var psi = second.MkNot(row.Second);
                if (!psi.Equals(second.False))
                    rows.Add(new Pair<S, T>(first.True, psi));
                var row_c = MkTable(rows);
                res = MkAnd(res, row_c);
                if (res.IsEmpty)
                    return False;
            }
            return res.Simplify();
        }

        /// <summary>
        /// Complement the pair predicate by complementing its components.
        /// </summary>
        public Table<S, T> Simplify(Table<S, T> predicate)
        {
            return predicate.Simplify();
        }

        public bool AreEquivalent(Table<S, T> table1, Table<S, T> table2)
        {
            var table1_and_not_table2 = MkAnd(table1, MkNot(table2));
            if (IsSatisfiable(table1_and_not_table2))
                return false;
            var table2_and_not_table1 = MkAnd(table2, MkNot(table1));
            if (IsSatisfiable(table2_and_not_table1))
                return false;

            return true;
        }

        public Table<S, T> MkOr(Table<S, T> table1, Table<S, T> table2)
        {
            var elems = new HashSet<Pair<S, T>>(table1.Rows);
            elems.UnionWith(table2.Rows);
            var table = MkTable_(elems);
            return table;
        }

        public Table<S, T> MkAnd(Table<S, T> table1, Table<S, T> table2)
        {
            var rowSet = new HashSet<Pair<S, T>>();
            foreach (var row1 in table1.Rows)
                foreach (var row2 in table2.Rows)
                {
                    var phi = first.MkAnd(row1.First, row2.First);
                    if (!phi.Equals(first.False)) //else unsat is trivial
                    {
                        var psi = second.MkAnd(row1.Second, row2.Second);
                        if (phi.Equals(first.True) && psi.Equals(second.True))
                            return True; //collapses to true
                        if (!phi.Equals(second.False)) //else unsat is trivial
                            rowSet.Add(new Pair<S, T>(phi, psi));
                    }
                }
            return MkTable_(rowSet).Simplify();
        }

        public bool IsSatisfiable(Table<S, T> predicate)
        {
            var e = predicate.Rows.GetEnumerator();
            while (e.MoveNext())
                if (first.IsSatisfiable(e.Current.First) && second.IsSatisfiable(e.Current.Second))
                    return true;
            return false;
        }

        public Table<S, T> MkTable(Pair<S, T> row)
        {
            var rowSet = new HashSet<Pair<S, T>>(new Pair<S, T>[]{row});
            return MkTable_(rowSet);
        }

        public Table<S, T> MkTable(IEnumerable<Pair<S, T>> rows)
        {
            var rowSet = new HashSet<Pair<S, T>>(rows);
            return MkTable_(rowSet);
        }

        Table<S, T> MkTable_(HashSet<Pair<S, T>> rows)
        {
            if (rows.Count == 0)
                return False;
            else
                return new Table<S, T>(this, rows);
        }
    }

    public class Table<S, T>
    {
        List<Pair<S, T>> rows;
        ProductBoolAlg<S, T> alg;
        internal Table(ProductBoolAlg<S, T> alg, IEnumerable<Pair<S, T>> rows)
        {
            this.rows = new List<Pair<S, T>>(rows);
            this.alg = alg;
        }
        internal Table(ProductBoolAlg<S, T> alg, params Pair<S, T>[] rows)
        {
            this.rows = new List<Pair<S, T>>(rows);
            this.alg = alg;
        }
        public bool IsEmpty
        {
            get { return rows.Count == 0; }
        }
        public List<Pair<S, T>> Rows
        {
            get { return rows; }
        }

        public S ProjectFirstColumn()
        {
            var res = alg.First.MkOr(EnumerateFirstColumn());
            return res;
        }

        public T ProjectSecondColumn()
        {
            var res = alg.Second.MkOr(EnumerateSecondColumn());
            return res;
        }

        private IEnumerable<S> EnumerateFirstColumn()
        {
            foreach (var row in rows)
                yield return row.First;
        }

        private IEnumerable<T> EnumerateSecondColumn()
        {
            foreach (var row in rows)
                yield return row.Second;
        }

        public Table<S, T> Simplify()
        {
            var newRows = new List<Pair<S, T>>();
            foreach (var r in rows)
                newRows.Add(new Pair<S, T>(alg.First.Simplify(r.First), alg.Second.Simplify(r.Second)));
            return new Table<S,T>(alg,newRows);
        }
    }
}
