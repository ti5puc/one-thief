using UnityEngine;

public class GridVisibility : MonoBehaviour
{
    [SerializeField] private Player player;

    private bool? lastTrapModeActive = null;
    private MeshRenderer meshRenderer;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    private void Update()
    {
        if (player == null) return;

        bool current = player.IsTrapModeActive;
        if (lastTrapModeActive == null || lastTrapModeActive != current)
        {
            meshRenderer.enabled = current;
            lastTrapModeActive = current;
        }
    }
}
