using TMPro;
using UnityEngine;

public class TreasureUI : MonoBehaviour
{
    [SerializeField] private TMP_Text goldAmountText;
    [SerializeField] private TMP_Text interactionHintText;

    private void Awake()
    {
        TreasureChest.OnPlayerEnteredChestArea += ShowInteractionHint;
        TreasureChest.OnPlayerExitedChestArea += HideInteractionHint;
        TreasureChest.OnChestOpened += HideInteractionHint;

        PlayerGold.OnGoldChanged += OnGoldChanged;

        HideInteractionHint();
    }

    private void OnDestroy()
    {
        TreasureChest.OnPlayerEnteredChestArea -= ShowInteractionHint;
        TreasureChest.OnPlayerExitedChestArea -= HideInteractionHint;
        TreasureChest.OnChestOpened -= HideInteractionHint;

        PlayerGold.OnGoldChanged -= OnGoldChanged;
    }

    private void ShowInteractionHint()
    {
        if (GameManager.CurrentGameState != GameState.Exploring) return;
        interactionHintText.gameObject.SetActive(true);
    }

    private void HideInteractionHint(int goldAmount) => HideInteractionHint();
    private void HideInteractionHint()
    {
        if (GameManager.CurrentGameState != GameState.Exploring) return;
        interactionHintText.gameObject.SetActive(false);
    }

    private void OnGoldChanged(int newGoldAmount)
    {
        goldAmountText.text = $"$ {newGoldAmount}";
    }
}
