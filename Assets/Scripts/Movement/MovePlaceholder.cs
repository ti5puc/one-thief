using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MovePlaceholder : MonoBehaviour
{
    [Header("Movement Settings")]
    public float MoveSpeed = 5f;
    public float MouseSensitivity = 2f;

    [Header("Death Camera Offset")]
    public float DeathCameraUpOffset = 2f;
    public float DeathCameraBackOffset = 4f;
    public float DeathRotateSpeed = 60f; // degrees per second

    [Header("Vfx")]
    public GameObject DeathVfxPrefab;
    [SerializeField] private float vfxOffset = -1f;

    private Rigidbody rigidBody;
    private Transform cameraTransform;
    private float xRotation = 0f;
    private bool isDead = false;

    public bool IsDead => isDead;

    private void Awake()
    {
        cameraTransform = GetComponentInChildren<Camera>().transform;
        rigidBody = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        rigidBody.isKinematic = false;


    }

    private void Update()
    {
        if (!isDead)
        {
            HandleMovement();
            HandleMouseLook();
        }
        else
        {
            // Keep rotating player on Y axis after death
            transform.Rotate(Vector3.up * DeathRotateSpeed * Time.deltaTime);
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void HandleMovement()
    {
        if (isDead) return;

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        transform.position += move * MoveSpeed * Time.deltaTime;
    }

    private void HandleMouseLook()
    {
        if (isDead) return;

        float mouseX = Input.GetAxis("Mouse X") * MouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * MouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    public void Death(float customCameraDeathRotationX = 20f, float customCameraDeathOffsetY = 0f, float customCameraDeathOffsetZ = 0f)
    {
        isDead = true;

        Vector3 upOffset = Vector3.up * DeathCameraUpOffset + Vector3.up * customCameraDeathOffsetY;
        Vector3 backOffset = -Vector3.forward * DeathCameraBackOffset + Vector3.back * customCameraDeathOffsetZ;
        Vector3 targetPosition = upOffset + backOffset;

        rigidBody.isKinematic = true;
        var collider = GetComponent<Collider>();
        collider.isTrigger = true;

        cameraTransform.DOLocalMove(targetPosition, 1f).SetEase(Ease.OutQuad);
        cameraTransform.localRotation = Quaternion.Euler(customCameraDeathRotationX, 0f, 0f);

        var position = new Vector3(transform.position.x, transform.position.y + vfxOffset, transform.position.z);
        Instantiate(DeathVfxPrefab, position, DeathVfxPrefab.transform.rotation);
    }
}
