using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonFadeTrigger : MonoBehaviour
{
    public enum FadeAction
    {
        FadeOutToBlack,
        FadeInFromBlack,
        FadeOutThenIn
    }

    [Header("References")]
    [SerializeField] private UIFadeTransition fadeTransition;
    [SerializeField] private Button button;

    [Header("Action")]
    [SerializeField] private FadeAction fadeAction = FadeAction.FadeOutThenIn;

    [Header("Timing")]
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float waitTime = 0f;
    [SerializeField] private float fadeInDuration = 0.5f;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClick);
    }

    private void HandleClick()
    {
        if (fadeTransition == null)
        {
            Debug.LogWarning("ButtonFadeTrigger: No UIFadeTransition assigned.");
            return;
        }

        switch (fadeAction)
        {
            case FadeAction.FadeOutToBlack:
                fadeTransition.FadeOutToBlack(fadeOutDuration);
                break;

            case FadeAction.FadeInFromBlack:
                fadeTransition.FadeInFromBlack(fadeInDuration);
                break;

            case FadeAction.FadeOutThenIn:
                fadeTransition.FadeOutThenIn(fadeOutDuration, waitTime, fadeInDuration);
                break;
        }
    }
}