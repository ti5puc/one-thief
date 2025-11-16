using System;
using NaughtyAttributes;
using UnityEngine;

[System.Serializable]
public class InventoryData
{
    public int Gold;
}

public class PlayerInventory : MonoBehaviour
{
    public static event Action<int> OnGoldChanged;
    public static event Action<int> OnGoldToGainChanged;
    public static event Action<int> OnGoldToRemoveChanged;

    [Header("Debug")]
    [SerializeField, ReadOnly] private int currentGold = 0;
    [SerializeField, ReadOnly] private int goldCache = 0;
    
    private InventoryData loadedData;
    
    public static PlayerInventory Instance { get; private set; }
    
    public int CurrentGold
    {
        get => loadedData != null ? currentGold = loadedData.Gold : 0;
        private set => currentGold = Mathf.Max(0, value);
    }
    public int GoldCache => goldCache;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        loadedData = SaveSystem.LoadInventory();
        
        TreasureChest.OnAnyChestOpened += AddGoldToGain;
        WinUI.OnGetGold += ApplyGoldInCache;
    }

    private void OnDestroy()
    {
        TreasureChest.OnAnyChestOpened -= AddGoldToGain;
        WinUI.OnGetGold -= ApplyGoldInCache;
    }

    public void ApplyGoldInCache()
    {
        if (goldCache <= 0) return;

        currentGold += goldCache;
        
        var newInventoryData = new InventoryData { Gold = currentGold };
        SaveSystem.SaveInventory(newInventoryData);
        loadedData = newInventoryData;
        
        OnGoldChanged?.Invoke(CurrentGold);
        ClearGoldCache();
    }

    public void SpendGoldToRemove()
    {
        if (goldCache <= 0) return;

        currentGold -= goldCache;
        
        var newInventoryData = new InventoryData { Gold = currentGold };
        SaveSystem.SaveInventory(newInventoryData);
        loadedData = newInventoryData;
        
        OnGoldChanged?.Invoke(CurrentGold);
        ClearGoldCache();
    }
    
    public void AddGoldToGain(int amount)
    {
        if (amount <= 0) return;
        if (GameManager.IsTestingToSubmit) return;

        goldCache += amount;
        
        OnGoldToGainChanged?.Invoke(goldCache);
    }
    
    public void AddGoldToRemove(int amount)
    {
        if (amount <= 0) return;

        goldCache += amount;
        
        OnGoldToRemoveChanged?.Invoke(goldCache);
    }

    public void ClearGoldCache()
    {
        goldCache = 0;
        OnGoldToRemoveChanged?.Invoke(goldCache);
    }
}