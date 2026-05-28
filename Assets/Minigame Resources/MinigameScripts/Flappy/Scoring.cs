using UnityEngine;

public class ScoreZone : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (GameManager.IsGameOver)
                return;

            GameManager.instance.AddScore();
        }
    }
}
