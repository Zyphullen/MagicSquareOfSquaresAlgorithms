using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class QuickerM6M2 : MonoBehaviour
{
    public long targetSum = 1;
    public long incrementer;
    public float bestScore = -1f;

    public int pairCount;
    public long pairCounterTargetSum;

    private readonly List<long> squaredLookup = new();    
    private readonly ConcurrentQueue<(long[] grid, long squareCount, long targetSum, long matchCount)> validGridsQueue = new();

    public void Start()
    {          
        Task.Run(() => RunTask());
        StartCoroutine(ProcessQueue());
    }

    // fully checked upto 290790690

    // 78,110,100

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
        for (long i = 1; i <= 250000; i++) squaredLookup.Add(i * i);

        int maxConcurrency = Environment.ProcessorCount / 2;
        const int batchSize = 300;
        long batchStart = targetSum;
        
        while (batchStart % 3 != 0) batchStart++; // Ensure batchStart is a multiple of 3

        while (true)
        {
            // Prepare a batch of target sums
            List<long> targetSumsBatch = new List<long>(batchSize);
            for (long ts = batchStart; targetSumsBatch.Count < batchSize && ts <= long.MaxValue - 3; ts += 3) { targetSumsBatch.Add(ts); }
           
            Parallel.ForEach(targetSumsBatch, new ParallelOptions { MaxDegreeOfParallelism = maxConcurrency }, localTargetSum =>
            {
                //if (!IsPerfectSquare(localTargetSum)) return; // only for focrcing squared targetSum

                long center = localTargetSum / 3;
                long minCenter = FindMaxIndex(squaredLookup, (long)(center * 0.5f)); // center - 10%
                long maxCenter = FindMaxIndex(squaredLookup, (long)(center * 1.5f)); // center + 10%                

                for (long c = minCenter; c < maxCenter; c++)
                {
                    center = c * c;

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

                    if (pairGroups.Count < 8) continue;

                    if (pairGroups.Count > pairCount)
                    {
                        pairCount = pairGroups.Count;
                        pairCounterTargetSum = targetSum;
                        Debug.Log("Pair Count: " + pairGroups.Count + " / 2 = : " + (pairGroups.Count / 2) + " TargetSum: " + pairCounterTargetSum);
                        //string pairsLog = "Pairs in pairGroups: " + targetSum + " :";
                        ///foreach (var pair in pairGroups) { pairsLog += $"({pair.a}, {pair.b}), "; }
                        //Debug.Log(pairsLog.TrimEnd(',', ' '));
                    }

                    foreach (var pair1 in pairGroups)
                    {
                        foreach (var pair2 in pairGroups)
                        {
                            long[] grid = new long[9];
                            grid[4] = center;
                            grid[0] = pair1.a;
                            grid[8] = pair1.b;
                            grid[2] = pair2.a;
                            grid[6] = pair2.b;

                            grid[1] = localTargetSum - (grid[0] + grid[2]); if (!IsPerfectSquare(grid[1])) continue;
                            grid[7] = localTargetSum - (grid[1] + grid[4]); if (!IsPerfectSquare(grid[7])) continue;
                            grid[3] = localTargetSum - (grid[0] + grid[6]); if (!IsPerfectSquare(grid[3])) continue;
                            grid[5] = localTargetSum - (grid[3] + grid[4]); if (!IsPerfectSquare(grid[5])) continue;

                            if (CheckForDuplicates(grid)) continue;

                            validGridsQueue.Enqueue((grid, 9, localTargetSum, 6));

                            Debug.Log("Pair Count: " + pairGroups.Count / 2 + " TargetSum: " + localTargetSum);
                        }
                    }
                }
            });

            // Update targetSum to the last processed value
            batchStart = targetSumsBatch[targetSumsBatch.Count - 1] + 3;
            Interlocked.Exchange(ref targetSum, targetSumsBatch[targetSumsBatch.Count - 1]);           
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
            if (!set.Add(grid[i])) return true;
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
            grid[0], grid[1], grid[2],
            grid[3], grid[4], grid[5],
            grid[6], grid[7], grid[8]
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
        else { results.Add((targetSum, percent, grid.ToArray(), score)); }

        if (score > bestScore)
        {
            bestScore = score;
            Debug.Log($"High Score: {score:F4} | Matches: {matchCount} | " +
                      $"TargetSum: {targetSum} | Closeness: {percent}% | \nGrid: [{string.Join(", ", grid)}]");
        }

        UpdateStrings();
    }

    private void UpdateStrings()
    {
        lock (results)
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
    }

    [Serializable]
    public class ResultsDisplay
    {
        [TextArea(3, 10)] public string targetSumsString = "";
        [TextArea(3, 10)] public string percentsString = "";
        [TextArea(3, 10)] public string gridsString = "";
    }    

    public ResultsDisplay resultsDisplay = new ResultsDisplay();

    private readonly List<(long targetSum, float percent, long[] grid, float score)> results = new();
    [SerializeField] public List<MagicSquareResult> magicSquares = new();

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