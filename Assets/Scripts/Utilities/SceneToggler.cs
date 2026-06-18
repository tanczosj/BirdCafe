using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneToggler : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string sceneA = "MainMenu";
    [SerializeField] private string sceneB = "Stage";

    [Header("Hotkey Settings")]
    [SerializeField] private KeyCode hotkey = KeyCode.Q;
    private bool requireCtrl = true;
    private bool requireShift = true;

    private void Update()
    {
        DontDestroyOnLoad(gameObject);
        if (IsHotkeyPressed())
            ToggleScene();
    }

    private bool IsHotkeyPressed()
    {
        bool ctrl = !requireCtrl || (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
        bool shift = !requireShift || (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

        return ctrl && shift && Input.GetKeyDown(hotkey);
    }

    private void ToggleScene()
    {
        string current = SceneManager.GetActiveScene().name;

        if (current == sceneA)
            SceneManager.LoadScene(sceneB);
        else if (current == sceneB)
            SceneManager.LoadScene(sceneA);
        else
            SceneManager.LoadScene(sceneB);
    }
}