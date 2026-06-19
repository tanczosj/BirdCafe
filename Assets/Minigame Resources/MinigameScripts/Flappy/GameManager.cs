using BirdCafe.Shared;
using Ricimi;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static int score = 0;
    public int maxScore = 10;
    public static GameManager instance;
    public static bool gameFrozen = false;
    public static bool IsGameOver => gameFrozen;

    [Header("Start Screen")]
    [SerializeField] private GameObject startScreen; // Assign your "Press Space to Start" panel here

    private MinigameSceneController _sceneController;
    private Bird _bird;
    private bool gameEnded;
    private bool gameStarted = false;

    void Awake()
    {
        instance = this;
        score = 0;
        gameEnded = false;
        gameFrozen = true; // Frozen until player presses Space
        gameStarted = false;

        _sceneController = FindAnyObjectByType<MinigameSceneController>();
        _bird = FindAnyObjectByType<Bird>();

        // Show the start screen
        if (startScreen != null)
            startScreen.SetActive(true);
    }

    void Update()
    {
        if (!gameStarted && Input.GetKeyDown(KeyCode.Space))
            StartGame();
    }

    private void StartGame()
    {
        gameStarted = true;
        gameFrozen = false;

        if (startScreen != null)
            startScreen.SetActive(false);

        // Unfreeze the bird
        if (_bird == null)
            _bird = FindAnyObjectByType<Bird>();

        if (_bird != null)
            _bird.UnfreezeBird();

        Debug.Log("[GameManager] Game started!");
    }

    public void AddScore()
    {
        if (!gameStarted || gameEnded || gameFrozen)
            return;

        score++;
        Debug.Log("Score: " + score);

        if (score >= maxScore)
            EndGame();
    }

    public void GameOver()
    {
        if (gameEnded) return;

        gameEnded = true;
        gameFrozen = true;
        FreezeGameplay();

        Debug.Log("You crashed.");

        if (_sceneController != null)
            _sceneController.ReportFailure(score, "You crashed.");
    }

    void EndGame()
    {
        if (gameEnded) return;

        gameEnded = true;
        gameFrozen = true;
        FreezeGameplay();

        Debug.Log("You Win!");

        if (_sceneController != null)
            _sceneController.ReportSuccess(score, "Flappy completed.");
    }

    void FreezeGameplay()
    {
        if (_bird == null)
            _bird = FindAnyObjectByType<Bird>();

        if (_bird != null)
            _bird.FreezeBird();
    }


    void OnDestroy()
    {
        gameFrozen = false;
    }
}