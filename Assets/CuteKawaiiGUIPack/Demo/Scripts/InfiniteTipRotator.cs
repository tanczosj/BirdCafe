using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InfiniteTipRotator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text tipText;

    [Header("Tips")]
    [TextArea(2, 4)]
    [SerializeField]
    private List<string> tips = new List<string>
    {
        "Happy birds help create a better cafe atmosphere.",
        "Rare birds can bring in more curious customers.",
        "Feeding birds they love can help build trust faster.",
        "Trusted birds tend to perform better during the day.",
        "Birds with close friendships can boost your cafe’s energy.",
        "Keep an eye on your supplies before feeding birds.",
        "Buying food from Rick’s Pet Store keeps your birds ready for care.",
        "A well-cared-for bird is more valuable than a neglected one.",
        "Some birds are pickier eaters than others.",
        "Strong bird bonds can mean stronger daily earnings.",
        "Planning ahead can make tomorrow much more profitable.",
        "A balanced flock can be better than one flashy bird.",
        "Costumes can make certain birds stand out more to customers.",
        "Toys can be useful for keeping birds engaged and happy.",
        "Trust takes time to build, but it pays off.",
        "Different food choices can matter more than you think.",
        "A thriving cafe starts with thriving birds.",
        "Some birds shine brightest when paired with a friend.",
        "Investing in care can pay off during simulation.",
        "Small bird improvements can add up over time."
    };

    [Header("Timing")]
    [SerializeField] private float typeDelay = 0.04f;
    [SerializeField] private float deleteDelay = 0.02f;
    [SerializeField] private float holdTime = 5f;
    [SerializeField] private bool useUnscaledTime = true;

    private int lastTipIndex = -1;
    private Coroutine loopCoroutine;

    private void OnEnable()
    {
        if (tipText != null)
        {
            tipText.text = string.Empty;
        }

        loopCoroutine = StartCoroutine(TipLoop());
    }

    private void OnDisable()
    {
        if (loopCoroutine != null)
        {
            StopCoroutine(loopCoroutine);
            loopCoroutine = null;
        }
    }

    private IEnumerator TipLoop()
    {
        if (tipText == null)
        {
            Debug.LogWarning($"{nameof(InfiniteTipRotator)} on {gameObject.name} has no TMP_Text assigned.");
            yield break;
        }

        if (tips == null || tips.Count == 0)
        {
            Debug.LogWarning($"{nameof(InfiniteTipRotator)} on {gameObject.name} has no tips assigned.");
            yield break;
        }

        while (true)
        {
            string nextTip = GetNextTip();

            yield return TypeText(nextTip);
            yield return Wait(holdTime);
            yield return DeleteText();
        }
    }

    private string GetNextTip()
    {
        if (tips.Count == 1)
        {
            lastTipIndex = 0;
            return tips[0];
        }

        int newIndex = Random.Range(0, tips.Count);

        while (newIndex == lastTipIndex)
        {
            newIndex = Random.Range(0, tips.Count);
        }

        lastTipIndex = newIndex;
        return tips[newIndex];
    }

    private IEnumerator TypeText(string fullText)
    {
        tipText.text = string.Empty;

        for (int i = 0; i <= fullText.Length; i++)
        {
            tipText.text = fullText.Substring(0, i);
            yield return Wait(typeDelay);
        }
    }

    private IEnumerator DeleteText()
    {
        string current = tipText.text;

        for (int i = current.Length; i >= 0; i--)
        {
            tipText.text = current.Substring(0, i);
            yield return Wait(deleteDelay);
        }
    }

    private IEnumerator Wait(float seconds)
    {
        if (useUnscaledTime)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(seconds);
        }
    }
}