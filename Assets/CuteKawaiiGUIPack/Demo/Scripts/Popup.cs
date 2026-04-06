using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Ricimi
{
    public class Popup : MonoBehaviour
    {
        [Header("Background")]
        public bool useBackground = true;
        public Color backgroundTint = new Color(10f / 255f, 10f / 255f, 10f / 255f, 0.35f);

        [Tooltip("A camera output texture that shows the scene behind the popup.")]
        public RenderTexture blurredBackgroundTexture;

        [Tooltip("Optional blur material for the RawImage.")]
        public Material blurMaterial;

        [Header("Timing")]
        public float destroyTime = 0.5f;

        [Header("Sorting")]
        public int popupSortingOrder = 9;
        public int backgroundSortingOrder = 4;

        private GameObject m_background;

        public void Open()
        {
            EnsurePopupCanvasSorting();

            if (useBackground)
            {
                AddBackground();
            }
        }

        public void Close()
        {
            var animator = GetComponent<Animator>();
            if (animator != null && animator.GetCurrentAnimatorStateInfo(0).IsName("Open"))
            {
                animator.Play("Close");
            }

            if (useBackground)
            {
                RemoveBackground();
            }

            StartCoroutine(RunPopupDestroy());
        }

        private void EnsurePopupCanvasSorting()
        {
            var parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null)
            {
                Debug.LogError($"Popup '{name}' could not find a parent Canvas.");
                return;
            }

            var rootCanvas = parentCanvas.rootCanvas;

            var popupCanvas = GetComponent<Canvas>();
            if (popupCanvas == null)
            {
                popupCanvas = gameObject.AddComponent<Canvas>();
            }

            popupCanvas.overrideSorting = true;
            popupCanvas.sortingLayerID = rootCanvas.sortingLayerID;
            popupCanvas.sortingOrder = popupSortingOrder;

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private IEnumerator RunPopupDestroy()
        {
            yield return new WaitForSeconds(destroyTime);

            if (m_background != null)
            {
                Destroy(m_background);
            }

            Destroy(gameObject);
        }

        private void AddBackground()
        {
            var parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null)
            {
                Debug.LogError($"Popup '{name}' could not find a parent Canvas.");
                return;
            }

            var rootCanvas = parentCanvas.rootCanvas;

            m_background = new GameObject("PopupBackground");

            var rectTransform = m_background.AddComponent<RectTransform>();
            var rawImage = m_background.AddComponent<RawImage>();

            m_background.transform.SetParent(rootCanvas.transform, false);
            m_background.transform.localScale = Vector3.one;

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;

            // Show the captured scene texture.
            rawImage.texture = blurredBackgroundTexture;
            rawImage.color = backgroundTint;
            rawImage.raycastTarget = true;

            if (blurMaterial != null)
            {
                rawImage.material = new Material(blurMaterial);
            }

            var bgCanvas = m_background.AddComponent<Canvas>();
            bgCanvas.overrideSorting = true;
            bgCanvas.sortingLayerID = rootCanvas.sortingLayerID;
            bgCanvas.sortingOrder = backgroundSortingOrder;

            if (m_background.GetComponent<GraphicRaycaster>() == null)
            {
                m_background.AddComponent<GraphicRaycaster>();
            }

            rawImage.canvasRenderer.SetAlpha(0.0f);
            rawImage.CrossFadeAlpha(1.0f, 0.4f, false);
        }

        private void RemoveBackground()
        {
            if (m_background == null)
            {
                return;
            }

            var rawImage = m_background.GetComponent<RawImage>();
            if (rawImage != null)
            {
                rawImage.CrossFadeAlpha(0.0f, 0.2f, false);
            }
        }
    }
}