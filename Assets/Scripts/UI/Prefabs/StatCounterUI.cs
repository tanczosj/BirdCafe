using UnityEngine;
using TMPro;
using System.Collections;

namespace BirdCafe.UI.Components
{
    public class StatCounterUI : MonoBehaviour
    {
        [Header("UI Reference")]
        [Tooltip("The TextMeshPro label to update.")]
        [SerializeField] private TMP_Text textComponent;

        [Header("Settings")]
        [Tooltip("Format for the text. Use '$0.00' for money or '0' for integers.")]
        [SerializeField] private string formatString = "0";

        // Internal state
        private float _currentValue;
        private Coroutine _activeCoroutine;

        /// <summary>
        /// Gets or Sets the value immediately without animation.
        /// Setting this updates the text instantly.
        /// </summary>
        public float Value
        {
            get => _currentValue;
            set
            {
                _currentValue = value;
                UpdateText();
            }
        }

        /// <summary>
        /// Smoothly counts from the current value to the new value over time.
        /// </summary>
        /// <param name="newValue">The target value.</param>
        /// <param name="duration">Time in seconds to complete the change.</param>
        public void AnimateValue(float newValue, float duration)
        {
            // 1. Stop any existing animation so they don't fight
            if (_activeCoroutine != null)
            {
                StopCoroutine(_activeCoroutine);
            }

            // 2. Start the new animation
            _activeCoroutine = StartCoroutine(CountRoutine(_currentValue, newValue, duration));
        }

        private IEnumerator CountRoutine(float start, float end, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                
                // Calculate percentage (0.0 to 1.0)
                float t = Mathf.Clamp01(elapsed / duration);

                // SmoothStep makes the animation start slow, speed up, then end slow
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                // Interpolate
                float displayedValue = Mathf.Lerp(start, end, smoothT);

                // Update property (which updates text) but bypassing the coroutine cancel
                _currentValue = displayedValue;
                UpdateText();

                yield return null;
            }

            // Ensure we land exactly on the target number
            Value = end;
            _activeCoroutine = null;
        }

        private void UpdateText()
        {
            if (textComponent != null)
            {
                textComponent.text = _currentValue.ToString(formatString);
            }
        }
        
        // Optional: Validate component in editor
        private void OnValidate()
        {
            if (textComponent == null)
                textComponent = GetComponent<TMP_Text>();
        }
    }
}