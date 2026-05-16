using UnityEngine;
using DG.Tweening;
using System;

public class TweenPopupUI : MonoBehaviour
{
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private Ease popupEase = Ease.OutBack;
    [SerializeField] private Ease popdownEase = Ease.InBack;

    void OnEnable()
    {
        transform.localScale = Vector3.zero;
        transform.DOKill();
        transform.DOScale(Vector3.one, duration).SetEase(popupEase);
    }

    public void Hide(bool disableGameObject = true)
    {
        transform.DOKill();
        transform.DOScale(Vector3.zero, duration)
            .SetEase(popdownEase)
            .OnComplete(() =>
            {
                if (disableGameObject)
                    gameObject.SetActive(false);
            });
    }
    
    public void Hide(Action onComplete, bool disableGameObject = true)
    {
        transform.DOKill();
        transform.DOScale(Vector3.zero, duration)
            .SetEase(popdownEase)
            .OnComplete(() =>
            {
                if (disableGameObject)
                    gameObject.SetActive(false);
                onComplete?.Invoke();
            });
    }
}
