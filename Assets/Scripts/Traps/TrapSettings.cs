using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = nameof(TrapSettings), menuName = "OneThief/Traps/" + nameof(TrapSettings))]
public class TrapSettings : PlaceableSettings
{
    [Header("Behaviour Settings")]
    [SerializeField] private bool isAlwaysActive = false;
    [SerializeField, HideIf(nameof(isAlwaysActive))] private float delayToHitAfterActivated = 0.7f;

    [Space(10)]
    [SerializeField, HideIf(nameof(isAlwaysActive))] private bool keepHitTriggerAfterActivated = false;
    [SerializeField, HideIf(EConditionOperator.Or, nameof(isAlwaysActive), nameof(keepHitTriggerAfterActivated))] private float hitTriggerActiveTime = .2f;

    [Space(10)]
    [SerializeField, HideIf(nameof(isAlwaysActive))] private bool canReactive;
    [SerializeField, ShowIf(nameof(canReactive))] private float delayBeforeReactivate = 4f;

    public bool IsAlwaysActive => isAlwaysActive;
    public float DelayToHit => delayToHitAfterActivated;
    public bool KeepHitTrigger => keepHitTriggerAfterActivated;
    public float HitTriggerActiveTime => hitTriggerActiveTime;
    public bool CanReactive => canReactive;
    public float DelayBeforeReactivate => delayBeforeReactivate;
}
