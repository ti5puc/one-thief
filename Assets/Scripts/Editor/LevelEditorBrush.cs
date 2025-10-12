using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LevelLayoutBrush", menuName = "Brushes/Level Layout Brush")]
[CustomGridBrush(false, true, false, "Level Layout Brush")]
public class LevelEditorBrush : GridBrushBase
{
    // Constants for level geometry
    private const float GROUND_VERTICAL_OFFSET = -0.5f;
    private const float CEILING_VERTICAL_OFFSET = 6.5f;
    private const float WALL_DISTANCE_FROM_CELL_EDGE = 0.5f;
    private const float LOW_WALL_HEIGHT_MULTIPLIER = 1f;
    private const float HIGH_WALL_HEIGHT_MULTIPLIER = 3f;
    private const float POSITION_MATCH_TOLERANCE = 0.5f;
    private const float GROUND_DETECTION_TOLERANCE_MULTIPLIER = 0.4f;
    private const float MINIMUM_GROUND_DETECTION_TOLERANCE = 0.25f;

    private const int WALL_DIRECTION_COUNT = 4;
    private const int LEFT_WALL_INDEX = 0;
    private const int RIGHT_WALL_INDEX = 1;
    private const int BOTTOM_WALL_INDEX = 2;
    private const int TOP_WALL_INDEX = 3;

    public GameObject GroundPrefab;
    public GameObject WallPrefab;
    public GameObject CeilPrefab;
    public bool DebugLogs = false;

    // Helper struct to encapsulate cell geometry calculations
    private struct CellGeometry
    {
        public GridLayout Grid;
        public Vector3 CellOrigin;
        public Vector3 CellCenter;
        public float HalfCellWidth;
        public float HalfCellDepth;
    }

    // Helper struct to encapsulate wall configuration data
    private struct WallConfiguration
    {
        public Vector3Int[] WallDirections;
        public Quaternion[] WallRotations;
        public Vector3[] WallOffsets;
        public float[] WallHeights;
    }

    public override void Paint(GridLayout grid, GameObject brushTarget, Vector3Int cellPosition)
    {
        if (!ValidatePaintRequirements(brushTarget))
            return;

        // if the cell already has ground, skip painting
        if (HasGroundAtCell(grid, brushTarget, cellPosition))
        {
            if (DebugLogs)
                Debug.Log($"Paint skipped at {cellPosition}: cell already painted (ground exists)");

            return;
        }

        LevelEditorManager levelEditorManager = InitializeLevelEditorManager();

        CellGeometry cellGeometry = CalculateCellGeometry(grid, cellPosition);

        PlaceGroundIfNeeded(brushTarget, cellPosition, cellGeometry, levelEditorManager);
        ProcessWallsForCell(grid, brushTarget, cellPosition, cellGeometry, levelEditorManager);
        PlaceCeiling(brushTarget, cellGeometry, levelEditorManager);
    }

    private bool ValidatePaintRequirements(GameObject brushTarget)
    {
        return brushTarget != null && GroundPrefab != null && WallPrefab != null && CeilPrefab != null;
    }

    private LevelEditorManager InitializeLevelEditorManager()
    {
        LevelEditorManager levelEditorManager = FindAnyObjectByType<LevelEditorManager>();
        if (levelEditorManager != null)
        {
            if (levelEditorManager.PlacedObjects == null)
            {
                levelEditorManager.PlacedObjects = new List<GameObject>();
            }
            else
            {
                levelEditorManager.CleanupNullReferences();
            }
        }
        return levelEditorManager;
    }

    private void PlaceGroundIfNeeded(GameObject brushTarget, Vector3Int cellPosition, CellGeometry cellGeometry, LevelEditorManager levelEditorManager)
    {
        if (!HasGroundAtCell(cellGeometry.Grid, brushTarget, cellPosition))
        {
            Vector3 groundPosition = cellGeometry.CellCenter + new Vector3(0f, GROUND_VERTICAL_OFFSET, 0f);
            GameObject ground = InstantiatePrefabIntoParent(brushTarget, GroundPrefab, groundPosition, Quaternion.identity);
            RegisterPlacedObject(ground, levelEditorManager);
        }
    }

    private void ProcessWallsForCell(GridLayout grid, GameObject brushTarget, Vector3Int cellPosition, CellGeometry cellGeometry, LevelEditorManager levelEditorManager)
    {
        WallConfiguration wallConfig = GetWallConfiguration(cellGeometry);

        for (int currentWallIndex = 0; currentWallIndex < WALL_DIRECTION_COUNT; currentWallIndex++)
        {
            Vector3Int neighborCellPosition = cellPosition + wallConfig.WallDirections[currentWallIndex];
            bool neighborHasGround = HasGroundAtCell(grid, brushTarget, neighborCellPosition);

            LogNeighborCheck(cellPosition, neighborCellPosition, neighborHasGround);

            if (!neighborHasGround)
            {
                PlaceWallsForEdge(brushTarget, cellGeometry, wallConfig, currentWallIndex, levelEditorManager);
            }
            else
            {
                RemoveWallsBetweenAdjacentCells(grid, brushTarget, cellPosition, neighborCellPosition, cellGeometry, wallConfig, currentWallIndex, levelEditorManager);
            }
        }
    }

    private void PlaceWallsForEdge(GameObject brushTarget, CellGeometry cellGeometry, WallConfiguration wallConfig, int wallDirectionIndex, LevelEditorManager levelEditorManager)
    {
        foreach (float wallHeight in wallConfig.WallHeights)
        {
            Vector3 wallPosition = cellGeometry.CellCenter + wallConfig.WallOffsets[wallDirectionIndex];
            wallPosition.y = wallHeight;
            GameObject wallObject = InstantiatePrefabIntoParent(brushTarget, WallPrefab, wallPosition, wallConfig.WallRotations[wallDirectionIndex]);
            RegisterPlacedObject(wallObject, levelEditorManager);
        }
    }

    private void RemoveWallsBetweenAdjacentCells(GridLayout grid, GameObject brushTarget, Vector3Int currentCellPosition, Vector3Int neighborCellPosition, CellGeometry cellGeometry, WallConfiguration wallConfig, int currentWallIndex, LevelEditorManager levelEditorManager)
    {
        for (int wallHeightIndex = 0; wallHeightIndex < wallConfig.WallHeights.Length; wallHeightIndex++)
        {
            Vector3 currentCellWallPosition = cellGeometry.CellCenter + wallConfig.WallOffsets[currentWallIndex];
            currentCellWallPosition.y = wallConfig.WallHeights[wallHeightIndex];
            bool removedFromCurrentCell = RemoveWallAtPosition(brushTarget, currentCellWallPosition, DebugLogs, levelEditorManager);

            if (DebugLogs)
                Debug.Log($"remove at {currentCellWallPosition} => {removedFromCurrentCell}");

            CellGeometry neighborGeometry = CalculateCellGeometry(grid, neighborCellPosition);
            int oppositeWallIndex = GetOppositeWallIndex(currentWallIndex);
            Vector3 neighborWallPosition = neighborGeometry.CellCenter + wallConfig.WallOffsets[oppositeWallIndex];
            neighborWallPosition.y = wallConfig.WallHeights[wallHeightIndex];
            bool removedFromNeighbor = RemoveWallAtPosition(brushTarget, neighborWallPosition, DebugLogs, levelEditorManager);

            if (DebugLogs)
                Debug.Log($"remove neighbor at {neighborWallPosition} => {removedFromNeighbor}");
        }
    }

    private void PlaceCeiling(GameObject brushTarget, CellGeometry cellGeometry, LevelEditorManager levelEditorManager)
    {
        Vector3 ceilingPosition = cellGeometry.CellCenter + new Vector3(0f, CEILING_VERTICAL_OFFSET, 0f);
        GameObject ceiling = InstantiatePrefabIntoParent(brushTarget, CeilPrefab, ceilingPosition, Quaternion.identity);
        RegisterPlacedObject(ceiling, levelEditorManager);
    }

    private void RegisterPlacedObject(GameObject placedObject, LevelEditorManager levelEditorManager)
    {
        if (placedObject != null && levelEditorManager != null && levelEditorManager.PlacedObjects != null)
            levelEditorManager.PlacedObjects.Add(placedObject);
    }

    private void LogNeighborCheck(Vector3Int currentCell, Vector3Int neighborCell, bool neighborHasGround)
    {
        if (DebugLogs)
            Debug.Log($"paint cell {currentCell} checking neighbor {neighborCell} hasGround={neighborHasGround}");
    }

    private CellGeometry CalculateCellGeometry(GridLayout grid, Vector3Int cellPosition)
    {
        Vector3 cellOrigin = grid.CellToWorld(cellPosition);
        float halfCellWidth = grid.cellSize.x * 0.5f;
        float halfCellDepth = grid.cellSize.z * 0.5f;
        Vector3 cellCenter = cellOrigin + new Vector3(halfCellWidth, 0f, halfCellDepth);

        return new CellGeometry
        {
            Grid = grid,
            CellOrigin = cellOrigin,
            CellCenter = cellCenter,
            HalfCellWidth = halfCellWidth,
            HalfCellDepth = halfCellDepth
        };
    }

    private WallConfiguration GetWallConfiguration(CellGeometry cellGeometry)
    {
        Vector3Int[] wallDirections = new Vector3Int[]
        {
            new Vector3Int(-1, 0, 0),  // Left
            new Vector3Int(1, 0, 0),   // Right
            new Vector3Int(0, -1, 0),  // Bottom
            new Vector3Int(0, 1, 0)    // Top
        };

        Quaternion[] wallRotations = new Quaternion[]
        {
            Quaternion.Euler(90f, 90f, 0f),   // Left wall rotation
            Quaternion.Euler(90f, -90f, 0f),  // Right wall rotation
            Quaternion.Euler(90f, 0f, 0f),    // Bottom wall rotation
            Quaternion.Euler(90f, 180f, 0f)   // Top wall rotation
        };

        float wallOffsetDistance = Mathf.Max(cellGeometry.HalfCellWidth, cellGeometry.HalfCellDepth) + WALL_DISTANCE_FROM_CELL_EDGE;
        Vector3[] wallOffsets = new Vector3[]
        {
            new Vector3(-wallOffsetDistance, 0f, 0f),  // Left wall offset
            new Vector3(wallOffsetDistance, 0f, 0f),   // Right wall offset
            new Vector3(0f, 0f, -wallOffsetDistance),  // Bottom wall offset
            new Vector3(0f, 0f, wallOffsetDistance)    // Top wall offset
        };

        float lowWallHeight = cellGeometry.CellCenter.y + cellGeometry.HalfCellWidth * LOW_WALL_HEIGHT_MULTIPLIER;
        float highWallHeight = cellGeometry.CellCenter.y + cellGeometry.HalfCellWidth * HIGH_WALL_HEIGHT_MULTIPLIER;
        float[] wallHeights = { lowWallHeight, highWallHeight };

        return new WallConfiguration
        {
            WallDirections = wallDirections,
            WallRotations = wallRotations,
            WallOffsets = wallOffsets,
            WallHeights = wallHeights
        };
    }

    private int GetOppositeWallIndex(int wallIndex)
    {
        // For walls: 0<->1 (Left<->Right), 2<->3 (Bottom<->Top)
        return (wallIndex % 2 == 0) ? wallIndex + 1 : wallIndex - 1;
    }

    public override void Erase(GridLayout grid, GameObject brushTarget, Vector3Int cellPosition)
    {
        if (brushTarget == null)
            return;

        LevelEditorManager levelEditorManager = InitializeLevelEditorManager();
        CellGeometry cellGeometry = CalculateCellGeometry(grid, cellPosition);

        List<GameObject> objectsToRemove = FindAllObjectsInCell(cellGeometry, levelEditorManager);
        RemoveFoundObjects(objectsToRemove, levelEditorManager);
        AddWallsToNeighboringCells(grid, brushTarget, cellPosition, cellGeometry, levelEditorManager);

        if (DebugLogs)
            Debug.Log($"Erase at cell {cellPosition}: removed {objectsToRemove.Count} objects");
    }

    private List<GameObject> FindAllObjectsInCell(CellGeometry cellGeometry, LevelEditorManager levelEditorManager)
    {
        List<GameObject> objectsToRemove = new List<GameObject>();

        if (levelEditorManager == null || levelEditorManager.PlacedObjects == null)
            return objectsToRemove;

        FindGroundInCell(cellGeometry, levelEditorManager, objectsToRemove);
        FindCeilingInCell(cellGeometry, levelEditorManager, objectsToRemove);
        FindWallsInCell(cellGeometry, levelEditorManager, objectsToRemove);

        return objectsToRemove;
    }

    private void FindGroundInCell(CellGeometry cellGeometry, LevelEditorManager levelEditorManager, List<GameObject> objectsToRemove)
    {
        Vector3 groundPosition = cellGeometry.CellCenter + new Vector3(0f, GROUND_VERTICAL_OFFSET, 0f);

        foreach (GameObject placedObject in levelEditorManager.PlacedObjects)
        {
            if (placedObject == null) continue;
            if (!placedObject.name.Contains("Ground")) continue;

            if (IsPositionMatch(placedObject.transform.position, groundPosition, POSITION_MATCH_TOLERANCE))
            {
                objectsToRemove.Add(placedObject);
            }
        }
    }

    private void FindCeilingInCell(CellGeometry cellGeometry, LevelEditorManager levelEditorManager, List<GameObject> objectsToRemove)
    {
        Vector3 ceilingPosition = cellGeometry.CellCenter + new Vector3(0f, CEILING_VERTICAL_OFFSET, 0f);

        foreach (GameObject placedObject in levelEditorManager.PlacedObjects)
        {
            if (placedObject == null) continue;
            if (!placedObject.name.Contains("Ceil")) continue;

            if (IsPositionMatch(placedObject.transform.position, ceilingPosition, POSITION_MATCH_TOLERANCE))
            {
                objectsToRemove.Add(placedObject);
            }
        }
    }

    private void FindWallsInCell(CellGeometry cellGeometry, LevelEditorManager levelEditorManager, List<GameObject> objectsToRemove)
    {
        WallConfiguration wallConfig = GetWallConfiguration(cellGeometry);

        foreach (GameObject placedObject in levelEditorManager.PlacedObjects)
        {
            if (placedObject == null) continue;
            if (!placedObject.name.Contains("Wall")) continue;

            foreach (Vector3 wallOffset in wallConfig.WallOffsets)
            {
                foreach (float wallHeight in wallConfig.WallHeights)
                {
                    Vector3 wallPosition = cellGeometry.CellCenter + wallOffset;
                    wallPosition.y = wallHeight;

                    if (IsPositionMatch(placedObject.transform.position, wallPosition, POSITION_MATCH_TOLERANCE))
                    {
                        if (!objectsToRemove.Contains(placedObject))
                        {
                            objectsToRemove.Add(placedObject);
                        }
                    }
                }
            }
        }
    }

    private void RemoveFoundObjects(List<GameObject> objectsToRemove, LevelEditorManager levelEditorManager)
    {
        foreach (GameObject objectToRemove in objectsToRemove)
        {
            if (DebugLogs)
                Debug.Log($"Erasing {objectToRemove.name} at {objectToRemove.transform.position}");

            if (levelEditorManager != null && levelEditorManager.PlacedObjects != null)
            {
                levelEditorManager.PlacedObjects.Remove(objectToRemove);
            }
            Undo.DestroyObjectImmediate(objectToRemove);
        }
    }

    private void AddWallsToNeighboringCells(GridLayout grid, GameObject brushTarget, Vector3Int cellPosition, CellGeometry cellGeometry, LevelEditorManager levelEditorManager)
    {
        WallConfiguration wallConfig = GetWallConfiguration(cellGeometry);

        for (int currentWallIndex = 0; currentWallIndex < WALL_DIRECTION_COUNT; currentWallIndex++)
        {
            Vector3Int neighborCellPosition = cellPosition + wallConfig.WallDirections[currentWallIndex];
            bool neighborHasGround = HasGroundAtCell(grid, brushTarget, neighborCellPosition);

            LogEraseNeighborCheck(cellPosition, neighborCellPosition, neighborHasGround);

            if (neighborHasGround)
            {
                PlaceWallsOnNeighborEdge(grid, brushTarget, neighborCellPosition, currentWallIndex, wallConfig, levelEditorManager);
            }
        }
    }

    private void PlaceWallsOnNeighborEdge(GridLayout grid, GameObject brushTarget, Vector3Int neighborCellPosition, int currentWallIndex, WallConfiguration wallConfig, LevelEditorManager levelEditorManager)
    {
        CellGeometry neighborGeometry = CalculateCellGeometry(grid, neighborCellPosition);
        int oppositeWallIndex = GetOppositeWallIndex(currentWallIndex);

        foreach (float wallHeight in wallConfig.WallHeights)
        {
            Vector3 wallPosition = neighborGeometry.CellCenter + wallConfig.WallOffsets[oppositeWallIndex];
            wallPosition.y = wallHeight;

            if (!DoesWallExistAtPosition(wallPosition, levelEditorManager))
            {
                GameObject wallObject = InstantiatePrefabIntoParent(brushTarget, WallPrefab, wallPosition, wallConfig.WallRotations[oppositeWallIndex]);
                RegisterPlacedObject(wallObject, levelEditorManager);

                if (DebugLogs)
                    Debug.Log($"Added wall at {wallPosition} on neighbor {neighborCellPosition} side");
            }
        }
    }

    private bool DoesWallExistAtPosition(Vector3 wallPosition, LevelEditorManager levelEditorManager)
    {
        if (levelEditorManager == null || levelEditorManager.PlacedObjects == null)
            return false;

        foreach (GameObject placedObject in levelEditorManager.PlacedObjects)
        {
            if (placedObject == null) continue;
            if (!placedObject.name.Contains("Wall")) continue;

            if (IsPositionMatch(placedObject.transform.position, wallPosition, POSITION_MATCH_TOLERANCE))
            {
                return true;
            }
        }

        return false;
    }

    private void LogEraseNeighborCheck(Vector3Int currentCell, Vector3Int neighborCell, bool neighborHasGround)
    {
        if (DebugLogs)
            Debug.Log($"erase cell {currentCell} checking neighbor {neighborCell} hasGround={neighborHasGround}");
    }

    private bool IsPositionMatch(Vector3 position1, Vector3 position2, float tolerance)
    {
        return (position1 - position2).sqrMagnitude <= tolerance * tolerance;
    }

    private bool HasGroundAtCell(GridLayout grid, GameObject parent, Vector3Int cellPosition)
    {
        CellGeometry cellGeometry = CalculateCellGeometry(grid, cellPosition);
        Vector3 groundCenterPosition = cellGeometry.CellOrigin + new Vector3(cellGeometry.HalfCellWidth, GROUND_VERTICAL_OFFSET, cellGeometry.HalfCellDepth);
        float detectionTolerance = Mathf.Max(MINIMUM_GROUND_DETECTION_TOLERANCE, Mathf.Min(cellGeometry.HalfCellWidth, cellGeometry.HalfCellDepth) * GROUND_DETECTION_TOLERANCE_MULTIPLIER);

        LevelEditorManager levelEditorManager = FindAnyObjectByType<LevelEditorManager>();
        if (levelEditorManager != null && levelEditorManager.PlacedObjects != null)
        {
            foreach (GameObject placedGroundObject in levelEditorManager.PlacedObjects)
            {
                if (placedGroundObject == null) continue;
                if (!placedGroundObject.name.Contains("Ground")) continue;

                float distanceToGround = (placedGroundObject.transform.position - groundCenterPosition).magnitude;
                if (DebugLogs)
                    Debug.Log($"hasGround: manager check {placedGroundObject.name} at {placedGroundObject.transform.position} dist={distanceToGround:F3} tol={detectionTolerance:F3}");

                if (IsPositionMatch(placedGroundObject.transform.position, groundCenterPosition, detectionTolerance))
                    return true;
            }
        }

        return false;
    }

    private bool RemoveWallAtPosition(GameObject parent, Vector3 worldPosition, bool enableDebugLogs, LevelEditorManager levelEditorManager = null)
    {
        bool removedAnyWall = false;
        List<GameObject> wallsToRemove = new List<GameObject>();

        if (levelEditorManager != null && levelEditorManager.PlacedObjects != null)
        {
            foreach (GameObject placedObject in levelEditorManager.PlacedObjects)
            {
                if (placedObject == null) continue;
                if (!placedObject.name.Contains("Wall")) continue;

                if (IsPositionMatch(placedObject.transform.position, worldPosition, POSITION_MATCH_TOLERANCE))
                {
                    wallsToRemove.Add(placedObject);
                }
            }
        }

        foreach (GameObject wallToRemove in wallsToRemove)
        {
            if (enableDebugLogs)
                Debug.Log($"removing wall {wallToRemove.name} at {wallToRemove.transform.position}");

            if (levelEditorManager != null && levelEditorManager.PlacedObjects != null)
            {
                levelEditorManager.PlacedObjects.Remove(wallToRemove);
            }
            Undo.DestroyObjectImmediate(wallToRemove);
            removedAnyWall = true;
        }

        return removedAnyWall;
    }

    private GameObject InstantiatePrefabIntoParent(GameObject parentObject, GameObject prefabToInstantiate, Vector3 worldPosition, Quaternion worldRotation)
    {
        GameObject instantiatedObject = (GameObject)PrefabUtility.InstantiatePrefab(prefabToInstantiate);
        if (instantiatedObject == null)
            return null;

        instantiatedObject.transform.position = worldPosition;
        instantiatedObject.transform.rotation = worldRotation;
        instantiatedObject.transform.SetParent(parentObject.transform);
        Undo.RegisterCreatedObjectUndo(instantiatedObject, "Paint Room");

        return instantiatedObject;
    }
}
