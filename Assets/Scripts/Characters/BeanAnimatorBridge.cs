using UnityEngine;

public class BeanAnimatorBridge : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private BeanPlayerController controller;
    [SerializeField] private BeanCombat combat;

    [Header("Damping")]
    [SerializeField] private float moveDamp = 0.07f;
    [SerializeField] private float turnDamp = 0.08f;

    [Header("Locomotion Playback")]
    [SerializeField] private float idlePlaybackSpeed = 1.0f;
    [SerializeField] private float walkPlaybackSpeed = 0.9f;
    [SerializeField] private float runPlaybackSpeed = 1.0f;
    [SerializeField] private float sprintPlaybackSpeed = 1.18f;
    [SerializeField] private float playbackSpeedSmooth = 10f;

    private float currentPlaybackSpeed = 1f;

    private void OnEnable()
    {
        if (controller != null)
            controller.OnDodgeStarted += HandleDodgeStarted;
    }

    private void OnDisable()
    {
        if (controller != null)
            controller.OnDodgeStarted -= HandleDodgeStarted;
    }

    private void Update()
    {
        if (animator == null || controller == null)
            return;

        animator.SetBool("Grounded", controller.IsGrounded);
        animator.SetBool("Sprinting", controller.IsSprinting);
        animator.SetBool("LockedOn", controller.IsLockedOn);
        animator.SetBool("Dodging", controller.IsDodging);
        animator.SetBool("Attacking", combat != null && combat.IsAttacking);

        animator.SetFloat("VerticalVelocity", controller.VerticalVelocity);
        animator.SetFloat("MoveX", controller.AnimationMoveX, moveDamp, Time.deltaTime);
        animator.SetFloat("MoveY", controller.AnimationMoveY, moveDamp, Time.deltaTime);
        animator.SetFloat("MoveAmount", controller.AnimationMoveAmount, moveDamp, Time.deltaTime);
        animator.SetFloat("TurnAmount", controller.TurnAmount, turnDamp, Time.deltaTime);

        float targetPlaybackSpeed = GetTargetLocomotionPlaybackSpeed();

        currentPlaybackSpeed = Mathf.Lerp(
            currentPlaybackSpeed,
            targetPlaybackSpeed,
            1f - Mathf.Exp(-playbackSpeedSmooth * Time.deltaTime));

        animator.SetFloat("LocomotionPlaybackSpeed", currentPlaybackSpeed);
    }

    private float GetTargetLocomotionPlaybackSpeed()
    {
        if (!controller.IsGrounded || controller.IsDodging || (combat != null && combat.IsAttacking))
            return 1f;

        if (controller.MoveAmount < 0.05f)
            return idlePlaybackSpeed;

        if (controller.IsSprinting)
            return sprintPlaybackSpeed;

        if (controller.IsLockedOn)
            return runPlaybackSpeed;

        if (controller.MoveAmount < 0.55f)
            return walkPlaybackSpeed;

        return runPlaybackSpeed;
    }

    private void HandleDodgeStarted()
    {
        if (animator == null)
            return;

        animator.ResetTrigger("DodgeTrigger");
        animator.SetTrigger("DodgeTrigger");
    }
}