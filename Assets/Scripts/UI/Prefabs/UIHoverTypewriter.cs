using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIHoverTypewriter : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Text Target")]
    [SerializeField] private TMP_Text targetTextBox;

    [Header("Typewriter Settings")]
    [TextArea(2, 5)]
    [SerializeField] private string messageToType = "Hello there!";
    [SerializeField] private float characterDelay = 0.03f;
    [SerializeField] private bool clearOnExit = false;

    private Coroutine typingCoroutine;

    private void Awake()
    {
        if (targetTextBox != null)
            targetTextBox.text = "";
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (targetTextBox == null)
            return;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!clearOnExit || targetTextBox == null)
            return;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        targetTextBox.text = "";
    }

    private IEnumerator TypeText()
    {
        targetTextBox.text = "";

        for (int i = 0; i < messageToType.Length; i++)
        {
            targetTextBox.text += messageToType[i];
            yield return new WaitForSecondsRealtime(characterDelay);
        }

        typingCoroutine = null;
    }
}