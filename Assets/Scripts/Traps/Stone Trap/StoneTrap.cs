using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StoneTrap : TrapBase
{
    [Header("Stone Fall")]
    [SerializeField] private float stoneFallFinalY = 0f;
    [SerializeField] private float stoneFallDuration = 0.5f;
    [SerializeField] private Ease stoneFallEase = Ease.InQuad;

    [Space(10)]
    [SerializeField] private bool useGravity;

    [Header("References")]
    [SerializeField] private GameObject stoneObject;

    private Vector3 stoneInitialPosition;

    protected override void Awake()
    {
        base.Awake();

        stoneInitialPosition = stoneObject.transform.localPosition;
    }

    protected override void OnAction(float totalDuration)
    {
        float safeFallDuration = Mathf.Min(stoneFallDuration, totalDuration);
        float interval = Mathf.Max(totalDuration - safeFallDuration, 0f);

        Sequence seq = DOTween.Sequence();
        if (interval > 0f)
            seq.AppendInterval(interval);

        seq.Append(stoneObject.transform.DOLocalMoveY(stoneFallFinalY, safeFallDuration).SetEase(stoneFallEase));
        seq.onComplete += () =>
        {
            if (useGravity)
            {
                var rb = stoneObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                }
            }
        };

        Debug.Log($"Player activated stone trap (fallDuration: {safeFallDuration}, interval: {interval})");
    }

    protected override void OnHit(Collider player)
    {
        Debug.Log("Player hit by stone trap");
        var controller = player.GetComponent<PlayerDeathIdentifier>();
        controller.Death();
    }

    protected override void OnReactivate(float totalDuration)
    {
        float safeFallDuration = Mathf.Min(stoneFallDuration, totalDuration);
        float interval = Mathf.Max(totalDuration - safeFallDuration, 0f);

        Sequence seq = DOTween.Sequence();
        if (interval > 0f)
            seq.AppendInterval(interval);

        seq.Append(stoneObject.transform.DOLocalMoveY(stoneInitialPosition.y, safeFallDuration).SetEase(Ease.OutQuad));

        Debug.Log($"Stone trap reactivated (riseDuration: {safeFallDuration}, interval: {interval})");
    }
}
