using TMPro;
using UnityEngine;

namespace BirdCafe.Unity.UI
{
    [DisallowMultipleComponent]
    public sealed class SoldOutMessageUI : MonoBehaviour
    {
        [Header("Text References")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;

        [Header("Default Messages")]
        [SerializeField] private string defaultTitleMessage = "[SOLD OUT]";
        [SerializeField] private string defaultDescriptionMessage = "Every pet has already been purchased.";

        private void Awake()
        {
            ApplyDefaultMessages();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
                ApplyDefaultMessages();
        }

        public void SetMessages(string titleMessage, string descriptionMessage)
        {
            if (titleText != null)
                titleText.text = titleMessage;

            if (descriptionText != null)
                descriptionText.text = descriptionMessage;
        }

        public void ApplyDefaultMessages()
        {
            SetMessages(defaultTitleMessage, defaultDescriptionMessage);
        }
    }
}