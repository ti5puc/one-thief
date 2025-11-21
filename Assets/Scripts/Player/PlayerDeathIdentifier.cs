using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerDeathIdentifier : MonoBehaviour
{
    public static event Action<bool> OnGodModeChanged;
    
    [Header("Death Camera Offset")]
    public float DeathCameraUpOffset = 2f;
    public float DeathCameraBackOffset = 4f;
    public float DeathRotateSpeed = 60f; // degrees per second
    
    [Header("Camera Collision")]
    public LayerMask CameraCollisionLayers = -1;
    public float CameraCollisionOffset = 0.3f;
    public float CameraRadius = 0.2f;
    public bool DebugDrawCameraRay = true;

    [Header("Vfx")]
    public GameObject DeathVfxPrefab;
    public GameObject DeathGhostPrefab;

    [Space(10)]
    public GameObject[] VisualsToHide;
    
    [Header("Hacks")]
    public InputActionReference godModeHackAction;

    private Rigidbody rigidBody;
    private Transform cameraTransform;
    private bool isDead = false;
    private float vfxOffset;
    private GameObject deathGhost;
    private bool isGodMode = false;
    private Vector3 targetDeathCameraPosition;
    private bool isDeathCameraMoving = false;
    private float deathCameraAnimationTime = 0f;
    private const float DEATH_CAMERA_DURATION = 1f;
    private Transform preferredCameraPositionMarker;
    private float currentCameraDistance = 0f;
    private float preferredCameraDistance = 0f;

    public bool IsDead
    {
        get => isDead;
        set => isDead = GameManager.IsPlayerDead = value;
    }
    public float VfxOffset
    {
        get => vfxOffset;
        set => vfxOffset = value;
    }

    private void Awake()
    {
        cameraTransform = GetComponentInChildren<Camera>().transform;
        rigidBody = GetComponent<Rigidbody>();
        
        GameManager.IsPlayerDead = false;
        
        godModeHackAction.action.Enable();
        godModeHackAction.action.performed += ToggleGodMode;
    }

    private void OnDestroy()
    {
        godModeHackAction.action.Disable();
        godModeHackAction.action.performed -= ToggleGodMode;
    }

    private void Update()
    {
        if (IsDead)
        {
            transform.Rotate(Vector3.up * DeathRotateSpeed * Time.deltaTime);
            AdjustCameraPosition();
        }

        if (deathGhost != null)
        {
            float floatAmplitude = 0.2f;
            float floatFrequency = 1.5f;
            float baseY = DeathGhostPrefab.transform.position.y + vfxOffset;
            float sineY = baseY + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
            var position = new Vector3(transform.position.x, sineY, transform.position.z);
            deathGhost.transform.position = position;
        }
    }

    public void Death()
    {
        Death(20f, 0f, 0f, true);
    }

    public void Death(bool spawnBloodVfx = true)
    {
        Death(20f, 0f, 0f, spawnBloodVfx);
    }

    public void Death(float customCameraDeathRotationX = 20f, float customCameraDeathOffsetY = 0f, float customCameraDeathOffsetZ = 0f, bool spawnBloodVfx = true)
    {
        if (IsDead) return;
        if (GameManager.CurrentGameState != GameState.Exploring) return;
        if (isGodMode) return;

        IsDead = true;
        foreach (var visual in VisualsToHide)
            visual.SetActive(false);

        Vector3 upOffset = Vector3.up * DeathCameraUpOffset + Vector3.up * customCameraDeathOffsetY;
        Vector3 backOffset = -Vector3.forward * DeathCameraBackOffset + Vector3.back * customCameraDeathOffsetZ;
        targetDeathCameraPosition = upOffset + backOffset;
        preferredCameraDistance = targetDeathCameraPosition.magnitude;

        rigidBody.isKinematic = true;
        var collider = GetComponent<Collider>();
        collider.isTrigger = true;

        // Create invisible marker at preferred camera position
        if (preferredCameraPositionMarker == null)
        {
            GameObject markerObj = new GameObject("PreferredCameraPosition");
            preferredCameraPositionMarker = markerObj.transform;
            preferredCameraPositionMarker.SetParent(transform);
        }
        preferredCameraPositionMarker.localPosition = targetDeathCameraPosition;

        // Enable continuous camera collision checking and movement
        isDeathCameraMoving = true;
        deathCameraAnimationTime = 0f;
        currentCameraDistance = 0f; // Start from player position
        
        cameraTransform.localRotation = Quaternion.Euler(customCameraDeathRotationX, 0f, 0f);

        // Vfx spawn
        var ghostPosition = new Vector3(transform.position.x, DeathGhostPrefab.transform.position.y + vfxOffset, transform.position.z);
        deathGhost = Instantiate(DeathGhostPrefab, ghostPosition, DeathGhostPrefab.transform.rotation);

        if (spawnBloodVfx)
        {
            var position = new Vector3(transform.position.x, DeathVfxPrefab.transform.position.y + vfxOffset, transform.position.z);
            Instantiate(DeathVfxPrefab, position, DeathVfxPrefab.transform.rotation);
        }

        VfxOffset = 0f;
    }

    // TODO: placeholder here, change to movement script
    public void Knockback(Vector3 direction, float force)
    {
        if (IsDead) return;
        rigidBody.AddForce(direction.normalized * force, ForceMode.Impulse);
    }
    
    private void ToggleGodMode(InputAction.CallbackContext obj)
    {
        isGodMode = !isGodMode;
        OnGodModeChanged?.Invoke(isGodMode);
        Debug.Log($"God Mode: {isGodMode}");
    }
    
    private float GetSafeCameraDistance()
    {
        if (preferredCameraPositionMarker == null)
            return preferredCameraDistance;

        Vector3 preferredWorldPosition = preferredCameraPositionMarker.position;
        Vector3 playerPosition = transform.position;
        
        // Calculate direction FROM player TOWARD preferred camera position
        Vector3 directionToCamera = (preferredWorldPosition - playerPosition).normalized;
        float preferredDistance = Vector3.Distance(playerPosition, preferredWorldPosition);
        
        // Raycast from player toward preferred camera position to detect walls
        RaycastHit hit;
        bool hitDetected = Physics.SphereCast(playerPosition, CameraRadius, directionToCamera, out hit, preferredDistance, CameraCollisionLayers);
        
        if (DebugDrawCameraRay)
        {
            if (hitDetected)
            {
                Vector3 hitPosition = playerPosition + directionToCamera * hit.distance;
                Debug.DrawLine(playerPosition, hitPosition, Color.red, 0.02f);
                Debug.DrawLine(hitPosition, preferredWorldPosition, Color.magenta, 0.02f);
                
                if (Time.frameCount % 30 == 0)
                {
                    Debug.Log($"Wall detected! Hit: {hit.collider.name} at distance {hit.distance} from player. Max safe distance: {hit.distance - CameraCollisionOffset:F2}");
                }
            }
            else
            {
                Debug.DrawLine(playerPosition, preferredWorldPosition, Color.green, 0.02f);
            }
        }
        
        if (hitDetected)
        {
            float safeDistanceFromPlayer = hit.distance - CameraCollisionOffset;
            safeDistanceFromPlayer = Mathf.Max(safeDistanceFromPlayer, 0.5f); // Ensure minimum distance
            
            return safeDistanceFromPlayer;
        }
        
        return preferredCameraDistance;
    }
    
    private void AdjustCameraPosition()
    {
        if (isDeathCameraMoving)
        {
            deathCameraAnimationTime += Time.deltaTime;
            
            if (deathCameraAnimationTime >= DEATH_CAMERA_DURATION)
                isDeathCameraMoving = false;
        }
        
        // Calculate animation progress with easing (OutQuad)
        float t = Mathf.Clamp01(deathCameraAnimationTime / DEATH_CAMERA_DURATION);
        float easedT = 1f - (1f - t) * (1f - t); // OutQuad easing
        
        // Get the safe distance (will be less than preferred if wall is detected)
        float targetSafeDistance = GetSafeCameraDistance();
        
        float interpSpeed = (targetSafeDistance < currentCameraDistance) ? 15f : 5f;
        currentCameraDistance = Mathf.Lerp(currentCameraDistance, targetSafeDistance, Time.deltaTime * interpSpeed);
        
        float animatedDistance = isDeathCameraMoving 
            ? Mathf.Lerp(0f, currentCameraDistance, easedT)
            : currentCameraDistance;
        
        // Position camera at the animated distance in the direction of the target position
        Vector3 direction = targetDeathCameraPosition.normalized;
        Vector3 newCameraPosition = direction * animatedDistance;
        cameraTransform.localPosition = newCameraPosition;
    }
}
