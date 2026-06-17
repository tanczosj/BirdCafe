using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public class CameraHeadLook : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private BeanPlayerController playerController;

    [Header("Optional Animator Params")]
    [SerializeField] private string isAttackingParam = "IsAttacking";
    [SerializeField] private bool disableWhileAttacking = false;
    [SerializeField] private bool disableWhileDodging = true;

    [Header("Look Target")]
    [SerializeField] private float lookDistance = 8f;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.04f, 0f);

    [Header("Physical Limits")]
    [SerializeField] private float maxYaw = 75f;
    [SerializeField] private float maxPitchUp = 35f;
    [SerializeField] private float maxPitchDown = 25f;

    [Header("Hard Cutoff")]
    [SerializeField] private float hardYawCutoff = 100f;
    [SerializeField] private float hardPitchCutoff = 55f;
    [SerializeField][Range(0.1f, 0.95f)] private float fadeStartRatio = 0.75f;

    [Header("Look Weights")]
    [SerializeField][Range(0f, 1f)] private float overallWeight = 0.75f;
    [SerializeField][Range(0f, 1f)] private float bodyWeight = 0.03f;
    [SerializeField][Range(0f, 1f)] private float headWeight = 0.95f;
    [SerializeField][Range(0f, 1f)] private float eyesWeight = 0f;
    [SerializeField][Range(0f, 1f)] private float clampWeight = 0.55f;

    [Header("Smoothing")]
    [SerializeField] private float weightSmoothSpeed = 8f;
    [SerializeField] private float targetSmoothSpeed = 12f;

    [Header("Debug")]
    [SerializeField] private bool debugLine = false;

    private Transform headBone;
    private int attackingParamHash;
    private bool hasAttackingParam;

    private float currentWeight;
    private Vector3 smoothedLookTarget;
    private bool targetInitialized;

    private void Reset()
    {
        animator = GetComponent<Animator>();
    }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (playerController == null)
            playerController = GetComponentInParent<BeanPlayerController>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        attackingParamHash = Animator.StringToHash(isAttackingParam);

        if (animator != null)
        {
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.nameHash == attackingParamHash)
                {
                    hasAttackingParam = true;
                    break;
                }
            }

            if (animator.isHuman)
                headBone = animator.GetBoneTransform(HumanBodyBones.Head);
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || cameraTransform == null)
        {
            SetLookAtWeightImmediate(0f);
            return;
        }

        bool blocked =
            (disableWhileDodging && playerController != null && playerController.IsDodging) ||
            (disableWhileAttacking && hasAttackingParam && animator.GetBool(attackingParamHash));

        Vector3 origin = GetLookOrigin();
        Vector3 forwardFallbackTarget = origin + transform.forward * lookDistance + worldOffset;

        float targetWeight = 0f;
        Vector3 targetPoint = forwardFallbackTarget;

        if (!blocked)
        {
            Vector3 cameraForward = cameraTransform.forward.normalized;
            Vector3 localDir = transform.InverseTransformDirection(cameraForward);

            float yaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
            float pitch = Mathf.Asin(Mathf.Clamp(localDir.y, -1f, 1f)) * Mathf.Rad2Deg;

            bool beyondHardYaw = Mathf.Abs(yaw) > hardYawCutoff;
            bool beyondHardPitch = Mathf.Abs(pitch) > hardPitchCutoff;

            if (!beyondHardYaw && !beyondHardPitch)
            {
                float clampedYaw = Mathf.Clamp(yaw, -maxYaw, maxYaw);
                float clampedPitch = Mathf.Clamp(pitch, -maxPitchDown, maxPitchUp);

                float yawFade = Mathf.InverseLerp(maxYaw * fadeStartRatio, hardYawCutoff, Mathf.Abs(yaw));
                float pitchFade =
                    pitch >= 0f
                        ? Mathf.InverseLerp(maxPitchUp * fadeStartRatio, hardPitchCutoff, pitch)
                        : Mathf.InverseLerp(maxPitchDown * fadeStartRatio, hardPitchCutoff, -pitch);

                float edgeFade = 1f - Mathf.Clamp01(Mathf.Max(yawFade, pitchFade));
                targetWeight = overallWeight * edgeFade;

                float yawRad = clampedYaw * Mathf.Deg2Rad;
                float pitchRad = clampedPitch * Mathf.Deg2Rad;

                Vector3 limitedLocalDir = new Vector3(
                    Mathf.Sin(yawRad) * Mathf.Cos(pitchRad),
                    Mathf.Sin(pitchRad),
                    Mathf.Cos(yawRad) * Mathf.Cos(pitchRad)
                ).normalized;

                Vector3 worldDir = transform.TransformDirection(limitedLocalDir);
                targetPoint = origin + worldDir * lookDistance + worldOffset;
            }
        }

        float weightT = 1f - Mathf.Exp(-weightSmoothSpeed * Time.deltaTime);
        currentWeight = Mathf.Lerp(currentWeight, targetWeight, weightT);

        if (!targetInitialized)
        {
            smoothedLookTarget = targetPoint;
            targetInitialized = true;
        }
        else
        {
            float targetT = 1f - Mathf.Exp(-targetSmoothSpeed * Time.deltaTime);
            smoothedLookTarget = Vector3.Lerp(smoothedLookTarget, targetPoint, targetT);
        }

        animator.SetLookAtWeight(
            currentWeight,
            bodyWeight,
            headWeight,
            eyesWeight,
            clampWeight
        );

        animator.SetLookAtPosition(smoothedLookTarget);

        if (debugLine)
            Debug.DrawLine(origin, smoothedLookTarget, Color.yellow);
    }

    private Vector3 GetLookOrigin()
    {
        if (headBone != null)
            return headBone.position;

        return transform.position + Vector3.up * 1.6f;
    }

    private void SetLookAtWeightImmediate(float value)
    {
        currentWeight = value;
        animator.SetLookAtWeight(value, 0f, 0f, 0f, 0.5f);
    }
}