using NaughtyAttributes;
using UnityEngine;
using UnityEngine.XR;

public class PlayerVisual : MonoBehaviour
{
    [SerializeField] private Player player;

    [Space(10)]
    [SerializeField] private Animator animator;
    [SerializeField, AnimatorParam(nameof(animator))] private int isMovingParam;

    [Space(5)]
    [SerializeField] private float moveSpeedSlow = 1f;
    [SerializeField] private float moveSpeedFast = 2f;
    [SerializeField, AnimatorParam(nameof(animator))] private int moveSpeedParam;

    private bool lastStateIsMoving = false;
    
    private void Awake()
    {
        PauseMenuUI.OnPauseMenuToggled += HandlePauseMenuToggled;
        player.OnMoveChanged += HandleMoveChanged;
    }

    private void OnDestroy()
    {
        PauseMenuUI.OnPauseMenuToggled -= HandlePauseMenuToggled;
        player.OnMoveChanged -= HandleMoveChanged;
    }

    private void HandleMoveChanged(bool isMoving, bool isSprinting)
    {
        animator.SetBool(isMovingParam, isMoving);
        animator.SetFloat(moveSpeedParam, isSprinting ? moveSpeedFast : moveSpeedSlow);
        
        lastStateIsMoving = isMoving;
    }

    private void HandlePauseMenuToggled(bool isShowing)
    {
        if (isShowing)
            animator.SetBool(isMovingParam, false);
        else
            animator.SetBool(isMovingParam, lastStateIsMoving);
    }
}
