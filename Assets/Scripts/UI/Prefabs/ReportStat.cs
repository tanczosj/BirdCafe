using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace BirdCafe.UI.Components
{
    public class ReportStat : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("The name of the item (e.g., 'Coffee'). Updates the label automatically.")]
        [SerializeField] private string itemName = "Item Name";

        [Header("Data (Initial / Debug)")]
        [SerializeField] private int _sold;
        [SerializeField] private int _totalInventory;

        [Header("UI References")]
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private Slider progressBar; 
        [SerializeField] private TMP_Text progressLabel;

        // Internal state
        private Coroutine _animationRoutine;

        // --- PROPERTIES ---

        /// <summary>
        /// Gets or sets the Sold amount. Setting this updates the UI instantly.
        /// </summary>
        public int Sold
        {
            get => _sold;
            set
            {
                if (_animationRoutine != null) StopCoroutine(_animationRoutine);
                _sold = value;
                UpdateVisualsImmediate();
            }
        }

        /// <summary>
        /// Gets or sets the Total Inventory amount. Setting this updates the UI instantly.
        /// </summary>
        public int TotalInventory
        {
            get => _totalInventory;
            set
            {
                if (_animationRoutine != null) StopCoroutine(_animationRoutine);
                _totalInventory = value;
                UpdateVisualsImmediate();
            }
        }

        // --- LIFECYCLE ---

        // Called automatically when you change values in the Inspector
        private void OnValidate()
        {
            UpdateNameLabel();
            UpdateVisualsImmediate();
        }

        private void Awake()
        {
            UpdateNameLabel();
            UpdateVisualsImmediate(); 
        }

        private void UpdateNameLabel()
        {
            if (nameLabel != null)
                nameLabel.text = itemName;
        }

        // --- ANIMATION ---

        /// <summary>
        /// Animates the bar and text from 0 (or current) to the target values.
        /// </summary>
        public void UpdateValue(int sold, int totalInventory, float duration)
        {
            if (_animationRoutine != null) StopCoroutine(_animationRoutine);
            
            // Update backing fields so Inspector shows the target values
            _sold = sold;
            _totalInventory = totalInventory;

            _animationRoutine = StartCoroutine(AnimateRoutine(sold, totalInventory, duration));
        }

        private IEnumerator AnimateRoutine(int targetSold, int total, float duration)
        {
            float elapsed = 0f;
            
            // Determine target fill ratio
            float targetFill = (total > 0) ? (float)targetSold / total : 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                // 1. Animate Bar
                if (progressBar != null)
                {
                    progressBar.value = Mathf.Lerp(0f, targetFill, smoothT);
                }

                // 2. Animate Text Numbers
                if (progressLabel != null)
                {
                    int currentDisplayedSold = Mathf.RoundToInt(Mathf.Lerp(0, targetSold, smoothT));
                    progressLabel.text = $"{currentDisplayedSold} / {total} Sold";
                }

                yield return null;
            }

            // Snap to exact final values
            UpdateVisualsImmediate();
        }

        private void UpdateVisualsImmediate()
        {
            // Calculate fill
            float fill = (_totalInventory > 0) ? (float)_sold / _totalInventory : 0f;

            if (progressBar != null) 
                progressBar.value = fill;
            
            if (progressLabel != null) 
                progressLabel.text = $"{_sold} / {_totalInventory} Sold";
        }
    }
}