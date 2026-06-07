using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Firebase.Firestore;
using NaughtyAttributes;
using UnityEngine;

[System.Serializable]
public class RecurrenceData
{
    public List<string> LevelIds = new List<string>();
    public List<int> WinCounts = new List<int>();
    // Unix timestamp (seconds UTC) of the first win in the current 24-hour window per level
    public List<double> WindowStartTimes = new List<double>();
}

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
    public float TotalWins;
    public int EntryTax;
}

public class SaveSystem : MonoBehaviour
{
    private const string SAVE_FOLDER = "Saves";
    private const string INVENTORY_FILE = "inventory";
    private const string RECURRENCE_FILE = "level_recurrence";
    private const string FILE_EXTENSION = ".json";
    private const string USER_CREDENTIALS_FILE = "user_credentials";
    private const string CREDENTIALS_EXTENSION = ".txt";
    private const string DEFAULT_PASSWORD = "OnePasswordToRuleThemAll@12345";
    
    [Header("Difficulty — Ratio Thresholds")]
    [SerializeField] private float easyRatioThreshold     = 1.0f;
    [SerializeField] private float normalRatioThreshold   = 2.0f;
    [SerializeField] private float hardRatioThreshold     = 5.0f;
    [SerializeField] private float veryHardRatioThreshold = 9.0f;

    [Header("Difficulty — Gold Multipliers")]
    [SerializeField] private float veryEasyMultiplier = 1.0f;
    [SerializeField] private float easyMultiplier     = 1.2f;
    [SerializeField] private float normalMultiplier   = 1.5f;
    [SerializeField] private float hardMultiplier     = 2.0f;
    [SerializeField] private float veryHardMultiplier = 2.5f;

    [Header("Entry Tax")]
    [SerializeField] private float taxRate       = 0.11f;
    [SerializeField] private int   minGoldForTax = 500;

    [Header("Recurrence — Tax Multipliers")]
    [SerializeField] private float recurrence1Win  = 1.0f;
    [SerializeField] private float recurrence2Wins = 1.5f;
    [SerializeField] private float recurrence3Wins = 2.0f;
    [SerializeField] private float recurrence4Wins = 3.5f;
    [SerializeField] private float recurrence5Wins = 5.0f;
    [SerializeField] private float recurrence6Wins = 8.0f;

    [Header("Recurrence — Window")]
    [SerializeField] private double recurrenceWindowHours = 24.0;

    [Header("Debug")]
    [SerializeField, ReadOnly] private string nextSaveToLoad;

    private static int nextLevelTotalGold;
    private static int nextLevelEntryTax;
    private static string nextLevelCreatorId;
    private static string nextLevelId;

    public static SaveSystem Instance { get; private set; }
    public static string NextSaveToLoad
    {
        get => Instance.nextSaveToLoad;
        set => Instance.nextSaveToLoad = value;
    }
    public static int NextLevelTotalGold
    {
        get => nextLevelTotalGold;
        set => nextLevelTotalGold = value;
    }
    public static int NextLevelEntryTax
    {
        get => nextLevelEntryTax;
        set => nextLevelEntryTax = value;
    }
    public static string NextLevelCreatorId
    {
        get => nextLevelCreatorId;
        set => nextLevelCreatorId = value;
    }
    public static string NextLevelId
    {
        get => nextLevelId;
        set => nextLevelId = value;
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

            // Store level metadata
            nextLevelTotalGold = GetEffectiveGold(data.TotalGold, data.TotalDeaths, data.TotalWins);
            int baseTax = GetEffectiveTax(nextLevelTotalGold);
            int playerWins = LocalSaveHasLevelId(saveId) ? GetPlayerWinsOnLevel(nextLevelId) : 0;
            nextLevelEntryTax = Mathf.RoundToInt(baseTax * GetRecurrenceMultiplier(playerWins));

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
    public static async void SubmitLevelToFirebase(string levelName, int totalGold, int entryTax, int layoutIndex)
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
                TotalDeaths = 0,
                TotalWins = 0f,
                EntryTax = entryTax
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
    public static async void EditLevelOnFirebase(string levelId, string levelName, int totalGold, int entryTax, int layoutIndex)
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
                TotalDeaths = 0,
                TotalWins = 0f,
                EntryTax = entryTax
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
                { "TotalDeaths", FieldValue.Increment(1L) }
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
    /// Add a win value (0f-1f) to the current loaded level's TotalWins on Firebase
    /// winValue = 1f means all gold collected, 0.5f means half, etc.
    /// </summary>
    public static async void AddWinToLevel(float winValue)
    {
        string saveId = NextSaveToLoad;

        if (!LocalSaveHasLevelId(saveId))
        {
            Debug.Log("[SaveSystem] Current level is not from Firebase, skipping win tracking");
            return;
        }

        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsAuthenticated)
        {
            Debug.LogError("[SaveSystem] Cannot update wins - not authenticated");
            return;
        }

        try
        {
            string levelId = GetLocalSaveLevelId(saveId);

            if (string.IsNullOrEmpty(levelId))
            {
                Debug.LogError("[SaveSystem] Failed to get level ID from save");
                return;
            }

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

            levelData.TotalWins += Mathf.Clamp01(winValue);

            Debug.Log($"[SaveSystem] Adding {winValue} win to level '{levelId}', total: {levelData.TotalWins}");

            var updates = new Dictionary<string, object>
            {
                { "TotalWins", FieldValue.Increment((double)Mathf.Clamp01(winValue)) }
            };

            bool success = await FirebaseManager.Instance.UpdateDocumentFields("levels", levelId, updates);

            if (success)
            {
                string updatedJson = JsonUtility.ToJson(levelData, true);
                File.WriteAllText(filePath, updatedJson);
                Debug.Log($"[SaveSystem] Successfully updated wins for level '{levelId}'");
            }
            else
            {
                Debug.LogError("[SaveSystem] Failed to update wins on Firebase");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SaveSystem] Error adding win to level: {ex.Message}");
        }
    }

    /// <summary>
    /// Returns a localized difficulty label based on the deaths/wins ratio.
    /// </summary>
    public static string GetDifficultyLabel(int totalDeaths, float totalWins)
    {
        if (totalWins <= 0f || totalDeaths <= 0)
            return "Normal";

        float ratio = totalDeaths / totalWins;

        float e  = Instance != null ? Instance.easyRatioThreshold     : 1.0f;
        float n  = Instance != null ? Instance.normalRatioThreshold   : 2.0f;
        float h  = Instance != null ? Instance.hardRatioThreshold     : 5.0f;
        float vh = Instance != null ? Instance.veryHardRatioThreshold : 9.0f;

        if (ratio < e)  return "Muito Fácil";
        if (ratio < n)  return "Fácil";
        if (ratio < h)  return "Normal";
        if (ratio < vh) return "Difícil";
        return "Muito Difícil";
    }

    /// <summary>
    /// Returns the gold multiplier for a difficulty based on deaths/wins ratio.
    /// New levels (no data) start at 1.0x (Muito Fácil).
    /// </summary>
    public static float GetDifficultyMultiplier(int totalDeaths, float totalWins)
    {
        if (totalWins <= 0f || totalDeaths <= 0)
            return Instance != null ? Instance.normalMultiplier : 1.5f;

        float ratio = totalDeaths / totalWins;

        float e  = Instance != null ? Instance.easyRatioThreshold     : 1.0f;
        float n  = Instance != null ? Instance.normalRatioThreshold   : 2.0f;
        float h  = Instance != null ? Instance.hardRatioThreshold     : 5.0f;
        float vh = Instance != null ? Instance.veryHardRatioThreshold : 9.0f;

        if (ratio < e)  return Instance != null ? Instance.veryEasyMultiplier : 1.0f;
        if (ratio < n)  return Instance != null ? Instance.easyMultiplier     : 1.2f;
        if (ratio < h)  return Instance != null ? Instance.normalMultiplier   : 1.5f;
        if (ratio < vh) return Instance != null ? Instance.hardMultiplier     : 2.0f;
        return Instance != null ? Instance.veryHardMultiplier : 2.5f;
    }

    /// <summary>
    /// Returns the effective gold a player can loot, scaled by difficulty.
    /// baseGold is the creator's submitted value (Muito Fácil reference, 1.0x).
    /// </summary>
    public static int GetEffectiveGold(int baseGold, int totalDeaths, float totalWins)
    {
        return Mathf.RoundToInt(baseGold * GetDifficultyMultiplier(totalDeaths, totalWins));
    }

    /// <summary>
    /// Returns the base entry tax for a given effective gold amount (11% if > 500, else 0).
    /// </summary>
    public static int GetEffectiveTax(int effectiveGold)
    {
        int   minGold = Instance != null ? Instance.minGoldForTax : 500;
        float rate    = Instance != null ? Instance.taxRate        : 0.11f;
        return effectiveGold > minGold ? Mathf.RoundToInt(effectiveGold * rate) : 0;
    }

    /// <summary>
    /// Returns the recurrence multiplier applied to entry tax based on how many
    /// times the player has already won this level.
    /// </summary>
    public static float GetRecurrenceMultiplier(int wins)
    {
        if (wins <= 1) return Instance != null ? Instance.recurrence1Win  : 1.0f;
        if (wins == 2) return Instance != null ? Instance.recurrence2Wins : 1.5f;
        if (wins == 3) return Instance != null ? Instance.recurrence3Wins : 2.0f;
        if (wins == 4) return Instance != null ? Instance.recurrence4Wins : 3.5f;
        if (wins == 5) return Instance != null ? Instance.recurrence5Wins : 5.0f;
        return Instance != null ? Instance.recurrence6Wins : 8.0f;
    }

    private static double GetUnixTimeSeconds() =>
        (System.DateTime.UtcNow - new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds;

    private static double RecurrenceWindowSeconds =>
        Instance != null ? Instance.recurrenceWindowHours * 3600.0 : 86400.0;

    /// <summary>
    /// Returns how many times the local player has won a specific Firebase level
    /// within the current 24-hour window. Auto-resets when the window expires.
    /// </summary>
    public static int GetPlayerWinsOnLevel(string levelId)
    {
        if (string.IsNullOrEmpty(levelId)) return 0;
        RecurrenceData data = LoadRecurrenceData();
        if (data == null) return 0;
        int idx = data.LevelIds.IndexOf(levelId);
        if (idx < 0) return 0;

        // Check if the 24-hour window has expired
        double windowStart = idx < data.WindowStartTimes.Count ? data.WindowStartTimes[idx] : 0;
        if (windowStart > 0 && (GetUnixTimeSeconds() - windowStart) >= RecurrenceWindowSeconds)
        {
            data.WinCounts[idx] = 0;
            data.WindowStartTimes[idx] = 0;
            SaveRecurrenceData(data);
            return 0;
        }

        return data.WinCounts[idx];
    }

    /// <summary>
    /// Increments the local player's win count for a specific Firebase level.
    /// Starts or resets the 24-hour recurrence window as needed.
    /// Call this whenever the player exits a level with gold collected.
    /// </summary>
    public static void IncrementPlayerWinsOnLevel(string levelId)
    {
        if (string.IsNullOrEmpty(levelId)) return;
        RecurrenceData data = LoadRecurrenceData() ?? new RecurrenceData();
        int idx = data.LevelIds.IndexOf(levelId);
        double now = GetUnixTimeSeconds();

        if (idx >= 0)
        {
            // Ensure WindowStartTimes is in sync
            while (data.WindowStartTimes.Count <= idx)
                data.WindowStartTimes.Add(0);

            double windowStart = data.WindowStartTimes[idx];
            bool windowExpired = windowStart <= 0 || (now - windowStart) >= RecurrenceWindowSeconds;

            if (windowExpired)
            {
                data.WinCounts[idx] = 1;
                data.WindowStartTimes[idx] = now;
            }
            else
            {
                data.WinCounts[idx]++;
            }
        }
        else
        {
            data.LevelIds.Add(levelId);
            data.WinCounts.Add(1);
            data.WindowStartTimes.Add(now);
        }

        SaveRecurrenceData(data);
    }

    private static RecurrenceData LoadRecurrenceData()
    {
        string path = Path.Combine(Application.persistentDataPath, SAVE_FOLDER, RECURRENCE_FILE + FILE_EXTENSION);
        if (!File.Exists(path)) return null;
        try
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<RecurrenceData>(json);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[SaveSystem] Error loading recurrence data: {ex.Message}");
            return null;
        }
    }

    private static void SaveRecurrenceData(RecurrenceData data)
    {
        try
        {
            string folderPath = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
            string path = Path.Combine(folderPath, RECURRENCE_FILE + FILE_EXTENSION);
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SaveSystem] Error saving recurrence data: {ex.Message}");
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

            nextLevelCreatorId = levelData.PlayerId;
            nextLevelId = levelId;
            
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
    /// Add tax gold to the level creator's Firebase inventory (TaxGoldToGain field).
    /// </summary>
    public static async void AddTaxGoldToCreator(string creatorPlayerId, int taxAmount)
    {
        if (taxAmount <= 0 || string.IsNullOrEmpty(creatorPlayerId)) return;
        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsAuthenticated) return;

        try
        {
            var updates = new Dictionary<string, object>
            {
                { "TaxGoldToGain", FieldValue.Increment((long)taxAmount) }
            };

            bool success = await FirebaseManager.Instance.UpdateDocumentFields("players", creatorPlayerId, updates);

            if (success)
                Debug.Log($"[SaveSystem] Added {taxAmount} tax gold to creator '{creatorPlayerId}'");
            else
                Debug.LogError($"[SaveSystem] Failed to add tax gold to creator '{creatorPlayerId}'");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SaveSystem] Error adding tax gold to creator: {ex.Message}");
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