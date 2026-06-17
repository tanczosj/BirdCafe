using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeanCombat : MonoBehaviour
{
    [System.Serializable]
    public class AttackStep
    {
        public string animationTrigger = "Attack1";
        public float staminaCost = 20f;
        public float damage = 20f;
        public float range = 1.4f;
        public float hitRadius = 0.65f;
        public float windup = 0.12f;
        public float activeTime = 0.08f;
        public float recovery = 0.28f;
        public float forwardLungeSpeed = 2.4f;
    }

    [Header("References")]
    [SerializeField] private BeanPlayerController controller;
    [SerializeField] private BeanLockOn lockOn;
    [SerializeField] private Stamina stamina;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform hitOrigin;

    [Header("Attacks")]
    [SerializeField] private AttackStep[] combo;
    [SerializeField] private LayerMask hitMask;
    [SerializeField] private float comboResetTime = 1f;
    [SerializeField] private float targetFacingSharpness = 20f;

    public bool IsAttacking { get; private set; }

    public event Action OnAttackStarted;

    private readonly Collider[] hitBuffer = new Collider[24];
    private bool queuedAttack;
    private float comboExpireTime;
    private int currentComboIndex;

    private void Update()
    {
        if (combo == null || combo.Length == 0)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            if (IsAttacking)
            {
                queuedAttack = true;
            }
            else
            {
                if (Time.time > comboExpireTime)
                    currentComboIndex = 0;

                StartCoroutine(AttackRoutine(currentComboIndex));
            }
        }

        if (!IsAttacking && Time.time > comboExpireTime)
            currentComboIndex = 0;
    }

    private IEnumerator AttackRoutine(int index)
    {
        if (index < 0 || index >= combo.Length)
            yield break;

        AttackStep step = combo[index];

        if (controller != null && controller.IsDodging)
            yield break;

        if (stamina != null && !stamina.Consume(step.staminaCost))
            yield break;

        IsAttacking = true;
        queuedAttack = false;
        comboExpireTime = Time.time + comboResetTime;

        if (controller != null)
            controller.SetInputSuppressed(true);

        if (animator != null && !string.IsNullOrWhiteSpace(step.animationTrigger))
            animator.SetTrigger(step.animationTrigger);

        OnAttackStarted?.Invoke();

        float elapsed = 0f;
        while (elapsed < step.windup)
        {
            elapsed += Time.deltaTime;
            FaceLockTarget();
            ApplyForwardLunge(step.forwardLungeSpeed);
            yield return null;
        }

        elapsed = 0f;
        bool hitApplied = false;
        while (elapsed < step.activeTime)
        {
            elapsed += Time.deltaTime;
            FaceLockTarget();
            ApplyForwardLunge(step.forwardLungeSpeed);

            if (!hitApplied)
            {
                PerformHit(step);
                hitApplied = true;
            }

            yield return null;
        }

        elapsed = 0f;
        while (elapsed < step.recovery)
        {
            elapsed += Time.deltaTime;
            FaceLockTarget();
            yield return null;
        }

        if (controller != null)
            controller.SetInputSuppressed(false);

        IsAttacking = false;

        if (queuedAttack && index + 1 < combo.Length)
        {
            currentComboIndex = index + 1;
            StartCoroutine(AttackRoutine(currentComboIndex));
        }
        else
        {
            currentComboIndex = (index + 1) % combo.Length;
        }
    }

    private void ApplyForwardLunge(float lungeSpeed)
    {
        if (controller == null || lungeSpeed <= 0f)
            return;

        controller.AddExternalDelta(transform.forward * lungeSpeed * Time.deltaTime);
    }

    private void FaceLockTarget()
    {
        if (lockOn == null || !lockOn.IsLockedOn || lockOn.CurrentTarget == null)
            return;

        Vector3 toTarget = lockOn.CurrentTarget.WorldCenter - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude <= 0.001f)
            return;

        Quaternion desired = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desired,
            1f - Mathf.Exp(-targetFacingSharpness * Time.deltaTime));
    }

    private void PerformHit(AttackStep step)
    {
        Transform source = hitOrigin != null ? hitOrigin : transform;
        Vector3 center = source.position + transform.forward * step.range;
        int hitCount = Physics.OverlapSphereNonAlloc(center, step.hitRadius, hitBuffer, hitMask, QueryTriggerInteraction.Ignore);

        HashSet<IDamageable> hitSet = new HashSet<IDamageable>();

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = hitBuffer[i];
            if (col == null)
                continue;

            if (col.transform.IsChildOf(transform))
                continue;

            if (!TryGetDamageable(col, out IDamageable damageable))
                continue;

            if (damageable == null || damageable.IsDead)
                continue;

            if (hitSet.Contains(damageable))
                continue;

            hitSet.Add(damageable);
            Vector3 hitPoint = col.ClosestPoint(center);
            Vector3 hitDirection = (col.transform.position - transform.position).normalized;
            damageable.TakeDamage(step.damage, hitPoint, hitDirection, gameObject);
        }
    }

    private bool TryGetDamageable(Collider col, out IDamageable damageable)
    {
        damageable = null;
        MonoBehaviour[] behaviours = col.GetComponentsInParent<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IDamageable found)
            {
                damageable = found;
                return true;
            }
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (combo == null || combo.Length == 0)
            return;

        Transform source = hitOrigin != null ? hitOrigin : transform;
        AttackStep step = combo[0];
        Vector3 center = source.position + transform.forward * step.range;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, step.hitRadius);
    }
}