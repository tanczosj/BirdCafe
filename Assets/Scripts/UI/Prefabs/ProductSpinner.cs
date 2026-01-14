using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace BirdCafe.UI.Components
{
    public class ProductSpinner : MonoBehaviour
    {
        [Header("Data Configuration")]
        [SerializeField] private string itemName = "Item";
        [SerializeField] private float costPerItem = 1.0f;
        [SerializeField] private int amount = 0;

        [Header("UI References")]
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text priceLabel; // Calculated: Amount * Cost

        [Header("Input Controls")]
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button upButton;
        [SerializeField] private Button downButton;

        // Optional: Reference to placeholder if you need to manipulate it directly, 
        // though TMP_InputField usually handles this automatically.
        [SerializeField] private TMP_Text placeholderLabel;

        // --- EVENTS ---
        /// <summary>
        /// Fired whenever the amount changes (via button or typing). 
        /// Passes the new Amount and the Total Cost for this line item.
        /// </summary>
        public event Action<int, decimal> OnValueChanged;

        // --- PROPERTIES ---

        public string ItemName
        {
            get => itemName;
            set
            {
                itemName = value;
                UpdateLabels();
            }
        }

        public float CostPerItem
        {
            get => costPerItem;
            set
            {
                costPerItem = value;
                UpdateLabels();
            }
        }

        public int Amount
        {
            get => amount;
            set
            {
                // Clamp between 0 and 9999
                int clamped = Mathf.Clamp(value, 0, 9999);

                if (amount != clamped)
                {
                    amount = clamped;
                    UpdateInputText();
                    UpdateLabels();
                    NotifyChange();
                }
            }
        }

        // --- LIFECYCLE ---

        private void OnValidate()
        {
            // Updates UI in Editor immediately when you change Inspector values
            UpdateLabels();
            // We don't update InputText in OnValidate to avoid dirtying scene too aggressively,
            // but we ensure name/price are correct.
        }

        private void Awake()
        {
            // Wire up buttons
            if (upButton) upButton.onClick.AddListener(Increment);
            if (downButton) downButton.onClick.AddListener(Decrement);

            // Wire up input field
            if (inputField)
            {
                // Validate input as Integer only
                inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
                inputField.characterLimit = 4; // Max 9999

                // Use onEndEdit to finalize value (prevents weirdness while typing)
                inputField.onEndEdit.AddListener(OnInputChanged);
            }

            // Init UI
            UpdateLabels();
            UpdateInputText();
        }

        // --- LOGIC ---

        private void Increment()
        {
            Amount++;
        }

        private void Decrement()
        {
            Amount--;
        }

        private void OnInputChanged(string text)
        {
            if (int.TryParse(text, out int result))
            {
                Amount = result; // Property setter handles clamping and refreshing
            }
            else
            {
                // Invalid input (empty or symbols), reset to 0
                Amount = 0;
            }

            // Force the text box to match the clamped/cleaned Amount
            UpdateInputText();
        }

        private void UpdateInputText()
        {
            if (inputField != null)
            {
                // SetTextWithoutNotify prevents infinite loops where 
                // changing text triggers the listener again
                inputField.SetTextWithoutNotify(amount.ToString());
            }
        }

        private void UpdateLabels()
        {
            if (nameLabel != null)
                nameLabel.text = itemName;

            if (priceLabel != null)
            {
                decimal total = (decimal)amount * (decimal)costPerItem;
                priceLabel.text = $"${total:F2}";
            }
        }

        private void NotifyChange()
        {
            decimal total = (decimal)amount * (decimal)costPerItem;
            OnValueChanged?.Invoke(amount, total);
        }
    }
}