using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class TSPScoreSystem : MonoBehaviour
{
    [SerializeField] private int numCities = 99;
    [SerializeField] private float mapSize = 10f;
    [SerializeField] private GameObject cityPrefab;
    [SerializeField] private LineRenderer tourLine;
    public long counter;

    private List<Vector2> cities = new List<Vector2>();
    private float[,] distances;
    private ConcurrentQueue<(int[] tour, float distance, int smoothTurns, float score, long counter)> tourQueue = new ConcurrentQueue<(int[], float, int, float, long)>();
    private System.Random random;
    private CancellationTokenSource cts;
    private float bestScore = -1f;

    void Start()
    {
        random = new System.Random();
        mapSize = Mathf.Sqrt(numCities) * 5f;

        if (tourLine == null) { Debug.LogError("LineRenderer not assigned!"); return; }
        tourLine.positionCount = 0;
        tourLine.startWidth = 0.1f;
        tourLine.endWidth = 0.1f;
        tourLine.loop = true;
        tourLine.useWorldSpace = true;
        tourLine.material = new Material(Shader.Find("Sprites/Default"));
        tourLine.startColor = Color.blue;
        tourLine.endColor = Color.blue;

        cts = new CancellationTokenSource();
        GenerateCities();
        PrecomputeDistances();
        VisualizeCities();
        Task.Run(() => RunTSPTask(cts.Token));
        StartCoroutine(ProcessQueue());
    }

    private void GenerateCities()
    {
        cities.Clear();
        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(numCities));
        float cellSize = mapSize / gridSize;
        int cityIndex = 0;
        for (int i = 0; i < gridSize && cityIndex < numCities; i++)
            for (int j = 0; j < gridSize && cityIndex < numCities; j++)
            {
                float x = (i + 0.5f) * cellSize - mapSize / 2;
                float y = (j + 0.5f) * cellSize - mapSize / 2;
                cities.Add(new Vector2(x, y));
                cityIndex++;
            }
    }

    private void PrecomputeDistances()
    {
        distances = new float[numCities, numCities];
        for (int i = 0; i < numCities; i++)
            for (int j = 0; j < numCities; j++)
                distances[i, j] = Vector2.Distance(cities[i], cities[j]);
    }

    private void VisualizeCities()
    {
        foreach (Vector2 city in cities)
            if (cityPrefab != null)
            {
                GameObject cityObj = Instantiate(cityPrefab, new Vector3(city.x, city.y, 0), Quaternion.identity);
                if (cityObj.GetComponent<SpriteRenderer>() is SpriteRenderer sr)
                {
                    sr.color = Color.red;
                    sr.sortingOrder = 1;
                }
            }
    }

    private int[] NearestNeighborTour()
    {
        List<int> tour = new List<int> { 0 };
        HashSet<int> visited = new HashSet<int> { 0 };
        int current = 0;
        while (visited.Count < numCities)
        {
            float minDist = float.MaxValue;
            int nearest = -1;
            for (int i = 0; i < numCities; i++)
                if (!visited.Contains(i) && distances[current, i] < minDist)
                {
                    minDist = distances[current, i];
                    nearest = i;
                }
            if (nearest == -1) break;
            tour.Add(nearest);
            visited.Add(nearest);
            current = nearest;
        }
        return tour.ToArray();
    }

    private float CalculateTourDistance(int[] tour)
    {
        float dist = 0f;
        for (int i = 0; i < tour.Length; i++)
            dist += distances[tour[i], tour[(i + 1) % tour.Length]];
        return dist;
    }

    private int CalculateSmoothTurns(int[] tour)
    {
        int smoothTurns = 0;
        for (int i = 0; i < tour.Length; i++)
        {
            Vector2 dir1 = cities[tour[(i + 1) % tour.Length]] - cities[tour[i]];
            Vector2 dir2 = cities[tour[(i + 2) % tour.Length]] - cities[tour[(i + 1) % tour.Length]];
            float angle = Vector2.Angle(dir1, dir2);
            if (angle >= 135f && angle <= 225f) smoothTurns++;
        }
        return smoothTurns;
    }

    private float CalculateScore(float distance, int smoothTurns)
    {
        return (1f / distance) * 100000f + smoothTurns * 50f;
    }

    private void RunTSPTask(CancellationToken token)
    {
        counter = 0;
        int[] currentTour = NearestNeighborTour();
        float currentDist = CalculateTourDistance(currentTour);
        int smoothTurns = CalculateSmoothTurns(currentTour);
        float score = CalculateScore(currentDist, smoothTurns);
        tourQueue.Enqueue((currentTour, currentDist, smoothTurns, score, counter));

        while (!token.IsCancellationRequested)
        {
            counter++;

            // 2-opt optimization
            bool improved = false;
            for (int i = 1; i < currentTour.Length - 1; i++)
                for (int j = i + 1; j < currentTour.Length; j++)
                {
                    int[] newTour = (int[])currentTour.Clone();
                    Array.Reverse(newTour, i, j - i + 1);
                    float newDist = CalculateTourDistance(newTour);
                    if (newDist < currentDist)
                    {
                        currentTour = newTour;
                        currentDist = newDist;
                        smoothTurns = CalculateSmoothTurns(newTour);
                        score = CalculateScore(newDist, smoothTurns);
                        tourQueue.Enqueue((currentTour, currentDist, smoothTurns, score, counter));
                        improved = true;
                    }
                }

            // Controlled 3-opt perturbation every 1000 iterations
            if (!improved && counter % 1000 == 0)
            {
                int i = random.Next(1, currentTour.Length - 4);
                int j = random.Next(i + 1, currentTour.Length - 2);
                int k = random.Next(j + 11, currentTour.Length);
                int[] newTour = (int[])currentTour.Clone();
                Array.Reverse(newTour, i, j - i + 1);
                Array.Reverse(newTour, j, k - j + 1);
                currentTour = newTour;
                currentDist = CalculateTourDistance(newTour);
                smoothTurns = CalculateSmoothTurns(newTour);
                score = CalculateScore(currentDist, smoothTurns);
                tourQueue.Enqueue((currentTour, currentDist, smoothTurns, score, counter));
            }

            if (counter % 1000 == 0)
                Debug.Log($"Counter: {counter} | Distance: {currentDist:F2} | Smooth Turns: {smoothTurns} | Score: {score:F2}");

            Thread.Sleep(1);
        }
    }

    private IEnumerator ProcessQueue()
    {
        while (true)
        {
            if (tourQueue.TryDequeue(out var result) && result.score > bestScore)
            {
                bestScore = result.score;
                UpdateTourVisualization(result.tour);
                Debug.Log($"New Best - Distance: {result.distance:F2} | Smooth Turns: {result.smoothTurns} | Score: {result.score:F2} | Counter: {result.counter}");
            }
            yield return null;
        }
    }

    private void UpdateTourVisualization(int[] tour)
    {
        if (tourLine == null) return;
        tourLine.positionCount = tour.Length;
        for (int i = 0; i < tour.Length; i++)
            tourLine.SetPosition(i, new Vector3(cities[tour[i]].x, cities[tour[i]].y, -1));
    }

    void OnDestroy()
    {
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
            cts = null;
            while (tourQueue.TryDequeue(out _)) { }
        }
    }
}