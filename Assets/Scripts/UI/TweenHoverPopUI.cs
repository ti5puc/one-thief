using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;

public class TweenHoverPopUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float duration = 0.2f;
    [SerializeField] private Ease ease = Ease.OutBack;

    private Button button;
    private Vector3 originalScale;

    void Awake()
    {
        originalScale = Vector3.one;
        button = GetComponent<Button>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;

        transform.DOKill();
        transform.DOScale(originalScale * hoverScale, duration).SetEase(ease);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;

        transform.DOKill();
        transform.DOScale(originalScale, duration).SetEase(ease);
    }
}
