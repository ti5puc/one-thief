using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraWobble : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player player;
    [SerializeField] private Camera cam;

    [Header("Head Bob")]
    [SerializeField] private float bobFrequency = 8f;
    [SerializeField] private float bobAmplitudeY = 0.04f;
    [SerializeField] private float bobAmplitudeX = 0.02f;
    [SerializeField] private float sprintMultiplier = 1.4f;

    [Header("Tilt on Strafe")]
    [SerializeField] private float tiltAngle = 2.5f;
    [SerializeField] private float tiltSpeed = 6f;

    [Header("FOV")]
    [SerializeField] private float fovDefault = 70f;
    [SerializeField] private float fovWalk = 72f;
    [SerializeField] private float fovSprint = 75f;
    [SerializeField] private float fovDash = 85f;
    [SerializeField] private float fovJump = 73f;        // kick sutil ao pular
    [SerializeField] private float fovDashSpeed = 14f;
    [SerializeField] private float fovReturnSpeed = 5f;

    [Header("Landing Squash")]
    [SerializeField] private float landDipAmount = 0.08f;
    [SerializeField] private float landDipSpeed = 18f;
    [SerializeField] private float landRecoverSpeed = 7f;
    [SerializeField] private float landVelocityThreshold = 3f;

    [Header("Air Jump Squash")]
    [SerializeField] private float airJumpDipAmount = 0.05f;  // menor que o pouso
    [SerializeField] private float airJumpDipSpeed = 22f;     // mais rápido (snap cartoon)
    [SerializeField] private float airJumpRecoverSpeed = 9f;

    [Header("Smoothing")]
    [SerializeField] private float bobReturnSpeed = 8f;

    private float _bobTimer;
    private Vector3 _bobOffset;
    private float _currentTilt;
    private Vector3 _initialLocalPos;

    private bool _isMoving;
    private bool _isSprinting;

    // Jump / land state
    private bool _wasGrounded;
    private float _landDipCurrent;
    private float _landDipTarget;
    private float _fallVelocityOnLand;
    private float _currentDipSpeed;
    private float _currentRecoverSpeed;

    // FOV jump kick
    private float _fovJumpOffset;

    private void Awake()
    {
        player.OnMoveChanged += HandleMoveChanged;
        player.OnJumped += HandleJumped;

        if (cam == null)
            cam = GetComponent<Camera>();
    }

    private void OnDestroy()
    {
        player.OnMoveChanged -= HandleMoveChanged;
        player.OnJumped -= HandleJumped;
    }

    private void Start()
    {
        _initialLocalPos = transform.localPosition;
        _wasGrounded = player.isGrounded;
        _currentDipSpeed = landDipSpeed;
        _currentRecoverSpeed = landRecoverSpeed;

        if (cam != null)
            cam.fieldOfView = fovDefault;
    }

    private void HandleMoveChanged(bool isMoving, bool isSprinting)
    {
        _isMoving = isMoving;
        _isSprinting = isSprinting;
    }

    private void HandleJumped(bool isAirJump)
    {
        if (!isAirJump) return; // pulo do chão já é coberto pela transição isGrounded

        _landDipTarget = -airJumpDipAmount;
        _currentDipSpeed = airJumpDipSpeed;
        _currentRecoverSpeed = airJumpRecoverSpeed;
        _fovJumpOffset = fovJump - fovDefault;
    }

    private void LateUpdate()
    {
        DetectJumpAndLand();
        UpdateBob();
        UpdateTilt();
        UpdateFov();
    }

    private void DetectJumpAndLand()
    {
        bool grounded = player.isGrounded;

        // Acabou de pousar
        if (grounded && !_wasGrounded)
        {
            float fallSpeed = Mathf.Abs(_fallVelocityOnLand);
            if (fallSpeed >= landVelocityThreshold)
            {
                float dipStrength = Mathf.Clamp01((fallSpeed - landVelocityThreshold) / 8f);
                _landDipTarget = -landDipAmount * (0.5f + dipStrength * 0.5f);
                _currentDipSpeed = landDipSpeed;
                _currentRecoverSpeed = landRecoverSpeed;
            }
        }

        // Acabou de sair do chão (pulo normal)
        if (!grounded && _wasGrounded)
        {
            _fovJumpOffset = fovJump - fovDefault;
        }

        // Registra velocidade vertical enquanto está no ar
        if (!grounded)
            _fallVelocityOnLand = player.rb.linearVelocity.y;

        _wasGrounded = grounded;

        // Anima o dip: vai rápido até o target, volta devagar (cartoon squash)
        if (_landDipCurrent > _landDipTarget)
            _landDipCurrent = Mathf.MoveTowards(_landDipCurrent, _landDipTarget, _currentDipSpeed * Time.deltaTime);
        else
            _landDipCurrent = Mathf.Lerp(_landDipCurrent, 0f, _currentRecoverSpeed * Time.deltaTime);

        // Quando chegou perto de zero, zera o target também
        if (Mathf.Abs(_landDipCurrent) < 0.001f)
            _landDipTarget = 0f;

        // FOV kick decai suavemente
        _fovJumpOffset = Mathf.Lerp(_fovJumpOffset, 0f, fovReturnSpeed * Time.deltaTime);
    }

    private void UpdateBob()
    {
        if (_isMoving)
        {
            float freq = bobFrequency * (_isSprinting ? sprintMultiplier : 1f);
            _bobTimer += Time.deltaTime * freq;

            float bobY = Mathf.Sin(_bobTimer) * bobAmplitudeY;
            float bobX = Mathf.Cos(_bobTimer * 0.5f) * bobAmplitudeX;
            _bobOffset = Vector3.Lerp(_bobOffset, new Vector3(bobX, bobY, 0f), Time.deltaTime * freq);
        }
        else
        {
            _bobOffset = Vector3.Lerp(_bobOffset, Vector3.zero, Time.deltaTime * bobReturnSpeed);
        }

        transform.localPosition = _initialLocalPos + _bobOffset + new Vector3(0f, _landDipCurrent, 0f);
    }

    private void UpdateTilt()
    {
        Vector3 localVel = player.transform.InverseTransformDirection(player.rb.linearVelocity);
        float strafeInput = Mathf.Clamp(localVel.x / 3f, -1f, 1f);

        float targetTilt = -strafeInput * tiltAngle;
        _currentTilt = Mathf.Lerp(_currentTilt, targetTilt, Time.deltaTime * tiltSpeed);

        float currentX = transform.localEulerAngles.x;
        if (currentX > 180f) currentX -= 360f;

        transform.localEulerAngles = new Vector3(currentX, 0f, _currentTilt);
    }

    private void UpdateFov()
    {
        if (cam == null) return;

        float targetFov;
        float speed;

        if (player.isDashing)
        {
            targetFov = fovDash;
            speed = fovDashSpeed;
        }
        else if (_isMoving && _isSprinting)
        {
            targetFov = fovSprint;
            speed = fovReturnSpeed;
        }
        else if (_isMoving)
        {
            targetFov = fovWalk;
            speed = fovReturnSpeed;
        }
        else
        {
            targetFov = fovDefault;
            speed = fovReturnSpeed;
        }

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov + _fovJumpOffset, Time.deltaTime * speed);
    }
}
