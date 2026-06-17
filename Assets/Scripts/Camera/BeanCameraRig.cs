using UnityEngine;

public class BeanCameraRig : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private Transform pitchPivot;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private BeanLockOn lockOn;
    [SerializeField] private BeanPlayerController controller;

    [Header("Follow")]
    [SerializeField] private float followSharpness = 16f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 220f;
    [SerializeField] private float minPitch = -35f;
    [SerializeField] private float maxPitch = 70f;
    [SerializeField] private float rotationSharpness = 18f;
    [SerializeField] private float lockOnRotationSharpness = 12f;
    [SerializeField] private bool lockCursorOnPlay = true;

    [Header("Base Camera Offset")]
    [SerializeField] private Vector3 cameraLocalOffset = new Vector3(0.85f, 0.7f, -4.6f);
    [SerializeField] private float cameraLocalPositionSharpness = 18f;

    [Header("Collision")]
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private float collisionRadius = 0.2f;
    [SerializeField] private float minCollisionDistance = 0.7f;

    [Header("Lock-On Framing")]
    [SerializeField] private float lockLookBlend = 0.75f;

    // NEW:
    // Raises the camera upward while locked on.
    [SerializeField] private float lockOnExtraCameraHeight = 0.45f;

    // NEW:
    // Raises the point the camera uses when calculating lock-on aim.
    [SerializeField] private float lockOnPlayerLookHeight = 1.15f;

    [Header("Target Size Zoom")]
    [SerializeField] private bool scaleDistanceByTargetSize = true;
    [SerializeField] private float minTargetSize = 1.2f;
    [SerializeField] private float maxTargetSize = 8f;
    [SerializeField] private float extraBackDistanceAtMaxSize = 3f;
    [SerializeField] private float extraHeightAtMaxSize = 0.35f;

    [Header("FOV Effects")]
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float sprintFOV = 67f;
    [SerializeField] private float lockOnFOV = 63f;
    [SerializeField] private float dodgeFOVKick = 2.5f;
    [SerializeField] private float fovSharpness = 8f;

    [Header("Motion Effects")]
    [SerializeField] private float bobAmplitude = 0.045f;
    [SerializeField] private float bobFrequency = 9f;
    [SerializeField] private float sprintBobMultiplier = 1.35f;
    [SerializeField] private float bobPitchAmount = 1.5f;
    [SerializeField] private float strafeRollAmount = 4f;
    [SerializeField] private float sprintRollAmount = 1.25f;

    [Header("Effect Smoothing")]
    [SerializeField] private float effectPositionSharpness = 10f;
    [SerializeField] private float effectRotationSharpness = 8f;

    [Header("Landing Effects")]
    [SerializeField] private float landDipDistance = 0.08f;
    [SerializeField] private float landPitchAmount = 4.5f;
    [SerializeField] private float landRecoverSharpness = 10f;

    public Camera PlayerCamera => playerCamera;

    public Vector3 PlanarForward
    {
        get
        {
            Vector3 forward = Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up);
            return forward.sqrMagnitude < 0.0001f ? transform.forward : forward.normalized;
        }
    }

    public Vector3 PlanarRight
    {
        get
        {
            Vector3 right = Vector3.ProjectOnPlane(playerCamera.transform.right, Vector3.up);
            return right.sqrMagnitude < 0.0001f ? transform.right : right.normalized;
        }
    }

    private float yaw;
    private float pitch;
    private float bobTime;
    private float dodgeFovBoost;
    private float landDip;
    private float landPitchKick;
    private Vector3 currentBaseLocalPos;

    private Vector3 smoothedEffectOffset;
    private float smoothedEffectPitch;
    private float smoothedEffectRoll;

    private void OnEnable()
    {
        if (controller != null)
        {
            controller.OnDodgeStarted += HandleDodgeStarted;
            controller.OnLanded += HandleLanded;
        }
    }

    private void OnDisable()
    {
        if (controller != null)
        {
            controller.OnDodgeStarted -= HandleDodgeStarted;
            controller.OnLanded -= HandleLanded;
        }
    }

    private void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        yaw = transform.eulerAngles.y;
        pitch = pitchPivot.localEulerAngles.x;
        pitch = NormalizeAngle(pitch);

        currentBaseLocalPos = cameraLocalOffset;
        playerCamera.fieldOfView = normalFOV;

        if (lockCursorOnPlay)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void LateUpdate()
    {
        if (followTarget == null || pitchPivot == null || playerCamera == null)
            return;

        float dt = Time.deltaTime;

        float followT = 1f - Mathf.Exp(-followSharpness * dt);
        transform.position = Vector3.Lerp(transform.position, followTarget.position, followT);

        if (lockOn != null && lockOn.IsLockedOn && lockOn.CurrentTarget != null)
            UpdateLockOnRotation(dt);
        else
            UpdateFreeLookRotation(dt);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.Euler(0f, yaw, 0f),
            1f - Mathf.Exp(-rotationSharpness * dt)
        );

        pitchPivot.localRotation = Quaternion.Slerp(
            pitchPivot.localRotation,
            Quaternion.Euler(pitch, 0f, 0f),
            1f - Mathf.Exp(-rotationSharpness * dt)
        );

        Vector3 baseLocal = ResolveCameraCollision(dt);
        ApplyCameraEffects(baseLocal, dt);
    }

    private void UpdateFreeLookRotation(float dt)
    {
        yaw += Input.GetAxisRaw("Mouse X") * mouseSensitivity * dt;
        pitch -= Input.GetAxisRaw("Mouse Y") * mouseSensitivity * dt;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void UpdateLockOnRotation(float dt)
    {
        EnemyTarget target = lockOn.CurrentTarget;

        Vector3 playerLookPoint = followTarget.position + Vector3.up * lockOnPlayerLookHeight;
        Vector3 lookPoint = Vector3.Lerp(playerLookPoint, target.AimPosition, lockLookBlend);

        Vector3 lookDirection = lookPoint - pitchPivot.position;

        if (lookDirection.sqrMagnitude < 0.001f)
            return;

        lookDirection.Normalize();

        Quaternion desiredRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        Vector3 desiredEuler = desiredRotation.eulerAngles;

        float desiredYaw = desiredEuler.y;
        float desiredPitch = Mathf.Clamp(NormalizeAngle(desiredEuler.x), minPitch, maxPitch);

        float t = 1f - Mathf.Exp(-lockOnRotationSharpness * dt);

        yaw = Mathf.LerpAngle(yaw, desiredYaw, t);
        pitch = Mathf.Lerp(pitch, desiredPitch, t);
    }

    private Vector3 ResolveCameraCollision(float dt)
    {
        Vector3 desiredLocal = GetDesiredCameraLocalOffset();

        float desiredDistance = desiredLocal.magnitude;
        Vector3 localDirection = desiredLocal.normalized;
        float finalDistance = desiredDistance;

        Vector3 castOrigin = pitchPivot.position;
        Vector3 castDirection = pitchPivot.TransformDirection(localDirection);

        if (Physics.SphereCast(
            castOrigin,
            collisionRadius,
            castDirection,
            out RaycastHit hit,
            desiredDistance,
            collisionMask,
            QueryTriggerInteraction.Ignore))
        {
            finalDistance = Mathf.Max(minCollisionDistance, hit.distance - collisionRadius);
        }

        Vector3 finalLocal = localDirection * finalDistance;

        currentBaseLocalPos = Vector3.Lerp(
            currentBaseLocalPos,
            finalLocal,
            1f - Mathf.Exp(-cameraLocalPositionSharpness * dt)
        );

        return currentBaseLocalPos;
    }

    private Vector3 GetDesiredCameraLocalOffset()
    {
        Vector3 desired = cameraLocalOffset;

        bool lockedOn =
            lockOn != null &&
            lockOn.IsLockedOn &&
            lockOn.CurrentTarget != null;

        if (!lockedOn)
            return desired;

        // This is the main new part:
        // it raises the camera whenever lock-on is active.
        desired.y += lockOnExtraCameraHeight;

        // This keeps your old large-enemy zoom behavior.
        if (scaleDistanceByTargetSize)
        {
            float targetSize = lockOn.CurrentTarget.EstimatedSize;

            float size01 = Mathf.Clamp01(
                (targetSize - minTargetSize) /
                Mathf.Max(0.001f, maxTargetSize - minTargetSize)
            );

            desired.y += extraHeightAtMaxSize * size01;
            desired.z -= extraBackDistanceAtMaxSize * size01;
        }

        return desired;
    }

    private void ApplyCameraEffects(Vector3 baseLocal, float dt)
    {
        Transform camTransform = playerCamera.transform;

        landDip = Mathf.Lerp(landDip, 0f, 1f - Mathf.Exp(-landRecoverSharpness * dt));
        landPitchKick = Mathf.Lerp(landPitchKick, 0f, 1f - Mathf.Exp(-landRecoverSharpness * dt));
        dodgeFovBoost = Mathf.Lerp(dodgeFovBoost, 0f, 1f - Mathf.Exp(-8f * dt));

        float targetBobY = 0f;
        float targetPitch = 0f;
        float targetRoll = 0f;

        if (controller != null)
        {
            bool shouldBob =
                controller.IsGrounded &&
                !controller.IsDodging &&
                controller.MoveAmount > 0.1f;

            if (shouldBob)
            {
                float bobMultiplier = controller.IsSprinting ? sprintBobMultiplier : 1f;

                bobTime += dt * bobFrequency * bobMultiplier;

                targetBobY =
                    Mathf.Sin(bobTime) *
                    bobAmplitude *
                    controller.MoveAmount *
                    bobMultiplier;

                targetPitch =
                    Mathf.Sin(bobTime) *
                    bobPitchAmount *
                    controller.MoveAmount;
            }
            else
            {
                bobTime += dt * bobFrequency * 0.25f;
            }

            float localX = controller.LocalPlanarVelocity.x;
            float strafe01 = Mathf.Clamp(localX / 4f, -1f, 1f);

            targetRoll = -strafe01 * strafeRollAmount;

            if (controller.IsSprinting)
                targetRoll += -strafe01 * sprintRollAmount;
        }

        Vector3 targetEffectOffset = new Vector3(0f, targetBobY - landDip, 0f);

        float fullPositionT = 1f - Mathf.Exp(-effectPositionSharpness * dt);
        float fullRotationT = 1f - Mathf.Exp(-effectRotationSharpness * dt);

        smoothedEffectOffset = Vector3.Lerp(
            smoothedEffectOffset,
            targetEffectOffset,
            fullPositionT
        );

        smoothedEffectPitch = Mathf.Lerp(
            smoothedEffectPitch,
            targetPitch + landPitchKick,
            fullRotationT
        );

        smoothedEffectRoll = Mathf.Lerp(
            smoothedEffectRoll,
            targetRoll,
            fullRotationT
        );

        camTransform.localPosition = baseLocal + smoothedEffectOffset;
        camTransform.localRotation = Quaternion.Euler(
            smoothedEffectPitch,
            0f,
            smoothedEffectRoll
        );

        float targetFov = normalFOV;

        if (lockOn != null && lockOn.IsLockedOn)
            targetFov = lockOnFOV;

        if (controller != null && controller.IsSprinting)
            targetFov = Mathf.Max(targetFov, sprintFOV);

        targetFov += dodgeFovBoost;

        playerCamera.fieldOfView = Mathf.Lerp(
            playerCamera.fieldOfView,
            targetFov,
            1f - Mathf.Exp(-fovSharpness * dt)
        );
    }

    private void HandleDodgeStarted()
    {
        dodgeFovBoost = dodgeFOVKick;
    }

    private void HandleLanded(float impact)
    {
        float t = Mathf.Clamp01(impact / 18f);

        landDip += landDipDistance * t;
        landPitchKick += landPitchAmount * t;
    }

    private float NormalizeAngle(float angle)
    {
        while (angle > 180f)
            angle -= 360f;

        while (angle < -180f)
            angle += 360f;

        return angle;
    }
}