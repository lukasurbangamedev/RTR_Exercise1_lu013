using System.IO;
using UnityEditor;
using UnityEngine;

public class LogManager_Exercise_FrustumCulling : MonoBehaviour
{
    [field: SerializeField] public GPUInstancingBenchmark instancingBenchmark;
    [field: SerializeField] public CameraController_Ex04 cameraController;

    private void OnValidate()
    {

    }


    string filePath;

    void Start()
    {
        filePath = Application.dataPath + "/frustum_log.csv";

        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, "FPS|deltaTimeS|renderingMode|cameraView|totalObjectCount|visibleObjectCount|culledObjectCount|\n");
        }



    }


    Quaternion rotatorCurrent;
    Quaternion rotatorLast;
    void Update()
    {
        float deltaTimeS = Time.deltaTime;
        float fps = 1.0f / deltaTimeS;

        string renderingMode = instancingBenchmark.useFrustumCulling ? "frustum" : "no";

        int totalObjectCount = instancingBenchmark._instanceCount;
        int visibleObjectCount = instancingBenchmark._visibleCount;
        int culledObjectCount = instancingBenchmark._culledCount;
        string cameraView = cameraController.mode.ToString();


        string line = fps + "|" + deltaTimeS + "|" + renderingMode + "|" + cameraView + "|" + totalObjectCount + "|" + visibleObjectCount + "|" + culledObjectCount + "\n";
        File.AppendAllText(filePath, line);

    }






}
