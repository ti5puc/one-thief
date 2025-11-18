using TMPro;
using UnityEngine;

public class TreasureUI : MonoBehaviour
{
    [SerializeField] private TMP_Text goldAmountText;
    [SerializeField] private TMP_Text goldToGainAmountText;
    [SerializeField] private TMP_Text interactionHintText;

    private void Awake()
    {
        TreasureChest.OnPlayerEnteredChestArea += ShowInteractionHint;
        TreasureChest.OnPlayerExitedChestArea += HideInteractionHint;
        TreasureChest.OnAnyChestOpened += HideInteractionHint;
        
        PlayerInventory.OnGoldToGainChanged += OnGoldToGainChanged;
        PlayerInventory.OnGoldToRemoveChanged += OnGoldToRemoveChanged;
        PlayerInventory.OnGoldChanged += OnGoldChanged;

        HideInteractionHint();
    }

    private void OnDestroy()
    {
        TreasureChest.OnPlayerEnteredChestArea -= ShowInteractionHint;
        TreasureChest.OnPlayerExitedChestArea -= HideInteractionHint;
        TreasureChest.OnAnyChestOpened -= HideInteractionHint;

        PlayerInventory.OnGoldToGainChanged -= OnGoldToGainChanged;
        PlayerInventory.OnGoldToRemoveChanged -= OnGoldToRemoveChanged;
        PlayerInventory.OnGoldChanged -= OnGoldChanged;
    }

    private void Start()
    {
        OnGoldChanged(PlayerInventory.Instance.CurrentGold);
        goldToGainAmountText.gameObject.SetActive(false);
    }

    private void ShowInteractionHint()
    {
        if (GameManager.CurrentGameState == GameState.Building) return;
        
        bool isExploring = GameManager.CurrentGameState == GameState.Exploring;
        interactionHintText.text = isExploring ? "Aperte 'E' para interagir" : "(Desabilitado) Aperte 'E' para interagir";
        interactionHintText.gameObject.SetActive(true);
    }

    private void HideInteractionHint(int goldAmount) => HideInteractionHint();
    private void HideInteractionHint()
    {
        interactionHintText.gameObject.SetActive(false);
    }

    private void OnGoldToGainChanged(int newGoldAmount)
    {
        goldToGainAmountText.gameObject.SetActive(newGoldAmount > 0);
        goldToGainAmountText.text = $"+ $ {newGoldAmount}";
    }
    
    private void OnGoldToRemoveChanged(int newGoldAmount)
    {
        goldToGainAmountText.gameObject.SetActive(newGoldAmount > 0);
        goldToGainAmountText.text = $"- $ {newGoldAmount}";
    }
    
    private void OnGoldChanged(int newGoldAmount)
    {
        goldAmountText.text = $"$ {newGoldAmount}";
    }
}
