using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDeathIdentifier : MonoBehaviour
{
    [Header("Death Camera Offset")]
    public float DeathCameraUpOffset = 2f;
    public float DeathCameraBackOffset = 4f;
    public float DeathRotateSpeed = 60f; // degrees per second

    [Header("Vfx")]
    public GameObject DeathVfxPrefab;

    private Rigidbody rigidBody;
    private Transform cameraTransform;
    private bool isDead = false;
    private float vfxOffset;

    public bool IsDead => isDead;
    public float VfxOffset
    {
        get => vfxOffset;
        set => vfxOffset = value;
    }

    private void Awake()
    {
        cameraTransform = GetComponentInChildren<Camera>().transform;
        rigidBody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        // TODO: remove placeholder restart

        if (Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        if (isDead)
        {
            // keep rotating player on Y axis after death
            transform.Rotate(Vector3.up * DeathRotateSpeed * Time.deltaTime);
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
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

        var position = new Vector3(transform.position.x, DeathVfxPrefab.transform.position.y + vfxOffset, transform.position.z);
        Instantiate(DeathVfxPrefab, position, DeathVfxPrefab.transform.rotation);
    }
}
