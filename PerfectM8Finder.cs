using System;
using System.Collections.Generic;
using UnityEngine;

public class PerfectM8Finder : MonoBehaviour
{
    public long targetSum, incrementer;

    private bool IsPerfectSquare(long number) => (long)Math.Sqrt(number) * (long)Math.Sqrt(number) == number;

    public void Start()    
    {        
        long sum = 0, center = 0, squareNum = 0, leftover = 0;      

        while (true)
        {           
            squareNum++;
            center = squareNum * squareNum;
          
            sum = center * 3;
            leftover = sum - center;
          
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

                    grid[1] = sum - (grid[0] + grid[2]); if (!IsPerfectSquare(grid[1])) continue;
                    grid[3] = sum - (grid[0] + grid[6]); if (!IsPerfectSquare(grid[3])) continue;
                    grid[5] = sum - (grid[2] + grid[8]); if (!IsPerfectSquare(grid[5])) continue;
                    grid[7] = sum - (grid[6] + grid[8]); if (!IsPerfectSquare(grid[7])) continue;                   
                }
            }
        }        
    }       
}
