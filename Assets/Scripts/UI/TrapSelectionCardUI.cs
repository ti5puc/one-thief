using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TrapSelectionCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public static event Action<PlaceableSettings> OnTrapSelected;

    [SerializeField] private TMP_Text trapNameText;
    [SerializeField] private Button selectButton;

    private TrapSelectionUI parentUI;
    private PlaceableSettings currentSettings;

    public PlaceableSettings PlaceableSettings => currentSettings;

    private void Awake()
    {
        TrapSelectionCardUI.OnTrapSelected += ClearInteractable;
        selectButton.onClick.AddListener(OnSelectButtonClicked);
    }

    private void OnDestroy()
    {
        TrapSelectionCardUI.OnTrapSelected -= ClearInteractable;
        selectButton.onClick.RemoveListener(OnSelectButtonClicked);
    }

    public void SetTrapSettings(TrapSelectionUI parentUI, PlaceableSettings trapSettings, int trapIndex, bool isSelected)
    {
        this.parentUI = parentUI;

        currentSettings = trapSettings;
        trapNameText.text = trapIndex.ToString();

        UpdateInteractable();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (parentUI != null && currentSettings != null)
        {
            var trapName = $"{currentSettings.TrapName}";
            var tooltip = currentSettings.PlacementCost > 0 ? trapName + $"\n $ {currentSettings.PlacementCost}" : trapName;
            parentUI.ToggleTooltip(tooltip, true);
        }
        
        UpdateInteractable();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (parentUI != null && currentSettings != null)
            parentUI.ToggleTooltip(string.Empty, false);
        
        UpdateInteractable();
    }

    private void OnSelectButtonClicked()
    {
        selectButton.interactable = false;
        OnTrapSelected?.Invoke(currentSettings);
    }

    private void ClearInteractable(PlaceableSettings settings)
    {
        if (currentSettings != null && settings != currentSettings)
            selectButton.interactable = true;
        
        UpdateInteractable();
    }
    
    private void UpdateInteractable()
    {
        if (currentSettings is null) return;
        
        var gold = PlayerInventory.Instance.CurrentGold;
        selectButton.interactable = gold >= currentSettings.PlacementCost;
    }
}
