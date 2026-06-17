using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class BeanPlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private BeanCameraRig cameraRig;
    [SerializeField] private BeanLockOn lockOn;
    [SerializeField] private Stamina stamina;
    [SerializeField] private Health health;

    [Header("Free Locomotion")]
    [SerializeField] private float walkSpeed = 4.2f;
    [SerializeField] private float sprintSpeed = 7.0f;
    [SerializeField] private float acceleration = 18f;
    [SerializeField] private float deceleration = 24f;
    [SerializeField] private float airAcceleration = 8f;
    [SerializeField] private float freeRotationSharpness = 18f;

    [Header("Lock-On Locomotion")]
    [SerializeField] private float lockOnForwardSpeed = 4.4f;
    [SerializeField] private float lockOnBackwardSpeed = 3.2f;
    [SerializeField] private float lockOnStrafeSpeed = 4.0f;
    [SerializeField] private float lockOnRotationSharpness = 24f;

    [Header("Animation Smoothing")]
    [SerializeField] private float inputSmoothingSharpness = 14f;
    [SerializeField] private float animationVelocitySharpness = 14f;
    [SerializeField] private float moveAmountSharpness = 12f;

    [Header("Post-Dodge Lock-On Rotation")]
    [SerializeField] private float postDodgeLockOnRecoverDuration = 0.24f;

    [Header("Unlock Rotation")]
    [SerializeField] private float unlockFaceCameraDuration = 0.18f;

    [Header("Jump / Gravity")]
    [SerializeField] private float gravity = -28f;
    [SerializeField] private float groundedStickForce = -5f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float jumpStaminaCost = 12f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Fall Damage")]
    [SerializeField] private bool enableFallDamage = true;
    [SerializeField] private float minFallDamageImpactSpeed = 16f;
    [SerializeField] private float maxFallDamageImpactSpeed = 30f;
    [SerializeField] private float maxFallDamage = 100f;
    [SerializeField] private AnimationCurve fallDamageCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Sprint")]
    [SerializeField] private float sprintStaminaDrainPerSecond = 18f;

    [Header("Dodge")]
    [SerializeField] private float dodgeStaminaCost = 22f;
    [SerializeField] private float dodgeDistance = 5.5f;
    [SerializeField] private float backstepDistance = 3.2f;
    [SerializeField] private int dodgeFrames = 36;
    [SerializeField] private float dodgeAnimationFPS = 60f;
    [SerializeField] private float dodgeCooldown = 0.1f;
    [SerializeField] private float dodgeRotationSharpness = 40f;
    [SerializeField] private float dodgeRecoveryTime = 0.08f;
    [SerializeField] private AnimationCurve dodgeSpeedCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.15f);

    [Header("Lock-On Keys")]
    [SerializeField] private KeyCode toggleLockKey = KeyCode.Q;
    [SerializeField] private KeyCode cycleLeftKey = KeyCode.Z;
    [SerializeField] private KeyCode cycleRightKey = KeyCode.C;

    [Header("Action Keys")]
    [SerializeField] private KeyCode dodgeKey = KeyCode.LeftControl;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;

    public Vector2 MoveInput { get; private set; }
    public Vector3 PlanarVelocity => planarVelocity;
    public Vector3 LocalPlanarVelocity => transform.InverseTransformDirection(planarVelocity);
    public float VerticalVelocity => verticalVelocity;
    public bool IsGrounded { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool IsDodging { get; private set; }
    public bool IsLockedOn => lockOn != null && lockOn.IsLockedOn;
    public bool IsInputSuppressed => inputSuppressed;

    public float AnimationMoveX { get; private set; }
    public float AnimationMoveY { get; private set; }
    public float AnimationMoveAmount { get; private set; }
    public float TurnAmount { get; private set; }
    public float MoveAmount => AnimationMoveAmount;

    public event Action OnDodgeStarted;
    public event Action OnJumped;
    public event Action<float> OnLanded;

    private Vector3 planarVelocity;
    private float verticalVelocity;
    private bool inputSuppressed;
    private float lastGroundedTime = -999f;
    private float lastJumpPressedTime = -999f;
    private float nextDodgeAllowedTime;
    private Vector3 pendingExternalDelta;
    private bool wasGroundedLastFrame;

    private Vector2 rawMoveInput;
    private Vector2 smoothedMoveInput;
    private Vector3 animationLocalVelocity;

    private bool skipNextMovementFrameAfterDodge;
    private float dodgeRecoveryUntilTime;

    private bool postDodgeLockOnBlendActive;
    private float postDodgeLockOnBlendStartTime;
    private Quaternion postDodgeStartRotation;

    private bool postUnlockFaceCameraActive;
    private float postUnlockFaceCameraStartTime;
    private Quaternion postUnlockStartRotation;
    private Quaternion postUnlockTargetRotation;

    private Vector3 lastFreeFacingDirection = Vector3.forward;

    private float DodgeDurationSeconds => dodgeFrames / Mathf.Max(1f, dodgeAnimationFPS);
    private bool IsInDodgeRecovery => Time.time < dodgeRecoveryUntilTime;

    private void Reset()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Awake()
    {
        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        wasGroundedLastFrame = characterController.isGrounded;
        lastFreeFacingDirection = transform.forward;
    }

    private void Update()
    {
        if (health != null && health.IsDead)
            return;

        HandleLockOnInput();
        UpdateGrounding();
        ReadInput();
        HandleJumpInput();
        HandleDodgeInput();
        HandleMovement();
        HandleRotation();
        ApplyFinalMovement();
        UpdateAnimationData();
    }

    public void SetInputSuppressed(bool suppressed)
    {
        inputSuppressed = suppressed;
    }

    public void AddExternalDelta(Vector3 worldDelta)
    {
        pendingExternalDelta += worldDelta;
    }

    private void HandleLockOnInput()
    {
        if (lockOn == null)
            return;

        if (Input.GetKeyDown(toggleLockKey))
        {
            bool wasLocked = IsLockedOn;

            lockOn.ToggleLockOn();

            bool isLockedNow = IsLockedOn;

            if (wasLocked && !isLockedNow)
                BeginUnlockFaceCameraBlend();

            if (!wasLocked && isLockedNow)
                postUnlockFaceCameraActive = false;
        }

        if (Input.GetKeyDown(cycleLeftKey))
            lockOn.CycleTarget(-1);

        if (Input.GetKeyDown(cycleRightKey))
            lockOn.CycleTarget(1);
    }

    private void BeginUnlockFaceCameraBlend()
    {
        Vector3 targetDirection = transform.forward;

        if (cameraRig != null)
        {
            targetDirection = cameraRig.PlanarForward;
            if (targetDirection.sqrMagnitude < 0.001f)
                targetDirection = transform.forward;
        }

        targetDirection.y = 0f;

        if (targetDirection.sqrMagnitude < 0.001f)
            return;

        targetDirection.Normalize();

        postUnlockFaceCameraActive = true;
        postUnlockFaceCameraStartTime = Time.time;
        postUnlockStartRotation = transform.rotation;
        postUnlockTargetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);

        lastFreeFacingDirection = targetDirection;
    }

    private void UpdateGrounding()
    {
        bool groundedNow = characterController.isGrounded;

        if (groundedNow && !wasGroundedLastFrame && verticalVelocity < -2f)
        {
            float impact = Mathf.Abs(verticalVelocity);
            OnLanded?.Invoke(impact);
            ApplyFallDamage(impact);
        }

        IsGrounded = groundedNow;

        if (IsGrounded)
            lastGroundedTime = Time.time;

        wasGroundedLastFrame = groundedNow;
    }

    private void ReadInput()
    {
        if (IsDodging || IsInDodgeRecovery || inputSuppressed)
        {
            rawMoveInput = Vector2.zero;
            smoothedMoveInput = Vector2.Lerp(
                smoothedMoveInput,
                Vector2.zero,
                1f - Mathf.Exp(-inputSmoothingSharpness * Time.deltaTime));
            MoveInput = smoothedMoveInput;
            IsSprinting = false;
            return;
        }

        rawMoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        rawMoveInput = Vector2.ClampMagnitude(rawMoveInput, 1f);

        smoothedMoveInput = Vector2.Lerp(
            smoothedMoveInput,
            rawMoveInput,
            1f - Mathf.Exp(-inputSmoothingSharpness * Time.deltaTime));

        MoveInput = smoothedMoveInput;

        bool sprintHeld = Input.GetKey(KeyCode.LeftShift);
        bool forwardEnough = rawMoveInput.y > 0.1f;
        bool hasMoveInput = rawMoveInput.sqrMagnitude > 0.04f;

        IsSprinting = !IsLockedOn && sprintHeld && forwardEnough && hasMoveInput;

        if (IsSprinting && stamina != null)
        {
            if (!stamina.Consume(sprintStaminaDrainPerSecond * Time.deltaTime))
                IsSprinting = false;
        }
    }

    private void HandleJumpInput()
    {
        if (Input.GetKeyDown(jumpKey))
            lastJumpPressedTime = Time.time;

        if (IsDodging || IsInDodgeRecovery)
            return;

        bool bufferedJump = Time.time - lastJumpPressedTime <= jumpBufferTime;
        bool canUseCoyote = Time.time - lastGroundedTime <= coyoteTime;

        if (bufferedJump && canUseCoyote && !inputSuppressed)
        {
            if (stamina == null || stamina.Consume(jumpStaminaCost))
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                lastJumpPressedTime = -999f;
                lastGroundedTime = -999f;
                IsGrounded = false;
                OnJumped?.Invoke();
            }
        }

        if (IsGrounded && verticalVelocity < 0f)
            verticalVelocity = groundedStickForce;
        else
            verticalVelocity += gravity * Time.deltaTime;
    }

    private void HandleDodgeInput()
    {
        if (Input.GetKeyDown(dodgeKey) && CanStartDodge())
        {
            Vector3 direction;
            float distance;

            if (rawMoveInput.sqrMagnitude > 0.04f)
            {
                direction = GetDodgeDirectionWorld().normalized;
                distance = dodgeDistance;
            }
            else if (IsLockedOn)
            {
                direction = -transform.forward;
                distance = backstepDistance;
            }
            else
            {
                direction = transform.forward;
                distance = dodgeDistance;
            }

            StartCoroutine(DodgeRoutine(direction, distance));
        }
    }

    private bool CanStartDodge()
    {
        if (IsDodging)
            return false;

        if (IsInDodgeRecovery)
            return false;

        if (inputSuppressed)
            return false;

        if (Time.time < nextDodgeAllowedTime)
            return false;

        if (stamina != null && stamina.Current < dodgeStaminaCost)
            return false;

        return true;
    }

    private IEnumerator DodgeRoutine(Vector3 direction, float distance)
    {
        if (stamina != null && !stamina.Consume(dodgeStaminaCost))
            yield break;

        float dodgeDuration = DodgeDurationSeconds;

        IsDodging = true;
        postDodgeLockOnBlendActive = false;
        postUnlockFaceCameraActive = false;
        nextDodgeAllowedTime = Time.time + dodgeDuration + dodgeCooldown;
        planarVelocity = Vector3.zero;
        skipNextMovementFrameAfterDodge = false;
        OnDodgeStarted?.Invoke();

        float elapsed = 0f;
        float baseSpeed = distance / Mathf.Max(0.01f, dodgeDuration);

        while (elapsed < dodgeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dodgeDuration);
            float speed = baseSpeed * dodgeSpeedCurve.Evaluate(t);

            if (direction.sqrMagnitude > 0.001f)
                RotateTowards(direction, dodgeRotationSharpness);

            Vector3 delta = direction * speed * Time.deltaTime;
            Vector3 verticalDelta = Vector3.up * verticalVelocity * Time.deltaTime;
            characterController.Move(delta + verticalDelta);

            if (characterController.isGrounded && verticalVelocity < 0f)
                verticalVelocity = groundedStickForce;
            else
                verticalVelocity += gravity * Time.deltaTime;

            yield return null;
        }

        IsDodging = false;
        dodgeRecoveryUntilTime = Time.time + dodgeRecoveryTime;
        skipNextMovementFrameAfterDodge = true;
        planarVelocity = Vector3.zero;

        if (characterController.isGrounded || IsGrounded)
            verticalVelocity = groundedStickForce;

        if (IsLockedOn && lockOn != null && lockOn.CurrentTarget != null)
        {
            postDodgeLockOnBlendActive = true;
            postDodgeLockOnBlendStartTime = Time.time;
            postDodgeStartRotation = transform.rotation;
        }
    }

    private void HandleMovement()
    {
        if (IsDodging || IsInDodgeRecovery)
            return;

        Vector3 desiredVelocity = Vector3.zero;
        float accel = deceleration;

        if (IsLockedOn)
        {
            Vector3 localDesired = new Vector3(
                MoveInput.x * lockOnStrafeSpeed,
                0f,
                MoveInput.y >= 0f ? MoveInput.y * lockOnForwardSpeed : MoveInput.y * lockOnBackwardSpeed);

            desiredVelocity = transform.right * localDesired.x + transform.forward * localDesired.z;
            accel = localDesired.sqrMagnitude > 0.001f
                ? (IsGrounded ? acceleration : airAcceleration)
                : deceleration;
        }
        else
        {
            Vector3 moveDirection = GetFreeMoveDirectionWorld();
            float targetSpeed = IsSprinting ? sprintSpeed : walkSpeed;
            desiredVelocity = moveDirection * targetSpeed;

            if (moveDirection.sqrMagnitude > 0.001f)
                lastFreeFacingDirection = moveDirection.normalized;

            accel = moveDirection.sqrMagnitude > 0.001f
                ? (IsGrounded ? acceleration : airAcceleration)
                : deceleration;
        }

        planarVelocity = Vector3.MoveTowards(planarVelocity, desiredVelocity, accel * Time.deltaTime);
    }

    private void HandleRotation()
    {
        if (IsDodging)
            return;

        if (IsLockedOn && lockOn.CurrentTarget != null)
        {
            postUnlockFaceCameraActive = false;

            Vector3 toTarget = lockOn.CurrentTarget.WorldCenter - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude > 0.001f)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);

                if (postDodgeLockOnBlendActive)
                {
                    float t = Mathf.Clamp01((Time.time - postDodgeLockOnBlendStartTime) / Mathf.Max(0.0001f, postDodgeLockOnRecoverDuration));
                    transform.rotation = Quaternion.Slerp(postDodgeStartRotation, desiredRotation, t);

                    if (t >= 1f || Quaternion.Angle(transform.rotation, desiredRotation) < 0.25f)
                        postDodgeLockOnBlendActive = false;
                }
                else if (!IsInDodgeRecovery)
                {
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        desiredRotation,
                        1f - Mathf.Exp(-lockOnRotationSharpness * Time.deltaTime));
                }
            }

            return;
        }

        postDodgeLockOnBlendActive = false;

        if (IsInDodgeRecovery)
            return;

        if (postUnlockFaceCameraActive)
        {
            bool hasMoveInput = MoveInput.sqrMagnitude > 0.01f;
            bool hasPlanarVelocity = planarVelocity.sqrMagnitude > 0.05f;

            if (hasMoveInput || hasPlanarVelocity)
            {
                postUnlockFaceCameraActive = false;
            }
            else
            {
                float t = Mathf.Clamp01((Time.time - postUnlockFaceCameraStartTime) / Mathf.Max(0.0001f, unlockFaceCameraDuration));
                transform.rotation = Quaternion.Slerp(postUnlockStartRotation, postUnlockTargetRotation, t);

                if (t >= 1f || Quaternion.Angle(transform.rotation, postUnlockTargetRotation) < 0.25f)
                    postUnlockFaceCameraActive = false;

                return;
            }
        }

        Vector3 facingDirection = Vector3.zero;

        if (MoveInput.sqrMagnitude > 0.01f)
            facingDirection = GetFreeMoveDirectionWorld();
        else if (planarVelocity.sqrMagnitude > 0.05f)
            facingDirection = planarVelocity.normalized;
        else
            facingDirection = lastFreeFacingDirection;

        if (facingDirection.sqrMagnitude > 0.001f)
            RotateTowards(facingDirection, freeRotationSharpness);
    }

    private void RotateTowards(Vector3 direction, float sharpness)
    {
        Quaternion desired = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desired,
            1f - Mathf.Exp(-sharpness * Time.deltaTime));
    }

    private Vector3 GetFreeMoveDirectionWorld()
    {
        if (cameraRig == null)
            return new Vector3(MoveInput.x, 0f, MoveInput.y);

        Vector3 forward = cameraRig.PlanarForward;
        Vector3 right = cameraRig.PlanarRight;
        Vector3 direction = forward * MoveInput.y + right * MoveInput.x;
        return direction.sqrMagnitude > 1f ? direction.normalized : direction;
    }

    private Vector3 GetDodgeDirectionWorld()
    {
        if (IsLockedOn)
        {
            Vector3 direction = transform.right * rawMoveInput.x + transform.forward * rawMoveInput.y;
            return direction.sqrMagnitude > 1f ? direction.normalized : direction;
        }

        if (cameraRig == null)
            return new Vector3(rawMoveInput.x, 0f, rawMoveInput.y);

        Vector3 forward = cameraRig.PlanarForward;
        Vector3 right = cameraRig.PlanarRight;
        Vector3 move = forward * rawMoveInput.y + right * rawMoveInput.x;
        return move.sqrMagnitude > 1f ? move.normalized : move;
    }

    private void ApplyFinalMovement()
    {
        if (IsDodging)
        {
            pendingExternalDelta = Vector3.zero;
            return;
        }

        if (skipNextMovementFrameAfterDodge)
        {
            skipNextMovementFrameAfterDodge = false;
            pendingExternalDelta = Vector3.zero;
            return;
        }

        Vector3 finalDelta = (planarVelocity + Vector3.up * verticalVelocity) * Time.deltaTime;
        finalDelta += pendingExternalDelta;
        pendingExternalDelta = Vector3.zero;
        characterController.Move(finalDelta);
    }

    private void UpdateAnimationData()
    {
        float dt = Time.deltaTime;

        animationLocalVelocity = Vector3.Lerp(
            animationLocalVelocity,
            transform.InverseTransformDirection(planarVelocity),
            1f - Mathf.Exp(-animationVelocitySharpness * dt));

        float maxX = IsLockedOn ? lockOnStrafeSpeed : sprintSpeed;
        float maxForward = IsLockedOn ? lockOnForwardSpeed : sprintSpeed;
        float maxBackward = IsLockedOn ? lockOnBackwardSpeed : walkSpeed;

        float targetMoveX = Mathf.Clamp(animationLocalVelocity.x / Mathf.Max(0.01f, maxX), -1f, 1f);

        float zDenominator = animationLocalVelocity.z >= 0f ? maxForward : maxBackward;
        float targetMoveY = Mathf.Clamp(animationLocalVelocity.z / Mathf.Max(0.01f, zDenominator), -1f, 1f);

        float planarSpeed = new Vector2(planarVelocity.x, planarVelocity.z).magnitude;
        float moveAmountMaxSpeed = IsLockedOn
            ? Mathf.Max(lockOnForwardSpeed, lockOnStrafeSpeed)
            : (IsSprinting ? sprintSpeed : walkSpeed);

        float targetMoveAmount = Mathf.Clamp01(planarSpeed / Mathf.Max(0.01f, moveAmountMaxSpeed));

        AnimationMoveX = Mathf.Lerp(AnimationMoveX, targetMoveX, 1f - Mathf.Exp(-moveAmountSharpness * dt));
        AnimationMoveY = Mathf.Lerp(AnimationMoveY, targetMoveY, 1f - Mathf.Exp(-moveAmountSharpness * dt));
        AnimationMoveAmount = Mathf.Lerp(AnimationMoveAmount, targetMoveAmount, 1f - Mathf.Exp(-moveAmountSharpness * dt));

        Vector3 desiredFacing = IsLockedOn && lockOn != null && lockOn.CurrentTarget != null
            ? (lockOn.CurrentTarget.WorldCenter - transform.position)
            : GetFreeMoveDirectionWorld();

        desiredFacing.y = 0f;

        if (desiredFacing.sqrMagnitude > 0.001f)
        {
            float angle = Vector3.SignedAngle(transform.forward, desiredFacing.normalized, Vector3.up);
            TurnAmount = Mathf.Lerp(TurnAmount, Mathf.Clamp(angle / 90f, -1f, 1f), 1f - Mathf.Exp(-moveAmountSharpness * dt));
        }
        else
        {
            TurnAmount = Mathf.Lerp(TurnAmount, 0f, 1f - Mathf.Exp(-moveAmountSharpness * dt));
        }
    }

    private void ApplyFallDamage(float impactSpeed)
    {
        if (!enableFallDamage || health == null)
            return;

        if (impactSpeed < minFallDamageImpactSpeed)
            return;

        float t = Mathf.InverseLerp(minFallDamageImpactSpeed, maxFallDamageImpactSpeed, impactSpeed);
        float damage = maxFallDamage * fallDamageCurve.Evaluate(t);

        if (damage <= 0.01f)
            return;

        health.TakeDamage(damage);
    }
}