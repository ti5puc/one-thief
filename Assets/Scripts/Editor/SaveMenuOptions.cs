using UnityEngine;
using UnityEditor;

public class SaveMenuOptions
{
	[MenuItem("One Thief/Clear all saves")]
	public static void ClearAllSaves()
	{
		SaveSystem.ClearAllSaves();
	}
}
