using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TrapNameUI : MonoBehaviour
{
    [SerializeField] private TMP_Text trapNameText;

    private void Awake()
    {
        Player.OnTrapModeChanged += UpdateVisibility;
        Player.OnToggleTrapSelect += UpdateVisibility;
        Player.OnSelectedTrapChanged += SetupName;

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        Player.OnTrapModeChanged -= UpdateVisibility;
        Player.OnToggleTrapSelect -= UpdateVisibility;
        Player.OnSelectedTrapChanged -= SetupName;
    }

    private void UpdateVisibility(bool isActive, List<PlaceableSettings> trapsSettings, int selectedTrapIndex)
    {
        gameObject.SetActive(!isActive);
    }

    private void UpdateVisibility(bool isTrapModeActive, string currentTrapName)
    {
        gameObject.SetActive(isTrapModeActive);
        SetupName(currentTrapName);
    }

    private void SetupName(string trapName)
    {
        if (trapNameText != null)
        {
            trapNameText.text = $"Trap: {trapName}";
        }
    }
}
