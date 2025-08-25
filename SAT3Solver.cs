using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class SAT3SolverMinimal : MonoBehaviour
{
    public int numVariables = 100;
    public int numClauses = 426;
    public int numInstances = 100;
    public float bestScore = -1f;
    public long iterationCounter;

    private readonly ConcurrentQueue<(bool[] assignment, int satisfiedClauses, float percent, int instanceId, long iterations, string strategy)> solutionQueue = new();
    [SerializeField] private List<SAT3Result> bestSolutions = new();
    private List<Clause[]> instances;
    private int[] instanceResults;
    private bool[] instanceSolved;

    private struct Clause
    {
        public int lit1, lit2, lit3;
        public bool neg1, neg2, neg3;
        public Clause(int l1, bool n1, int l2, bool n2, int l3, bool n3)
        {
            lit1 = l1; neg1 = n1;
            lit2 = l2; neg2 = n2;
            lit3 = l3; neg3 = n3;
        }
    }

    [Serializable]
    public class SAT3Result
    {
        public string name;
        public int satisfiedClauses;
        public float score;
        public float percent;
        public bool[] assignment;

        public SAT3Result(string name, int satisfiedClauses, float score, float percent, bool[] assignment)
        {
            this.name = name;
            this.satisfiedClauses = satisfiedClauses;
            this.score = score;
            this.percent = percent;
            this.assignment = assignment;
        }
    }

    void Start()
    {
        instances = new List<Clause[]>();
        instanceResults = new int[numInstances];
        instanceSolved = new bool[numInstances];
        for (int i = 0; i < numInstances; i++)
        {
            instances.Add(GenerateRandom3SAT(numVariables, numClauses));
            instanceResults[i] = 0;
            instanceSolved[i] = false;
        }
        Debug.Log($"Generated {numInstances} 3-SAT instances with {numVariables} variables and {numClauses} clauses each.");

        Task.Run(RunTask);
        StartCoroutine(ProcessQueue());
    }

    private Clause[] GenerateRandom3SAT(int n, int m)
    {
        Clause[] clauses = new Clause[m];
        System.Random rand = new System.Random();
        for (int i = 0; i < m; i++)
        {
            int v1, v2, v3;
            do { v1 = rand.Next(1, n + 1); v2 = rand.Next(1, n + 1); v3 = rand.Next(1, n + 1); }
            while (v1 == v2 || v2 == v3 || v1 == v3);
            clauses[i] = new Clause(v1, rand.Next(2) == 0, v2, rand.Next(2) == 0, v3, rand.Next(2) == 0);
        }
        return clauses;
    }

    private int EvaluateAssignment(Clause[] instance, bool[] assignment)
    {
        int satisfied = 0;
        for (int c = 0; c < instance.Length; c++)
        {
            var clause = instance[c];
            bool lit1Val = assignment[clause.lit1 - 1] ^ clause.neg1;
            bool lit2Val = assignment[clause.lit2 - 1] ^ clause.neg2;
            bool lit3Val = assignment[clause.lit3 - 1] ^ clause.neg3;
            if (lit1Val || lit2Val || lit3Val) satisfied++;
        }
        return satisfied;
    }

    private void RunTask()
    {
        System.Random rand = new System.Random();
        long maxIterationsPerStrategy = numVariables * numVariables * 100;

        while (true)
        {
            int instanceId = -1;
            for (int i = 0; i < numInstances; i++)
            {
                if (!instanceSolved[i]) { instanceId = i; break; }
            }
            if (instanceId == -1)
            {
                Debug.Log("All instances solved!");
                break;
            }

            Clause[] instance = instances[instanceId];
            long localIterations = 0;
            bool[] assignment = new bool[numVariables];
            for (int i = 0; i < numVariables; i++) assignment[i] = rand.Next(2) == 0;
            int bestSatisfied = EvaluateAssignment(instance, assignment);

            while (localIterations < maxIterationsPerStrategy)
            {
                Interlocked.Increment(ref iterationCounter);
                localIterations++;

                List<int> unsatisfiedClauses = new List<int>();
                for (int c = 0; c < instance.Length; c++)
                {
                    var clause = instance[c];
                    bool lit1Val = assignment[clause.lit1 - 1] ^ clause.neg1;
                    bool lit2Val = assignment[clause.lit2 - 1] ^ clause.neg2;
                    bool lit3Val = assignment[clause.lit3 - 1] ^ clause.neg3;
                    if (!(lit1Val || lit2Val || lit3Val)) unsatisfiedClauses.Add(c);
                }
                if (unsatisfiedClauses.Count == 0)
                {
                    bestSatisfied = numClauses;
                    break;
                }

                int clauseIdx = unsatisfiedClauses[rand.Next(unsatisfiedClauses.Count)];
                var selectedClause = instance[clauseIdx];
                int[] vars = { selectedClause.lit1 - 1, selectedClause.lit2 - 1, selectedClause.lit3 - 1 };

                if (rand.NextDouble() < 0.9)
                {
                    int bestVar = vars[0];
                    int minUnsatisfied = int.MaxValue;
                    bool[] tempAssignment = (bool[])assignment.Clone();
                    foreach (int var in vars)
                    {
                        tempAssignment[var] = !tempAssignment[var];
                        int newUnsatisfied = 0;
                        for (int c = 0; c < instance.Length; c++)
                        {
                            var clause = instance[c];
                            bool lit1Val = tempAssignment[clause.lit1 - 1] ^ clause.neg1;
                            bool lit2Val = tempAssignment[clause.lit2 - 1] ^ clause.neg2;
                            bool lit3Val = tempAssignment[clause.lit3 - 1] ^ clause.neg3;
                            if (!(lit1Val || lit2Val || lit3Val)) newUnsatisfied++;
                        }
                        if (newUnsatisfied < minUnsatisfied)
                        {
                            minUnsatisfied = newUnsatisfied;
                            bestVar = var;
                        }
                        tempAssignment[var] = assignment[var];
                    }
                    assignment[bestVar] = !assignment[bestVar];
                }
                else
                {
                    int flipVar = vars[rand.Next(3)];
                    assignment[flipVar] = !assignment[flipVar];
                }

                int newSatisfied = EvaluateAssignment(instance, assignment);
                if (newSatisfied > bestSatisfied) bestSatisfied = newSatisfied;
                if (bestSatisfied == numClauses) break;
            }

            float percent = (float)bestSatisfied / numClauses * 100f;
            solutionQueue.Enqueue((assignment.ToArray(), bestSatisfied, percent, instanceId, localIterations, "WalkSAT"));

            if (bestSatisfied == numClauses) instanceSolved[instanceId] = true;
        }
    }

    private IEnumerator ProcessQueue()
    {
        while (true)
        {
            if (solutionQueue.TryDequeue(out var result))
            {
                float baseScore = result.satisfiedClauses * 100f + result.percent;
                if (result.satisfiedClauses == numClauses) baseScore += 1000f;
                float score = baseScore - (result.iterations / 1000f);

                instanceResults[result.instanceId] = result.satisfiedClauses;
                int perfectSolutions = instanceResults.Count(r => r == numClauses);
                float successRate = (float)perfectSolutions / numInstances * 100f;
                float displayScore = score * (successRate / 100f);

                string name = $"Instance {result.instanceId}: Satisfied: {result.satisfiedClauses}/{numClauses} | Score: {displayScore:F2} | Percent: {result.percent:F2}% | Success Rate: {successRate:F2}% | Strategy: WalkSAT";
                var newResult = new SAT3Result(name, result.satisfiedClauses, score, result.percent, result.assignment);

                bestSolutions.Add(newResult);
                bestSolutions.Sort((a, b) => b.score.CompareTo(a.score));
                if (bestSolutions.Count > 10) bestSolutions.RemoveRange(10, bestSolutions.Count - 10);

                if (score > bestScore)
                {
                    bestScore = score;
                    Debug.Log($"New High Score: {displayScore:F2} | Instance {result.instanceId} | Satisfied: {result.satisfiedClauses}/{numClauses} | " +
                              $"Percent: {result.percent:F2}% | Success Rate: {successRate:F2}% | Strategy: WalkSAT | Iterations: {iterationCounter}");
                }
            }
            yield return null;
        }
    }
}