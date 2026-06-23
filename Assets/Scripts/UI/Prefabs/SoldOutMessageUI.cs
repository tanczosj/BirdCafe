using TMPro;
using UnityEngine;

public class SoldOutMessageUI : MonoBehaviour
{
    [Header("Text References")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    public void SetMessages(string titleMessage, string descriptionMessage)
    {
        if (titleText != null)
            titleText.text = titleMessage;

        if (descriptionText != null)
            descriptionText.text = descriptionMessage;
    }
}