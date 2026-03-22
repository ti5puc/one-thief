using System;
using System.Collections.Generic;
using UnityEngine;

public class TriggerEventSender : MonoBehaviour
{
    public event Action<Collider> OnEnter;
    public event Action<Collider> OnStay;
    public event Action<Collider> OnExit;

    private List<Collider> selfColliders;
    public List<Collider> Colliders => selfColliders ??= new List<Collider>(GetComponents<Collider>());

    private void OnTriggerEnter(Collider other)
    {
        OnEnter?.Invoke(other);
    }

    private void OnTriggerStay(Collider other)
    {
        OnStay?.Invoke(other);
    }

    private void OnTriggerExit(Collider other)
    {
        OnExit?.Invoke(other);
    }
}
