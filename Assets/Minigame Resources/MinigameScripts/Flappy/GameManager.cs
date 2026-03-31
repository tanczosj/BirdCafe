using BirdCafe.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static int score = 0;
    public int maxScore = 10;

    public static GameManager instance;

    void Awake()
    {
        instance = this;
        score = 0;
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
        Time.timeScale = 0f;
        SceneManager.UnloadSceneAsync("Flappy");
    }
}