using BirdCafe.Shared;
using Ricimi;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static int score = 0;
    public int maxScore = 10;

    public static GameManager instance;
    private MinigameSceneController _sceneController;

    void Awake()
    {
        instance = this;
        score = 0;
        _sceneController = FindAnyObjectByType<MinigameSceneController>();
    }

    public void AddScore()
    {
        score++;
        Debug.Log("Score: " + score);

        if (score >= maxScore)
        {
            EndGame();
        }
    }

    void EndGame()
    {
        Debug.Log("You Win!");

        if (_sceneController != null)
            _sceneController.ReportSuccess(score, "Flappy completed.");
    }
}