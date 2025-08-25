using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using UnityEngine;

public class Match8FinderBigInt : MonoBehaviour
{
    public string targetSum;
    public float bestScore = -1f;
    public int pairCount;
    public string pairCounterTargetSum;
    public long counter;

    public void Start() { Task.Run(RunTask); }

     public void RunTask()
    {
        Debug.Log("Started");
        BigInteger incrementer = 243061325;
        BigInteger center = incrementer * incrementer;
        BigInteger localTargetSum = center * 3;
        BigInteger leftover = localTargetSum - center;      

        BigInteger maxIndex = BigIntegerSqrt(leftover);
        BigInteger maxLoop = BigIntegerSqrt(leftover / 2) + 1;
        Debug.Log("Started");
        var pairGroups = new List<(BigInteger a, BigInteger b)>();
        for (BigInteger i = maxIndex; i >= maxLoop; i--)
        {
            counter++;
            BigInteger a = i * i;
            BigInteger b = leftover - a;
            if (IsPerfectSquareBigInt(b))
            {
                pairGroups.Add((a, b));
                pairGroups.Add((b, a));
            }
        }


        targetSum = localTargetSum.ToString();

        if (pairGroups.Count > pairCount)
        {
            pairCount = pairGroups.Count;
            pairCounterTargetSum = localTargetSum.ToString();
            Debug.Log($"Pair Count: {pairGroups.Count} / 2 = : {pairGroups.Count / 2}, TargetSum: {localTargetSum}");
        }

        Debug.Log("Finished");
    }

    private bool IsPerfectSquareBigInt(BigInteger number)
    {
        if (number < 0) return false;
        BigInteger sqrt = BigIntegerSqrt(number);
        return sqrt * sqrt == number;
    }

    private BigInteger BigIntegerSqrt(BigInteger number)
    {      

        // Convert to double for initial guess
        double doubleApprox = Math.Sqrt((double)BigInteger.Log(number) * Math.Log(10));
        BigInteger guess = BigInteger.Parse(Math.Floor(Math.Exp(doubleApprox)).ToString());

        // Newton's method for refinement
        for (int i = 0; i < 10; i++)
        {
            BigInteger newGuess = (guess + number / guess) / 2;
            if (newGuess == guess || newGuess == guess + 1) break;
            guess = newGuess;
        }

        // Ensure floor of square root
        while (guess * guess > number)
        {
            guess--;
        }
        while ((guess + 1) * (guess + 1) <= number)
        {
            guess++;
        }
        return guess;
    }
}