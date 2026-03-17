using UnityEngine;

public class PlaybackSpeed : MonoBehaviour
{
    private Animator animator;

    [SerializeField]
    public float playbackSpeed = 1f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
        animator.speed = playbackSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
