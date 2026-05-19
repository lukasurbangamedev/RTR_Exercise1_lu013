using UnityEngine;

public class CameraController_Ex04 : MonoBehaviour
{

    public Transform fullView;
    public Transform closeUp;
    public Transform nothing;
    public Transform angle;


    public enum ECameraMode { FULL_View, CLOSE_UP, NOTHING, ANGLE };

    public ECameraMode mode;

    void Start()
    {
        mode = ECameraMode.FULL_View;
    }




    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            mode = ECameraMode.FULL_View;
            this.transform.SetPositionAndRotation(fullView.position, fullView.rotation);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            mode = ECameraMode.ANGLE;
            this.transform.SetPositionAndRotation(angle.position, angle.rotation);
        }
        if (Input.GetKey(KeyCode.Alpha3))
        {
            mode = ECameraMode.CLOSE_UP;
            this.transform.SetPositionAndRotation(closeUp.position, closeUp.rotation);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            mode = ECameraMode.NOTHING;
            this.transform.SetPositionAndRotation(nothing.position, nothing.rotation);
        }
    }




}
