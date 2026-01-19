namespace PixeLadder.SimpleTooltip
{
    using System.Collections;
    using System.Text;
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;
#if ENABLE_INPUT_SYSTEM
    using UnityEngine.InputSystem;
#endif

    /// <summary>
    /// A singleton manager that controls the lifecycle of a single tooltip instance.
    /// It handles showing, hiding, positioning, resizing, and parenting logic.
    /// </summary>
    public class TooltipManager : MonoBehaviour
    {
        #region Static Instance
        public static TooltipManager Instance { get; private set; }
        #endregion

        #region Fields
        [Header("Core Configuration")]
        [Tooltip("The UI Prefab for the tooltip itself.")]
        [SerializeField] private Tooltip tooltipPrefab;

        [Header("Layout Settings")]
        [Tooltip("The maximum width the tooltip can have before its text starts wrapping.")]
        [SerializeField, Min(50f)] private float maxTooltipWidth = 350f;

        [Header("Animation Settings")]
        [Tooltip("The duration of the fade-in and fade-out animations in seconds.")]
        [SerializeField, Min(0f)] private float fadeDuration = 0.2f;

        [Header("Positioning")]
        [Tooltip("An offset to apply to the tooltip's position relative to the mouse cursor.")]
        [SerializeField] private Vector2 positionOffset = new(0, -20);

        // --- Private State ---
        private Tooltip tooltipInstance;
        private RectTransform tooltipRect;
        private CanvasGroup canvasGroup;

        // We track showing and hiding separately to allow smooth transitions.
        // If a user hovers a new item, the old one can continue fading out during the new item's Hover Delay.
        private Coroutine activeShowCoroutine;
        private Coroutine activeHideCoroutine;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
            else { Destroy(gameObject); }
        }
        #endregion

        #region Public API
        /// <summary>
        /// Schedules the tooltip to be shown. Requires the trigger context to determine the correct parent Canvas.
        /// </summary>
        public void ShowTooltip(string content, string title, Sprite icon, Color titleColor, Color iconColor, float delay, Transform triggerContext)
        {
            // Stop any pending SHOW routine (e.g., if we hovered Item A then immediately hovered Item B).
            // We do NOT stop 'activeHideCoroutine' here; we let the old tooltip fade out smoothly while waiting for the delay.
            if (activeShowCoroutine != null) StopCoroutine(activeShowCoroutine);

            activeShowCoroutine = StartCoroutine(ShowRoutine(content, title, icon, titleColor, iconColor, delay, triggerContext));
        }

        public void HideTooltip()
        {
            // If the instance was destroyed (e.g. parent canvas unloaded), we can't animate it out.
            if (tooltipInstance == null) return;

            // Stop any pending SHOW routine (e.g., user hovered, waited 0.1s, then left before it appeared).
            if (activeShowCoroutine != null) StopCoroutine(activeShowCoroutine);

            // If we are already fading out, restart it to ensure a smooth transition from the current alpha.
            if (activeHideCoroutine != null) StopCoroutine(activeHideCoroutine);

            if (tooltipInstance.gameObject.activeInHierarchy)
            {
                activeHideCoroutine = StartCoroutine(FadeOut());
            }
        }
        #endregion

        #region Coroutines & Logic
        private IEnumerator ShowRoutine(string content, string title, Sprite icon, Color titleColor, Color iconColor, float delay, Transform triggerContext)
        {
            // Wait for the hover delay before doing any work
            if (delay > 0)
            {
                yield return new WaitForSeconds(delay);
            }

            // The delay is over. NOW we stop the hiding animation (if it's still running)
            // so we can snap the tooltip to the new position and show it.
            if (activeHideCoroutine != null) StopCoroutine(activeHideCoroutine);

            // Ensure the tooltip exists and is parented to the correct Canvas for this trigger
            if (!EnsureTooltipReady(triggerContext))
            {
                yield break;
            }

            // Reset alpha to 0 before resizing to prevent one-frame flicker of old content
            if (canvasGroup != null) canvasGroup.alpha = 0;

            yield return ResizeTooltipRoutine(content, title, icon, titleColor, iconColor);

            tooltipInstance.gameObject.SetActive(true);
            tooltipInstance.transform.SetAsLastSibling();
            PositionTooltip();

            // Hand off the "Show" lifecycle to the fade-in animation
            activeShowCoroutine = StartCoroutine(FadeIn());
        }

        /// <summary>
        /// Ensures the tooltip instance is instantiated, active, and parented to the correct root canvas.
        /// </summary>
        private bool EnsureTooltipReady(Transform triggerContext)
        {
            // 1. Determine the target Canvas based on the trigger's location
            Canvas targetCanvas = null;
            if (triggerContext != null)
            {
                Canvas foundCanvas = triggerContext.GetComponentInParent<Canvas>();
                if (foundCanvas != null)
                {
                    // Use rootCanvas to ensure we are on top of nested canvases
                    targetCanvas = foundCanvas.rootCanvas;
                }
            }

            // Fallback: If trigger has no canvas (e.g. non-UI object), find any canvas
            if (targetCanvas == null) targetCanvas = FindFirstObjectByType<Canvas>();

            if (targetCanvas == null)
            {
                Debug.LogWarning("TooltipManager: Could not find any Canvas to display the tooltip.");
                return false;
            }

            // 2. Instantiate if missing (or if destroyed by scene unload)
            if (tooltipInstance == null)
            {
                GameObject tooltipObj = Instantiate(tooltipPrefab.gameObject, targetCanvas.transform, false);
                tooltipInstance = tooltipObj.GetComponent<Tooltip>();
                tooltipRect = tooltipObj.GetComponent<RectTransform>();
                canvasGroup = tooltipObj.GetComponent<CanvasGroup>();
                tooltipObj.SetActive(false);
            }

            // 3. Reparent if it's currently on a different canvas
            if (tooltipInstance.transform.parent != targetCanvas.transform)
            {
                tooltipInstance.transform.SetParent(targetCanvas.transform);
                // Resetting scale is important when moving between canvases with different scalers
                tooltipInstance.transform.localScale = Vector3.one;
            }

            return true;
        }

        private IEnumerator ResizeTooltipRoutine(string content, string title, Sprite icon, Color titleColor, Color iconColor)
        {
            tooltipInstance.gameObject.SetActive(false);

            float availableTitleWidth = CalculateAvailableWidthForText(tooltipInstance.titleField);
            float availableContentWidth = CalculateAvailableWidthForText(tooltipInstance.contentField);

            string wrappedTitle = WrapText(title, tooltipInstance.titleField, availableTitleWidth);
            string wrappedContent = WrapText(content, tooltipInstance.contentField, availableContentWidth);

            tooltipInstance.SetText(wrappedContent, wrappedTitle, icon, titleColor, iconColor);

            // The robust "triple cycle" resize method to handle nested layout groups correctly
            for (int i = 0; i < 3; i++)
            {
                tooltipInstance.gameObject.SetActive(true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
                yield return new WaitForEndOfFrame();
                tooltipInstance.gameObject.SetActive(false);
            }
        }

        private IEnumerator FadeIn()
        {
            float start = Time.unscaledTime;
            while (Time.unscaledTime < start + fadeDuration)
            {
                if (canvasGroup == null) yield break;
                canvasGroup.alpha = Mathf.Lerp(0, 1, (Time.unscaledTime - start) / fadeDuration);
                yield return null;
            }
            if (canvasGroup != null) canvasGroup.alpha = 1;
        }

        private IEnumerator FadeOut()
        {
            float start = Time.unscaledTime;
            float startAlpha = canvasGroup.alpha;
            while (Time.unscaledTime < start + fadeDuration)
            {
                if (canvasGroup == null) yield break;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0, (Time.unscaledTime - start) / fadeDuration);
                yield return null;
            }
            if (canvasGroup != null) canvasGroup.alpha = 0;
            if (tooltipInstance != null) tooltipInstance.gameObject.SetActive(false);
        }
        #endregion

        #region Helper Methods
        private void PositionTooltip()
        {
            if (tooltipInstance == null) return;

#if ENABLE_INPUT_SYSTEM
            Vector2 screenPos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
            Vector2 screenPos = Input.mousePosition;
#endif
            screenPos += positionOffset;

            Canvas rootCanvas = tooltipInstance.GetComponentInParent<Canvas>();
            if (rootCanvas == null) return;

            // Handle Render Modes (Overlay vs Camera/World)
            Camera uiCamera = (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : rootCanvas.worldCamera;
            RectTransform parentRect = tooltipInstance.transform.parent as RectTransform;

            // Convert Screen pixels to Canvas Local coordinates.
            // This is crucial for supporting World Space Canvases and different Screen Resolutions.
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPos, uiCamera, out Vector2 localPoint))
            {
                // Set Z to 0 to ensure it's on the canvas plane
                tooltipInstance.transform.localPosition = new Vector3(localPoint.x, localPoint.y, 0);
                ClampTooltipToRect(parentRect);
            }
        }

        private void ClampTooltipToRect(RectTransform parentRect)
        {
            float outlineSize = 0;
            // Feature: Check children for Outline (e.g., if attached to a background image)
            Outline outline = tooltipInstance.GetComponentInChildren<Outline>();
            if (outline != null)
            {
                outlineSize = outline.effectDistance.x;
            }

            Rect parentBounds = parentRect.rect;
            Rect tooltipBounds = tooltipRect.rect;

            // Feature: Account for the tooltip's scale (important if the Canvas scales UI elements)
            Vector3 scale = tooltipInstance.transform.localScale;
            float scaledWidth = (tooltipBounds.width + outlineSize) * scale.x;
            float scaledHeight = (tooltipBounds.height + outlineSize) * scale.y;

            Vector3 currentLocalPos = tooltipInstance.transform.localPosition;

            // Calculate edges based on Pivot to support any prefab setup (Top-Left, Center, etc.)
            float pivotX = tooltipRect.pivot.x;
            float pivotY = tooltipRect.pivot.y;

            float left = currentLocalPos.x - (scaledWidth * pivotX);
            float right = currentLocalPos.x + (scaledWidth * (1f - pivotX));
            float bottom = currentLocalPos.y - (scaledHeight * pivotY);
            float top = currentLocalPos.y + (scaledHeight * (1f - pivotY));

            // Clamp Horizontal
            if (right > parentBounds.xMax)
            {
                currentLocalPos.x -= (right - parentBounds.xMax);
            }
            else if (left < parentBounds.xMin)
            {
                currentLocalPos.x += (parentBounds.xMin - left);
            }

            // Clamp Vertical
            if (top > parentBounds.yMax)
            {
                currentLocalPos.y -= (top - parentBounds.yMax);
            }
            else if (bottom < parentBounds.yMin)
            {
                currentLocalPos.y += (parentBounds.yMin - bottom);
            }

            tooltipInstance.transform.localPosition = currentLocalPos;
        }

        private float CalculateAvailableWidthForText(TMP_Text textElement)
        {
            float availableWidth = maxTooltipWidth;
            if (textElement == null) return availableWidth;

            Transform current = textElement.transform;
            while (current != null && current != tooltipInstance.transform)
            {
                if (current.TryGetComponent<LayoutGroup>(out var layoutGroup))
                {
                    availableWidth -= (layoutGroup.padding.left + layoutGroup.padding.right);
                    if (layoutGroup is HorizontalLayoutGroup hlg)
                    {
                        availableWidth -= hlg.spacing * (current.parent.childCount - 1);
                    }
                }
                current = current.parent;
            }
            return availableWidth;
        }

        private string WrapText(string text, TMP_Text tmp, float maxWidth)
        {
            if (string.IsNullOrEmpty(text) || tmp == null) return text;
            if (tmp.GetPreferredValues(text).x <= maxWidth) return text;

            StringBuilder sb = new StringBuilder();
            string[] words = text.Split(' ');
            string line = "";

            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                string testLine = string.IsNullOrEmpty(line) ? word : $"{line} {word}";
                if (tmp.GetPreferredValues(testLine).x > maxWidth && !string.IsNullOrEmpty(line))
                {
                    sb.AppendLine(line);
                    line = word;
                }
                else
                {
                    line = testLine;
                }
            }
            sb.Append(line);
            return sb.ToString();
        }
        #endregion
    }
}