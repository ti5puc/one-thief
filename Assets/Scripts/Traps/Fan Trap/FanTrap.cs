using System;
using UnityEngine;

public class FanTrap : TrapBase
{
    [Header("Fan Settings")]
    [SerializeField] private bool isVerticalFan = false; // If true, pushes upward; if false, pushes forward
    [SerializeField] private float pushForce = 8f;
    [SerializeField] private float pushVerticalForce = 1f;
    [SerializeField] private float pushInterval = 0.1f; // How often to apply push force
    [SerializeField] private float maxWindDistance = 10f; // Maximum distance where wind has effect
    [SerializeField] private float maxLateralDistance = 5f; // Maximum lateral distance from center where wind has effect
    [SerializeField] private AnimationCurve windFalloffCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f); // Distance (0-1) to force multiplier
    [SerializeField] private AnimationCurve centerFalloffCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f); // Lateral distance (0-1) to force multiplier

    [Space(10)]
    [SerializeField] private Transform[] fanCenters; // The center points of the fans (for calculating direction)

    private PlayerDeathIdentifier currentPlayerInTrigger;
    private float lastPushTime;

    protected override void Awake()
    {
        base.Awake();
        actionTrigger.OnExit += OnExit;

        if (fanCenters == null || fanCenters.Length == 0)
            fanCenters = new Transform[] { transform };
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        actionTrigger.OnExit -= OnExit;
    }

    protected override void OnAlwaysActive()
    {
        // Fan is always active - push player if they're in the trigger
        if (currentPlayerInTrigger != null && !currentPlayerInTrigger.IsDead)
        {
            if (Time.time - lastPushTime >= pushInterval)
            {
                PushPlayer(currentPlayerInTrigger);
                lastPushTime = Time.time;
            }
        }
    }

    protected override void OnActionTriggerEnter(Collider other)
    {
        if (GameManager.CurrentGameState == GameState.Building) return;
        if (other.CompareTag(GameManager.PlayerTag) == false) return;

        var controller = other.GetComponent<PlayerDeathIdentifier>();
        if (controller.IsDead) return;

        currentPlayerInTrigger = controller;
        lastPushTime = Time.time - pushInterval; // Allow immediate push on enter
    }

    protected override void OnActionTriggerStay(Collider other)
    {
        // Keep reference to player while they're in trigger
        if (GameManager.CurrentGameState == GameState.Building) return;
        if (other.CompareTag(GameManager.PlayerTag) == false) return;

        var controller = other.GetComponent<PlayerDeathIdentifier>();
        if (controller != null && !controller.IsDead)
        {
            currentPlayerInTrigger = controller;
        }
    }

    private void OnExit(Collider other)
    {
        if (other.CompareTag(GameManager.PlayerTag))
        {
            var controller = other.GetComponent<PlayerDeathIdentifier>();
            if (controller == currentPlayerInTrigger)
            {
                currentPlayerInTrigger = null;

                // Stop the knockback momentum when player exits
                var rb = controller.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                }
            }
        }
    }

    private void PushPlayer(PlayerDeathIdentifier controller)
    {
        // Find the closest fan center to the player
        Transform closestFanCenter = fanCenters[0];
        float closestDistance = Vector3.Distance(controller.transform.position, closestFanCenter.position);

        for (int i = 1; i < fanCenters.Length; i++)
        {
            float distance = Vector3.Distance(controller.transform.position, fanCenters[i].position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestFanCenter = fanCenters[i];
            }
        }

        Vector3 pushDirection;
        float forceMultiplier;

        if (isVerticalFan)
        {
            // Vertical fan - push upward only
            pushDirection = Vector3.up;

            // Calculate 3D distance falloff
            float normalizedDistance = Mathf.Clamp01(closestDistance / maxWindDistance);
            float distanceMultiplier = windFalloffCurve.Evaluate(normalizedDistance);

            // Calculate horizontal distance from center (XZ plane)
            Vector3 fanPos = closestFanCenter.position;
            Vector3 playerPos = controller.transform.position;
            float horizontalDistance = Vector3.Distance(
                new Vector3(fanPos.x, 0, fanPos.z),
                new Vector3(playerPos.x, 0, playerPos.z)
            );
            float normalizedHorizontalDistance = Mathf.Clamp01(horizontalDistance / maxLateralDistance);
            float centerMultiplier = centerFalloffCurve.Evaluate(normalizedHorizontalDistance);

            // Combine both multipliers
            forceMultiplier = distanceMultiplier * centerMultiplier;
        }
        else
        {
            // Horizontal fan - push forward
            // Calculate forward distance from fan center
            Vector3 toPlayer = controller.transform.position - closestFanCenter.position;
            float forwardDistance = Vector3.Dot(toPlayer, closestFanCenter.forward);

            // Calculate lateral distance from center axis (how far from the center)
            Vector3 projectedPoint = closestFanCenter.position + closestFanCenter.forward * forwardDistance;
            float lateralDistance = Vector3.Distance(controller.transform.position, projectedPoint);

            // Calculate force falloff based on forward distance
            float normalizedForwardDistance = Mathf.Clamp01(forwardDistance / maxWindDistance);
            float forwardMultiplier = windFalloffCurve.Evaluate(normalizedForwardDistance);

            // Calculate force falloff based on lateral distance (center = strong, edges = weak)
            float normalizedLateralDistance = Mathf.Clamp01(lateralDistance / maxLateralDistance);
            float centerMultiplier = centerFalloffCurve.Evaluate(normalizedLateralDistance);

            // Combine both multipliers
            forceMultiplier = forwardMultiplier * centerMultiplier;

            // Push in the forward direction of the fan
            pushDirection = closestFanCenter.forward;
            pushDirection.y = pushVerticalForce;
        }

        // Apply distance-based force
        float adjustedForce = pushForce * forceMultiplier;
        controller.Knockback(pushDirection, adjustedForce);
    }

    protected override void Initialize()
    {
        base.Initialize();
        actionTrigger.gameObject.SetActive(true);
        currentPlayerInTrigger = null;
    }

    protected override void OnHit(Collider player)
    {
        // Fan doesn't kill player, only pushes them
        // This could be used if you add a second trigger for damage
    }

    protected override void OnAction(Collider player, float totalDuration) => throw new NotImplementedException();
    protected override void OnReactivate(float totalDuration) => throw new NotImplementedException();
}
