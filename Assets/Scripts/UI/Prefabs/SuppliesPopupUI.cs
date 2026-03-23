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
    [Header("Content Roots")]
    [SerializeField] private Transform foodContent;
    [SerializeField] private Transform toysContent;
    [SerializeField] private Transform costumesContent;
    [SerializeField] private Transform surpriseContent;

    [Header("Item Prefab")]
    [SerializeField] private SupplySaleItemUI supplyItemPrefab;

    [Header("Optional Special Egg UI")]
    [SerializeField] private Button openEggButton;
    [SerializeField] private TMP_Text openEggButtonText;
    [SerializeField] private TMP_Text eggRewardText;

    [Header("Optional Money Label")]
    [SerializeField] private TMP_Text moneyText;

    private readonly List<SupplySaleItemUI> spawnedItems = new();

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

        foreach (var offer in offers)
        {
            Transform parent = GetTabContent(offer.SupplyType);
            if (parent == null)
                continue;

            var item = Instantiate(supplyItemPrefab, parent);
            spawnedItems.Add(item);

            item.Bind(
                offer,
                OnBuyClicked,
                ResolveSupplySprite(offer.ItemId)
            );
        }

        SetupSpecialEggButton(offers);
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
        {
            Refresh();
        }
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
        // Replace this with your real sprite lookup.
        // Example: use a dictionary, Resources, Addressables, or serialized list.
        return null;
    }
}