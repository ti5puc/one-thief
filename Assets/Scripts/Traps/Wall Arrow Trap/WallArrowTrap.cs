using System;
using System.Collections.Generic;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
// Add the correct namespace if ArrowTrapPart is in one (replace below if needed)
// using YourGameNamespace.Traps.WallArrowTrap;

public class WallArrowTrap : TrapBase
{
    [Serializable]
    public class ArrowSettings
    {
        public float ArrowSpeed = 15f;

        [Space(10)]
        public float ArrowAppearPosition = -2f;
        public float ArrowAppearDuration = 0.5f;
        public Ease ArrowAppearEase = Ease.OutBack;

        [Space(10)]
        public float ArrowDisappearDuration = .1f;
        public Ease ArrowDisappearEase = Ease.InBack;
        public float DelayToActivateArrow = .1f;
    }

    [Header("Arrow Trap Settings")]
    [SerializeField] private ArrowSettings arrowSettings;
    [SerializeField] private float minimumWaitTimeToShoot = 4f;

    [Header("Debug")]
    [SerializeField, ReadOnly] private List<ArrowTrapPart> arrowTrapParts = new();

    private Sequence arrowShootSequence;
    private int activeArrows = 0;
    private bool isAwaitingArrows = false;
    private float lastShotTime = -Mathf.Infinity;
    private Tween scheduledRestartTween;

    protected override void Awake()
    {
        base.Awake();

        PlayerSave.OnLevelLoaded += ResetArrows;
        
        arrowTrapParts.AddRange(GetComponentsInChildren<ArrowTrapPart>(true));
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        PlayerSave.OnLevelLoaded -= ResetArrows;
    }

    protected override void OnAlwaysActive()
    {
        if (arrowShootSequence == null)
        {
            arrowShootSequence = DOTween.Sequence();
            arrowShootSequence.AppendCallback(() =>
            {
                if (isAwaitingArrows) return;

                scheduledRestartTween?.Kill();
                scheduledRestartTween = null;

                lastShotTime = Time.time;

                activeArrows = arrowTrapParts.Count;
                isAwaitingArrows = true;
                foreach (var part in arrowTrapParts)
                    part.LaunchArrow(arrowSettings, OnArrowDisappeared);
            });

            arrowShootSequence.SetAutoKill(false);
        }

        if (!arrowShootSequence.IsPlaying() && !isAwaitingArrows)
        {
            if (scheduledRestartTween == null)
            {
                var elapsed = Time.time - lastShotTime;
                if (lastShotTime == -Mathf.Infinity || elapsed >= minimumWaitTimeToShoot)
                {
                    arrowShootSequence.Restart();
                }
                else
                {
                    var remaining = minimumWaitTimeToShoot - elapsed;
                    scheduledRestartTween?.Kill();
                    scheduledRestartTween = DOVirtual.DelayedCall(remaining, () =>
                    {
                        scheduledRestartTween = null;
                        arrowShootSequence.Restart();
                    }, false);
                }
            }
        }

        if (GameManager.CurrentGameState != GameState.Building)
            ResumeSequence();
        else
            PauseSequence();
    }

    protected override void Initialize()
    {
        base.Initialize();
        actionTrigger.gameObject.SetActive(false);
        hitTrigger.gameObject.SetActive(false);
    }

    protected override void OnHit(Collider player) => throw new NotImplementedException();
    protected override void OnAction(Collider player, float totalDuration) => throw new NotImplementedException();
    protected override void OnReactivate(float totalDuration) => throw new NotImplementedException();

    private void OnArrowDisappeared()
    {
        activeArrows--;
        if (activeArrows <= 0)
        {
            // all arrows disappeared, allow the sequence to launch again only if
            // the minimum wait time since the last shot has passed. Otherwise schedule a restart.
            isAwaitingArrows = false;

            var elapsed = Time.time - lastShotTime;
            if (lastShotTime == -Mathf.Infinity || elapsed >= minimumWaitTimeToShoot)
            {
                // enough time passed, restart immediately
                scheduledRestartTween?.Kill();
                scheduledRestartTween = null;
                arrowShootSequence.Restart();
            }
            else
            {
                // schedule a restart after the remaining time
                var remaining = minimumWaitTimeToShoot - elapsed;
                scheduledRestartTween?.Kill();
                scheduledRestartTween = DOVirtual.DelayedCall(remaining, () =>
                {
                    scheduledRestartTween = null;
                    arrowShootSequence.Restart();
                }, false);
            }
        }
    }

    private void ResumeSequence()
    {
        arrowShootSequence?.Play();
        scheduledRestartTween?.Play();
        foreach (var part in arrowTrapParts)
            part.ResumeMovement();
    }

    private void PauseSequence()
    {
        arrowShootSequence?.Pause();
        scheduledRestartTween?.Pause();
        foreach (var part in arrowTrapParts)
            part.PauseMovement();
    }
    
    private void ResetArrows()
    {
        // Kill all active sequences and tweens
        arrowShootSequence?.Kill();
        arrowShootSequence = null;
        
        scheduledRestartTween?.Kill();
        scheduledRestartTween = null;
        
        // Reset tracking variables
        activeArrows = 0;
        isAwaitingArrows = false;
        lastShotTime = -Mathf.Infinity;
        
        // Reset all arrow trap parts to their initial state
        foreach (var part in arrowTrapParts)
        {
            part.ResetToInitialState();
        }
    }
}
