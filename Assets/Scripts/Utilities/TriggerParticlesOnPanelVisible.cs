using UnityEngine;

public class TriggerParticlesOnPanelVisible : MonoBehaviour
{
    [SerializeField] private GameObject panelEveningSummary;
    [SerializeField] private ParticleSystem[] particleSystems;

    private bool wasVisible;

    private void Start()
    {
        if (panelEveningSummary != null)
            wasVisible = panelEveningSummary.activeInHierarchy;
    }

    private void Update()
    {
        if (panelEveningSummary == null)
            return;

        bool isVisible = panelEveningSummary.activeInHierarchy;

        if (!wasVisible && isVisible)
        {
            PlayParticles();
        }

        wasVisible = isVisible;
    }

    private void PlayParticles()
    {
        foreach (ParticleSystem ps in particleSystems)
        {
            if (ps == null) continue;

            ps.gameObject.SetActive(true);
            ps.Clear();
            ps.Play();
        }
    }
}