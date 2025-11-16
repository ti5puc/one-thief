using UnityEngine;
using UnityEditor;

public class SaveMenuOptions
{
	[MenuItem("One Thief/Saves/Clear current build save")]
	public static void ClearCurrentBuild()
	{
		SaveSystem.DeleteSave("current_build");
	}
	
	[MenuItem("One Thief/Saves/Clear inventory save")]
	public static void ClearInventory()
	{
		SaveSystem.DeleteSave("inventory");
	}
	
	[MenuItem("One Thief/Saves/Clear all saves")]
	public static void ClearAllSaves()
	{
		SaveSystem.ClearAllSaves();
	}
	
	[MenuItem("One Thief/Saves/Export current build save")]
	public static void ExportCurrentBuildSave()
	{
		ExportSaveWindow.ShowWindow();
	}
	
	[MenuItem("One Thief/Saves/Open saves folder")]
	public static void OpenSavesFolder()
	{
		string saveFolderPath = System.IO.Path.Combine(Application.persistentDataPath, "Saves");
		EditorUtility.RevealInFinder(saveFolderPath);
	}
}

public class ExportSaveWindow : EditorWindow
{
	private string saveName = "";

	public static void ShowWindow()
	{
		var window = GetWindow<ExportSaveWindow>(true, "Export Save", true);
		window.position = new Rect(Screen.width / 2, Screen.height / 2, 300, 100);
	}

	void OnGUI()
	{
		GUILayout.Label("Enter save name:", EditorStyles.boldLabel);
		saveName = EditorGUILayout.TextField("Save Name", saveName);

		GUILayout.Space(10);
		if (GUILayout.Button("Export"))
		{
			if (string.IsNullOrWhiteSpace(saveName))
			{
				EditorUtility.DisplayDialog("Error", "Please enter a valid save name.", "OK");
				return;
			}

			// Find the Player in the scene
			Player player = GameObject.FindAnyObjectByType<Player>();
			if (player == null)
			{
				EditorUtility.DisplayDialog("Error", "No Player found in the scene.", "OK");
				return;
			}

			int[,] trapIdGrid = player.GetTrapIdGrid();
			int[,] trapRotationGrid = player.GetTrapRotationGrid();
			int rows = player.gridRows;
			int cols = player.gridCols;

			bool success = SaveSystem.Save(saveName, trapIdGrid, trapRotationGrid, rows, cols);
			if (success)
			{
				string saveFolderPath = System.IO.Path.Combine(Application.persistentDataPath, "Saves");
				string filePath = System.IO.Path.Combine(saveFolderPath, saveName + ".json");
				EditorUtility.RevealInFinder(filePath);
				Close();
			}
			else
			{
				EditorUtility.DisplayDialog("Error", "Failed to export save.", "OK");
			}
		}
	}
}
