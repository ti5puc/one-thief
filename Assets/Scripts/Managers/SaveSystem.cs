using System.IO;
using NaughtyAttributes;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public int Rows;
    public int Cols;
    public int[] Data; // matriz achatada
    public int[] Rotations; // rotações (quarter turns) para cada célula
}

public class SaveSystem : MonoBehaviour
{
    private const string SAVE_FOLDER = "Saves";
    private const string FILE_EXTENSION = ".json";

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
            SaveData data = new SaveData
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
                    Debug.LogWarning($"[TrapGridSaveSystem] Arquivo de save '{saveId}' não encontrado em: {filePath} nem em Resources/{resourcePath}");
                    return false;
                }
            }

            // Deserialize
            SaveData data = JsonUtility.FromJson<SaveData>(json);

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

            Debug.Log($"[TrapGridSaveSystem] Grid '{saveId}' carregado com sucesso de: {(loadedFromResources ? "Resources" : filePath)}");
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

    public static void ClearAllSaves()
    {
        string saveFolderPath = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
            
        if (!Directory.Exists(saveFolderPath))
            return;
        
        // Find all .json files in the Saves directory
        string[] jsonFiles = System.IO.Directory.GetFiles(saveFolderPath, "*.json", System.IO.SearchOption.TopDirectoryOnly);
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

        Debug.Log($"Deleted {deletedCount} save file(s) in:\n{saveFolderPath}");
    }

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
}
