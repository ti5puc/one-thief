using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;

[System.Serializable]
public class LevelSaveData
{
    public int Rows;
    public int Cols;
    public int[] Data; // matriz achatada
    public int[] Rotations; // rotações (quarter turns) para cada célula
    
    public string LevelName;
    public string PlayerId;
    public int LayoutIndex;
    public int TotalGold;
    public int TotalDeaths;
}

public class SaveSystem : MonoBehaviour
{
    private const string SAVE_FOLDER = "Saves";
    private const string INVENTORY_FILE = "inventory";
    private const string FILE_EXTENSION = ".json";
    private const string USER_CREDENTIALS_FILE = "user_credentials";
    private const string CREDENTIALS_EXTENSION = ".txt";
    private const string DEFAULT_PASSWORD = "OnePasswordToRuleThemAll@12345";
    
    [Header("Debug")]
    [SerializeField, ReadOnly] private string nextSaveToLoad;

    public static SaveSystem Instance { get; private set; }
    public static string NextSaveToLoad
    {
        get => Instance.nextSaveToLoad;
        set => Instance.nextSaveToLoad = value;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static bool Save(string saveId, int[,] trapIdGrid, int[,] rotationGrid, int rows, int cols)
    {
        if (trapIdGrid == null)
        {
            Debug.LogError("[TrapGridSaveSystem] trapIdGrid é null, nada para salvar.");
            return false;
        }

        if (rotationGrid == null)
        {
            Debug.LogError("[TrapGridSaveSystem] rotationGrid é null, nada para salvar.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(saveId))
        {
            Debug.LogError("[TrapGridSaveSystem] saveId é inválido.");
            return false;
        }

        try
        {
            // Ensure save folder exists
            string saveFolderPath = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
            if (!Directory.Exists(saveFolderPath))
            {
                Directory.CreateDirectory(saveFolderPath);
            }

            // Debug: count non-zero cells
            int nonZero = 0;
            for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                if (trapIdGrid[r, c] != 0)
                    nonZero++;

            Debug.Log($"[TrapGridSaveSystem] Salvando '{saveId}' com {nonZero} células != 0");

            // Create save data
            LevelSaveData data = new LevelSaveData
            {
                Rows = rows,
                Cols = cols,
                Data = FlattenGrid(trapIdGrid, rows, cols),
                Rotations = FlattenGrid(rotationGrid, rows, cols)
            };

            // Serialize to JSON
            string json = JsonUtility.ToJson(data, true);

            // Save to file
            string filePath = Path.Combine(saveFolderPath, saveId + FILE_EXTENSION);
            File.WriteAllText(filePath, json);

            Debug.Log($"[TrapGridSaveSystem] Grid salvo com sucesso em: {filePath}");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[TrapGridSaveSystem] Erro ao salvar: {ex.Message}");
            return false;
        }
    }

    public static bool Load(string saveId, out int[,] trapIdGrid, out int[,] rotationGrid, out int rows, out int cols)
    {
        trapIdGrid = null;
        rotationGrid = null;
        rows = 0;
        cols = 0;

        if (string.IsNullOrWhiteSpace(saveId))
        {
            Debug.LogError("[TrapGridSaveSystem] saveId é inválido.");
            return false;
        }

        try
        {
            string saveFolderPath = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
            string filePath = Path.Combine(saveFolderPath, saveId + FILE_EXTENSION);
            string json = null;
            bool loadedFromResources = false;

            if (File.Exists(filePath))
            {
                // Read from file
                json = File.ReadAllText(filePath);
            }
            else
            {
                // Try loading from Resources/Saves Placeholder
                string resourcePath = $"Saves Placeholder/{saveId}";
                TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
                if (textAsset != null)
                {
                    json = textAsset.text;
                    loadedFromResources = true;
                    Debug.Log($"[TrapGridSaveSystem] Save '{saveId}' carregado de Resources: {resourcePath}");
                }
                else
                {
                    Debug.LogWarning(
                        $"[TrapGridSaveSystem] Arquivo de save '{saveId}' não encontrado em: {filePath} nem em Resources/{resourcePath}");
                    return false;
                }
            }

            // Deserialize
            LevelSaveData data = JsonUtility.FromJson<LevelSaveData>(json);

            if (data == null)
            {
                Debug.LogError($"[TrapGridSaveSystem] Falha ao deserializar save '{saveId}'");
                return false;
            }

            // Unflatten grid
            rows = data.Rows;
            cols = data.Cols;
            trapIdGrid = UnflattenGrid(data.Data, rows, cols);

            // Unflatten rotations (handle backward compatibility)
            if (data.Rotations != null && data.Rotations.Length > 0)
            {
                rotationGrid = UnflattenGrid(data.Rotations, rows, cols);
            }
            else
            {
                // For old saves without rotation data, initialize to zero
                rotationGrid = new int[rows, cols];
                Debug.LogWarning("[TrapGridSaveSystem] Save file has no rotation data, initializing to zero.");
            }

            Debug.Log(
                $"[TrapGridSaveSystem] Grid '{saveId}' carregado com sucesso de: {(loadedFromResources ? "Resources" : filePath)}");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[TrapGridSaveSystem] Erro ao carregar: {ex.Message}");
            return false;
        }
    }

    public static bool SaveExists(string saveId)
    {
        if (string.IsNullOrWhiteSpace(saveId))
            return false;

        string saveFolderPath = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
        string filePath = Path.Combine(saveFolderPath, saveId + FILE_EXTENSION);
        return File.Exists(filePath);
    }

    public static bool DeleteSave(string saveId)
    {
        if (string.IsNullOrWhiteSpace(saveId))
            return false;

        try
        {
            string saveFolderPath = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
            string filePath = Path.Combine(saveFolderPath, saveId + FILE_EXTENSION);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Debug.Log($"[TrapGridSaveSystem] Save '{saveId}' deletado com sucesso.");
                return true;
            }

            return false;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[TrapGridSaveSystem] Erro ao deletar save: {ex.Message}");
            return false;
        }
    }

    public static string[] GetAllSaveIds()
    {
        try
        {
            string saveFolderPath = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);

            if (!Directory.Exists(saveFolderPath))
                return new string[0];

            string[] files = Directory.GetFiles(saveFolderPath, "*" + FILE_EXTENSION);
            string[] saveIds = new string[files.Length];

            for (int i = 0; i < files.Length; i++)
            {
                saveIds[i] = Path.GetFileNameWithoutExtension(files[i]);
            }

            return saveIds;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[TrapGridSaveSystem] Erro ao listar saves: {ex.Message}");
            return new string[0];
        }
    }

    public static void ClearAllSaves(bool alsoClearInventory, bool alsoClearCredentials)
    {
        string saveFolderPath = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);

        if (!Directory.Exists(saveFolderPath))
            return;

        // Find all .json files in the Saves directory
        string[] jsonFiles =
            System.IO.Directory.GetFiles(saveFolderPath, "*.json", System.IO.SearchOption.TopDirectoryOnly);
        
        if (alsoClearInventory == false)
        {
            jsonFiles = System.Array.FindAll(jsonFiles, file => !file.EndsWith(INVENTORY_FILE + FILE_EXTENSION));
        }
        
        int deletedCount = 0;
        foreach (string file in jsonFiles)
        {
            try
            {
                System.IO.File.Delete(file);
                deletedCount++;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to delete {file}: {ex.Message}");
            }
        }
        
        // Delete user credentials file
        if (alsoClearCredentials)
        {
            string credentialsPath = Path.Combine(saveFolderPath, USER_CREDENTIALS_FILE + CREDENTIALS_EXTENSION);
            if (File.Exists(credentialsPath))
            {
                try
                {
                    File.Delete(credentialsPath);
                    Debug.Log("Deleted user credentials file");
                    
                    // Sign out from Firebase and re-authenticate with new credentials
                    FirebaseManager.SignOut();
                    
                    // Wait a frame then re-authenticate with new account
                    if (Instance != null)
                    {
                        Instance.StartCoroutine(ReAuthenticateAfterDelay());
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to delete credentials file: {ex.Message}");
                }
            }
        }

        Debug.Log($"Deleted {deletedCount} save file(s) in:\n{saveFolderPath}");
    }

    private static System.Collections.IEnumerator ReAuthenticateAfterDelay()
    {
        // Wait a short moment for sign out to complete
        yield return new UnityEngine.WaitForSeconds(0.5f);
        
        Debug.Log("[SaveSystem] Triggering re-authentication with new account...");
        FirebaseManager.ReAuthenticate();
    }

    #region User Credentials Management
    
    /// <summary>
    /// Get the stored user email or create a new one if it doesn't exist
    /// </summary>
    public static string GetOrCreateUserEmail()
    {
        string saveFolderPath = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
        string credentialsPath = Path.Combine(saveFolderPath, USER_CREDENTIALS_FILE + CREDENTIALS_EXTENSION);
        
        // Create folder if it doesn't exist
        if (!Directory.Exists(saveFolderPath))
        {
            Directory.CreateDirectory(saveFolderPath);
        }
        
        // Check if credentials file exists
        if (File.Exists(credentialsPath))
        {
            try
            {
                string storedEmail = File.ReadAllText(credentialsPath).Trim();
                
                if (!string.IsNullOrEmpty(storedEmail))
                {
                    Debug.Log("[SaveSystem] Loaded existing user email from local storage");
                    return storedEmail;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SaveSystem] Error reading credentials file: {ex.Message}");
            }
        }
        
        // Generate new email
        string newEmail = GenerateRandomEmail();
        
        try
        {
            // Save to file
            File.WriteAllText(credentialsPath, newEmail);
            Debug.Log($"[SaveSystem] Generated and saved new user email: {newEmail}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SaveSystem] Error saving credentials file: {ex.Message}");
        }
        
        return newEmail;
    }

    /// <summary>
    /// Generate a random email with 15 character username
    /// </summary>
    private static string GenerateRandomEmail()
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        System.Text.StringBuilder sb = new System.Text.StringBuilder(15);
        
        // Use a combination of Guid timestamp and UnityEngine.Random for better entropy
        System.Random random = new System.Random(System.Guid.NewGuid().GetHashCode());
        
        for (int i = 0; i < 15; i++)
        {
            int index = random.Next(chars.Length);
            sb.Append(chars[index]);
        }
        
        return sb.ToString() + "@onethief.local";
    }

    /// <summary>
    /// Get the default password for authentication
    /// </summary>
    public static string GetDefaultPassword()
    {
        return DEFAULT_PASSWORD;
    }
    
    #endregion

    private static int[] FlattenGrid(int[,] grid, int rows, int cols)
    {
        int[] flat = new int[rows * cols];
        int idx = 0;
        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
            flat[idx++] = grid[r, c];
        return flat;
    }

    private static int[,] UnflattenGrid(int[] flat, int rows, int cols)
    {
        int[,] grid = new int[rows, cols];
        int idx = 0;
        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
            grid[r, c] = flat[idx++];
        return grid;
    }
    
    public static void SaveInventory(InventoryData data)
    {
        string saveFolderPath = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
        
        if (!Directory.Exists(saveFolderPath))
        {
            Directory.CreateDirectory(saveFolderPath);
        }
        
        try
        {
            var inventoryFilePath = Path.Combine(saveFolderPath, INVENTORY_FILE + FILE_EXTENSION);
            
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(inventoryFilePath, json);
            
            // Also save to Firebase if available
            if (FirebaseManager.Instance != null && FirebaseManager.Instance.IsAuthenticated)
            {
                SaveInventoryToFirebase(data);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[InventorySaveSystem] Failed to save inventory: {ex.Message}");
        }
    }

    public static InventoryData LoadInventory()
    {
        string saveFolderPath = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
        var inventoryFilePath = Path.Combine(saveFolderPath, INVENTORY_FILE + FILE_EXTENSION);
        
        try
        {
            if (File.Exists(inventoryFilePath))
            {
                string json = File.ReadAllText(inventoryFilePath);
                InventoryData data = JsonUtility.FromJson<InventoryData>(json);
                return data;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[InventorySaveSystem] Failed to load inventory: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Save inventory to Firebase asynchronously
    /// </summary>
    private static async void SaveInventoryToFirebase(InventoryData data)
    {
        string userId = FirebaseManager.Instance.UserId;
        string collection = $"players";
        string documentId = userId;
        string json = JsonUtility.ToJson(data);
        
        await FirebaseManager.Instance.SaveDocument(collection, documentId, json);
    }

    /// <summary>
    /// Load inventory from Firebase - Call this after authentication
    /// Returns the loaded inventory data
    /// </summary>
    public static async Task<InventoryData> LoadInventoryFromFirebase()
    {
        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsAuthenticated)
        {
            Debug.LogWarning("[SaveSystem] Cannot load from Firebase - not authenticated");
            return null;
        }

        string userId = FirebaseManager.Instance.UserId;
        string collection = "players";
        string documentId = userId;
        
        string json = await FirebaseManager.Instance.LoadDocument(collection, documentId);
        
        if (!string.IsNullOrEmpty(json))
        {
            InventoryData data = JsonUtility.FromJson<InventoryData>(json);
            
            // Save locally
            string saveFolderPath = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
            if (!Directory.Exists(saveFolderPath))
            {
                Directory.CreateDirectory(saveFolderPath);
            }
            
            var inventoryFilePath = Path.Combine(saveFolderPath, INVENTORY_FILE + FILE_EXTENSION);
            File.WriteAllText(inventoryFilePath, json);
            
            Debug.Log($"[SaveSystem] Inventory loaded from Firebase and saved locally. Gold: {data.Gold}");
            return data;
        }
        
        Debug.Log("[SaveSystem] No inventory found in Firebase");
        return null;
    }

    /// <summary>
    /// Submit the current level to Firebase with a custom name
    /// Each submission creates a new level with a unique ID
    /// </summary>
    public static async void SubmitLevelToFirebase(string levelName, int totalGold, int layoutIndex)
    {
        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsAuthenticated)
        {
            Debug.LogError("[SaveSystem] Cannot submit level - not authenticated");
            return;
        }

        // Load the current build save
        string saveId = "current_build";
        
        if (Load(saveId, out int[,] trapIdGrid, out int[,] rotationGrid, out int rows, out int cols))
        {
            // Create LevelSaveData from the loaded grid with level name and player ID
            LevelSaveData levelData = new LevelSaveData
            {
                Rows = rows,
                Cols = cols,
                Data = FlattenGrid(trapIdGrid, rows, cols),
                Rotations = FlattenGrid(rotationGrid, rows, cols),
                
                LevelName = levelName,
                PlayerId = FirebaseManager.Instance.UserId,
                LayoutIndex = layoutIndex,
                TotalGold = totalGold,
                TotalDeaths = 0
            };
            
            string json = JsonUtility.ToJson(levelData);
            
            // Generate unique document ID using userId and timestamp
            string documentId = $"{FirebaseManager.Instance.UserId}_{System.DateTime.UtcNow.Ticks}";
            
            bool success = await FirebaseManager.Instance.SaveDocument("levels", documentId, json);
            
            if (success)
            {
                Debug.Log($"[SaveSystem] Level '{levelName}' submitted successfully!");
            }
        }
        else
        {
            Debug.LogError("[SaveSystem] Failed to load current build for submission");
        }
    }
    
    /// <summary>
    /// Edit an existing level on Firebase by updating the document
    /// Similar to SubmitLevelToFirebase, but updates an existing document instead of creating a new one
    /// </summary>
    public static async void EditLevelOnFirebase(string levelId, string levelName, int totalGold, int layoutIndex)
    {
        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsAuthenticated)
        {
            Debug.LogError("[SaveSystem] Cannot submit level - not authenticated");
            return;
        }

        // Load the current build save
        string saveId = "firebase_" + levelId;
        
        if (Load(saveId, out int[,] trapIdGrid, out int[,] rotationGrid, out int rows, out int cols))
        {
            // Create LevelSaveData from the loaded grid with level name and player ID
            LevelSaveData levelData = new LevelSaveData
            {
                Rows = rows,
                Cols = cols,
                Data = FlattenGrid(trapIdGrid, rows, cols),
                Rotations = FlattenGrid(rotationGrid, rows, cols),
                
                LevelName = levelName,
                PlayerId = FirebaseManager.Instance.UserId,
                LayoutIndex = layoutIndex,
                TotalGold = totalGold,
                TotalDeaths = 0
            };
            
            string json = JsonUtility.ToJson(levelData);
            bool success = await FirebaseManager.Instance.SaveDocument("levels", levelId, json);
            
            if (success)
            {
                Debug.Log($"[SaveSystem] Level '{levelName}' submitted successfully!");
            }
        }
        else
        {
            Debug.LogError("[SaveSystem] Failed to load current build for submission");
        }
    }

    /// <summary>
    /// Increase player deaths on the current loaded level by 1 and save to Firebase
    /// Only works if the current level is a Firebase level (starts with "firebase_" prefix)
    /// </summary>
    public static async void IncreasePlayerDeathsOnLevel()
    {
        string saveId = NextSaveToLoad;
        
        // Check if the current level is from Firebase
        if (!LocalSaveHasLevelId(saveId))
        {
            Debug.Log("[SaveSystem] Current level is not from Firebase, skipping death tracking");
            return;
        }

        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsAuthenticated)
        {
            Debug.LogError("[SaveSystem] Cannot update deaths - not authenticated");
            return;
        }

        try
        {
            // Get the Firebase level ID
            string levelId = GetLocalSaveLevelId(saveId);
            
            if (string.IsNullOrEmpty(levelId))
            {
                Debug.LogError("[SaveSystem] Failed to get level ID from save");
                return;
            }

            // Load the current local save data to get the current death count
            string saveFolderPath = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
            string filePath = Path.Combine(saveFolderPath, saveId + FILE_EXTENSION);
            
            if (!File.Exists(filePath))
            {
                Debug.LogError($"[SaveSystem] Local save file not found: {filePath}");
                return;
            }

            string json = File.ReadAllText(filePath);
            LevelSaveData levelData = JsonUtility.FromJson<LevelSaveData>(json);
            
            if (levelData == null)
            {
                Debug.LogError("[SaveSystem] Failed to deserialize level data");
                return;
            }

            // Increment deaths
            levelData.TotalDeaths++;
            
            Debug.Log($"[SaveSystem] Increasing deaths for level '{levelId}' to {levelData.TotalDeaths}");

            // Update Firebase with the new death count
            var updates = new Dictionary<string, object>
            {
                { "TotalDeaths", levelData.TotalDeaths }
            };
            
            bool success = await FirebaseManager.Instance.UpdateDocumentFields("levels", levelId, updates);
            
            if (success)
            {
                // Also update the local save file
                string updatedJson = JsonUtility.ToJson(levelData, true);
                File.WriteAllText(filePath, updatedJson);
                
                Debug.Log($"[SaveSystem] Successfully updated deaths for level '{levelId}'");
            }
            else
            {
                Debug.LogError("[SaveSystem] Failed to update deaths on Firebase");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SaveSystem] Error increasing player deaths: {ex.Message}");
        }
    }

    /// <summary>
    /// Load a specific level from Firebase by its levelId and save it locally
    /// Returns the localSaveId if successful, null otherwise
    /// </summary>
    public static async Task<string> LoadFirebaseLevel(string levelId)
    {
        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsAuthenticated)
        {
            Debug.LogError("[SaveSystem] Cannot load level from Firebase - not authenticated");
            return null;
        }

        try
        {
            // Load the level from Firebase
            string saveJson = await FirebaseManager.Instance.LoadDocument("levels", levelId);
            
            if (string.IsNullOrEmpty(saveJson))
            {
                Debug.LogError($"[SaveSystem] Level '{levelId}' not found in Firebase");
                return null;
            }

            // Parse the LevelSaveData
            LevelSaveData levelData = JsonUtility.FromJson<LevelSaveData>(saveJson);
            
            if (levelData == null)
            {
                Debug.LogError("[SaveSystem] Failed to deserialize level data");
                return null;
            }

            // Save it locally
            string saveFolderPath = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
            if (!Directory.Exists(saveFolderPath))
            {
                Directory.CreateDirectory(saveFolderPath);
            }

            var localSaveId = "firebase_" + levelId;
            string filePath = Path.Combine(saveFolderPath, localSaveId + FILE_EXTENSION);
            File.WriteAllText(filePath, saveJson);
            
            Debug.Log($"[SaveSystem] Firebase level '{levelId}' loaded and saved as '{localSaveId}'");
            return localSaveId;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SaveSystem] Error loading Firebase level: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Load a random level from Firebase and save it locally with a specific saveId
    /// Returns the saveId if successful, null otherwise
    /// </summary>
    public static async Task<string> LoadRandomFirebaseLevel()
    {
        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsAuthenticated)
        {
            Debug.LogError("[SaveSystem] Cannot load random level from Firebase - not authenticated");
            return null;
        }

        try
        {
            // Get a random level from Firebase
            var levels = await FirebaseManager.Instance.GetAllLevels(10);
            
            if (levels.Count == 0)
            {
                Debug.Log("[SaveSystem] No levels found in Firebase");
                return null;
            }

            // Pick a random level
            int randomIndex = Random.Range(0, levels.Count);
            var (levelId, _) = levels[randomIndex];
            
            // Use the generic method to load and save the level
            return await LoadFirebaseLevel(levelId);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SaveSystem] Error loading random Firebase level: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Delete all downloaded Firebase levels (cleanup)
    /// </summary>
    public static void DeleteDownloadedLevels()
    {
        try
        {
            string saveFolderPath = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
            
            if (!Directory.Exists(saveFolderPath))
                return;

            // Delete all files that start with "firebase_"
            string[] files = Directory.GetFiles(saveFolderPath, "firebase_*" + FILE_EXTENSION);
            
            int deletedCount = 0;
            foreach (string file in files)
            {
                File.Delete(file);
                deletedCount++;
            }

            if (deletedCount > 0)
            {
                Debug.Log($"[SaveSystem] Deleted {deletedCount} downloaded Firebase level(s)");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SaveSystem] Error deleting downloaded levels: {ex.Message}");
        }
    }

    public static LevelSaveData ParseLevelDataFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogError("[SaveSystem] Provided JSON is null or empty.");
            return null;
        }
        LevelSaveData data = null;
        try
        {
            data = JsonUtility.FromJson<LevelSaveData>(json);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SaveSystem] Failed to parse level data from JSON: {ex.Message}");
        }
        if (data == null)
        {
            Debug.LogError("[SaveSystem] Failed to deserialize LevelSaveData from JSON.");
        }
        return data;
    }

    public static async Task<string> GetPlayerName(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            return "Unknown";
        }

        // Use FirebaseManager's cached method
        if (FirebaseManager.Instance != null && FirebaseManager.Instance.IsAuthenticated)
        {
            return await FirebaseManager.Instance.GetPlayerName(playerId);
        }

        Debug.LogWarning("[SaveSystem] Cannot get player name - not authenticated");
        return playerId;
    }

    /// <summary>
    /// Check if a local save was loaded from Firebase (has a level ID)
    /// Returns true if the saveId starts with "firebase_" prefix
    /// </summary>
    public static bool LocalSaveHasLevelId(string saveId)
    {
        if (string.IsNullOrEmpty(saveId))
            return false;
        
        // Firebase levels are saved locally with the "firebase_" prefix
        return saveId.StartsWith("firebase_");
    }

    /// <summary>
    /// Get the Firebase level ID from a local save
    /// Extracts the levelId by removing the "firebase_" prefix
    /// </summary>
    public static string GetLocalSaveLevelId(string saveId)
    {
        if (string.IsNullOrEmpty(saveId))
            return null;
        
        // Remove the "firebase_" prefix to get the actual Firebase document ID
        if (saveId.StartsWith("firebase_"))
        {
            return saveId.Substring(9); // "firebase_" has 9 characters
        }
        
        // If it doesn't have the prefix, return null
        Debug.LogWarning($"[SaveSystem] SaveId '{saveId}' does not have 'firebase_' prefix");
        return null;
    }
}