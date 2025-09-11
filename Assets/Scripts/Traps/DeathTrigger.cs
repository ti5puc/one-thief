using UnityEngine;

public class DeathTrigger : MonoBehaviour
{
    [SerializeField] private TriggerEventSender triggerEventSender;

    private bool hasCustomDeathCam = false;
    private float customDeathCameraRotationX = 20f;
    private float customDeathCameraOffsetY = 0f;
    private float customDeathCameraOffsetZ = 0f;

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

        var controller = other.GetComponent<MovePlaceholder>();

        if (hasCustomDeathCam)
            controller.Death(customDeathCameraRotationX, customDeathCameraOffsetY, customDeathCameraOffsetZ);
        else
            controller.Death();
    }

    public void SetCustomDeathCam(float rotationX, float offsetY, float offsetZ)
    {
        hasCustomDeathCam = true;
        customDeathCameraRotationX = rotationX;
        customDeathCameraOffsetY = offsetY;
        customDeathCameraOffsetZ = offsetZ;
    }
}
