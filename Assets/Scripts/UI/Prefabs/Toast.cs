using UnityEngine;
using TMPro;

namespace BirdCafe.UI.Components
{
    public class Toast : MonoBehaviour
    {
        [Header("Content")]
        [Tooltip("The header text of the toast notification.")]
        [SerializeField] private string title = "Alert";

        [Tooltip("The body text of the toast notification.")]
        [TextArea(2, 4)] // Makes the box bigger in Inspector
        [SerializeField] private string message = "Something happened.";

        [Header("UI References")]
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text messageLabel;

        // --- PROPERTIES ---

        public string Title
        {
            get => title;
            set
            {
                title = value;
                UpdateVisuals();
            }
        }

        public string Message
        {
            get => message;
            set
            {
                message = value;
                UpdateVisuals();
            }
        }

        // --- LIFECYCLE ---

        private void OnValidate()
        {
            // Updates text immediately when you type in the Inspector
            UpdateVisuals();
        }

        private void Awake()
        {
            UpdateVisuals();
        }

        // --- HELPER ---

        /// <summary>
        /// Sets both fields at once to ensure a clean update.
        /// </summary>
        public void Initialize(string newTitle, string newMessage)
        {
            title = newTitle;
            message = newMessage;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (titleLabel != null)
                titleLabel.text = title;

            if (messageLabel != null)
                messageLabel.text = message;
        }
    }
}