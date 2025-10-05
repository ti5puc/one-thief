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
    public InputActionReference rotateTrapInput;

    private bool isTrapModeActive = false;
    private int selectedTrapIndex = 0;
    private int selectedTrapPlacementIndex = 0;
    private PlayerDeathIdentifier deathIdentifier;
    private List<GameObject> trapPreviews = new();
    private List<GameObject> ghostTrapObjects = new();
    private int ghostTrapRotationQuarterTurns = 0;

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

        rotateTrapInput.action.Enable();
        rotateTrapInput.action.performed += RotateObject;

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

        rotateTrapInput.action.Disable();
        rotateTrapInput.action.performed -= RotateObject;


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
                DestroyGhostTraps();
            }

            return;
        }

        if (isTrapModeActive && ghostTrapObjects != null && ghostTrapObjects.Count > 0)
        {
            UpdateGhostTrapPositions();
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

        foreach (var preview in trapPreviews)
        {
            if (preview != null)
                preview.SetActive(isTrapModeActive);
        }

        if (isTrapModeActive)
        {
            if (selectedTrapIndex != -1)
            {
                CreateGhostTrapsForSelectedTrap();
            }
        }
        else
        {
            DestroyGhostTraps();
        }

        if (isTrapModeActive)
            GameManager.ChangeGameStateToBuilding();
        else
            GameManager.ChangeGameStateToExploring();
    }

    private void SelectObject(InputAction.CallbackContext context)
    {
        if (!isTrapModeActive) return;

        if (selectedTrapIndex < 0 || selectedTrapIndex >= TrapsSettings.Count)
        {
            Debug.LogWarning($"Índice de armadilha {selectedTrapIndex} é inválido.");
            return;
        }

        selectedTrapIndex++;
        if (selectedTrapIndex >= TrapsSettings.Count)
        {
            selectedTrapIndex = 0;
        }

        selectedTrapPlacementIndex = selectedTrapIndex;

        Debug.Log($"Armadilha selecionada: {TrapsSettings[selectedTrapIndex].name}");
        OnSelectedTrapChanged?.Invoke(TrapsSettings[selectedTrapIndex].TrapName);

        if (isTrapModeActive)
        {
            CreateGhostTrapsForSelectedTrap();
        }
    }

    private void CreateGhostTrapsForSelectedTrap()
    {
        DestroyGhostTraps();
        if (selectedTrapIndex < 0 || selectedTrapIndex >= TrapsSettings.Count)
            return;
        var trapSettings = TrapsSettings[selectedTrapIndex];
        var positioningMatrix = trapSettings.PositioningMatrix;
        int totalRows = positioningMatrix.Rows;
        int totalCols = positioningMatrix.Cols;
        ghostTrapObjects = new List<GameObject>();

        int ignoreLayer = trapSettings.IgnorePlacementLayer != 0 ? (int)Mathf.Log(trapSettings.IgnorePlacementLayer.value, 2) : 0;
        ghostTrapRotationQuarterTurns = 0;

        for (int row = 0; row < totalRows; row++)
        {
            for (int col = 0; col < totalCols; col++)
            {
                var cellType = positioningMatrix[row, col];
                if (cellType == TrapPositioningType.None) continue;
                GameObject prefab = null;
                if (cellType == TrapPositioningType.Trap)
                    prefab = trapSettings.TrapPreview;
                else if (cellType == TrapPositioningType.Spacer)
                    prefab = trapSettings.TrapSpacerPreview;
                if (prefab == null) continue;
                var ghostTrap = Instantiate(prefab);
                ghostTrap.SetActive(false); // will be positioned and enabled in UpdateGhostTrapPositions
                ghostTrapObjects.Add(ghostTrap);

                var trapPreview = ghostTrap.GetComponent<TrapPreview>();
                if (trapPreview != null)
                {
                    foreach (var collider in trapPreview.Colliders)
                    {
                        if (collider != null)
                            collider.gameObject.layer = ignoreLayer;
                    }
                }
            }
        }
    }

    private void DestroyGhostTraps()
    {
        if (ghostTrapObjects != null)
        {
            foreach (var ghostTrap in ghostTrapObjects)
            {
                if (ghostTrap != null)
                    Destroy(ghostTrap);
            }
            ghostTrapObjects.Clear();
        }
    }

    private void UpdateGhostTrapPositions()
    {
        Ray ray = cameraTransform.GetComponent<Camera>().ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        LayerMask trapSurfaceLayer = 0;
        if (selectedTrapIndex >= 0 && selectedTrapIndex < TrapsSettings.Count)
        {
            var settings = TrapsSettings[selectedTrapIndex];
            trapSurfaceLayer = settings.TrapSurface | settings.TrapPlacementLayer;
        }

        if (Physics.Raycast(ray, out hit, interactionDistance, trapSurfaceLayer))
        {
            var trapSettings = TrapsSettings[selectedTrapIndex];
            var positioningMatrix = trapSettings.PositioningMatrix;
            int totalRows = positioningMatrix.Rows;
            int totalCols = positioningMatrix.Cols;
            int centerRow = totalRows / 2;
            int centerCol = totalCols / 2;

            float snappedX = Mathf.Round(hit.point.x / gridSize) * gridSize;
            float snappedY = Mathf.Round(hit.point.y / gridSize) * gridSize;
            float snappedZ = Mathf.Round(hit.point.z / gridSize) * gridSize;
            Vector3 centerPosition = new Vector3(snappedX, snappedY, snappedZ);

            bool allCellsValid = true;
            int ghostTrapIdx = 0;
            List<(GameObject ghostTrap, TrapPositioningType cellType, bool isValid)> ghostTrapValidity = new();
            for (int row = 0; row < totalRows; row++)
            {
                for (int col = 0; col < totalCols; col++)
                {
                    var cellType = positioningMatrix[row, col];
                    if (cellType == TrapPositioningType.None) continue;
                    Vector3 offset = GetRotatedTrapOffset(row, col, centerRow, centerCol, ghostTrapRotationQuarterTurns);
                    if (ghostTrapIdx >= ghostTrapObjects.Count)
                    {
                        Debug.LogError($"[UpdateGhostTrapPositions] ghostTrapIdx {ghostTrapIdx} out of range for {ghostTrapObjects.Count} ghosts");
                        continue;
                    }
                    GameObject ghostTrap = ghostTrapObjects[ghostTrapIdx++];
                    ghostTrap.SetActive(true);
                    ghostTrap.transform.position = centerPosition + offset;
                    float yRot = 90f * ghostTrapRotationQuarterTurns;
                    ghostTrap.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

                    bool isValid = IsTrapCellPlacementValid(cellType, ghostTrap.transform.position, ghostTrap.transform.rotation, trapSettings);
                    ghostTrapValidity.Add((ghostTrap, cellType, isValid));
                    if (!isValid) allCellsValid = false;
                }
            }
            foreach (var (ghostTrap, cellType, isValid) in ghostTrapValidity)
            {
                var trapPreview = ghostTrap.GetComponent<TrapPreview>();
                bool showValid = allCellsValid ? isValid : false;
                if (trapPreview != null)
                {
                    if (showValid)
                        trapPreview.SetValid();
                    else
                        trapPreview.SetInvalid();
                }
            }
            canPlaceObject = allCellsValid;
        }
        else
        {
            foreach (var ghostTrap in ghostTrapObjects)
                if (ghostTrap != null) ghostTrap.SetActive(false);
            canPlaceObject = false;
        }
    }

    private Vector3 GetRotatedTrapOffset(int row, int col, int centerRow, int centerCol, int quarterTurns)
    {
        int relRow = row - centerRow;
        int relCol = col - centerCol;
        int rotRow = relRow, rotCol = relCol;
        for (int r = 0; r < quarterTurns; r++)
        {
            int temp = rotRow;
            rotRow = -rotCol;
            rotCol = temp;
        }
        return new Vector3(rotCol * gridSize, 0f, rotRow * gridSize);
    }

    private bool IsTrapCellPlacementValid(TrapPositioningType cellType, Vector3 position, Quaternion rotation, TrapSettings trapSettings)
    {
        float checkBoxSize = gridSize * 0.45f;
        Vector3 halfExtents = new Vector3(checkBoxSize, checkBoxSize, checkBoxSize);
        if (cellType == TrapPositioningType.Trap)
        {
            bool blockedByPlacement = Physics.CheckBox(position, halfExtents, rotation, trapSettings.TrapPlacementLayer);
            bool hasSurface = Physics.CheckBox(position, halfExtents, rotation, trapSettings.TrapSurface);
            return !blockedByPlacement && hasSurface;
        }
        else if (cellType == TrapPositioningType.Spacer)
        {
            bool blockedByPlacement = Physics.CheckBox(position, halfExtents, rotation, trapSettings.TrapPlacementLayer);
            bool blockedByInvalidSurface = Physics.CheckBox(position, halfExtents, rotation, trapSettings.InvalidSurfacesForSpacer);
            return !blockedByPlacement && !blockedByInvalidSurface;
        }
        return true;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !isTrapModeActive || ghostTrapObjects == null || ghostTrapObjects.Count == 0) return;

        foreach (var ghostTrap in ghostTrapObjects)
        {
            if (ghostTrap == null) continue;
            Vector3 boxCenter = ghostTrap.transform.position;
            float checkBoxSize = gridSize * 0.45f;
            Vector3 halfExtents = new Vector3(checkBoxSize, checkBoxSize, checkBoxSize);
            bool isBlocked = Physics.CheckBox(boxCenter, halfExtents, ghostTrap.transform.rotation, collisionCheckLayer);
            Gizmos.color = isBlocked ? new Color(1f, 0f, 0f, 0.4f) : new Color(0f, 1f, 0f, 0.4f);
            Gizmos.matrix = Matrix4x4.TRS(boxCenter, ghostTrap.transform.rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, halfExtents * 2);
        }
        Gizmos.color = Color.white;
        Gizmos.matrix = Matrix4x4.identity;
    }

    private void PlaceTrap(InputAction.CallbackContext context)
    {
        if (!isTrapModeActive || !canPlaceObject || ghostTrapObjects == null || ghostTrapObjects.Count == 0)
            return;
        if (selectedTrapPlacementIndex < 0 || selectedTrapPlacementIndex >= TrapsSettings.Count)
            return;

        var trapSettings = TrapsSettings[selectedTrapPlacementIndex];
        var positioningMatrix = trapSettings.PositioningMatrix;
        int totalRows = positioningMatrix.Rows;
        int totalCols = positioningMatrix.Cols;
        int centerRow = totalRows / 2;
        int centerCol = totalCols / 2;

        int ghostTrapIdx = 0;
        for (int row = 0; row < totalRows; row++)
        {
            for (int col = 0; col < totalCols; col++)
            {
                var cellType = positioningMatrix[row, col];
                if (cellType == TrapPositioningType.None) continue;
                Vector3 offset = new Vector3((col - centerCol) * gridSize, 0f, (row - centerRow) * gridSize);
                if (ghostTrapIdx >= ghostTrapObjects.Count)
                {
                    Debug.LogError($"[PlaceTrap] ghostTrapIdx {ghostTrapIdx} out of range for {ghostTrapObjects.Count} ghosts");
                    continue;
                }
                GameObject ghostTrap = ghostTrapObjects[ghostTrapIdx++];
                Vector3 pos = ghostTrap.transform.position;
                Quaternion rot = ghostTrap.transform.rotation;
                if (cellType == TrapPositioningType.Trap && trapSettings.TrapObject != null)
                {
                    Instantiate(trapSettings.TrapObject, pos, rot);
                    var pointer = Instantiate(trapSettings.TrapPreview, pos, rot);
                    pointer.SetActive(isTrapModeActive);
                    trapPreviews.Add(pointer);
                }
                else if (cellType == TrapPositioningType.Spacer && trapSettings.TrapSpacerPreview != null)
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

    public void RotateObject(InputAction.CallbackContext context)
    {
        if (!isTrapModeActive || ghostTrapObjects == null || ghostTrapObjects.Count == 0)
            return;

        float scrollValue = context.ReadValue<float>();
        if (Mathf.Abs(scrollValue) < 0.01f)
            return;

        int direction = (int)Mathf.Sign(scrollValue);
        if (direction == 0) return;
        direction = -direction;

        ghostTrapRotationQuarterTurns = (ghostTrapRotationQuarterTurns + direction) % 4;
        if (ghostTrapRotationQuarterTurns < 0)
            ghostTrapRotationQuarterTurns += 4;

        Debug.Log($"[RotateObject] Rotated matrix, new ghostTrapRotationQuarterTurns={ghostTrapRotationQuarterTurns}");

        UpdateGhostTrapPositions();
    }
}
