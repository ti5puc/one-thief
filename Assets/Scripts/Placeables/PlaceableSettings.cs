using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = nameof(PlaceableSettings), menuName = "OneThief/Placeables/" + nameof(PlaceableSettings))]
public class PlaceableSettings : ScriptableObject
{
    [Header("General Settings")]
    [SerializeField] protected string trapName;
    [InfoBox("To set this ID go to One Thief > Trap Settings > Assign Unique IDs")]
    [SerializeField, ReadOnly] protected int id;

    [Space(10)]
    [SerializeField] protected int placementCost = 0;
    
    [Header("Layer Settings")]
    [SerializeField] protected LayerMask trapPlacementLayer;
    [SerializeField] protected LayerMask ignorePlacementLayer;
    [SerializeField] protected LayerMask trapSurface;

    [Space(10)]
    [SerializeField] protected bool needsWallToPlace = false;
    [SerializeField, ShowIf(nameof(needsWallToPlace))] protected LayerMask wallLayer;

    [Header("Preview & Object")]
    [SerializeField, ShowAssetPreview(128)] protected GameObject trapPreview;
    [SerializeField, ShowAssetPreview(128)] protected GameObject trapObject;

    [Header("Spacer Settings")]
    [SerializeField] protected LayerMask invalidSurfacesForSpacer;
    [SerializeField, ShowAssetPreview(128)] protected GameObject trapSpacerPreview;

    [Header("Positioning Settings")]
    [SerializeField, HideInInspector] protected TrapPositioningMatrix2D positioningMatrix = new TrapPositioningMatrix2D(3, 3);

    public string TrapName => trapName;
    public LayerMask TrapPlacementLayer => trapPlacementLayer;
    public LayerMask IgnorePlacementLayer => ignorePlacementLayer;
    public LayerMask TrapSurface => trapSurface;
    public LayerMask InvalidSurfacesForSpacer => invalidSurfacesForSpacer;
    public GameObject TrapSpacerPreview => trapSpacerPreview;
    public GameObject TrapPreview => trapPreview;
    public GameObject TrapObject => trapObject;
    public bool NeedsWallToPlace => needsWallToPlace;
    public LayerMask WallLayer => wallLayer;
    public int PlacementCost => placementCost;
    public TrapPositioningMatrix2D PositioningMatrix
    {
        get => positioningMatrix;
        set => positioningMatrix = value;
    }
    public int ID
    {
        get => id;
        set => id = value;
    }
}

public enum TrapPositioningType { None, Trap, Spacer }

[System.Serializable]
public class TrapPositioningMatrix2D
{
    [SerializeField] private int rows = 3;
    [SerializeField] private int cols = 3;
    [SerializeField] private TrapPositioningType[] data = new TrapPositioningType[9];

    public int Rows => rows;
    public int Cols => cols;

    public TrapPositioningType this[int row, int col]
    {
        get => data[row * cols + col];
        set => data[row * cols + col] = value;
    }

    public TrapPositioningMatrix2D(int rows = 3, int cols = 3)
    {
        this.rows = rows;
        this.cols = cols;
        data = new TrapPositioningType[rows * cols];
    }

    public void Resize(int newRows, int newCols)
    {
        var newData = new TrapPositioningType[newRows * newCols];
        for (int i = 0; i < newRows; i++)
            for (int j = 0; j < newCols; j++)
                if (i < rows && j < cols)
                    newData[i * newCols + j] = this[i, j];
                else
                    newData[i * newCols + j] = TrapPositioningType.None;

        rows = newRows;
        cols = newCols;
        data = newData;
    }
}
