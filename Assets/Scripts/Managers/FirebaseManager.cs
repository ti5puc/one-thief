using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }
    
    public static event Action OnAuthenticationComplete;
    public static event Action<string> OnAuthenticationFailed;
    
    private FirebaseAuth auth;
    private FirebaseFirestore firestore;
    private FirebaseUser currentUser;
    
    [Header("Status")]
    [SerializeField] private bool isInitialized;
    [SerializeField] private bool isAuthenticated;
    [SerializeField] private string userId = "";
    [SerializeField] private bool isFirstLogin = false;
    [SerializeField] private string playerName = "";
    
    public bool IsInitialized => isInitialized;
    public bool IsAuthenticated => isAuthenticated;
    public string UserId => userId;
    public bool IsFirstLogin => Instance != null ? Instance.isFirstLogin : false;
    public string PlayerName => Instance != null ? Instance.playerName : "";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                firestore = FirebaseFirestore.DefaultInstance;
                isInitialized = true;
                
                Debug.Log("[FirebaseManager] Firebase initialized successfully!");
                
                // Auto-login anonymously
                SignInAnonymously();
            }
            else
            {
                Debug.LogError($"[FirebaseManager] Could not resolve Firebase dependencies: {task.Result}");
                isInitialized = false;
            }
        });
    }

    public void SignInAnonymously()
    {
        if (!isInitialized)
        {
            Debug.LogError("[FirebaseManager] Firebase not initialized yet!");
            return;
        }

        auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("[FirebaseManager] SignInAnonymouslyAsync was canceled.");
                OnAuthenticationFailed?.Invoke("Authentication was canceled");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError($"[FirebaseManager] SignInAnonymouslyAsync encountered an error: {task.Exception}");
                OnAuthenticationFailed?.Invoke(task.Exception?.Message ?? "Unknown error");
                return;
            }

            currentUser = task.Result.User;
            userId = currentUser.UserId;
            isAuthenticated = true;
            
            Debug.Log($"[FirebaseManager] User signed in successfully: {userId}");
            
            // Check if this is first login by checking if inventory exists
            CheckFirstLogin();
            
            OnAuthenticationComplete?.Invoke();
        });
    }

    private async void CheckFirstLogin()
    {
        // Load inventory from Firebase to check if user data exists
        string json = await LoadDocument("players", userId);
        
        if (string.IsNullOrEmpty(json))
        {
            // No data found - this is first login
            isFirstLogin = true;
            playerName = "";
            Debug.Log("[FirebaseManager] First login detected - no player data found");
        }
        else
        {
            // Data exists - returning user
            isFirstLogin = false;
            
            // Parse the player name from the inventory data
            try
            {
                var data = new Dictionary<string, object>();
                var dict = JsonUtility.FromJson<Dictionary<string, string>>(json);
                
                // Try to get the json field which contains the actual inventory data
                string inventoryJson = json;
                if (json.Contains("\"json\""))
                {
                    // Extract the nested json field
                    int startIndex = json.IndexOf("\"json\":\"") + 8;
                    int endIndex = json.IndexOf("\",\"timestamp\"");
                    if (endIndex > startIndex)
                    {
                        inventoryJson = json.Substring(startIndex, endIndex - startIndex);
                        inventoryJson = inventoryJson.Replace("\\\"", "\"");
                    }
                }
                
                InventoryData inventoryData = JsonUtility.FromJson<InventoryData>(inventoryJson);
                if (inventoryData != null)
                {
                    playerName = inventoryData.PlayerName ?? "";
                }
                
                Debug.Log($"[FirebaseManager] Returning user - Player: {playerName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseManager] Error parsing player name: {ex.Message}");
                playerName = "";
            }
        }
    }

    public static void SetPlayerName(string name)
    {
        if (Instance != null)
        {
            Instance.playerName = name;
            Instance.isFirstLogin = false;
            Debug.Log($"[FirebaseManager] Player name set to: {name}");
        }
    }

    /// <summary>
    /// Delete player data from Firebase
    /// </summary>
    public static async Task<bool> DeletePlayerData()
    {
        if (Instance != null && Instance.IsAuthenticated)
        {
            string userId = Instance.userId;
            bool success = await Instance.DeleteDocument("players", userId);
            
            if (success)
            {
                // Reset local state
                Instance.isFirstLogin = true;
                Instance.playerName = "";
                Debug.Log("[FirebaseManager] Player data deleted from Firebase");
            }
            
            return success;
        }
        
        return false;
    }

    #region Firestore Document Operations
    
    /// <summary>
    /// Save a JSON document to Firestore
    /// </summary>
    public async Task<bool> SaveDocument(string collection, string documentId, string jsonData)
    {
        if (!isAuthenticated || !isInitialized)
        {
            Debug.LogError("[FirebaseManager] Cannot save document - not authenticated!");
            return false;
        }

        try
        {
            var document = firestore.Collection(collection).Document(documentId);
            
            var data = new Dictionary<string, object>
            {
                { "json", jsonData },
                { "timestamp", FieldValue.ServerTimestamp }
            };

            await document.SetAsync(data);
            
            Debug.Log($"[FirebaseManager] Document saved to {collection}/{documentId}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseManager] Error saving document: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Load a JSON document from Firestore
    /// </summary>
    public async Task<string> LoadDocument(string collection, string documentId)
    {
        if (!isAuthenticated || !isInitialized)
        {
            Debug.LogError("[FirebaseManager] Cannot load document - not authenticated!");
            return null;
        }

        try
        {
            var document = firestore.Collection(collection).Document(documentId);
            var snapshot = await document.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                string json = snapshot.GetValue<string>("json");
                Debug.Log($"[FirebaseManager] Document loaded from {collection}/{documentId}");
                return json;
            }
            else
            {
                Debug.Log($"[FirebaseManager] No document found at {collection}/{documentId}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseManager] Error loading document: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Delete a document from Firestore
    /// </summary>
    public async Task<bool> DeleteDocument(string collection, string documentId)
    {
        if (!isAuthenticated || !isInitialized)
        {
            Debug.LogError("[FirebaseManager] Cannot delete document - not authenticated!");
            return false;
        }

        try
        {
            var document = firestore.Collection(collection).Document(documentId);
            await document.DeleteAsync();
            Debug.Log($"[FirebaseManager] Document deleted from {collection}/{documentId}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseManager] Error deleting document: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Submit a new level to Firestore
    /// </summary>
    public async Task<bool> SubmitLevel(string levelName, string saveJson)
    {
        if (!isAuthenticated || !isInitialized)
        {
            Debug.LogError("[FirebaseManager] Cannot submit level - not authenticated!");
            return false;
        }

        if (string.IsNullOrWhiteSpace(levelName))
        {
            Debug.LogError("[FirebaseManager] Level name cannot be empty!");
            return false;
        }

        try
        {
            var levelsCollection = firestore.Collection("levels");
            var newLevelDoc = levelsCollection.Document(); // Auto-generate ID
            
            var data = new Dictionary<string, object>
            {
                { "levelName", levelName },
                { "playerId", userId },
                { "saveJson", saveJson },
                { "timestamp", FieldValue.ServerTimestamp }
            };

            await newLevelDoc.SetAsync(data);
            
            Debug.Log($"[FirebaseManager] Level '{levelName}' submitted successfully with ID: {newLevelDoc.Id}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseManager] Error submitting level: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get a paginated list of all levels
    /// Returns list of (levelId, saveJson) pairs
    /// </summary>
    public async Task<List<(string levelId, string saveJson)>> GetAllLevels(int maxResults = 10)
    {
        if (!isAuthenticated || !isInitialized)
        {
            Debug.LogError("[FirebaseManager] Cannot get levels - not authenticated!");
            return new List<(string, string)>();
        }

        try
        {
            // Query the top-level levels collection
            var levelsQuery = firestore.Collection("levels")
                .OrderByDescending("timestamp")
                .Limit(maxResults);

            var snapshot = await levelsQuery.GetSnapshotAsync();
            var levels = new List<(string, string)>();

            foreach (var document in snapshot.Documents)
            {
                string saveJson = document.GetValue<string>("saveJson");
                levels.Add((document.Id, saveJson));
            }

            Debug.Log($"[FirebaseManager] Retrieved {levels.Count} levels from Firestore");
            return levels;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseManager] Error getting levels: {ex.Message}");
            return new List<(string, string)>();
        }
    }

    /// <summary>
    /// Get levels created by the current player
    /// Returns list of (levelId, saveJson) pairs
    /// </summary>
    public async Task<List<(string levelId, string saveJson)>> GetMyLevels(int maxResults = 10)
    {
        if (!isAuthenticated || !isInitialized)
        {
            Debug.LogError("[FirebaseManager] Cannot get levels - not authenticated!");
            return new List<(string, string)>();
        }

        try
        {
            // Query levels where playerId matches current user
            // Note: Removed OrderByDescending to avoid composite index requirement
            var levelsCollection = firestore.Collection("levels");
            Query query = levelsCollection
                .WhereEqualTo("playerId", userId)
                .Limit(maxResults);

            var snapshot = await query.GetSnapshotAsync();
            var levels = new List<(string, string)>();

            foreach (var document in snapshot.Documents)
            {
                string saveJson = document.GetValue<string>("saveJson");
                levels.Add((document.Id, saveJson));
            }

            Debug.Log($"[FirebaseManager] Retrieved {levels.Count} of my levels from Firestore");
            return levels;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseManager] Error getting my levels: {ex.Message}");
            return new List<(string, string)>();
        }
    }
    
    #endregion
}
