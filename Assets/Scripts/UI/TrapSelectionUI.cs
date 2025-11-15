using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TrapSelectionUI : MonoBehaviour
{
    [SerializeField] private TrapSelectionCardUI trapSelectionCardUIPrefab;
    [SerializeField] private Transform scrollViewParent;

    [Space(10)]
    [SerializeField] private RectTransform tooltipTransform;
    [SerializeField] private TMP_Text tooltipText;
    [SerializeField] private Vector3 tooltipOffset;

    private void Awake()
    {
        Hide();

        Player.OnToggleTrapSelect += UpdateTrapSelectionUI;
    }

    private void OnDestroy()
    {
        Player.OnToggleTrapSelect -= UpdateTrapSelectionUI;
    }

    private void Update()
    {
        Vector3 mousePosition = Input.mousePosition;
        Vector3 tooltipPos = mousePosition + new Vector3(tooltipTransform.rect.width / 2f, 0, 0) + tooltipOffset;
        tooltipTransform.position = tooltipPos;
    }

    public void ToggleTooltip(string tooltip, bool isActive)
    {
        tooltipText.text = tooltip;
        tooltipTransform.gameObject.SetActive(isActive);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void UpdateTrapSelectionUI(bool isActive, List<PlaceableSettings> trapsSettings, int selectedTrapIndex)
    {
        if (!isActive)
        {
            Hide();
            return;
        }

        foreach (Transform child in scrollViewParent)
            Destroy(child.gameObject);

        for (int trapIndex = 0; trapIndex < trapsSettings.Count; trapIndex++)
        {
            PlaceableSettings trapSettings = trapsSettings[trapIndex];
            var trapCard = Instantiate(trapSelectionCardUIPrefab, scrollViewParent);
            trapCard.SetTrapSettings(this, trapSettings, trapIndex, trapIndex == selectedTrapIndex);
        }

        Show();
    }
}
