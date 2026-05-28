using BirdCafe.Shared;
using Ricimi;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static int score = 0;
    public int maxScore = 10;

    public static GameManager instance;
    public static bool gameFrozen = false;
    public static bool IsGameOver => gameFrozen;

    private MinigameSceneController _sceneController;
    private Bird _bird;
    private bool gameEnded;

    void Awake()
    {
        instance = this;
        score = 0;
        gameEnded = false;
        gameFrozen = false;

        _sceneController = FindAnyObjectByType<MinigameSceneController>();
        _bird = FindAnyObjectByType<Bird>();
    }

    public void AddScore()
    {
        if (gameEnded || gameFrozen)
            return;

        score++;
        Debug.Log("Score: " + score);

        if (score >= maxScore)
        {
            EndGame();
        }
    }

    public void GameOver()
    {
        if (gameEnded)
            return;

        gameEnded = true;
        gameFrozen = true;

        FreezeGameplay();

        Debug.Log("You crashed.");

        if (_sceneController != null)
            _sceneController.ReportFailure(score, "You crashed.");
    }

    void EndGame()
    {
        if (gameEnded)
            return;

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
