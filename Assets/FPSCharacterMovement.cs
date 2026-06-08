using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    [Header("References")]
    public Transform playerCamera;

    [Header("Weapon")]
    public Transform weapon;
    public Transform hipPosition;
    public Transform aimPosition;
    public float weaponTransitionSpeed = 12f;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float jumpHeight = 1.5f;
    public float gravity = -20f;

    [Header("Look")]
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 90f;

    private CharacterController controller;
    private Vector3 velocity;
    private float cameraPitch;

    private void Start()
    {
        controller = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        Look();
        Move();
        UpdateWeaponPosition();
    }

    private void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);

        playerCamera.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void Move()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        float speed = Input.GetKey(KeyCode.LeftShift)
            ? sprintSpeed
            : moveSpeed;

        Vector3 moveDirection =
            (transform.right * x + transform.forward * z).normalized;

        controller.Move(moveDirection * speed * Time.deltaTime);

        if (controller.isGrounded)
        {
            if (velocity.y < 0f)
                velocity.y = -2f;

            if (Input.GetButtonDown("Jump"))
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void UpdateWeaponPosition()
    {
        if (weapon == null || hipPosition == null || aimPosition == null)
            return;

        bool aiming = Input.GetMouseButton(1);

        Transform target = aiming ? aimPosition : hipPosition;

        weapon.localPosition = Vector3.Lerp(
            weapon.localPosition,
            target.localPosition,
            weaponTransitionSpeed * Time.deltaTime);

        weapon.localRotation = Quaternion.Slerp(
            weapon.localRotation,
            target.localRotation,
            weaponTransitionSpeed * Time.deltaTime);
    }
}