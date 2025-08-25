using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class Match8FinderV2 : MonoBehaviour
{
    public long targetSum = 1;
    public long incrementer;
    public float bestScore = -1f;
    public long counter;

    private readonly List<long> squaredLookup = new();
    private readonly HashSet<long> squaredSet = new(); // Added for O(1) lookups
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
            else
            {
                right = mid - 1;
            }
        }
        return result;
    }

    private void RunTask()
    {
        // Populate squaredLookup and squaredSet
        for (long i = 1; i <= 250000000; i++)
        {
            long square = i * i;
            squaredLookup.Add(square);
            squaredSet.Add(square);
        }

        long itt = 0;

        while (true)
        {
            itt++;
            incrementer = itt;
            long center = itt * itt;
            targetSum = center * 3;
            long leftover = targetSum - center;
            long maxIndex = FindMaxIndex(squaredLookup, leftover);
            long maxLoop = FindMaxIndex(squaredLookup, leftover / 2) + 1;

            var pairGroups = new List<(long a, long b)>();
            for (long i = maxIndex; i >= maxLoop; i--)
            {
                long a = squaredLookup[(int)i];
                long b = leftover - a;
                if (squaredSet.Contains(b)) // Use HashSet for fast lookup
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

                    long[] grid = new long[9];
                    grid[4] = center;
                    grid[0] = pair1.a;
                    grid[8] = pair1.b;
                    grid[2] = pair2.a;
                    grid[6] = pair2.b;

                    // Early exit if computed elements aren't positive squares
                    grid[1] = targetSum - (grid[0] + grid[2]);
                    if (grid[1] <= 0 || !squaredSet.Contains(grid[1])) continue;

                    grid[3] = targetSum - (grid[0] + grid[6]);
                    if (grid[3] <= 0 || !squaredSet.Contains(grid[3])) continue;

                    grid[5] = targetSum - (grid[2] + grid[8]);
                    if (grid[5] <= 0 || !squaredSet.Contains(grid[5])) continue;

                    grid[7] = targetSum - (grid[6] + grid[8]);
                    if (grid[7] <= 0 || !squaredSet.Contains(grid[7])) continue;

                    if (!CheckForDuplicates(grid))
                    {
                        validGridsQueue.Enqueue((grid, 9, targetSum, 0)); // matchCount computed later
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
                return true; // Duplicates found
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

    private void ScoreCalculator(long[] grid, long squareCount, long targetSum, long _)
    {
        for (int i = 0; i < 9; i++)
        {
            squaredNums[i] = grid[i];
        }

        long[] sums = Sums(grid);
        long actualMatchCount = sums.Count(sum => sum == targetSum); // Compute exact matches

        float percent = 100f;
        for (int i = 0; i < 8; i++)
        {
            float dev = (sums[i] > targetSum) ? (sums[i] - targetSum) * 100f / sums[i] : (targetSum - sums[i]) * 100f / targetSum;
            percent = Math.Min(percent, 100f - dev);
        }

        float score = squareCount * 100 + actualMatchCount * 100 + percent;

        if (actualMatchCount == 8) { score += 1000; percent = 100f; }
        else if (score < 1300) { return; }

        if (percent < 99) { return; }

        string name = $"TargetSum: {targetSum} | Score: {score} | Matches: {actualMatchCount} | Squares: {squareCount} | Percent: {percent:F2}%";

        var newResult = new MagicSquareResult(
            name, targetSum, actualMatchCount, score, percent,
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
            Debug.Log($"High Score: {score:F4} | Squares: {squareCount} | Matches: {actualMatchCount} | " +
                      $"TargetSum: {targetSum} | Closeness: {percent:F2}% | \nGrid: [{string.Join(", ", squaredNums)}]");
        }

        if (actualMatchCount == 8 && squareCount == 9)
        {
            Debug.Log("Perfect magic square of squares found!");
            // Optionally stop the search here if desired
        }
    }
}