using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;


[System.Serializable]
public class TrapGridSaveData
{
    public int Rows;
    public int Cols;
    public int[] Data; // matriz achatada
}

public class Player : MonoBehaviour
{
    private class PlacedTrapGroup
    {
        public List<GameObject> Previews = new();
        public List<GameObject> ActualTraps = new();
    }

    public static event Action<bool, string> OnTrapModeChanged; // bool isTrapModeActive, string currentTrapName
    public static event Action<string> OnSelectedTrapChanged; // string trapName
    public static event Action<bool, List<PlaceableSettings>, int> OnToggleTrapSelect; // bool isTrapSelectionActive, int selectedTrapIndex
    public event Action<bool, bool> OnMoveChanged; // bool isMoving, bool isSprinting

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
    public bool isCrouching;
    public float defaultStandHeight = 2f;
    public float crouchHeight = 0.8f;
    public float cameraCrouchOffset = -0.4f;
    public float crouchSpeedMultiplier = 0.55f;
    public bool canSlide;

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
    public bool canDoubleJump;
    public int maxAirJumps = 2;         // Total de pulos no ar
    public int airJumpsRemaining;   // Pulos no ar restantes
    public float jumpForce = 5f;
    public float airJumpForceMultiplier = 0.9f;
    public float lastJumpPressedTime;
    public float coyoteTime = 0.1f;      // Tempo após sair do chão que o pulo ainda é permitido   
    public float jumpBufferTime = 0.1f;  // Tempo pertmitido para o jogador apertar pulo antes de estar no chão

    [Header("Sprint")]
    public bool isSprinting = true; // Inverted: true by default (always sprinting)
    public bool canSprint;
    public float sprintMultiplier = 1.7f;

    [Header("Dash")]
    public bool canDash = true;
    public bool isDashing;
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
    public Vector2 gridOffset = Vector2.zero;
    private bool canPlaceObject;

    //---------- >>> NOVO <<< ----------//
    [Header("Grid / Save")]
    public int gridRows = 100;
    public int gridCols = 100;
    private int[,] trapIdGrid;
    //---------- >>> NOVO <<< ----------//

    [Header("Traps")]
    public List<PlaceableSettings> trapsSettings = new();

    [Header("Referências InputSystem")]
    public InputActionReference mouseXInput;
    public InputActionReference mouseYInput;
    public InputActionReference moveInput;
    public InputActionReference trapModeToggleInput;
    public InputActionReference placeObjectInput;
    [FormerlySerializedAs("switchTrapInput")] public InputActionReference removeTrapInput;
    public InputActionReference rotateTrapInput;
    public InputActionReference openTrapMenuInput;

    private bool isTrapModeActive;
    private bool isTrapSelectionActive;
    private int selectedTrapIndex;
    private int selectedTrapPlacementIndex;
    private PlayerDeathIdentifier deathIdentifier;
    private readonly List<GameObject> trapPreviews = new();
    private List<GameObject> ghostTrapObjects = new();
    private int ghostTrapRotationQuarterTurns;
    private Vector3 initialPosition;
    private readonly List<PlacedTrapGroup> placedTrapGroups = new();

    public bool IsTrapModeActive => isTrapModeActive && (deathIdentifier == null || !deathIdentifier.IsDead);
    public bool IsTrapMenuActive => isTrapSelectionActive;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        deathIdentifier = GetComponent<PlayerDeathIdentifier>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        capsule = GetComponent<CapsuleCollider>();
        lastGroundedTime = -999f;
        airJumpsRemaining = 0;
        initialPosition = transform.position;
        isSprinting = true; // Always start sprinting
        deathIdentifier.OnTryResetDeath += ResetPlayer;

        //Matriz de IDs de traps para salvamento de layout
        trapIdGrid = new int[gridRows, gridCols]; 

        // Evita problemas de pulo ao iniciar
        lastJumpPressedTime = -999f;
        lastGroundedTime = -999f;
        airJumpsRemaining = 0;

        //---------- >>> NOVO <<< ----------//
        // >>> NOVO: inicializa a matriz de IDs de traps <<<
        if (trapIdGrid == null)
            trapIdGrid = new int[gridRows, gridCols]; // tudo inicializa em 0 = vazio
        //---------- >>> NOVO <<< ----------//
        TrapSelectionCardUI.OnTrapSelected += SelectObject;
        PauseMenuUI.OnTest += ResetPlayer;
    }

    private void OnDestroy()
    {
        deathIdentifier.OnTryResetDeath -= ResetPlayer;
        TrapSelectionCardUI.OnTrapSelected -= SelectObject;
        PauseMenuUI.OnTest -= ResetPlayer;
    }

    private void Start()
    {
        if (GameManager.CurrentGameState == GameState.Building && GameManager.CanEnterBuildMode)
        {
            isTrapModeActive = true;
            foreach (var preview in trapPreviews)
            {
                if (preview != null)
                    preview.SetActive(isTrapModeActive);
            }
            CreateGhostTrapsForSelectedTrap();
        }
    }

    private void OnEnable()
    {

        trapModeToggleInput.action.Enable();
        trapModeToggleInput.action.performed += ToggleTrapMode;

        placeObjectInput.action.Enable();
        placeObjectInput.action.performed += PlaceTrap;

        openTrapMenuInput.action.Enable();
        openTrapMenuInput.action.performed += ToggleTrapSelection;
        
        removeTrapInput.action.Enable();
        removeTrapInput.action.performed += RemoveTrap;

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
            //CrouchInput.action.performed += OnCrouchPress;
            //CrouchInput.action.canceled += OnCrouchRelease;
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

        openTrapMenuInput.action.Disable();
        openTrapMenuInput.action.performed -= ToggleTrapSelection;
        
        removeTrapInput.action.Disable();
        removeTrapInput.action.performed -= RemoveTrap;

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
            //CrouchInput.action.performed -= OnCrouchPress;
            //CrouchInput.action.canceled -= OnCrouchRelease;
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
        if (isTrapSelectionActive) return;
        if (GameManager.IsGamePaused) return;

        float deltaX = context.ReadValue<float>() * senseX;
        transform.Rotate(0f, deltaX, 0f);
    }

    public void OnMouseY(InputAction.CallbackContext context)
    {
        if (deathIdentifier != null && deathIdentifier.IsDead) return;
        if (isTrapSelectionActive) return;
        if (GameManager.IsGamePaused) return;

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

        if (isTrapSelectionActive) return;
        if (GameManager.IsGamePaused) return;

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
        //-------------------------------- Fim do Update de Movimentacao --------------------------------

        // >>> NOVO: atalhos de debug para salvar / carregar / reconstruir <<<

        if (Input.GetKeyDown(KeyCode.I))
        {
            SaveTrapGridToDisk();
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            bool ok = LoadTrapGridFromDisk();
            Debug.Log($"[DEBUG] LoadTrapGridFromDisk retornou: {ok}");
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            RebuildTrapsFromGrid();
        }

    }

    void FixedUpdate()
    {
        if (deathIdentifier != null && deathIdentifier.IsDead) return;
        if (isTrapSelectionActive) return;
        if (GameManager.IsGamePaused) return;

        //Movimentação durante o dash
        if (isDashing)
        {
            Vector3 step = dashDirection * (dashSpeed * Time.fixedDeltaTime);
            Vector3 target = rb.position + step;

            rb.MovePosition(target);

            if (Time.time >= dashEndTime)
            {
                isDashing = false;
                rb.useGravity = true;
                var v = rb.linearVelocity; v.y = 0f; rb.linearVelocity = v;
                isSprinting = true; // Always resume sprinting after dash
            }
            return; // Durante o dash ignora o resto da movimentação
        }

        float speed = moveSpeed;
        OnMoveChanged?.Invoke(_moveDirection.sqrMagnitude > 0.0001f, isSprinting);

        if (isCrouching)
        {
            isSprinting = false;
            speed *= crouchSpeedMultiplier;
        }
        else if (isSprinting && _moveDirection.sqrMagnitude > 0.0001f)
        {
            speed *= sprintMultiplier;
        }

        Vector3 moveDirection = transform.TransformDirection(new Vector3(_moveDirection.x, 0f, _moveDirection.y));
        if (moveDirection.sqrMagnitude > 1f) moveDirection.Normalize();
        Vector3 newPosition = rb.position + moveDirection * (speed * Time.fixedDeltaTime);
        rb.MovePosition(newPosition);
    }

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

    void OnDash(InputAction.CallbackContext ctx)
    {
        if (Time.time < lastDashTime + dashCooldown) return;

        isDashing = true;
        // Do NOT set IsSprinting = false here, let sprint resume after dash
        lastDashTime = Time.time;
        dashEndTime = Time.time + dashDuration;

        Vector3 inputDir = new Vector3(_moveDirection.x, 0f, _moveDirection.y);
        if (inputDir.sqrMagnitude > 0.0001f)
        {
            Vector3 worldDir = cameraTransform.TransformDirection(inputDir);
            dashDirection = Vector3.ProjectOnPlane(worldDir, Vector3.up).normalized;
        }
        else
        {
            Vector3 camFwd = cameraTransform.forward;
            dashDirection = Vector3.ProjectOnPlane(camFwd, Vector3.up).normalized;
        }
        rb.useGravity = false;
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
        // Holding sprint: walk (not sprinting)
        isSprinting = false;
    }

    void OnSprintRelease(InputAction.CallbackContext ctx)
    {
        // Released: sprint
        isSprinting = true;
    }

    //----------------------------- Fim Funcoes dos Comandos de Movimentacao -----------------------------

    private void ToggleTrapMode(InputAction.CallbackContext context)
    {
        if (GameManager.CanEnterBuildMode == false) return;
        if (isTrapSelectionActive) return;
        
        isTrapModeActive = !isTrapModeActive;
        Debug.Log("Modo de Construção: " + (isTrapModeActive ? "Ativado" : "Desativado"));

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
            GameManager.ChangeGameStateToTestingBuild();

        if (isTrapModeActive == false)
            ToggleTrapSelection(false);

        OnTrapModeChanged?.Invoke(isTrapModeActive, trapsSettings[selectedTrapIndex].TrapName);
    }
    
    private void ToggleTrapSelection(InputAction.CallbackContext context)
    {
        if (isTrapModeActive == false) return;
        ToggleTrapSelection(!isTrapSelectionActive);
    }

    private void ToggleTrapSelection(bool isActive)
    {
        isTrapSelectionActive = isActive;
        OnToggleTrapSelect?.Invoke(isTrapSelectionActive, trapsSettings, selectedTrapPlacementIndex);

        Cursor.lockState = isTrapSelectionActive ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isTrapSelectionActive;

        if (isTrapSelectionActive)
            OnMoveChanged?.Invoke(false, false);
    }

    private void SelectObject(PlaceableSettings placeableSettings)
    {
        if (!isTrapModeActive) return;
        if (!isTrapSelectionActive) return;

        int newIndex = trapsSettings.IndexOf(placeableSettings);
        if (newIndex == -1)
        {
            Debug.LogWarning($"PlaceableSettings {placeableSettings.TrapName} not found in TrapsSettings.");
            return;
        }

        selectedTrapIndex = newIndex;
        selectedTrapPlacementIndex = selectedTrapIndex;

        Debug.Log($"Armadilha selecionada: {trapsSettings[selectedTrapIndex].TrapName}");
        OnSelectedTrapChanged?.Invoke(trapsSettings[selectedTrapIndex].TrapName);

        if (isTrapModeActive)
        {
            CreateGhostTrapsForSelectedTrap();
        }
    }

    private void CreateGhostTrapsForSelectedTrap()
    {
        DestroyGhostTraps();
        if (selectedTrapIndex < 0 || selectedTrapIndex >= trapsSettings.Count)
            return;
        var trapSettings = trapsSettings[selectedTrapIndex];
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
                    foreach (var coll in trapPreview.Colliders)
                    {
                        if (coll != null)
                            coll.gameObject.layer = ignoreLayer;
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
        if (selectedTrapIndex >= 0 && selectedTrapIndex < trapsSettings.Count)
        {
            var settings = trapsSettings[selectedTrapIndex];
            trapSurfaceLayer = settings.TrapSurface | settings.TrapPlacementLayer;
        }

        if (Physics.Raycast(ray, out hit, interactionDistance, trapSurfaceLayer))
        {
            var trapSettings = trapsSettings[selectedTrapIndex];
            var positioningMatrix = trapSettings.PositioningMatrix;
            int totalRows = positioningMatrix.Rows;
            int totalCols = positioningMatrix.Cols;
            int centerRow = totalRows / 2;
            int centerCol = totalCols / 2;

            float snappedX = Mathf.Round((hit.point.x - gridOffset.x) / gridSize) * gridSize + gridOffset.x;
            float snappedY = Mathf.Round(hit.point.y / gridSize) * gridSize;
            float snappedZ = Mathf.Round((hit.point.z - gridOffset.y) / gridSize) * gridSize + gridOffset.y;
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

                    bool isValid = IsTrapCellPlacementValid(cellType, ghostTrap.transform.position, ghostTrap.transform.rotation, trapSettings, ghostTrap);
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

    private bool IsTrapCellPlacementValid(TrapPositioningType cellType, Vector3 position, Quaternion rotation, PlaceableSettings trapSettings, GameObject ghostTrap)
    {
        float checkBoxSize = gridSize * 0.45f;
        Vector3 halfExtents = new Vector3(checkBoxSize, checkBoxSize, checkBoxSize);
        if (cellType == TrapPositioningType.Trap)
        {
            bool blockedByPlacement = Physics.CheckBox(position, halfExtents, rotation, trapSettings.TrapPlacementLayer | collisionCheckLayer);
            bool hasSurface = Physics.CheckBox(position, halfExtents, rotation, trapSettings.TrapSurface);
            if (trapSettings.NeedsWallToPlace)
            {
                if (ghostTrap == null)
                {
                    Debug.LogError("[IsTrapCellPlacementValid] ghostTrap is null but trap requires a wall to place.");
                    return false;
                }
                var trapPreviewComp = ghostTrap.GetComponent<TrapPreview>();
                if (trapPreviewComp == null || trapPreviewComp.WallCheckPoint == null)
                {
                    Debug.LogError($"[IsTrapCellPlacementValid] Trap '{trapSettings.TrapName}' requires a wall to place but its preview has no WallCheckPoint.");
                    return false;
                }

                Vector3 wallCheckPos = trapPreviewComp.WallCheckPoint.transform.position;
                float wallCheckRadius = Mathf.Max(0.05f, gridSize * 0.25f);
                bool hasWall = Physics.CheckSphere(wallCheckPos, wallCheckRadius, trapSettings.WallLayer);
                bool wallIsBlockedByPlacement = Physics.CheckSphere(wallCheckPos, wallCheckRadius, trapSettings.TrapPlacementLayer | collisionCheckLayer);

                return !blockedByPlacement && hasSurface && hasWall && !wallIsBlockedByPlacement;
            }

            return !blockedByPlacement && hasSurface;
        }
        else if (cellType == TrapPositioningType.Spacer)
        {
            bool blockedByPlacement = Physics.CheckBox(position, halfExtents, rotation, trapSettings.TrapPlacementLayer | collisionCheckLayer);
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

    //----------------- >>> NOVO <<< ----------------- //
    // row = eixo Z, col = eixo X
    // row = eixo Z, col = eixo X, com (0,0) do mundo no centro da matriz
    private bool TryWorldToGrid(Vector3 worldPos, out int row, out int col)
    {
        int centerRow = gridRows / 2;
        int centerCol = gridCols / 2;

        col = centerCol + Mathf.RoundToInt((worldPos.x - gridOffset.x) / gridSize);
        row = centerRow + Mathf.RoundToInt((worldPos.z - gridOffset.y) / gridSize);

        if (row < 0 || row >= gridRows || col < 0 || col >= gridCols)
        {
            Debug.LogWarning($"[TryWorldToGrid] Fora dos limites: worldPos={worldPos} -> row={row}, col={col}");
            row = col = -1;
            return false;
        }

        return true;
    }
    //----------------- >>> NOVO <<< ----------------- //


    private void PlaceTrap(InputAction.CallbackContext context)
    {
        if (!isTrapModeActive || !canPlaceObject || ghostTrapObjects == null || ghostTrapObjects.Count == 0)
            return;
        if (selectedTrapPlacementIndex < 0 || selectedTrapPlacementIndex >= trapsSettings.Count)
            return;
        if (isTrapSelectionActive)
            return;
        if (GameManager.IsGamePaused)
            return;

        var trapSettings = trapsSettings[selectedTrapPlacementIndex];
        var positioningMatrix = trapSettings.PositioningMatrix;
        int totalRows = positioningMatrix.Rows;
        int totalCols = positioningMatrix.Cols;

        PlacedTrapGroup trapGroup = new PlacedTrapGroup();

        int ghostTrapIdx = 0;
        for (int row = 0; row < totalRows; row++)
        {
            for (int col = 0; col < totalCols; col++)
            {
                var cellType = positioningMatrix[row, col];
                if (cellType == TrapPositioningType.None) continue;

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
                    var trapInstance = Instantiate(trapSettings.TrapObject, pos, rot);

                    if (TryWorldToGrid(pos, out int gridRow, out int gridCol))
                    {
                        trapIdGrid[gridRow, gridCol] = selectedTrapPlacementIndex + 1;
                        Debug.Log($"[GRID WRITE] TrapID={selectedTrapPlacementIndex + 1} em ({gridRow},{gridCol})");
                    }
                    else
                    {
                        Debug.LogWarning($"[PlaceTrap] Trap fora dos limites da matriz. WorldPos={pos}");
                    }

                    var pointer = Instantiate(trapSettings.TrapPreview, pos, rot);
                    pointer.SetActive(isTrapModeActive);
                    trapPreviews.Add(pointer);
                    
                    trapGroup.ActualTraps.Add(trapInstance);
                    trapGroup.Previews.Add(pointer);
                }

                else if (cellType == TrapPositioningType.Spacer && trapSettings.TrapSpacerPreview != null)
                {
                    var pointer = Instantiate(trapSettings.TrapSpacerPreview, pos, rot);
                    pointer.SetActive(isTrapModeActive);
                    trapPreviews.Add(pointer);
                    
                    trapGroup.Previews.Add(pointer);
                }
            }
        }

        placedTrapGroups.Add(trapGroup);

        Debug.Log($"[PlaceTrap] Placement completed for {trapSettings.TrapObject.name}");
    }

    private void RemoveTrap(InputAction.CallbackContext context)
    {
        if (!isTrapModeActive) return;
        if (isTrapSelectionActive) return;
        if (GameManager.IsGamePaused) return;
        if (ghostTrapObjects == null || ghostTrapObjects.Count == 0) return;

        GameObject centerGhostTrap = ghostTrapObjects[0];
        if (centerGhostTrap == null || !centerGhostTrap.activeSelf) return;

        Vector3 targetGridPosition = centerGhostTrap.transform.position;
        
        PlacedTrapGroup groupToRemove = null;
        foreach (var group in placedTrapGroups)
        {
            foreach (var preview in group.Previews)
            {
                if (preview != null)
                {
                    float distance = Vector3.Distance(preview.transform.position, targetGridPosition);
                    if (distance < gridSize * 0.1f)
                    {
                        groupToRemove = group;
                        break;
                    }
                }
            }
            if (groupToRemove != null) break;
        }

        if (groupToRemove != null)
        {
            foreach (var actualTrap in groupToRemove.ActualTraps)
            {
                if (actualTrap != null)
                {
                    Debug.Log($"[RemoveTrap] Removed trap: {actualTrap.name}");
                    Destroy(actualTrap);
                }
            }

            foreach (var preview in groupToRemove.Previews)
            {
                if (preview != null)
                {
                    trapPreviews.Remove(preview);
                    Destroy(preview);
                }
            }

            placedTrapGroups.Remove(groupToRemove);
            
            Debug.Log($"[RemoveTrap] Removed trap group with {groupToRemove.ActualTraps.Count} traps and {groupToRemove.Previews.Count} previews at position {targetGridPosition}");
        }
        else
        {
            Debug.Log($"[RemoveTrap] No trap found at grid position {targetGridPosition}");
        }
    }

    public void SaveTrapGridToDisk()
    {
        if (trapIdGrid == null)
        {
            Debug.LogError("[SAVE] trapIdGrid é null, nada para salvar.");
            return;
        }

        int nonZero = 0;
        for (int r = 0; r < gridRows; r++)
            for (int c = 0; c < gridCols; c++)
                if (trapIdGrid[r, c] != 0)
                    nonZero++;

        Debug.Log($"[SAVE] Células != 0 na matriz em memória: {nonZero}");

        TrapGridSaveData data = new TrapGridSaveData
        {
            Rows = gridRows,
            Cols = gridCols,
            Data = FlattenGrid(trapIdGrid, gridRows, gridCols)
        };

        string json = JsonUtility.ToJson(data, true);
        string path = System.IO.Path.Combine(Application.persistentDataPath, "trapGrid.json");
        System.IO.File.WriteAllText(path, json);

        Debug.Log($"[SAVE] Grid salvo em: {path}");
    }

    public bool LoadTrapGridFromDisk()
    {
        string path = System.IO.Path.Combine(Application.persistentDataPath, "trapGrid.json");

        if (!System.IO.File.Exists(path))
        {
            Debug.LogWarning("[LOAD] Arquivo de grid não encontrado");
            return false;
        }

        string json = System.IO.File.ReadAllText(path);

        TrapGridSaveData data = JsonUtility.FromJson<TrapGridSaveData>(json);

        trapIdGrid = UnflattenGrid(data.Data, data.Rows, data.Cols);

        Debug.Log("[LOAD] Grid carregado com sucesso");

        return true;
    }

    public void RebuildTrapsFromGrid()
    {
        if (trapIdGrid == null)
        {
            Debug.LogWarning("[LOAD] trapIdGrid é null, nada para reconstruir.");
            return;
        }

        int centerRow = gridRows / 2;
        int centerCol = gridCols / 2;

        for (int r = 0; r < gridRows; r++)
        {
            for (int c = 0; c < gridCols; c++)
            {
                int id = trapIdGrid[r, c];

                if (id <= 0) continue;

                int trapIndex = id - 1;

                if (trapIndex < 0 || trapIndex >= trapsSettings.Count)
                {
                    Debug.LogWarning($"[LOAD] ID inválido na célula [{r},{c}]: {id}");
                    continue;
                }

                float x = gridOffset.x + (c - centerCol) * gridSize;
                float z = gridOffset.y + (r - centerRow) * gridSize;
                float y = 0f;

                Vector3 pos = new Vector3(x, y, z);

                Instantiate(trapsSettings[trapIndex].TrapObject, pos, Quaternion.identity);
            }
        }

        Debug.Log("[LOAD] Reconstrução de traps concluída.");
    }

    private void ResetPlayer()
    {
        transform.position = initialPosition;
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

        UpdateGhostTrapPositions();
    }

    private int[] FlattenGrid(int[,] grid, int rows, int cols)
    {
        int[] flat = new int[rows * cols];
        int idx = 0;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                flat[idx++] = grid[r, c];
        return flat;
    }

    private int[,] UnflattenGrid(int[] flat, int rows, int cols)
    {
        int[,] grid = new int[rows, cols];
        int idx = 0;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                grid[r, c] = flat[idx++];
        return grid;
    }
}
