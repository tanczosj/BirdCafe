using UnityEngine;
using TriForge;

[DisallowMultipleComponent]
public class WindCameraShake : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TFFWFoliageController windController;

    [Header("Overall")]
    [SerializeField] private bool enableShake = true;
    [SerializeField] private float strengthMultiplier = 1.0f;
    [SerializeField] private float smoothing = 8f;

    [Header("Position Shake")]
    [SerializeField] private Vector3 maxLocalPositionOffset = new Vector3(0.025f, 0.015f, 0.02f);
    [SerializeField] private float directionalPositionInfluence = 0.35f;

    [Header("Rotation Shake")]
    [SerializeField] private Vector3 maxLocalRotationOffset = new Vector3(0.6f, 0.4f, 0.8f);
    [SerializeField] private float directionalRollInfluence = 0.5f;

    [Header("Noise")]
    [SerializeField] private float noiseSpeedMin = 0.25f;
    [SerializeField] private float noiseSpeedMax = 1.4f;
    [SerializeField] private float noiseScale = 1.0f;

    [Header("Wind Response")]
    [SerializeField] private bool useGrassWindAsExtraInfluence = false;
    [SerializeField] private float grassWindContribution = 0.2f;
    [SerializeField] private AnimationCurve windResponse = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;

    private Vector3 currentLocalPositionOffset;
    private Vector3 currentLocalRotationOffset;

    private float seedA;
    private float seedB;
    private float seedC;
    private float seedD;
    private float seedE;
    private float seedF;

    private void Awake()
    {
        baseLocalPosition = transform.localPosition;
        baseLocalRotation = transform.localRotation;

        seedA = Random.Range(0f, 1000f);
        seedB = Random.Range(0f, 1000f);
        seedC = Random.Range(0f, 1000f);
        seedD = Random.Range(0f, 1000f);
        seedE = Random.Range(0f, 1000f);
        seedF = Random.Range(0f, 1000f);

        if (windController == null)
            windController = FindFirstObjectByType<TFFWFoliageController>();
    }

    private void LateUpdate()
    {
        if (!enableShake || windController == null)
        {
            ResetToBase();
            return;
        }

        float windStrength = Mathf.Max(0f, windController.WindStrength);

        if (useGrassWindAsExtraInfluence)
            windStrength += windController.GrassWindStrength * grassWindContribution;

        windStrength *= strengthMultiplier;

        float normalizedWind = Mathf.Clamp01(windStrength / 2f);
        float response = windResponse.Evaluate(normalizedWind);

        if (response <= 0.0001f)
        {
            ResetToBase();
            return;
        }

        float noiseSpeed = Mathf.Lerp(noiseSpeedMin, noiseSpeedMax, response);
        float t = Time.time * noiseSpeed * noiseScale;

        Vector3 worldWindDir = windController.transform.right.normalized;

        Transform relativeSpace = transform.parent != null ? transform.parent : transform;
        Vector3 localWindDir = relativeSpace.InverseTransformDirection(worldWindDir);
        localWindDir.Normalize();

        Vector3 noisePos = new Vector3(
            RemapNoise(seedA, t),
            RemapNoise(seedB, t),
            RemapNoise(seedC, t)
        );

        Vector3 directionalPos = new Vector3(
            localWindDir.x * maxLocalPositionOffset.x,
            0f,
            localWindDir.z * maxLocalPositionOffset.z
        ) * directionalPositionInfluence;

        Vector3 targetPosOffset = Vector3.Scale(noisePos, maxLocalPositionOffset) * response;
        targetPosOffset += directionalPos * response;

        Vector3 noiseRot = new Vector3(
            RemapNoise(seedD, t),
            RemapNoise(seedE, t),
            RemapNoise(seedF, t)
        );

        Vector3 targetRotOffset = Vector3.Scale(noiseRot, maxLocalRotationOffset) * response;

        // Add a little wind-based roll so stronger sideways wind feels directional.
        targetRotOffset.z += (-localWindDir.x * maxLocalRotationOffset.z * directionalRollInfluence * response);

        float lerpT = 1f - Mathf.Exp(-smoothing * Time.deltaTime);

        currentLocalPositionOffset = Vector3.Lerp(currentLocalPositionOffset, targetPosOffset, lerpT);
        currentLocalRotationOffset = Vector3.Lerp(currentLocalRotationOffset, targetRotOffset, lerpT);

        transform.localPosition = baseLocalPosition + currentLocalPositionOffset;
        transform.localRotation = baseLocalRotation * Quaternion.Euler(currentLocalRotationOffset);
    }

    private void ResetToBase()
    {
        float lerpT = 1f - Mathf.Exp(-smoothing * Time.deltaTime);

        currentLocalPositionOffset = Vector3.Lerp(currentLocalPositionOffset, Vector3.zero, lerpT);
        currentLocalRotationOffset = Vector3.Lerp(currentLocalRotationOffset, Vector3.zero, lerpT);

        transform.localPosition = baseLocalPosition + currentLocalPositionOffset;
        transform.localRotation = baseLocalRotation * Quaternion.Euler(currentLocalRotationOffset);
    }

    private float RemapNoise(float seed, float timeValue)
    {
        return Mathf.PerlinNoise(seed, timeValue) * 2f - 1f;
    }
}