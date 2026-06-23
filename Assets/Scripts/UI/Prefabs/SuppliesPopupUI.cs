using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BirdCafe.Shared;
using BirdCafe.Shared.Enums;
using BirdCafe.Shared.ViewModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SuppliesPopupUI : MonoBehaviour
{
    [Serializable]
    private class CatalogSupplySpriteEntry
    {
        [SerializeField] private string itemId;
        [SerializeField] private Sprite sprite;

        public string ItemId => itemId;
        public Sprite Sprite => sprite;
    }

    [Serializable]
    private class SoldOutMessageConfig
    {
        [SerializeField] private string title = "[SOLD OUT]";

        [TextArea]
        [SerializeField] private string description = "Everything in this tab has already been purchased.";

        public string Title => title;
        public string Description => description;
    }

    [Header("Content Roots")]
    [SerializeField] private Transform foodContent;
    [SerializeField] private Transform toysContent;
    [SerializeField] private Transform costumesContent;
    [SerializeField] private Transform surpriseContent;

    [Header("Item Prefab")]
    [SerializeField] private SupplySaleItemUI supplyItemPrefab;

    [Header("Sold Out Message Prefab")]
    [SerializeField] private SoldOutMessageUI soldOutMessagePrefab;

    [Header("Sold Out Messages")]
    [SerializeField] private SoldOutMessageConfig foodSoldOutMessage = new SoldOutMessageConfig();
    [SerializeField] private SoldOutMessageConfig toysSoldOutMessage = new SoldOutMessageConfig();
    [SerializeField] private SoldOutMessageConfig costumesSoldOutMessage = new SoldOutMessageConfig();
    [SerializeField] private SoldOutMessageConfig surpriseSoldOutMessage = new SoldOutMessageConfig();

    [Header("Catalog Supplies")]
    [SerializeField] private List<CatalogSupplySpriteEntry> catalogSupplies = new();

    [Header("Optional Special Egg UI")]
    [SerializeField] private Button openEggButton;
    [SerializeField] private TMP_Text openEggButtonText;
    [SerializeField] private TMP_Text eggRewardText;

    [Header("Optional Money Label")]
    [SerializeField] private TMP_Text moneyText;

    [Header("Scrolling")]
    [SerializeField] private ScrollRect suppliesScrollRect;

    private readonly List<SupplySaleItemUI> spawnedItems = new();
    private Dictionary<string, Sprite> supplySpriteLookup;

    private void Awake()
    {
        RebuildSupplySpriteLookup();
    }

    private void OnValidate()
    {
        RebuildSupplySpriteLookup();
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        ClearAllContent();

        var game = BirdCafeGame.Instance;
        var offers = game.GetPetStoreSupplyOffers();

        if (moneyText != null)
        {
            var dashboard = game.GetPetStoreDashboard();
            moneyText.text = $"${dashboard.CurrentMoney:F2}";
        }

        int foodItemCount = 0;
        int toysItemCount = 0;
        int costumesItemCount = 0;
        int surpriseItemCount = 0;

        foreach (var offer in offers)
        {
            if (!offer.Buyable)
                continue;

            Transform parent = GetTabContent(offer.SupplyType);

            if (parent == null)
                continue;

            var item = Instantiate(supplyItemPrefab, parent, false);
            spawnedItems.Add(item);

            item.Bind(
                offer,
                OnBuyClicked,
                ResolveSupplySprite(offer.ItemId)
            );

            IncrementSpawnCount(
                offer.SupplyType,
                ref foodItemCount,
                ref toysItemCount,
                ref costumesItemCount,
                ref surpriseItemCount
            );

            ForceImmediateLayout(item.transform as RectTransform);
        }

        AddSoldOutMessageIfEmpty(foodContent, foodItemCount, foodSoldOutMessage);
        AddSoldOutMessageIfEmpty(toysContent, toysItemCount, toysSoldOutMessage);
        AddSoldOutMessageIfEmpty(costumesContent, costumesItemCount, costumesSoldOutMessage);
        AddSoldOutMessageIfEmpty(surpriseContent, surpriseItemCount, surpriseSoldOutMessage);

        SetupSpecialEggButton(offers);

        ForceAllContentLayouts();
    }

    private void IncrementSpawnCount(
        PetStoreSupplyType supplyType,
        ref int foodItemCount,
        ref int toysItemCount,
        ref int costumesItemCount,
        ref int surpriseItemCount)
    {
        switch (supplyType)
        {
            case PetStoreSupplyType.BirdFood:
                foodItemCount++;
                break;

            case PetStoreSupplyType.Toy:
                toysItemCount++;
                break;

            case PetStoreSupplyType.Costume:
                costumesItemCount++;
                break;

            case PetStoreSupplyType.SpecialEggToy:
                surpriseItemCount++;
                break;
        }
    }

    private void AddSoldOutMessageIfEmpty(
        Transform parent,
        int spawnedItemCount,
        SoldOutMessageConfig messageConfig)
    {
        if (parent == null)
            return;

        if (soldOutMessagePrefab == null)
            return;

        if (spawnedItemCount > 0)
            return;

        var soldOutMessage = Instantiate(soldOutMessagePrefab, parent, false);
        soldOutMessage.name = "Sold Out Message";

        string title = messageConfig != null
            ? messageConfig.Title
            : "[SOLD OUT]";

        string description = messageConfig != null
            ? messageConfig.Description
            : "Everything in this tab has already been purchased.";

        soldOutMessage.SetMessages(title, description);

        ForceImmediateLayout(soldOutMessage.transform as RectTransform);
    }

    private void ForceAllContentLayouts()
    {
        Canvas.ForceUpdateCanvases();

        ForceImmediateLayout(foodContent as RectTransform);
        ForceImmediateLayout(toysContent as RectTransform);
        ForceImmediateLayout(costumesContent as RectTransform);
        ForceImmediateLayout(surpriseContent as RectTransform);

        if (foodContent != null) ForceImmediateLayout(foodContent.parent as RectTransform);
        if (toysContent != null) ForceImmediateLayout(toysContent.parent as RectTransform);
        if (costumesContent != null) ForceImmediateLayout(costumesContent.parent as RectTransform);
        if (surpriseContent != null) ForceImmediateLayout(surpriseContent.parent as RectTransform);

        Canvas.ForceUpdateCanvases();
    }

    private void ForceImmediateLayout(RectTransform rect)
    {
        if (rect == null)
            return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

    private void RebuildSupplySpriteLookup()
    {
        supplySpriteLookup = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        if (catalogSupplies == null)
            return;

        foreach (var entry in catalogSupplies)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.ItemId))
                continue;

            supplySpriteLookup[entry.ItemId] = entry.Sprite;
        }
    }

    private Transform GetTabContent(PetStoreSupplyType supplyType)
    {
        switch (supplyType)
        {
            case PetStoreSupplyType.BirdFood:
                return foodContent;

            case PetStoreSupplyType.Toy:
                return toysContent;

            case PetStoreSupplyType.Costume:
                return costumesContent;

            case PetStoreSupplyType.SpecialEggToy:
                return surpriseContent;

            default:
                return null;
        }
    }

    private void OnBuyClicked(PetStoreSupplyOfferViewModel offer)
    {
        bool bought = BirdCafeGame.Instance.BuyPetStoreSupply(offer.ItemId, offer.SupplyType);

        if (bought)
            Refresh();
    }

    private void SetupSpecialEggButton(List<PetStoreSupplyOfferViewModel> offers)
    {
        if (openEggButton == null)
            return;

        var eggOffer = offers.FirstOrDefault(x => x.SupplyType == PetStoreSupplyType.SpecialEggToy);
        bool canOpen = eggOffer != null && eggOffer.OwnedQuantity > 0;

        openEggButton.onClick.RemoveAllListeners();
        openEggButton.interactable = canOpen;

        if (openEggButtonText != null)
        {
            openEggButtonText.text = canOpen
                ? $"Open owned Special Egg Toy (x{eggOffer.OwnedQuantity})"
                : "No Special Egg Toy Owned";
        }

        openEggButton.onClick.AddListener(OpenSpecialEggToy);
    }

    public void OpenSpecialEggToy()
    {
        var result = BirdCafeGame.Instance.OpenSpecialEggToy();

        if (eggRewardText != null)
        {
            eggRewardText.text = result.HasReward
                ? $"{result.RewardName} ({result.RewardTypeText})\n{result.RewardDescription}"
                : "No reward opened.";
        }

        Refresh();
    }

    private void ClearAllContent()
    {
        ClearContent(foodContent);
        ClearContent(toysContent);
        ClearContent(costumesContent);
        ClearContent(surpriseContent);

        spawnedItems.Clear();
    }

    private void ClearContent(Transform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    private Sprite ResolveSupplySprite(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return null;

        if (supplySpriteLookup == null)
            RebuildSupplySpriteLookup();

        return supplySpriteLookup.TryGetValue(itemId, out var sprite)
            ? sprite
            : null;
    }

    /// <summary>
    /// Resets the supplies list to the top when a tab becomes selected.
    /// Wire this to each tab Toggle's On Value Changed event.
    /// </summary>
    public void ResetScrollToTop(bool isSelected)
    {
        if (!isSelected || suppliesScrollRect == null)
            return;

        StartCoroutine(ResetScrollToTopNextFrame());
    }

    private IEnumerator ResetScrollToTopNextFrame()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();

        if (suppliesScrollRect.content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                suppliesScrollRect.content
            );
        }

        suppliesScrollRect.StopMovement();

        suppliesScrollRect.verticalNormalizedPosition = 1f;
    }
}