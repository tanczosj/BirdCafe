using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BirdCafe.Shared;
using BirdCafe.Shared.Enums;
using BirdCafe.Shared.ViewModels;

namespace BirdCafe.UI.Reporting
{
    /// <summary>
    /// Binds the shared <see cref="CostOfCareReportViewModel"/> to the Unity Cost of Care report popup.
    /// This class stays presentation-only: it requests data through <see cref="BirdCafeGame"/> and renders it.
    /// </summary>
    public sealed class CostOfCareReportPopupUI : MonoBehaviour
    {
        private enum ReportTab
        {
            Overview = 0,
            CareCosts = 1,
            BirdBreakdown = 2,
            CafeSales = 3
        }

        [Header("Optional Labels")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text scopeText;

        [Header("Time Filter")]
        [SerializeField] private TMP_Dropdown timeFilterDropdown;

        [Header("Tab Pages")]
        [SerializeField] private GameObject overviewPage;
        [SerializeField] private GameObject careCostsPage;
        [SerializeField] private GameObject birdBreakdownPage;
        [SerializeField] private GameObject cafeSalesPage;

        [Header("Overview Roots")]
        [SerializeField] private Transform overviewSummaryRowsRoot;
        [SerializeField] private Transform overviewExpenseRowsRoot;

        [Header("Care Costs Roots")]
        [SerializeField] private Transform careCategoriesRowsRoot;
        [SerializeField] private Transform careCostsTotalRowRoot;

        [Header("Bird Breakdown Roots")]
        [SerializeField] private Transform birdCardsRoot;
        [SerializeField] private Transform birdBreakdownTotalRowRoot;

        [Header("Cafe Sales Roots")]
        [SerializeField] private Transform salesTotalsRowsRoot;
        [SerializeField] private Transform unitsSoldRowsRoot;
        [SerializeField] private Transform trafficRowsRoot;

        [Header("UI Item Prefabs")]
        [SerializeField] private ReportValueRowUI reportValueRowPrefab;
        [SerializeField] private BirdBreakdownCardUI birdBreakdownCardPrefab;

        [Header("Optional Empty-State Labels")]
        [SerializeField] private TMP_Text noCareCostsText;
        [SerializeField] private TMP_Text noBirdsText;

        private readonly List<GameObject> _spawnedInstances = new List<GameObject>();

        private CostOfCareReportViewModel _currentReport;
        private CostOfCareReportTimeFilter _currentTimeFilter = CostOfCareReportTimeFilter.Today;
        private ReportTab _currentTab = ReportTab.Overview;
        private bool _isBindingDropdown;

        /// <summary>
        /// Initializes static UI state and syncs the dropdown to the report filter enum.
        /// </summary>
        private void Awake()
        {
            if (titleText != null)
            {
                titleText.text = "Cost of Care Report";
            }

            ConfigureTimeFilterDropdown();
            ShowTab(_currentTab);
        }

        /// <summary>
        /// Refreshes the report when the popup becomes active so the user always sees current values.
        /// </summary>
        private void OnEnable()
        {
            RefreshReport();
        }

        /// <summary>
        /// Public inspector hook for the Overview tab button.
        /// </summary>
        public void ShowOverviewTab()
        {
            ShowTab(ReportTab.Overview);
        }

        /// <summary>
        /// Public inspector hook for the Care Costs tab button.
        /// </summary>
        public void ShowCareCostsTab()
        {
            ShowTab(ReportTab.CareCosts);
        }

        /// <summary>
        /// Public inspector hook for the Bird Breakdown tab button.
        /// </summary>
        public void ShowBirdBreakdownTab()
        {
            ShowTab(ReportTab.BirdBreakdown);
        }

        /// <summary>
        /// Public inspector hook for the Cafe Sales tab button.
        /// </summary>
        public void ShowCafeSalesTab()
        {
            ShowTab(ReportTab.CafeSales);
        }

        /// <summary>
        /// Public refresh entry point. Call this after opening the popup or after game state changes.
        /// </summary>
        public void RefreshReport()
        {
            _currentReport = BirdCafeGame.Instance.GetCostOfCareReportViewModel(_currentTimeFilter);
            BindReport(_currentReport);
        }

        /// <summary>
        /// Inspector hook for the TMP dropdown OnValueChanged event.
        /// </summary>
        /// <param name="dropdownIndex">Selected dropdown index.</param>
        public void OnTimeFilterDropdownChanged(int dropdownIndex)
        {
            if (_isBindingDropdown)
            {
                return;
            }

            _currentTimeFilter = DropdownIndexToFilter(dropdownIndex);
            RefreshReport();
        }

        /// <summary>
        /// Rebuilds the popup from the current shared report payload.
        /// </summary>
        /// <param name="report">Shared report returned by <see cref="BirdCafeGame"/>.</param>
        private void BindReport(CostOfCareReportViewModel report)
        {
            if (report == null)
            {
                ClearGeneratedContent();

                if (scopeText != null)
                {
                    scopeText.text = string.Empty;
                }

                SetEmptyState(noCareCostsText, false);
                SetEmptyState(noBirdsText, false);
                return;
            }

            _currentTimeFilter = report.TimeFilter;
            SyncDropdownWithoutNotify(report.TimeFilter);

            if (scopeText != null)
            {
                scopeText.text = report.ScopeText ?? string.Empty;
            }

            ClearGeneratedContent();

            BuildOverview(report.Overview);
            BuildCareCosts(report.CareCosts);
            BuildBirdBreakdown(report.BirdBreakdown);
            BuildCafeSales(report.CafeSales);

            ShowTab(_currentTab);
        }

        /// <summary>
        /// Builds the Overview tab using value rows instead of hard-coded label references.
        /// </summary>
        private void BuildOverview(CostOfCareOverviewViewModel overview)
        {
            if (overview == null)
            {
                return;
            }

            CreateValueRow(overviewSummaryRowsRoot, "Current Balance", FormatCurrency(overview.CurrentBalance));
            CreateValueRow(overviewSummaryRowsRoot, "Total Sales", FormatCurrency(overview.TotalSales));
            CreateValueRow(overviewSummaryRowsRoot, "Total Expenses", FormatCurrency(overview.TotalExpenses));
            CreateValueRow(overviewSummaryRowsRoot, "Net Profit", FormatCurrency(overview.NetProfit));

            CreateValueRow(overviewExpenseRowsRoot, "Care Expenses Total", FormatCurrency(overview.CareExpensesTotal));
            CreateValueRow(overviewExpenseRowsRoot, "Inventory Expenses Total", FormatCurrency(overview.InventoryExpensesTotal));
            CreateValueRow(overviewExpenseRowsRoot, "Bird-Attributed Expenses Total", FormatCurrency(overview.BirdAttributedExpensesTotal));
            CreateValueRow(overviewExpenseRowsRoot, "Cafe-Wide Expenses Total", FormatCurrency(overview.CafeWideExpensesTotal));
        }

        /// <summary>
        /// Builds the Care Costs tab from the shared category rows.
        /// </summary>
        private void BuildCareCosts(CostOfCareCategoryBreakdownViewModel careCosts)
        {
            if (careCosts == null)
            {
                SetEmptyState(noCareCostsText, false);
                return;
            }

            bool hasCategories = careCosts.Categories != null && careCosts.Categories.Count > 0;
            SetEmptyState(noCareCostsText, !hasCategories);

            if (hasCategories)
            {
                foreach (var category in careCosts.Categories)
                {
                    string label = string.IsNullOrWhiteSpace(category.CategoryText)
                        ? category.Category.ToString()
                        : category.CategoryText;

                    CreateValueRow(careCategoriesRowsRoot, label, FormatCurrency(category.TotalCost));
                }
            }

            CreateValueRow(careCostsTotalRowRoot, "Total", FormatCurrency(careCosts.Total));
        }

        /// <summary>
        /// Builds the Bird Breakdown tab using one card per bird.
        /// </summary>
        private void BuildBirdBreakdown(CostOfCareBirdBreakdownViewModel birdBreakdown)
        {
            if (birdBreakdown == null)
            {
                SetEmptyState(noBirdsText, false);
                return;
            }

            bool hasBirds = birdBreakdown.Birds != null && birdBreakdown.Birds.Count > 0;
            SetEmptyState(noBirdsText, !hasBirds);

            CreateValueRow(birdBreakdownTotalRowRoot, "Total Bird Cost", FormatCurrency(birdBreakdown.Total));

            if (!hasBirds || birdBreakdownCardPrefab == null || birdCardsRoot == null)
            {
                return;
            }

            foreach (var bird in birdBreakdown.Birds)
            {
                var instance = Instantiate(birdBreakdownCardPrefab, birdCardsRoot);
                instance.Bind(bird);
                _spawnedInstances.Add(instance.gameObject);
            }
        }

        /// <summary>
        /// Builds the Cafe Sales tab from the shared sales view model.
        /// </summary>
        private void BuildCafeSales(CostOfCareCafeSalesViewModel cafeSales)
        {
            if (cafeSales == null)
            {
                return;
            }

            CreateValueRow(salesTotalsRowsRoot, "Total Sales", FormatCurrency(cafeSales.TotalSales));
            CreateValueRow(salesTotalsRowsRoot, "Coffee Sales", FormatCurrency(cafeSales.CoffeeSales));
            CreateValueRow(salesTotalsRowsRoot, "Baked Goods Sales", FormatCurrency(cafeSales.BakedGoodsSales));
            CreateValueRow(salesTotalsRowsRoot, "Merch Sales", FormatCurrency(cafeSales.MerchSales));

            CreateValueRow(unitsSoldRowsRoot, "Coffee Units Sold", cafeSales.CoffeeUnitsSold.ToString());
            CreateValueRow(unitsSoldRowsRoot, "Baked Goods Units Sold", cafeSales.BakedGoodsUnitsSold.ToString());
            CreateValueRow(unitsSoldRowsRoot, "Merch Units Sold", cafeSales.MerchUnitsSold.ToString());

            CreateValueRow(trafficRowsRoot, "Customers Served", cafeSales.CustomersServed.ToString());
            CreateValueRow(trafficRowsRoot, "Customers Lost", cafeSales.CustomersLost.ToString());
        }

        /// <summary>
        /// Activates the selected tab page and hides the others.
        /// </summary>
        private void ShowTab(ReportTab tab)
        {
            _currentTab = tab;

            SetPageActive(overviewPage, tab == ReportTab.Overview);
            SetPageActive(careCostsPage, tab == ReportTab.CareCosts);
            SetPageActive(birdBreakdownPage, tab == ReportTab.BirdBreakdown);
            SetPageActive(cafeSalesPage, tab == ReportTab.CafeSales);
        }

        /// <summary>
        /// Creates a simple label/value row under the provided root.
        /// </summary>
        private void CreateValueRow(Transform parent, string label, string value)
        {
            if (reportValueRowPrefab == null || parent == null)
            {
                return;
            }

            var instance = Instantiate(reportValueRowPrefab, parent);
            instance.Bind(label, value);
            _spawnedInstances.Add(instance.gameObject);
        }

        /// <summary>
        /// Destroys all runtime-instantiated rows and cards so the popup can be rebuilt from fresh shared data.
        /// </summary>
        private void ClearGeneratedContent()
        {
            for (int i = 0; i < _spawnedInstances.Count; i++)
            {
                if (_spawnedInstances[i] != null)
                {
                    Destroy(_spawnedInstances[i]);
                }
            }

            _spawnedInstances.Clear();
        }

        /// <summary>
        /// Configures the dropdown options to match the shared enum order.
        /// </summary>
        private void ConfigureTimeFilterDropdown()
        {
            if (timeFilterDropdown == null)
            {
                return;
            }

            _isBindingDropdown = true;
            timeFilterDropdown.ClearOptions();
            timeFilterDropdown.AddOptions(new List<string>
            {
                "Today",
                "This Week",
                "All Time"
            });
            timeFilterDropdown.SetValueWithoutNotify((int)_currentTimeFilter);
            _isBindingDropdown = false;
        }

        /// <summary>
        /// Updates the dropdown without re-triggering a refresh callback.
        /// </summary>
        private void SyncDropdownWithoutNotify(CostOfCareReportTimeFilter filter)
        {
            if (timeFilterDropdown == null)
            {
                return;
            }

            _isBindingDropdown = true;
            timeFilterDropdown.SetValueWithoutNotify((int)filter);
            _isBindingDropdown = false;
        }

        /// <summary>
        /// Converts the dropdown selection index into the shared report filter enum.
        /// </summary>
        private static CostOfCareReportTimeFilter DropdownIndexToFilter(int dropdownIndex)
        {
            switch (dropdownIndex)
            {
                case 1:
                    return CostOfCareReportTimeFilter.ThisWeek;
                case 2:
                    return CostOfCareReportTimeFilter.AllTime;
                default:
                    return CostOfCareReportTimeFilter.Today;
            }
        }

        /// <summary>
        /// Formats currency consistently with the rest of the game's UI mockups.
        /// </summary>
        private static string FormatCurrency(decimal amount)
        {
            return string.Format("${0:F2}", amount);
        }

        /// <summary>
        /// Applies active state to a page root if it exists.
        /// </summary>
        private static void SetPageActive(GameObject page, bool isActive)
        {
            if (page != null)
            {
                page.SetActive(isActive);
            }
        }

        /// <summary>
        /// Shows or hides an optional empty-state label.
        /// </summary>
        private static void SetEmptyState(TMP_Text label, bool isActive)
        {
            if (label != null)
            {
                label.gameObject.SetActive(isActive);
            }
        }
    }
}
