using TMPro;
using UnityEngine;
using BirdCafe.Shared.ViewModels;

namespace BirdCafe.UI.Reporting
{
    /// <summary>
    /// Reusable UI item for the Bird Breakdown tab.
    /// One card represents all cost buckets attributed to a single bird.
    /// </summary>
    public sealed class BirdBreakdownCardUI : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private TMP_Text birdNameText;
        [SerializeField] private TMP_Text totalCostText;

        [Header("Body")]
        [SerializeField] private TMP_Text foodAndSuppliesText;
        [SerializeField] private TMP_Text vetCareText;
        [SerializeField] private TMP_Text toysAndActivitiesText;
        [SerializeField] private TMP_Text acquisitionText;
        [SerializeField] private TMP_Text otherText;

        /// <summary>
        /// Populates the card from the shared per-bird view model.
        /// </summary>
        /// <param name="bird">The per-bird row returned by the shared report.</param>
        public void Bind(CostOfCareBirdRowViewModel bird)
        {
            if (bird == null)
            {
                SetText(birdNameText, string.Empty);
                SetText(totalCostText, string.Empty);
                SetText(foodAndSuppliesText, string.Empty);
                SetText(vetCareText, string.Empty);
                SetText(toysAndActivitiesText, string.Empty);
                SetText(acquisitionText, string.Empty);
                SetText(otherText, string.Empty);
                return;
            }

            SetText(birdNameText, string.IsNullOrWhiteSpace(bird.BirdName) ? "Unknown Bird" : bird.BirdName);
            SetText(totalCostText, FormatCurrency(bird.TotalCost));
            SetText(foodAndSuppliesText, $"Food & Supplies: {FormatCurrency(bird.FoodAndSuppliesCost)}");
            SetText(vetCareText, $"Vet Care: {FormatCurrency(bird.VetCareCost)}");
            SetText(toysAndActivitiesText, $"Toys & Activities: {FormatCurrency(bird.ToysAndActivitiesCost)}");
            SetText(acquisitionText, $"Acquisition: {FormatCurrency(bird.AcquisitionCost)}");
            SetText(otherText, $"Other: {FormatCurrency(bird.OtherCosts)}");
        }

        /// <summary>
        /// Formats the card's currency values in the same simple dollar style used elsewhere in the report.
        /// </summary>
        private static string FormatCurrency(decimal amount)
        {
            return string.Format("${0:F2}", amount);
        }

        /// <summary>
        /// Avoids repetitive null checks when assigning optional TMP references.
        /// </summary>
        private static void SetText(TMP_Text textField, string value)
        {
            if (textField != null)
            {
                textField.text = value ?? string.Empty;
            }
        }
    }
}
