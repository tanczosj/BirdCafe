using System;
using BirdCafe.Shared.ViewModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SupplySaleItemUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text effectText;
    [SerializeField] private TMP_Text ownedText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button buyButton;

    private PetStoreSupplyOfferViewModel currentOffer;
    private Action<PetStoreSupplyOfferViewModel> onBuy;

    public void Bind(
        PetStoreSupplyOfferViewModel offer,
        Action<PetStoreSupplyOfferViewModel> onBuyClicked,
        Sprite icon = null)
    {
        currentOffer = offer;
        onBuy = onBuyClicked;

        if (nameText != null)
            nameText.text = offer.Name;

        if (effectText != null)
            effectText.text = offer.EffectText;

        if (ownedText != null)
            ownedText.text = offer.OwnedQuantity > 0 ? $"(x{offer.OwnedQuantity})" : "";

        if (priceText != null)
            priceText.text = $"${offer.Price:F2}";

        if (iconImage != null)
            iconImage.sprite = icon;

        if (buyButton != null)
        {
            buyButton.interactable = offer.IsAffordable;
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(HandleBuyClicked);
        }
    }

    private void HandleBuyClicked()
    {
        onBuy?.Invoke(currentOffer);
    }
}