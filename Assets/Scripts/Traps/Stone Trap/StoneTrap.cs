using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StoneTrap : TrapBase
{
    [Header("Stone Fall")]
    [SerializeField] private float stoneFallFinalY = 0f;
    [SerializeField] private float stoneFallDuration = 0.5f;
    [SerializeField] private Ease stoneFallEase = Ease.InQuad;

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

        Debug.Log($"Player activated stone trap (fallDuration: {safeFallDuration}, interval: {interval})");
    }

    protected override void OnHit()
    {
        Debug.Log("Player hit by stone trap");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
