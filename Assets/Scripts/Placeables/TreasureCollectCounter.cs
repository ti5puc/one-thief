using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;

public class TreasureCollectCounter : MonoBehaviour
{
    public static event Action OnAllTreasuresCollected;
    
    [SerializeField] private PlaceableSettings treasureChestSettings;
    
    [Header("Debug")]
    [SerializeField, ReadOnly] private int collectedTreasureCount = 0;
    [SerializeField, ReadOnly] private List<TreasureChest> treasuresOnScene;
    
    public int CollectedTreasureCount => collectedTreasureCount;
    public int TreasuresOnScene => treasuresOnScene.Count;
    
    private void Awake()
    {
        Player.OnTrapPlaced += CacheTreasuresOnScene;
        PlayerSave.OnLevelLoaded += CacheTreasuresOnScene;
    }
    
    private void OnDestroy()
    {
        Player.OnTrapPlaced -= CacheTreasuresOnScene;
        PlayerSave.OnLevelLoaded -= CacheTreasuresOnScene;
        
        if (treasuresOnScene != null)
        {
            treasuresOnScene.RemoveAll(t => t == null);
            foreach (var treasure in treasuresOnScene)
                treasure.OnChestOpened -= HandleTreasureCollected;
        }
    }

    private void CacheTreasuresOnScene(PlaceableSettings settings)
    {
        if (settings != treasureChestSettings) return;
        CacheTreasuresOnScene();
    }
    
    private void CacheTreasuresOnScene()
    {
        treasuresOnScene = FindObjectsOfType<TreasureChest>().ToList();
        treasuresOnScene.RemoveAll(t => t == null);
        
        foreach (var treasure in treasuresOnScene)
            treasure.OnChestOpened += HandleTreasureCollected;
    }
    
    private void HandleTreasureCollected(int goldAmount)
    {
        treasuresOnScene.RemoveAll(t => t == null);
        
        if (GameManager.CurrentGameState != GameState.Exploring)
            return;
        
        collectedTreasureCount++;
        if (collectedTreasureCount >= treasuresOnScene.Count)
            OnAllTreasuresCollected?.Invoke();
    }
}
