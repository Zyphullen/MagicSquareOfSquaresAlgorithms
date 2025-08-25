using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class Match8PairChecker : MonoBehaviour
{
    public long incrementer; // Manually input number
    public long targetSum;
    public int pairCount;
    public long pairCounterTargetSum;
    public List<PairCount> pairCountList = new List<PairCount>();

    [Serializable]
    public class PairCount
    {
        public string name;
        public long pairCount;
        public long pairAmount;
        public long firstTargetSum;
        public long value; // Store individual number (a or b)
    }

    public void Start() { Task.Run(RunTask); }

    private void RunTask()
    {
        // Calculate center and target sum
        long center = incrementer * incrementer;
        targetSum = center * 3;
        long leftover = targetSum - center;

        long maxIndex = (long)Math.Sqrt(leftover);
        long maxLoop = (long)Math.Sqrt(leftover / 2.0) + 1;

        // Find pairs and store a and b in a single list
        var numberList = new List<long>();
        for (long j = maxIndex; j >= maxLoop; j--)
        {
            long a = j * j;
            long b = leftover - a;
            if (IsPerfectSquare(b))
            {
                numberList.Add(a);
                numberList.Add(b);
            }
        }

        // Update pair count list
        pairCount = numberList.Count / 2; // Each pair contributes two numbers
        if (pairCount >= 4)
        {
            pairCounterTargetSum = targetSum;
            Debug.Log($"Pair Count: {pairCount}");           
        }
        else
        {
            Debug.Log($"Pair Count: {pairCount} (less than 4, no triplets checked)");
            return;
        }

        // Check for triplets (x, y, z) where x + y + z = targetSum
        int tripletCount = 0;
        for (int i = 0; i < numberList.Count; i++)
        {
            for (int j = 0; j < numberList.Count; j++)
            {
                for (int k = 0; k < numberList.Count; k++)
                {
                    long sum = numberList[i] + numberList[j] + numberList[k];
                    if (sum == targetSum)
                    {
                        tripletCount++;
                        Debug.Log($"Triplet found: ({numberList[i]}, {numberList[j]}, {numberList[k]})");
                    }
                }
            }
        }

        Debug.Log($"Total triplets found: {tripletCount}");
    }

    private bool IsPerfectSquare(long number)
    {
        long sqrt = (long)Math.Sqrt(number);
        return sqrt * sqrt == number;
    }   
}