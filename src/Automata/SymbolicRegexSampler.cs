using System;
using System.Collections.Generic;
using Microsoft.Automata.Rex;

namespace Microsoft.Automata
{
    class SymbolicRegexSampler
    {

        // Inverse of pobability of taking a corner 
        // case (i.e. lower/upper bound) of the number
        // of iterations a loop may be unrolled.
        private int cornerCaseProb;

        // The maximum number of iterations in order to
        // collect the requested number of samples
        // (see GetDataset method)
        private int maxSamplingIter;

        private string regex;
        CharSetSolver solver;
        Random rand;
        int maxUnroll;
        SymbolicRegex<BDD> sr;
        System.Text.RegularExpressions.RegexOptions options;

        public SymbolicRegexSampler(string regex, CharSetSolver solver, int mu, int cornerCaseProb = 5, int maxSamplingIter = 3, System.Text.RegularExpressions.RegexOptions options = System.Text.RegularExpressions.RegexOptions.None)
        {
            this.options = options;
            this.cornerCaseProb = cornerCaseProb;
            this.maxSamplingIter = maxSamplingIter;
            this.maxUnroll = mu;
            this.regex = regex;
            this.sr = solver.RegexConverter.ConvertToSymbolicRegex(regex, options);
            this.solver = solver;
            rand = new Random();
        }

        public SymbolicRegexSampler(SymbolicRegex<BDD> sr, int mu, int cornerCaseProb = 5, int maxSamplingIter = 3)
        {
            this.cornerCaseProb = cornerCaseProb;
            this.maxSamplingIter = maxSamplingIter;
            this.maxUnroll = mu;
            this.regex = sr.ToStringWithAnchors();
            this.options = System.Text.RegularExpressions.RegexOptions.None;
            this.sr = sr;
            if (!(sr.Solver is CharSetSolver))
                throw new AutomataException(AutomataExceptionKind.NotSupported);

            solver = sr.Solver as CharSetSolver;
            rand = new Random();
        }

        private int SampleChildNode(int lscore, int rscore)
        {
            // Given two scores return either 0/1 with probability
            // proportional to the corresponding scores
            int coinFlip = rand.Next(lscore + rscore);
            if (coinFlip < lscore)
            {
                return 0;
            }
            return 1;
        }

        private int SampleLoopIterations(int lb, int ub)
        {
            int realUb = (maxUnroll > ub) ? ub : maxUnroll;
            int shouldTakeCornerCase = rand.Next(cornerCaseProb);

            // With probability 1/CornerCaseProb we unroll the min and max
            // number of allowed iterations. In all other cases we select
            // the number of iterations at random.
            switch (shouldTakeCornerCase)
            {
                case 0:
                    return lb;
                case 1:
                    return realUb;
            }
            return rand.Next(lb + 1, realUb);
        }

        public SymbolicRegex<BDD> UnrollRE(SymbolicRegex<BDD> re)
        {
            // Create a regular expression without loops by unrolling 
            // each loop a random number of times as dictated by the 
            // maxUnroll parameter
            SymbolicRegex<BDD> newRoot = null;
            switch (re.Kind)
            {
                case SymbolicRegexKind.Concat:
                    newRoot = solver.RegexConverter.MkConcat(UnrollRE(re.Left),
                        UnrollRE(re.Right));
                    break;
                case SymbolicRegexKind.Epsilon:
                    newRoot = re;
                    break;
                case SymbolicRegexKind.IfThenElse:
                    newRoot = solver.RegexConverter.MkIfThenElse(re.IteCond,
                        UnrollRE(re.Left), UnrollRE(re.Right));
                    break;
                case SymbolicRegexKind.Or:
                    newRoot = solver.RegexConverter.MkOr(UnrollRE(re.Left),
                        UnrollRE(re.Right));
                    break;
                case SymbolicRegexKind.Singleton:
                    newRoot = re;
                    break;
                case SymbolicRegexKind.Loop:
                    newRoot = UnrollRE(UnrollLoop(re));
                    break;
            }
            return newRoot;
        }

        private SymbolicRegex<BDD> UnrollLoop(SymbolicRegex<BDD> node)
        {
            // select the number of times the loop will be unrolled
            int times = SampleLoopIterations(node.LowerBound, node.UpperBound);
            switch (times)
            {
                case 0:
                    return solver.RegexConverter.MkEpsilon();
                case 1:
                    return node.Left;
            }
            SymbolicRegex<BDD> loop = node.Left;
            SymbolicRegex<BDD> root = node.Left;
            for (int i = 0; i < times - 1; i++)
            {
                root = solver.RegexConverter.MkConcat(root, loop);
            }
            return root;
        }

        string GenerateRandomMember(SymbolicRegex<BDD> root)
        {
            // TODO: ITE is currently not supported.
            string sample = "";
            Stack<SymbolicRegex<BDD>> nodeQueue = new Stack<SymbolicRegex<BDD>>();
            SymbolicRegex<BDD> curNode = null;

            nodeQueue.Push(UnrollRE(root));
            while (nodeQueue.Count > 0 || curNode != null)
            {
                if (curNode == null)
                {
                    curNode = nodeQueue.Pop();
                }
                switch (curNode.Kind)
                {
                    case SymbolicRegexKind.Singleton:
                        if (curNode.Set.IsEmpty)
                            throw new AutomataException(AutomataExceptionKind.SetIsEmpty);

                        sample += solver.ChooseUniformly(curNode.Set);
                        curNode = null;
                        break;
                    case SymbolicRegexKind.Loop:
                        curNode = curNode.Left;
                        break;
                    case SymbolicRegexKind.Epsilon:
                        curNode = curNode.Left;
                        break;
                    case SymbolicRegexKind.Concat:
                        nodeQueue.Push(curNode.Right);
                        curNode = curNode.Left;
                        break;
                    case SymbolicRegexKind.Or:
                        int choice = SampleChildNode(curNode.Left.OrCount,
                                                    curNode.Right.OrCount);
                        curNode = (choice == 0) ? curNode.Left : curNode.Right;
                        break;
                }
            }
            return sample;
        }

        public string GenerateRandomMember()
        {
            return GenerateRandomMember(sr);
        }

        public HashSet<string> GetPositiveDataset(int sampleNum)
        {
            HashSet<string> dataset = new HashSet<string>();

            int totalTries = maxSamplingIter * sampleNum;
            // We iterate this loop at most totalTries to collect the request nr of samples
            while (dataset.Count < sampleNum && totalTries > 0)
            {
                dataset.Add(GenerateRandomMember());
                totalTries = totalTries - 1;
            }
            return dataset;
        }

        public HashSet<string> GetNegativeDataset(int sampleNum)
        {
            HashSet<string> dataset = new HashSet<string>();
            string negRE = solver.ConvertToRegex(solver.Convert(regex).Complement());

            // Invert the regex by converting to SFA and complementing and back to RE
            var root = solver.RegexConverter.ConvertToSymbolicRegex(negRE);

            int totalTries = maxSamplingIter * sampleNum;
            // We iterate this loop at most totalTries to collect the request nr of samples
            while (dataset.Count < sampleNum && totalTries > 0)
            {
                dataset.Add(GenerateRandomMember(root));
                totalTries = totalTries - 1;
            }
            return dataset;
        }

    }
}
