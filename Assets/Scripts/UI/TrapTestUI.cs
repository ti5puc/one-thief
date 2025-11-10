using System;
using UnityEngine;
using UnityEngine.UI;

public class TrapTestUI : MonoBehaviour
{
    public static event Action OnConfirmTestTrap;
    public static event Action OnDenyTestTrap;
    
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button denyButton;

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirm);
        denyButton.onClick.AddListener(OnDeny);

        Player.OnTrapModeChanged += Show;
        
        Hide();
    }

    private void OnDestroy()
    {
        confirmButton.onClick.RemoveListener(OnConfirm);
        denyButton.onClick.RemoveListener(OnDeny);
        
        Player.OnTrapModeChanged -= Show;
    }

    private void Show(bool isTrapModeActive, string trapName)
    {
        if (isTrapModeActive == false)
            Show();
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
    
    private void OnConfirm()
    {
        OnConfirmTestTrap?.Invoke();
        Hide();
    }

    private void OnDeny()
    {
        OnDenyTestTrap?.Invoke();
        Hide();
    }
}
