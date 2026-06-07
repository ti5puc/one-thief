using UnityEngine;

[CreateAssetMenu(fileName = "RoomBrushSettings", menuName = "Brushes/Room Brush Settings")]
public class RoomBrushSettings : ScriptableObject
{
    public GameObject GroundPrefab;
    public GameObject WallPrefab;
    public GameObject CeilPrefab;
    public bool ExtraWallsDown = false;
    public bool DebugLogs = false;
}
