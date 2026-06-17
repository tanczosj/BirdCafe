using UnityEngine;

public class EnemyTarget : MonoBehaviour
{
    [SerializeField] private Transform targetRoot;
    [SerializeField] private Transform aimPoint;
    [SerializeField] private float scoreBias;
    [SerializeField] private Health health;

    [Header("Optional Size Sources")]
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private Collider[] colliders;
    [SerializeField] private float fallbackSize = 1.5f;

    public Transform TargetRoot => targetRoot != null ? targetRoot : transform;
    public Vector3 WorldCenter => GetCombinedBounds(out Bounds b) ? b.center : TargetRoot.position;
    public Vector3 AimPosition => aimPoint != null ? aimPoint.position : WorldCenter + Vector3.up * 0.25f;
    public float ScoreBias => scoreBias;
    public bool IsDead => health != null && health.IsDead;

    // Largest dimension of the enemy in world space.
    public float EstimatedSize
    {
        get
        {
            if (!GetCombinedBounds(out Bounds b))
                return fallbackSize;

            Vector3 size = b.size;
            return Mathf.Max(size.x, size.y, size.z);
        }
    }

    private void Awake()
    {
        CacheBoundsSources();
    }

    private void Reset()
    {
        targetRoot = transform;
        health = GetComponentInParent<Health>();
        CacheBoundsSources();
    }

    private void CacheBoundsSources()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>();

        if (colliders == null || colliders.Length == 0)
            colliders = GetComponentsInChildren<Collider>();
    }

    private bool GetCombinedBounds(out Bounds combined)
    {
        combined = default;
        bool hasBounds = false;

        if (renderers != null)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || !r.enabled)
                    continue;

                if (!hasBounds)
                {
                    combined = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    combined.Encapsulate(r.bounds);
                }
            }
        }

        if (hasBounds)
            return true;

        if (colliders != null)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider c = colliders[i];
                if (c == null || !c.enabled || !c.gameObject.activeInHierarchy)
                    continue;

                if (!hasBounds)
                {
                    combined = c.bounds;
                    hasBounds = true;
                }
                else
                {
                    combined.Encapsulate(c.bounds);
                }
            }
        }

        return hasBounds;
    }
}