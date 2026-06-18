using UnityEngine;
using UnityEngine.UI;

public class AutoScroller : MonoBehaviour
{
    [Header("Scroll Settings")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private float scrollSpeed = 0.05f;  // Units per second (0-1 range)

    [Header("Hotkey Settings")]
    [SerializeField] private KeyCode hotkey = KeyCode.D;
    private bool requireCtrl = true;
    private bool requireShift = true;

    private bool isAutoScrolling = false;

    private void Update()
    {
        if (IsHotkeyPressed())
            ToggleAutoScroll();

        if (isAutoScrolling)
            AutoScroll();
    }

    private bool IsHotkeyPressed()
    {
        bool ctrl = !requireCtrl || (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
        bool shift = !requireShift || (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

        return ctrl && shift && Input.GetKeyDown(hotkey);
    }

    private void ToggleAutoScroll()
    {
        isAutoScrolling = !isAutoScrolling;
        Debug.Log($"[AutoScroller] Auto-scroll {(isAutoScrolling ? "ON" : "OFF")}");
    }

    private void AutoScroll()
    {
        if (scrollRect == null)
        {
            Debug.LogWarning("[AutoScroller] No ScrollRect assigned!");
            return;
        }

        // Scroll downward (verticalNormalizedPosition goes from 1 = top to 0 = bottom)
        scrollRect.verticalNormalizedPosition -= scrollSpeed * Time.deltaTime;

        // Auto-stop when it reaches the bottom
        if (scrollRect.verticalNormalizedPosition <= 0f)
        {
            scrollRect.verticalNormalizedPosition = 0f;
            isAutoScrolling = false;
            Debug.Log("[AutoScroller] Reached the bottom. Auto-scroll stopped.");
        }
    }

    // Optional: Reset scroll back to top
    public void ResetScroll()
    {
        scrollRect.verticalNormalizedPosition = 1f;
        isAutoScrolling = false;
    }
}