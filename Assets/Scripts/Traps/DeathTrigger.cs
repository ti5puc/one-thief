using UnityEngine;

public class DeathTrigger : MonoBehaviour
{
    [SerializeField] private TriggerEventSender triggerEventSender;

    private bool hasCustomDeathCam = false;
    private float customDeathCameraRotationX = 20f;
    private float customDeathCameraOffsetY = 0f;
    private float customDeathCameraOffsetZ = 0f;
    private float deathVfxOffset = 0f;

    private void Awake()
    {
        triggerEventSender.OnEnter += OnTriggerEnter;
    }

    private void OnDestroy()
    {
        triggerEventSender.OnEnter -= OnTriggerEnter;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") == false) return;

        var controller = other.GetComponent<PlayerDeathIdentifier>();
        controller.VfxOffset = deathVfxOffset;
        
        if (hasCustomDeathCam)
            controller.Death(customDeathCameraRotationX, customDeathCameraOffsetY, customDeathCameraOffsetZ, false);
        else
            controller.Death();
    }

    public void SetCustomDeathCam(float rotationX, float offsetY, float offsetZ, float deathVfxOffset = 0f)
    {
        hasCustomDeathCam = true;
        customDeathCameraRotationX = rotationX;
        customDeathCameraOffsetY = offsetY;
        customDeathCameraOffsetZ = offsetZ;
        this.deathVfxOffset = deathVfxOffset;
    }
}
