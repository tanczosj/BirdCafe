using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BirdCafe.Shared;
using BirdCafe.Shared.ViewModels;
using BirdCafe.Shared.Enums;
using BirdCafe.UI.Components;

namespace BirdCafe.UI.Gameplay.Evening
{
    public class EveningPlanningUI : MonoBehaviour
    {
        [Header("Global Stats")]
        [SerializeField] private StatCounterUI moneyCounter;
        [SerializeField] private StatCounterUI popularityCounter;

        [Header("History Data")]
        [SerializeField] private SalesHistoryUI salesHistory;

        [Header("Inventory Inputs")]
        [SerializeField] private ProductSpinner coffeeSpinner;
        [SerializeField] private ProductSpinner bakedSpinner;
        [SerializeField] private ProductSpinner merchSpinner;

        [Header("Budgeting")]
        [SerializeField] private TMP_Text projectedCostLabel;
        [SerializeField] private Button startDayButton;

        private void OnEnable()
        {
            // 1. Initial Data Fetch
            RefreshFull();

            // 2. Subscribe to Spinner Events
            // We listen to changes so we can update the Engine and the Projected Cost in real-time
            if(coffeeSpinner) coffeeSpinner.OnValueChanged += (amt, cost) => OnInventoryChanged(ProductType.Coffee, amt);
            if(bakedSpinner)  bakedSpinner.OnValueChanged += (amt, cost) => OnInventoryChanged(ProductType.BakedGoods, amt);
            if(merchSpinner)  merchSpinner.OnValueChanged += (amt, cost) => OnInventoryChanged(ProductType.ThemedMerch, amt);
        }

        private void OnDisable()
        {
            // 3. Unsubscribe to prevent errors/leaks
            if(coffeeSpinner) coffeeSpinner.OnValueChanged -= (amt, cost) => OnInventoryChanged(ProductType.Coffee, amt);
            if(bakedSpinner)  bakedSpinner.OnValueChanged -= (amt, cost) => OnInventoryChanged(ProductType.BakedGoods, amt);
            if(merchSpinner)  merchSpinner.OnValueChanged -= (amt, cost) => OnInventoryChanged(ProductType.ThemedMerch, amt);
        }

        private void RefreshFull()
        {
            var data = BirdCafeGame.Instance.GetPlanningDashboard();
            if (data == null) return;

            // Globals
            if (moneyCounter) moneyCounter.Value = (float)data.CurrentMoney;
            if (popularityCounter) popularityCounter.Value = data.CurrentPopularity;

            // History Table
            if (salesHistory) salesHistory.History = data.RecentHistory;

            // Setup Spinners (Initialize values from Engine defaults/current state)
            SetupSpinner(coffeeSpinner, data, ProductType.Coffee, "Coffee Beans");
            SetupSpinner(bakedSpinner, data, ProductType.BakedGoods, "Pastries");
            SetupSpinner(merchSpinner, data, ProductType.ThemedMerch, "Themed Merch");

            // Update Cost Text
            UpdateBudgetDisplay(data);
        }

        private void SetupSpinner(ProductSpinner spinner, PlanningDashboardViewModel data, ProductType type, string displayName)
        {
            if (spinner == null) return;

            var item = data.Inventory.Find(x => x.Type == type);
            if (item != null)
            {
                // We set properties directly. 
                // Note: ProductSpinner handles its own UI updates when these change.
                spinner.ItemName = displayName;
                spinner.CostPerItem = (float)item.UnitCost;
                spinner.Amount = item.PlannedPurchase;
            }
        }

        private void OnInventoryChanged(ProductType type, int newAmount)
        {
            // 1. Send update to Engine (Update Draft Plan)
            bool isValid = BirdCafeGame.Instance.SetInventory(type, newAmount);

            if (isValid)
            {
                // 2. Fetch updated Dashboard to get the canonical Projected Cost
                var data = BirdCafeGame.Instance.GetPlanningDashboard();
                UpdateBudgetDisplay(data);
            }
        }

        private void UpdateBudgetDisplay(PlanningDashboardViewModel data)
        {
            if (projectedCostLabel == null) return;

            decimal cost = data.ProjectedCost;
            bool canAfford = data.CurrentMoney >= cost;

            // Format: "Projected Cost: $50.00"
            // Color Red if unaffordable
            string colorHex = canAfford ? "#18367B" : "#FF000E"; // White or Red
            projectedCostLabel.text = $"<color={colorHex}>${cost:F2}</color>";

            // Optional: Disable Start Button if unaffordable?
            // The Engine also prevents this, but UI feedback is nice.
            // if (startDayButton) startDayButton.interactable = canAfford; 
        }

        // --- BUTTON HANDLER ---

        public void OnStartDayClicked()
        {
            // Call Facade to finalize. 
            // If it fails (e.g. money), Facade handles the Toast message.
            BirdCafeGame.Instance.FinalizeDay();
        }
    }
}