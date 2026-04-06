using UnityEngine;
using System.IO;

public class LogManager : MonoBehaviour
{
    [field: SerializeField] public RotationController rotationController { get; private set; }
    [field: SerializeField] public CameraController cameraController { get; private set; }

    private void OnValidate()
    {
        
    }


    string filePath;

    void Start()
    {
        filePath = Application.dataPath + "/log.csv";

        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, "deltaTimeS|cameraDistance|cameraPosition|rotationAngularSpeed\n");
        }



    }


    Quaternion rotatorCurrent;
    Quaternion rotatorLast;
    void Update()
    {
        float deltaTimeS = Time.deltaTime;
        float cameraDistance = Vector3.Distance(cameraController.transform.position, rotationController.transform.position);
        Vector3 cameraPosition = cameraController.transform.position;


        rotatorLast = rotatorCurrent;
        rotatorCurrent = rotationController.transform.rotation;

        float rotationAngularSpeed = Quaternion.Angle(rotatorLast, rotatorCurrent) / deltaTimeS;


        string line = deltaTimeS + "|" + cameraDistance + "|" + cameraPosition + "|" + rotationAngularSpeed + "\n";
        File.AppendAllText(filePath, line);

    }
}
