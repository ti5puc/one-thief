using System.Collections.Generic;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpikeTrap : TrapBase
{
    [Header("Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float delayToHit = 0.7f;

    [Header("References")]
    [SerializeField] private TriggerEventSender actionTrigger;
    [SerializeField] private TriggerEventSender hitTrigger;

    [Header("Debug")]
    [SerializeField, ReadOnly] private List<SpikeTrapPart> spikeTrapPart = new();

    private void Awake()
    {
        spikeTrapPart.AddRange(GetComponentsInChildren<SpikeTrapPart>());

        actionTrigger.gameObject.SetActive(true);
        hitTrigger.gameObject.SetActive(false);

        actionTrigger.OnEnter += OnActionTriggerEnter;
        hitTrigger.OnEnter += OnHitTriggerEnter;
    }

    private void OnDestroy()
    {
        actionTrigger.OnEnter -= OnActionTriggerEnter;
        hitTrigger.OnEnter -= OnHitTriggerEnter;
    }

    private void OnActionTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) == false) return;

        actionTrigger.gameObject.SetActive(false);

        // activate trap with delay to hit
        foreach (var part in spikeTrapPart)
            part.Activate(delayToHit);

        DOVirtual.DelayedCall(delayToHit, () =>
        {
            hitTrigger.gameObject.SetActive(true);
        });

        Debug.Log("Player activated spike trap");
    }

    private void OnHitTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) == false) return;

        Debug.Log("Player hit by spike trap");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
