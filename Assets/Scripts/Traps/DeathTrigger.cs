using NaughtyAttributes;
using UnityEngine;

public class DeathTrigger : MonoBehaviour
{
    [SerializeField] private TriggerEventSender triggerEventSender;
    [SerializeField] private bool resetsPlayerPosition = false;
    
    [Header("Custom Death Cam")]
    [SerializeField] private bool hasCustomDeathCam = true;
    
    [Space(10)]
    [SerializeField, ShowIf(nameof(hasCustomDeathCam))] private float customCameraDeathRotationX = 65f;
    [SerializeField, ShowIf(nameof(hasCustomDeathCam))] private float customCameraDeathOffsetY = 4f;
    [SerializeField, ShowIf(nameof(hasCustomDeathCam))] private float customCameraDeathOffsetZ = -4f;

    [Space(10)]
    [SerializeField, ShowIf(nameof(hasCustomDeathCam))] private float deathVfxOffset = -4.2f;


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
        
        if (resetsPlayerPosition && (GameManager.CurrentGameState != GameState.Exploring || controller.IsGodMode))
        {
            controller.ResetPlayerPosition();
            return;
        }
        
        controller.VfxOffset = deathVfxOffset;
        if (hasCustomDeathCam)
            controller.Death(customCameraDeathRotationX, customCameraDeathOffsetY, customCameraDeathOffsetZ, false);
        else
            controller.Death();
    }
}
