using BirdCafe.Shared;
using BirdCafe.Shared.ViewModels;
using System.Collections.Generic;
using UnityEngine;

// using BirdCafe.Shared.ViewModels;

public class PetPopupUI : MonoBehaviour
{
    [Header("Bird Sale List")]
    [SerializeField] private Transform birdSaleItemContainer;
    [SerializeField] private BirdSaleItemUI birdSaleItemPrefab;

    private readonly List<BirdSaleItemUI> spawnedItems = new List<BirdSaleItemUI>();

    private void Start()
    {
        RefreshBirdOffers();
    }

    public void RefreshBirdOffers()
    {
        ClearBirdOffers();

        if (BirdCafeGame.Instance == null)
        {
            Debug.LogError("BirdCafeGame.Instance is null.");
            return;
        }

        if (birdSaleItemContainer == null)
        {
            Debug.LogError("Bird sale item container is not assigned.");
            return;
        }

        if (birdSaleItemPrefab == null)
        {
            Debug.LogError("Bird sale item prefab is not assigned.");
            return;
        }

        List<PetStoreBirdOfferViewModel> offers = BirdCafeGame.Instance.GetPetStoreBirdOffers();

        if (offers == null || offers.Count == 0)
            return;

        for (int i = 0; i < offers.Count; i++)
        {
            PetStoreBirdOfferViewModel offer = offers[i];

            BirdSaleItemUI item = Instantiate(birdSaleItemPrefab, birdSaleItemContainer);
            item.Initialize(offer, HandleBuyBirdClicked);

            spawnedItems.Add(item);
        }
    }

    private void ClearBirdOffers()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i] != null)
                Destroy(spawnedItems[i].gameObject);
        }

        spawnedItems.Clear();

        // Optional safety cleanup in case old children exist that were not tracked.
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

        // Refresh after purchase so affordability / availability updates.
        RefreshBirdOffers();
    }
}