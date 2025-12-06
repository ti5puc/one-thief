using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }
    
    public static event Action OnAuthenticationComplete;
    public static event Action<string> OnAuthenticationFailed;
    
    private FirebaseAuth auth;
    private FirebaseFirestore firestore;
    private FirebaseUser currentUser;
    
    // Cache for player names to avoid repeated Firebase queries
    private static Dictionary<string, string> playerNameCache = new Dictionary<string, string>();
    
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
        // Use async method instead of ContinueWithOnMainThread
        _ = InitializeFirebaseAsync();
    }

    private async Task InitializeFirebaseAsync()
    {
        try
        {
            Debug.Log("[FirebaseManager] Checking Firebase dependencies...");
            
            var dependencyTask = FirebaseApp.CheckAndFixDependenciesAsync();
            await dependencyTask;
            
            var dependencyStatus = dependencyTask.Result;
            
            if (dependencyStatus == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                firestore = FirebaseFirestore.DefaultInstance;
                isInitialized = true;
                
                Debug.Log("[FirebaseManager] Firebase initialized successfully!");
                
                // Auto-login anonymously
                await SignInAnonymouslyAsync();
            }
            else
            {
                Debug.LogError($"[FirebaseManager] Could not resolve Firebase dependencies: {dependencyStatus}");
                isInitialized = false;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseManager] Error initializing Firebase: {ex.Message}\n{ex.StackTrace}");
            isInitialized = false;
        }
    }

    private async Task SignInAnonymouslyAsync()
    {
        if (!isInitialized)
        {
            Debug.LogError("[FirebaseManager] Firebase not initialized yet!");
            return;
        }

        try
        {
            Debug.Log("[FirebaseManager] Starting anonymous sign in...");
            
            var authTask = auth.SignInAnonymouslyAsync();
            await authTask;
            
            if (authTask.IsCanceled)
            {
                Debug.LogError("[FirebaseManager] SignInAnonymouslyAsync was canceled.");
                OnAuthenticationFailed?.Invoke("Authentication was canceled");
                return;
            }
            
            if (authTask.IsFaulted)
            {
                Debug.LogError($"[FirebaseManager] SignInAnonymouslyAsync encountered an error: {authTask.Exception}");
                OnAuthenticationFailed?.Invoke(authTask.Exception?.Message ?? "Unknown error");
                return;
            }

            currentUser = authTask.Result.User;
            userId = currentUser.UserId;
            isAuthenticated = true;
            
            Debug.Log($"[FirebaseManager] User signed in successfully: {userId}");
            
            // Check if this is first login by checking if inventory exists
            await CheckFirstLoginAndNotify();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseManager] Exception during sign in: {ex.Message}\n{ex.StackTrace}");
            OnAuthenticationFailed?.Invoke(ex.Message);
        }
    }

    private async Task CheckFirstLoginAndNotify()
    {
        try
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
                    // LoadDocument returns the inventory data directly as JSON
                    var data = MiniJSON.Json.Deserialize(json) as Dictionary<string, object>;
                    if (data != null && data.ContainsKey("PlayerName"))
                    {
                        playerName = data["PlayerName"].ToString();
                    }
                    else
                    {
                        playerName = "";
                    }
                    
                    Debug.Log($"[FirebaseManager] Returning user - Player: {playerName}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[FirebaseManager] Error parsing player name: {ex.Message}");
                    playerName = "";
                }
            }
            
            // Trigger authentication complete event after checking first login
            OnAuthenticationComplete?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseManager] Error in CheckFirstLoginAndNotify: {ex.Message}\n{ex.StackTrace}");
            
            // Even if there's an error, set default values and notify
            isFirstLogin = true;
            playerName = "";
            OnAuthenticationComplete?.Invoke();
        }
    }

    public static void SetPlayerName(string name)
    {
        if (Instance != null)
        {
            Instance.playerName = name;
            Instance.isFirstLogin = false;
            
            // Add to cache
            playerNameCache[Instance.userId] = name;
            
            Debug.Log($"[FirebaseManager] Player name set to: {name}");
        }
    }

    /// <summary>
    /// Get a player's name from their userId with caching
    /// </summary>
    public async Task<string> GetPlayerName(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            return "Unknown";
        }

        // Check cache first
        if (playerNameCache.ContainsKey(playerId))
        {
            return playerNameCache[playerId];
        }

        // If it's the current user, use cached name
        if (playerId == userId && !string.IsNullOrEmpty(playerName))
        {
            playerNameCache[playerId] = playerName;
            return playerName;
        }

        // Load from Firebase
        if (!isAuthenticated || !isInitialized)
        {
            Debug.LogWarning($"[FirebaseManager] Cannot get player name - not authenticated");
            return playerId; // Return playerId as fallback
        }

        try
        {
            string json = await LoadDocument("players", playerId);
            
            if (!string.IsNullOrEmpty(json))
            {
                var data = MiniJSON.Json.Deserialize(json) as Dictionary<string, object>;
                if (data != null && data.ContainsKey("PlayerName"))
                {
                    string name = data["PlayerName"].ToString();
                    
                    // Cache the result
                    playerNameCache[playerId] = name;
                    
                    return name;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseManager] Error loading player name for {playerId}: {ex.Message}");
        }

        // Fallback to playerId if we couldn't get the name
        return playerId;
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
    /// Deserializes the JSON and saves each field individually
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
            Dictionary<string, object> data = new Dictionary<string, object>();
            
            try
            {
                // Use a simple JSON parser for flexibility
                var parsedData = MiniJSON.Json.Deserialize(jsonData) as Dictionary<string, object>;
                if (parsedData != null)
                {
                    data = parsedData;
                }
            }
            catch
            {
                // Fallback: store as single JSON field if parsing fails
                data = new Dictionary<string, object>
                {
                    { "JSON", jsonData }
                };
            }
            
            // Add timestamp
            data["Timestamp"] = FieldValue.ServerTimestamp;

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
    /// Reconstructs JSON from individual fields
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
            Debug.Log($"[FirebaseManager] Attempting to load document from {collection}/{documentId}");
            
            var document = firestore.Collection(collection).Document(documentId);
            var snapshot = await document.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                var data = snapshot.ToDictionary();
                
                // Remove timestamp field as it's not part of the original JSON
                data.Remove("Timestamp");
                
                // Convert back to JSON string
                string json = MiniJSON.Json.Serialize(data);
                
                Debug.Log($"[FirebaseManager] Document loaded from {collection}/{documentId} - Size: {json.Length} chars");
                return json;
            }
            else
            {
                Debug.Log($"[FirebaseManager] No document found at {collection}/{documentId}");
                return null;
            }
        }
        catch (Firebase.FirebaseException firebaseEx)
        {
            Debug.LogError($"[FirebaseManager] Firebase error loading document: {firebaseEx.Message}\nError Code: {firebaseEx.ErrorCode}\n{firebaseEx.StackTrace}");
            return null;
        }
        catch (System.Threading.Tasks.TaskCanceledException)
        {
            Debug.LogWarning($"[FirebaseManager] Load document task was canceled for {collection}/{documentId}");
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseManager] Unexpected error loading document: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
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
    /// Update specific fields in a Firestore document
    /// </summary>
    public async Task<bool> UpdateDocumentFields(string collection, string documentId, Dictionary<string, object> updates)
    {
        if (!isAuthenticated || !isInitialized)
        {
            Debug.LogError("[FirebaseManager] Cannot update document - not authenticated!");
            return false;
        }

        try
        {
            var document = firestore.Collection(collection).Document(documentId);
            await document.UpdateAsync(updates);
            Debug.Log($"[FirebaseManager] Document fields updated in {collection}/{documentId}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseManager] Error updating document fields: {ex.Message}");
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
            // Query the levels collection - no ordering to avoid index requirements
            var levelsQuery = firestore.Collection("levels")
                .Limit(maxResults);

            var snapshot = await levelsQuery.GetSnapshotAsync();
            var levels = new List<(string, string)>();

            foreach (var document in snapshot.Documents)
            {
                var data = document.ToDictionary();
                
                // Remove timestamp field as it's not part of the original JSON
                data.Remove("Timestamp");
                
                // Convert back to JSON string using MiniJSON
                string saveJson = MiniJSON.Json.Serialize(data);
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
    /// Note: Filters client-side since PlayerId is now a direct field
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
            // Get more results than needed to account for filtering
            var levelsQuery = firestore.Collection("levels")
                .Limit(maxResults * 5); // Get more to ensure we have enough after filtering

            var snapshot = await levelsQuery.GetSnapshotAsync();
            var myLevels = new List<(string, string)>();

            foreach (var document in snapshot.Documents)
            {
                var data = document.ToDictionary();
                
                // Check if PlayerId field matches current user
                if (data.ContainsKey("PlayerId") && data["PlayerId"].ToString() == userId)
                {
                    data.Remove("timestamp"); // Remove timestamp
                    
                    // Convert back to JSON string
                    string saveJson = MiniJSON.Json.Serialize(data);
                    myLevels.Add((document.Id, saveJson));
                    
                    if (myLevels.Count >= maxResults)
                        break;
                }
            }

            Debug.Log($"[FirebaseManager] Retrieved {myLevels.Count} of my levels from Firestore");
            return myLevels;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseManager] Error getting my levels: {ex.Message}");
            return new List<(string, string)>();
        }
    }
    
    #endregion
}
