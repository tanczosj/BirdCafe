// Copyright (C) 2024 ricimi. All rights reserved.
// This code can only be used under the standard Unity Asset Store EULA,
// a copy of which is available at https://unity.com/legal/as-terms.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Ricimi
{
    // This class is responsible for popup management. Popups follow the traditional behavior of
    // automatically blocking the input on elements behind it and adding a background texture.
    public class Popup : MonoBehaviour
    {
        [Header("Background")]
        public bool useBackground = true;
        public Color backgroundColor = new Color(10.0f / 255.0f, 10.0f / 255.0f, 10.0f / 255.0f, 0.6f);

        [Header("Timing")]
        public float destroyTime = 0.5f;

        [Header("Sorting")]
        public int popupSortingOrder = 9;
        public int backgroundSortingOrder = 8;

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

            var bgTex = new Texture2D(1, 1);
            bgTex.SetPixel(0, 0, backgroundColor);
            bgTex.Apply();

            m_background = new GameObject("PopupBackground");

            var rectTransform = m_background.AddComponent<RectTransform>();
            var image = m_background.AddComponent<Image>();

            var rect = new Rect(0, 0, bgTex.width, bgTex.height);
            var sprite = Sprite.Create(bgTex, rect, new Vector2(0.5f, 0.5f), 1);

            image.material = new Material(image.material);
            image.material.mainTexture = bgTex;
            image.sprite = sprite;
            image.raycastTarget = true;

            m_background.transform.SetParent(rootCanvas.transform, false);
            m_background.transform.localScale = Vector3.one;

            // Stretch to fill the entire root canvas / screen.
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;

            var bgCanvas = m_background.AddComponent<Canvas>();
            bgCanvas.overrideSorting = true;
            bgCanvas.sortingLayerID = rootCanvas.sortingLayerID;
            bgCanvas.sortingOrder = backgroundSortingOrder;

            if (m_background.GetComponent<GraphicRaycaster>() == null)
            {
                m_background.AddComponent<GraphicRaycaster>();
            }

            image.canvasRenderer.SetAlpha(0.0f);
            image.CrossFadeAlpha(1.0f, 0.4f, false);
        }

        private void RemoveBackground()
        {
            if (m_background == null)
            {
                return;
            }

            var image = m_background.GetComponent<Image>();
            if (image != null)
            {
                image.CrossFadeAlpha(0.0f, 0.2f, false);
            }
        }
    }
}