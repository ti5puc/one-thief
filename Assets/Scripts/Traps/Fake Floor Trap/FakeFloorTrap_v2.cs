using System;
using DG.Tweening;
using UnityEngine;

public class FakeFloorTrap_v2 : TrapBase
{
    [Header("Fake Floor Settings")]
    [SerializeField] private float vibrationDuration = 1f;
    [SerializeField] private float vibrationStrength = .2f;
    [SerializeField] private int vibrationFrequency = 10;
    [SerializeField] private Vector3 vibrationRotation = new Vector3(5f, 5f, 5f);

    [Header("Custom Death Cam")]
    [SerializeField] private float customCameraDeathRotationX = 40f;
    [SerializeField] private float customCameraDeathOffsetY = 3f;
    [SerializeField] private float customCameraDeathOffsetZ = -2f;

    [Space(10)]
    [SerializeField] private float deathVfxOffset = -4f;

    [Header("References")]
    [SerializeField] private GameObject fakeFloorVisual;
    [SerializeField] private DeathTrigger deathTrigger;

    protected override void Awake()
    {
        base.Awake();
        deathTrigger.SetCustomDeathCam(customCameraDeathRotationX, customCameraDeathOffsetY, customCameraDeathOffsetZ);
    }

    protected override void OnAction(Collider player, float totalDuration)
    {
        Debug.Log("Player activated fake floor trap");

        var movePlaceholder = player.GetComponent<PlayerDeathIdentifier>();
        movePlaceholder.VfxOffset = deathVfxOffset;

        float safeBreakDuration = Mathf.Min(vibrationDuration, totalDuration);
        float interval = Mathf.Max(totalDuration - safeBreakDuration, 0f);

        Sequence seq = DOTween.Sequence();
        if (interval > 0f)
            seq.AppendInterval(interval);

        if (foundNearestGround)
        {
            var groundTransform = fakeFloorVisual.transform;
            Sequence shakeSeq = DOTween.Sequence();
            shakeSeq.Join(groundTransform.DOShakePosition(safeBreakDuration, vibrationStrength, vibrationFrequency));
            shakeSeq.Join(groundTransform.DOShakeRotation(safeBreakDuration, vibrationRotation, vibrationFrequency));
            shakeSeq.OnComplete(() =>
            {
                fakeFloorVisual.SetActive(false);
            });
        }
    }

    protected override void OnHit(Collider player) { }

    protected override void OnReactivate(float totalDuration)
    {
        if (!foundNearestGround) return;

        float safeBreakDuration = Mathf.Min(vibrationDuration, totalDuration);
        float interval = Mathf.Max(totalDuration - safeBreakDuration, 0f);

        Sequence seq = DOTween.Sequence();
        if (interval > 0f)
            seq.AppendInterval(interval);

        var originalScale = fakeFloorVisual.transform.localScale;
        fakeFloorVisual.transform.localScale = Vector3.zero;
        fakeFloorVisual.SetActive(true);
        seq.Append(fakeFloorVisual.transform.DOScale(originalScale, .3f)).SetEase(Ease.OutQuad);

        Debug.Log($"Fake floor trap reactivated (riseDuration: {safeBreakDuration}, interval: {interval})");
    }

    protected override void OnAlwaysActive() => throw new NotImplementedException();
}
