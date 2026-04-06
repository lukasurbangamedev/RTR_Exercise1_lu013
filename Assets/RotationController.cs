using UnityEngine;

public class RotationController : MonoBehaviour
{
    [field: SerializeField] public float rotationSpeed;
    [field: SerializeField] public Vector3 rotationDirection;

    void Start()
    {
        
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            print("KeyDown");
            transform.Rotate(rotationDirection * rotationSpeed * Time.deltaTime);

        }

        
    }
}
