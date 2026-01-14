using UnityEngine;
using TMPro;
using UnityEngine.UI;
using BirdCafe.Shared.ViewModels;

namespace BirdCafe.UI.Components
{
    public class HistoryRowUI : MonoBehaviour
    {
        [Header("Columns")]
        [SerializeField] private TMP_Text dayLabel;
        [SerializeField] private TMP_Text trafficLabel;
        [SerializeField] private TMP_Text coffeeLabel; // Format: "Sold (Waste)"
        [SerializeField] private TMP_Text bakedLabel;
        [SerializeField] private TMP_Text merchLabel;

        [Header("Styling")]
        [SerializeField] private Image background;
        [SerializeField] private Color oddRowColor = new Color(1f, 1f, 1f, 0.1f); // Faint white
        [SerializeField] private Color evenRowColor = new Color(0f, 0f, 0f, 0.1f); // Faint black

        public void Initialize(DailySalesHistoryModel data, bool isEven)
        {
            if (data == null) return;

            // 1. Set Text
            if (dayLabel) dayLabel.text = data.DayNumber.ToString();
            if (trafficLabel) trafficLabel.text = data.CustomersArrived.ToString();

            // Format: "Sold (Wasted)"
            // Example: "10 (2)"
            // If waste is high, maybe color it red? Let's keep it simple for now.
            if (coffeeLabel) coffeeLabel.text = $"{data.CoffeeSold} ({data.CoffeeWasted})";
            if (bakedLabel) bakedLabel.text = $"{data.BakedSold} ({data.BakedWasted})";

            // Merch has no waste
            if (merchLabel) merchLabel.text = data.MerchSold.ToString();

            // 2. Stripe the background for readability
            if (background)
            {
                background.color = isEven ? evenRowColor : oddRowColor;
            }
        }
    }
}