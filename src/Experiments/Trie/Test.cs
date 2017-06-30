using System;

using Microsoft.Automata;
using Microsoft.Automata.BooleanAlgebras;

namespace Experiments.Trie
{
    internal class Test
    {
        static PredicateTrie<BV128> CreateRandomTrie(BV128Algebra alg, int k)
        {
            PredicateTrie<BV128> trie = new PredicateTrie<BV128>(alg);

            for (int j = 0; j < k; j++)
            {
                var pred = BV128.Random();
                trie.Search(pred);
            }
            return trie;
        }

        internal static void Run()
        {
            Console.WriteLine("Trie.Test.RunRandom");
            BV128Algebra alg = new BV128Algebra();

            Console.WriteLine("log2(n),  depth, avg depth, depth/log2(n), avg-log2(n)");
            System.Diagnostics.Debug.WriteLine("log2(n),  depth, avg depth");

            int K = 100;

            double ACC = 0;
            double ACC2 = 0;
            double X = 0;

            for (double d = 5; d <= 10; d += 0.5)
            {
                int n = (int)Math.Pow(2.0, d);
                double count = 0;
                double depth = 0;
                double avg = 0;
                double avg2 = 0;
                for (int rep = 0; rep < K; rep++)
                {
                    var trie = CreateRandomTrie(alg, n);
                    count += (double)trie.LeafCount;
                    depth += (double)trie.Depth;
                    avg += trie.AverageNodeDepth();
                    avg2 += ((double)trie.TotalLeafDepth()) / ((double)trie.LeafCount);
                }
                count = count / (double)K;
                depth = depth / (double)K;
                avg = avg / (double)K;
                avg2 = avg2 / (double)K;

                double log2n = Math.Log((double)n, (double)2);

                ACC += (avg - log2n);
                ACC2 += (depth / log2n);
                X += 1;

                Console.WriteLine("{0:F2},   {1:F2},   {2:F5},   {3:F5}, {4:F5}", log2n, depth, avg, depth / log2n, avg - log2n);
                System.Diagnostics.Debug.WriteLine("{0:F2},   {1:F5},   {2:F5}", log2n, depth, avg);
            }
            System.Diagnostics.Debug.WriteLine("Median of avg-log2(n) = {0:F3}", ACC / X);
            System.Diagnostics.Debug.WriteLine("Median of depth/log2(n) = {0:F3}", ACC2 / X);
        }
    }
}

