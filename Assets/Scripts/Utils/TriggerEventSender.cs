using System;
using UnityEngine;

public class TriggerEventSender : MonoBehaviour
{
    public event Action<Collider> OnEnter;
    public event Action<Collider> OnStay;
    public event Action<Collider> OnExit;

    private Collider selfCollider;
    public Collider Collider => selfCollider ??= GetComponent<Collider>();

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
