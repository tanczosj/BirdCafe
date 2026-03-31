using BirdCafe.Shared;
using BirdCafe.UI.Components;
using UnityEngine;

public class HubUI : MonoBehaviour
{
    public StatCounterUI moneyCounter;
    public StatCounterUI popularityCounter;

    private void OnEnable()
    {
        RefreshFull();
    }

    private void RefreshFull()
    {
        var data = BirdCafeGame.Instance.GetPlanningDashboard();
        if (data == null) return;

        if (moneyCounter) moneyCounter.Value = (float)data.CurrentMoney;
        if (popularityCounter) popularityCounter.Value = data.CurrentPopularity;
    }
}