using BirdCafe.Shared;
using BirdCafe.Shared.ViewModels;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PetPopupUI : MonoBehaviour
{
    [Header("Bird Sale List")]
    [SerializeField] private Transform birdSaleItemContainer;
    [SerializeField] private BirdSaleItemUI birdSaleItemPrefab;

    [Header("Sold Out Message")]
    [SerializeField] private SoldOutMessageUI soldOutMessagePrefab;
    [SerializeField] private string soldOutTitleMessage = "Sold Out";

    [TextArea]
    [SerializeField] private string soldOutDescriptionMessage = "Every bird for sale at Pete's Pet Shop already been purchased.";

    [Header("Optional Money Label")]
    [SerializeField] private TMP_Text moneyText;

    private readonly List<BirdSaleItemUI> spawnedItems = new List<BirdSaleItemUI>();

    private void OnEnable()
    {
        RefreshBirdOffers();
    }

    public void RefreshBirdOffers()
    {
        if (BirdCafeGame.Instance == null)
        {
            Debug.LogError("BirdCafeGame.Instance is null.", this);
            return;
        }

        if (birdSaleItemContainer == null)
        {
            Debug.LogError("Bird sale item container is not assigned.", this);
            return;
        }

        if (birdSaleItemPrefab == null)
        {
            Debug.LogError("Bird sale item prefab is not assigned.", this);
            return;
        }

        ClearBirdOffers();

        UpdateMoneyLabel();

        List<PetStoreBirdOfferViewModel> offers = BirdCafeGame.Instance.GetPetStoreBirdOffers();

        int spawnedBirdCount = 0;

        if (offers != null)
        {
            for (int i = 0; i < offers.Count; i++)
            {
                PetStoreBirdOfferViewModel offer = offers[i];

                if (offer == null)
                    continue;

                BirdSaleItemUI item = Instantiate(
                    birdSaleItemPrefab,
                    birdSaleItemContainer,
                    false
                );

                item.Initialize(offer, HandleBuyBirdClicked);

                spawnedItems.Add(item);
                spawnedBirdCount++;

                ForceImmediateLayout(item.transform as RectTransform);
            }
        }

        if (spawnedBirdCount <= 0)
            AddSoldOutMessage();

        ForceContainerLayout();
    }

    private void UpdateMoneyLabel()
    {
        if (moneyText == null)
            return;

        var dashboard = BirdCafeGame.Instance.GetPetStoreDashboard();
        moneyText.text = $"${dashboard.CurrentMoney:F2}";
    }

    private void AddSoldOutMessage()
    {
        if (soldOutMessagePrefab == null)
        {
            Debug.LogWarning("Sold Out Message Prefab is not assigned.", this);
            return;
        }

        SoldOutMessageUI soldOutMessage = Instantiate(
            soldOutMessagePrefab,
            birdSaleItemContainer,
            false
        );

        soldOutMessage.name = "Sold Out Message";

        soldOutMessage.SetMessages(
            soldOutTitleMessage,
            soldOutDescriptionMessage
        );

        ForceImmediateLayout(soldOutMessage.transform as RectTransform);
    }

    private void ClearBirdOffers()
    {
        spawnedItems.Clear();

        if (birdSaleItemContainer == null)
            return;

        for (int i = birdSaleItemContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(birdSaleItemContainer.GetChild(i).gameObject);
        }
    }

    private void HandleBuyBirdClicked(PetStoreBirdOfferViewModel offer)
    {
        if (offer == null)
            return;

        Debug.Log("Clicked buy for bird: " + offer.Name);

        BirdCafeGame.Instance.BuyPetStoreBird(offer.SpeciesId);

        RefreshBirdOffers();
    }

    private void ForceContainerLayout()
    {
        Canvas.ForceUpdateCanvases();

        ForceImmediateLayout(birdSaleItemContainer as RectTransform);

        if (birdSaleItemContainer != null)
            ForceImmediateLayout(birdSaleItemContainer.parent as RectTransform);

        Canvas.ForceUpdateCanvases();
    }

    private void ForceImmediateLayout(RectTransform rect)
    {
        if (rect == null)
            return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }
}