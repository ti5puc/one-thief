using System;
using System.Collections.Generic;
using UnityEngine;

public class TrapPreview : MonoBehaviour
{
    [Serializable]
    public struct RenderersByMaterial
    {
        public Material material;
        public Renderer[] renderers;
    }

    [SerializeField] private List<RenderersByMaterial> validMaterialsList = new();
    [SerializeField] private List<RenderersByMaterial> invalidMaterialsList = new();
    [SerializeField] private GameObject notAllowedPointer;
    [SerializeField] private List<Collider> colliders;

    public List<Collider> Colliders => colliders;

    private void Awake()
    {
        notAllowedPointer?.SetActive(false);
        foreach (var col in colliders)
        {
            if (col != null)
                col.isTrigger = true;
        }
    }

    public void SetValid()
    {
        SetMaterial(validMaterialsList);
        notAllowedPointer?.SetActive(false);
    }

    public void SetInvalid()
    {
        SetMaterial(invalidMaterialsList);
        notAllowedPointer?.SetActive(true);
    }

    private void SetMaterial(List<RenderersByMaterial> materialsList)
    {
        foreach (var item in materialsList)
        {
            if (item.material == null || item.renderers == null) continue;

            foreach (var renderer in item.renderers)
            {
                if (renderer == null) continue;
                renderer.material = item.material;
            }
        }
    }
}
