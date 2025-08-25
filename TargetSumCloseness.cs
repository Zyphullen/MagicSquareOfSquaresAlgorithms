using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class TargetSumCloseness : MonoBehaviour
{
    public string targetSumString;
    public long inc = 1;
    public float bestPercent = -1f;
    public string targetSumPercentClosest;

    [Serializable]
    public class CloseSquareResult
    {
        public string name;
        public long targetSum; // Truncated to long for Inspector
        public long closestSquare;

        public CloseSquareResult(decimal targetSumDecimal, long closestSquare)
        {
            this.targetSum = (long)targetSumDecimal; // Truncate to long for Inspector
            this.closestSquare = closestSquare;
            this.name = $"(target: {(long)targetSumDecimal}, closest square: {closestSquare})";
        }
    }

    public List<CloseSquareResult> magicSquareResults = new List<CloseSquareResult>();
    private readonly ConcurrentQueue<(decimal targetSum, long closestSquare, decimal percentCloseness, float percentClosenessFloat, long difference)> resultQueue = new ConcurrentQueue<(decimal, long, decimal, float, long)>();

    public void Start()
    {
        Task.Run(RunTask);
        StartCoroutine(ProcessQueue());
    }

    private void RunTask()
    {
        while (true)
        {
            // Check for reasonable upper limit (e.g., 10^20)
            if (inc > 1e10) // Arbitrary limit, adjust as needed
            {
                Debug.Log("Reached upper limit for inc. Stopping.");
                break;
            }

            decimal center = (decimal)inc * inc; // Use decimal to avoid overflow
            decimal targetSum = center * 3;

            // Find the closest square number to targetSum
            decimal sqrt = (decimal)Math.Sqrt((double)targetSum); // Approximate sqrt
            long sqrtLong = (long)Math.Floor((double)sqrt);
            long lowerSquare = sqrtLong * sqrtLong;
            long upperSquare = (sqrtLong + 1) * (sqrtLong + 1);

            // Determine which square is closer
            decimal diffLower = Math.Abs(targetSum - (decimal)lowerSquare);
            decimal diffUpper = Math.Abs(targetSum - (decimal)upperSquare);
            long closestSquare = diffLower < diffUpper ? lowerSquare : upperSquare;

            // Calculate percentage closeness using decimal for precision
            decimal difference = Math.Abs(targetSum - (decimal)closestSquare);
            decimal targetSumDecimal = targetSum;
            decimal percentCloseness = difference == 0 ? 100m : (1m - difference / targetSumDecimal) * 100m;
            float percentClosenessFloat = (float)percentCloseness;

            // Only queue results if difference is 0 or 1
            if (difference <= 1)
            {
                resultQueue.Enqueue((targetSum: targetSum, closestSquare: closestSquare, percentCloseness: percentCloseness, percentClosenessFloat: percentClosenessFloat, difference: (long)difference));
            }

            inc++;
        }
    }

    private IEnumerator ProcessQueue()
    {
        while (true)
        {
            while (resultQueue.TryDequeue(out var result))
            {
                decimal targetSum = result.targetSum;
                long closestSquare = result.closestSquare;
                decimal percentCloseness = result.percentCloseness;
                float percentClosenessFloat = result.percentClosenessFloat;
                long difference = result.difference;

                // Add to magicSquareResults
                lock (magicSquareResults)
                {
                    magicSquareResults.Add(new CloseSquareResult(targetSum, closestSquare));
                    Debug.Log($"Added close match: {magicSquareResults[magicSquareResults.Count - 1].name}");
                }

                // Update best percent if this is closer, but only set to 100 if difference is 0
                lock (this)
                {
                    if (percentClosenessFloat > bestPercent && (difference == 0 || percentClosenessFloat < 100f))
                    {
                        bestPercent = difference == 0 ? 100f : percentClosenessFloat;
                        targetSumPercentClosest = $"{percentCloseness:F32}% (target: {(long)targetSum}, closest square: {closestSquare})";
                        Debug.Log($"New best: {targetSumPercentClosest}");
                    }
                    // Update targetSumString
                    targetSumString = ((long)targetSum).ToString();
                }
            }
            yield return null;
        }
    }
}