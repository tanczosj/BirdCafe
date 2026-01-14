using BirdCafe.Shared;
using BirdCafe.Shared.ViewModels;
using BirdCafe.UI.Components;
using TMPro;
using UnityEngine;

public class DayIntroUI : MonoBehaviour
{
    [Header("UI References")]
    public StatCounterUI statCounter;   // e.g., "Popularity: 15"
    public TMP_Text messageText;      // e.g., "It's a beautiful morning!"

    private void OnEnable()
    {
        RefreshData();
    }

    private void RefreshData()
    {
        // 1. Get data from Facade
        DayIntroViewModel vm = BirdCafeGame.Instance.GetDayIntro();

        if (vm == null) return;

        // 2. Bind to UI
        if (statCounter) statCounter.AnimateValue(vm.Popularity, 0.7f);
        if (messageText) messageText.text = vm.Message;
    }

    // Hook this to your "Start Day" / "Open Cafe" button
    public void OnStartDayClicked()
    {
        // Triggers the simulation. 
        // If successful, the Library fires OnScreenChanged -> GameplayUI hides this panel and shows the Simulation.
        BirdCafeGame.Instance.StartSimulationPlayback();
    }
}