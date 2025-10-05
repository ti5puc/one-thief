using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = nameof(TrapSettings), menuName = "Traps/" + nameof(TrapSettings))]
public class TrapSettings : ScriptableObject
{
    [Header("General Settings")]
    [SerializeField] private string trapName;
    [InfoBox("To set this ID go to One Thief > Trap Settings > Assign Unique IDs")]
    [SerializeField, ReadOnly] private int id;

    [Space(10)]
    [SerializeField] private bool isAlwaysActive = false;
    [SerializeField, HideIf(nameof(isAlwaysActive))] private float delayToHitAfterActivated = 0.7f;

    [Space(10)]
    [SerializeField, HideIf(nameof(isAlwaysActive))] private bool keepHitTriggerAfterActivated = false;
    [SerializeField, HideIf(EConditionOperator.Or, nameof(isAlwaysActive), nameof(keepHitTriggerAfterActivated))] private float hitTriggerActiveTime = .2f;

    [Space(10)]
    [SerializeField, HideIf(nameof(isAlwaysActive))] private bool canReactive;
    [SerializeField, ShowIf(nameof(canReactive))] private float delayBeforeReactivate = 4f;

    [Header("Layer Settings")]
    [SerializeField] private LayerMask trapPlacementLayer;
    [SerializeField] private LayerMask ignorePlacementLayer;
    [SerializeField] private LayerMask trapSurface;

    [Header("Preview & Object")]
    [SerializeField, ShowAssetPreview(128)] private GameObject trapPreview;
    [SerializeField, ShowAssetPreview(128)] private GameObject trapObject;

    [Header("Spacer Settings")]
    [SerializeField] private LayerMask invalidSurfacesForSpacer;
    [SerializeField, ShowAssetPreview(128)] private GameObject trapSpacerPreview;

    [Header("Positioning Settings")]
    [SerializeField, HideInInspector] private TrapPositioningMatrix2D positioningMatrix = new TrapPositioningMatrix2D(3, 3);

    public string TrapName => trapName;
    public bool IsAlwaysActive => isAlwaysActive;
    public float DelayToHit => delayToHitAfterActivated;
    public bool KeepHitTrigger => keepHitTriggerAfterActivated;
    public float HitTriggerActiveTime => hitTriggerActiveTime;
    public bool CanReactive => canReactive;
    public float DelayBeforeReactivate => delayBeforeReactivate;
    public LayerMask TrapPlacementLayer => trapPlacementLayer;
    public LayerMask IgnorePlacementLayer => ignorePlacementLayer;
    public LayerMask TrapSurface => trapSurface;
    public LayerMask InvalidSurfacesForSpacer => invalidSurfacesForSpacer;
    public GameObject TrapSpacerPreview => trapSpacerPreview;
    public GameObject TrapPreview => trapPreview;
    public GameObject TrapObject => trapObject;
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
