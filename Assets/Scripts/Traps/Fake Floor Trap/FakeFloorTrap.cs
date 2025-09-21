using System;
using DG.Tweening;
using UnityEngine;

[Obsolete("This trap is deprecated. Use FakeFloor_v2 instead.")]
public class FakeFloorTrap : TrapBase
{
    [Header("Fake Floor Settings")]
    [SerializeField] private float fallDuration = 1f;

    [Header("Custom Death Cam")]
    [SerializeField] private float customCameraDeathRotationX = 40f;
    [SerializeField] private float customCameraDeathOffsetY = 3f;
    [SerializeField] private float customCameraDeathOffsetZ = -2f;

    [Space(10)]
    [SerializeField] private float deathVfxOffset = -5f;

    [Header("References")]
    [SerializeField] private GameObject fakeFloorVisual;
    [SerializeField] private DeathTrigger deathTrigger;

    protected override void Awake()
    {
        base.Awake();
        deathTrigger.SetCustomDeathCam(customCameraDeathRotationX, customCameraDeathOffsetY, customCameraDeathOffsetZ);
    }

    protected override void OnAction(Collider player, float totalDuration) { }

    protected override void OnHit(Collider player)
    {
        fakeFloorVisual.SetActive(false);
        Debug.Log("Player hit by fake floor trap");

        var collider = player.GetComponent<Collider>();
        collider.isTrigger = true;

        var movePlaceholder = player.GetComponent<PlayerDeathIdentifier>();
        // movePlaceholder.DisableMove();
        movePlaceholder.VfxOffset = deathVfxOffset;

        var seq = DOTween.Sequence();
        seq.Join(movePlaceholder.transform.DOMoveX(transform.position.x, 0.3f).SetEase(Ease.InCubic));
        seq.Join(movePlaceholder.transform.DOMoveZ(transform.position.z, 0.3f).SetEase(Ease.OutCubic));
        seq.Join(movePlaceholder.transform.DOMoveY(transform.position.y - 3f, .8f).SetEase(Ease.OutQuad));

        DOVirtual.DelayedCall(fallDuration, () =>
        {
            collider.isTrigger = false;
        });
    }

    protected override void OnReactivate(float totalDuration) { }
}
