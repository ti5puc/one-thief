using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;

public class TurretTrap : TrapBase
{
    [Header("Turret Trap Settings")]
    [SerializeField] private TurretBullet bulletPrefab;
    [SerializeField] private GameObject turretObject;
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private float shootInterval = 2f;
    [SerializeField] private float rotationSpeed = 1f;
    [SerializeField] private float bulletSpeed = 10f;

    [Space(10)]
    [SerializeField] private Animator turretAnimator;
    [SerializeField, AnimatorParam(nameof(turretAnimator))] private int shootAnimTriggerHash;

    [Space(10)]
    [SerializeField] private MeshRenderer crystalRenderer;
    [SerializeField] private Color baseCrystalColor;
    [SerializeField] private Color standByCrystalColor;
    [SerializeField] private Color shootingCrystalColor;
    [SerializeField] private float chargeUpDuration = 0.5f;
    [SerializeField] private float crystalTransitionDuration = 0.4f;

    private MaterialPropertyBlock crystalMPB;
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private Color currentCrystalColor;
    private Tweener crystalColorTween;
    private Transform currentTarget;
    private float lastShootTime;
    private bool isChargingUp;

    protected override void Awake()
    {
        base.Awake();
        crystalMPB = new MaterialPropertyBlock();
        SetCrystalColor(baseCrystalColor);
        currentCrystalColor = baseCrystalColor;
        actionTrigger.OnEnter += HandleIdentifyTarget;
        actionTrigger.OnExit += HandleResetTarget;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        crystalColorTween?.Kill();
        actionTrigger.OnEnter -= HandleIdentifyTarget;
        actionTrigger.OnExit -= HandleResetTarget;
    }

    protected override void OnAlwaysActive()
    {
        if (currentTarget == null) return;
        if (GameManager.CurrentGameState == GameState.Building) return;
        if (GameManager.IsGamePaused) return;

        // Rotate towards target
        RotateTowardsTarget();

        // Charge-up crystal color as we approach the next shot
        float timeUntilShoot = (lastShootTime + shootInterval) - Time.time;
        if (chargeUpDuration > 0f && timeUntilShoot <= chargeUpDuration)
        {
            if (!isChargingUp)
            {
                isChargingUp = true;
                crystalColorTween?.Kill();
                if (turretAnimator != null)
                    turretAnimator.SetTrigger(shootAnimTriggerHash);
            }
            float t = Mathf.Clamp01(1f - timeUntilShoot / chargeUpDuration);
            SetCrystalColor(Color.Lerp(standByCrystalColor, shootingCrystalColor, t));
        }

        // Shoot at intervals
        if (Time.time >= lastShootTime + shootInterval)
        {
            ShootAtTarget();
            lastShootTime = Time.time;
            isChargingUp = false;
            TransitionCrystalColor(standByCrystalColor, crystalTransitionDuration);
        }
    }

    private void HandleIdentifyTarget(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SetTarget(other.transform);
            lastShootTime = Time.time;
            isChargingUp = false;
            TransitionCrystalColor(standByCrystalColor, crystalTransitionDuration);
        }
    }

    private void HandleResetTarget(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SetTarget(null);
            isChargingUp = false;
            TransitionCrystalColor(baseCrystalColor, crystalTransitionDuration);
        }
    }

    private void SetTarget(Transform target)
    {
        currentTarget = target;
    }

    private void RotateTowardsTarget()
    {
        if (turretObject == null) return;

        Vector3 directionToTarget = currentTarget.position - turretObject.transform.position;
        directionToTarget.y = 0; // Only rotate on Y axis

        if (directionToTarget.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

            // Smoothly rotate towards target at constant speed (degrees per second)
            turretObject.transform.rotation = Quaternion.RotateTowards(
                turretObject.transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    private void SetCrystalColor(Color color)
    {
        currentCrystalColor = color;
        if (crystalRenderer == null) return;
        crystalMPB.SetColor(BaseColorID, color);
        crystalRenderer.SetPropertyBlock(crystalMPB);
    }

    private void TransitionCrystalColor(Color targetColor, float duration)
    {
        crystalColorTween?.Kill();
        Color startColor = currentCrystalColor;
        float progress = 0f;
        crystalColorTween = DOTween.To(
            () => progress,
            x => { progress = x; SetCrystalColor(Color.Lerp(startColor, targetColor, x)); },
            1f,
            duration
        );
    }

    private void ShootAtTarget()
    {
        if (GameManager.CurrentGameState == GameState.Building) return;
        if (GameManager.IsGamePaused) return;
        if (bulletPrefab == null || bulletSpawnPoint == null) return;

        Vector3 spawnPos = bulletSpawnPoint.position;
        Vector3 targetPos = currentTarget.position;

        // Check line of sight to target
        Vector3 directionToTarget = targetPos - spawnPos;
        float distanceToTarget = directionToTarget.magnitude;

        if (Physics.Raycast(spawnPos, directionToTarget.normalized, out RaycastHit hit, distanceToTarget, ~0, QueryTriggerInteraction.Ignore))
        {
            // If raycast hits something other than the player, don't shoot
            if (!hit.collider.CompareTag("Player"))
            {
                return;
            }
        }

        // Shoot straight at the player
        Vector3 velocity = directionToTarget.normalized * bulletSpeed;
        TurretBullet bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        bullet.Setup(velocity, 0f);
    }

    protected override void OnHit(Collider player)
    {
        Debug.Log("Player hit by wheel trap");
        var controller = player.GetComponent<PlayerDeathIdentifier>();
        if (controller != null)
            controller.Death();
    }

    protected override void OnAction(Collider player, float totalDuration)
    {
    }

    protected override void OnReactivate(float totalDuration)
    {
    }
}
