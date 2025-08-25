using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class TwoLayer : MonoBehaviour
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

    private long FindMaxIndex(List<long> squaredLookup, long leftover)
    {
        int left = 0;
        int right = squaredLookup.Count - 1;
        int result = -1;

        while (left <= right)
        {
            int mid = left + (right - left) / 2;
            if (squaredLookup[mid] <= leftover)
            {
                result = mid;
                left = mid + 1;
            }
            else { right = mid - 1; }
        }
        return result;
    }

    // fully checked upto 100 trillion --- 103,113,923,848,857

    private void RunTask()
    {
        for (long i = 1; i <= 25000000; i++) squaredLookup.Add(i * i);

        long center, leftover, maxIndex, a, b, maxLoop;

        while (true)
        {
            while (true) { if (++targetSum % 3 == 0) break; }

            center = targetSum / 3;
            if (!IsPerfectSquare(center)) { continue; }

            leftover = targetSum - center;

            maxIndex = FindMaxIndex(squaredLookup, leftover);
            maxLoop = FindMaxIndex(squaredLookup, leftover / 2) + 1;

            long maxRoot = Math.Min((long)Math.Ceiling(Math.Sqrt(targetSum)), squaredLookup.Count - 1);

            var pairGroups = new List<(long a, long b)>();
            for (long i = maxIndex; i >= maxLoop; i--)
            {
                a = squaredLookup[(int)i];
                b = leftover - a;
                if (IsPerfectSquare(b))
                {
                    pairGroups.Add((a, b));
                    pairGroups.Add((b, a));
                }
            }

            if (pairGroups.Count < 8) continue;

            foreach (var pair1 in pairGroups)
            {
                foreach (var pair2 in pairGroups)
                {
                    Interlocked.Increment(ref counter);

                    // 0 1 2
                    // 3 4 5
                    // 6 7 8

                    long[] grid = new long[9];

                    grid[4] = center;

                    grid[0] = pair1.a;
                    grid[8] = pair1.b;

                    grid[2] = pair2.a;
                    grid[6] = pair2.b;

                    // below Forcing a match 8 with none squared numbers to lower the Forloops down to 2 then check if all 4 numbers are perfect square
                    // if not don't return data

                    grid[1] = targetSum - (grid[0] + grid[2]); 
                    grid[3] = targetSum - (grid[0] + grid[6]);

                    grid[5] = targetSum - (grid[2] + grid[8]);
                    grid[7] = targetSum - (grid[6] + grid[8]);

                    if (CheckForDuplicates(grid)) continue;                   

                    long sqCounter = CountSquares(grid);

                    if (sqCounter >= 7) { validGridsQueue.Enqueue((grid, sqCounter, targetSum, 8)); }                                 
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
                    ScoreCalculator(result.grid, result.squareCount, result.targetSum, result.matchCount);
                    //yield return null;
                }
            }
            yield return null;
        }
    }

    private long CountSquares(long[] grid)
    {
        long squareCount = 9;
        for (long i = 0; i < 9; i++)
        {
            if (!IsPerfectSquare(grid[i]))
            {
                squareCount--;
                if (squareCount < 7) { break; }
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
            percent = Math.Min(percent, 100f - dev); // Convert to closeness percentage
        }

        float score = squareCount * 100 + matchCount * 100 + percent;

        if (matchCount == 8) { score += 1000; percent = 100f; }
        else if (score < 1300) { return; }

        if (percent < 99) { return; }

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