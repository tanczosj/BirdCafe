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

    [Tooltip("Assign Panel_HubHUD here")]
    public GameObject hubPanel;

    [Tooltip("Assign Panel_PetStore here")]
    public GameObject petStorePanel;

    [Tooltip("Assign Panel_PetStoreSupplies here")]
    public GameObject petStoreSuppliesPanel;

    [Tooltip("Assign Panel_PetStoreBirds here")]
    public GameObject petStoreBirdsPanel;

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
        BirdCafeGame.Instance.OnScreenChanged += HandleScreenChanged;
        BirdCafeGame.Instance.OnToastMessage += ShowToast;

        HandleScreenChanged(BirdCafeGame.Instance.CurrentScreen);
    }

    private void OnDestroy()
    {
        BirdCafeGame.Instance.OnScreenChanged -= HandleScreenChanged;
        BirdCafeGame.Instance.OnToastMessage -= ShowToast;
    }

    private void HandleScreenChanged(GameScreen newScreen)
    {
        HideAll();

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
                if (simVisualizer) simVisualizer.enabled = true;
                break;

            case GameScreen.EveningSummary:
                if (eveningSummaryPanel) eveningSummaryPanel.SetActive(true);
                break;

            case GameScreen.Hub:
                if (hubPanel) hubPanel.SetActive(true);
                break;

            case GameScreen.EveningPetStore:
                if (petStorePanel) petStorePanel.SetActive(true);
                break;

            case GameScreen.EveningPetStoreSupplies:
                if (petStoreSuppliesPanel) petStoreSuppliesPanel.SetActive(true);
                break;

            case GameScreen.EveningPetStoreBirds:
                if (petStoreBirdsPanel) petStoreBirdsPanel.SetActive(true);
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
        if (tutorialPanel) tutorialPanel.SetActive(false);
        if (dayIntroPanel) dayIntroPanel.SetActive(false);
        if (eveningSummaryPanel) eveningSummaryPanel.SetActive(false);
        if (hubPanel) hubPanel.SetActive(false);
        if (petStorePanel) petStorePanel.SetActive(false);
        if (petStoreSuppliesPanel) petStoreSuppliesPanel.SetActive(false);
        if (petStoreBirdsPanel) petStoreBirdsPanel.SetActive(false);
        if (carePanel) carePanel.SetActive(false);
        if (planningPanel) planningPanel.SetActive(false);
        if (weeklyReportPanel) weeklyReportPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);

        if (simulationPanel) simulationPanel.SetActive(false);

        if (simVisualizer) simVisualizer.enabled = false;
    }

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