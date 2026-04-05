using BirdCafe.Shared;
using BirdCafe.UI.Components;
using UnityEngine;

public class SecretHotkey : MonoBehaviour
{
    [SerializeField] private StatCounterUI statCounterUI;
    [SerializeField] private KeyCode modifier1 = KeyCode.LeftControl;
    [SerializeField] private KeyCode modifier2 = KeyCode.LeftShift;
    [SerializeField] private KeyCode triggerKey = KeyCode.M;

    [SerializeField] private int bonusMoney = 1000;

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Input.GetKey(modifier1) &&
            Input.GetKey(modifier2) &&
            Input.GetKeyDown(triggerKey))
        {
            RunSecretAction();
        }
#endif
    }

    private void RunSecretAction()
    {
        Debug.Log("Secret hotkey activated: adding money");

        BirdCafeGame.Instance.AddMoney(bonusMoney);
        statCounterUI.AnimateValue((float)BirdCafeGame.Instance.Controller.CurrentState.Economy.CurrentBalance, 1f);
    }
}