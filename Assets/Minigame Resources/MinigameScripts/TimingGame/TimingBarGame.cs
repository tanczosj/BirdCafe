using Ricimi;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AdvancedTimingBarGame : MonoBehaviour
{
    public enum HitResult
    {
        Miss,
        Hit,
        Perfect
    }

    [Header("UI References")]
    [SerializeField] private RectTransform track;
    [SerializeField] private RectTransform targetZone;
    [SerializeField] private RectTransform perfectZone;
    [SerializeField] private RectTransform mover;

    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text timerText;

    [Header("Optional FX")]
    [SerializeField] private RectTransform floatingTextSpawn;
    [SerializeField] private RectTransform uiShakeRoot;

    [Header("Track Images")]
    [SerializeField] private Image trackImage;
    [SerializeField] private Image targetImage;
    [SerializeField] private Image perfectImage;
    [SerializeField] private Image moverImage;

    [Header("Base Gameplay")]
    [SerializeField] private float baseMoveSpeed = 360f;
    [SerializeField] private float maxMoveSpeed = 1000f;
    [SerializeField] private float baseTargetWidth = 140f;
    [SerializeField] private float minTargetWidth = 35f;
    [SerializeField] private float perfectZoneRatio = 0.22f;
    [SerializeField] private float minPerfectZoneWidth = 10f;
    [SerializeField] private float targetEdgePadding = 20f;

    [Header("Difficulty Scaling")]
    [SerializeField] private float speedIncreasePerPoint = 15f;
    [SerializeField] private float targetShrinkPerPoint = 4f;
    [SerializeField] private AnimationCurve difficultyCurve = AnimationCurve.Linear(0f, 1f, 50f, 1.5f);

    [Header("Timing / Round")]
    [SerializeField] private bool useRoundTimer = true;
    [SerializeField] private float roundDuration = 20f;
    [SerializeField] private bool stopGameWhenTimerEnds = true;

    [Header("Scoring")]
    [SerializeField] private int pointsPerHit = 1;
    [SerializeField] private int pointsPerPerfect = 2;
    [SerializeField] private int comboBonusEvery = 5;
    [SerializeField] private int comboBonusPoints = 1;

    [Header("Feedback Timing")]
    [SerializeField] private float hitPauseDuration = 0.05f;
    [SerializeField] private float perfectPauseDuration = 0.08f;
    [SerializeField] private float missPauseDuration = 0.04f;

    [Header("Colors")]
    [SerializeField] private Color defaultTrackColor = new Color(0.35f, 0.35f, 0.35f);
    [SerializeField] private Color hitTrackColor = new Color(0.70f, 1f, 0.70f);
    [SerializeField] private Color perfectTrackColor = new Color(1f, 0.93f, 0.55f);
    [SerializeField] private Color missTrackColor = new Color(1f, 0.70f, 0.70f);

    [SerializeField] private Color statusReadyColor = Color.white;
    [SerializeField] private Color statusHitColor = new Color(0.85f, 1f, 0.85f);
    [SerializeField] private Color statusPerfectColor = new Color(1f, 0.93f, 0.45f);
    [SerializeField] private Color statusMissColor = new Color(1f, 0.75f, 0.75f);

    [SerializeField] private Color comboIdleColor = Color.white;
    [SerializeField] private Color comboActiveColor = new Color(0.55f, 0.95f, 1f);
    [SerializeField] private Color comboHotColor = new Color(1f, 0.92f, 0.45f);

    [Header("Text Juice")]
    [SerializeField] private float comboPulseScale = 1.18f;
    [SerializeField] private float statusPunchScale = 1.26f;
    [SerializeField] private float textAnimDuration = 0.15f;

    [Header("Floating Text")]
    [SerializeField] private bool enableFloatingText = true;
    [SerializeField] private Vector2 floatingTextOffsetRange = new Vector2(28f, 10f);
    [SerializeField] private float floatingTextRise = 40f;
    [SerializeField] private float floatingTextLifetime = 0.45f;
    [SerializeField] private float floatingTextScale = 0.8f;
    [SerializeField] private int floatingTextFontSize = 20;

    [Header("Target Motion")]
    [SerializeField] private bool animateTargetMove = true;
    [SerializeField] private float targetMoveDuration = 0.14f;

    [Header("Shake")]
    [SerializeField] private bool enableUiShake = true;
    [SerializeField] private float perfectShakeDuration = 0.14f;
    [SerializeField] private float perfectShakeMagnitude = 10f;
    [SerializeField] private float milestoneShakeDuration = 0.20f;
    [SerializeField] private float milestoneShakeMagnitude = 16f;

    [Header("Milestones")]
    [SerializeField] private int comboMilestoneStep = 5;
    [SerializeField] private bool pulseTargetOnSuccess = true;
    [SerializeField] private bool pulseMoverOnPerfect = true;

    [Header("Input")]
    [SerializeField] private KeyCode[] extraKeys = { KeyCode.Space, KeyCode.Return, KeyCode.E };

    private float _direction = 1f;
    private float _currentSpeed;
    private float _currentTargetWidth;

    private int _score;
    private int _combo;
    private int _bestCombo;
    private int _hits;
    private int _perfects;
    private int _misses;

    private bool _gameOver;
    private bool _pausedForFeedback;
    private float _timeRemaining;

    private Vector3 _comboBaseScale;
    private Vector3 _statusBaseScale;
    private Vector3 _targetBaseScale;
    private Vector3 _perfectBaseScale;
    private Vector3 _moverBaseScale;
    private Vector2 _uiShakeBasePos;

    private Coroutine _targetMoveRoutine;
    private Coroutine _uiShakeRoutine;

    private float TrackWidth => track.rect.width;
    private float HalfTrackWidth => TrackWidth * 0.5f;
    private float HalfMoverWidth => mover.rect.width * 0.5f;
    private float HalfTargetWidth => targetZone.rect.width * 0.5f;
    private float HalfPerfectWidth => perfectZone.rect.width * 0.5f;

    private void Awake()
    {
        if (trackImage == null) trackImage = track.GetComponent<Image>();
        if (targetImage == null) targetImage = targetZone.GetComponent<Image>();
        if (perfectImage == null) perfectImage = perfectZone.GetComponent<Image>();
        if (moverImage == null) moverImage = mover.GetComponent<Image>();
        if (uiShakeRoot == null && track.parent is RectTransform parentRect)
        {
            uiShakeRoot = parentRect;
        }

        _comboBaseScale = comboText != null ? comboText.rectTransform.localScale : Vector3.one;
        _statusBaseScale = statusText != null ? statusText.rectTransform.localScale : Vector3.one;
        _targetBaseScale = targetZone.localScale;
        _perfectBaseScale = perfectZone.localScale;
        _moverBaseScale = mover.localScale;
        _uiShakeBasePos = uiShakeRoot != null ? uiShakeRoot.anchoredPosition : Vector2.zero;
    }

    private void Start()
    {
        RestartGame();
    }

    private void Update()
    {
        if (_gameOver)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartGame();
            }
            return;
        }

        if (useRoundTimer)
        {
            UpdateTimer();
        }

        if (!_pausedForFeedback)
        {
            MoveMover();
        }

        if (WasSubmitPressedThisFrame())
        {
            TryHit();
        }
    }

    public void RestartGame()
    {
        StopAllCoroutines();
        _targetMoveRoutine = null;
        _uiShakeRoutine = null;

        _direction = 1f;
        _currentSpeed = baseMoveSpeed;
        _currentTargetWidth = baseTargetWidth;

        _score = 0;
        _combo = 0;
        _bestCombo = 0;
        _hits = 0;
        _perfects = 0;
        _misses = 0;

        _timeRemaining = roundDuration;
        _gameOver = false;
        _pausedForFeedback = false;

        mover.anchoredPosition = new Vector2(0f, mover.anchoredPosition.y);

        ApplyTargetWidth(_currentTargetWidth);

        float initialX = GetRandomTargetX();
        targetZone.anchoredPosition = new Vector2(initialX, targetZone.anchoredPosition.y);
        perfectZone.anchoredPosition = Vector2.zero;

        if (trackImage != null) trackImage.color = defaultTrackColor;
        if (comboText != null) comboText.rectTransform.localScale = _comboBaseScale;
        if (statusText != null) statusText.rectTransform.localScale = _statusBaseScale;

        targetZone.localScale = _targetBaseScale;
        perfectZone.localScale = _perfectBaseScale;
        mover.localScale = _moverBaseScale;

        if (uiShakeRoot != null)
        {
            uiShakeRoot.anchoredPosition = _uiShakeBasePos;
        }

        SetStatus("Ready", statusReadyColor);
        RefreshUI();
    }

    private void UpdateTimer()
    {
        _timeRemaining -= Time.unscaledDeltaTime;

        if (_timeRemaining <= 0f)
        {
            _timeRemaining = 0f;
            RefreshUI();

            if (stopGameWhenTimerEnds)
            {
                _gameOver = true;
                SetStatus($"TIME UP  SCORE {_score}  BEST {_bestCombo}  PRESS R", statusPerfectColor);
            }
        }
    }

    private void MoveMover()
    {
        float x = mover.anchoredPosition.x;
        x += _direction * _currentSpeed * Time.unscaledDeltaTime;

        float minX = -HalfTrackWidth + HalfMoverWidth;
        float maxX = HalfTrackWidth - HalfMoverWidth;

        if (x >= maxX)
        {
            x = maxX;
            _direction = -1f;
        }
        else if (x <= minX)
        {
            x = minX;
            _direction = 1f;
        }

        mover.anchoredPosition = new Vector2(x, mover.anchoredPosition.y);
    }

    private bool WasSubmitPressedThisFrame()
    {
        if (Input.GetMouseButtonDown(0))
            return true;

        if (Input.GetKeyDown(KeyCode.JoystickButton0))
            return true;

        foreach (KeyCode key in extraKeys)
        {
            if (Input.GetKeyDown(key))
                return true;
        }

        return false;
    }

    private void TryHit()
    {
        HitResult result = EvaluateHit();

        switch (result)
        {
            case HitResult.Perfect:
                HandlePerfect();
                break;

            case HitResult.Hit:
                HandleHit();
                break;

            default:
                HandleMiss();
                break;
        }
    }

    private HitResult EvaluateHit()
    {
        float moverCenter = mover.anchoredPosition.x;

        float targetLeft = targetZone.anchoredPosition.x - HalfTargetWidth;
        float targetRight = targetZone.anchoredPosition.x + HalfTargetWidth;

        float perfectWorldX = targetZone.anchoredPosition.x + perfectZone.anchoredPosition.x;
        float perfectLeft = perfectWorldX - HalfPerfectWidth;
        float perfectRight = perfectWorldX + HalfPerfectWidth;

        if (moverCenter >= perfectLeft && moverCenter <= perfectRight)
            return HitResult.Perfect;

        if (moverCenter >= targetLeft && moverCenter <= targetRight)
            return HitResult.Hit;

        return HitResult.Miss;
    }

    private void HandleHit()
    {
        _hits++;
        _combo++;
        _bestCombo = Mathf.Max(_bestCombo, _combo);

        int gained = pointsPerHit;
        if (comboBonusEvery > 0 && _combo % comboBonusEvery == 0)
        {
            gained += comboBonusPoints;
        }

        _score += gained;

        ScaleDifficulty();
        MoveTargetToNewPosition();
        RefreshUI();

        SetStatus(gained > pointsPerHit ? $"HIT +{gained}" : "HIT", statusHitColor);
        AnimateComboText();
        AnimateStatusText();
        CreateFloatingText(gained > pointsPerHit ? $"+{gained}" : "HIT!", statusHitColor);

        if (pulseTargetOnSuccess)
        {
            StartCoroutine(PulseRect(targetZone, _targetBaseScale, 1.10f, 0.13f));
            StartCoroutine(PulseRect(perfectZone, _perfectBaseScale, 1.15f, 0.13f));
        }

        bool milestone = comboMilestoneStep > 0 && _combo > 0 && _combo % comboMilestoneStep == 0;
        if (milestone)
        {
            CreateFloatingText($"{_combo} COMBO!", comboHotColor, 1.15f);
            StartUiShake(milestoneShakeDuration, milestoneShakeMagnitude);
        }

        StartCoroutine(FeedbackRoutine(HitResult.Hit));
    }

    private void HandlePerfect()
    {
        _hits++;
        _perfects++;
        _combo++;
        _bestCombo = Mathf.Max(_bestCombo, _combo);

        int gained = pointsPerPerfect;
        if (comboBonusEvery > 0 && _combo % comboBonusEvery == 0)
        {
            gained += comboBonusPoints;
        }

        _score += gained;

        ScaleDifficulty();
        MoveTargetToNewPosition();
        RefreshUI();

        SetStatus(gained > pointsPerPerfect ? $"PERFECT +{gained}" : "PERFECT!", statusPerfectColor);
        AnimateComboText(1.26f);
        AnimateStatusText(1.34f);
        CreateFloatingText(gained > pointsPerPerfect ? $"+{gained} PERFECT!" : "PERFECT!", statusPerfectColor, 1.12f);

        if (pulseTargetOnSuccess)
        {
            StartCoroutine(PulseRect(targetZone, _targetBaseScale, 1.14f, 0.16f));
            StartCoroutine(PulseRect(perfectZone, _perfectBaseScale, 1.25f, 0.16f));
        }

        if (pulseMoverOnPerfect)
        {
            StartCoroutine(PulseRect(mover, _moverBaseScale, 1.16f, 0.10f));
        }

        bool milestone = comboMilestoneStep > 0 && _combo > 0 && _combo % comboMilestoneStep == 0;
        if (milestone)
        {
            CreateFloatingText($"{_combo} COMBO STREAK!", comboHotColor, 1.20f);
            StartUiShake(milestoneShakeDuration, milestoneShakeMagnitude);
        }
        else
        {
            StartUiShake(perfectShakeDuration, perfectShakeMagnitude);
        }

        StartCoroutine(FeedbackRoutine(HitResult.Perfect));

        Transition.LoadLevel("Gameplay", 1f, Color.black);
    }

    private void HandleMiss()
    {
        _misses++;
        _combo = 0;
        RefreshUI();

        SetStatus("MISS", statusMissColor);
        AnimateStatusText(1.16f);
        CreateFloatingText("MISS!", statusMissColor);

        StartCoroutine(FeedbackRoutine(HitResult.Miss));
    }

    private IEnumerator FeedbackRoutine(HitResult result)
    {
        _pausedForFeedback = true;

        if (trackImage != null)
        {
            switch (result)
            {
                case HitResult.Hit:
                    trackImage.color = hitTrackColor;
                    break;
                case HitResult.Perfect:
                    trackImage.color = perfectTrackColor;
                    break;
                default:
                    trackImage.color = missTrackColor;
                    break;
            }
        }

        float pause =
            result == HitResult.Perfect ? perfectPauseDuration :
            result == HitResult.Hit ? hitPauseDuration :
            missPauseDuration;

        if (pause > 0f)
        {
            yield return new WaitForSecondsRealtime(pause);
        }

        if (trackImage != null)
        {
            float t = 0f;
            Color start = trackImage.color;

            while (t < 0.12f)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / 0.12f);
                trackImage.color = Color.Lerp(start, defaultTrackColor, p);
                yield return null;
            }

            trackImage.color = defaultTrackColor;
        }

        _pausedForFeedback = false;
    }

    private void ScaleDifficulty()
    {
        float multiplier = difficultyCurve.Evaluate(_score);

        _currentSpeed = Mathf.Min(
            maxMoveSpeed,
            (baseMoveSpeed + (_score * speedIncreasePerPoint)) * multiplier);

        _currentTargetWidth = Mathf.Max(
            minTargetWidth,
            baseTargetWidth - (_score * targetShrinkPerPoint));

        ApplyTargetWidth(_currentTargetWidth);
    }

    private void ApplyTargetWidth(float width)
    {
        targetZone.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);

        float perfectWidth = Mathf.Max(minPerfectZoneWidth, width * perfectZoneRatio);
        perfectZone.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, perfectWidth);
        perfectZone.anchoredPosition = Vector2.zero;
    }

    private void MoveTargetToNewPosition()
    {
        float randomX = GetRandomTargetX();

        if (_targetMoveRoutine != null)
        {
            StopCoroutine(_targetMoveRoutine);
        }

        if (animateTargetMove)
        {
            _targetMoveRoutine = StartCoroutine(AnimateTargetMove(randomX, targetMoveDuration));
        }
        else
        {
            targetZone.anchoredPosition = new Vector2(randomX, targetZone.anchoredPosition.y);
            perfectZone.anchoredPosition = Vector2.zero;
        }
    }

    private float GetRandomTargetX()
    {
        float halfTarget = targetZone.rect.width * 0.5f;

        float minX = -HalfTrackWidth + halfTarget + targetEdgePadding;
        float maxX = HalfTrackWidth - halfTarget - targetEdgePadding;

        return maxX < minX ? 0f : Random.Range(minX, maxX);
    }

    private IEnumerator AnimateTargetMove(float targetX, float duration)
    {
        float startX = targetZone.anchoredPosition.x;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = SmoothStep(t);

            float x = Mathf.Lerp(startX, targetX, eased);
            targetZone.anchoredPosition = new Vector2(x, targetZone.anchoredPosition.y);
            perfectZone.anchoredPosition = Vector2.zero;
            yield return null;
        }

        targetZone.anchoredPosition = new Vector2(targetX, targetZone.anchoredPosition.y);
        perfectZone.anchoredPosition = Vector2.zero;
        _targetMoveRoutine = null;
    }

    private void RefreshUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"SCORE  {_score}";
        }

        if (comboText != null)
        {
            if (_combo <= 0)
            {
                comboText.text = $"COMBO x0   BEST x{_bestCombo}";
                comboText.color = comboIdleColor;
            }
            else if (_combo < comboMilestoneStep)
            {
                comboText.text = $"COMBO x{_combo}   BEST x{_bestCombo}";
                comboText.color = comboActiveColor;
            }
            else
            {
                comboText.text = $"🔥 COMBO x{_combo}   BEST x{_bestCombo} 🔥";
                comboText.color = comboHotColor;
            }
        }

        if (timerText != null)
        {
            timerText.text = useRoundTimer ? $"TIME  {_timeRemaining:0.0}" : "";
        }
    }

    private void SetStatus(string message, Color color)
    {
        if (statusText == null)
            return;

        statusText.text = message;
        statusText.color = color;
    }

    private void AnimateComboText(float scale = -1f)
    {
        if (comboText == null)
            return;

        float targetScale = scale > 0f ? scale : comboPulseScale;
        StartCoroutine(PulseRect(comboText.rectTransform, _comboBaseScale, targetScale, textAnimDuration));
    }

    private void AnimateStatusText(float scale = -1f)
    {
        if (statusText == null)
            return;

        float targetScale = scale > 0f ? scale : statusPunchScale;
        StartCoroutine(PulseRect(statusText.rectTransform, _statusBaseScale, targetScale, textAnimDuration));
    }

    private IEnumerator PulseRect(RectTransform rect, Vector3 baseScale, float peakMultiplier, float duration)
    {
        Vector3 peak = baseScale * peakMultiplier;
        float half = duration * 0.5f;
        float t = 0f;

        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / half);
            rect.localScale = Vector3.Lerp(baseScale, peak, EaseOutBack(p));
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / half);
            rect.localScale = Vector3.Lerp(peak, baseScale, p);
            yield return null;
        }

        rect.localScale = baseScale;
    }

    private void CreateFloatingText(string text, Color color, float scaleMultiplier = 1f)
    {
        if (!enableFloatingText || floatingTextSpawn == null)
            return;

        GameObject go = new GameObject("FloatingText", typeof(RectTransform));
        go.transform.SetParent(floatingTextSpawn, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        float offsetX = Random.Range(-floatingTextOffsetRange.x, floatingTextOffsetRange.x);
        float offsetY = Random.Range(-floatingTextOffsetRange.y, floatingTextOffsetRange.y);
        rect.anchoredPosition = new Vector2(offsetX, offsetY);

        TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = floatingTextFontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.raycastTarget = false;

        rect.localScale = Vector3.one * floatingTextScale * scaleMultiplier;

        StartCoroutine(AnimateFloatingText(rect, tmp));
    }

    private IEnumerator AnimateFloatingText(RectTransform rect, TMP_Text tmp)
    {
        Vector2 start = rect.anchoredPosition;
        Vector2 end = start + Vector2.up * floatingTextRise;

        Color startColor = tmp.color;
        Vector3 startScale = rect.localScale;
        Vector3 peakScale = startScale * 1.12f;

        float t = 0f;
        while (t < floatingTextLifetime)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / floatingTextLifetime);

            rect.anchoredPosition = Vector2.Lerp(start, end, p);
            rect.localScale = Vector3.Lerp(
                p < 0.25f ? startScale : peakScale,
                startScale * 0.88f,
                p);

            Color c = startColor;
            c.a = 1f - p;
            tmp.color = c;

            yield return null;
        }

        Destroy(rect.gameObject);
    }

    private void StartUiShake(float duration, float magnitude)
    {
        if (!enableUiShake || uiShakeRoot == null)
            return;

        if (_uiShakeRoutine != null)
        {
            StopCoroutine(_uiShakeRoutine);
            uiShakeRoot.anchoredPosition = _uiShakeBasePos;
        }

        _uiShakeRoutine = StartCoroutine(ShakeUiRoutine(duration, magnitude));
    }

    private IEnumerator ShakeUiRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float strength = 1f - Mathf.Clamp01(elapsed / duration);

            Vector2 offset = Random.insideUnitCircle * magnitude * strength;
            uiShakeRoot.anchoredPosition = _uiShakeBasePos + offset;

            yield return null;
        }

        uiShakeRoot.anchoredPosition = _uiShakeBasePos;
        _uiShakeRoutine = null;
    }

    private float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }

    private float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}