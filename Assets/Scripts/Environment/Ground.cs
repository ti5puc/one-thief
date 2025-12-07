using System;
using NaughtyAttributes;
using UnityEngine;

public class Ground : MonoBehaviour
{
    [SerializeField, ReadOnly] private Vector3 cachedPosition;
    [SerializeField, ReadOnly] private Quaternion cachedRotation;

    private void Awake()
    {
        cachedPosition = transform.position;
        cachedRotation = transform.rotation;
    }
    
    public void ResetTransform()
    {
        transform.position = cachedPosition;
        transform.rotation = cachedRotation;
    }
}
