using DG.Tweening;
using UnityEngine;

public class FakeFloorTrap_v2 : TrapBase
{
    [Header("Fake Floor Settings")]
    [SerializeField] private float fallDuration = 1f;

    [Header("Custom Death Cam")]
    [SerializeField] private float customCameraDeathRotationX = 40f;
    [SerializeField] private float customCameraDeathOffsetY = 3f;
    [SerializeField] private float customCameraDeathOffsetZ = -2f;

    [Space(10)]
    [SerializeField] private float deathVfxOffset = -4f;

    [Header("References")]
    [SerializeField] private GameObject fakeFloorVisual;
    [SerializeField] private DeathTrigger deathTrigger;

    private bool groundDisabled = false;

    protected override void Awake()
    {
        base.Awake();
        deathTrigger.SetCustomDeathCam(customCameraDeathRotationX, customCameraDeathOffsetY, customCameraDeathOffsetZ);
    }

    protected override void OnAction(float totalDuration) { }

    protected override void OnHit(Collider player)
    {
        Debug.Log("Player hit by fake floor trap");

        fakeFloorVisual.SetActive(false);

        var movePlaceholder = player.GetComponent<PlayerDeathIdentifier>();
        movePlaceholder.VfxOffset = deathVfxOffset;

        if (!groundDisabled)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, 5f, LayerMask.GetMask("Ground"));
            if (colliders.Length > 0)
            {
                Collider nearest = colliders[0];
                float minDistance = Vector3.Distance(transform.position, nearest.transform.position);
                foreach (var collider in colliders)
                {
                    float distance = Vector3.Distance(transform.position, collider.transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearest = collider;
                    }
                }

                nearest.gameObject.SetActive(false);
                groundDisabled = true;
                Debug.Log("Nearest ground object found: " + nearest.name);
            }
        }
    }

    protected override void OnReactivate(float totalDuration) { }
}
