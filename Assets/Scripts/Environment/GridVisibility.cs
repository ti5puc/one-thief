using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class GridVisibility : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Material gridMaterial;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float raycastHeight = 5f;

    private bool? lastTrapModeActive = null;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        if (gridMaterial != null)
            meshRenderer.sharedMaterial = gridMaterial;
    }

    private void Start()
    {
        BuildValidCellsMesh();
    }

    private void Update()
    {
        if (player == null) return;

        bool current = player.IsTrapModeActive || player.IsTrapMenuActive;
        if (lastTrapModeActive == null || lastTrapModeActive != current)
        {
            meshRenderer.enabled = current;
            lastTrapModeActive = current;
        }
    }

    public void BuildValidCellsMesh()
    {
        if (player == null) return;

        int rows = player.gridRows;
        int cols = player.gridCols;
        float cellSize = player.gridSize;
        Vector2 offset = player.gridOffset;
        int centerRow = rows / 2;
        int centerCol = cols / 2;

        // Se groundLayer for 0 (Nothing), usa Everything para não gerar malha vazia silenciosamente
        int layerMask = groundLayer.value == 0 ? ~0 : groundLayer.value;
        if (groundLayer.value == 0)
            Debug.LogWarning("[GridVisibility] groundLayer não configurada — usando todas as layers. Configure a layer correta no Inspector.", this);

        int validCount = 0;
        var validCells = new bool[rows, cols];
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                float wx = (c - centerCol) * cellSize + offset.x;
                float wz = (r - centerRow) * cellSize + offset.y;
                var origin = new Vector3(wx, transform.position.y + raycastHeight, wz);
                validCells[r, c] = Physics.Raycast(origin, Vector3.down, raycastHeight * 2f, layerMask);
                if (validCells[r, c]) validCount++;
            }
        }

        Debug.Log($"[GridVisibility] {validCount}/{rows * cols} células válidas detectadas.");
        meshFilter.mesh = BuildMesh(validCells, rows, cols, cellSize, offset, centerRow, centerCol);
    }

    private Mesh BuildMesh(bool[,] validCells, int rows, int cols, float cellSize, Vector2 offset, int centerRow, int centerCol)
    {
        var verts = new List<Vector3>();
        var tris = new List<int>();
        float half = cellSize * 0.5f;
        float ly = 0f;
        Vector3 worldOrigin = transform.position;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (!validCells[r, c]) continue;

                // posição local relativa ao transform do objeto
                float lx = (c - centerCol) * cellSize + offset.x - worldOrigin.x;
                float lz = (r - centerRow) * cellSize + offset.y - worldOrigin.z;

                int i = verts.Count;
                verts.Add(new Vector3(lx - half, ly, lz - half));
                verts.Add(new Vector3(lx + half, ly, lz - half));
                verts.Add(new Vector3(lx + half, ly, lz + half));
                verts.Add(new Vector3(lx - half, ly, lz + half));

                tris.Add(i);     tris.Add(i + 2); tris.Add(i + 1);
                tris.Add(i);     tris.Add(i + 3); tris.Add(i + 2);
            }
        }

        var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
