using BirdCafe.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdCafe.UI.Gameplay.Day
{
    public class DayProgressUI : MonoBehaviour
    {
        [Header("Internal Components")]
        [Tooltip("The Slider component on the child 'Progress-Bar' object")]
        [SerializeField] private Slider progressBar;

        [Tooltip("The TextMeshPro label to show the time (e.g. '08:00 AM')")]
        [SerializeField] private TMP_Text timeLabel;

        [Header("Configuration")]
        [Tooltip("How long is the simulation day in seconds? Must match Engine config.")]
        [SerializeField] private float dayDurationSeconds = BirdCafeGame.Instance?.Controller?.CurrentState?.Config?.DayDurationSeconds ?? 45; 

        [Header("Shop Hours (for formatting)")]
        [SerializeField] private int startHour = 7; // 7 AM
        [SerializeField] private int endHour = 15;  // 3 PM

        /// <summary>
        /// Sets the current time in seconds (0 to dayDurationSeconds).
        /// Automatically updates the slider percentage.
        /// </summary>
        public float TimeSeconds
        {
            set
            {
                if (progressBar != null)
                {
                    dayDurationSeconds = BirdCafeGame.Instance.Controller.CurrentState.Config.DayDurationSeconds;

                    // Convert seconds (0-120) to normalized value (0.0-1.0)
                    float percentage = Mathf.Clamp01(value / dayDurationSeconds);
                    progressBar.value = percentage;
                }
            }
        }

        /// <summary>
        /// Sets the displayed text label directly.
        /// </summary>
        public string FormattedTime
        {
            set
            {
                if (timeLabel != null)
                {
                    timeLabel.text = value;
                }
            }
        }

        /// <summary>
        /// HELPER: Converts raw seconds into a formatted clock string 
        /// and updates both the slider and text in one go.
        /// </summary>
        /// <param name="currentSeconds">Time from the simulation timeline.</param>
        public void UpdateVisuals(float currentSeconds)
        {
            // 1. Update Slider via Property
            TimeSeconds = currentSeconds;

            // 2. Calculate Clock Time
            // Normalized time (0.0 to 1.0)
            float t = currentSeconds / dayDurationSeconds;
            
            // Map 0-1 to Hour range (e.g., 8 to 20)
            float currentHourDecimal = Mathf.Lerp(startHour, endHour, t);
            
            int hour = Mathf.FloorToInt(currentHourDecimal);
            int minute = Mathf.FloorToInt((currentHourDecimal - hour) * 60);

            // Format Logic (Simple AM/PM conversion)
            string period = (hour >= 12) ? "PM" : "AM";
            int displayHour = (hour > 12) ? hour - 12 : hour;
            if (displayHour == 0) displayHour = 12;

            // 3. Update Text via Property
            FormattedTime = $"{displayHour}:{minute:00} {period}";
        }
    }
}