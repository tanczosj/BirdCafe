using UnityEngine;
using TMPro;

namespace BirdCafe.UI.Gameplay.Day
{
    public class ThoughtBubble : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private float lifeTime = 2.0f;

        // UI uses pixels, so offset is larger (e.g., 50 pixels up)
        [SerializeField] private Vector2 floatOffset = new Vector2(0, 100f);

        private RectTransform _rect;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
        }

        public void Initialize(string text)
        {
            if (label != null) label.text = text;
            Destroy(gameObject, lifeTime);
            StartCoroutine(FloatUp());
        }

        private System.Collections.IEnumerator FloatUp()
        {
            Vector2 startPos = _rect.anchoredPosition;
            Vector2 endPos = startPos + floatOffset;
            float elapsed = 0;

            while (elapsed < lifeTime)
            {
                elapsed += Time.deltaTime;
                // Move in UI space
                _rect.anchoredPosition = Vector2.Lerp(startPos, endPos, elapsed / lifeTime);
                yield return null;
            }
        }
    }
}