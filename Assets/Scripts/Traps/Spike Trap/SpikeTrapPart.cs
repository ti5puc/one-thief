using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;

public class SpikeTrapPart : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float preDelayMoveY = 0.1f;
    [SerializeField] private float preDelayDuration = 0.2f;
    [SerializeField] private Ease preDelayEase = Ease.Linear;

    [Space(10)]
    [SerializeField] private float spikeMoveY = 1.0f;
    [SerializeField] private float spikeMoveDuration = 0.15f;
    [SerializeField] private Ease spikeMoveEase = Ease.Linear;

    [Space(10)]
    [SerializeField,] private float initialPositionY;

    private void Awake()
    {
        transform.localPosition = new Vector3(transform.localPosition.x, initialPositionY, transform.localPosition.z);
    }

    public void Activate(float totalDuration)
    {
        float safePreDelay = preDelayDuration;
        float safeSpikeMove = spikeMoveDuration;
        float safeInterval = totalDuration - (preDelayDuration + spikeMoveDuration);
        if (safeInterval < 0f)
        {
            float available = Mathf.Max(totalDuration, 0f);
            float totalAnim = preDelayDuration + spikeMoveDuration;
            float ratio = totalAnim > 0f ? available / totalAnim : 0f;
            safePreDelay = preDelayDuration * ratio;
            safeSpikeMove = spikeMoveDuration * ratio;
            safeInterval = 0f;
            Debug.LogWarning($"SpikeTrapPart: Animation durations exceed totalDuration! Durations scaled. (preDelay: {safePreDelay}, spikeMove: {safeSpikeMove})");
        }

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOLocalMoveY(initialPositionY + preDelayMoveY, safePreDelay).SetEase(preDelayEase));
        if (safeInterval > 0f)
            seq.AppendInterval(safeInterval);
        seq.Append(transform.DOLocalMoveY(initialPositionY + spikeMoveY, safeSpikeMove).SetEase(spikeMoveEase));
    }

    public void Reactivate(float totalDuration)
    {
        float safeSpikeMove = spikeMoveDuration;
        float safeInterval = totalDuration - spikeMoveDuration;
        if (safeInterval < 0f)
        {
            float available = Mathf.Max(totalDuration, 0f);
            float ratio = spikeMoveDuration > 0f ? available / spikeMoveDuration : 0f;
            safeSpikeMove = spikeMoveDuration * ratio;
            safeInterval = 0f;
            Debug.LogWarning($"SpikeTrapPart: Animation durations exceed totalDuration! Durations scaled. (spikeMove: {safeSpikeMove})");
        }

        Sequence seq = DOTween.Sequence();
        if (safeInterval > 0f)
            seq.AppendInterval(safeInterval);
        seq.Append(transform.DOLocalMoveY(initialPositionY, safeSpikeMove).SetEase(spikeMoveEase));
    }

    [Button]
    private void TestActivate()
    {
        Activate(0.5f);
    }

    [Button]
    private void TestReactivate()
    {
        Reactivate(0.5f);
    }
}
