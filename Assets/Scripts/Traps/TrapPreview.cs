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

    public void SetValid()
    {
        SetMaterial(validMaterialsList);
    }

    public void SetInvalid()
    {
        SetMaterial(invalidMaterialsList);
    }

    private void SetMaterial(List<RenderersByMaterial> materialsList)
    {
        foreach (var item in materialsList)
        {
            foreach (var renderer in item.renderers)
            {
                renderer.material = item.material;
            }
        }
    }
}
