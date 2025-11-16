using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;

public class TreasureCollectCounter : MonoBehaviour
{
    public static event Action OnAllTreasuresCollected;
    
    [SerializeField, ReadOnly] private int collectedTreasureCount = 0;
    [SerializeField, ReadOnly] private List<TreasureChest> treasuresOnScene;
    
    private void Awake()
    {
        PlayerSave.OnLevelLoaded += CacheTreasuresOnScene;
    }
    
    private void OnDestroy()
    {
        PlayerSave.OnLevelLoaded -= CacheTreasuresOnScene;
        
        foreach (var treasure in treasuresOnScene)
            treasure.OnChestOpened -= HandleTreasureCollected;
    }

    private void CacheTreasuresOnScene()
    {
        treasuresOnScene = FindObjectsOfType<TreasureChest>().ToList();
        foreach (var treasure in treasuresOnScene)
            treasure.OnChestOpened += HandleTreasureCollected;
    }
    
    private void HandleTreasureCollected(int goldAmount)
    {
        collectedTreasureCount++;
        
        if (collectedTreasureCount >= treasuresOnScene.Count)
            OnAllTreasuresCollected?.Invoke();
    }
}
