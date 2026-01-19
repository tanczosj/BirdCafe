using UnityEngine;
using TMPro;
using BirdCafe.Shared.ViewModels;

namespace BirdCafe.UI.Components
{
    public class BirdCareCard : MonoBehaviour
    {
        [Header("Text References")]
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text statusLabel;

        [Header("Stat Containers")]
        [SerializeField] private BirdStatContainer hungerStat;
        [SerializeField] private BirdStatContainer moodStat;
        [SerializeField] private BirdStatContainer energyStat;
        [SerializeField] private BirdStatContainer healthStat;

        [Header("Animation Configuration")]
        [Tooltip("How long the entry animation takes.")]
        [SerializeField] private float entryDuration = 0.6f;

        [Tooltip("How long the update animation takes (usually faster).")]
        [SerializeField] private float updateDuration = 0.3f;

        [Tooltip("The delay between bars for the entry animation.")]
        [SerializeField] private float staggerDelay = 0.15f;

        private BirdCareViewModel _viewModel;
        private string _lastBirdId = string.Empty;

        public BirdCareViewModel ViewModel
        {
            get => _viewModel;
            set
            {
                // Check if this is the same bird updating, or a new bird appearing
                // (Using IDs handles object pooling correctly)
                bool isSameBird = (value != null && value.Id == _lastBirdId);

                _viewModel = value;

                if (_viewModel != null)
                {
                    _lastBirdId = _viewModel.Id;
                    RefreshVisuals(isSameBird);
                }
            }
        }

        private void RefreshVisuals(bool isUpdate)
        {
            // 1. Basic Info
            if (nameLabel != null) nameLabel.text = _viewModel.Name;

            if (statusLabel != null)
            {
                if (_viewModel.IsSick) statusLabel.text = "Status: <color=red>SICK</color>";
                else if (_viewModel.WillRestTomorrow) statusLabel.text = "Status: <color=blue>Resting</color>";
                else statusLabel.text = "Status: <color=green>Happy</color>";
            }

            // 2. Animate Stats
            // If it's an update (same bird), we animate efficiently without delays.
            // If it's a new bird (or first load), we do the fancy staggered entry.

            if (isUpdate)
            {
                // Smooth update from current values
                if (hungerStat) hungerStat.AnimateUpdate(_viewModel.Hunger, updateDuration);
                if (moodStat) moodStat.AnimateUpdate(_viewModel.Mood, updateDuration);
                if (energyStat) energyStat.AnimateUpdate(_viewModel.Energy, updateDuration);
                if (healthStat) healthStat.AnimateUpdate(_viewModel.Health, updateDuration);
            }
            else
            {
                // Fancy entry from zero
                float currentDelay = 0f;

                if (hungerStat)
                {
                    hungerStat.StatName = "Hunger";
                    hungerStat.AnimateFromZero(_viewModel.Hunger, currentDelay, entryDuration);
                    currentDelay += staggerDelay;
                }
                if (moodStat)
                {
                    moodStat.StatName = "Mood";
                    moodStat.AnimateFromZero(_viewModel.Mood, currentDelay, entryDuration);
                    currentDelay += staggerDelay;
                }
                if (energyStat)
                {
                    energyStat.StatName = "Energy";
                    energyStat.AnimateFromZero(_viewModel.Energy, currentDelay, entryDuration);
                    currentDelay += staggerDelay;
                }
                if (healthStat)
                {
                    healthStat.StatName = "Health";
                    healthStat.AnimateFromZero(_viewModel.Health, currentDelay, entryDuration);
                }
            }
        }
    }
}