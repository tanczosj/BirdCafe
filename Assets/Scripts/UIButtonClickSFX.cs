using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonClickSFX : MonoBehaviour, IPointerEnterHandler
{
    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Click Sound")]
    [SerializeField] private AudioClip clickClip;
    [SerializeField][Range(0f, 1f)] private float clickVolume = 1f;
    [SerializeField] private float clickStartTime = 0f;

    [Header("Hover Sound")]
    [SerializeField] private AudioClip hoverClip;
    [SerializeField][Range(0f, 1f)] private float hoverVolume = 1f;
    [SerializeField] private float hoverStartTime = 0f;

    [Header("Behavior")]
    [SerializeField] private bool stopCurrentSoundBeforePlaying = true;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = FindFirstObjectByType<AudioSource>();
    }

    private void OnEnable()
    {
        if (button != null)
            button.onClick.AddListener(PlayClickSFX);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(PlayClickSFX);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayClipFromTime(hoverClip, hoverStartTime, hoverVolume);
    }

    private void PlayClickSFX()
    {
        PlayClipFromTime(clickClip, clickStartTime, clickVolume);
    }

    private void PlayClipFromTime(AudioClip clip, float startTime, float volume)
    {
        if (audioSource == null || clip == null)
            return;

        if (stopCurrentSoundBeforePlaying)
            audioSource.Stop();

        audioSource.clip = clip;
        audioSource.volume = volume;

        float safeStartTime = Mathf.Clamp(startTime, 0f, Mathf.Max(0f, clip.length - 0.01f));
        audioSource.time = safeStartTime;
        audioSource.Play();
    }
}