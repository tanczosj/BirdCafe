using UnityEngine;
using UnityEngine.UI;

namespace BirdCafe.Unity.Birds
{
    [DisallowMultipleComponent]
    public sealed class BirdSpriteSizeController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Image targetImage;

        [Header("Size")]
        [SerializeField] private bool applyOnEnable = true;
        [SerializeField] private bool applyInEditor = true;
        [SerializeField] private Vector2 spriteSize = new Vector2(256f, 256f);
        [SerializeField] private bool preserveAspect = true;

        [Header("Layout Group Support")]
        [Tooltip("Enable this if the bird is inside a Grid Layout Group, Vertical Layout Group, or Horizontal Layout Group.")]
        [SerializeField] private bool updateLayoutElement = true;

        [SerializeField] private LayoutElement targetLayoutElement;

        public Vector2 SpriteSize => spriteSize;

        private void Reset()
        {
            FindReferences();
            ApplySize();
        }

        private void OnEnable()
        {
            FindReferences();

            if (applyOnEnable)
            {
                ApplySize();
            }
        }

        private void OnValidate()
        {
            spriteSize = new Vector2(
                Mathf.Max(1f, spriteSize.x),
                Mathf.Max(1f, spriteSize.y)
            );

            if (!applyInEditor)
            {
                return;
            }

            FindReferences();
            ApplySize();
        }

        [ContextMenu("Apply Size")]
        public void ApplySize()
        {
            FindReferences();

            if (targetImage == null)
            {
                return;
            }

            spriteSize = new Vector2(
                Mathf.Max(1f, spriteSize.x),
                Mathf.Max(1f, spriteSize.y)
            );

            targetImage.preserveAspect = preserveAspect;

            RectTransform rectTransform = targetImage.rectTransform;
            if (rectTransform != null)
            {
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, spriteSize.x);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, spriteSize.y);
            }

            if (updateLayoutElement && targetLayoutElement != null)
            {
                targetLayoutElement.preferredWidth = spriteSize.x;
                targetLayoutElement.preferredHeight = spriteSize.y;
                targetLayoutElement.minWidth = spriteSize.x;
                targetLayoutElement.minHeight = spriteSize.y;
            }
        }

        public void SetSize(Vector2 newSize)
        {
            spriteSize = new Vector2(
                Mathf.Max(1f, newSize.x),
                Mathf.Max(1f, newSize.y)
            );

            ApplySize();
        }

        public void SetSize(float width, float height)
        {
            SetSize(new Vector2(width, height));
        }

        private void FindReferences()
        {
            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }

            if (targetImage == null)
            {
                targetImage = GetComponentInChildren<Image>(true);
            }

            if (targetLayoutElement == null && targetImage != null)
            {
                targetLayoutElement = targetImage.GetComponent<LayoutElement>();
            }
        }
    }
}