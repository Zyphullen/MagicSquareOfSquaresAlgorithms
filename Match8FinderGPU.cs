using UnityEngine;

public class Match8FinderGPU : MonoBehaviour
{
    public ComputeShader computeShader;
    private ComputeBuffer resultBuffer;
    private ComputeBuffer resultCountBuffer;
    private const int MAX_RESULTS = 1000;
    private const int THREAD_GROUPS_PER_DISPATCH = 65536;

    void Start()
    {
        // Initialize buffers
        resultBuffer = new ComputeBuffer(MAX_RESULTS * 9, sizeof(ulong));
        resultCountBuffer = new ComputeBuffer(1, sizeof(uint));
        uint[] count = new uint[] { 0 };
        resultCountBuffer.SetData(count);

        // Set buffers to shader
        int kernel = computeShader.FindKernel("MainKernel");
        computeShader.SetBuffer(kernel, "resultBuffer", resultBuffer);
        computeShader.SetBuffer(kernel, "resultCount", resultCountBuffer);

        // Run for a range of incrementers
        ulong startIncrementer = 1;
        for (int batch = 0; batch < 10; batch++) // Example: 10 batches
        {
            // Set startIncrementer as two uints
            uint low = (uint)(startIncrementer & 0xFFFFFFFF);
            uint high = (uint)(startIncrementer >> 32);
            computeShader.SetInt("startIncrementerLow", (int)low);
            computeShader.SetInt("startIncrementerHigh", (int)high);

            // Dispatch
            computeShader.Dispatch(kernel, THREAD_GROUPS_PER_DISPATCH, 1, 1);

            // Read results
            resultCountBuffer.GetData(count);
            if (count[0] > 0)
            {
                ulong[] results = new ulong[count[0] * 9];
                resultBuffer.GetData(results, 0, 0, (int)count[0] * 9);
                for (int i = 0; i < count[0]; i++)
                {
                    Debug.Log($"Grid {i}: [{results[i * 9 + 0]}, {results[i * 9 + 1]}, {results[i * 9 + 2]}; " +
                              $"{results[i * 9 + 3]}, {results[i * 9 + 4]}, {results[i * 9 + 5]}; " +
                              $"{results[i * 9 + 6]}, {results[i * 9 + 7]}, {results[i * 9 + 8]}]");
                }
            }

            startIncrementer += THREAD_GROUPS_PER_DISPATCH;
        }
    }

    void OnDestroy()
    {
        resultBuffer?.Release();
        resultCountBuffer?.Release();
    }
}