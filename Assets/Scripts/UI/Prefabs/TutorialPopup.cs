using UnityEngine;
using TMPro;
using BirdCafe.Shared;
using BirdCafe.Shared.ViewModels;
using Ricimi;

namespace BirdCafe.UI.Meta
{
    /// <summary>
    /// Attached to the "Tutorial_Popup_Prefab".
    /// Handles fetching data, populating the list, and the Continue button.
    /// </summary>
    public class TutorialPopup : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("The main header text for the popup.")]
        [SerializeField] private TMP_Text mainTitleText;

        [Tooltip("The parent transform where step items will be spawned.")]
        [SerializeField] private Transform tutorialStepContainer;

        [Tooltip("The prefab representing a single step (Must have TutorialStepUI component).")]
        [SerializeField] private GameObject tutorialStepPrefab;

        private void Start()
        {
            // We use Start here because this object is instantiated dynamically
            RefreshData();
        }

        private void RefreshData()
        {
            // 1. Get Data from Engine
            var vm = BirdCafeGame.Instance.GetTutorialContent();
            if (vm == null) return;

            // 2. Set Main Header
            if (mainTitleText != null)
            {
                mainTitleText.text = vm.Title;
            }

            // 3. Clear existing items
            if (tutorialStepContainer != null)
            {
                foreach (Transform child in tutorialStepContainer)
                {
                    Destroy(child.gameObject);
                }
            }

            // 4. Instantiate new steps
            if (tutorialStepPrefab != null && tutorialStepContainer != null)
            {
                foreach (var stepData in vm.Steps)
                {
                    GameObject stepObj = Instantiate(tutorialStepPrefab, tutorialStepContainer);

                    var stepScript = stepObj.GetComponent<TutorialStepUI>();
                    if (stepScript != null)
                    {
                        stepScript.Initialize(stepData);
                    }
                }
            }
        }

        /// <summary>
        /// Hook this to your "Got it" / "Continue" button OnClick event IN THE PREFAB.
        /// </summary>
        public void OnContinueClicked()
        {
            // 1. Close the visual popup using Ricimi's animation system
            var popup = GetComponent<Popup>();
            if (popup != null)
            {
                popup.Close();
            }
            else
            {
                Destroy(gameObject);
            }

            // 2. Tell the Engine the tutorial is finished.
            BirdCafeGame.Instance.CompleteTutorial();
        }
    }
}