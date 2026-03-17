using BirdCafe.Shared;
using BirdCafe.UI.Components;
using UnityEngine;

public class HubUI : MonoBehaviour
{
    [Header("Header Stats")]
    [Tooltip("Shows current money balance")]
    public StatCounterUI moneyCounter;

    [Tooltip("Shows current popularity")]
    public StatCounterUI popularityCounter;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnEnable()
    {
        RefreshFull();
    }

    private void RefreshFull()
    {
        var data = BirdCafeGame.Instance.GetPlanningDashboard();
        if (data == null) return;

        // Globals
        if (moneyCounter) moneyCounter.Value = (float)data.CurrentMoney;
        if (popularityCounter) popularityCounter.Value = data.CurrentPopularity;
    }

    public void OnEveningSummaryClicked()
    {
        BirdCafeGame.Instance.GoToSummary();
    }

    public void OnPlanningClicked()
    {

        BirdCafeGame.Instance.GoToPlanning();
    }
    public void OnPetStoreClicked()
    {
        BirdCafeGame.Instance.GoToPetStore();
    }
    public void OnCareClicked()
    {
        BirdCafeGame.Instance.GoToCare();
    }
}
