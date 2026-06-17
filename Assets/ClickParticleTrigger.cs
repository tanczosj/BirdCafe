using UnityEngine;

public class ClickParticleTrigger : MonoBehaviour
{
    [Header("Particle System")]
    [SerializeField] private ParticleSystem particles;

    [Header("Options")]
    [SerializeField] private bool restartIfAlreadyPlaying = true;

    private void Awake()
    {
        if (particles == null)
            particles = GetComponent<ParticleSystem>();

        if (particles != null)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    public void PlayParticles()
    {
        if (particles == null)
        {
            Debug.LogWarning("No ParticleSystem assigned to ClickParticleTrigger.");
            return;
        }

        if (restartIfAlreadyPlaying)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        particles.Play();
    }
}