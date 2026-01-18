using BirdCafe.Shared;
using Ricimi;
using System.Collections;
using UnityEngine;

namespace BirdCafe.UI.Meta
{
    /// <summary>
    /// Attached to the "Panel_Tutorial".
    /// Automatically triggers the PopupOpener when this panel is enabled.
    /// </summary>
    [RequireComponent(typeof(PopupOpener))]
    public class TutorialUI : MonoBehaviour
    {
        private PopupOpener _popupOpener;

        private void Awake()
        {
            _popupOpener = GetComponent<PopupOpener>();
        }

        private void OnEnable()
        {
            if (_popupOpener != null)
            {
                // We wait one frame because Ricimi's PopupOpener initializes its canvas 
                // reference in Start(), which happens AFTER OnEnable.
                StartCoroutine(OpenPopupRoutine());
            }
        }

        private IEnumerator OpenPopupRoutine()
        {
            yield return null; // Wait for Start() to run on the Opener
            _popupOpener.OpenPopup();
        }
        public void OnCompleteClicked()
        {
            // Tells the engine the tutorial is done.
            // Engine will fire OnScreenChanged -> DayIntro
            BirdCafeGame.Instance.CompleteTutorial();
        }
    }
}

