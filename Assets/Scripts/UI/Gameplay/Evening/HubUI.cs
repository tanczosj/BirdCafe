using BirdCafe.Shared;
using UnityEngine;

public class HubUI : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
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
