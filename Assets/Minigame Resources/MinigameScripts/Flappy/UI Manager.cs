using UnityEngine;
using UnityEngine.UI; // Required for Legacy Text

public class ScoreDisplay : MonoBehaviour
{
    public Text scoreText; // Drag your Legacy Text component here in the Inspector

    void Update()
    {
        // Access the score from the GameManager instance and convert to string
        if (GameManager.instance != null)
        {
            scoreText.text = GameManager.score.ToString();
        }
    }
}