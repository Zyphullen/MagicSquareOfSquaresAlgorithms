using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class Match8Finder : MonoBehaviour
{
    public long targetSum = 1;
    public long incrementer;
    public float bestScore = -1f;
    public int pairCount;
    public long pairCounterTargetSum;
    public int counter;

    public float muitplyer;
    public int sqCountM;

    public void Start() { Task.Run(RunTask); }  

    private void RunTask() // will work faster with better CPU more cores / higher batch size!
    {                
        int maxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2); // auto on CPU cores
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism };        
        const long batchSize = 100; // edit this number for more speed! Intel Core i5 12400F can handle 100.       
        int counter = 0;

        while (counter < 100)
        {
            long startIncrementer = incrementer;
            long endIncrementer = startIncrementer + batchSize;
           

            Parallel.For(startIncrementer, endIncrementer, parallelOptions, (inc, state) =>
            {
                long center = inc * inc;
                long localTargetSum = (long)(center * muitplyer);
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
                //counter++;

                //if (pairGroups.Count > pairCount)
                //{
                //    pairCount = pairGroups.Count;
                //    pairCounterTargetSum = targetSum;                   
                //    Debug.Log("Pair Count: " + pairGroups.Count + " / 2 = : " + (pairGroups.Count / 2));
                //    string pairsLog = "Pairs in pairGroups: " + targetSum + " :";
                //    foreach (var pair in pairGroups) { pairsLog += $"({pair.a}, {pair.b}), "; }
                //    Debug.Log(pairsLog.TrimEnd(',', ' '));
                   
                //}

                //string pairsLog = "Pairs in pairGroups: " + localTargetSum + " :";
                //foreach (var pair in pairGroups) { pairsLog += $"({pair.a}, {pair.b}), "; }
                //Debug.Log(pairsLog.TrimEnd(',', ' '));

                foreach (var pair1 in pairGroups)
                {
                    foreach (var pair2 in pairGroups)
                    {
                        long[] grid = new long[9];

                        // 0 1 2
                        // 3 4 5
                        // 6 7 8

                        grid[4] = center;

                        grid[0] = pair1.a;
                        grid[8] = pair1.b;
                        grid[2] = pair2.a;
                        grid[6] = pair2.b;

                        grid[1] = localTargetSum - (grid[0] + grid[2]);
                        if (!IsPerfectSquare(grid[1])) continue;
                        //if (grid[1] < 0) continue;
                        grid[3] = localTargetSum - (grid[0] + grid[6]);
                        if (grid[3] < 0) continue;
                        //if (!IsPerfectSquare(grid[3])) continue;
                        grid[5] = localTargetSum - (grid[2] + grid[8]);
                        if (grid[5] < 0) continue;
                        //if (!IsPerfectSquare(grid[5])) continue;
                        grid[7] = localTargetSum - (grid[6] + grid[8]);
                        if (grid[7] < 0) continue;
                        //if (!IsPerfectSquare(grid[7])) continue;
                        if (CheckForDuplicates(grid)) continue;

                        long sqCounter = CountSquares(grid);

                        long totalMatches = Sums(grid).Count(s => s == localTargetSum);

                        
                        if (sqCounter >= sqCountM)
                        {
                            Debug.Log($"Congratulations M{totalMatches} Found! TargetSum: {localTargetSum} | \nGrid: [{string.Join(", ", grid)}]");
                            bestScore = 800;
                        }
                       
                    }
                }
            });

            incrementer += batchSize;
            targetSum = incrementer * incrementer * 3;
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

private long CountSquares(long[] grid)
    {
        long squareCount = 9;
        for (long i = 0; i < 9; i++)
        {
            if (!IsPerfectSquare(grid[i]))
            {
                squareCount--;
                if (squareCount < sqCountM) { break; }
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

    private bool IsPerfectSquare(long number) => (long)Math.Sqrt(number) * (long)Math.Sqrt(number) == number;
}
