using System;
using NaughtyAttributes;
using UnityEngine;

public class PlayerGold : MonoBehaviour
{
    public static event Action<int> OnGoldChanged;

    [Header("Debug")]
    [SerializeField, ReadOnly] private int currentGold = 0;

    public int CurrentGold
    {
        get => currentGold;
        private set => currentGold = Mathf.Max(0, value);
    }

    private void Awake()
    {
        TreasureChest.OnChestOpened += AddGold;
    }

    private void OnDestroy()
    {
        TreasureChest.OnChestOpened -= AddGold;
    }

    private void Start()
    {
        OnGoldChanged?.Invoke(CurrentGold);
    }

    public void AddGold(int amount)
    {
        if (amount <= 0) return;

        currentGold += amount;
        OnGoldChanged?.Invoke(CurrentGold);
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0) return false;
        if (CurrentGold < amount) return false;

        CurrentGold -= amount;
        OnGoldChanged?.Invoke(CurrentGold);
        return true;
    }
}