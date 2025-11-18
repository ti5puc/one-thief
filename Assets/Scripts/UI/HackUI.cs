using System;
using TMPro;
using UnityEngine;

public class HackUI : MonoBehaviour
{
    [SerializeField] private TMP_Text godModText;
    
    private void Awake()
    {
        PlayerDeathIdentifier.OnGodModeChanged += UpdateGodModeText;
        
        godModText.gameObject.SetActive(false);
    }
    
    private void OnDestroy()
    {
        PlayerDeathIdentifier.OnGodModeChanged -= UpdateGodModeText;
    }

    private void UpdateGodModeText(bool isGodMode)
    {
        godModText.gameObject.SetActive(isGodMode);
    }
}
