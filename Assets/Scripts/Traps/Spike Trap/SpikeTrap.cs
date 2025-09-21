using System;
using System.Collections.Generic;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpikeTrap : TrapBase
{
    [Header("Debug")]
    [SerializeField, ReadOnly] private List<SpikeTrapPart> spikeTrapPart = new();

    protected override void Awake()
    {
        base.Awake();

        spikeTrapPart.AddRange(GetComponentsInChildren<SpikeTrapPart>());
    }

    protected override void OnAction(Collider player, float totalDuration)
    {
        foreach (var part in spikeTrapPart)
            part.Activate(totalDuration);

        Debug.Log("Player activated spike trap");
    }

    protected override void OnHit(Collider player)
    {
        Debug.Log("Player hit by spike trap");
        var controller = player.GetComponent<PlayerDeathIdentifier>();
        controller.Death();
    }

    protected override void OnReactivate(float totalDuration)
    {
        foreach (var part in spikeTrapPart)
            part.Reactivate(totalDuration);

        Debug.Log("Spike trap reactivated");
    }
}
