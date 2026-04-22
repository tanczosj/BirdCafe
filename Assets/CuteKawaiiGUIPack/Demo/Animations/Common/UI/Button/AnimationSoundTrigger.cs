using UnityEngine;

public class AnimationSoundTrigger : StateMachineBehaviour
{
    [Tooltip("The AudioClip to play when this animation state is entered.")]
    public AudioClip soundClip;

    [Range(0f, 1f)]
    public float volume = 1f;

    private bool hasPlayed = false;

    // Called when the state is first entered
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        hasPlayed = false;

        if (soundClip == null)
        {
            Debug.LogWarning("AnimationSoundTrigger: No AudioClip assigned.");
            return;
        }

        AudioSource audioSource = animator.GetComponent<AudioSource>();

        if (audioSource == null)
        {
            Debug.LogWarning("AnimationSoundTrigger: No AudioSource found on the Animator's GameObject. Adding one.");
            audioSource = animator.gameObject.AddComponent<AudioSource>();
        }

        if (!hasPlayed)
        {
            audioSource.PlayOneShot(soundClip, volume);
            hasPlayed = true;
        }
    }
}