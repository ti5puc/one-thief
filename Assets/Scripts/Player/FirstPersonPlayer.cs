using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class FirstPersonPlayer : MonoBehaviour
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

    private bool isTrapModeActive = false;
    private GameObject currentGhostObject;
    private int selectedObjectIndex = -1;

    [Header("Referências InputSystem")]

    public InputActionReference moveInput;
    public InputActionReference trapModeToggleInput;
    public InputActionReference placeObjectInput;
    public InputActionReference selectObject1Input;
    public InputActionReference selectObject2Input;


    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        trapModeToggleInput.action.Enable();
        trapModeToggleInput.action.performed += ToggleTrapMode;

        placeObjectInput.action.Enable();
        placeObjectInput.action.performed += PlaceTrap;

        selectObject1Input.action.Enable();
        selectObject1Input.action.performed += ctx => SelectObject(0);

        selectObject2Input.action.Enable();
        selectObject2Input.action.performed += ctx => SelectObject(1);
    }

    private void OnDisable()
    {
        trapModeToggleInput.action.Disable();
        trapModeToggleInput.action.performed -= ToggleTrapMode;

        placeObjectInput.action.Disable();
        placeObjectInput.action.performed -= PlaceTrap;

        selectObject1Input.action.Disable();
        selectObject1Input.action.performed -= ctx => SelectObject(0);

        selectObject2Input.action.Disable();
        selectObject2Input.action.performed -= ctx => SelectObject(1);
    }

    public void OnMouseX(InputAction.CallbackContext context)
    {
        float deltaX = context.ReadValue<float>() * senseX;
        transform.Rotate(0f, deltaX, 0f);
    }

    public void OnMouseY(InputAction.CallbackContext context)
    {
        float deltaY = context.ReadValue<float>() * senseY;
        float newXRotation = cameraTransform.localEulerAngles.x - deltaY;
        if(newXRotation > 180)
        {
            newXRotation -= 360;
        }
        newXRotation = Mathf.Clamp(newXRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(newXRotation, 0f, 0f);
    }

    void Update()
    {
        if(isTrapModeActive && currentGhostObject != null)
        {
            UpdateGhostPosition();
        }
    }

    void FixedUpdate()
    {
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
            if(selectedObjectIndex != -1)
            {
                InstantiateGhostObject();
            }
        }
        else
        {
            DestroyGhostObject();
        }
    }

    private void SelectObject(int index)
    {
        if(index < 0 || index >= TrapsSettings.Count)
        {
            Debug.LogWarning($"Índice de objeto {index} é inválido.");
            return;
        }

        selectedObjectIndex = index;
        Debug.Log($"Objeto selecionado: {TrapsSettings[selectedObjectIndex].name}");

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
        if(currentGhostObject != null)
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
                float yOffset = ghostRenderer.bounds.size.y / 2f;
                snappedPosition.y += yOffset;
            }

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

        if (selectedObjectIndex < 0 || selectedObjectIndex >= TrapsSettings.Count)
            return;
        var trapSettings = TrapsSettings[selectedObjectIndex];
        if (trapSettings.TrapObject == null)
            return;
        var trapObj = Instantiate(trapSettings.TrapObject, currentGhostObject.transform.position, currentGhostObject.transform.rotation);
        // Set layer from TrapSurface
        int surfaceLayer = trapSettings.TrapSurface != 0 ? Mathf.RoundToInt(Mathf.Log(trapSettings.TrapSurface.value, 2)) : 0;
        trapObj.layer = surfaceLayer;
        foreach (Transform child in trapObj.transform)
        {
            child.gameObject.layer = surfaceLayer;
        }
        Debug.Log("Objeto posicionado!");
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        _moveDirection = context.ReadValue<Vector2>();
    }
}
