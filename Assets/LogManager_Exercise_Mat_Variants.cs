using System.IO;
using UnityEditor;
using UnityEngine;

public class LogManager_Exercise_Mat_Variants : MonoBehaviour
{
    [field: SerializeField] public GPUInstancingBenchmark instancingBenchmark;
    [field: SerializeField] public CameraController_Ex04 cameraController;

    private void OnValidate()
    {

    }


    string filePath;

    void Start()
    {
        filePath = Application.dataPath + "/mat_variants_log.csv";

        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, "FPS|deltaTimeS|materialMode|cameraView|totalObjectCount|visibleObjectCount|culledObjectCount|\n");
        }



    }


    Quaternion rotatorCurrent;
    Quaternion rotatorLast;
    void Update()
    {
        float deltaTimeS = Time.deltaTime;
        float fps = 1.0f / deltaTimeS;

        string materialMode = instancingBenchmark.materialMode.ToString();

        string cameraView = cameraController.mode.ToString();


        int totalObjectCount = instancingBenchmark._instanceCount;
        int visibleObjectCount = instancingBenchmark._visibleCount;
        int culledObjectCount = instancingBenchmark._culledCount;


        string line = fps + "|" + deltaTimeS + "|" + materialMode + "|" + cameraView + "|" + totalObjectCount + "|" + visibleObjectCount + "|" + culledObjectCount + "\n";
        File.AppendAllText(filePath, line);

    }






}
