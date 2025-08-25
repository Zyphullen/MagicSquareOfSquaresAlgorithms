using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class Match8PairCounter : MonoBehaviour
{
    public long targetSum = 1;
    public long incrementer;
    public float bestScore = -1f;
    public int pairCount;
    public long pairCounterTargetSum;

    public List<PairCount> pairCountList;

    public void Start() { Task.Run(() => RunTask()); }

    // 85,683,000,000

    // 36,687,027,000,000

    // fully checked upto 5165000 - 12/07/25  

    private void RunTask()
    {
        const int batchSize = 200;
        int maxDegreeOfParallelism = Environment.ProcessorCount / 2;
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism };

        while (true)
        {
            long startIncrementer = incrementer;
            long endIncrementer = startIncrementer + batchSize;

            Parallel.For(startIncrementer, endIncrementer, parallelOptions, (inc, state) =>
            {
                long center = inc * inc;
                long localTargetSum = center * 3;
                long leftover = localTargetSum - center;

                long maxIndex = (long)Math.Sqrt(leftover);
                long maxLoop = (long)Math.Sqrt(leftover / 2.0) + 1;

                var numberList = new List<long>();
                for (long j = maxIndex; j >= maxLoop; j--)
                {
                    long a = j * j;
                    long b = leftover - a;
                    if (IsPerfectSquare(b))
                    {
                        numberList.Add(a);
                        numberList.Add(b);
                    }
                }

                if (numberList.Count < 4) return;

                if (numberList.Count > pairCount)
                {
                    pairCount = numberList.Count;
                    pairCounterTargetSum = localTargetSum;
                    //string pairsLog = "Pairs in pairGroups: ";
                    //foreach (var pair in pairGroups) { pairsLog += $"({pair.a}, {pair.b}), "; }
                    //Debug.Log(pairsLog.TrimEnd(',', ' '));
                    Debug.Log("Pair Count: " + numberList.Count);
                    //UpdatePairCount("Pair Count = " + pairGroups.Count, pairGroups.Count, localTargetSum);
                }

                //UpdatePairCount("Pair Count = " + pairGroups.Count, pairGroups.Count, localTargetSum);

                int tripletCount = 0;
                for (int i = 0; i < numberList.Count; i++)
                {
                    for (int j = 0; j < numberList.Count; j++)
                    {
                        for (int k = 0; k < numberList.Count; k++)
                        {
                            long sum = numberList[i] + numberList[j] + numberList[k];
                            if (sum == targetSum)
                            {
                                tripletCount++;
                                Debug.Log($"Triplet found: ({numberList[i]}, {numberList[j]}, {numberList[k]})");
                            }
                        }
                    }
                }

                if(tripletCount >= 1)
                {
                    Debug.Log($"Total triplets found: {tripletCount}");
                }
               


            });

            incrementer += batchSize;
            targetSum = incrementer * incrementer * 3;
        }
    }

    private bool IsPerfectSquare(long number)
    {
        long sqrt = (long)Math.Sqrt(number);
        return sqrt * sqrt == number;
    }

    [Serializable]
    public class PairCount
    {
        public string name;
        public long pairCount;
        public long pairAmount;
        public long firstTargetSum;
    }   

    public void UpdatePairCount(string name, long pairCount, long targetSum)
    {
        PairCount existingPair = pairCountList.Find(pc => pc.pairCount == pairCount);

        if (existingPair != null)
        {
            existingPair.pairAmount++;
            existingPair.name = name + " | PairAmount = " + existingPair.pairAmount;
        }
        else
        {
            string nameThis = name + " | PairAmount = " + 1;

            pairCountList.Add(new PairCount
            {
                name = nameThis,
                pairCount = pairCount,
                pairAmount = 1,
                firstTargetSum = targetSum
            });

            pairCountList = pairCountList.OrderBy(pc => pc.pairCount).ToList();
        }
    }


}