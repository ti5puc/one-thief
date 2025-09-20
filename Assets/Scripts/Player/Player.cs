using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class Player : MonoBehaviour
{
    [Header("Câmera")]
    public float senseX;
    public float senseY;
    public Transform cameraTransform;

    [Header("Movimento")]
    public float moveSpeed = 3f;
    private Vector2 _moveDirection;
    public Rigidbody rb;

    [Header("Construção")]
    public float interactionDistance = 10f;
    public LayerMask collisionCheckLayer;
    public float gridSize = 0.5f;
    private bool canPlaceObject = false;

    [Header("Traps")]
    public List<TrapSettings> TrapsSettings = new();

    [Header("Referências InputSystem")]
    public InputActionReference mouseXInput;
    public InputActionReference mouseYInput;
    public InputActionReference moveInput;
    public InputActionReference trapModeToggleInput;
    public InputActionReference placeObjectInput;
    public InputActionReference switchTrapInput;

    private bool isTrapModeActive = false;
    private GameObject currentGhostObject;
    private int selectedObjectIndex = 0;
    private int selectedPlaceObjectIndex = 0;
    private PlayerDeathIdentifier deathIdentifier;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        deathIdentifier = GetComponent<PlayerDeathIdentifier>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        mouseXInput.action.Enable();
        mouseXInput.action.performed += OnMouseX;

        mouseYInput.action.Enable();
        mouseYInput.action.performed += OnMouseY;

        moveInput.action.Enable();
        moveInput.action.performed += OnMove;

        trapModeToggleInput.action.Enable();
        trapModeToggleInput.action.performed += ToggleTrapMode;

        placeObjectInput.action.Enable();
        placeObjectInput.action.performed += PlaceTrap;

        switchTrapInput.action.Enable();
        switchTrapInput.action.performed += SelectObject;
    }

    private void OnDisable()
    {
        mouseXInput.action.Disable();
        mouseXInput.action.performed -= OnMouseX;

        mouseYInput.action.Disable();
        mouseYInput.action.performed -= OnMouseY;

        moveInput.action.Disable();
        moveInput.action.performed -= OnMove;

        trapModeToggleInput.action.Disable();
        trapModeToggleInput.action.performed -= ToggleTrapMode;

        placeObjectInput.action.Disable();
        placeObjectInput.action.performed -= PlaceTrap;

        switchTrapInput.action.Disable();
        switchTrapInput.action.performed -= SelectObject;
    }

    public void OnMouseX(InputAction.CallbackContext context)
    {
        if (deathIdentifier != null && deathIdentifier.IsDead) return;

        float deltaX = context.ReadValue<float>() * senseX;
        transform.Rotate(0f, deltaX, 0f);
    }

    public void OnMouseY(InputAction.CallbackContext context)
    {
        if (deathIdentifier != null && deathIdentifier.IsDead) return;

        float deltaY = context.ReadValue<float>() * senseY;
        float newXRotation = cameraTransform.localEulerAngles.x - deltaY;
        if (newXRotation > 180)
        {
            newXRotation -= 360;
        }
        newXRotation = Mathf.Clamp(newXRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(newXRotation, 0f, 0f);
    }

    void Update()
    {
        if (deathIdentifier != null && deathIdentifier.IsDead)
        {
            if (isTrapModeActive)
            {
                isTrapModeActive = false;
                DestroyGhostObject();
            }

            return;
        }

        if (isTrapModeActive && currentGhostObject != null)
        {
            UpdateGhostPosition();
        }
    }

    void FixedUpdate()
    {
        if (deathIdentifier != null && deathIdentifier.IsDead) return;

        Vector3 moveDirection = transform.TransformDirection(new Vector3(_moveDirection.x, 0, _moveDirection.y));
        Vector3 newPosition = rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);
    }

    private void ToggleTrapMode(InputAction.CallbackContext context)
    {
        isTrapModeActive = !isTrapModeActive;
        Debug.Log("Modo de Construção: " + (isTrapModeActive ? "Ativado" : "Desativado"));

        if (isTrapModeActive)
        {
            if (selectedObjectIndex != -1)
            {
                InstantiateGhostObject();
            }
        }
        else
        {
            DestroyGhostObject();
        }
    }

    private void SelectObject(InputAction.CallbackContext context)
    {
        if (isTrapModeActive == false) return;

        if (selectedObjectIndex < 0 || selectedObjectIndex >= TrapsSettings.Count)
        {
            Debug.LogWarning($"Índice de objeto {selectedObjectIndex} é inválido.");
            return;
        }

        Debug.Log($"Objeto selecionado: {TrapsSettings[selectedObjectIndex].name}");

        selectedObjectIndex++;
        if (selectedObjectIndex >= TrapsSettings.Count)
        {
            selectedObjectIndex = 0;
        }

        selectedPlaceObjectIndex = selectedObjectIndex;

        if (isTrapModeActive)
        {
            InstantiateGhostObject();
        }
    }

    private void InstantiateGhostObject()
    {
        DestroyGhostObject();
        if (selectedObjectIndex < 0 || selectedObjectIndex >= TrapsSettings.Count)
            return;
        var trapSettings = TrapsSettings[selectedObjectIndex];
        if (trapSettings.TrapPreview == null)
            return;
        currentGhostObject = Instantiate(trapSettings.TrapPreview);
    }

    private void DestroyGhostObject()
    {
        if (currentGhostObject != null)
        {
            Destroy(currentGhostObject);
        }
    }

    private void UpdateGhostPosition()
    {
        Ray ray = cameraTransform.GetComponent<Camera>().ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        // use TrapSurface of selected trap as placementLayer
        LayerMask trapSurfaceLayer = 0;
        if (selectedObjectIndex >= 0 && selectedObjectIndex < TrapsSettings.Count)
        {
            trapSurfaceLayer = TrapsSettings[selectedObjectIndex].TrapSurface;
        }

        if (Physics.Raycast(ray, out hit, interactionDistance, trapSurfaceLayer))
        {
            currentGhostObject.SetActive(true);

            float snappedX = Mathf.Round(hit.point.x / gridSize) * gridSize;
            float snappedY = Mathf.Round(hit.point.y / gridSize) * gridSize;
            float snappedZ = Mathf.Round(hit.point.z / gridSize) * gridSize;

            Vector3 snappedPosition = new Vector3(snappedX, snappedY, snappedZ);

            Renderer ghostRenderer = currentGhostObject.GetComponentInChildren<Renderer>();
            if (ghostRenderer != null)
            {
                // TODO: is this needed?

                // float yOffset = ghostRenderer.bounds.size.y / 2f;
                // snappedPosition.y += yOffset;
            }

            snappedPosition.y = 0f;
            currentGhostObject.transform.position = snappedPosition;

            Vector3 boxCenter = ghostRenderer != null ? ghostRenderer.bounds.center : currentGhostObject.transform.position;
            Vector3 halfExtents = ghostRenderer != null ? ghostRenderer.bounds.extents : Vector3.one * 0.5f;

            bool isValid = !Physics.CheckBox(boxCenter, halfExtents, currentGhostObject.transform.rotation, collisionCheckLayer);
            var trapPreview = currentGhostObject.GetComponent<TrapPreview>();
            if (trapPreview != null)
            {
                if (isValid)
                    trapPreview.SetValid();
                else
                    trapPreview.SetInvalid();
            }
            canPlaceObject = isValid;
        }
        else
        {
            currentGhostObject.SetActive(false);
            canPlaceObject = false;
        }
    }

    private void PlaceTrap(InputAction.CallbackContext context)
    {
        if (!isTrapModeActive || !canPlaceObject || currentGhostObject == null || !currentGhostObject.activeSelf) return;

        if (selectedPlaceObjectIndex < 0 || selectedPlaceObjectIndex >= TrapsSettings.Count)
            return;
        var trapSettings = TrapsSettings[selectedPlaceObjectIndex];
        if (trapSettings.TrapObject == null)
            return;

        var trapPos = currentGhostObject.transform.position;
        Instantiate(trapSettings.TrapObject, trapPos, currentGhostObject.transform.rotation);

        Debug.Log("Objeto posicionado!");
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (deathIdentifier != null && deathIdentifier.IsDead) return;

        _moveDirection = context.ReadValue<Vector2>();
    }
}
