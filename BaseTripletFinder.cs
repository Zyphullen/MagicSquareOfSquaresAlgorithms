using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class BaseTripletFinder : MonoBehaviour
{
    public long targetSum = 1;
    public long incrementer;
    public float bestScore = -1f;
    public long counter;

    private readonly List<long> squaredLookup = new();
    private readonly long[] squaredNums = new long[9];
    private readonly ConcurrentQueue<(long[] grid, long squareCount, long targetSum, long matchCount)> validGridsQueue = new();

    [SerializeField] private List<MagicSquareResult> magicSquares = new();

    [Serializable]
    public class MagicSquareResult
    {
        public string name;
        public long targetSum;
        public long matchCount;
        public float score;
        public float percent;
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
        return sqrt * sqrt == number;
    }

    public float decimalPlace = 1f;

    private void RunTask()
    {
        for (long i = 1; i <= 250000; i++) squaredLookup.Add(i * i);

        while (true)
        {
            targetSum++;

            long maxRoot = Math.Min((long)Math.Ceiling(Math.Sqrt(targetSum)), squaredLookup.Count - 1);

            var tripletGroups = new List<List<long>>();

            for (long a = 0; a < maxRoot; a++)
            {
                long aSq = squaredLookup[(int)a];
                if (aSq >= targetSum) break;
                for (long b = a; b < maxRoot; b++)
                {
                    long bSq = squaredLookup[(int)b];
                    long sumAb = aSq + bSq;
                    if (sumAb >= targetSum) break;
                    long remainder = targetSum - sumAb;

                    int c = squaredLookup.BinarySearch(remainder);
                    if (c < 0 || c <= b) continue;

                    tripletGroups.Add(new List<long> { a, b, c });
                    tripletGroups.Add(new List<long> { a, c, b });
                    tripletGroups.Add(new List<long> { b, a, c });
                    tripletGroups.Add(new List<long> { b, c, a });
                    tripletGroups.Add(new List<long> { c, a, b });
                    tripletGroups.Add(new List<long> { c, b, a });

                    Interlocked.Increment(ref counter);
                }
            }



            //Debug.Log($"targetSum={targetSum}, tripletGroups.Count={tripletGroups.Count}");
            if (tripletGroups.Count < 3) continue;

            foreach (var trip1 in tripletGroups)
            {
                foreach (var trip2 in tripletGroups)
                {
                    foreach (var trip3 in tripletGroups)
                    {
                        Interlocked.Increment(ref counter);

                        // 0 1 2
                        // 3 4 5
                        // 6 7 8


                        long[] grid = new long[9]
                        {
                            squaredLookup[(int)trip1[0]],
                            squaredLookup[(int)trip1[1]],
                            squaredLookup[(int)trip1[2]],
                            squaredLookup[(int)trip2[0]],
                            squaredLookup[(int)trip2[1]],
                            squaredLookup[(int)trip2[2]],
                            squaredLookup[(int)trip3[0]],
                            squaredLookup[(int)trip3[1]],
                            squaredLookup[(int)trip3[2]]
                        };

                        if (CheckForDuplicates(grid)) continue;

                        long[] sums = Sums(grid);
                        long totalMatches = sums.Count(s => s == targetSum);

                        if (totalMatches >= 4)
                        {
                            validGridsQueue.Enqueue((grid, 9, targetSum, totalMatches));
                            //Debug.Log($"Valid grid found: targetSum={targetSum}, matches={totalMatches}, grid=[{string.Join(", ", grid)}]");
                        }
                    }
                }
            }
        }
    }

    private bool CheckForDuplicates(long[] grid)
    {
        var set = new HashSet<long>();
        for (int i = 0; i < 9; i++)
        {
            if (!set.Add(grid[i]))
            {
                return true;
            }
        }
        return false;
    }

    private long[] Sums(long[] grid)
    {
        return new[]
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
    }

    private IEnumerator ProcessQueue()
    {
        while (true)
        {
            while (validGridsQueue.Count > 0)
            {
                if (validGridsQueue.TryDequeue(out var result))
                {
                    ScoreCalculator(result.grid, result.squareCount, result.targetSum, result.matchCount);
                }
            }
            yield return null;
        }
    }

    private long CountSquares(long[] grid)
    {
        long squareCount = 0;
        for (int i = 0; i < 9; i++)
        {
            if (IsPerfectSquare(grid[i]))
            {
                squareCount++;
            }
        }
        return squareCount;
    }

    private void ScoreCalculator(long[] grid, long squareCount, long targetSum, long matchCount)
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
            percent = Math.Min(percent, 100f - dev);
        }

        float score = squareCount * 100 + matchCount * 100 + percent;

        if (score < 1000) { return; }
        if (percent < 95) { return; }

        string name = $"TargetSum: {targetSum} | Score: {score} | Matches: {matchCount} | Squares: {squareCount} | Percent: {percent:F2}%";

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
            Debug.Log($"High Score: {score:F4} | Squares: {squareCount} | Matches: {matchCount} | " +
                      $"TargetSum: {targetSum} | Closeness: {percent:F2}% | \nGrid: [{string.Join(", ", squaredNums)}]");
        }
    }
}