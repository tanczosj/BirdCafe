using System;
using System.Linq;
using System.Text;
using BirdCafe.Shared;
using BirdCafe.Shared.Enums;
using BirdCafe.Shared.Models.Reporting;
using BirdCafe.Shared.ViewModels;
using TMPro;
using UnityEngine;

namespace BirdCafe.UI.Reporting
{
    /// <summary>
    /// Unity-side TextMeshPro report renderer for BirdCafe.Shared expense reports.
    /// 
    /// Shared library responsibility:
    /// - build the report data
    /// 
    /// Unity/UI responsibility:
    /// - format that data into a readable multiline string for TMP
    /// 
    /// This keeps presentation out of BirdCafe.Shared and follows the facade boundary
    /// through BirdCafeGame.Instance.
    /// </summary>
    public class BirdExpenseReportTextUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_Text targetText;

        [Header("Report Source")]
        [Tooltip("When true, uses BirdCafeGame.Instance.GetBirdExpenseReport(...).")]
        [SerializeField] private bool useBirdSpecificReport;

        [Tooltip("Required only when Use Bird Specific Report is enabled.")]
        [SerializeField] private string birdId;

        [Header("Scope")]
        [SerializeField] private ExpenseReportScope scope = ExpenseReportScope.CurrentWeek;
        [SerializeField] private ExpenseReportGroupBy groupBy = ExpenseReportGroupBy.ByTransaction;

        [Tooltip("Only used when Scope is CustomDayRange.")]
        [SerializeField] private int startDayNumber = 1;

        [Tooltip("Only used when Scope is CustomDayRange.")]
        [SerializeField] private int endDayNumber = 7;

        [Header("Filters")]
        [SerializeField] private bool includeCareExpenses = true;
        [SerializeField] private bool includeInventoryExpenses = true;
        [SerializeField] private bool includeRunningTotal = true;

        [Tooltip("Optional category filter.")]
        [SerializeField] private bool useCategoryFilter;

        [SerializeField] private ExpenseCategory categoryFilter = ExpenseCategory.FoodAndSupplies;

        [Header("Refresh")]
        [SerializeField] private bool refreshOnEnable = true;

        [Tooltip("Maximum label width before trimming for table layout.")]
        [SerializeField] private int labelColumnWidth = 28;

        [Tooltip("Maximum secondary label width before trimming for table layout.")]
        [SerializeField] private int secondaryColumnWidth = 20;

        private void OnEnable()
        {
            if (refreshOnEnable)
            {
                RefreshReport();
            }
        }

        /// <summary>
        /// Rebuilds the report from BirdCafe.Shared and writes the formatted text into TMP.
        /// </summary>
        [ContextMenu("Refresh Report")]
        public void RefreshReport()
        {
            if (targetText == null)
            {
                Debug.LogWarning($"{nameof(BirdExpenseReportTextUI)} has no TMP_Text assigned.", this);
                return;
            }

            try
            {
                ExpenseReportRequest request = BuildRequest();

                ExpenseReportViewModel report = useBirdSpecificReport && !string.IsNullOrWhiteSpace(birdId)
                    ? BirdCafeGame.Instance.GetBirdExpenseReport(birdId, request)
                    : BirdCafeGame.Instance.GetExpenseReport(request);

                targetText.text = BuildReportText(report);
            }
            catch (Exception ex)
            {
                targetText.text = $"Failed to build expense report.\n{ex.Message}";
                Debug.LogException(ex, this);
            }
        }

        private ExpenseReportRequest BuildRequest()
        {
            var request = new ExpenseReportRequest
            {
                Scope = scope,
                GroupBy = groupBy,
                IncludeCareExpenses = includeCareExpenses,
                IncludeInventoryExpenses = includeInventoryExpenses,
                IncludeRunningTotal = includeRunningTotal
            };

            if (scope == ExpenseReportScope.CustomDayRange)
            {
                request.StartDayNumber = startDayNumber;
                request.EndDayNumber = endDayNumber;
            }

            if (useCategoryFilter)
            {
                request.ExpenseCategory = categoryFilter;
            }

            return request;
        }

        private string BuildReportText(ExpenseReportViewModel report)
        {
            if (report == null)
            {
                return "No expense report was returned.";
            }

            var sb = new StringBuilder(2048);

            // Header
            sb.AppendLine(report.Title ?? "Expense Report");
            sb.AppendLine(report.ScopeText ?? string.Empty);
            sb.AppendLine(new string('=', 72));

            // Summary
            sb.AppendLine($"Care Expenses : {FormatMoney(report.TotalCareExpenses)}");
            sb.AppendLine($"Bird Expenses : {FormatMoney(report.TotalBirdExpenses)}");
            sb.AppendLine($"Cafe Expenses : {FormatMoney(report.TotalCafeExpenses)}");
            sb.AppendLine($"Grand Total   : {FormatMoney(report.GrandTotalExpenses)}");

            // Warnings
            if (report.Warnings != null && report.Warnings.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Warnings:");
                foreach (string warning in report.Warnings.Where(w => !string.IsNullOrWhiteSpace(w)))
                {
                    sb.AppendLine($"- {warning}");
                }
            }

            sb.AppendLine();
            sb.AppendLine(BuildTableHeader());
            sb.AppendLine(new string('-', 72));

            if (report.Rows == null || report.Rows.Count == 0)
            {
                sb.AppendLine("No matching expense rows.");
                return sb.ToString();
            }

            foreach (ExpenseReportRowViewModel row in report.Rows)
            {
                sb.AppendLine(BuildRowLine(row));
            }

            return sb.ToString();
        }

        private string BuildTableHeader()
        {
            string label = PadOrTrim("Label", labelColumnWidth);
            string detail = PadOrTrim("Detail", secondaryColumnWidth);
            string amount = PadLeft("Amount", 10);

            if (includeRunningTotal)
            {
                string running = PadLeft("Running", 10);
                return $"{label} {detail} {amount} {running}";
            }

            return $"{label} {detail} {amount}";
        }

        private string BuildRowLine(ExpenseReportRowViewModel row)
        {
            string label = PadOrTrim(GetPrimaryLabel(row), labelColumnWidth);
            string detail = PadOrTrim(GetSecondaryLabel(row), secondaryColumnWidth);
            string amount = PadLeft(FormatMoney(row.Amount), 10);

            if (includeRunningTotal)
            {
                string running = PadLeft(FormatMoney(row.RunningTotal), 10);
                return $"{label} {detail} {amount} {running}";
            }

            return $"{label} {detail} {amount}";
        }

        private string GetPrimaryLabel(ExpenseReportRowViewModel row)
        {
            if (!string.IsNullOrWhiteSpace(row.Label))
            {
                return row.Label;
            }

            if (!string.IsNullOrWhiteSpace(row.CategoryText))
            {
                return row.CategoryText;
            }

            return "Expense";
        }

        private string GetSecondaryLabel(ExpenseReportRowViewModel row)
        {
            if (!string.IsNullOrWhiteSpace(row.BirdName))
            {
                return row.BirdName;
            }

            if (!string.IsNullOrWhiteSpace(row.SecondaryLabel))
            {
                return row.SecondaryLabel;
            }

            return $"Day {row.DayNumber}";
        }

        private static string FormatMoney(decimal amount)
        {
            return $"${amount:F2}";
        }

        private static string PadLeft(string value, int width)
        {
            value ??= string.Empty;
            return value.Length >= width ? value.Substring(0, width) : value.PadLeft(width);
        }

        private static string PadOrTrim(string value, int width)
        {
            value ??= string.Empty;

            if (width <= 0)
            {
                return value;
            }

            if (value.Length == width)
            {
                return value;
            }

            if (value.Length < width)
            {
                return value.PadRight(width);
            }

            if (width <= 3)
            {
                return value.Substring(0, width);
            }

            return value.Substring(0, width - 3) + "...";
        }
    }
}