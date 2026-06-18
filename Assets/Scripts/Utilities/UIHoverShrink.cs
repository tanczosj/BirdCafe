using UnityEngine;
using UnityEngine.EventSystems;
[RequireComponent(typeof(RectTransform))]
public class UIHoverShrink : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Scale Settings")]
    [SerializeField] private Vector3 normalScale = Vector3.one;
    [SerializeField] private Vector3 hoverScale = new Vector3(0.9f, 0.9f, 0.9f);
    [SerializeField] private float shrinkSpeed = 8f;
    [Header("Visual Target (child to scale)")]
    [SerializeField] private RectTransform visual;
    [Header("Hover Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hoverSound;
    private Vector3 targetScale;
    private bool isHovered = false;
    private void Awake()
    {
        // Auto-assign visual if not set
        if (visual == null)
        {
            Debug.LogError("UIHoverShrink: Visual reference is not assigned!", gameObject);
            return;
        }
        normalScale = Vector3.one;
        visual.localScale = Vector3.one;
        targetScale = Vector3.one;
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }
    private void Update()
    {
        if (visual == null) return;
        visual.localScale = Vector3.Lerp(
            visual.localScale,
            targetScale,
            Time.unscaledDeltaTime * shrinkSpeed
        );
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isHovered) return;
        isHovered = true;
        targetScale = hoverScale;
        if (audioSource != null && hoverSound != null)
            audioSource.PlayOneShot(hoverSound);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        targetScale = normalScale;
    }
}