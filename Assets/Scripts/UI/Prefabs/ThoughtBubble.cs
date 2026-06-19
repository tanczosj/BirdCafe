using UnityEngine;
using TMPro;

namespace BirdCafe.UI.Gameplay.Day
{
    public class ThoughtBubble : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private CanvasGroup canvasGroup;

        public void Initialize(string text)
        {
            if (label != null)
                label.text = text;

            // start invisible → fade in handled by stack system
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }

        public CanvasGroup GetCanvasGroup()
        {
            return canvasGroup;
        }
    }
}