using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class CornerCenterMatcher : MonoBehaviour
{
    public long targetSum = 1;
    public long incrementer;
    public float bestScore = -1f;
    public int pairCount;
    public long pairCounterTargetSum;

    public int muitplyer;

    public void Start() { Task.Run(RunTask); }

    private void RunTask() // will work faster with better CPU more cores / higher batch size!
    {
        int maxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2); // auto on CPU cores
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism };
        const long batchSize = 100; // edit this number for more speed! Intel Core i5 12400F can handle 100.       

        while (true)
        {
            long startIncrementer = incrementer;
            long endIncrementer = startIncrementer + batchSize;

            Parallel.For(startIncrementer, endIncrementer, parallelOptions, (inc, state) =>
            {
                long center = inc * inc;
                long localTargetSum = center * muitplyer - (center / 8);
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

                if (pairGroups.Count < 8) return;

                targetSum = localTargetSum;

                if (pairGroups.Count > pairCount)
                {
                    pairCount = pairGroups.Count;
                    pairCounterTargetSum = targetSum;
                    Debug.Log("Pair Count: " + pairGroups.Count + " / 2 = : " + (pairGroups.Count / 2));
                    //string pairsLog = "Pairs in pairGroups: " + targetSum + " :";
                    ///foreach (var pair in pairGroups) { pairsLog += $"({pair.a}, {pair.b}), "; }
                    //Debug.Log(pairsLog.TrimEnd(',', ' '));

                }

                foreach (var pair1 in pairGroups)
                {
                    foreach (var pair2 in pairGroups)
                    {   
                        foreach (var pair3 in pairGroups)
                        {

                            long[] grid = new long[9];

                            // 0 1 2
                            // 3 4 5
                            // 6 7 8

                            grid[0] = center;

                            grid[1] = pair1.a;
                            grid[2] = pair1.b;

                            grid[4] = pair2.a;
                            grid[8] = pair2.b; 
                            
                            grid[3] = pair3.a;
                            grid[6] = pair3.b;

                            grid[5] = localTargetSum - (grid[2] + grid[8]); //if (!IsPerfectSquare(grid[5])) continue;
                            grid[7] = localTargetSum - (grid[6] + grid[8]); //if (!IsPerfectSquare(grid[7])) continue;
                           
                            if (CheckForDuplicates(grid)) continue;

                            long totalMatches = Sums(grid).Count(s => s == targetSum);

                            if (totalMatches >= 6)
                            {
                                Debug.Log($"Congratulations M8 Found! TargetSum: {localTargetSum} | \nGrid: [{string.Join(", ", grid)}]");
                                bestScore = 800;
                            }
                        }                        
                    }
                }
            });

            incrementer += batchSize;
            targetSum = incrementer * incrementer * 3;
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

    private bool IsPerfectSquare(long number)
    {
        long sqrt = (long)Math.Sqrt(number);
        return sqrt * sqrt == number; // Fixed logic: return true if it *is* a perfect square
    }
}
