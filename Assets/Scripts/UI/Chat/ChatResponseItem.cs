
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace BirdCafe.Unity.UI.Chat
{
    /// <summary>
    /// Component for the QA-Response prefab. 
    /// Handles the display of a single player response option and reports selection.
    /// </summary>
    public class ChatResponseItem : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TMP_Text _responseTextLabel;
        [SerializeField] private Toggle _toggle;

        private Action<int> _onOptionSelected;
        private int _optionIndex;
        
        // Guard to prevent setup logic from triggering events
        private bool _isInitialized = false; 

        /// <summary>
        /// Configures the response item with data from the engine.
        /// </summary>
        public void Setup(string text, string nextStateKey, int index, ToggleGroup group, Action<int> callback)
        {
            _isInitialized = false;

            _responseTextLabel.text = text;
            _optionIndex = index;
            _onOptionSelected = callback;

            // Important: We assign the group if provided, but generally we want null
            // to avoid infinite loops in dynamic lists.
            _toggle.group = group;
            
            // Turn it off without firing events (if possible), or handle via flag
            _toggle.isOn = false; 
            
            // Reset listener
            _toggle.onValueChanged.RemoveAllListeners();
            _toggle.onValueChanged.AddListener(OnToggleChanged);
            
            _isInitialized = true;
        }

        private void OnToggleChanged(bool isOn)
        {
            // Only fire if we are fully initialized and the toggle was turned ON
            if (_isInitialized && isOn)
            {
                // We reset it to false immediately so it acts like a button click
                // This allows the user to click it again if the UI didn't destroy it (though it usually does)
                _isInitialized = false; // Prevent double firing
                _toggle.isOn = false; 
                _isInitialized = true;

                _onOptionSelected?.Invoke(_optionIndex);
            }
        }
    }
}