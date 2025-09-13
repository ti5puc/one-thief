using System.Collections.Generic;
using DG.Tweening;
using Unity.VisualScripting;
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

    [Header("Traps")]
    public List<TrapSettings> TrapsSettings = new();
    public float TrapPlacementDistance = 5f;

    private Rigidbody rigidBody;
    private Transform cameraTransform;
    private float xRotation = 0f;
    private bool isDead = false;
    private bool isMoveActive = true;
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

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        rigidBody.isKinematic = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        if (!isDead)
        {
            HandleMovement();
            HandleMouseLook();

            HandleTrapPlacement();
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
        if (isMoveActive == false) return;

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

    private int selectedTrapIndex = -1;
    private GameObject currentTrapPreview = null;

    private void HandleTrapPlacement()
    {
        // Adjust trap placement distance with mouse wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            TrapPlacementDistance += scroll * 2f; // Change multiplier for sensitivity
            TrapPlacementDistance = Mathf.Clamp(TrapPlacementDistance, 1f, 20f);
            // If preview exists, update its position
            if (currentTrapPreview != null)
            {
                Vector3 previewPos = transform.position + transform.forward * TrapPlacementDistance;
                previewPos.y -= 1;
                currentTrapPreview.transform.position = previewPos;
            }
        }

        // Select trap with keys 1-4
        for (int i = 0; i < TrapsSettings.Count; i++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)))
            {
                if (i < TrapsSettings.Count)
                {
                    selectedTrapIndex = i;

                    if (currentTrapPreview != null)
                        Destroy(currentTrapPreview);

                    var trapPreviewPrefab = TrapsSettings[i].TrapPreview;
                    if (trapPreviewPrefab != null)
                    {
                        Vector3 previewPos = transform.position + transform.forward * TrapPlacementDistance;
                        previewPos.y -= 1;
                        Quaternion previewRot = Quaternion.LookRotation(-transform.forward, Vector3.up);
                        currentTrapPreview = Instantiate(trapPreviewPrefab, previewPos, previewRot, transform);
                    }
                }
            }
        }

        if (currentTrapPreview != null && selectedTrapIndex >= 0)
        {
            if (Input.GetMouseButtonDown(0))
            {
                var trapObjectPrefab = TrapsSettings[selectedTrapIndex].TrapObject;
                if (trapObjectPrefab != null)
                    Instantiate(trapObjectPrefab, currentTrapPreview.transform.position, currentTrapPreview.transform.rotation);

                Destroy(currentTrapPreview);
                currentTrapPreview = null;
                selectedTrapIndex = -1;
            }
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

    public void DisableMove()
    {
        isMoveActive = false;
    }

    public void EnableMove()
    {
        isMoveActive = true;
    }
}
