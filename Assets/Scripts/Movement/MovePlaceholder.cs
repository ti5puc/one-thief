using UnityEngine;

public class MovePlaceholder : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;

    private Transform cameraTransform;
    private float xRotation = 0f;

    private void Start()
    {
        cameraTransform = GetComponentInChildren<Camera>().transform;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        HandleMovement();
        HandleMouseLook();
    }

    private void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        transform.position += move * moveSpeed * Time.deltaTime;
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }
}
