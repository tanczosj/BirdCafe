using BirdCafe.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Menu Navigation")]
    [Tooltip("The big sliding panel containing all screens")]
    public RectTransform panelMain;

    [Header("New Game Inputs")]
    public TMP_InputField playerNameInput;
    public TMP_InputField cafeNameInput;

    private void Start()
    {
        // RESET: Ensure the menu starts at 0,0 (Home) every time this scene loads.
        // This prevents the menu from being stuck on the "Options" screen 
        // if you return to the Main Menu later.
        if (panelMain != null)
        {
            panelMain.anchoredPosition = Vector2.zero;
        }
    }

    // ---------------------------------------------------------
    // LOGIC ONLY (Visuals are handled by PanelMover on buttons)
    // ---------------------------------------------------------

    // Hook this to the "Start" or "Confirm" button on your New Game slide
    public void SubmitNewGame()
    {
        string pName = playerNameInput.text;
        string cName = cafeNameInput.text;

        if (string.IsNullOrEmpty(pName)) pName = "Player";
        if (string.IsNullOrEmpty(cName)) cName = "Bird Cafe";

        // Call the Library
        BirdCafeGame.Instance.StartNewGame(pName, cName);
    }

    // Hook this to your "Load Game" button
    // (If you have a Load Game slide, you might hook this to the specific save slot button)
    public void LoadGameDemo()
    {
        BirdCafeGame.Instance.LoadGame("demo_save");
    }

    // Hook this to your "Quit" button
    public void QuitGame()
    {
        Debug.Log("Quitting Game..."); // Visible in Editor
        Application.Quit();
    }
}