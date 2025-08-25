using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class M6S8Finder : MonoBehaviour
{
    public long targetSum = 1;
    public long incrementer;
    public float bestScore = -1f;
    public int pairCount;
    public long pairCounterTargetSum;    

    public void Start() { Task.Run(RunTask); StartCoroutine(ProcessQueue()); }   

    private void RunTask() // will work faster with better CPU more cores / higher batch size!
    {
        int maxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2); // auto on CPU cores
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism };
        const long batchSize = 250; // edit this number for more speed! Intel Core i5 12400F can handle 100.       

        while (true)
        {
            long startIncrementer = incrementer;
            long endIncrementer = startIncrementer + batchSize;

            Parallel.For(startIncrementer, endIncrementer, parallelOptions, (inc, state) =>
            {
                long center = inc;
                long localTargetSum = center * 3;
                long leftover = localTargetSum - center;

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

                if (pairGroups.Count < 4) return;               

                foreach (var pair1 in pairGroups)
                {
                    foreach (var pair2 in pairGroups)
                    {
                        foreach (var pair3 in pairGroups)
                        {
                            foreach (var pair4 in pairGroups)
                            {  
                                long[] grid = new long[9];

                                grid[4] = center;

                                grid[0] = pair1.a;
                                grid[8] = pair1.b;

                                grid[1] = pair2.a;
                                grid[7] = pair2.b;

                                grid[2] = pair3.a;
                                grid[6] = pair3.b;

                                grid[3] = pair4.a;
                                grid[5] = pair4.b;

                                if (CheckForDuplicates(grid)) continue;

                                long totalMatches = Sums(grid).Count(s => s == localTargetSum);

                                if (totalMatches >= 6) { validGridsQueue.Enqueue((grid, localTargetSum, totalMatches, 8)); }
                            }
                        }
                    }
                }
            });

            incrementer += batchSize;
            targetSum = incrementer * 3;
        }
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

    private bool CheckForDuplicates(long[] grid)
    {
        var set = new HashSet<long>();
        for (long i = 0; i < 9; i++)
        {
            if (!set.Add(grid[i])) { return true; }
        }
        return false;
    }

    private bool IsPerfectSquare(long number)
    {
        long sqrt = (long)Math.Sqrt(number);
        return sqrt * sqrt == number; // Fixed logic: return true if it *is* a perfect square
    }

    private IEnumerator ProcessQueue()
    {        
        while (true)
        {
            while (validGridsQueue.Count > 0)
            {
                if (validGridsQueue.TryDequeue(out var result))
                {
                    ScoreCalculator(result.grid, result.targetSum, result.matchCount, result.sqCount); 
                }
            }
            yield return null;
        }
    }

    private void ScoreCalculator(long[] grid, long targetSum, long matchCount, long squareCount)
    {
        for (int i = 0; i < 9; i++) { squaredNums[i] = grid[i]; }

        long[] sums = Sums(grid);

        float percent = 100f;
        for (int i = 0; i < 8; i++)
        {
            float dev = (sums[i] > targetSum) ? (sums[i] - targetSum) * 100f / sums[i] : (targetSum - sums[i]) * 100f / targetSum;
            percent = Math.Min(percent, 100f - dev); // Convert to closeness percentage
        }

        float score = squareCount * 100 + matchCount * 100 + percent;

        string name = $"TargetSum: {targetSum} | Score: {score} | Matches: {matchCount} | Percent: {percent:F4}%";

        var newResult = new MagicSquareResult(
            name, targetSum, matchCount, score, percent,
            squaredNums[0], squaredNums[1], squaredNums[2],
            squaredNums[3], squaredNums[4], squaredNums[5],
            squaredNums[6], squaredNums[7], squaredNums[8]
        );

        var existingTarget = magicSquares.Find(m => m.targetSum == targetSum);
        if (existingTarget != null)
        {
            if (score > existingTarget.score)
            {
                magicSquares[magicSquares.IndexOf(existingTarget)] = newResult;
            }
        }
        else
        {
            magicSquares.Add(newResult);
        }

        magicSquares.Sort((a, b) => b.score.CompareTo(a.score));

        // Update the results list
        var existingResultIndex = results.FindIndex(r => r.targetSum == targetSum);
        if (existingResultIndex >= 0)
        {
            if (score > results[existingResultIndex].score)
            {
                results[existingResultIndex] = (targetSum, percent, grid.ToArray(), score);
            }
        }
        else
        {
            results.Add((targetSum, percent, grid.ToArray(), score));
        }

        if (score > bestScore)
        {
            bestScore = score;
            Debug.Log($"High Score: {score:F4} | Matches: {matchCount} | " +
                      $"TargetSum: {targetSum} | Closeness: {percent:F2}% | \nGrid: [{string.Join(", ", squaredNums)}]");
        }

        UpdateStrings();
    }

    private readonly long[] squaredNums = new long[9];
    private readonly ConcurrentQueue<(long[] grid, long targetSum, long matchCount, long sqCount)> validGridsQueue = new();   

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

    private void UpdateStrings()
    {
        var sortedResults = results.OrderBy(r => r.targetSum).ToList();

        resultsDisplay.targetSumsString = string.Join("\n", sortedResults.Select(r => r.targetSum));

        List<string> formattedPercents = new List<string>();
        foreach (var result in sortedResults)
        {
            float percent = result.percent;
            string percentStr = percent.ToString("G");
            string[] parts = percentStr.Split('.');
            if (parts.Length > 1)
            {
                string decimalPart = parts[1].TrimEnd('0');
                if (decimalPart.Length > 10) decimalPart = decimalPart.Substring(0, 10);
                percentStr = parts[0] + (decimalPart.Length > 0 ? "." + decimalPart : "");
            }
            formattedPercents.Add(percentStr + "%");
        }
        resultsDisplay.percentsString = string.Join("\n", formattedPercents);

        resultsDisplay.gridsString = string.Join("\n", sortedResults.Select(r => string.Join(" ", r.grid)));
    }

    // Serializable class to group the strings into a foldable section
    [Serializable]
    public class ResultsDisplay
    {
        [TextArea(3, 10)] public string targetSumsString = "";
        [TextArea(3, 10)] public string percentsString = "";
        [TextArea(3, 10)] public string gridsString = "";
    }

    // Public instance of ResultsDisplay, which will be foldable in the Inspector
    public ResultsDisplay resultsDisplay = new ResultsDisplay();

    private readonly List<long> squaredLookup = new();   
    private readonly List<(long targetSum, float percent, long[] grid, float score)> results = new();
}
