using UnityEngine;

public class FootstepDustEmitter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BeanPlayerController playerController;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform leftFoot;
    [SerializeField] private Transform rightFoot;
    [SerializeField] private ParticleSystem footstepPrefab;

    [Header("Grounding")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private float raycastHeight = 0.3f;
    [SerializeField] private float raycastDistance = 1.0f;
    [SerializeField] private float spawnOffset = 0.05f;

    [Header("Automatic Steps")]
    [SerializeField] private bool useAutomaticSteps = true;
    [SerializeField] private float minMoveAmount = 0.15f;
    [SerializeField] private float stepDistanceWalk = 1.5f;
    [SerializeField] private float stepDistanceRun = 2.1f;
    [SerializeField] private bool emitWhileSprinting = true;

    [Header("Chance To Emit")]
    [SerializeField][Range(0f, 1f)] private float emitChanceWalk = 0.35f;
    [SerializeField][Range(0f, 1f)] private float emitChanceRun = 0.50f;
    [SerializeField][Range(0f, 1f)] private float emitChanceSprint = 0.65f;

    [Header("Surface Color")]
    [SerializeField] private bool tintFromSurface = true;
    [SerializeField] private bool sampleTextureColor = false;
    [SerializeField] private Color fallbackParticleColor = new Color(0.62f, 0.47f, 0.36f, 1f);
    [SerializeField] private float colorBrightnessMultiplier = 0.95f;
    [SerializeField] private float minColorBrightness = 0.18f;
    [SerializeField] private float maxColorBrightness = 0.95f;

    [Header("Debug")]
    [SerializeField] private bool debugRays = false;
    [SerializeField] private bool debugStepLogs = false;

    private float distanceAccumulator;
    private Vector3 lastPosition;
    private bool hasLastPosition;
    private bool nextStepLeft = true;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (playerController == null)
            playerController = GetComponentInParent<BeanPlayerController>();

        if (animator != null && animator.isHuman)
        {
            if (leftFoot == null)
                leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);

            if (rightFoot == null)
                rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        }
    }

    private void Start()
    {
        if (playerController == null)
            Debug.LogWarning($"{nameof(FootstepDustEmitter)} on {name} could not find BeanPlayerController.");

        if (animator == null)
            Debug.LogWarning($"{nameof(FootstepDustEmitter)} on {name} could not find Animator.");

        if (leftFoot == null || rightFoot == null)
            Debug.LogWarning($"{nameof(FootstepDustEmitter)} on {name} is missing left/right foot references.");

        if (footstepPrefab == null)
            Debug.LogWarning($"{nameof(FootstepDustEmitter)} on {name} is missing a footstep particle prefab.");
    }

    private void Update()
    {
        if (!useAutomaticSteps || playerController == null || footstepPrefab == null)
            return;

        if (!playerController.IsGrounded || playerController.IsDodging)
        {
            ResetDistanceTracking();
            return;
        }

        if (playerController.MoveAmount < minMoveAmount)
        {
            ResetDistanceTracking();
            return;
        }

        if (playerController.IsSprinting && !emitWhileSprinting)
            return;

        Vector3 currentPosition = transform.position;

        if (!hasLastPosition)
        {
            lastPosition = currentPosition;
            hasLastPosition = true;
            return;
        }

        Vector3 delta = currentPosition - lastPosition;
        delta.y = 0f;

        distanceAccumulator += delta.magnitude;
        lastPosition = currentPosition;

        float stepDistance = playerController.IsSprinting ? stepDistanceRun : stepDistanceWalk;

        if (distanceAccumulator >= stepDistance)
        {
            distanceAccumulator = 0f;

            if (nextStepLeft)
                EmitLeftStep();
            else
                EmitRightStep();

            nextStepLeft = !nextStepLeft;
        }
    }

    private void ResetDistanceTracking()
    {
        distanceAccumulator = 0f;
        hasLastPosition = false;
    }

    // Animation event support
    public void EmitLeftStep()
    {
        TrySpawnAtFoot(leftFoot);
    }

    public void EmitRightStep()
    {
        TrySpawnAtFoot(rightFoot);
    }

    private void TrySpawnAtFoot(Transform foot)
    {
        if (footstepPrefab == null || foot == null || playerController == null)
            return;

        if (!ShouldEmitThisStep())
        {
            if (debugStepLogs)
                Debug.Log("Footstep FX skipped by chance.");
            return;
        }

        Vector3 origin = foot.position + Vector3.up * raycastHeight;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastDistance, groundLayers, QueryTriggerInteraction.Ignore))
        {
            if (debugRays)
                Debug.DrawRay(origin, Vector3.down * raycastDistance, Color.green, 0.25f);

            Vector3 forwardOnSurface = Vector3.ProjectOnPlane(transform.forward, hit.normal);
            if (forwardOnSurface.sqrMagnitude < 0.0001f)
                forwardOnSurface = Vector3.forward;

            Quaternion rotation = Quaternion.LookRotation(forwardOnSurface.normalized, hit.normal);
            Vector3 spawnPos = hit.point + hit.normal * spawnOffset;

            ParticleSystem spawned = Instantiate(footstepPrefab, spawnPos, rotation);

            if (tintFromSurface)
            {
                Color surfaceColor = ResolveSurfaceColor(hit);
                ApplyColorToParticleSystem(spawned, surfaceColor);
            }

            spawned.Play();

            if (debugStepLogs)
                Debug.Log("Spawned footstep FX at: " + spawnPos);
        }
        else if (debugRays)
        {
            Debug.DrawRay(origin, Vector3.down * raycastDistance, Color.red, 0.25f);
        }
    }

    private bool ShouldEmitThisStep()
    {
        float chance;

        if (playerController.IsSprinting)
            chance = emitChanceSprint;
        else if (playerController.MoveAmount >= 0.55f)
            chance = emitChanceRun;
        else
            chance = emitChanceWalk;

        return Random.value <= chance;
    }

    private Color ResolveSurfaceColor(RaycastHit hit)
    {
        Color result = fallbackParticleColor;

        Renderer rend = hit.collider.GetComponent<Renderer>();
        if (rend == null)
            rend = hit.collider.GetComponentInParent<Renderer>();

        if (rend == null || rend.sharedMaterial == null)
            return AdjustParticleColor(result);

        Material mat = rend.sharedMaterial;

        bool sampledTexture = false;

        if (sampleTextureColor && hit.collider is MeshCollider)
        {
            Texture mainTex = null;

            if (mat.HasProperty("_BaseMap"))
                mainTex = mat.GetTexture("_BaseMap");
            else if (mat.HasProperty("_MainTex"))
                mainTex = mat.GetTexture("_MainTex");

            if (mainTex is Texture2D tex && tex.isReadable)
            {
                Vector2 uv = hit.textureCoord;
                result = tex.GetPixelBilinear(uv.x, uv.y);
                sampledTexture = true;

                if (mat.HasProperty("_BaseColor"))
                    result *= mat.GetColor("_BaseColor");
                else if (mat.HasProperty("_Color"))
                    result *= mat.GetColor("_Color");
            }
        }

        if (!sampledTexture)
        {
            if (mat.HasProperty("_BaseColor"))
                result = mat.GetColor("_BaseColor");
            else if (mat.HasProperty("_Color"))
                result = mat.GetColor("_Color");
        }

        return AdjustParticleColor(result);
    }

    private Color AdjustParticleColor(Color input)
    {
        Color.RGBToHSV(input, out float h, out float s, out float v);

        s = Mathf.Clamp01(s * 0.85f);
        v = Mathf.Clamp(v * colorBrightnessMultiplier, minColorBrightness, maxColorBrightness);

        Color adjusted = Color.HSVToRGB(h, s, v);
        adjusted.a = 1f;
        return adjusted;
    }

    private void ApplyColorToParticleSystem(ParticleSystem ps, Color color)
    {
        if (ps == null)
            return;

        var main = ps.main;
        main.startColor = color;
    }
}