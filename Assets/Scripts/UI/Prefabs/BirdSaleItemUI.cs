using BirdCafe.Shared.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BirdSaleItemUI : MonoBehaviour
{
    [Serializable]
    private class SpeciesSpriteEntry
    {
        public string speciesId;
        public Sprite sprite;
    }

    [Header("UI References")]
    [SerializeField] private Image birdImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text effectText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text priceText;

    [Header("Sprite Lookup")]
    [SerializeField] private Sprite fallbackSprite;
    [SerializeField] private List<SpeciesSpriteEntry> speciesSprites = new List<SpeciesSpriteEntry>();

    private PetStoreBirdOfferViewModel currentOffer;
    private Action<PetStoreBirdOfferViewModel> onBuyClicked;

    public PetStoreBirdOfferViewModel CurrentOffer => currentOffer;

    public void Initialize(
        PetStoreBirdOfferViewModel offerViewModel,
        Action<PetStoreBirdOfferViewModel> buyClickedCallback = null)
    {
        if (offerViewModel == null)
        {
            Debug.LogError($"{nameof(BirdSaleItemUI)} was initialized with a null offer.");
            return;
        }

        currentOffer = offerViewModel;
        onBuyClicked = buyClickedCallback;

        if (nameText != null)
            nameText.text = offerViewModel.Name ?? string.Empty;

        if (!string.IsNullOrEmpty(offerViewModel.RarityText))
            nameText.text += $" [{offerViewModel.RarityText}]";

        if (effectText != null)
            effectText.text = offerViewModel.EffectText ?? string.Empty;

        if (priceText != null)
            priceText.text = FormatPrice(offerViewModel.Price);

        if (birdImage != null)
        {
            birdImage.sprite = GetSpriteForSpeciesId(offerViewModel.SpeciesId);
            birdImage.enabled = birdImage.sprite != null;
        }

        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(HandleBuyClicked);
            buyButton.onClick.AddListener(HandleBuyClicked);
            buyButton.interactable = offerViewModel.IsAffordable;
        }
    }

    private void HandleBuyClicked()
    {
        if (currentOffer == null)
            return;

        onBuyClicked?.Invoke(currentOffer);
    }

    private Sprite GetSpriteForSpeciesId(string speciesId)
    {
        if (string.IsNullOrWhiteSpace(speciesId))
            return fallbackSprite;

        for (int i = 0; i < speciesSprites.Count; i++)
        {
            SpeciesSpriteEntry entry = speciesSprites[i];

            if (string.Equals(entry.speciesId, speciesId, StringComparison.OrdinalIgnoreCase))
                return entry.sprite != null ? entry.sprite : fallbackSprite;
        }

        return fallbackSprite;
    }

    private string FormatPrice(decimal price)
    {
        return "$" + price.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private void OnDestroy()
    {
        if (buyButton != null)
            buyButton.onClick.RemoveListener(HandleBuyClicked);
    }
}