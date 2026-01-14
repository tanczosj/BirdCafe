using UnityEngine;
using UnityEngine.UI;

public class PanelMover : MonoBehaviour
{
    public RectTransform panelMain;       // The panel to move
    public Button optionsButton;          // Button that triggers the move
    public Vector3 targetPosition;        // Where you want the panel to go
    public float moveDuration = 0.5f;     // Duration of the tween

    private Vector3 startPosition;

    private void Start()
    {
        if (panelMain == null)
            Debug.LogError("Panel_Main not assigned!");

        if (optionsButton != null)
            optionsButton.onClick.AddListener(MovePanel);

        // Save starting position
        startPosition = panelMain.anchoredPosition;
    }

    public void MovePanel()
    {
        // Stop any running tween to prevent conflicts
        StopAllCoroutines();
        StartCoroutine(TweenPanel(panelMain.anchoredPosition, targetPosition, moveDuration));
    }

    private System.Collections.IEnumerator TweenPanel(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Smooth interpolation
            panelMain.anchoredPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));

            yield return null;
        }

        panelMain.anchoredPosition = to; // Ensure final position is exact
    }

    // Optional: Reset panel to start
    public void ResetPanel()
    {
        panelMain.anchoredPosition = startPosition;
    }
}
