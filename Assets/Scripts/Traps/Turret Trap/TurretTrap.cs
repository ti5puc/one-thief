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
    [SerializeField] private float gravity = 9.81f;
    [SerializeField] private float shootAngleOffset = 0f; // Additional angle above minimum required angle

    private Transform currentTarget;
    private float lastShootTime;

    protected override void Awake()
    {
        base.Awake();
        actionTrigger.OnEnter += HandleIdentifyTarget;
        actionTrigger.OnExit += HandleResetTarget;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
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

        // Shoot at intervals
        if (Time.time >= lastShootTime + shootInterval)
        {
            ShootAtTarget();
            lastShootTime = Time.time;
        }
    }

    private void HandleIdentifyTarget(Collider other)
    {
        if (other.CompareTag("Player"))
            SetTarget(other.transform);
    }

    private void HandleResetTarget(Collider other)
    {
        if (other.CompareTag("Player"))
            SetTarget(null);
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

        // Calculate launch angle and velocity
        if (CalculateLaunchData(spawnPos, targetPos, bulletSpeed, gravity, shootAngleOffset,
            out Vector3 launchVelocity, out float launchAngle))
        {
            // Spawn and setup bullet
            TurretBullet bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
            bullet.Setup(launchVelocity, gravity);
        }
    }

    /// <summary>
    /// Calculates the launch velocity needed to hit a target with projectile motion.
    /// </summary>
    private bool CalculateLaunchData(Vector3 startPos, Vector3 targetPos, float speed, float gravityValue,
        float angleOffset, out Vector3 velocity, out float angle)
    {
        velocity = Vector3.zero;
        angle = 0f;

        Vector3 toTarget = targetPos - startPos;
        Vector3 toTargetXZ = new Vector3(toTarget.x, 0, toTarget.z);
        float horizontalDistance = toTargetXZ.magnitude;
        float verticalDistance = toTarget.y;

        // Calculate the minimum angle needed to hit the target
        // Using the projectile motion formula: tan(2θ) = (gx²) / (v²x - gy)
        float speedSquared = speed * speed;
        float gravityTimesDistance = gravityValue * horizontalDistance;

        // Discriminant for the quadratic equation
        float discriminant = speedSquared * speedSquared -
            gravityValue * (gravityValue * horizontalDistance * horizontalDistance + 2 * verticalDistance * speedSquared);

        if (discriminant < 0)
        {
            // Target is out of range
            Debug.LogWarning("Target is out of range for turret!");
            return false;
        }

        // Calculate the two possible angles (low and high trajectory)
        float sqrtDiscriminant = Mathf.Sqrt(discriminant);
        float angle1 = Mathf.Atan2(speedSquared - sqrtDiscriminant, gravityTimesDistance);
        float angle2 = Mathf.Atan2(speedSquared + sqrtDiscriminant, gravityTimesDistance);

        // Use the lower angle and add the offset
        angle = Mathf.Min(angle1, angle2) + angleOffset * Mathf.Deg2Rad;

        // Ensure angle is valid
        if (angle < 0 || angle > Mathf.PI / 2)
        {
            Debug.LogWarning("Invalid launch angle calculated!");
            return false;
        }

        // Calculate the velocity vector
        Vector3 direction = toTargetXZ.normalized;
        float horizontalVelocity = speed * Mathf.Cos(angle);
        float verticalVelocity = speed * Mathf.Sin(angle);

        velocity = direction * horizontalVelocity + Vector3.up * verticalVelocity;
        return true;
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
