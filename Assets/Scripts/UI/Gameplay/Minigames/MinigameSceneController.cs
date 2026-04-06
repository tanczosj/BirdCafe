using BirdCafe.Shared;
using BirdCafe.Shared.Enums;
using BirdCafe.Shared.ViewModels;
using UnityEngine;

public class MinigameSceneController : MonoBehaviour
{
    private bool _finished;

    private void Awake()
    {
        Time.timeScale = 1f;
    }

    public void ReportSuccess(int score = 0, string message = null)
    {
        if (_finished) return;
        _finished = true;

        Time.timeScale = 1f;

        BirdCafeGame.Instance.CompleteCurrentMinigame(new MinigameCompletionViewModel
        {
            Status = MinigameCompletionStatus.Success,
            Score = score,
            ResultMessage = message ?? "Minigame completed."
        });
    }

    public void ReportFailure(int score = 0, string message = null)
    {
        if (_finished) return;
        _finished = true;

        Time.timeScale = 1f;

        BirdCafeGame.Instance.CompleteCurrentMinigame(new MinigameCompletionViewModel
        {
            Status = MinigameCompletionStatus.Failure,
            Score = score,
            ResultMessage = message ?? "Minigame failed."
        });
    }

    public void CancelAndReturn(string message = null)
    {
        if (_finished) return;
        _finished = true;

        Time.timeScale = 1f;
        BirdCafeGame.Instance.CancelCurrentMinigame();
    }
}