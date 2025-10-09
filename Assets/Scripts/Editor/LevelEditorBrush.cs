using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LevelLayoutBrush", menuName = "Brushes/Level Layout Brush")]
[CustomGridBrush(false, true, false, "Level Layout Brush")]
public class LevelEditorBrush : GridBrushBase
{
    public GameObject GroundPrefab;
    public GameObject WallPrefab;
    public GameObject CeilPrefab;
    public bool DebugLogs = false;



    public override void Paint(GridLayout grid, GameObject brushTarget, Vector3Int cellPosition)
    {
        if (brushTarget == null || GroundPrefab == null || WallPrefab == null || CeilPrefab == null)
            return;

        LevelEditorManager levelEditorManager = FindAnyObjectByType<LevelEditorManager>();
        if (levelEditorManager != null)
        {
            if (levelEditorManager.PlacedObjects == null)
            {
                levelEditorManager.PlacedObjects = new List<GameObject>();
            }
            else
            {
                // Clean up null references that accumulate when objects are destroyed
                levelEditorManager.CleanupNullReferences();
            }
        }

        Vector3 cellOrigin = grid.CellToWorld(cellPosition);
        float halfCellX = grid.cellSize.x * 0.5f;
        float halfCellZ = grid.cellSize.z * 0.5f;
        Vector3 cellCenter = cellOrigin + new Vector3(halfCellX, 0f, halfCellZ);

        Vector3 groundPosition = cellCenter + new Vector3(0f, -0.5f, 0f);
        if (!HasGroundAtCell(grid, brushTarget, cellPosition))
        {
            GameObject ground = InstantiatePrefabIntoParent(brushTarget, GroundPrefab, groundPosition, Quaternion.identity);
            if (ground != null && levelEditorManager != null && levelEditorManager.PlacedObjects != null) 
                levelEditorManager.PlacedObjects.Add(ground);
        }

        Vector3Int[] wallDirections =
        {
            new Vector3Int(-1, 0, 0),
            new Vector3Int(1, 0, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(0, 1, 0)
        };
        Quaternion[] wallRotations =
        {
            Quaternion.Euler(90f, 90f, 0f),
            Quaternion.Euler(90f, -90f, 0f),
            Quaternion.Euler(90f, 0f, 0f),
            Quaternion.Euler(90f, 180f, 0f)
        };

        float wallOffsetValue = Mathf.Max(halfCellX, halfCellZ) + 0.5f;
        Vector3[] wallOffsets =
        {
            new Vector3(-wallOffsetValue, 0f, 0f),
            new Vector3(wallOffsetValue, 0f, 0f),
            new Vector3(0f, 0f, -wallOffsetValue),
            new Vector3(0f, 0f, wallOffsetValue)
        };

        float lowWallY = cellCenter.y + halfCellX;
        float highWallY = cellCenter.y + 3f * halfCellX;
        float[] wallHeights = { lowWallY, highWallY };

        for (int wallIndex = 0; wallIndex < 4; wallIndex++)
        {
            Vector3Int neighborCell = cellPosition + wallDirections[wallIndex];
            bool neighborHasGround = HasGroundAtCell(grid, brushTarget, neighborCell);

            if (DebugLogs)
                Debug.Log($"paint cell {cellPosition} checking neighbor {neighborCell} hasGround={neighborHasGround}");

            if (!neighborHasGround)
            {
                foreach (float wallY in wallHeights)
                {
                    Vector3 wallPosition = cellCenter + wallOffsets[wallIndex];
                    wallPosition.y = wallY;
                    GameObject wallObject = InstantiatePrefabIntoParent(brushTarget, WallPrefab, wallPosition, wallRotations[wallIndex]);
                    if (wallObject != null && levelEditorManager != null && levelEditorManager.PlacedObjects != null) 
                        levelEditorManager.PlacedObjects.Add(wallObject);
                }
            }
            else
            {
                for (int wallHeightIndex = 0; wallHeightIndex < wallHeights.Length; wallHeightIndex++)
                {
                    Vector3 wallPosition = cellCenter + wallOffsets[wallIndex];
                    wallPosition.y = wallHeights[wallHeightIndex];
                    bool removed = RemoveWallAtPosition(brushTarget, wallPosition, DebugLogs, levelEditorManager);
                    if (DebugLogs) Debug.Log($"remove at {wallPosition} => {removed}");

                    Vector3 neighborOrigin = grid.CellToWorld(neighborCell);
                    Vector3 neighborCenter = neighborOrigin + new Vector3(halfCellX, 0f, halfCellZ);
                    int oppositeWallIndex = (wallIndex % 2 == 0) ? wallIndex + 1 : wallIndex - 1;
                    Vector3 neighborWallPosition = neighborCenter + wallOffsets[oppositeWallIndex];
                    neighborWallPosition.y = wallHeights[wallHeightIndex];
                    bool removedNeighbor = RemoveWallAtPosition(brushTarget, neighborWallPosition, DebugLogs, levelEditorManager);
                    if (DebugLogs) Debug.Log($"remove neighbor at {neighborWallPosition} => {removedNeighbor}");
                }
            }
        }

        Vector3 ceilPosition = cellCenter + new Vector3(0f, 6.5f, 0f);
        GameObject ceil = InstantiatePrefabIntoParent(brushTarget, CeilPrefab, ceilPosition, Quaternion.identity);
        if (ceil != null && levelEditorManager != null && levelEditorManager.PlacedObjects != null) 
            levelEditorManager.PlacedObjects.Add(ceil);
    }    private bool HasGroundAtCell(GridLayout grid, GameObject parent, Vector3Int cell)
    {
        Vector3 cellOrigin = grid.CellToWorld(cell);
        float halfCellX = grid.cellSize.x * 0.5f;
        float halfCellZ = grid.cellSize.z * 0.5f;
        Vector3 groundCenter = cellOrigin + new Vector3(halfCellX, -0.5f, halfCellZ);
        float tolerance = Mathf.Max(0.25f, Mathf.Min(halfCellX, halfCellZ) * 0.4f);

        // Only check the manager's list - our single source of truth
        LevelEditorManager levelEditorManager = FindAnyObjectByType<LevelEditorManager>();
        if (levelEditorManager != null && levelEditorManager.PlacedObjects != null)
        {
            foreach (GameObject placedGroundObject in levelEditorManager.PlacedObjects)
            {
                if (placedGroundObject == null) continue;
                if (!placedGroundObject.name.Contains("Ground")) continue;
                float distance = (placedGroundObject.transform.position - groundCenter).magnitude;
                if (DebugLogs) Debug.Log($"hasGround: manager check {placedGroundObject.name} at {placedGroundObject.transform.position} dist={distance:F3} tol={tolerance:F3}");
                if ((placedGroundObject.transform.position - groundCenter).sqrMagnitude <= tolerance * tolerance)
                    return true;
            }
        }

        return false;
    }

    private bool RemoveWallAtPosition(GameObject parent, Vector3 worldPosition, bool debug, LevelEditorManager levelEditorManager = null)
    {
        float tolerance = 0.5f;
        bool removedAny = false;
        List<GameObject> wallsToRemove = new List<GameObject>();

        // Check the manager's list - this is our single source of truth
        if (levelEditorManager != null && levelEditorManager.PlacedObjects != null)
        {
            foreach (GameObject obj in levelEditorManager.PlacedObjects)
            {
                if (obj == null) continue;
                if (!obj.name.Contains("Wall")) continue;
                if ((obj.transform.position - worldPosition).sqrMagnitude <= tolerance * tolerance)
                {
                    wallsToRemove.Add(obj);
                }
            }
        }

        // Remove all found walls
        foreach (GameObject wall in wallsToRemove)
        {
            if (debug) Debug.Log($"removing wall {wall.name} at {wall.transform.position}");
            if (levelEditorManager != null && levelEditorManager.PlacedObjects != null)
            {
                levelEditorManager.PlacedObjects.Remove(wall);
            }
            Undo.DestroyObjectImmediate(wall);
            removedAny = true;
        }

        return removedAny;
    }

    private GameObject InstantiatePrefabIntoParent(GameObject parent, GameObject prefab, Vector3 worldPosition, Quaternion rotation)
    {
        GameObject instantiatedObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (instantiatedObject == null) return null;
        instantiatedObject.transform.position = worldPosition;
        instantiatedObject.transform.rotation = rotation;
        instantiatedObject.transform.SetParent(parent.transform);
        Undo.RegisterCreatedObjectUndo(instantiatedObject, "Paint Room");
        return instantiatedObject;
    }
}
