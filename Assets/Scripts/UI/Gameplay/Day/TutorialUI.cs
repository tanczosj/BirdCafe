using System.Collections.Generic;
using BirdCafe.Shared;
using BirdCafe.Shared.ViewModels;
using TMPro;
using UnityEngine;

namespace BirdCafe.UI.Meta
{
    public class TutorialUI : MonoBehaviour
    {
        [Header("UI References")]
        public TMP_Text titleText;

        [Tooltip("The parent object where step items will be instantiated")]
        public Transform stepContainer;

        [Tooltip("A prefab with 2 Text components (Title, Description)")]
        public GameObject stepItemPrefab;

        private void OnEnable()
        {
            RefreshData();
        }

        private void RefreshData()
        {
            // 1. Get Data from Engine
            var vm = BirdCafeGame.Instance.GetTutorialContent();
            if (vm == null) return;

            // 2. Set Title
            if (titleText) titleText.text = vm.Title;

            // 3. Clear old steps (if any)
            foreach (Transform child in stepContainer)
            {
                Destroy(child.gameObject);
            }

            // 4. Instantiate new steps
            if (stepItemPrefab && stepContainer)
            {
                foreach (var step in vm.Steps)
                {
                    var go = Instantiate(stepItemPrefab, stepContainer);

                    // Simple binding: Assumes the prefab has a script OR we just find text components
                    // Here we try to find TMP_Text components by name or order
                    var texts = go.GetComponentsInChildren<TMP_Text>();

                    if (texts.Length >= 1) texts[0].text = step.Title;
                    if (texts.Length >= 2) texts[1].text = step.Description;
                }
            }
        }

        // Hook this to your "Got it" / "Continue" button in the Inspector
        public void OnCompleteClicked()
        {
            // Tells the engine the tutorial is done.
            // Engine will fire OnScreenChanged -> DayIntro
            BirdCafeGame.Instance.CompleteTutorial();
        }
    }
}