using UnityEngine;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "RoomBrush", menuName = "Brushes/Room Brush")]
[CustomGridBrush(false, true, false, "Room Brush")]
public class RoomBrush : GridBrushBase
{
    public GameObject groundPrefab;
    public GameObject wallPrefab;
    public GameObject ceilPrefab;
    public Vector3Int roomSize = new Vector3Int(3, 3, 1); // 3x3 room
    public int wallHeight = 2;

    public override void Paint(GridLayout grid, GameObject brushTarget, Vector3Int position)
    {
        if (brushTarget == null || groundPrefab == null || wallPrefab == null || ceilPrefab == null)
            return;

        // Place ground
        for (int x = 0; x < roomSize.x; x++)
        {
            for (int y = 0; y < roomSize.y; y++)
            {
                Vector3Int groundPos = position + new Vector3Int(x, y, 0);
                if (!HasGround(grid, brushTarget, groundPos))
                    InstantiatePrefab(grid, brushTarget, groundPrefab, groundPos);
            }
        }

        // Place walls
        for (int x = 0; x < roomSize.x; x++)
        {
            for (int y = 0; y < roomSize.y; y++)
            {
                // Only on edges
                if (x == 0 || x == roomSize.x - 1 || y == 0 || y == roomSize.y - 1)
                {
                    Vector3Int wallBase = position + new Vector3Int(x, y, 0);
                    // Only place wall if not adjacent to another room
                    if (ShouldPlaceWall(grid, brushTarget, wallBase, x, y, position))
                    {
                        for (int h = 1; h <= wallHeight; h++)
                        {
                            Vector3Int wallPos = wallBase + new Vector3Int(0, 0, h);
                            InstantiatePrefab(grid, brushTarget, wallPrefab, wallPos);
                        }
                    }
                }
            }
        }

        // Place ceiling
        for (int x = 0; x < roomSize.x; x++)
        {
            for (int y = 0; y < roomSize.y; y++)
            {
                Vector3Int ceilPos = position + new Vector3Int(x, y, wallHeight + 1);
                InstantiatePrefab(grid, brushTarget, ceilPrefab, ceilPos);
            }
        }
    }

    private bool HasGround(GridLayout grid, GameObject brushTarget, Vector3Int pos)
    {
        foreach (Transform child in brushTarget.transform)
        {
            if (child.position == grid.CellToWorld(pos))
                return true;
        }
        return false;
    }

    private bool ShouldPlaceWall(GridLayout grid, GameObject brushTarget, Vector3Int wallBase, int x, int y, Vector3Int roomOrigin)
    {
        // Check if adjacent cell is another room's ground
        Vector3Int[] directions = new Vector3Int[]
        {
            new Vector3Int(-1, 0, 0), // left
            new Vector3Int(1, 0, 0),  // right
            new Vector3Int(0, -1, 0), // down
            new Vector3Int(0, 1, 0)   // up
        };
        foreach (var dir in directions)
        {
            Vector3Int neighbor = wallBase + dir;
            // If this wall is on the edge and neighbor is inside another room, skip wall
            if (!IsInsideRoom(x + dir.x, y + dir.y))
            {
                if (HasGround(grid, brushTarget, neighbor))
                    return false;
            }
        }
        return true;
    }

    private bool IsInsideRoom(int x, int y)
    {
        return x >= 0 && x < roomSize.x && y >= 0 && y < roomSize.y;
    }

    private void InstantiatePrefab(GridLayout grid, GameObject parent, GameObject prefab, Vector3Int cellPosition)
    {
        Vector3 worldPos = grid.CellToWorld(cellPosition);
        GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        obj.transform.position = worldPos;
        obj.transform.SetParent(parent.transform);
        Undo.RegisterCreatedObjectUndo(obj, "Paint Room");
    }
}
