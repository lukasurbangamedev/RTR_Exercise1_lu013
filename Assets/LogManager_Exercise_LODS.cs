using UnityEngine;
using System.IO;

public class LogManager_Exercise_LODS : MonoBehaviour
{
    [field: SerializeField] public Transform horse { get; private set; }
    [field: SerializeField] public CameraController cameraController { get; private set; }

    [field: SerializeField] public LODGroup lodGroup { get; private set; }

    private void OnValidate()
    {
        
    }


    string filePath;

    void Start()
    {
        filePath = Application.dataPath + "/lods_log.csv";

        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, "deltaTimeS|cameraDistance|lod_level\n");
        }



    }


    Quaternion rotatorCurrent;
    Quaternion rotatorLast;
    void Update()
    {
        float deltaTimeS = Time.deltaTime;
        float cameraDistance = Vector3.Distance(cameraController.transform.position, horse.position);
        Vector3 cameraPosition = cameraController.transform.position;


        int lodIdx = GetActiveLODByRenderer();

        string line = deltaTimeS + "|" + cameraDistance + "|" + lodIdx + "\n";
        File.AppendAllText(filePath, line);

    }


    public int GetActiveLODByRenderer()
    {
        LOD[] lods = lodGroup.GetLODs();

        for (int i = 0; i < lods.Length; i++)
        {
            foreach (Renderer r in lods[i].renderers)
            {
                if (r != null && r.isVisible) return i;
            }
        }

        return -1;
    }




}
