using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class M4M4 : MonoBehaviour
{
    public long targetSum = 1;
    public long incrementer;
    public float bestScore = -1f;
    public long counter;

    private readonly List<long> squaredLookup = new();
    private readonly long[] squaredNums = new long[9];
    private readonly ConcurrentQueue<(long[] grid, long targetSum, long localTargetSum, long matchCount)> validGridsQueue = new();

    [SerializeField] private List<MagicSquareResult> magicSquares = new();

    [Serializable]
    public class MagicSquareResult
    {
        public string name;
        public long targetSum;
        public long matchCount;
        public float score;
        public float percent; // Stores closeness percentage (100 - deviation%)
        public long a, b, c, d, e, f, g, h, i;

        public MagicSquareResult(string name, long targetSum, long matchCount, float score, float percent, long a, long b, long c, long d, long e, long f, long g, long h, long i)
        {
            this.name = name;
            this.targetSum = targetSum;
            this.matchCount = matchCount;
            this.score = score;
            this.percent = percent;
            this.a = a; this.b = b; this.c = c; this.d = d; this.e = e; this.f = f; this.g = g; this.h = h; this.i = i;
        }
    }

    public void Start()
    {
        Task.Run(RunTask);
        StartCoroutine(ProcessQueue());
    }

    private bool IsPerfectSquare(long number)
    {
        long sqrt = (long)Math.Sqrt(number);
        return sqrt * sqrt == number; // Fixed logic: return true if it *is* a perfect square
    }

    private void RunTask()
    {
        int maxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2); // auto on CPU cores
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism };
        long batchSize = 1; // edit this number for more speed! Intel Core i5 12400F can handle 100.       

        while (batchSize < 100000)
        {
            incrementer++;        

            long center = incrementer * incrementer;
            targetSum = center * 3;
            long leftover = targetSum - center;

            long maxIndex = (long)Math.Sqrt(leftover);
            long maxLoop = (long)Math.Sqrt(leftover / 2.0) + 1;

            var pairGroups = new List<(long a, long b)>();
            for (long i = maxIndex; i >= maxLoop; i--)
            {
                long a = i * i;
                long b = leftover - a;
                if (IsPerfectSquare(b))
                {
                    pairGroups.Add((a, b));
                    pairGroups.Add((b, a));
                }
            }

            if (pairGroups.Count < 8) continue;

            batchSize++;

            foreach (var pair1 in pairGroups)
            {
                foreach (var pair2 in pairGroups)
                {
                    foreach (var pair3 in pairGroups)
                    {
                        foreach (var pair4 in pairGroups)
                        {
                            // 0 1 2
                            // 3 4 5
                            // 6 7 8

                            long[] grid = new long[9];

                            grid[4] = center;

                            grid[0] = pair1.a;
                            grid[8] = pair1.b;

                            grid[2] = pair2.a;
                            grid[6] = pair2.b;

                            grid[1] = pair3.a;
                            grid[7] = pair3.b;

                            grid[3] = pair4.a;
                            grid[5] = pair4.b;

                            if (CheckForDuplicates(grid)) continue;

                            //// here check if sum 0,3,6 / 0,1,2 / 2,5,8 / 6/7/8 match if 3 or higher do vaild grid quare

                            ////validGridsQueue.Enqueue((grid, targetSum));

                            // Calculate sums for rows, columns, and diagonals
                            // Manually check sums for 0,3,6 / 0,1,2 / 2,5,8 / 6,7,8
                            long sumCol1 = grid[0] + grid[3] + grid[6]; // Column 1
                            long sumRow1 = grid[0] + grid[1] + grid[2]; // Row 1
                            long sumCol3 = grid[2] + grid[5] + grid[8]; // Column 3
                            long sumRow3 = grid[6] + grid[7] + grid[8]; // Row 3

                            // Count how many sums are equal to each other
                            long[] sums = { sumCol1, sumRow1, sumCol3, sumRow3 };
                            long matchCount = 0;
                            for (int i = 0; i < sums.Length; i++)
                            {
                                for (int j = i + 1; j < sums.Length; j++)
                                {
                                    if (sums[i] == sums[j])
                                    {
                                        matchCount++;
                                    }
                                }
                            }

                            if(matchCount >= 1)
                            {
                                validGridsQueue.Enqueue((grid, targetSum, targetSum, 4));

                                Debug.Log($" TargetSum: {targetSum} | \nGrid: [{string.Join(", ", grid)}]");
                            }


                           

                        }
                    }
                }
            }
        }
    }

    private bool CheckForDuplicates(long[] grid)
    {
        // Check for duplicates among indices
        var set = new HashSet<long>();
        bool hasDuplicates = false;
        for (int i = 0; i < 9; i++)
        {
            if (!set.Add(grid[i]))
            {
                hasDuplicates = true;
                break;
            }
        }
        return hasDuplicates;
    }

    private long[] Sums(long[] grid)
    {
        // Calculate sums for worst percent
        long[] sums = new[]
        {
            grid[0] + grid[1] + grid[2], // Row 1
            grid[3] + grid[4] + grid[5], // Row 2
            grid[6] + grid[7] + grid[8], // Row 3
            grid[0] + grid[3] + grid[6], // Col 1
            grid[1] + grid[4] + grid[7], // Col 2
            grid[2] + grid[5] + grid[8], // Col 3
            grid[0] + grid[4] + grid[8], // Diag 1
            grid[2] + grid[4] + grid[6]  // Diag 2
        };

        return sums;
    }

    private IEnumerator ProcessQueue()
    {
        while (true)
        {
            while (validGridsQueue.Count > 0)
            {
                if (validGridsQueue.TryDequeue(out var result))
                {
                    ScoreCalculator(result.grid, result.targetSum, result.localTargetSum, result.matchCount);
                    //yield return null;
                }
            }
            yield return null;
        }
    }   

    private void ScoreCalculator(long[] grid, long targetSum, long localtargetSum, long matchCount)
    {
        for (int i = 0; i < 9; i++)
        {
            squaredNums[i] = grid[i];
        }

        long[] sums = Sums(grid);

        float percent = 100f;
        for (int i = 0; i < 8; i++)
        {
            float dev = (sums[i] > targetSum) ? (sums[i] - targetSum) * 100f / sums[i] : (targetSum - sums[i]) * 100f / targetSum;
            percent = Math.Min(percent, 100f - dev); // Convert to closeness percentage
        }

        float score = matchCount * 100 + percent;

      

        string name = $"TargetSum: {targetSum} | Score: {score} | Matches: {matchCount} | Squares: {localtargetSum} | Percent: {percent:F2}%";

        var newResult = new MagicSquareResult(
            name, targetSum, matchCount, score, percent,
            squaredNums[0], squaredNums[1], squaredNums[2],
            squaredNums[3], squaredNums[4], squaredNums[5],
            squaredNums[6], squaredNums[7], squaredNums[8]
        );

        var existing = magicSquares.Find(m => m.targetSum == targetSum);
        if (existing != null) { if (score > existing.score) magicSquares[magicSquares.IndexOf(existing)] = newResult; }
        else { magicSquares.Add(newResult); }

        magicSquares.Sort((a, b) => b.score.CompareTo(a.score));

        if (score > bestScore)
        {
            bestScore = score;
            Debug.Log($"High Score: {score:F4} | Squares: {localtargetSum} | Matches: {matchCount} | " +
                      $"TargetSum: {targetSum} | Closeness: {percent:F2}% | \nGrid: [{string.Join(", ", squaredNums)}]");
        }
    }
}