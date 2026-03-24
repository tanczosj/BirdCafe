using BirdCafe.Shared.ViewModels;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BirdProfileCardUI : MonoBehaviour
{
    [Serializable]
    public class SpeciesSpriteEntry
    {
        public string SpeciesId;
        public Sprite Sprite;
    }

    [Header("UI")]
    [SerializeField] private Image birdImage;
    [SerializeField] private TMP_Text nameText;

    [Header("Species Image Mapping")]
    [SerializeField] private List<SpeciesSpriteEntry> speciesSprites = new List<SpeciesSpriteEntry>();
    [SerializeField] private Sprite fallbackSprite;

    public void Initialize(BirdCareViewModel viewModel)
    {
        if (viewModel == null)
        {
            Debug.LogWarning("BirdProfileCardUI.Initialize called with null viewModel.");
            return;
        }

        if (nameText != null)
            nameText.text = viewModel.Name;

        if (birdImage != null)
            birdImage.sprite = GetSpriteForSpecies(viewModel.SpeciesId);
    }

    private Sprite GetSpriteForSpecies(string speciesId)
    {
        if (string.IsNullOrWhiteSpace(speciesId))
            return fallbackSprite;

        string cleanedSpeciesId = speciesId.Trim().ToLowerInvariant();

        foreach (var entry in speciesSprites)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.SpeciesId))
                continue;

            if (entry.SpeciesId.Trim().ToLowerInvariant() == cleanedSpeciesId)
                return entry.Sprite != null ? entry.Sprite : fallbackSprite;
        }

        Debug.LogWarning($"No sprite mapping found for speciesId '{speciesId}'.");
        return fallbackSprite;
    }
}