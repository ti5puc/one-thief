using System;
using UnityEngine;

public class FanTrap : TrapBase
{
    [Header("Fan Settings")]
    [SerializeField] private float pushForce = 8f;
    [SerializeField] private float pushVerticalForce = 1f;
    [SerializeField] private float pushInterval = 0.1f; // How often to apply push force

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

        // Calculate direction from closest fan center to player (outwards)
        Vector3 fanToPlayer = controller.transform.position - closestFanCenter.position;
        fanToPlayer.y = 0f; // Keep horizontal
        fanToPlayer.Normalize();

        // Add vertical component
        Vector3 pushDirection = fanToPlayer;
        pushDirection.y = pushVerticalForce;
        pushDirection.Normalize();

        controller.Knockback(pushDirection, pushForce);
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
