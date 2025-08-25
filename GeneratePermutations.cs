using UnityEngine;
using System.Collections.Generic;

public class GeneratePermutations : MonoBehaviour
{
    public List<string> allCombinations = new List<string>();

    void Start()
    {
        char[] digits = { '2', '4', '0', '8', '6', '3' };
        GeneratePermutation(digits, 0, digits.Length, allCombinations);

        // Output to Unity Console (you can access allCombinations list elsewhere in your code)
        Debug.Log("Generated " + allCombinations.Count + " permutations.");
        foreach (string perm in allCombinations)
        {
            Debug.Log(perm);
        }
    }

    void GeneratePermutation(char[] arr, int index, int n, List<string> result)
    {
        if (index == n)
        {
            result.Add(new string(arr));
            return;
        }

        for (int i = index; i < n; i++)
        {
            Swap(arr, index, i);
            GeneratePermutation(arr, index + 1, n, result);
            Swap(arr, index, i); // Backtrack
        }
    }

    void Swap(char[] arr, int i, int j)
    {
        char temp = arr[i];
        arr[i] = arr[j];
        arr[j] = temp;
    }
}