using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class TweenHoverPopUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float duration = 0.2f;
    [SerializeField] private Ease ease = Ease.OutBack;

    private Vector3 _originalScale;

    void Awake()
    {
        _originalScale = Vector3.one;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.DOKill();
        transform.DOScale(_originalScale * hoverScale, duration).SetEase(ease);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.DOKill();
        transform.DOScale(_originalScale, duration).SetEase(ease);
    }
}
