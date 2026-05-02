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
        Player.OnTrapRemoved += CacheTreasuresOnScene;
    }

    private void OnDestroy()
    {
        Player.OnTrapPlaced -= CacheTreasuresOnScene;
        PlayerSave.OnLevelLoaded -= CacheTreasuresOnScene;
        Player.OnTrapRemoved -= CacheTreasuresOnScene;
        
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

        // Distribute total gold among chests when playing a level (not during build/test)
        int totalGold = SaveSystem.NextLevelTotalGold;
        if (totalGold > 0 && treasuresOnScene.Count > 0 && GameManager.CurrentGameState == GameState.Exploring)
        {
            int[] distributed = DistributeGold(totalGold, treasuresOnScene.Count);
            for (int i = 0; i < treasuresOnScene.Count; i++)
                treasuresOnScene[i].SetGoldAmount(distributed[i]);
        }
    }

    private static int[] DistributeGold(int totalGold, int chestCount)
    {
        if (chestCount == 1)
            return new int[] { totalGold };

        int avg = totalGold / chestCount;
        int maxDeviation = Mathf.Max(1, Mathf.RoundToInt(avg * 0.4f));
        int[] amounts = new int[chestCount];
        int remaining = totalGold;

        System.Random rng = new System.Random();
        for (int i = 0; i < chestCount - 1; i++)
        {
            int chestsLeft = chestCount - i;
            int minAmount = Mathf.Max(1, avg - maxDeviation);
            int maxAmount = Mathf.Min(avg + maxDeviation, remaining - (chestsLeft - 1));
            if (maxAmount < minAmount) maxAmount = minAmount;
            amounts[i] = rng.Next(minAmount, maxAmount + 1);
            remaining -= amounts[i];
        }
        amounts[chestCount - 1] = Mathf.Max(1, remaining);
        return amounts;
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
