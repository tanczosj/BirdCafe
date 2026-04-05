using TMPro;
using UnityEngine;

namespace BirdCafe.UI.Reporting
{
    /// <summary>
    /// Simple reusable UI item that displays one left-aligned label and one right-aligned value.
    /// </summary>
    public sealed class ReportValueRowUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private TMP_Text valueText;

        /// <summary>
        /// Populates the row with already-formatted display text.
        /// </summary>
        /// <param name="label">Left-hand label text.</param>
        /// <param name="value">Right-hand value text.</param>
        public void Bind(string label, string value)
        {
            if (labelText != null)
            {
                labelText.text = label ?? string.Empty;
            }

            if (valueText != null)
            {
                valueText.text = value ?? string.Empty;
            }
        }
    }
}
