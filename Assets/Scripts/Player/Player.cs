using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public static event Action<bool> OnTrapModeChanged; // bool isTrapModeActive
    public static event Action<string> OnSelectedTrapChanged; // string trapName

    //---------------------------------- Inicio Movimentacao e Camera ----------------------------------
    [Header("Câmera")]
    public float senseX;
    public float senseY;
    public Transform cameraTransform;

    [Header("Movimento")]
    public float moveSpeed = 3f;
    private Vector2 _moveDirection;
    public Rigidbody rb;

    [Header("Crouch")]
    public bool canCrouch = true;
    public bool isCrouching = false;
    public float defaultStandHeight = 2f;
    public float crouchHeight = 0.8f;
    public float cameraCrouchOffset = -0.4f;
    public float crouchSpeedMultiplier = 0.55f;
    public bool canSlide = false;

    [Header("GroundCheck")]
    public LayerMask groundLayer;
    public CapsuleCollider capsule;
    public float groundProbeUp = 0.05f; // Começa um pouco acima da capsula
    public float groundProbeDown = 0.50f; // Deteccao um pouco abaixo da capsula
    public bool isGrounded;
    public float lastGroundedTime;
    public RaycastHit lastGroundHit;

    [Header("JumpAndDoubleJump")]
    public bool canJump = true;
    public bool canDoubleJump = false;
    public int maxAirJumps = 2;         // Total de pulos no ar
    public int airJumpsRemaining = 0;   // Pulos no ar restantes
    public float jumpForce = 5f;
    public float airJumpForceMultiplier = 0.9f;
    public float lastJumpPressedTime;
    public float coyoteTime = 0.1f;      // Tempo após sair do chão que o pulo ainda é permitido   
    public float jumpBufferTime = 0.1f;  // Tempo pertmitido para o jogador apertar pulo antes de estar no chão

    [Header("Sprint")]
    public bool isSprinting = false;
    public bool canSprint = false;
    public float sprintMultiplier = 1.7f;

    [Header("Dash")]
    public bool canDash = true;
    public bool isDashing = false;
    public float dashSpeed = 13f;
    public float dashCooldown = 2f;
    public float dashDuration = 0.2f;
    public float lastDashTime;
    public float dashEndTime;
    private Vector3 dashDirection;
    public float dashMaxSlopeAngle = 45f;     // (implementar) inclinação máxima que o dash sobe uma campa
    public float dashMaxStepHeight = 0.35f;   // (implementar) altura máxima de degrau que o dash sobe
    public float dashGroundTestUp = 1.2f;    // (implementar) teste para movimentação em rampa

    //Referencias para o InputSystem
    [Header("ReferenciasMovimentacaoECamera")]
    public InputActionReference jumpInput;
    public InputActionReference sprintInput;
    public InputActionReference dashInput;
    public InputActionReference crouchInput;

    //---------------------------------- Fim Movimentacao e Camera ----------------------------------

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
    private int selectedObjectIndex = 0;
    private int selectedPlaceObjectIndex = 0;
    private PlayerDeathIdentifier deathIdentifier;
    private List<GameObject> trapPreviews = new();
    private List<GameObject> currentGhostObjects = new();

    public bool IsTrapModeActive => isTrapModeActive && (deathIdentifier == null || !deathIdentifier.IsDead);

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        deathIdentifier = GetComponent<PlayerDeathIdentifier>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        capsule = GetComponent<CapsuleCollider>();

        // Evita problemas de pulo ao iniciar
        lastJumpPressedTime = -999f;
        lastGroundedTime = -999f;
        airJumpsRemaining = 0;
    }

    private void OnEnable()
    {

        trapModeToggleInput.action.Enable();
        trapModeToggleInput.action.performed += ToggleTrapMode;

        placeObjectInput.action.Enable();
        placeObjectInput.action.performed += PlaceTrap;

        switchTrapInput.action.Enable();
        switchTrapInput.action.performed += SelectObject;

        //---------------------------------- Início da Ativação do Input System de Movimentacao e Camera ----------------------------------

        mouseXInput.action.Enable();
        mouseXInput.action.performed += OnMouseX;

        mouseYInput.action.Enable();
        mouseYInput.action.performed += OnMouseY;

        moveInput.action.Enable();
        moveInput.action.performed += OnMove;

        if (jumpInput)
        {
            jumpInput.action.Enable();
            jumpInput.action.performed += OnJump;
        }
        if (dashInput)
        {
            dashInput.action.Enable();
            dashInput.action.performed += OnDash;
        }
        if (crouchInput)
        {
            crouchInput.action.Enable();
            //crouchInput.action.performed += OnCrouchPress;
            //crouchInput.action.canceled += OnCrouchRelease;
        }
        if (sprintInput)
        {
            sprintInput.action.Enable();
            sprintInput.action.performed += OnSprintPress;
            sprintInput.action.canceled += OnSprintRelease;
        }


        //---------------------------------- Fim da Ativação do Input System de Movimentacao e Camera ----------------------------------
    }

    private void OnDisable()
    {

        trapModeToggleInput.action.Disable();
        trapModeToggleInput.action.performed -= ToggleTrapMode;

        placeObjectInput.action.Disable();
        placeObjectInput.action.performed -= PlaceTrap;

        switchTrapInput.action.Disable();
        switchTrapInput.action.performed -= SelectObject;


        //---------------------------------- Início da Desativação do Input System de Movimentacao e Camera ----------------------------------

        mouseXInput.action.Disable();
        mouseXInput.action.performed -= OnMouseX;

        mouseYInput.action.Disable();
        mouseYInput.action.performed -= OnMouseY;

        moveInput.action.Disable();
        moveInput.action.performed -= OnMove;

        if (jumpInput)
        {
            jumpInput.action.performed -= OnJump;
            jumpInput.action.Disable();
        }

        if (dashInput)
        {
            dashInput.action.performed -= OnDash;
            dashInput.action.Disable();
        }

        if (crouchInput)
        {
            //crouchInput.action.performed -= OnCrouchPress;
            //crouchInput.action.canceled -= OnCrouchRelease;
            crouchInput.action.Disable();
        }

        if (sprintInput)
        {
            sprintInput.action.performed -= OnSprintPress;
            sprintInput.action.canceled -= OnSprintRelease;
            sprintInput.action.Disable();
        }
        //----------------------------- Fim da Desativação do Input System de Movimentacao e Camera -----------------------------
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

        if (isTrapModeActive && currentGhostObjects != null && currentGhostObjects.Count > 0)
        {
            UpdateGhostPosition();
        }
        //-------------------------------- Inicio do Update de Movimentacao --------------------------------

        //Sempre atualizar o GroundCheck
        GroundCheck();

        // Recarrega as cargas quando encosta no chão
        if (isGrounded)
            airJumpsRemaining = maxAirJumps;

        // Verifica se há pulo no buffer
        bool jumpBuffered = false;
        if ((Time.time - lastJumpPressedTime) <= jumpBufferTime)
            jumpBuffered = true;

        if (jumpBuffered && canJump)
        {
            // 1) Pulo do chão
            if ((Time.time - lastGroundedTime) <= coyoteTime)
            {
                DoJump(false);              // pulo do chão
                lastJumpPressedTime = -999f; // consome o buffer
            }
            else
            {
                // 2) Pulos no ar (cargas)
                if (airJumpsRemaining > 0)
                {
                    DoJump(true);           // pulo aéreo
                    airJumpsRemaining = airJumpsRemaining - 1;
                    lastJumpPressedTime = -999f; // consome o buffer
                }
            }
        }
        //-------------------------------- Inicio do Update de Movimentacao --------------------------------

    }

    void FixedUpdate()
    {
        if (deathIdentifier != null && deathIdentifier.IsDead) return;

        //Movimentação durante o dash
        if (isDashing)
        {
            Vector3 step = dashDirection * dashSpeed * Time.fixedDeltaTime;
            Vector3 target = rb.position + step;

            rb.MovePosition(target);

            if (Time.time >= dashEndTime)
            {
                isDashing = false;

                // Restaura gravidade
                rb.useGravity = true;

                // Zera o movimento vertical
                var v = rb.linearVelocity; v.y = 0f; rb.linearVelocity = v;
            }
            return; // Durante o dash ignora o resto da movimentação
        }

        //Locomoção normal com Sprint/Crouch
        float speed = moveSpeed;

        if (isCrouching)
        {
            //Sem sprint enquanto agachado
            isSprinting = false;
            speed *= crouchSpeedMultiplier;
        }
        else if (isSprinting && _moveDirection.sqrMagnitude > 0.0001f)
        {
            speed *= sprintMultiplier;
        }

        Vector3 moveDirection = transform.TransformDirection(new Vector3(_moveDirection.x, 0f, _moveDirection.y));

        // Diagonal corrigida
        if (moveDirection.sqrMagnitude > 1f) moveDirection.Normalize();

        Vector3 newPosition = rb.position + moveDirection * speed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);

    }

    //----------------------------- Inicio Funcoes dos Comandos de Movimentacao -----------------------------

    void GroundCheck()
    {
        // Posição da cápsula
        float radius = Mathf.Max(0.05f, capsule.radius * 0.95f);
        float halfH = Mathf.Max(capsule.height * 0.5f, radius);
        Vector3 centerW = transform.TransformPoint(capsule.center);
        Vector3 feet = centerW - Vector3.up * (halfH - radius);

        // origem acima do pé e alcance para baixo
        Vector3 origin = feet + Vector3.up * groundProbeUp;
        float distance = groundProbeUp + groundProbeDown;

        // 1 ÚNICO TESTE: SphereCast curto para baixo
        if (Physics.SphereCast(origin, radius, Vector3.down, out lastGroundHit, distance, groundLayer, QueryTriggerInteraction.Ignore))
        {
            isGrounded = true;
            lastGroundedTime = Time.time;
        }
        else
        {
            isGrounded = false;
        }
    }

    //Pulo
    void DoJump(bool isAirJump)
    {
        // zera componente vertical para consistência
        Vector3 v = rb.linearVelocity;
        v.y = 0f;
        rb.linearVelocity = v;

        float force;
        if (isAirJump)
        {
            force = jumpForce * airJumpForceMultiplier;
        }
        else
        {
            force = jumpForce;
        }

        rb.AddForce(Vector3.up * force, ForceMode.VelocityChange);
    }
    void OnJump(InputAction.CallbackContext ctx)
    {
        lastJumpPressedTime = Time.time;
    }

    //Dash
    void OnDash(InputAction.CallbackContext ctx)
    {
        if (Time.time < lastDashTime + dashCooldown) return;

        isDashing = true;
        isSprinting = false;
        lastDashTime = Time.time;
        dashEndTime = Time.time + dashDuration;

        // Evitando que o dash seja afetado pela inclinação da camera
        Vector3 inputDir = new Vector3(_moveDirection.x, 0f, _moveDirection.y);
        // Direção fixa durante o dash:
        if (inputDir.sqrMagnitude > 0.0001f)
        {
            // Converte o input (local da câmera) para mundo e remove componente vertical
            Vector3 worldDir = cameraTransform.TransformDirection(inputDir);
            dashDirection = Vector3.ProjectOnPlane(worldDir, Vector3.up).normalized;
        }
        else
        {
            // Sem input: dasha para onde a câmera aponta (somente yaw)
            Vector3 camFwd = cameraTransform.forward;
            dashDirection = Vector3.ProjectOnPlane(camFwd, Vector3.up).normalized;
        }

        // Desabilita gravidade durante o dash
        rb.useGravity = false;

        // Movimento vertical zerado durante o dash
        var v = rb.linearVelocity;
        v.y = 0f;
        rb.linearVelocity = v;
    }

    //Crouch
    //void OnCrouchPress(InputAction.CallbackContext ctx) => CrouchOn();
    //void OnCrouchRelease(InputAction.CallbackContext ctx) => CrouchOff();

    void CrouchOn()
    {
        //if (isCrouching) return;
        //isCrouching = true;
        //isSprinting = false;
        //if (capsule)
        //{
        //    capsule.height = crouchHeight;
        //    capsule.center = new Vector3(defaultCapsuleCenter.x, crouchHeight * 0.5f, defaultCapsuleCenter.z);
        //}
        //if (cameraTransform)
        //    cameraTransform.localPosition = defaultCameraLocalPos + new Vector3(0f, cameraCrouchOffset, 0f);
    }

    void CrouchOff()
    {
        //if (!isCrouching) return;

        //// Não levantar se não tiver espaço
        //Vector3 top = transform.position + Vector3.up * defaultCapsuleHeight;
        //bool blocked = Physics.CheckSphere(top, groundCheckRadius * 0.9f, groundLayer);
        //if (blocked) return;

        //isCrouching = false;
        //if (capsule)
        //{
        //    capsule.height = defaultCapsuleHeight;
        //    capsule.center = defaultCapsuleCenter;
        //}
        //if (cameraTransform)
        //    cameraTransform.localPosition = defaultCameraLocalPos;
    }

    //Sprint
    void OnSprintPress(InputAction.CallbackContext ctx)
    {
        if (isCrouching) CrouchOff(); // Correr levanta o player
        isSprinting = true;
    }

    void OnSprintRelease(InputAction.CallbackContext ctx)
    {
        isSprinting = false;
    }

    //----------------------------- Fim Funcoes dos Comandos de Movimentacao -----------------------------

    private void ToggleTrapMode(InputAction.CallbackContext context)
    {
        isTrapModeActive = !isTrapModeActive;
        Debug.Log("Modo de Construção: " + (isTrapModeActive ? "Ativado" : "Desativado"));

        OnTrapModeChanged?.Invoke(isTrapModeActive);

        // toggle trapPreviews visibility
        foreach (var preview in trapPreviews)
        {
            if (preview != null)
                preview.SetActive(isTrapModeActive);
        }

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

        if (isTrapModeActive)
            GameManager.ChangeGameStateToBuilding();
        else
            GameManager.ChangeGameStateToExploring();
    }

    private void SelectObject(InputAction.CallbackContext context)
    {
        if (isTrapModeActive == false) return;

        if (selectedObjectIndex < 0 || selectedObjectIndex >= TrapsSettings.Count)
        {
            Debug.LogWarning($"Índice de objeto {selectedObjectIndex} é inválido.");
            return;
        }

        selectedObjectIndex++;
        if (selectedObjectIndex >= TrapsSettings.Count)
        {
            selectedObjectIndex = 0;
        }

        selectedPlaceObjectIndex = selectedObjectIndex;

        Debug.Log($"Objeto selecionado: {TrapsSettings[selectedObjectIndex].name}");
        OnSelectedTrapChanged?.Invoke(TrapsSettings[selectedObjectIndex].TrapName);

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
        var matrix = trapSettings.PositioningMatrix;
        int rows = matrix.Rows;
        int cols = matrix.Cols;
        currentGhostObjects = new List<GameObject>();
        int ignoreLayer = trapSettings.IgnorePlacementLayer != 0 ? (int)Mathf.Log(trapSettings.IgnorePlacementLayer.value, 2) : 0;
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                var type = matrix[i, j];
                if (type == TrapPositioningType.None) continue;
                GameObject prefab = null;
                if (type == TrapPositioningType.Trap)
                    prefab = trapSettings.TrapPreview;
                else if (type == TrapPositioningType.Spacer)
                    prefab = trapSettings.TrapSpacerPreview;
                if (prefab == null) continue;
                var go = Instantiate(prefab);
                go.SetActive(false); // will be positioned and enabled in UpdateGhostPosition
                currentGhostObjects.Add(go);

                var trapPreview = go.GetComponent<TrapPreview>();
                if (trapPreview != null)
                {
                    foreach (var col in trapPreview.Colliders)
                    {
                        if (col != null)
                            col.gameObject.layer = ignoreLayer;
                    }
                }
            }
        }
    }

    private void DestroyGhostObject()
    {
        if (currentGhostObjects != null)
        {
            foreach (var go in currentGhostObjects)
            {
                if (go != null)
                    Destroy(go);
            }

            currentGhostObjects.Clear();
        }
    }

    private void UpdateGhostPosition()
    {
        Ray ray = cameraTransform.GetComponent<Camera>().ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        LayerMask trapSurfaceLayer = 0;
        if (selectedObjectIndex >= 0 && selectedObjectIndex < TrapsSettings.Count)
        {
            var settings = TrapsSettings[selectedObjectIndex];
            trapSurfaceLayer = settings.TrapSurface | settings.TrapPlacementLayer;
        }

        if (Physics.Raycast(ray, out hit, interactionDistance, trapSurfaceLayer))
        {
            var trapSettings = TrapsSettings[selectedObjectIndex];
            var matrix = trapSettings.PositioningMatrix;
            int rows = matrix.Rows;
            int cols = matrix.Cols;
            int centerRow = rows / 2;
            int centerCol = cols / 2;

            float snappedX = Mathf.Round(hit.point.x / gridSize) * gridSize;
            float snappedY = Mathf.Round(hit.point.y / gridSize) * gridSize;
            float snappedZ = Mathf.Round(hit.point.z / gridSize) * gridSize;
            Vector3 centerPosition = new Vector3(snappedX, snappedY, snappedZ);

            bool allValid = true;
            int ghostIdx = 0;
            // Store per-ghost validity for later pass
            List<(GameObject ghost, TrapPositioningType type, bool isValid)> ghostValidity = new();
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    var type = matrix[i, j];
                    if (type == TrapPositioningType.None) continue;
                    int offsetRow = i - centerRow;
                    int offsetCol = j - centerCol;
                    Vector3 offset = new Vector3(offsetCol * gridSize, 0f, offsetRow * gridSize);
                    if (ghostIdx >= currentGhostObjects.Count)
                    {
                        Debug.LogError($"[UpdateGhostPosition] ghostIdx {ghostIdx} out of range for {currentGhostObjects.Count} ghosts");
                        continue;
                    }
                    GameObject ghost = currentGhostObjects[ghostIdx++];
                    ghost.SetActive(true);
                    ghost.transform.position = centerPosition + offset;
                    ghost.transform.rotation = Quaternion.identity;

                    // Check placement conditions for this cell
                    Vector3 boxCenter = ghost.transform.position;
                    float checkBoxSize = gridSize * 0.45f;
                    Vector3 halfExtents = new Vector3(checkBoxSize, checkBoxSize, checkBoxSize);
                    bool isValid = true;
                    if (type == TrapPositioningType.Trap)
                    {
                        // 1. No overlap with TrapPlacementLayer (should block both Trap and Spacer)
                        bool blockedByPlacement = Physics.CheckBox(boxCenter, halfExtents, ghost.transform.rotation, trapSettings.TrapPlacementLayer);
                        // 2. Must overlap with TrapSurface
                        bool hasSurface = Physics.CheckBox(boxCenter, halfExtents, ghost.transform.rotation, trapSettings.TrapSurface);
                        isValid = !blockedByPlacement && hasSurface;
                    }
                    else if (type == TrapPositioningType.Spacer)
                    {
                        // 1. No overlap with TrapPlacementLayer (should block both Trap and Spacer)
                        bool blockedByPlacement = Physics.CheckBox(boxCenter, halfExtents, ghost.transform.rotation, trapSettings.TrapPlacementLayer);
                        // 2. No overlap with InvalidSurfacesForSpacer
                        bool blockedByInvalidSurface = Physics.CheckBox(boxCenter, halfExtents, ghost.transform.rotation, trapSettings.InvalidSurfacesForSpacer);
                        isValid = !blockedByPlacement && !blockedByInvalidSurface;
                    }
                    ghostValidity.Add((ghost, type, isValid));
                    if (!isValid) allValid = false;
                }
            }
            // If any are invalid, mark all as invalid visually
            foreach (var (ghost, type, isValid) in ghostValidity)
            {
                var trapPreview = ghost.GetComponent<TrapPreview>();
                bool showValid = allValid ? isValid : false;
                if (trapPreview != null)
                {
                    if (showValid)
                        trapPreview.SetValid();
                    else
                        trapPreview.SetInvalid();
                }
            }
            canPlaceObject = allValid;
        }
        else
        {
            foreach (var go in currentGhostObjects)
                if (go != null) go.SetActive(false);
            canPlaceObject = false;
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !isTrapModeActive || currentGhostObjects == null || currentGhostObjects.Count == 0) return;

        // debug ghost valid positioning for all ghosts
        foreach (var ghost in currentGhostObjects)
        {
            if (ghost == null) continue;
            Vector3 boxCenter = ghost.transform.position;
            float checkBoxSize = gridSize * 0.45f;
            Vector3 halfExtents = new Vector3(checkBoxSize, checkBoxSize, checkBoxSize);
            bool isBlocked = Physics.CheckBox(boxCenter, halfExtents, ghost.transform.rotation, collisionCheckLayer);
            Gizmos.color = isBlocked ? new Color(1f, 0f, 0f, 0.4f) : new Color(0f, 1f, 0f, 0.4f);
            Gizmos.matrix = Matrix4x4.TRS(boxCenter, ghost.transform.rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, halfExtents * 2);
        }
        Gizmos.color = Color.white;
        Gizmos.matrix = Matrix4x4.identity;
    }

    private void PlaceTrap(InputAction.CallbackContext context)
    {
        if (!isTrapModeActive || !canPlaceObject || currentGhostObjects == null || currentGhostObjects.Count == 0)
            return;
        if (selectedPlaceObjectIndex < 0 || selectedPlaceObjectIndex >= TrapsSettings.Count)
            return;

        var trapSettings = TrapsSettings[selectedPlaceObjectIndex];
        var matrix = trapSettings.PositioningMatrix;
        int rows = matrix.Rows;
        int cols = matrix.Cols;
        int centerRow = rows / 2;
        int centerCol = cols / 2;

        // Place all objects in the matrix
        int ghostIdx = 0;
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                var type = matrix[i, j];
                if (type == TrapPositioningType.None) continue;
                int offsetRow = i - centerRow;
                int offsetCol = j - centerCol;
                Vector3 offset = new Vector3(offsetCol * gridSize, 0f, offsetRow * gridSize);
                if (ghostIdx >= currentGhostObjects.Count)
                {
                    Debug.LogError($"[PlaceTrap] ghostIdx {ghostIdx} out of range for {currentGhostObjects.Count} ghosts");
                    continue;
                }
                GameObject ghost = currentGhostObjects[ghostIdx++];
                Vector3 pos = ghost.transform.position;
                Quaternion rot = ghost.transform.rotation;
                if (type == TrapPositioningType.Trap && trapSettings.TrapObject != null)
                {
                    Instantiate(trapSettings.TrapObject, pos, rot);
                    var pointer = Instantiate(trapSettings.TrapPreview, pos, rot);
                    pointer.SetActive(isTrapModeActive);
                    trapPreviews.Add(pointer);
                }
                else if (type == TrapPositioningType.Spacer && trapSettings.TrapSpacerPreview != null)
                {
                    var pointer = Instantiate(trapSettings.TrapSpacerPreview, pos, rot);
                    pointer.SetActive(isTrapModeActive);
                    trapPreviews.Add(pointer);
                }
            }
        }

        Debug.Log($"[PlaceTrap] Placement completed for {trapSettings.TrapObject.name}");
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (deathIdentifier != null && deathIdentifier.IsDead) return;

        _moveDirection = context.ReadValue<Vector2>();
    }
}
