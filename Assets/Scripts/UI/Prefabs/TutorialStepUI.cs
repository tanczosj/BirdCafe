using UnityEngine;
using TMPro;
using BirdCafe.Shared.ViewModels;

namespace BirdCafe.UI.Meta
{
    /// <summary>
    /// Component attached to a Tutorial Step prefab to display its data.
    /// </summary>
    public class TutorialStepUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("The text component for the step title (e.g. 'Step 1').")]
        [SerializeField] private TMP_Text titleText;

        [Tooltip("The text component for the instructions.")]
        [SerializeField] private TMP_Text descriptionText;

        /// <summary>
        /// Populates the UI elements with data from the view model.
        /// </summary>
        /// <param name="step">The data object containing title and description.</param>
        public void Initialize(TutorialStep step)
        {
            if (step == null) return;

            if (titleText != null)
            {
                titleText.text = step.Title;
            }

            if (descriptionText != null)
            {
                descriptionText.text = step.Description;
            }
        }
    }
}