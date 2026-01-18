using BirdCafe.Shared;
using BirdCafe.Shared.ViewModels;
using BirdCafe.UI.Components;
using BirdCafe.UI.Gameplay.Day;
using Ricimi;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GamePlayUI : MonoBehaviour
{
    [Header("--- UI PANELS ---")]
    [Tooltip("Assign Panel_Tutorial here")]
    public GameObject tutorialPanel;

    [Tooltip("Assign Panel_DayIntro here")]
    public GameObject dayIntroPanel;

    [Tooltip("Assign Panel_EveningSummary here")]
    public GameObject eveningSummaryPanel;

    [Tooltip("Assign Panel_Care here")]
    public GameObject carePanel;

    [Tooltip("Assign Panel_Planning here")]
    public GameObject planningPanel;

    [Tooltip("Assign Panel_WeeklyReport here")]
    public GameObject weeklyReportPanel;

    [Tooltip("Assign Panel_GameOver here")]
    public GameObject gameOverPanel;

    [Tooltip("Assign Panel_Simulation here ")]
    public GameObject simulationPanel;

    [Header("--- TOASTS ---")]
    [Tooltip("The prefab to instantiate. Must have 'Toast' and 'Popup' components.")]
    public GameObject toastPrefab;

    [Tooltip("The parent container for toasts (e.g. the Canvas or a SafeArea panel).")]
    public Transform toastContainer;

    [Header("Logic Scripts")]
    public SimulationVisualizer simVisualizer;

    private void Start()
    {
        // 1. Subscribe to the Library Events
        BirdCafeGame.Instance.OnScreenChanged += HandleScreenChanged;
        BirdCafeGame.Instance.OnToastMessage += ShowToast;

        // 2. Initialize the first state (Usually DayIntro for a loaded game)
        // We act as if the screen just changed to whatever the game thinks is current.
        HandleScreenChanged(BirdCafeGame.Instance.CurrentScreen);
    }

    private void OnDestroy()
    {
        // 3. Always Unsubscribe when the object is destroyed to prevent memory leaks
        BirdCafeGame.Instance.OnScreenChanged -= HandleScreenChanged;
        BirdCafeGame.Instance.OnToastMessage -= ShowToast;
    }

    /// <summary>
    /// This is the "Traffic Cop" function. 
    /// It turns everything off, then turns on only what is needed.
    /// </summary>
    private void HandleScreenChanged(GameScreen newScreen)
    {
        // A. Reset everything to OFF
        HideAll();

        // B. Turn on specific layers based on the screen
        switch (newScreen)
        {
            case GameScreen.Tutorial:
                if (tutorialPanel) tutorialPanel.SetActive(true);
                break;

            case GameScreen.DayIntro:
                if (dayIntroPanel) dayIntroPanel.SetActive(true);
                break;

            case GameScreen.DaySimulation:
                if (simulationPanel) simulationPanel.SetActive(true);
                // Enable the visualizer logic component
                if (simVisualizer) simVisualizer.enabled = true;
                break;

            case GameScreen.EveningSummary:
                if (eveningSummaryPanel) eveningSummaryPanel.SetActive(true);
                break;

            case GameScreen.EveningCare:
                if (carePanel) carePanel.SetActive(true);
                break;

            case GameScreen.EveningPlanning:
                if (planningPanel) planningPanel.SetActive(true);
                break;

            case GameScreen.WeeklySummary:
                if (weeklyReportPanel) weeklyReportPanel.SetActive(true);
                break;

            case GameScreen.GameOver:
                if (gameOverPanel) gameOverPanel.SetActive(true);
                break;
        }
    }

    private void HideAll()
    {
        // UI
        if (tutorialPanel) tutorialPanel.SetActive(false);
        if (dayIntroPanel) dayIntroPanel.SetActive(false);
        if (eveningSummaryPanel) eveningSummaryPanel.SetActive(false);
        if (carePanel) carePanel.SetActive(false);
        if (planningPanel) planningPanel.SetActive(false);
        if (weeklyReportPanel) weeklyReportPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);

        // World
        if (simulationPanel) simulationPanel.SetActive(false);

        // Logic
        if (simVisualizer) simVisualizer.enabled = false;
    }

    /// <summary>
    /// PUBLIC METHOD FOR UI BUTTONS:
    /// Drag the GameManager object onto the Button OnClick event and select this method.
    /// </summary>
    public void OnSkipSimulationClicked()
    {
        if (simVisualizer != null && simVisualizer.enabled)
        {
            simVisualizer.SkipSimulation();
        }
        else
        {
            Debug.LogWarning("Cannot skip: Visualizer is missing or disabled.");
        }
    }

    private void ShowToast(string message)
    {
        if (toastPrefab == null || toastContainer == null) return;

        GameObject instance = Instantiate(toastPrefab, toastContainer);

        var toastContent = instance.GetComponent<Toast>();
        if (toastContent != null)
        {
            toastContent.Initialize("Heya!", message);
        }

        var popup = instance.GetComponent<Popup>();
        if (popup != null)
        {
            popup.Open();
            StartCoroutine(AutoClosePopup(popup, 5.0f));
        }
    }

    private System.Collections.IEnumerator AutoClosePopup(Popup popup, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (popup != null) popup.Close();
    }
}