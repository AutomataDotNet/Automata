using System;
using System.Collections.Generic;
using Microsoft.Automata.Internal.Generated;

namespace Microsoft.Automata.Internal
{
    internal class UnicodeCategoryToCharSetProvider : IUnicodeCategoryTheory<BDD>
    {
        CharSetSolver solver;
        BDD[] catConditions = new BDD[30];
        BDD whiteSpaceCondition = null;
        BDD wordLetterCondition = null;

        public UnicodeCategoryToCharSetProvider(CharSetSolver solver)
        {
            this.solver = solver;
            InitializeUnicodeCategoryDefinitions();
        }

        private void InitializeUnicodeCategoryDefinitions()
        {
            if (solver.Encoding == BitWidth.BV7)
            {
                for (int i = 0; i < 30; i++)
                    if (UnicodeCategoryRanges.ASCIIBdd[i] == null)
                        catConditions[i] = solver.False;
                    else
                        catConditions[i] = solver.DeserializeCompact(UnicodeCategoryRanges.ASCIIBdd[i]);
                whiteSpaceCondition = solver.DeserializeCompact(UnicodeCategoryRanges.ASCIIWhitespaceBdd);
                wordLetterCondition = solver.DeserializeCompact(UnicodeCategoryRanges.ASCIIWordCharacterBdd);
            }
            else if (solver.Encoding == BitWidth.BV8)
            {
                for (int i = 0; i < 30; i++)
                    if (UnicodeCategoryRanges.CP437Bdd[i] == null)
                        catConditions[i] = solver.False;
                    else
                        catConditions[i] = solver.DeserializeCompact(UnicodeCategoryRanges.CP437Bdd[i]);
                whiteSpaceCondition = solver.DeserializeCompact(UnicodeCategoryRanges.CP437WhitespaceBdd);
                wordLetterCondition = solver.DeserializeCompact(UnicodeCategoryRanges.CP437WordCharacterBdd);
            }
            else
            {
                for (int i = 0; i < 30; i++)
                    catConditions[i] = solver.DeserializeCompact(UnicodeCategoryRanges.UnicodeBdd[i]);
                whiteSpaceCondition = solver.DeserializeCompact(UnicodeCategoryRanges.UnicodeWhitespaceBdd);
                wordLetterCondition = solver.DeserializeCompact(UnicodeCategoryRanges.UnicodeWordCharacterBdd);
            }
        }

        #region IUnicodeCategoryTheory<BDD> Members

        public BDD CategoryCondition(int cat)
        {
            return catConditions[cat];
        }

        public BDD WhiteSpaceCondition
        {
            get { return whiteSpaceCondition; }
        }

        public BDD WordLetterCondition
        {
            get { return wordLetterCondition; }
        }

        #endregion
    }

    internal class UnicodeCategoryToRangesProvider : IUnicodeCategoryTheory<HashSet<Pair<char, char>>>
    {
        CharRangeSolver solver;
        CharSetSolver solverBDD;
        BDD[] catConditionsBDD = new BDD[30];
        BDD whiteSpaceConditionBDD = null;
        BDD wordLetterConditionBDD = null;

        HashSet<Pair<char, char>>[] catConditions = new HashSet<Pair<char, char>>[30];
        HashSet<Pair<char, char>> whiteSpaceCondition = null;
        HashSet<Pair<char, char>> wordLetterCondition = null;
        BitWidth encoding;

        public UnicodeCategoryToRangesProvider(CharRangeSolver solver)
        {
            this.solver = solver;
            this.encoding = solver.Encoding;
            this.solverBDD = new CharSetSolver(solver.Encoding);
            InitializeUnicodeCategoryDefinitions();
        }

        private void InitializeUnicodeCategoryDefinitions()
        {
            if (encoding == BitWidth.BV7)
            {
                for (int i = 0; i < 30; i++)
                    if (UnicodeCategoryRanges.ASCIIBdd[i] == null)
                        catConditionsBDD[i] = solverBDD.False;
                    else
                        catConditionsBDD[i] = solverBDD.DeserializeCompact(UnicodeCategoryRanges.ASCIIBdd[i]);
                whiteSpaceConditionBDD = solverBDD.DeserializeCompact(UnicodeCategoryRanges.ASCIIWhitespaceBdd);

                wordLetterConditionBDD = solverBDD.DeserializeCompact(UnicodeCategoryRanges.ASCIIWordCharacterBdd);
            }
            else if (encoding == BitWidth.BV8)
            {
                for (int i = 0; i < 30; i++)
                    if (UnicodeCategoryRanges.CP437Bdd[i] == null)
                        catConditionsBDD[i] = solverBDD.False;
                    else
                        catConditionsBDD[i] = solverBDD.DeserializeCompact(UnicodeCategoryRanges.CP437Bdd[i]);
                whiteSpaceConditionBDD = solverBDD.DeserializeCompact(UnicodeCategoryRanges.CP437WhitespaceBdd);
                wordLetterConditionBDD = solverBDD.DeserializeCompact(UnicodeCategoryRanges.CP437WordCharacterBdd);
            }
            else
            {
                for (int i = 0; i < 30; i++)
                    catConditionsBDD[i] = solverBDD.DeserializeCompact(UnicodeCategoryRanges.UnicodeBdd[i]);
                whiteSpaceConditionBDD = solverBDD.DeserializeCompact(UnicodeCategoryRanges.UnicodeWhitespaceBdd);
                wordLetterConditionBDD = solverBDD.DeserializeCompact(UnicodeCategoryRanges.UnicodeWordCharacterBdd);
            }
            #region create corresponding ranges
            for (int i = 0; i < 30; i++)
            {
                var ranges = solverBDD.ToRanges(catConditionsBDD[i]);
                catConditions[i] = new HashSet<Pair<char, char>>();
                foreach (var range in ranges)
                    catConditions[i].Add(new Pair<char, char>((char)range.First, (char)range.Second));
            }

            var ranges1 = solverBDD.ToRanges(whiteSpaceConditionBDD);
            whiteSpaceCondition = new HashSet<Pair<char, char>>();
            foreach (var range in ranges1)
                whiteSpaceCondition.Add(new Pair<char, char>((char)range.First, (char)range.Second));

            ranges1 = solverBDD.ToRanges(wordLetterConditionBDD); //new Utilities.UnicodeCategoryRangesGenerator.Ranges();
            wordLetterCondition = new HashSet<Pair<char, char>>();
            foreach (var range in ranges1)
                wordLetterCondition.Add(new Pair<char, char>((char)range.First, (char)range.Second));
            #endregion
        }

        #region IUnicodeCategoryTheory<HashSet<Pair<char,char>>> Members

        public HashSet<Pair<char, char>> CategoryCondition(int cat)
        {
            return catConditions[cat];
        }

        public HashSet<Pair<char, char>> WhiteSpaceCondition
        {
            get { return whiteSpaceCondition; }
        }

        public HashSet<Pair<char, char>> WordLetterCondition
        {
            get { return wordLetterCondition; }
        }

        #endregion
    }

    internal class UnicodeCategoryToHashSetProvider : IUnicodeCategoryTheory<HashSet<char>>
    {
        HashSetSolver solver;
        CharSetSolver solverBDD;
        BDD[] catConditionsBDD = new BDD[30];
        BDD whiteSpaceConditionBDD = null;
        BDD wordLetterConditionBDD = null;

        HashSet<char>[] catConditions = new HashSet<char>[30];
        HashSet<char> whiteSpaceCondition = null;
        HashSet<char> wordLetterCondition = null;
        BitWidth encoding;

        public UnicodeCategoryToHashSetProvider(HashSetSolver solver)
        {
            this.solver = solver;
            this.encoding = solver.Encoding;
            this.solverBDD = new CharSetSolver(solver.Encoding);
            InitializeUnicodeCategoryDefinitions();
        }

        private void InitializeUnicodeCategoryDefinitions()
        {
            if (encoding == BitWidth.BV7)
            {
                for (int i = 0; i < 30; i++)
                    if (UnicodeCategoryRanges.ASCIIBdd[i] == null)
                        catConditionsBDD[i] = solverBDD.False;
                    else
                        catConditionsBDD[i] = solverBDD.DeserializeCompact(UnicodeCategoryRanges.ASCIIBdd[i]);
                whiteSpaceConditionBDD = solverBDD.DeserializeCompact(UnicodeCategoryRanges.ASCIIWhitespaceBdd);

                wordLetterConditionBDD = solverBDD.DeserializeCompact(UnicodeCategoryRanges.ASCIIWordCharacterBdd);
            }
            else if (encoding == BitWidth.BV8)
            {
                for (int i = 0; i < 30; i++)
                    if (UnicodeCategoryRanges.CP437Bdd[i] == null)
                        catConditionsBDD[i] = solverBDD.False;
                    else
                        catConditionsBDD[i] = solverBDD.DeserializeCompact(UnicodeCategoryRanges.CP437Bdd[i]);
                whiteSpaceConditionBDD = solverBDD.DeserializeCompact(UnicodeCategoryRanges.CP437WhitespaceBdd);
                wordLetterConditionBDD = solverBDD.DeserializeCompact(UnicodeCategoryRanges.CP437WordCharacterBdd);
            }
            else
            {
                for (int i = 0; i < 30; i++)
                    catConditionsBDD[i] = solverBDD.DeserializeCompact(UnicodeCategoryRanges.UnicodeBdd[i]);
                whiteSpaceConditionBDD = solverBDD.DeserializeCompact(UnicodeCategoryRanges.UnicodeWhitespaceBdd);
                wordLetterConditionBDD = solverBDD.DeserializeCompact(UnicodeCategoryRanges.UnicodeWordCharacterBdd);
            }
            #region create corresponding ranges
            for (int i = 0; i < 30; i++)
                catConditions[i] = new HashSet<char>(solverBDD.GenerateAllCharacters(catConditionsBDD[i], false));
            whiteSpaceCondition = new HashSet<char>(solverBDD.GenerateAllCharacters(whiteSpaceConditionBDD, false));
            wordLetterCondition = new HashSet<char>(solverBDD.GenerateAllCharacters(wordLetterConditionBDD, false));
            #endregion
        }

        #region IUnicodeCategoryTheory<HashSet<Pair<char,char>>> Members

        public HashSet<char> CategoryCondition(int cat)
        {
            return catConditions[cat];
        }

        public HashSet<char> WhiteSpaceCondition
        {
            get { return whiteSpaceCondition; }
        }

        public HashSet<char> WordLetterCondition
        {
            get { return wordLetterCondition; }
        }

        #endregion
    }
}
