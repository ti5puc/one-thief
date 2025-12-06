using System;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class InventoryData
{
    public int Gold;
    public string PlayerName;
}

public class PlayerInventory : MonoBehaviour
{
    public static event Action<int> OnGoldChanged;
    public static event Action<int> OnGoldToGainChanged;
    public static event Action<int> OnGoldToRemoveChanged;
    
    [SerializeField] private InputActionReference gainGoldHackAction;

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
        gainGoldHackAction.action.Enable();
        gainGoldHackAction.action.performed += GainGoldHack;
        
        TreasureChest.OnAnyChestOpened += AddGoldToGain;
        WinUI.OnGetGold += ApplyGoldInCache;
        
        // Load from Firebase when authentication completes
        FirebaseManager.OnAuthenticationComplete += LoadInventoryFromFirebase;
    }

    private void OnDestroy()
    {
        // gainGoldHackAction.action.Disable();
        // gainGoldHackAction.action.performed -= GainGoldHack;
        
        TreasureChest.OnAnyChestOpened -= AddGoldToGain;
        WinUI.OnGetGold -= ApplyGoldInCache;
        
        FirebaseManager.OnAuthenticationComplete -= LoadInventoryFromFirebase;
    }

    private async void LoadInventoryFromFirebase()
    {
        try
        {
            Debug.Log("[PlayerInventory] Starting to load inventory from Firebase...");
            
            InventoryData firebaseData = await SaveSystem.LoadInventoryFromFirebase();
            
            if (firebaseData != null)
            {
                // Use the Firebase data
                loadedData = firebaseData;
                Debug.Log($"[PlayerInventory] Loaded from Firebase. Gold: {CurrentGold}");
            }
            else
            {
                // Reload local data if Firebase had nothing
                loadedData = SaveSystem.LoadInventory();
                Debug.Log($"[PlayerInventory] No Firebase data, using local. Gold: {CurrentGold}");
                
                if (loadedData != null && loadedData.Gold > 0)
                {
                    // Save local data to Firebase
                    SaveSystem.SaveInventory(loadedData);
                    Debug.Log($"[PlayerInventory] Uploaded local data to Firebase. Gold: {CurrentGold}");
                }
            }
            
            // Notify UI
            OnGoldChanged?.Invoke(CurrentGold);
            
            Debug.Log("[PlayerInventory] Successfully completed Firebase inventory load");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PlayerInventory] Error loading inventory from Firebase: {ex.Message}\n{ex.StackTrace}");
            
            // Fallback to local data on error
            try
            {
                loadedData = SaveSystem.LoadInventory();
                Debug.Log($"[PlayerInventory] Fallback to local data. Gold: {CurrentGold}");
                OnGoldChanged?.Invoke(CurrentGold);
            }
            catch (Exception fallbackEx)
            {
                Debug.LogError($"[PlayerInventory] Error loading local inventory: {fallbackEx.Message}");
            }
        }
    }

    public void ApplyGoldInCache()
    {
        if (goldCache <= 0) return;

        currentGold += goldCache;
        
        var newInventoryData = new InventoryData 
        { 
            Gold = currentGold,
            PlayerName = FirebaseManager.Instance.PlayerName
        };
        SaveSystem.SaveInventory(newInventoryData);
        loadedData = newInventoryData;
        
        OnGoldChanged?.Invoke(CurrentGold);
        ClearGoldCache();
    }

    public void SpendGoldToRemove()
    {
        if (goldCache <= 0) return;

        currentGold -= goldCache;
        
        var newInventoryData = new InventoryData 
        { 
            Gold = currentGold,
            PlayerName = FirebaseManager.Instance.PlayerName
        };
        SaveSystem.SaveInventory(newInventoryData);
        loadedData = newInventoryData;
        
        OnGoldChanged?.Invoke(CurrentGold);
        ClearGoldCache();
    }
    
    public void AddGoldToGain(int amount)
    {
        if (amount <= 0) return;
        if (GameManager.IsTestingToSubmit) return;
        if (GameManager.CurrentGameState != GameState.Exploring) return;

        goldCache += amount;
        
        OnGoldToGainChanged?.Invoke(goldCache);
    }
    
    public void AddGoldToRemove(int amount)
    {
        if (amount <= 0) return;
        if (GameManager.CurrentGameState == GameState.Exploring) return;

        goldCache += amount;
        
        OnGoldToRemoveChanged?.Invoke(goldCache);
    }

    public void ClearGoldCache()
    {
        goldCache = 0;
        OnGoldToRemoveChanged?.Invoke(goldCache);
    }
    
    private void GainGoldHack(InputAction.CallbackContext callback)
    {
        goldCache += 1000;
        ApplyGoldInCache();
    }
}