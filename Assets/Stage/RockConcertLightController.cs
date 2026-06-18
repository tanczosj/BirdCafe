using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Drives five URP 2D stage spotlights from a BPM-based clock.
/// Assign the channels in physical stage order: blue, yellow, white, red, green.
/// </summary>
[DisallowMultipleComponent]
public sealed class RockConcertLightController : MonoBehaviour
{
    private const int LightCount = 5;

    public enum LightingProgram
    {
        AutoRockShow = 0,
        AllOnPulse = 1,
        LeftToRightChase = 2,
        PingPong = 3,
        OddEven = 4,
        OutsideIn = 5,
        CenterOut = 6,
        RandomHits = 7,
        FullStage = 8,
        Blackout = 9
    }

    [Serializable]
    public sealed class SpotlightSettings
    {
        [Tooltip("The URP Light 2D component controlled by this channel.")]
        public Light2D light;

        [Tooltip("The base color assigned to this spotlight.")]
        public Color color = Color.white;

        [Min(0f)]
        [Tooltip("The brightest normal intensity this light may reach.")]
        public float maxIntensity = 1.5f;

        [Range(0f, 1f)]
        [Tooltip("Controls the brightness and distance of the light's edge falloff.")]
        public float falloffIntensity = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("Maximum Light 2D volume intensity. Set to 0 to disable the volume for this light.")]
        public float maxVolumeIntensity = 0.25f;

        [NonSerialized] public float currentIntensity;
        [NonSerialized] public float currentVolumeIntensity;
    }

    [Header("Spotlights (stage left to stage right)")]
    [SerializeField] private SpotlightSettings blueSpotlight = CreateBlueDefaults();
    [SerializeField] private SpotlightSettings yellowSpotlight = CreateYellowDefaults();
    [SerializeField] private SpotlightSettings whiteSpotlight = CreateWhiteDefaults();
    [SerializeField] private SpotlightSettings redSpotlight = CreateRedDefaults();
    [SerializeField] private SpotlightSettings greenSpotlight = CreateGreenDefaults();

    [Header("Song Clock")]
    [SerializeField, Min(1f)] private float beatsPerMinute = 120f;
    [SerializeField, Min(1)] private int beatsPerBar = 4;
    [SerializeField, Min(0.0625f)] private float beatsPerStep = 0.5f;
    [SerializeField] private float beatOffset = 0f;

    [Tooltip("Optional. When assigned, the lighting clock follows the AudioSource playback position, including pauses, seeks, and loops.")]
    [SerializeField] private AudioSource songAudioSource;

    [SerializeField] private bool useAudioSourceClock = true;
    [SerializeField] private bool startAutomatically = true;

    [Header("Program")]
    [SerializeField] private LightingProgram program = LightingProgram.AutoRockShow;

    [Min(1)]
    [Tooltip("In Auto Rock Show mode, change to the next pattern after this many bars.")]
    [SerializeField] private int barsPerAutoProgram = 4;

    [Range(0f, 1f)]
    [Tooltip("Brightness used for lights that are participating in the current pattern.")]
    [SerializeField] private float activeLevel = 0.82f;

    [Range(0f, 1f)]
    [Tooltip("Low background brightness used for lights that are not active in the current step.")]
    [SerializeField] private float inactiveLevel = 0.025f;

    [Range(0f, 1f)]
    [Tooltip("Lowest brightness reached by the All On Pulse pattern.")]
    [SerializeField] private float pulseMinimumLevel = 0.12f;

    [Range(0f, 1f)]
    [Tooltip("Probability that Random Hits will illuminate two lights instead of one.")]
    [SerializeField] private float randomDoubleHitChance = 0.4f;

    [SerializeField] private int randomSeed = 173;

    [Header("Transitions")]
    [Min(0f)]
    [Tooltip("Fade-in duration measured in beats. Small values create sharper concert hits.")]
    [SerializeField] private float attackBeats = 0.06f;

    [Min(0f)]
    [Tooltip("Fade-out duration measured in beats.")]
    [SerializeField] private float releaseBeats = 0.22f;

    [Header("Downbeat Accent")]
    [SerializeField] private bool accentFirstBeatOfBar = true;

    [Range(0f, 1f)]
    [Tooltip("Active lights are raised toward this level on the first beat of each bar.")]
    [SerializeField] private float downbeatLevel = 1f;

    [Range(0.01f, 1f)]
    [Tooltip("Length of the first-beat accent, measured in beats.")]
    [SerializeField] private float downbeatAccentLength = 0.2f;

    [Header("Manual Cue Overrides")]
    [Min(0.01f)]
    [SerializeField] private float fullStageHitLengthBeats = 0.5f;

    [Min(0.01f)]
    [SerializeField] private float blackoutLengthBeats = 1f;

    private readonly float[] desiredLevels = new float[LightCount];
    private SpotlightSettings[] spotlights;
    private bool isRunning;
    private double manualClockStartDspTime;
    private CueOverride cueOverride;
    private float cueRemainingSeconds;
    private long cachedRandomStep = long.MinValue;
    private int cachedRandomMask;

    private static readonly LightingProgram[] AutoPrograms =
    {
        LightingProgram.AllOnPulse,
        LightingProgram.OddEven,
        LightingProgram.PingPong,
        LightingProgram.CenterOut,
        LightingProgram.RandomHits,
        LightingProgram.OutsideIn,
        LightingProgram.LeftToRightChase,
        LightingProgram.FullStage
    };

    private static readonly int[] PingPongIndices = { 0, 1, 2, 3, 4, 3, 2, 1 };
    private static readonly int[] OutsideInMasks = { 0b10001, 0b01010, 0b00100, 0b01010 };
    private static readonly int[] CenterOutMasks = { 0b00100, 0b01110, 0b11111, 0b01110 };

    private enum CueOverride
    {
        None,
        FullStageHit,
        Blackout
    }

    /// <summary>Gets or sets the song tempo used by the lighting clock.</summary>
    public float BeatsPerMinute
    {
        get => beatsPerMinute;
        set => beatsPerMinute = Mathf.Max(1f, value);
    }

    /// <summary>Gets or sets the currently selected lighting program.</summary>
    public LightingProgram Program
    {
        get => program;
        set => program = value;
    }

    private void Awake()
    {
        CacheSpotlights();
        ApplyConfiguredLightProperties();
        CaptureCurrentLightValues();
    }

    private void OnEnable()
    {
        CacheSpotlights();

        if (Application.isPlaying && startAutomatically)
        {
            StartShow();
        }
    }

    private void OnValidate()
    {
        beatsPerMinute = Mathf.Max(1f, beatsPerMinute);
        beatsPerBar = Mathf.Max(1, beatsPerBar);
        beatsPerStep = Mathf.Max(0.0625f, beatsPerStep);
        barsPerAutoProgram = Mathf.Max(1, barsPerAutoProgram);
        attackBeats = Mathf.Max(0f, attackBeats);
        releaseBeats = Mathf.Max(0f, releaseBeats);
        fullStageHitLengthBeats = Mathf.Max(0.01f, fullStageHitLengthBeats);
        blackoutLengthBeats = Mathf.Max(0.01f, blackoutLengthBeats);

        CacheSpotlights();
        ApplyConfiguredLightProperties();
    }

    private void Update()
    {
        CacheSpotlightsIfNeeded();

        if (cueRemainingSeconds > 0f)
        {
            cueRemainingSeconds -= Time.deltaTime;
            if (cueRemainingSeconds <= 0f)
            {
                cueOverride = CueOverride.None;
            }
        }

        double beatPosition = GetBeatPosition();

        if (!isRunning)
        {
            FillDesiredLevels(0f);
        }
        else if (cueOverride == CueOverride.FullStageHit)
        {
            FillDesiredLevels(1f);
        }
        else if (cueOverride == CueOverride.Blackout)
        {
            FillDesiredLevels(0f);
        }
        else
        {
            BuildProgramLevels(beatPosition);
        }

        ApplyDesiredLevels(Time.deltaTime);
    }

    /// <summary>
    /// Starts or restarts the lighting clock. Call this at the same time the song starts
    /// when no AudioSource is assigned.
    /// </summary>
    public void StartShow()
    {
        isRunning = true;
        manualClockStartDspTime = AudioSettings.dspTime;
        cueOverride = CueOverride.None;
        cueRemainingSeconds = 0f;
        cachedRandomStep = long.MinValue;
    }

    /// <summary>Stops the show and smoothly fades all lights to zero.</summary>
    public void StopShow()
    {
        isRunning = false;
        cueOverride = CueOverride.None;
        cueRemainingSeconds = 0f;
    }

    /// <summary>Restarts the manual BPM clock without changing the selected program.</summary>
    public void RestartBeatClock()
    {
        manualClockStartDspTime = AudioSettings.dspTime;
        cachedRandomStep = long.MinValue;
    }

    /// <summary>Temporarily drives all five lights to their configured maximum values.</summary>
    public void TriggerFullStageHit()
    {
        cueOverride = CueOverride.FullStageHit;
        cueRemainingSeconds = fullStageHitLengthBeats * SecondsPerBeat;
    }

    /// <summary>Temporarily blacks out all five lights.</summary>
    public void TriggerBlackout()
    {
        cueOverride = CueOverride.Blackout;
        cueRemainingSeconds = blackoutLengthBeats * SecondsPerBeat;
    }

    /// <summary>Sets the program from a UnityEvent-friendly integer enum value.</summary>
    public void SetProgram(int programValue)
    {
        if (Enum.IsDefined(typeof(LightingProgram), programValue))
        {
            program = (LightingProgram)programValue;
        }
    }

    /// <summary>Sets the BPM from a UnityEvent or another gameplay script.</summary>
    public void SetBeatsPerMinute(float bpm)
    {
        BeatsPerMinute = bpm;
    }

    /// <summary>Immediately reapplies colors, falloff, and volumetric enablement.</summary>
    public void ReapplyLightConfiguration()
    {
        CacheSpotlights();
        ApplyConfiguredLightProperties();
    }

    private float SecondsPerBeat => 60f / Mathf.Max(1f, beatsPerMinute);

    private void BuildProgramLevels(double beatPosition)
    {
        LightingProgram activeProgram = ResolveProgram(beatPosition);
        long stepIndex = (long)Math.Floor(beatPosition / beatsPerStep);

        switch (activeProgram)
        {
            case LightingProgram.AllOnPulse:
                BuildPulseLevels(beatPosition);
                break;

            case LightingProgram.LeftToRightChase:
                BuildMaskLevels(1 << PositiveModulo(stepIndex, LightCount));
                break;

            case LightingProgram.PingPong:
            {
                int lightIndex = PingPongIndices[PositiveModulo(stepIndex, PingPongIndices.Length)];
                BuildMaskLevels(1 << lightIndex);
                break;
            }

            case LightingProgram.OddEven:
                BuildMaskLevels((stepIndex & 1L) == 0L ? 0b10101 : 0b01010);
                break;

            case LightingProgram.OutsideIn:
                BuildMaskLevels(OutsideInMasks[PositiveModulo(stepIndex, OutsideInMasks.Length)]);
                break;

            case LightingProgram.CenterOut:
                BuildMaskLevels(CenterOutMasks[PositiveModulo(stepIndex, CenterOutMasks.Length)]);
                break;

            case LightingProgram.RandomHits:
                BuildMaskLevels(GetRandomMask(stepIndex));
                break;

            case LightingProgram.FullStage:
                FillDesiredLevels(activeLevel);
                break;

            case LightingProgram.Blackout:
                FillDesiredLevels(0f);
                break;

            case LightingProgram.AutoRockShow:
            default:
                FillDesiredLevels(inactiveLevel);
                break;
        }

        ApplyDownbeatAccent(beatPosition);
    }

    private LightingProgram ResolveProgram(double beatPosition)
    {
        if (program != LightingProgram.AutoRockShow)
        {
            return program;
        }

        double beatsPerProgram = beatsPerBar * Math.Max(1, barsPerAutoProgram);
        long autoProgramIndex = (long)Math.Floor(beatPosition / beatsPerProgram);
        return AutoPrograms[PositiveModulo(autoProgramIndex, AutoPrograms.Length)];
    }

    private void BuildPulseLevels(double beatPosition)
    {
        float beatPhase = (float)(beatPosition - Math.Floor(beatPosition));

        // Starts at maximum on the beat and falls away quickly, like a lighting "hit."
        float pulse = Mathf.Exp(-5f * beatPhase);
        float level = Mathf.Lerp(pulseMinimumLevel, 1f, pulse);
        FillDesiredLevels(level);
    }

    private void BuildMaskLevels(int mask)
    {
        for (int i = 0; i < LightCount; i++)
        {
            desiredLevels[i] = (mask & (1 << i)) != 0 ? activeLevel : inactiveLevel;
        }
    }

    private void ApplyDownbeatAccent(double beatPosition)
    {
        if (!accentFirstBeatOfBar || beatsPerBar <= 0)
        {
            return;
        }

        double barPhase = beatPosition % beatsPerBar;
        if (barPhase < 0d)
        {
            barPhase += beatsPerBar;
        }

        if (barPhase > downbeatAccentLength)
        {
            return;
        }

        float fade = 1f - Mathf.Clamp01((float)(barPhase / downbeatAccentLength));
        float accent = Mathf.Lerp(activeLevel, downbeatLevel, fade);

        for (int i = 0; i < LightCount; i++)
        {
            if (desiredLevels[i] > inactiveLevel + 0.001f)
            {
                desiredLevels[i] = Mathf.Max(desiredLevels[i], accent);
            }
        }
    }

    private int GetRandomMask(long stepIndex)
    {
        if (cachedRandomStep == stepIndex)
        {
            return cachedRandomMask;
        }

        cachedRandomStep = stepIndex;

        int stepSeed = unchecked(randomSeed * 486187739 + (int)(stepIndex * 16777619L));
        var random = new System.Random(stepSeed);

        int first = random.Next(0, LightCount);
        int mask = 1 << first;

        if (random.NextDouble() < randomDoubleHitChance)
        {
            int second = random.Next(0, LightCount - 1);
            if (second >= first)
            {
                second++;
            }

            mask |= 1 << second;
        }

        cachedRandomMask = mask;
        return mask;
    }

    private void ApplyDesiredLevels(float deltaTime)
    {
        float attackSeconds = attackBeats * SecondsPerBeat;
        float releaseSeconds = releaseBeats * SecondsPerBeat;

        for (int i = 0; i < LightCount; i++)
        {
            SpotlightSettings settings = spotlights[i];
            if (settings == null || settings.light == null)
            {
                continue;
            }

            float level = Mathf.Clamp01(desiredLevels[i]);
            float targetIntensity = settings.maxIntensity * level;
            float targetVolume = settings.maxVolumeIntensity * level;

            settings.currentIntensity = Damp(
                settings.currentIntensity,
                targetIntensity,
                targetIntensity >= settings.currentIntensity ? attackSeconds : releaseSeconds,
                deltaTime);

            settings.currentVolumeIntensity = Damp(
                settings.currentVolumeIntensity,
                targetVolume,
                targetVolume >= settings.currentVolumeIntensity ? attackSeconds : releaseSeconds,
                deltaTime);

            settings.light.intensity = settings.currentIntensity;
            settings.light.volumeIntensity = settings.currentVolumeIntensity;
        }
    }

    private static float Damp(float current, float target, float duration, float deltaTime)
    {
        if (duration <= 0f || deltaTime <= 0f)
        {
            return target;
        }

        float interpolation = 1f - Mathf.Exp(-deltaTime / duration);
        return Mathf.Lerp(current, target, interpolation);
    }

    private double GetBeatPosition()
    {
        double clockSeconds;

        if (useAudioSourceClock && songAudioSource != null && songAudioSource.clip != null)
        {
            clockSeconds = (double)songAudioSource.timeSamples / songAudioSource.clip.frequency;
        }
        else
        {
            clockSeconds = AudioSettings.dspTime - manualClockStartDspTime;
        }

        double beats = clockSeconds / SecondsPerBeat + beatOffset;
        return Math.Max(0d, beats);
    }

    private void ApplyConfiguredLightProperties()
    {
        if (spotlights == null || spotlights.Length != LightCount)
        {
            return;
        }

        foreach (SpotlightSettings settings in spotlights)
        {
            if (settings == null || settings.light == null)
            {
                continue;
            }

            settings.light.color = settings.color;
            settings.light.falloffIntensity = settings.falloffIntensity;
            settings.light.volumetricEnabled = settings.maxVolumeIntensity > 0.0001f;

            if (!settings.light.volumetricEnabled)
            {
                settings.light.volumeIntensity = 0f;
            }
        }
    }

    private void CaptureCurrentLightValues()
    {
        foreach (SpotlightSettings settings in spotlights)
        {
            if (settings == null || settings.light == null)
            {
                continue;
            }

            settings.currentIntensity = settings.light.intensity;
            settings.currentVolumeIntensity = settings.light.volumeIntensity;
        }
    }

    private void FillDesiredLevels(float level)
    {
        for (int i = 0; i < LightCount; i++)
        {
            desiredLevels[i] = level;
        }
    }

    private void CacheSpotlights()
    {
        spotlights = new[]
        {
            blueSpotlight,
            yellowSpotlight,
            whiteSpotlight,
            redSpotlight,
            greenSpotlight
        };
    }

    private void CacheSpotlightsIfNeeded()
    {
        if (spotlights == null || spotlights.Length != LightCount)
        {
            CacheSpotlights();
        }
    }

    private static int PositiveModulo(long value, int modulus)
    {
        long result = value % modulus;
        return (int)(result < 0 ? result + modulus : result);
    }

    private static SpotlightSettings CreateBlueDefaults()
    {
        return new SpotlightSettings
        {
            color = new Color(0.18f, 0.45f, 1f, 1f)
        };
    }

    private static SpotlightSettings CreateYellowDefaults()
    {
        return new SpotlightSettings
        {
            color = new Color(1f, 0.82f, 0.16f, 1f)
        };
    }

    private static SpotlightSettings CreateWhiteDefaults()
    {
        return new SpotlightSettings
        {
            color = Color.white,
            maxIntensity = 1.8f,
            maxVolumeIntensity = 0.3f
        };
    }

    private static SpotlightSettings CreateRedDefaults()
    {
        return new SpotlightSettings
        {
            color = new Color(1f, 0.16f, 0.1f, 1f)
        };
    }

    private static SpotlightSettings CreateGreenDefaults()
    {
        return new SpotlightSettings
        {
            color = new Color(0.16f, 1f, 0.35f, 1f)
        };
    }
}
