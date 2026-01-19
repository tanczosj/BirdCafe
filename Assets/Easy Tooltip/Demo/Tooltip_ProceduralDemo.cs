namespace PixeLadder.SimpleTooltip.Demo
{
    using UnityEngine;
    using System.Collections;

    /// <summary>
    /// An example script demonstrating how to add and configure tooltips
    /// to UI elements entirely from code using the static helper method.
    /// </summary>
    public class Tooltip_ProceduralDemo : MonoBehaviour
    {
        [Header("Scene References")]
        [Tooltip("Assign a UI Button here for the simple 'Info' tooltip.")]
        [SerializeField] private GameObject infoButton;

        [Tooltip("Assign a UI Button here for the 'Warning' tooltip.")]
        [SerializeField] private GameObject warningButton;

        [Tooltip("Assign a UI Button here for the 'Item Stat' tooltip.")]
        [SerializeField] private GameObject itemButton;

        [Header("Asset References")]
        [Tooltip("Assign a sample sprite here to be used for the item icon.")]
        [SerializeField] private Sprite swordIcon;

        private IEnumerator Start()
        {
            if (infoButton == null || warningButton == null || itemButton == null)
            {
                Debug.LogWarning("Tooltip Procedural Demo: Please assign all target GameObjects in the Inspector.", this);
                yield break;
            }

            // Wait one frame to guarantee the TooltipManager has initialized.
            yield return null;

            SetupTooltips();
        }

        private void SetupTooltips()
        {
            // --- Example 1: Simple informational tooltip ---
            TooltipTrigger.AddTooltip(infoButton, "This button provides helpful information about the game settings.");

            // --- Example 2: Styled warning tooltip ---
            var warningTrigger = TooltipTrigger.AddTooltip(warningButton, "Are you sure you want to delete your save file? This action cannot be undone.", "Warning!");
            if (warningTrigger != null)
            {
                warningTrigger.TitleColor = Color.yellow;
                warningTrigger.HoverDelay = 1.5f;
            }

            // --- Example 3: Complex item stat tooltip ---
            string itemName = "Sword of Clarity";
            string itemStats = "ATK: 15\nDEF: 5\n<color=green>+5% Crit Chance</color>";

            var itemTrigger = TooltipTrigger.AddTooltip(itemButton, itemStats, itemName, swordIcon);
            if (itemTrigger != null)
            {
                itemTrigger.TitleColor = new Color(0.5f, 0.8f, 1f); // Light blue
                itemTrigger.IconColor = Color.cyan;
            }
        }
    }
}