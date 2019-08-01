using System;
using System.Collections.Generic;
using Microsoft.Automata.Rex;

namespace Microsoft.Automata
{
    class SymbolicRegexSampler<S>
    {

        // Inverse of pobability of taking a corner 
        // case (i.e. lower/upper bound) of the number
        // of iterations a loop may be unrolled.
        private int cornerCaseProb;

        // The maximum number of iterations in order to
        // collect the requested number of samples
        // (see GetDataset method)
        private int maxSamplingIter;

        Random rand;
        int maxUnroll;
        SymbolicRegexNode<S> sr;
        SymbolicRegexBuilder<S> builder;

        public SymbolicRegexSampler(SymbolicRegexBuilder<S> builder, SymbolicRegexNode<S> sr, int maxUnroll, int cornerCaseProb = 5, int maxSamplingIter = 3)
        {
            this.cornerCaseProb = cornerCaseProb;
            this.maxSamplingIter = maxSamplingIter;
            this.maxUnroll = maxUnroll;
            this.sr = sr;
            this.builder = builder;
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

        public SymbolicRegexNode<S> UnrollRE(SymbolicRegexNode<S> re)
        {
            // Create a regular expression without loops by unrolling 
            // each loop a random number of times as dictated by the 
            // maxUnroll parameter
            SymbolicRegexNode<S> newRoot = null;
            switch (re.Kind)
            {
                case SymbolicRegexKind.Concat:
                    newRoot = builder.MkConcat(UnrollRE(re.Left), UnrollRE(re.Right));
                    break;
                case SymbolicRegexKind.IfThenElse:
                    newRoot = builder.MkIfThenElse(re.IteCond,
                        UnrollRE(re.Left), UnrollRE(re.Right));
                    break;
                case SymbolicRegexKind.Or:
                    var alts = Array.ConvertAll(re.alts.ToArray(builder), UnrollRE);
                    newRoot = builder.MkOr(alts);
                    break;
                case SymbolicRegexKind.And:
                    var conj = Array.ConvertAll(re.alts.ToArray(builder), UnrollRE);
                    newRoot = builder.MkAnd(conj);
                    break;
                case SymbolicRegexKind.Loop:
                    newRoot = UnrollRE(UnrollLoop(re));
                    break;
                default: //anchors or singleton or epsilon
                    newRoot = re;
                    break;
            }
            return newRoot;
        }

        private SymbolicRegexNode<S> UnrollLoop(SymbolicRegexNode<S> node)
        {
            // select the number of times the loop will be unrolled
            int times = SampleLoopIterations(node.LowerBound, node.UpperBound);
            switch (times)
            {
                case 0:
                    return builder.epsilon;
                case 1:
                    return node.Left;
            }
            SymbolicRegexNode<S> loop = node.Left;
            SymbolicRegexNode<S> root = node.Left;
            for (int i = 0; i < times - 1; i++)
            {
                root = builder.MkConcat(root, loop);
            }
            return root;
        }

        string GenerateRandomMember(SymbolicRegexBuilder<S> builder, SymbolicRegexNode<S> root)
        {
            // TODO: ITE, And: are currently not supported.
            string sample = "";
            Stack<SymbolicRegexNode<S>> nodeQueue = new Stack<SymbolicRegexNode<S>>();
            SymbolicRegexNode<S> curNode = null;

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
                        if (!builder.solver.IsSatisfiable(curNode.Set))
                            throw new AutomataException(AutomataExceptionKind.SetIsEmpty);

                        sample += builder.solver.ChooseUniformly(curNode.Set);
                        curNode = null;
                        break;
                    case SymbolicRegexKind.Loop:
                        curNode = curNode.Left;
                        break;
                    case SymbolicRegexKind.Epsilon:
                        curNode = null;
                        break;
                    case SymbolicRegexKind.Concat:
                        nodeQueue.Push(curNode.Right);
                        curNode = curNode.Left;
                        break;
                    case SymbolicRegexKind.Or:
                        int choice = rand.Next(curNode.OrCount);
                        int i = 0;
                        foreach (var elem in curNode.alts)
                        {
                            if (i == choice)
                            {
                                curNode = elem;
                                break;
                            }
                            else
                                i += 1;
                        }
                        break;
                    case SymbolicRegexKind.EndAnchor:
                    case SymbolicRegexKind.StartAnchor:
                    case SymbolicRegexKind.WatchDog:
                        curNode = null;
                        break;
                    default:
                        throw new NotImplementedException(curNode.Kind.ToString());
                }
            }
            return sample;
        }

        public string GenerateRandomMember()
        {
            return GenerateRandomMember(this.builder, sr);
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

    }
}
