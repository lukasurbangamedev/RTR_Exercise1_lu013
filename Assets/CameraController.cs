using UnityEngine;


public class CameraController : MonoBehaviour
{
    [Header("Movement")]
    [field: SerializeField] public float moveSpeed { get; private set; } = 10f;
    [field: SerializeField] public float fastMoveMultiplier { get; private set; } = 3f; 

    [Header("Mouse Look")]
    [field: SerializeField] public float mouseSensitivity { get; private set; } = 3f;
    [field: SerializeField] public bool invertY { get; private set; } = false;

    private float yaw;
    private float pitch;

    void Start()
    {
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
    }

    void Update()
    {

        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            yaw += mouseX;
            pitch += invertY ? mouseY : -mouseY;
            pitch = Mathf.Clamp(pitch, -89f, 89f);

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        float y = 0f;

        if (Input.GetKey(KeyCode.E)) y = 1f;
        if (Input.GetKey(KeyCode.Q)) y = -1f;

        Vector3 dir = transform.right * x + transform.forward * z + Vector3.up * y;

        float speed = Input.GetKey(KeyCode.LeftShift) ? moveSpeed * fastMoveMultiplier : moveSpeed;

        transform.position += dir * speed * Time.deltaTime;
    }


}