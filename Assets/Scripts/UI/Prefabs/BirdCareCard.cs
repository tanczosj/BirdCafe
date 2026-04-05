using UnityEngine;
using TMPro;
using BirdCafe.Shared.ViewModels;
using BirdCafe.Unity.Birds;

namespace BirdCafe.UI.Components
{
    public class BirdCareCard : MonoBehaviour
    {
        [Header("Text References")]
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text statusLabel;

        [Header("Animated Bird View")]
        [SerializeField] private BirdVisualController birdVisualController;

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
                bool isSameBird = value != null && value.Id == _lastBirdId;

                _viewModel = value;

                if (_viewModel == null)
                {
                    _lastBirdId = string.Empty;
                    ClearVisuals();
                    return;
                }

                _lastBirdId = _viewModel.Id ?? string.Empty;
                RefreshVisuals(isSameBird);
            }
        }

        private void OnDisable()
        {
            // Cards may be pooled or hidden. Hide the embedded bird view cleanly when disabled.
            if (birdVisualController != null)
            {
                birdVisualController.Unbind(hideVisual: true);
            }
        }

        private void RefreshVisuals(bool isUpdate)
        {
            if (_viewModel == null)
            {
                ClearVisuals();
                return;
            }

            if (nameLabel != null)
            {
                nameLabel.text = _viewModel.Name;
            }

            if (statusLabel != null)
            {
                if (_viewModel.IsSick)
                {
                    statusLabel.text = "Status: <color=red>SICK</color>";
                }
                else if (_viewModel.WillRestTomorrow)
                {
                    statusLabel.text = "Status: <color=blue>Resting</color>";
                }
                else
                {
                    statusLabel.text = "Status: <color=green>Happy</color>";
                }
            }

            RefreshBirdVisual(isUpdate);

            if (isUpdate)
            {
                if (hungerStat) hungerStat.AnimateUpdate(_viewModel.Hunger, updateDuration);
                if (moodStat) moodStat.AnimateUpdate(_viewModel.Mood, updateDuration);
                if (energyStat) energyStat.AnimateUpdate(_viewModel.Energy, updateDuration);
                if (healthStat) healthStat.AnimateUpdate(_viewModel.Health, updateDuration);
            }
            else
            {
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

        private void RefreshBirdVisual(bool isUpdate)
        {
            if (birdVisualController == null)
            {
                return;
            }

            if (_viewModel == null || string.IsNullOrWhiteSpace(_viewModel.Id))
            {
                birdVisualController.Unbind(hideVisual: true);
                return;
            }

            if (isUpdate && birdVisualController.IsBound && birdVisualController.BirdId == _viewModel.Id)
            {
                birdVisualController.RefreshVisualFromShared();
            }
            else
            {
                birdVisualController.BindToBird(_viewModel.Id, immediateRefresh: true);
            }
        }

        private void ClearVisuals()
        {
            if (nameLabel != null)
            {
                nameLabel.text = string.Empty;
            }

            if (statusLabel != null)
            {
                statusLabel.text = string.Empty;
            }

            if (birdVisualController != null)
            {
                birdVisualController.Unbind(hideVisual: true);
            }
        }
    }
}