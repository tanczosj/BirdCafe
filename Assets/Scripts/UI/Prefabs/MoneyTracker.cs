using BirdCafe.Shared;
using BirdCafe.UI.Components;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class MoneyTracker : MonoBehaviour
{
    [SerializeField] public StatCounterUI moneyCounter;
    [SerializeField] public bool enableAutomaticUpdate = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (enableAutomaticUpdate)
        {
            BirdCafeGame.Instance.OnMoneyChanged += HandleMoneyChanged;
        }
    }
    private void OnDestroy()
    {
        if (BirdCafeGame.Instance != null && enableAutomaticUpdate)
        {
            BirdCafeGame.Instance.OnMoneyChanged -= HandleMoneyChanged;
        }
    }

    public void HandleMoneyChanged(decimal oldAmount, decimal newAmount)
    {
        if (moneyCounter != null && oldAmount != newAmount)
        {
            moneyCounter.AnimateValue((float)newAmount, 1f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
