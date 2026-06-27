using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using Random = UnityEngine.Random;

public class PropsSpawner : MonoBehaviour
{
    // ── Shared data types ─────────────────────────────────────────────────────

    [Serializable]
    public class PropEntry
    {
        public GameObject prefab;
        [Range(0f, 1f)] public float weight = 1f;
        public Vector3 positionOffset;
        public Vector3 rotationOffset;
    }

    [Serializable]
    public class PropCategory
    {
        public bool enabled = true;
        [Range(0f, 1f)] public float spawnChancePerPoint = 0.25f;
        [Min(0)] public int maxCount = 30;
        public LayerMask detectionLayer;
        [Min(0.1f)] public float raycastOriginHeight = 5f;
        [Min(0.1f)] public float raycastDistance = 10f;
        public PropEntry[] entries;
    }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Grid")]
    [SerializeField] private float cellSize = 3f;
    [Tooltip("Extra manual nudge applied to the auto-detected grid origin (XZ).")]
    [SerializeField] private Vector2 gridOffset;

    [Header("Spawn Settings")]
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private int seed;

    [Header("Floor Props")]
    [Tooltip("Spawned at cell border points (corners + edge midpoints) that sit next to a wall — never in the open, never the cell center.")]
    [SerializeField] private PropCategory floorProps;
    [Tooltip("A floor point is only valid if a wall is within this distance. Open-area points are discarded.")]
    [SerializeField] private float floorWallProximity = 0.8f;
    [Tooltip("How far the prop is pushed away from the wall, into the room.")]
    [SerializeField] private float floorWallClearance = 0.4f;
    [Tooltip("Height above the floor at which the wall proximity is tested.")]
    [SerializeField] private float floorWallProbeHeight = 0.5f;

    [Header("Ceiling Props")]
    [Tooltip("Spawned at the center of each valid ceiling cell.")]
    [SerializeField] private PropCategory ceilProps;

    [Header("Wall Props")]
    [Tooltip("Spawned on walls adjacent to valid floor cells. Rotation is snapped to face out from the wall.")]
    [SerializeField] private PropCategory wallProps;
    [SerializeField] private float wallCheckDistance = 2f;
    [SerializeField] private float wallCheckHeightOffset = 1.5f;
    [Tooltip("How square the wall must face our probe to accept it (1 = perfectly flat-on).")]
    [Range(0f, 1f)][SerializeField] private float wallNormalTolerance = 0.5f;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;
    [Tooltip("Layout root used to preview the grid in the editor. Leave empty to preview from this object's parent.")]
    [SerializeField] private Transform previewLayoutRoot;
    [SerializeField] private bool drawValidCells = true;
    [SerializeField] private bool drawFloorPoints = true;
    [SerializeField] private bool drawCeilPoints = true;
    [SerializeField] private bool drawWallProbes = true;
    [SerializeField] private float gizmoPointRadius = 0.12f;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private readonly List<GameObject> spawnedObjects = new();
    private Transform propsParent;
    private Transform activeLayoutRoot; // set per Spawn() / gizmos pass

    // Grid frame, resolved per Spawn() from the layout's floor bounds.
    private Vector3 gridOrigin;  // world XZ of cell (0,0)'s min corner; Y = floor level
    private int gridRows;
    private int gridCols;

    // ── Public API ────────────────────────────────────────────────────────────

    [Button]
    public void Spawn() => Spawn(null);

    public void Spawn(Transform layoutRoot)
    {
        Clear();

        if (useRandomSeed)
        {
            if (GameManager.IsResettingLevel)
                seed = GameManager.SavedPropsSeed;
            else
            {
                seed = Random.Range(int.MinValue, int.MaxValue);
                GameManager.SavedPropsSeed = seed;
            }
        }
        Random.InitState(seed);

        if (layoutRoot != null)
        {
            propsParent      = layoutRoot;
            activeLayoutRoot = layoutRoot;
        }

        if (!ResolveGrid(activeLayoutRoot))
        {
            Debug.LogWarning("[PropsSpawner] Could not resolve a floor grid — nothing spawned. Is the floor detection layer set?", this);
            return;
        }

        bool[,] floorCells = DetectCells(floorProps.detectionLayer);
        bool[,] ceilCells  = DetectCells(ceilProps.detectionLayer);

        Debug.Log($"[PropsSpawner] Grid {gridCols}×{gridRows}, root={(activeLayoutRoot != null ? activeLayoutRoot.name : "null")}", this);

        if (floorProps.enabled) SpawnFloor(floorCells);
        if (ceilProps.enabled) SpawnCeiling(ceilCells);
        if (wallProps.enabled) SpawnWalls(floorCells);
    }

    [Button]
    public void Clear()
    {
        foreach (var go in spawnedObjects)
            if (go != null) DestroyImmediate(go);
        spawnedObjects.Clear();
    }

    // ── Grid resolution ───────────────────────────────────────────────────────

    // Anchor the grid to the actual floor geometry so cell borders line up with
    // the real tile edges. Only floor-layer colliders are considered, otherwise
    // wall thickness would shift the origin.
    private bool ResolveGrid(Transform layoutRoot)
    {
        if (layoutRoot == null)
        {
            // Fallback: centre a fixed grid on this transform.
            gridRows = gridCols = 20;
            gridOrigin = new Vector3(
                transform.position.x - gridCols * cellSize * 0.5f + gridOffset.x,
                transform.position.y,
                transform.position.z - gridRows * cellSize * 0.5f + gridOffset.y);
            return true;
        }

        int floorMask = ToLayerMask(floorProps.detectionLayer);
        bool found = false;
        Bounds bounds = default;

        foreach (var col in layoutRoot.GetComponentsInChildren<Collider>())
        {
            if (((1 << col.gameObject.layer) & floorMask) == 0) continue;
            if (!found) { bounds = col.bounds; found = true; }
            else bounds.Encapsulate(col.bounds);
        }

        if (!found) return false;

        gridCols = Mathf.Max(1, Mathf.RoundToInt(bounds.size.x / cellSize));
        gridRows = Mathf.Max(1, Mathf.RoundToInt(bounds.size.z / cellSize));
        gridOrigin = new Vector3(bounds.min.x + gridOffset.x, bounds.max.y, bounds.min.z + gridOffset.y);
        return true;
    }

    // Cell (r,c) center in world space.
    private Vector3 CellCenter(int r, int c) => new(
        gridOrigin.x + (c + 0.5f) * cellSize,
        gridOrigin.y,
        gridOrigin.z + (r + 0.5f) * cellSize);

    // ── Detection ─────────────────────────────────────────────────────────────

    // Mark cells by checking whether each cell's XZ center falls inside a
    // collider on the given layer. Uses the layout's children directly —
    // no distance limits, no raycast misses.
    private bool[,] DetectCells(LayerMask layer)
    {
        var result = new bool[gridRows, gridCols];
        if (activeLayoutRoot == null) return result;

        int mask = ToLayerMask(layer);
        var colliders = new List<Bounds>();

        foreach (var col in activeLayoutRoot.GetComponentsInChildren<Collider>())
        {
            if (((1 << col.gameObject.layer) & mask) == 0) continue;
            colliders.Add(col.bounds);
        }

        for (int r = 0; r < gridRows; r++)
        for (int c = 0; c < gridCols; c++)
        {
            Vector3 center = CellCenter(r, c);
            foreach (var b in colliders)
            {
                if (center.x >= b.min.x && center.x <= b.max.x &&
                    center.z >= b.min.z && center.z <= b.max.z)
                {
                    result[r, c] = true;
                    break;
                }
            }
        }
        return result;
    }

    // Collect all Collider components on the given layer from a root transform.
    private List<Collider> GetChildColliders(LayerMask layer)
    {
        var list = new List<Collider>();
        if (activeLayoutRoot == null) return list;
        int mask = ToLayerMask(layer);
        foreach (var col in activeLayoutRoot.GetComponentsInChildren<Collider>())
            if (((1 << col.gameObject.layer) & mask) != 0)
                list.Add(col);
        return list;
    }

    // ── Floor spawning ────────────────────────────────────────────────────────

    private void SpawnFloor(bool[,] cells)
    {
        var points = CollectFloorPoints(cells);
        Shuffle(points);

        int spawned = 0;
        foreach (var pos in points)
        {
            if (spawned >= floorProps.maxCount) break;
            if (Random.value > floorProps.spawnChancePerPoint) continue;

            var entry = PickWeighted(floorProps.entries);
            if (entry?.prefab == null) continue;

            float yRot = Random.Range(0f, 360f);
            var rot = Quaternion.Euler(entry.rotationOffset.x, entry.rotationOffset.y + yRot, entry.rotationOffset.z);
            // World-space offset so Y stays vertical regardless of rotation.
            spawnedObjects.Add(SpawnProp(entry.prefab, pos + entry.positionOffset, rot));
            spawned++;
        }
    }

    // Cardinal direction paired with its row/col delta for neighbor lookup.
    private static readonly (Vector3 dir, int dr, int dc)[] CardinalWithDelta =
    {
        (Vector3.forward, 1,  0),
        (Vector3.back,   -1,  0),
        (Vector3.right,   0,  1),
        (Vector3.left,    0, -1),
    };

    // Collects valid floor spawn positions.
    // Strategy: per valid cell, cast DOWN from the CENTER (reliable Y), then probe
    // each cardinal direction for a wall only where there is no floor neighbor.
    // This avoids border-node raycasts that start inside wall colliders.
    // Shared by SpawnFloor and the gizmos so the preview matches exactly.
    private List<Vector3> CollectFloorPoints(bool[,] cells)
    {
        var positions    = new List<Vector3>();
        int floorMask    = ToLayerMask(floorProps.detectionLayer);
        int wallMask     = ToLayerMask(wallProps.detectionLayer);
        float probeRange = cellSize * 0.5f + floorWallProximity;
        float inset      = cellSize * 0.5f - floorWallClearance;
        var wallColliders = GetChildColliders(wallProps.detectionLayer);

        // Pre-compute ground height from each cell center (reliable, never inside a wall).
        var groundY   = new float[gridRows, gridCols];
        var hasGround = new bool[gridRows, gridCols];
        for (int r = 0; r < gridRows; r++)
        for (int c = 0; c < gridCols; c++)
        {
            if (!cells[r, c]) continue;
            Vector3 o = CellCenter(r, c) + Vector3.up * floorProps.raycastOriginHeight;
            if (Physics.Raycast(o, Vector3.down, out RaycastHit h,
                    floorProps.raycastDistance + floorProps.raycastOriginHeight, floorMask))
            {
                groundY[r, c]   = h.point.y;
                hasGround[r, c] = true;
            }
        }

        // ── Edge midpoints + inner (concave) corners ──────────────────────────
        for (int r = 0; r < gridRows; r++)
        for (int c = 0; c < gridCols; c++)
        {
            if (!cells[r, c] || !hasGround[r, c]) continue;

            float   gY     = groundY[r, c];
            Vector3 center = CellCenter(r, c);
            Vector3 probe  = new(center.x, gY + floorWallProbeHeight, center.z);

            bool wallN = false, wallS = false, wallE = false, wallW = false;

            foreach (var (dir, dr, dc) in CardinalWithDelta)
            {
                int nr = r + dr, nc = c + dc;
                if (CellHasFloor(cells, nr, nc)) continue; // interior edge — skip

                if (!FindWallPoint(probe, dir, probeRange, wallMask, wallColliders, out Vector3 wallPoint))
                    continue;

                positions.Add(new Vector3(
                    wallPoint.x - dir.x * floorWallClearance,
                    gY,
                    wallPoint.z - dir.z * floorWallClearance));

                if      (dir == Vector3.forward) wallN = true;
                else if (dir == Vector3.back)    wallS = true;
                else if (dir == Vector3.right)   wallE = true;
                else                             wallW = true;
            }

            // Inner corner: one cell has walls on two perpendicular sides.
            if (wallN && wallE) positions.Add(new Vector3(center.x + inset, gY, center.z + inset));
            if (wallN && wallW) positions.Add(new Vector3(center.x - inset, gY, center.z + inset));
            if (wallS && wallE) positions.Add(new Vector3(center.x + inset, gY, center.z - inset));
            if (wallS && wallW) positions.Add(new Vector3(center.x - inset, gY, center.z - inset));
        }

        // ── Outer (convex) corners ─────────────────────────────────────────────
        // Scan every 2×2 block. When exactly one cell in the block has floor, the
        // shared corner between the four cells is a convex outer corner that the
        // per-cell loop above never generates (no single cell has two walls there).
        for (int r = -1; r < gridRows; r++)
        for (int c = -1; c < gridCols; c++)
        {
            bool bl = CellHasFloor(cells, r,     c);
            bool br = CellHasFloor(cells, r,     c + 1);
            bool tl = CellHasFloor(cells, r + 1, c);
            bool tr = CellHasFloor(cells, r + 1, c + 1);

            if ((bl ? 1 : 0) + (br ? 1 : 0) + (tl ? 1 : 0) + (tr ? 1 : 0) != 1) continue;

            // World XZ of the shared corner between the 4 cells.
            float wx = gridOrigin.x + (c + 1) * cellSize;
            float wz = gridOrigin.z + (r + 1) * cellSize;

            // The one existing cell — inset the corner toward its center.
            int cr = bl ? r : br ? r     : tl ? r + 1 : r + 1;
            int cc = bl ? c : br ? c + 1 : tl ? c     : c + 1;
            if (!hasGround[cr, cc]) continue;

            Vector3 cellCenter = CellCenter(cr, cc);
            positions.Add(new Vector3(
                wx + Mathf.Sign(cellCenter.x - wx) * floorWallClearance,
                groundY[cr, cc],
                wz + Mathf.Sign(cellCenter.z - wz) * floorWallClearance));
        }

        return positions;
    }

    private bool CellHasFloor(bool[,] cells, int r, int c) =>
        r >= 0 && r < gridRows && c >= 0 && c < gridCols && cells[r, c];

    // Tries raycast first; if it misses, falls back to ClosestPoint on known
    // wall colliders so walls that raycasts clip through are still found.
    private static bool FindWallPoint(Vector3 probe, Vector3 dir, float range,
        int wallMask, List<Collider> wallColliders, out Vector3 hitPoint)
    {
        if (Physics.Raycast(probe, dir, out RaycastHit hit, range, wallMask))
        {
            hitPoint = hit.point;
            return true;
        }

        float bestDot = 0f;
        hitPoint = default;
        foreach (var col in wallColliders)
        {
            Vector3 cp    = col.ClosestPoint(probe + dir * range);
            Vector3 delta = cp - probe;
            delta.y = 0f;
            float dot  = Vector3.Dot(delta, dir);
            float perp = (delta - dir * dot).magnitude;
            if (dot <= 0f || dot > range) continue;
            if (perp > 0.5f) continue;          // off-axis — not this wall
            if (dot <= bestDot) continue;
            bestDot  = dot;
            hitPoint = new Vector3(cp.x, probe.y, cp.z);
        }
        return bestDot > 0f;
    }

    // ── Ceiling spawning ──────────────────────────────────────────────────────

    private void SpawnCeiling(bool[,] cells)
    {
        var cellList = new List<(int r, int c)>();
        for (int r = 0; r < gridRows; r++)
            for (int c = 0; c < gridCols; c++)
                if (cells[r, c]) cellList.Add((r, c));

        Shuffle(cellList);

        int layerMask = ToLayerMask(ceilProps.detectionLayer);
        int spawned = 0;

        foreach (var (r, c) in cellList)
        {
            if (spawned >= ceilProps.maxCount) break;
            if (Random.value > ceilProps.spawnChancePerPoint) continue;

            Vector3 center = CellCenter(r, c);
            Vector3 origin = center - Vector3.up * ceilProps.raycastOriginHeight; // start below, cast up
            if (!Physics.Raycast(origin, Vector3.up, out RaycastHit hit, ceilProps.raycastDistance + ceilProps.raycastOriginHeight, layerMask))
                continue;

            var entry = PickWeighted(ceilProps.entries);
            if (entry?.prefab == null) continue;

            float yRot = Random.Range(0f, 360f);
            var rot = Quaternion.Euler(entry.rotationOffset.x, entry.rotationOffset.y + yRot, entry.rotationOffset.z);
            spawnedObjects.Add(SpawnProp(entry.prefab, hit.point + entry.positionOffset, rot));
            spawned++;
        }
    }

    // ── Wall spawning ─────────────────────────────────────────────────────────

    private void SpawnWalls(bool[,] floorCells)
    {
        int floorMask = ToLayerMask(floorProps.detectionLayer);
        int wallMask = ToLayerMask(wallProps.detectionLayer);

        var candidates = new List<(Vector3 pos, Quaternion rot)>();

        for (int r = 0; r < gridRows; r++)
            for (int c = 0; c < gridCols; c++)
            {
                if (!floorCells[r, c]) continue;

                Vector3 center = CellCenter(r, c);

                // Floor surface height under this cell.
                Vector3 floorOrigin = center + Vector3.up * floorProps.raycastOriginHeight;
                if (!Physics.Raycast(floorOrigin, Vector3.down, out RaycastHit floorHit, floorProps.raycastDistance + floorProps.raycastOriginHeight, floorMask))
                    continue;

                Vector3 checkOrigin = new(center.x, floorHit.point.y + wallCheckHeightOffset, center.z);

                foreach (var (dir, dr, dc) in CardinalWithDelta)
                {
                    // Only probe toward non-floor neighbors — avoids hitting internal walls
                    // shared between two floor cells or walls far across the room.
                    if (CellHasFloor(floorCells, r + dr, c + dc)) continue;

                    if (!Physics.Raycast(checkOrigin, dir, out RaycastHit wallHit, wallCheckDistance, wallMask))
                        continue;

                    // Wall must be within cellSize of the probe (cell center to wall boundary
                    // is cellSize*0.5; this gives one extra cell of margin for thick walls).
                    if (wallHit.distance > cellSize) continue;

                    // Reject angled / perpendicular hits (e.g. through a gap).
                    if (Vector3.Dot(-wallHit.normal, dir) < wallNormalTolerance) continue;

                    // Reject walls that have no floor below them (e.g. outer boundary walls
                    // above lava or voids) — only keep walls that border walkable floor.
                    Vector3 aboveHit = new(wallHit.point.x, checkOrigin.y + floorProps.raycastOriginHeight, wallHit.point.z);
                    if (!Physics.Raycast(aboveHit, Vector3.down, floorProps.raycastDistance + floorProps.raycastOriginHeight, floorMask))
                        continue;

                    // Rotation snapped to the cardinal probe direction, never the noisy mesh normal.
                    var rot = Quaternion.LookRotation(-dir, Vector3.up);
                    candidates.Add((wallHit.point, rot));
                }
            }

        Shuffle(candidates);

        int spawned = 0;
        foreach (var (pos, baseRot) in candidates)
        {
            if (spawned >= wallProps.maxCount) break;
            if (Random.value > wallProps.spawnChancePerPoint) continue;

            var entry = PickWeighted(wallProps.entries);
            if (entry?.prefab == null) continue;

            // Wall offset is rotation-relative: X = along wall, Y = up, Z = out from wall.
            var rot = baseRot * Quaternion.Euler(entry.rotationOffset);
            var finalPos = pos + baseRot * entry.positionOffset;

            spawnedObjects.Add(SpawnProp(entry.prefab, finalPos, rot));
            spawned++;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private GameObject SpawnProp(GameObject prefab, Vector3 pos, Quaternion rot)
        => Instantiate(prefab, pos, rot, propsParent);

    private static int ToLayerMask(LayerMask mask) => mask.value == 0 ? ~0 : mask.value;

    private static PropEntry PickWeighted(PropEntry[] entries)
    {
        if (entries == null || entries.Length == 0) return null;
        float total = 0f;
        foreach (var e in entries) total += e.weight;
        if (total <= 0f) return entries[Random.Range(0, entries.Length)];
        float roll = Random.value * total, acc = 0f;
        foreach (var e in entries)
        {
            acc += e.weight;
            if (roll <= acc) return e;
        }
        return entries[^1];
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Transform root = Application.isPlaying ? activeLayoutRoot : previewLayoutRoot != null ? previewLayoutRoot : transform.parent;
        activeLayoutRoot = root;
        if (!ResolveGrid(root)) return;

        bool[,] floorCells = floorProps.enabled ? DetectCells(floorProps.detectionLayer) : null;
        bool[,] ceilCells  = (drawCeilPoints && ceilProps.enabled) ? DetectCells(ceilProps.detectionLayer) : null;

        // Valid floor cells (filled squares) + their border points.
        if (floorCells != null)
        {
            for (int r = 0; r < gridRows; r++)
                for (int c = 0; c < gridCols; c++)
                {
                    if (!floorCells[r, c]) continue;
                    Vector3 center = CellCenter(r, c);

                    if (drawValidCells)
                    {
                        Gizmos.color = new Color(0f, 1f, 0.4f, 0.08f);
                        Gizmos.DrawCube(center, new Vector3(cellSize, 0.02f, cellSize));
                        Gizmos.color = new Color(0f, 1f, 0.4f, 0.5f);
                        Gizmos.DrawWireCube(center, new Vector3(cellSize, 0.02f, cellSize));
                    }
                }

            // Valid floor points (near a wall, with clearance applied) — red, like the reference.
            if (drawFloorPoints)
            {
                Gizmos.color = Color.red;
                foreach (var pos in CollectFloorPoints(floorCells))
                    Gizmos.DrawSphere(pos, gizmoPointRadius);
            }
        }

        // Ceiling centers (cyan).
        if (ceilCells != null)
        {
            Gizmos.color = Color.cyan;
            for (int r = 0; r < gridRows; r++)
                for (int c = 0; c < gridCols; c++)
                    if (ceilCells[r, c]) Gizmos.DrawSphere(CellCenter(r, c), gizmoPointRadius);
        }

        // Wall probes (yellow rays from accepted floor cells, magenta arrow = facing).
        if (drawWallProbes && floorCells != null)
        {
            int floorMask = ToLayerMask(floorProps.detectionLayer);
            int wallMask = ToLayerMask(wallProps.detectionLayer);

            for (int r = 0; r < gridRows; r++)
                for (int c = 0; c < gridCols; c++)
                {
                    if (!floorCells[r, c]) continue;
                    Vector3 center = CellCenter(r, c);
                    Vector3 floorOrigin = center + Vector3.up * floorProps.raycastOriginHeight;
                    if (!Physics.Raycast(floorOrigin, Vector3.down, out RaycastHit floorHit, floorProps.raycastDistance + floorProps.raycastOriginHeight, floorMask))
                        continue;

                    Vector3 checkOrigin = new(center.x, floorHit.point.y + wallCheckHeightOffset, center.z);
                    foreach (var (dir, dr, dc) in CardinalWithDelta)
                    {
                        if (CellHasFloor(floorCells, r + dr, c + dc)) continue;
                        if (!Physics.Raycast(checkOrigin, dir, out RaycastHit wallHit, wallCheckDistance, wallMask))
                            continue;
                        if (wallHit.distance > cellSize) continue;
                        if (Vector3.Dot(-wallHit.normal, dir) < wallNormalTolerance) continue;
                        Vector3 aboveHit = new(wallHit.point.x, checkOrigin.y + floorProps.raycastOriginHeight, wallHit.point.z);
                        if (!Physics.Raycast(aboveHit, Vector3.down, floorProps.raycastDistance + floorProps.raycastOriginHeight, floorMask))
                            continue;

                        Gizmos.color = Color.yellow;
                        Gizmos.DrawLine(checkOrigin, wallHit.point);
                        Gizmos.color = Color.magenta;
                        Gizmos.DrawSphere(wallHit.point, gizmoPointRadius);
                        Gizmos.DrawLine(wallHit.point, wallHit.point - dir * 0.4f); // facing into room
                    }
                }
        }
    }
}
