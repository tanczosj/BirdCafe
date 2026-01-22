using BirdCafe.Shared;
using BirdCafe.Shared.ViewModels;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdCafe.Unity.UI.Chat
{
    public class ChatUIController : MonoBehaviour
    {
        [Header("Main Containers")]
        [SerializeField] private GameObject _menuPopupRoot;
        [SerializeField] private GameObject _qaChatPanel;

        [Header("Oracle Subject")]
        [SerializeField] private GameObject _oracleSubject;
        [SerializeField] private TMP_Text _oracleMessageText;

        [Header("Response Area")]
        [SerializeField] private Transform _responseContainer; 
        [SerializeField] private GameObject _responsePrefab; 

        private void Start()
        {
            BirdCafeGame.Instance.OnChatPopup += HandleChatPopupRequest;

            BirdCafeGame.Instance.FireChatPopup();
        }

        private void OnDestroy()
        {
            if (BirdCafeGame.Instance != null)
            {
                BirdCafeGame.Instance.OnChatPopup -= HandleChatPopupRequest;
            }
        }

        private void HandleChatPopupRequest()
        {
            _menuPopupRoot.SetActive(true);
            _qaChatPanel.SetActive(true);
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            var node = BirdCafeGame.Instance.GetCurrentChatNode();

            // 1. Reset Visibility
            if (_oracleSubject != null) _oracleSubject.SetActive(true);
            if (_responseContainer != null) _responseContainer.gameObject.SetActive(false);

            // 2. Update Oracle Text
            _oracleMessageText.text = node.OracleText;

            // 3. Clear old options (Safe Method)
            // We iterate backwards through children to safely check and destroy
            for (int i = _responseContainer.childCount - 1; i >= 0; i--)
            {
                var child = _responseContainer.GetChild(i);
                
                // Only destroy the object if it is a response button (has the script)
                // This preserves your static "What should I answer?" label.
                if (child.GetComponent<ChatResponseItem>() != null)
                {
                    Destroy(child.gameObject);
                }
            }

            // 4. Spawn new options
            for (int i = 0; i < node.Options.Count; i++)
            {
                var optionData = node.Options[i];
                var responseObj = Instantiate(_responsePrefab, _responseContainer);
                
                var script = responseObj.GetComponent<ChatResponseItem>();
                if (script != null)
                {
                    script.Setup(
                        optionData.ResponseText, 
                        optionData.NextStateId, 
                        i, 
                        null, 
                        OnResponseSelected
                    );
                }
            }
        }

        public void ShowResponses()
        {
            if (_oracleSubject != null) _oracleSubject.SetActive(false);
            if (_responseContainer != null) _responseContainer.gameObject.SetActive(true);
        }

        private void OnResponseSelected(int index)
        {
            var node = BirdCafeGame.Instance.GetCurrentChatNode();
            
            if (index < 0 || index >= node.Options.Count) return;

            var selectedOption = node.Options[index];

            if (selectedOption.IsExit)
            {
                CloseChat();
            }
            else
            {
                // Hide responses immediately
                if (_responseContainer != null) _responseContainer.gameObject.SetActive(false);
                if (_oracleSubject != null) _oracleSubject.SetActive(true);

                BirdCafeGame.Instance.SelectChatOption(index);
                RefreshDisplay();
            }
        }

        public void CloseChat()
        {
            _menuPopupRoot.SetActive(false);
        }
    }
}