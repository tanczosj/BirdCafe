using System;
using System.Collections.Generic;
using UnityEngine;

public class BeanLockOn : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerRoot;
    [SerializeField] private Camera playerCamera;

    [Header("Search")]
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private LayerMask obstructionMask;
    [SerializeField] private float searchRadius = 25f;
    [SerializeField] private float maxLockDistance = 22f;
    [SerializeField] private float breakDistance = 30f;
    [SerializeField] private float maxViewAngle = 75f;
    [SerializeField] private float viewportPadding = 0.08f;
    [SerializeField] private float keepLostTargetGrace = 0.75f;

    [Header("Scoring")]
    [SerializeField] private float distanceWeight = 0.20f;
    [SerializeField] private float angleWeight = 0.40f;
    [SerializeField] private float screenCenterWeight = 0.40f;

    public EnemyTarget CurrentTarget { get; private set; }
    public bool IsLockedOn => CurrentTarget != null;

    public event Action<EnemyTarget> OnTargetChanged;

    private readonly Collider[] overlapBuffer = new Collider[64];
    private float lostTargetTimer;

    private void Awake()
    {
        if (playerRoot == null)
            playerRoot = transform;

        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Update()
    {
        if (CurrentTarget == null)
            return;

        MaintainCurrentTarget();
    }

    public void ToggleLockOn()
    {
        if (CurrentTarget != null)
        {
            ClearTarget();
            return;
        }

        TryAcquireBestTarget();
    }

    public void ClearTarget()
    {
        CurrentTarget = null;
        lostTargetTimer = 0f;
        OnTargetChanged?.Invoke(null);
    }

    public void CycleTarget(int direction)
    {
        if (playerCamera == null)
            return;

        if (CurrentTarget == null)
        {
            TryAcquireBestTarget();
            return;
        }

        List<EnemyTarget> candidates = GatherValidTargets(false);
        if (candidates.Count == 0)
            return;

        Vector3 currentViewport = playerCamera.WorldToViewportPoint(CurrentTarget.AimPosition);
        EnemyTarget best = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            EnemyTarget candidate = candidates[i];
            if (candidate == CurrentTarget)
                continue;

            Vector3 vp = playerCamera.WorldToViewportPoint(candidate.AimPosition);
            float deltaX = vp.x - currentViewport.x;

            if (direction < 0 && deltaX >= -0.01f)
                continue;

            if (direction > 0 && deltaX <= 0.01f)
                continue;

            float horizontalCost = Mathf.Abs(deltaX);
            float verticalCost = Mathf.Abs(vp.y - currentViewport.y) * 0.35f;
            float depthCost = Mathf.Abs(vp.z - currentViewport.z) * 0.01f;
            float totalCost = horizontalCost + verticalCost + depthCost;

            if (totalCost < bestScore)
            {
                bestScore = totalCost;
                best = candidate;
            }
        }

        if (best != null)
            SetTarget(best);
    }

    public bool TryAcquireBestTarget()
    {
        List<EnemyTarget> candidates = GatherValidTargets(false);
        if (candidates.Count == 0)
        {
            ClearTarget();
            return false;
        }

        EnemyTarget best = null;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < candidates.Count; i++)
        {
            EnemyTarget candidate = candidates[i];
            float score = ScoreTarget(candidate);
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        if (best == null)
        {
            ClearTarget();
            return false;
        }

        SetTarget(best);
        return true;
    }

    private void SetTarget(EnemyTarget target)
    {
        CurrentTarget = target;
        lostTargetTimer = 0f;
        OnTargetChanged?.Invoke(CurrentTarget);
    }

    private void MaintainCurrentTarget()
    {
        if (CurrentTarget == null)
            return;

        if (CurrentTarget.IsDead)
        {
            TryAcquireBestTarget();
            return;
        }

        float distance = Vector3.Distance(playerRoot.position, CurrentTarget.WorldCenter);
        if (distance > breakDistance)
        {
            ClearTarget();
            return;
        }

        bool valid = PassesFilters(CurrentTarget, true);
        if (valid)
        {
            lostTargetTimer = 0f;
            return;
        }

        lostTargetTimer += Time.deltaTime;
        if (lostTargetTimer >= keepLostTargetGrace)
        {
            if (!TryAcquireBestTarget())
                ClearTarget();
        }
    }

    private List<EnemyTarget> GatherValidTargets(bool relaxedCurrentTargetRules)
    {
        List<EnemyTarget> results = new List<EnemyTarget>();
        int count = Physics.OverlapSphereNonAlloc(playerRoot.position, searchRadius, overlapBuffer, targetMask, QueryTriggerInteraction.Ignore);

        HashSet<EnemyTarget> unique = new HashSet<EnemyTarget>();

        for (int i = 0; i < count; i++)
        {
            Collider col = overlapBuffer[i];
            if (col == null)
                continue;

            EnemyTarget target = col.GetComponentInParent<EnemyTarget>();
            if (target == null)
                continue;

            if (unique.Contains(target))
                continue;

            if (!PassesFilters(target, relaxedCurrentTargetRules && target == CurrentTarget))
                continue;

            unique.Add(target);
            results.Add(target);
        }

        return results;
    }

    private bool PassesFilters(EnemyTarget target, bool relaxedForCurrentTarget)
    {
        if (target == null || target.IsDead)
            return false;

        Vector3 toTarget = target.AimPosition - playerRoot.position;
        float distance = toTarget.magnitude;
        if (distance > maxLockDistance && !relaxedForCurrentTarget)
            return false;

        Vector3 camToTarget = target.AimPosition - playerCamera.transform.position;
        float viewAngle = Vector3.Angle(playerCamera.transform.forward, camToTarget);
        if (viewAngle > maxViewAngle && !relaxedForCurrentTarget)
            return false;

        Vector3 viewport = playerCamera.WorldToViewportPoint(target.AimPosition);
        bool onScreen = viewport.z > 0f &&
                        viewport.x >= viewportPadding && viewport.x <= 1f - viewportPadding &&
                        viewport.y >= viewportPadding && viewport.y <= 1f - viewportPadding;

        if (!onScreen && !relaxedForCurrentTarget)
            return false;

        if (IsOccluded(target) && !relaxedForCurrentTarget)
            return false;

        return true;
    }

    private bool IsOccluded(EnemyTarget target)
    {
        Vector3 origin = playerCamera.transform.position;
        Vector3 end = target.AimPosition;
        Vector3 dir = end - origin;
        float distance = dir.magnitude;

        if (distance <= 0.01f)
            return false;

        dir /= distance;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, distance, obstructionMask, QueryTriggerInteraction.Ignore))
        {
            if (!hit.transform.IsChildOf(target.transform))
                return true;
        }

        return false;
    }

    private float ScoreTarget(EnemyTarget target)
    {
        Vector3 camToTarget = target.AimPosition - playerCamera.transform.position;
        float distance = Vector3.Distance(playerRoot.position, target.WorldCenter);
        float angle = Vector3.Angle(playerCamera.transform.forward, camToTarget);
        Vector3 vp = playerCamera.WorldToViewportPoint(target.AimPosition);

        float distanceScore = 1f - Mathf.Clamp01(distance / maxLockDistance);
        float angleScore = 1f - Mathf.Clamp01(angle / maxViewAngle);
        float centerDistance = Vector2.Distance(new Vector2(vp.x, vp.y), new Vector2(0.5f, 0.5f));
        float centerScore = 1f - Mathf.Clamp01(centerDistance / 0.7071f);

        float score =
            distanceScore * distanceWeight +
            angleScore * angleWeight +
            centerScore * screenCenterWeight +
            target.ScoreBias;

        return score;
    }
}