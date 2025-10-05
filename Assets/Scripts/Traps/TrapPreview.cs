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

    [Header("Materials")]
    [SerializeField] private List<RenderersByMaterial> validMaterialsList = new();
    [SerializeField] private List<RenderersByMaterial> invalidMaterialsList = new();

    [Header("Colliders to disable when placing")] // they need to stay on for overlap checks
    [SerializeField] private List<Collider> colliders;

    [Header("References")]
    [SerializeField] private GameObject notAllowedPointer;
    [SerializeField] private GameObject groundPreviewObject;

    public List<Collider> Colliders => colliders;

    private void Awake()
    {
        notAllowedPointer?.SetActive(false);
        if (groundPreviewObject != null)
            groundPreviewObject.transform.localPosition = Vector3.zero;

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
