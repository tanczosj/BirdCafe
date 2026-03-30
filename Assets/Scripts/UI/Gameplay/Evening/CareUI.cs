using BirdCafe.Shared;
using BirdCafe.Shared.ViewModels;
using BirdCafe.UI.Components;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private BirdProfileCardUI birdProfileCardPrefab;
        [SerializeField] private Transform cardContainer;
        [SerializeField] private GameObject birdSelectGrid;

        [Header("Actions")]
        [SerializeField] private Button feedButton;
        [SerializeField] private Button playButton;
        [SerializeField] private Button vetButton;
        [SerializeField] private Toggle restToggle;

        [Header("Navigation")]
        [SerializeField] private Button continueButton;

        private string _currentBirdId;

        private void OnEnable()
        {
            if (restToggle != null)
            {
                restToggle.onValueChanged.RemoveAllListeners();
                restToggle.onValueChanged.AddListener(OnRestToggled);
            }

            Refresh();
        }

        private void Refresh()
        {
            var dashboard = BirdCafeGame.Instance.GetCareDashboard();
            if (dashboard == null) return;

            if (moneyCounter) moneyCounter.AnimateValue((float)dashboard.CurrentMoney, 0.5f);
            if (popularityCounter) popularityCounter.AnimateValue((float)dashboard.CurrentPopularity, 0.5f);

            if (dashboard.Birds == null || dashboard.Birds.Count == 0)
                return;

            ShowBirds(dashboard.Birds);

            var selectedBird = FindBirdById(dashboard.Birds, _currentBirdId);

            if (selectedBird == null)
            {
                selectedBird = dashboard.Birds[0];
                _currentBirdId = selectedBird.Id;
            }

            LoadBirdIntoCard(selectedBird, dashboard.CurrentMoney);
        }

        private BirdCareViewModel FindBirdById(List<BirdCareViewModel> birds, string birdId)
        {
            if (birds == null || string.IsNullOrEmpty(birdId))
                return null;

            foreach (var bird in birds)
            {
                if (bird.Id == birdId)
                    return bird;
            }

            return null;
        }

        private void LoadBirdIntoCard(BirdCareViewModel bird, decimal currentMoney)
        {
            if (bird == null) return;

            _currentBirdId = bird.Id;

            if (birdCard)
                birdCard.ViewModel = bird;

            if (restToggle)
                restToggle.SetIsOnWithoutNotify(bird.WillRestTomorrow);

            UpdateButtons(currentMoney, bird);
        }

        public void ShowBirds(List<BirdCareViewModel> birds)
        {
            foreach (Transform child in cardContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var bird in birds)
            {
                var card = Instantiate(birdProfileCardPrefab, cardContainer);
                card.Initialize(bird);

                // Assumes the Care-Profile-Item prefab has a Button on the root
                // or somewhere in its children.
                var button = card.GetComponent<Button>();
                if (button == null)
                    button = card.GetComponentInChildren<Button>(true);

                if (button != null)
                {
                    string clickedBirdId = bird.Id;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => OnBirdSelected(clickedBirdId));
                }
            }
        }

        public void OnBirdSelected(string birdId)
        {
            var dashboard = BirdCafeGame.Instance.GetCareDashboard();
            if (dashboard == null || dashboard.Birds == null || dashboard.Birds.Count == 0)
                return;

            var selectedBird = FindBirdById(dashboard.Birds, birdId);
            if (selectedBird == null)
                return;

            _currentBirdId = selectedBird.Id;

            if (birdSelectGrid != null)
                birdSelectGrid.SetActive(false);

            LoadBirdIntoCard(selectedBird, dashboard.CurrentMoney);
        }

        public void ShowBirdSelectGrid()
        {
            if (birdSelectGrid != null)
                birdSelectGrid.SetActive(true);
        }

        private void UpdateButtons(decimal currentMoney, BirdCareViewModel bird)
        {
            if (feedButton) feedButton.interactable = currentMoney >= 5.0m;
            if (playButton) playButton.interactable = true;
            if (vetButton) vetButton.interactable = currentMoney >= 50.0m;
        }

        public void OnFeedClicked()
        {
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
                Refresh();
            }
        }

        public void OnRestToggled(bool isOn)
        {
            if (string.IsNullOrEmpty(_currentBirdId)) return;

            bool success = BirdCafeGame.Instance.ToggleRest(_currentBirdId);

            if (!success)
            {
                if (restToggle)
                    restToggle.SetIsOnWithoutNotify(!isOn);
            }
            else
            {
                Refresh();
            }
        }

        public void OnContinueClicked()
        {
            BirdCafeGame.Instance.GoToHub();
        }
    }
}