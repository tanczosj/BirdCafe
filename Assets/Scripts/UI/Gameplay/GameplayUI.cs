using System;
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
    public GameObject tutorialPanel;
    public GameObject dayIntroPanel;
    public GameObject eveningSummaryPanel;
    public GameObject hubPanel;
    public GameObject petStorePanel;
    public GameObject petStoreSuppliesPanel;
    public GameObject petStoreBirdsPanel;
    public GameObject carePanel;
    public GameObject planningPanel;
    public GameObject weeklyReportPanel;
    public GameObject gameOverPanel;
    public GameObject simulationPanel;

    [Header("--- TOASTS ---")]
    public GameObject toastPrefab;
    public Transform toastContainer;

    [Header("Logic Scripts")]
    public SimulationVisualizer simVisualizer;

    [Header("Transition")]
    [SerializeField] private CircleScreenTransition circleTransition;

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

    private void TransitionThen(Action action)
    {
        if (circleTransition == null)
        {
            action?.Invoke();
            return;
        }

        if (circleTransition.IsPlaying)
            return;

        circleTransition.Play(() =>
        {
            action?.Invoke();
        });
    }

    public void GoToSummaryWithTransition()
    {
        TransitionThen(() => BirdCafeGame.Instance.GoToSummary());
    }

    public void GoToPlanningWithTransition()
    {
        TransitionThen(() => BirdCafeGame.Instance.GoToPlanning());
    }

    public void GoToPetStoreWithTransition()
    {
        TransitionThen(() => BirdCafeGame.Instance.GoToPetStore());
    }

    public void GoToCareWithTransition()
    {
        TransitionThen(() => BirdCafeGame.Instance.GoToCare());
    }

    public void GoToHubWithTransition()
    {
        TransitionThen(() => BirdCafeGame.Instance.GoToHub());
    }

    public void GoToSuppliesWithTransition()
    {
        TransitionThen(() => BirdCafeGame.Instance.GoToPetStoreSupplies());
    }

    public void GoToBirdStoreWithTransition()
    {
        TransitionThen(() => BirdCafeGame.Instance.GoToPetStoreBirds());
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