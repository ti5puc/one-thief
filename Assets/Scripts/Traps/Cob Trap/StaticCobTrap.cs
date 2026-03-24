using DG.Tweening;
using UnityEngine;

public class StaticCobTrap : TrapBase
{
    [Header("Cob Trap Settings")]
    [SerializeField] private GameObject cobObject;
    [SerializeField] private float rotationSpeed = 5f;

    protected override void OnAlwaysActive()
    {
        if (GameManager.CurrentGameState == GameState.Building) return;
        if (GameManager.IsGamePaused) return;

        // rotate the cob object around its local Y-axis using dotween for smooth rotation
        cobObject.transform.Rotate(0, 0, rotationSpeed * Time.deltaTime, Space.Self);
    }

    protected override void OnHit(Collider player)
    {
        Debug.Log("Player hit by cob trap");
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
