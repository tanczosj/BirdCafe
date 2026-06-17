using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public class BlendedFootIK : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private bool enableIK = true;
    [SerializeField] private LayerMask groundLayer = ~0;
    [SerializeField] private float raycastHeight = 0.75f;
    [SerializeField] private float raycastDistance = 1.85f;
    [SerializeField] private float footHeightOffset = 0.11f;
    [SerializeField] private float maxSlopeAngle = 60f;
    [SerializeField] private bool debugRays = false;

    [Header("Hard Disable States")]
    [SerializeField] private BeanPlayerController playerController;
    [SerializeField] private BeanLockOn lockOn;
    [SerializeField] private bool disableIKWhileDodging = true;
    [SerializeField] private bool disableIKWhileRolling = true;
    [SerializeField] private string rollStateTag = "Roll";

    [Header("Head Look")]
    [SerializeField] private bool enableHeadLookAtTarget = true;
    [SerializeField] private float lookAtWeight = 0.65f;
    [SerializeField] private float lookAtBodyWeight = 0.05f;
    [SerializeField] private float lookAtHeadWeight = 0.9f;
    [SerializeField] private float lookAtEyesWeight = 0f;
    [SerializeField] private float lookAtClampWeight = 0.55f;
    [SerializeField] private float lookAtSmoothSpeed = 10f;
    [SerializeField] private Vector3 lookAtWorldOffset = new Vector3(0f, 0.05f, 0f);
    [SerializeField] private bool disableHeadLookWhileDodging = true;
    [SerializeField] private bool disableHeadLookWhileAttacking = false;

    [Header("Position IK Weights")]
    [SerializeField] private float idleIKWeight = 1.0f;
    [SerializeField] private float walkIKWeight = 0.24f;
    [SerializeField] private float runIKWeight = 0.04f;
    [SerializeField] private float sprintIKWeight = 0.0f;
    [SerializeField] private float airborneIKWeight = 0.0f;

    [Header("Idle Planting")]
    [SerializeField] private float idleSpeedThreshold = 0.06f;
    [SerializeField] private float idleMaxFootUpCorrection = 0.20f;
    [SerializeField] private float idleMaxFootDownCorrection = 0.10f;

    [Header("Moving Foot Phase")]
    [SerializeField] private float footPlantMaxHeight = 0.16f;
    [SerializeField] private float footSwingDisableHeight = 0.30f;
    [SerializeField] private float footPlantVerticalSpeedThreshold = 1.0f;
    [SerializeField] private float maxFootUpCorrection = 0.055f;
    [SerializeField] private float maxFootDownCorrection = 0.035f;

    [Header("Foot Rotation")]
    [SerializeField] private bool enableFootRotation = true;
    [SerializeField] private float footGroundRotationBlend = 0.10f;
    [SerializeField] private float idleFootRotationWeight = 0.10f;
    [SerializeField] private float movingFootRotationWeight = 0.0f;

    [Header("Pelvis")]
    [SerializeField] private bool enablePelvisAdjustment = false;
    [SerializeField] private float pelvisOffsetLimit = 0.015f;
    [SerializeField] private float pelvisSmoothSpeed = 16f;

    [Header("Smoothing")]
    [SerializeField] private float footPositionSmoothSpeed = 24f;
    [SerializeField] private float footRotationSmoothSpeed = 12f;

    [Header("Prediction")]
    [SerializeField] private float predictiveRayForward = 0.03f;

    [Header("Optional Animator Parameters")]
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string isGroundedParam = "IsGrounded";
    [SerializeField] private string isRollingParam = "IsRolling";
    [SerializeField] private string isAttackingParam = "IsAttacking";
    [SerializeField] private string isSprintingParam = "IsSprinting";

    [Header("Fallback Motion")]
    [SerializeField] private float fallbackMaxRunSpeed = 6f;
    [SerializeField] private float groundedProbeDistance = 0.65f;

    private Animator animator;
    private CharacterController characterController;
    private Rigidbody attachedRigidbody;

    private readonly HashSet<int> animatorParams = new HashSet<int>();

    private int speedParamHash;
    private int groundedParamHash;
    private int rollingParamHash;
    private int attackingParamHash;
    private int sprintingParamHash;

    private bool hasSpeedParam;
    private bool hasGroundedParam;
    private bool hasRollingParam;
    private bool hasAttackingParam;
    private bool hasSprintingParam;

    private bool cachedGrounded;
    private bool cachedRolling;
    private bool cachedAttacking;
    private bool cachedSprinting;
    private float cachedNormalizedSpeed;
    private Vector3 cachedPlanarVelocity;

    private Vector3 lastReferencePosition;
    private bool hasLastReferencePosition;

    private float currentPelvisOffsetY;
    private float currentLookWeight;
    private Vector3 smoothedLookPosition;

    private bool warnedMissingFootBones;
    private bool warnedNotHumanoid;
    private bool warnedNoMotionFallback;

    private FootState leftFoot;
    private FootState rightFoot;

    private sealed class FootState
    {
        public AvatarIKGoal goal;
        public Transform boneTransform;

        public bool initialized;
        public bool hasLastAnimatedSample;

        public Vector3 animatedPosition;
        public Quaternion animatedRotation;
        public Vector3 lastAnimatedPosition;
        public float animatedVerticalSpeed;

        public bool hasGroundHit;
        public float slopeAngle;

        public Vector3 targetPosition;
        public Quaternion targetRotation = Quaternion.identity;
        public float targetPositionWeight;
        public float targetRotationWeight;
        public float desiredPelvisDown;

        public Vector3 smoothedPosition;
        public Quaternion smoothedRotation = Quaternion.identity;
        public float smoothedPositionWeight;
        public float smoothedRotationWeight;
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponentInParent<CharacterController>();
        attachedRigidbody = GetComponentInParent<Rigidbody>();

        if (playerController == null)
            playerController = GetComponentInParent<BeanPlayerController>();

        if (lockOn == null)
            lockOn = GetComponentInParent<BeanLockOn>();

        speedParamHash = Animator.StringToHash(speedParam);
        groundedParamHash = Animator.StringToHash(isGroundedParam);
        rollingParamHash = Animator.StringToHash(isRollingParam);
        attackingParamHash = Animator.StringToHash(isAttackingParam);
        sprintingParamHash = Animator.StringToHash(isSprintingParam);

        CacheAnimatorParameters();
        CacheFootStates();
    }

    private void Start()
    {
        if (animator == null)
        {
            Debug.LogWarning($"{nameof(BlendedFootIK)} on {name} needs an Animator.");
            enabled = false;
            return;
        }

        if (!animator.isHuman && !warnedNotHumanoid)
        {
            warnedNotHumanoid = true;
            Debug.LogWarning($"{nameof(BlendedFootIK)} on {name} requires a Humanoid avatar for Animator IK and look-at.");
        }

        if ((leftFoot.boneTransform == null || rightFoot.boneTransform == null) && !warnedMissingFootBones)
        {
            warnedMissingFootBones = true;
            Debug.LogWarning($"{nameof(BlendedFootIK)} on {name} could not find humanoid foot bones.");
        }
    }

    private void Update()
    {
        if (animator == null)
            return;

        UpdateLocomotionState();
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || !enableIK)
        {
            ForceDisableFeetIKInstant();
            ApplyLookAt();
            return;
        }

        if (cachedRolling)
        {
            ForceDisableFeetIKInstant();
            ApplyLookAt();
            return;
        }

        if (!animator.isHuman || leftFoot.boneTransform == null || rightFoot.boneTransform == null)
        {
            ForceDisableFeetIKInstant();
            ApplyLookAt();
            return;
        }

        EvaluateFoot(leftFoot);
        EvaluateFoot(rightFoot);
        ApplyPelvisOffset();
        ApplyFootIK(leftFoot);
        ApplyFootIK(rightFoot);
        ApplyLookAt();
    }

    private void CacheAnimatorParameters()
    {
        animatorParams.Clear();

        if (animator == null)
            return;

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
            animatorParams.Add(parameters[i].nameHash);

        hasSpeedParam = !string.IsNullOrWhiteSpace(speedParam) && animatorParams.Contains(speedParamHash);
        hasGroundedParam = !string.IsNullOrWhiteSpace(isGroundedParam) && animatorParams.Contains(groundedParamHash);
        hasRollingParam = !string.IsNullOrWhiteSpace(isRollingParam) && animatorParams.Contains(rollingParamHash);
        hasAttackingParam = !string.IsNullOrWhiteSpace(isAttackingParam) && animatorParams.Contains(attackingParamHash);
        hasSprintingParam = !string.IsNullOrWhiteSpace(isSprintingParam) && animatorParams.Contains(sprintingParamHash);
    }

    private void CacheFootStates()
    {
        leftFoot = new FootState
        {
            goal = AvatarIKGoal.LeftFoot,
            boneTransform = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.LeftFoot) : null
        };

        rightFoot = new FootState
        {
            goal = AvatarIKGoal.RightFoot,
            boneTransform = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.RightFoot) : null
        };
    }

    private void UpdateLocomotionState()
    {
        cachedGrounded = ResolveGrounded();
        cachedRolling = ResolveRollingState();
        cachedAttacking = ResolveOptionalBool(hasAttackingParam, attackingParamHash, false);
        cachedNormalizedSpeed = ResolveNormalizedSpeed();
        cachedSprinting = ResolveOptionalBool(hasSprintingParam, sprintingParamHash, cachedNormalizedSpeed > 0.85f);
    }

    private bool ResolveRollingState()
    {
        if (disableIKWhileDodging && playerController != null && playerController.IsDodging)
            return true;

        if (disableIKWhileRolling)
        {
            if (hasRollingParam && animator.GetBool(rollingParamHash))
                return true;

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (!string.IsNullOrWhiteSpace(rollStateTag) && stateInfo.IsTag(rollStateTag))
                return true;
        }

        return false;
    }

    private bool ResolveGrounded()
    {
        if (hasGroundedParam)
            return animator.GetBool(groundedParamHash);

        if (characterController != null)
            return characterController.isGrounded;

        Vector3 origin = GetReferencePosition() + Vector3.up * 0.2f;
        bool hit = Physics.Raycast(
            origin,
            Vector3.down,
            groundedProbeDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore);

        if (debugRays)
            Debug.DrawRay(origin, Vector3.down * groundedProbeDistance, hit ? Color.cyan : Color.red);

        return hit;
    }

    private bool ResolveOptionalBool(bool hasParam, int hash, bool fallback)
    {
        return hasParam ? animator.GetBool(hash) : fallback;
    }

    private float ResolveNormalizedSpeed()
    {
        if (hasSpeedParam)
        {
            float animatorSpeed = Mathf.Abs(animator.GetFloat(speedParamHash));

            if (animatorSpeed <= 1.25f)
            {
                cachedPlanarVelocity = transform.forward * animatorSpeed * fallbackMaxRunSpeed;
                return Mathf.Clamp01(animatorSpeed);
            }

            cachedPlanarVelocity = transform.forward * animatorSpeed;
            return Mathf.Clamp01(animatorSpeed / Mathf.Max(0.01f, fallbackMaxRunSpeed));
        }

        if (characterController != null)
        {
            Vector3 velocity = characterController.velocity;
            velocity.y = 0f;
            cachedPlanarVelocity = velocity;
            return Mathf.Clamp01(velocity.magnitude / Mathf.Max(0.01f, fallbackMaxRunSpeed));
        }

        if (attachedRigidbody != null)
        {
            Vector3 velocity = attachedRigidbody.linearVelocity;
            velocity.y = 0f;
            cachedPlanarVelocity = velocity;
            return Mathf.Clamp01(velocity.magnitude / Mathf.Max(0.01f, fallbackMaxRunSpeed));
        }

        Vector3 currentReferencePosition = GetReferencePosition();

        if (!hasLastReferencePosition)
        {
            lastReferencePosition = currentReferencePosition;
            hasLastReferencePosition = true;
            cachedPlanarVelocity = Vector3.zero;

            if (!warnedNoMotionFallback)
            {
                warnedNoMotionFallback = true;
                Debug.LogWarning($"{nameof(BlendedFootIK)} on {name} could not find CharacterController, Rigidbody, or a Speed parameter. Falling back to transform delta speed.");
            }

            return 0f;
        }

        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 delta = currentReferencePosition - lastReferencePosition;
        lastReferencePosition = currentReferencePosition;
        delta.y = 0f;

        cachedPlanarVelocity = delta / dt;
        return Mathf.Clamp01(cachedPlanarVelocity.magnitude / Mathf.Max(0.01f, fallbackMaxRunSpeed));
    }

    private Vector3 GetReferencePosition()
    {
        if (characterController != null)
            return characterController.transform.position;

        if (attachedRigidbody != null)
            return attachedRigidbody.position;

        return transform.position;
    }

    private float GetBaseStatePositionWeight()
    {
        if (!cachedGrounded)
            return airborneIKWeight;

        if (cachedRolling || cachedAttacking)
            return 0f;

        if (cachedNormalizedSpeed < idleSpeedThreshold)
            return idleIKWeight;

        if (cachedSprinting)
            return sprintIKWeight;

        if (cachedNormalizedSpeed < 0.55f)
            return walkIKWeight;

        return runIKWeight;
    }

    private void EvaluateFoot(FootState foot)
    {
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        float baseWeight = GetBaseStatePositionWeight();
        bool isIdle = cachedGrounded && cachedNormalizedSpeed < idleSpeedThreshold && !cachedRolling && !cachedAttacking;

        foot.animatedPosition = animator.GetIKPosition(foot.goal);
        foot.animatedRotation = foot.boneTransform.rotation;

        if (foot.hasLastAnimatedSample)
            foot.animatedVerticalSpeed = (foot.animatedPosition.y - foot.lastAnimatedPosition.y) / dt;
        else
            foot.animatedVerticalSpeed = 0f;

        foot.lastAnimatedPosition = foot.animatedPosition;
        foot.hasLastAnimatedSample = true;

        Vector3 moveDir = cachedPlanarVelocity.sqrMagnitude > 0.0001f ? cachedPlanarVelocity.normalized : Vector3.zero;
        Vector3 rayOrigin = foot.animatedPosition + Vector3.up * raycastHeight + moveDir * predictiveRayForward;
        float rayLength = raycastHeight + raycastDistance;

        bool hitGround = Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out RaycastHit hit,
            rayLength,
            groundLayer,
            QueryTriggerInteraction.Ignore);

        if (debugRays)
            Debug.DrawRay(rayOrigin, Vector3.down * rayLength, hitGround ? Color.green : Color.red);

        foot.hasGroundHit = hitGround;
        foot.slopeAngle = 0f;

        foot.targetPosition = foot.animatedPosition;
        foot.targetRotation = foot.animatedRotation;
        foot.targetPositionWeight = 0f;
        foot.targetRotationWeight = 0f;
        foot.desiredPelvisDown = 0f;

        if (hitGround)
        {
            foot.slopeAngle = Vector3.Angle(hit.normal, Vector3.up);

            if (foot.slopeAngle <= maxSlopeAngle)
            {
                float desiredY = hit.point.y + (hit.normal * footHeightOffset).y;
                float yCorrection = desiredY - foot.animatedPosition.y;

                float maxUp = isIdle ? idleMaxFootUpCorrection : maxFootUpCorrection;
                float maxDown = isIdle ? idleMaxFootDownCorrection : maxFootDownCorrection;
                yCorrection = Mathf.Clamp(yCorrection, -maxDown, maxUp);

                foot.targetPosition = foot.animatedPosition;
                foot.targetPosition.y = foot.animatedPosition.y + yCorrection;

                float footHeightAboveGround = Mathf.Max(0f, foot.animatedPosition.y - desiredY);

                float phaseByHeight;
                float phaseByVerticalSpeed;

                if (isIdle)
                {
                    phaseByHeight = 1f;
                    phaseByVerticalSpeed = 1f;
                }
                else
                {
                    phaseByHeight = 1f - Mathf.InverseLerp(
                        footPlantMaxHeight,
                        footSwingDisableHeight,
                        footHeightAboveGround);
                    phaseByHeight = Mathf.Clamp01(phaseByHeight);

                    phaseByVerticalSpeed = 1f - Mathf.InverseLerp(
                        0.12f,
                        footPlantVerticalSpeedThreshold,
                        Mathf.Abs(foot.animatedVerticalSpeed));
                    phaseByVerticalSpeed = Mathf.Clamp01(phaseByVerticalSpeed);

                    if (foot.animatedVerticalSpeed > 0.03f)
                        phaseByVerticalSpeed *= 0.25f;
                }

                float footPlantPhase = Mathf.Clamp01(phaseByHeight * phaseByVerticalSpeed);
                foot.targetPositionWeight = baseWeight * footPlantPhase;

                if (enableFootRotation)
                {
                    Quaternion alignedToGround =
                        Quaternion.FromToRotation(foot.animatedRotation * Vector3.up, hit.normal) * foot.animatedRotation;

                    foot.targetRotation = Quaternion.Slerp(
                        foot.animatedRotation,
                        alignedToGround,
                        footGroundRotationBlend);

                    foot.targetRotationWeight = isIdle
                        ? foot.targetPositionWeight * idleFootRotationWeight
                        : foot.targetPositionWeight * movingFootRotationWeight;
                }

                if (enablePelvisAdjustment && isIdle && yCorrection < -0.0025f)
                {
                    foot.desiredPelvisDown = Mathf.Clamp(yCorrection * 0.25f, -pelvisOffsetLimit, 0f);
                }
            }
        }

        float posT = 1f - Mathf.Exp(-footPositionSmoothSpeed * dt);
        float rotT = 1f - Mathf.Exp(-footRotationSmoothSpeed * dt);

        if (!foot.initialized)
        {
            foot.smoothedPosition = foot.targetPosition;
            foot.smoothedRotation = foot.targetRotation;
            foot.smoothedPositionWeight = foot.targetPositionWeight;
            foot.smoothedRotationWeight = foot.targetRotationWeight;
            foot.initialized = true;
        }
        else
        {
            foot.smoothedPosition = Vector3.Lerp(foot.smoothedPosition, foot.targetPosition, posT);
            foot.smoothedRotation = Quaternion.Slerp(foot.smoothedRotation, foot.targetRotation, rotT);
            foot.smoothedPositionWeight = Mathf.Lerp(foot.smoothedPositionWeight, foot.targetPositionWeight, posT);
            foot.smoothedRotationWeight = Mathf.Lerp(foot.smoothedRotationWeight, foot.targetRotationWeight, rotT);
        }
    }

    private void ApplyPelvisOffset()
    {
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        float targetPelvisDown = 0f;

        if (enablePelvisAdjustment &&
            cachedGrounded &&
            cachedNormalizedSpeed < idleSpeedThreshold &&
            leftFoot.hasGroundHit &&
            rightFoot.hasGroundHit)
        {
            targetPelvisDown = Mathf.Min(leftFoot.desiredPelvisDown, rightFoot.desiredPelvisDown);
            targetPelvisDown = Mathf.Clamp(targetPelvisDown, -pelvisOffsetLimit, 0f);
        }

        float pelvisT = 1f - Mathf.Exp(-pelvisSmoothSpeed * dt);
        currentPelvisOffsetY = Mathf.Lerp(currentPelvisOffsetY, targetPelvisDown, pelvisT);

        Vector3 bodyPos = animator.bodyPosition;
        bodyPos.y += currentPelvisOffsetY;
        animator.bodyPosition = bodyPos;
    }

    private void ApplyFootIK(FootState foot)
    {
        animator.SetIKPositionWeight(foot.goal, foot.smoothedPositionWeight);
        animator.SetIKRotationWeight(foot.goal, foot.smoothedRotationWeight);
        animator.SetIKPosition(foot.goal, foot.smoothedPosition);
        animator.SetIKRotation(foot.goal, foot.smoothedRotation);
    }

    private void ApplyLookAt()
    {
        if (animator == null || !enableHeadLookAtTarget || lockOn == null)
        {
            SetLookAtWeightsImmediate(0f);
            return;
        }

        bool shouldLook =
            lockOn.IsLockedOn &&
            lockOn.CurrentTarget != null &&
            !(disableHeadLookWhileDodging && playerController != null && playerController.IsDodging) &&
            !(disableHeadLookWhileAttacking && cachedAttacking);

        float targetWeight = shouldLook ? lookAtWeight : 0f;
        float lookT = 1f - Mathf.Exp(-lookAtSmoothSpeed * Time.deltaTime);
        currentLookWeight = Mathf.Lerp(currentLookWeight, targetWeight, lookT);

        if (lockOn.CurrentTarget != null)
        {
            Vector3 targetPos = lockOn.CurrentTarget.AimPosition + lookAtWorldOffset;

            if (smoothedLookPosition == Vector3.zero)
                smoothedLookPosition = targetPos;
            else
                smoothedLookPosition = Vector3.Lerp(smoothedLookPosition, targetPos, lookT);

            animator.SetLookAtWeight(
                currentLookWeight,
                lookAtBodyWeight,
                lookAtHeadWeight,
                lookAtEyesWeight,
                lookAtClampWeight);

            animator.SetLookAtPosition(smoothedLookPosition);
        }
        else
        {
            SetLookAtWeightsImmediate(0f);
        }
    }

    private void SetLookAtWeightsImmediate(float weight)
    {
        currentLookWeight = weight;
        animator.SetLookAtWeight(weight, 0f, 0f, 0f, 0.5f);
    }

    private void ForceDisableFeetIKInstant()
    {
        currentPelvisOffsetY = 0f;

        leftFoot.smoothedPositionWeight = 0f;
        rightFoot.smoothedPositionWeight = 0f;
        leftFoot.smoothedRotationWeight = 0f;
        rightFoot.smoothedRotationWeight = 0f;

        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0f);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0f);
        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0f);
        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0f);
    }
}