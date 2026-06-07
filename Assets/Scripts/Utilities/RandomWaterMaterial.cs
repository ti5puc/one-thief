using NaughtyAttributes;
using UnityEngine;

public class RandomWaterMaterial : MonoBehaviour
{
    [SerializeField] private MeshRenderer meshRenderer;

    private void Awake()
    {
        GameManager.OnWaterChosen += UpdateWaterMaterial;
    }

    private void OnDestroy()
    {
        GameManager.OnWaterChosen -= UpdateWaterMaterial;
    }

    private void Start()
    {
        UpdateWaterMaterial();
    }

    private void UpdateWaterMaterial()
    {
        meshRenderer.material = GameManager.RandomWaterChoice;
    }
}
