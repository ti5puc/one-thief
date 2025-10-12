using UnityEngine;
using UnityEditor;

public class TrapMenuOptions
{
	[MenuItem("One Thief/Trap Settings/Assign Unique IDs")]
	public static void AssignUniqueTrapIds()
	{
		string[] guids = AssetDatabase.FindAssets("t:PlaceableSettings");
		int id = 0;
		foreach (string guid in guids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			var placeableSettings = AssetDatabase.LoadAssetAtPath<Object>(path) as ScriptableObject;
			if (placeableSettings == null) continue;
			var so = new SerializedObject(placeableSettings);
			var idProp = so.FindProperty("id");
			if (idProp != null)
			{
				idProp.intValue = id++;
				so.ApplyModifiedProperties();
				EditorUtility.SetDirty(placeableSettings);
			}
		}
		AssetDatabase.SaveAssets();
		Debug.Log($"Assigned unique IDs to {id} PlaceableSettings assets.");
	}

	[MenuItem("One Thief/Trap Settings/Check Duplicate IDs")]
	public static void CheckDuplicateTrapIds()
	{
		string[] guids = AssetDatabase.FindAssets("t:PlaceableSettings");
		var idSet = new System.Collections.Generic.HashSet<int>();
		bool hasDuplicate = false;
		foreach (string guid in guids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			var placeableSettings = AssetDatabase.LoadAssetAtPath<Object>(path) as ScriptableObject;
			if (placeableSettings == null) continue;
			var so = new SerializedObject(placeableSettings);
			var idProp = so.FindProperty("id");
			if (idProp != null)
			{
				int value = idProp.intValue;
				if (!idSet.Add(value))
				{
					Debug.LogError($"Duplicate PlaceableSettings id found: {value} in asset {path}");
					hasDuplicate = true;
				}
			}
		}
		if (!hasDuplicate)
		{
			Debug.Log("No duplicate PlaceableSettings ids found.");
		}
	}
}
