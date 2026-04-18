using UnityEngine;

public class WallRunRail : MonoBehaviour
{
    [Header("Wall Run Rail")]
    public bool useObjectForward = true;
    public Transform directionReference;

    public Vector3 GetRunDirection()
    {
        if (!useObjectForward && directionReference != null)
            return directionReference.forward.normalized;

        return transform.forward.normalized;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Player player = collision.collider.GetComponentInParent<Player>();
        if (player == null) return;

        player.EnterWallRun(this);
    }

    private void OnCollisionStay(Collision collision)
    {
        Player player = collision.collider.GetComponentInParent<Player>();
        if (player == null) return;

        player.EnterWallRun(this);
    }

    private void OnCollisionExit(Collision collision)
    {
        Player player = collision.collider.GetComponentInParent<Player>();
        if (player == null) return;

        player.ExitWallRun(this);
    }
}