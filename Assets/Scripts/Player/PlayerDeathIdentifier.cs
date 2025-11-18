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
            // keep rotating player on Y axis after death
            transform.Rotate(Vector3.up * DeathRotateSpeed * Time.deltaTime);
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

    public void Death(bool spawnGhost = true)
    {
        Death(20f, 0f, 0f, spawnGhost);
    }

    public void Death(float customCameraDeathRotationX = 20f, float customCameraDeathOffsetY = 0f, float customCameraDeathOffsetZ = 0f, bool spawnGhost = true)
    {
        if (IsDead) return;
        if (GameManager.CurrentGameState != GameState.Exploring) return;
        if (isGodMode) return;

        IsDead = true;
        foreach (var visual in VisualsToHide)
            visual.SetActive(false);

        Vector3 upOffset = Vector3.up * DeathCameraUpOffset + Vector3.up * customCameraDeathOffsetY;
        Vector3 backOffset = -Vector3.forward * DeathCameraBackOffset + Vector3.back * customCameraDeathOffsetZ;
        Vector3 targetPosition = upOffset + backOffset;

        rigidBody.isKinematic = true;
        var collider = GetComponent<Collider>();
        collider.isTrigger = true;

        cameraTransform.DOLocalMove(targetPosition, 1f).SetEase(Ease.OutQuad);
        cameraTransform.localRotation = Quaternion.Euler(customCameraDeathRotationX, 0f, 0f);

        if (spawnGhost == false)
            return;

        var position = new Vector3(transform.position.x, DeathVfxPrefab.transform.position.y + vfxOffset, transform.position.z);
        Instantiate(DeathVfxPrefab, position, DeathVfxPrefab.transform.rotation);

        var ghostPosition = new Vector3(transform.position.x, DeathGhostPrefab.transform.position.y + vfxOffset, transform.position.z);
        deathGhost = Instantiate(DeathGhostPrefab, ghostPosition, DeathGhostPrefab.transform.rotation);
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
}
