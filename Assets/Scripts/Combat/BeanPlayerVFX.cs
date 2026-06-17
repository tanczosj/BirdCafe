using System.Collections;
using UnityEngine;

public class BeanPlayerVFX : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BeanPlayerController controller;
    [SerializeField] private BeanCombat combat;

    [Header("Particles")]
    [SerializeField] private ParticleSystem moveDust;
    [SerializeField] private ParticleSystem dodgeBurst;
    [SerializeField] private ParticleSystem landBurst;
    [SerializeField] private ParticleSystem attackSwipe;

    [Header("Optional Dodge Trails")]
    [SerializeField] private TrailRenderer[] dodgeTrails;
    [SerializeField] private float dodgeTrailTime = 0.18f;

    [Header("Move Dust")]
    [SerializeField] private float moveDustMinMoveAmount = 0.15f;

    private Coroutine dodgeTrailRoutine;

    private void OnEnable()
    {
        if (controller != null)
        {
            controller.OnDodgeStarted += HandleDodgeStarted;
            controller.OnLanded += HandleLanded;
        }

        if (combat != null)
            combat.OnAttackStarted += HandleAttackStarted;
    }

    private void OnDisable()
    {
        if (controller != null)
        {
            controller.OnDodgeStarted -= HandleDodgeStarted;
            controller.OnLanded -= HandleLanded;
        }

        if (combat != null)
            combat.OnAttackStarted -= HandleAttackStarted;
    }

    private void Update()
    {
        UpdateMoveDust();
    }

    private void UpdateMoveDust()
    {
        if (moveDust == null || controller == null)
            return;

        bool shouldPlay =
            controller.IsGrounded &&
            !controller.IsDodging &&
            controller.MoveAmount > moveDustMinMoveAmount;

        if (shouldPlay)
        {
            if (!moveDust.isPlaying)
                moveDust.Play();
        }
        else
        {
            if (moveDust.isPlaying)
                moveDust.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void HandleDodgeStarted()
    {
        if (dodgeBurst != null)
            dodgeBurst.Play();

        if (dodgeTrails != null && dodgeTrails.Length > 0)
        {
            if (dodgeTrailRoutine != null)
                StopCoroutine(dodgeTrailRoutine);

            dodgeTrailRoutine = StartCoroutine(DodgeTrailRoutine());
        }
    }

    private void HandleLanded(float impact)
    {
        if (landBurst == null)
            return;

        int emitCount = Mathf.RoundToInt(Mathf.Lerp(8f, 20f, Mathf.Clamp01(impact / 18f)));
        landBurst.Emit(emitCount);
    }

    private void HandleAttackStarted()
    {
        if (attackSwipe != null)
            attackSwipe.Play();
    }

    private IEnumerator DodgeTrailRoutine()
    {
        for (int i = 0; i < dodgeTrails.Length; i++)
        {
            if (dodgeTrails[i] != null)
                dodgeTrails[i].emitting = true;
        }

        yield return new WaitForSeconds(dodgeTrailTime);

        for (int i = 0; i < dodgeTrails.Length; i++)
        {
            if (dodgeTrails[i] != null)
                dodgeTrails[i].emitting = false;
        }

        dodgeTrailRoutine = null;
    }
}