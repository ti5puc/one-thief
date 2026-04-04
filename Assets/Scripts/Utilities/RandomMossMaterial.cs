using NaughtyAttributes;
using UnityEngine;

public class RandomMossMaterial : MonoBehaviour
{
    [SerializeField] private bool isWall;
    [SerializeField, ShowIf(nameof(isWall))] private Material[] wallMaterials;
    [SerializeField, HideIf(nameof(isWall))] private Material[] floorMaterials;

    [Space(10)]
    [SerializeField] private MeshRenderer meshRenderer;

    private void Awake()
    {
        if (isWall && wallMaterials.Length > 0)
        {
            meshRenderer.material = wallMaterials[Random.Range(0, wallMaterials.Length)];
        }
        else if (!isWall && floorMaterials.Length > 0)
        {
            meshRenderer.material = floorMaterials[Random.Range(0, floorMaterials.Length)];
        }
    }
}
