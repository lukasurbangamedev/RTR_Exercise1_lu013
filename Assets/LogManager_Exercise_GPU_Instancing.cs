using UnityEngine;
using System.IO;
using UnityEditor;

public class LogManager_Exercise_GPU_Instancing : MonoBehaviour
{
    [field: SerializeField] public GPUInstancingBenchmark instancingBenchmark; 

    private void OnValidate()
    {
        
    }


    string filePath;

    void Start()
    {
        filePath = Application.dataPath + "/instancing_log.csv";

        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, "FPS|deltaTimeS|renderingMode|numTris|numDrawCalls|objectCount\n");
        }



    }


    Quaternion rotatorCurrent;
    Quaternion rotatorLast;
    void Update()
    {
        float deltaTimeS = Time.deltaTime;
        float fps = 1.0f / deltaTimeS;

        string renderingMode = instancingBenchmark.useGPUInstancing ? "instanced" : "non-instanced";

        int numTris = UnityStats.triangles;
        int drawCalls = UnityStats.drawCalls;
        int objectCount = instancingBenchmark._instanceCount;


        string line = fps + "|" + deltaTimeS + "|" +  renderingMode + "|" + numTris + "|" + drawCalls + "|" + objectCount + "\n";
        File.AppendAllText(filePath, line);

    }


    



}
