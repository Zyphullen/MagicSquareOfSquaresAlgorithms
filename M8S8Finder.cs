using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class M8S8Finder : MonoBehaviour
{
    public long targetSum = 1;
    public long incrementer;
    public float bestScore = -1f;
    public int pairCount;
    public long pairCounterTargetSum;

    public void Start() { Task.Run(RunTask); }

    private void RunTask()
    {
        int maxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2); // Use half of available CPU cores
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism };
        const long batchSize = 3000; // Adjust for performance (e.g., 100 works well for Intel Core i5 12400F)

        while (true)
        {
            long startTargetSum = targetSum;
            long endTargetSum = startTargetSum + batchSize;

            Parallel.For(startTargetSum, endTargetSum, parallelOptions, (localTargetSum, state) =>
            {
                // Ensure targetSum is divisible by 3
                if (localTargetSum % 3 != 0) return;

                long center = localTargetSum / 3;
                if (IsPerfectSquare(center)) return; // Skip if center is a perfect square

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

                lock (this) // Synchronize access to shared variables
                {
                    if (pairGroups.Count > pairCount)
                    {
                        pairCount = pairGroups.Count;
                        pairCounterTargetSum = localTargetSum;
                        Debug.Log($"Pair Count: {pairGroups.Count} / 2 = : {pairGroups.Count / 2}");
                    }
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
                        grid[3] = localTargetSum - (grid[0] + grid[6]); if (!IsPerfectSquare(grid[3])) continue;
                        grid[5] = localTargetSum - (grid[2] + grid[8]); if (!IsPerfectSquare(grid[5])) continue;
                        grid[7] = localTargetSum - (grid[6] + grid[8]); if (!IsPerfectSquare(grid[7])) continue;

                        lock (this) // Synchronize Debug.Log and bestScore update
                        {
                            Debug.Log($"Congratulations M8 Found! TargetSum: {localTargetSum} | \nGrid: [{string.Join(", ", grid)}]");
                            bestScore = 800;
                        }
                    }
                }
            });

            Interlocked.Add(ref targetSum, batchSize);
        }
    }

    private bool IsPerfectSquare(long number)
    {
        long sqrt = (long)Math.Sqrt(number);
        return sqrt * sqrt == number;
    }
}


// 36,832,480
// 426,895,252
// 1,659,264,905

// 9913885887 - pair count 144 at 9479391675