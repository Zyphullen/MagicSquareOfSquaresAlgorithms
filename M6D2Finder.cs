using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class M6D2Finder : MonoBehaviour
{
    public long targetSum = 1;  
    public float bestScore = -1f;
    public long counter;  

    public void Start()
    {
        Task.Run(RunTask);
        StartCoroutine(ProcessQueue());
    }

    private void RunTask() // Note This script finds Only M6D2, don't change make new script dumbass!
    {
        for (long i = 1; i <= 2500000; i++) { squaredLookup.Add(i * i); }

        var squareToRoot = new Dictionary<long, int>(squaredLookup.Count);
        for (int i = 0; i < squaredLookup.Count; i++) { squareToRoot[squaredLookup[i]] = i; }

        int halfCores = Math.Max(1, Environment.ProcessorCount / 2);
        long squaredCounter = 0; //17328;

        while (true)
        {
            //squaredCounter++;
            targetSum++;


            long maxRoot = Math.Min((long)Math.Ceiling(Math.Sqrt(targetSum)), squaredLookup.Count);

            var tripletGroups = new ConcurrentDictionary<long, List<List<long>>>();
            Parallel.For(0, maxRoot,
                new ParallelOptions { MaxDegreeOfParallelism = halfCores },
                () => new Dictionary<long, List<List<long>>>(),
                (a, loop, localDict) =>
                {
                    long aSq = squaredLookup[(int)a];

                    for (long b = a + 1; b < maxRoot; b++)
                    {
                        long bSq = squaredLookup[(int)b];
                        long sumAb = aSq + bSq;
                        if (sumAb >= targetSum) break;
                        long remainder = targetSum - sumAb;

                        if (squareToRoot.TryGetValue(remainder, out int c) && c > b)
                        {
                            var triplets = new[]
                            {
                                new List<long> { a, b, c },
                                new List<long> { a, c, b },
                                new List<long> { b, a, c },
                                new List<long> { b, c, a },
                                new List<long> { c, a, b },
                                new List<long> { c, b, a }
                            };

                            foreach (var triplet in triplets)
                            {
                                long center = triplet[1];
                                if (!localDict.ContainsKey(center))
                                    localDict[center] = new List<List<long>>();
                                localDict[center].Add(triplet);
                            }
                            Interlocked.Increment(ref counter);
                        }
                    }
                    return localDict;
                },
                (localDict) =>
                {
                    foreach (var kvp in localDict)
                    {
                        var globalList = tripletGroups.GetOrAdd(kvp.Key, _ => new List<List<long>>());
                        lock (globalList) { globalList.AddRange(kvp.Value); }
                    }
                });

            foreach (var group in tripletGroups)
            {
                if (group.Value.Count < 4) continue;

                var triplets = group.Value;

                foreach (var diagA in triplets)
                {
                    foreach (var rowB in triplets)
                    {
                        Interlocked.Increment(ref counter);
                        long[] grid = new long[9];

                        // 0 1 2
                        // 3 4 5
                        // 6 7 8

                        grid[0] = squaredLookup[(int)diagA[0]];
                        grid[4] = squaredLookup[(int)diagA[1]];
                        grid[8] = squaredLookup[(int)diagA[2]];

                        grid[2] = squaredLookup[(int)rowB[0]];
                        grid[6] = squaredLookup[(int)rowB[2]];

                        grid[1] = targetSum - (grid[0] + grid[2]);
                        grid[7] = targetSum - (grid[1] + grid[4]);

                        grid[3] = targetSum - (grid[0] + grid[6]);
                        grid[5] = targetSum - (grid[3] + grid[4]);

                        if (CheckForDuplicates(grid)) { continue; }

                        long totalMatches = Sums(grid).Count(s => s == targetSum);

                        if (totalMatches >= 6) { validGridsQueue.Enqueue((grid, targetSum, totalMatches)); }
                    }
                }
            }
        }
    }

    private bool IsPerfectSquare(long number)
    {
        long sqrt = (long)Math.Sqrt(number);
        return sqrt * sqrt == number;
    }

    private bool CheckForDuplicates(long[] grid)
    {
        var set = new HashSet<long>();
        for (long i = 0; i < 9; i++)
        {
            if (!set.Add(grid[i]) || !IsPerfectSquare(grid[i])) { return true; }
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
                    ScoreCalculator(result.grid, result.targetSum, result.matchCount);
                }
            }            
            yield return null;
        }
    }

    private void ScoreCalculator(long[] grid, long targetSum, long matchCount)
    {
        for (int i = 0; i < 9; i++) { squaredNums[i] = grid[i]; }

        long[] sums = Sums(grid);
        float percent = 100f;
        for (int i = 0; i < 8; i++)
        {
            float dev = (sums[i] > targetSum) ? (sums[i] - targetSum) * 100f / sums[i] : (targetSum - sums[i]) * 100f / targetSum;
            percent = Math.Min(percent, 100f - dev);
        }

        float score = matchCount * 100 + percent;
        string name = $"TargetSum: {targetSum} | Score: {score} | Matches: {matchCount} | Percent: {percent}%";

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
                      $"TargetSum: {targetSum} | Closeness: {percent}% | \nGrid: [{string.Join(", ", squaredNums)}]");
        }

        UpdateStrings();
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
    private readonly long[] squaredNums = new long[9];
    private readonly ConcurrentQueue<(long[] grid, long targetSum, long matchCount)> validGridsQueue = new();
    private readonly List<(long targetSum, float percent, long[] grid, float score)> results = new();

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
}