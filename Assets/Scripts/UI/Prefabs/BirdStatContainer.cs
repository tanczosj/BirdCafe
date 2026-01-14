using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace BirdCafe.UI.Components
{
    public class BirdStatContainer : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("The name of the stat (e.g., 'Hunger').")]
        [SerializeField] private string statName = "Stat Name";

        [Tooltip("Current value between 0 and 100.")]
        [Range(0, 100)]
        [SerializeField] private int value = 100;

        [Header("Visual Styling")]
        [SerializeField] private Gradient colorGradient;
        [Range(0f, 1f)]
        [SerializeField] private float shadingMultiplier = 0.75f;

        [Header("UI References")]
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TMP_Text valueLabel;

        [SerializeField] private Image mainImage;
        [SerializeField] private Image shadingImage;

        private Coroutine _animationRoutine;

        public string StatName
        {
            get => statName;
            set { statName = value; UpdateNameLabel(); }
        }

        public int Value
        {
            get => value;
            set
            {
                if (_animationRoutine != null) StopCoroutine(_animationRoutine);
                this.value = Mathf.Clamp(value, 0, 100);
                UpdateVisualsImmediate();
            }
        }

        private void OnValidate() { UpdateNameLabel(); UpdateVisualsImmediate(); }
        private void Awake() { UpdateNameLabel(); UpdateVisualsImmediate(); }

        // --- ANIMATION METHODS ---

        /// <summary>
        /// ENTRY ANIMATION: Resets bar to 0, waits for delay, fills to target.
        /// </summary>
        public void AnimateFromZero(int targetValue, float delay, float duration)
        {
            if (_animationRoutine != null) StopCoroutine(_animationRoutine);

            this.value = Mathf.Clamp(targetValue, 0, 100);

            // Reset visual state to 0 immediately
            if (progressBar != null) progressBar.value = 0;
            if (valueLabel != null) valueLabel.text = "0%";
            UpdateColors(0f);

            _animationRoutine = StartCoroutine(AnimateRoutine(0f, (float)this.value, delay, duration));
        }

        /// <summary>
        /// UPDATE ANIMATION: Animates from the CURRENT visual position to the new target.
        /// No delay is applied for responsiveness.
        /// </summary>
        public void AnimateUpdate(int targetValue, float duration)
        {
            if (_animationRoutine != null) StopCoroutine(_animationRoutine);

            this.value = Mathf.Clamp(targetValue, 0, 100);

            // Get current visual state to start from (handle interrupted animations smoothly)
            float startVal = (progressBar != null) ? progressBar.value * 100f : (float)this.value;

            _animationRoutine = StartCoroutine(AnimateRoutine(startVal, (float)this.value, 0f, duration));
        }

        private IEnumerator AnimateRoutine(float startVal, float endVal, float delay, float duration)
        {
            if (delay > 0)
                yield return new WaitForSeconds(delay);

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                float currentFloat = Mathf.Lerp(startVal, endVal, smoothT);
                float normalizedPercent = currentFloat / 100f;

                // Update UI elements
                if (progressBar != null)
                    progressBar.value = normalizedPercent;

                if (valueLabel != null)
                    valueLabel.text = $"{Mathf.RoundToInt(currentFloat)}%";

                UpdateColors(normalizedPercent);

                yield return null;
            }

            UpdateVisualsImmediate();
        }

        // --- HELPERS ---

        private void UpdateNameLabel()
        {
            if (nameLabel != null) nameLabel.text = statName;
        }

        private void UpdateVisualsImmediate()
        {
            float percent = (float)value / 100f;

            if (progressBar != null) progressBar.value = percent;
            if (valueLabel != null) valueLabel.text = $"{value}%";

            UpdateColors(percent);
        }

        private void UpdateColors(float percent)
        {
            Color c = Color.white;
            if (colorGradient != null) c = colorGradient.Evaluate(percent);
            Color shade = new Color(c.r * shadingMultiplier, c.g * shadingMultiplier, c.b * shadingMultiplier, c.a);

            if (mainImage != null) mainImage.color = c;
            if (shadingImage != null) shadingImage.color = shade;
        }
    }
}