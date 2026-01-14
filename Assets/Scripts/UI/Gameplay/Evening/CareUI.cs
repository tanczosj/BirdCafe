using UnityEngine;
using UnityEngine.UI;
using BirdCafe.Shared;
using BirdCafe.Shared.ViewModels;
using BirdCafe.UI.Components;

namespace BirdCafe.UI.Gameplay.Evening
{
    public class CareUI : MonoBehaviour
    {
        [Header("Global Stats")]
        [SerializeField] private StatCounterUI moneyCounter;
        [SerializeField] private StatCounterUI popularityCounter;

        [Header("Bird Display")]
        [Tooltip("The container for the bird's stats and info.")]
        [SerializeField] private BirdCareCard birdCard;

        [Header("Actions")]
        [SerializeField] private Button feedButton; // Cost $5
        [SerializeField] private Button playButton; // Free
        [SerializeField] private Button vetButton;  // Cost $50
        [SerializeField] private Toggle restToggle;

        [Header("Navigation")]
        [SerializeField] private Button continueButton;

        // State
        private string _currentBirdId;

        private void OnEnable()
        {
            // 1. Hook up listeners (if not set in Inspector)
            // Ideally, set OnClick in Inspector, but we set Toggle dynamically here
            restToggle.onValueChanged.RemoveAllListeners();
            restToggle.onValueChanged.AddListener(OnRestToggled);

            Refresh();
        }

        private void Refresh()
        {
            // 2. Fetch Data
            var dashboard = BirdCafeGame.Instance.GetCareDashboard();
            if (dashboard == null) return;
            
            // 3. Update Globals
            if (moneyCounter) moneyCounter.AnimateValue((float)dashboard.CurrentMoney, 0.5f);
            if (popularityCounter) popularityCounter.AnimateValue((float)dashboard.CurrentPopularity, 0.5f);

            // 4. Update Bird (First one only for now)
            if (dashboard.Birds.Count > 0)
            {
                var bird = dashboard.Birds[0];
                _currentBirdId = bird.Id;

                // Push data to the card (Triggers animations inside the card script)
                if (birdCard) birdCard.ViewModel = bird;

                // Update Rest Toggle without triggering the event loop
                if (restToggle) restToggle.SetIsOnWithoutNotify(bird.WillRestTomorrow);

                // Update Button Interactivity based on funds
                UpdateButtons(dashboard.CurrentMoney, bird);
            }
        }

        private void UpdateButtons(decimal currentMoney, BirdCareViewModel bird)
        {
            // Hardcoded costs based on your prompt requirements
            // In a full implementation, these costs would come from GetAvailableActions()

            if (feedButton) feedButton.interactable = currentMoney >= 5.0m;

            // Play is free, but maybe disable if bird is sleeping/sick?
            if (playButton) playButton.interactable = true;

            // Vet is expensive
            if (vetButton) vetButton.interactable = currentMoney >= 50.0m;
        }

        // --- BUTTON HANDLERS ---

        public void OnFeedClicked()
        {
            // "Feed" must match the ActionId expected by CareManager in the Engine
            AttemptAction("Feed");
        }

        public void OnPlayClicked()
        {
            AttemptAction("Play");
        }

        public void OnVetClicked()
        {
            AttemptAction("Vet");
        }

        private void AttemptAction(string actionId)
        {
            if (string.IsNullOrEmpty(_currentBirdId)) return;

            bool success = BirdCafeGame.Instance.PerformCare(_currentBirdId, actionId);

            if (success)
            {
                // Refresh UI to show new stats and deducted money
                // Note: The Card script handles animating from current -> new value
                Refresh();
            }
        }

        public void OnRestToggled(bool isOn)
        {
            if (string.IsNullOrEmpty(_currentBirdId)) return;

            // Call Facade
            bool success = BirdCafeGame.Instance.ToggleRest(_currentBirdId);

            if (!success)
            {
                // If it failed (e.g. engine rule), revert the toggle visual
                restToggle.SetIsOnWithoutNotify(!isOn);
            }
            else
            {
                Refresh();
            }
        }

        public void OnContinueClicked()
        {
            BirdCafeGame.Instance.GoToPlanning();
        }
    }
}