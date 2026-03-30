using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BirdCafe.Shared.ViewModels;

namespace BirdCafe.UI.Components
{
    public class BirdCareCard : MonoBehaviour
    {
        [Serializable]
        private class SpeciesSpriteEntry
        {
            public string speciesId;
            public Sprite sprite;
        }

        [Header("Text References")]
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text statusLabel;

        [Header("Image References")]
        [SerializeField] private Image birdImage;
        [SerializeField] private Sprite fallbackSprite;

        [Header("Species Sprite Mapping")]
        [SerializeField] private List<SpeciesSpriteEntry> speciesSprites = new List<SpeciesSpriteEntry>();

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
        private Dictionary<string, Sprite> _speciesSpriteLookup;

        public BirdCareViewModel ViewModel
        {
            get => _viewModel;
            set
            {
                bool isSameBird = (value != null && value.Id == _lastBirdId);

                _viewModel = value;

                if (_viewModel != null)
                {
                    _lastBirdId = _viewModel.Id;
                    RefreshVisuals(isSameBird);
                }
            }
        }

        private void Awake()
        {
            BuildSpeciesLookup();
        }

        private void BuildSpeciesLookup()
        {
            _speciesSpriteLookup = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in speciesSprites)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.speciesId))
                    continue;

                if (_speciesSpriteLookup.ContainsKey(entry.speciesId))
                {
                    Debug.LogWarning($"Duplicate speciesId mapping found on {name}: {entry.speciesId}", this);
                    continue;
                }

                _speciesSpriteLookup.Add(entry.speciesId, entry.sprite);
            }
        }

        private void RefreshVisuals(bool isUpdate)
        {
            if (_viewModel == null)
                return;

            if (nameLabel != null)
                nameLabel.text = _viewModel.Name;

            if (statusLabel != null)
            {
                if (_viewModel.IsSick)
                    statusLabel.text = "Status: <color=red>SICK</color>";
                else if (_viewModel.WillRestTomorrow)
                    statusLabel.text = "Status: <color=blue>Resting</color>";
                else
                    statusLabel.text = "Status: <color=green>Happy</color>";
            }

            RefreshBirdImage();

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

        private void RefreshBirdImage()
        {
            if (birdImage == null)
                return;

            Sprite spriteToUse = fallbackSprite;

            if (!string.IsNullOrWhiteSpace(_viewModel.SpeciesId) &&
                _speciesSpriteLookup != null &&
                _speciesSpriteLookup.TryGetValue(_viewModel.SpeciesId, out var mappedSprite) &&
                mappedSprite != null)
            {
                spriteToUse = mappedSprite;
            }

            birdImage.sprite = spriteToUse;
        }
    }
}