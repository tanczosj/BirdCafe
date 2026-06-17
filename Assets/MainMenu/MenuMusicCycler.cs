using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MenuMusicCycler : MonoBehaviour
{
    [Header("Playlist")]
    [SerializeField] private List<AudioClip> songs = new List<AudioClip>();
    [SerializeField] private bool shuffle = false;
    [SerializeField] private bool loopPlaylist = true;

    [Header("Playback")]
    [SerializeField] private float startVolume = 0.6f;
    [SerializeField] private float fadeInDuration = 0.4f;
    [SerializeField] private float fadeOutDuration = 0.4f;
    [SerializeField] private float gapBetweenSongs = 0.15f;

    private AudioSource audioSource;
    private int currentIndex = -1;
    private bool isStopping;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0f;
    }

    private void Start()
    {
        if (songs.Count == 0)
        {
            Debug.LogWarning("MenuMusicCycler has no songs assigned.");
            return;
        }

        StartCoroutine(PlaylistRoutine());
    }

    private IEnumerator PlaylistRoutine()
    {
        while (true)
        {
            AudioClip nextSong = GetNextSong();
            if (nextSong == null)
                yield break;

            audioSource.clip = nextSong;
            audioSource.Play();

            yield return StartCoroutine(FadeVolume(0f, startVolume, fadeInDuration));

            while (audioSource.isPlaying)
                yield return null;

            yield return StartCoroutine(FadeVolume(audioSource.volume, 0f, fadeOutDuration));

            if (gapBetweenSongs > 0f)
                yield return new WaitForSecondsRealtime(gapBetweenSongs);

            if (!loopPlaylist && !shuffle && currentIndex >= songs.Count - 1)
                yield break;
        }
    }

    private AudioClip GetNextSong()
    {
        if (songs.Count == 0)
            return null;

        if (shuffle)
        {
            if (songs.Count == 1)
            {
                currentIndex = 0;
                return songs[0];
            }

            int nextIndex = currentIndex;
            while (nextIndex == currentIndex)
                nextIndex = Random.Range(0, songs.Count);

            currentIndex = nextIndex;
            return songs[currentIndex];
        }

        currentIndex++;

        if (currentIndex >= songs.Count)
        {
            if (!loopPlaylist)
                return null;

            currentIndex = 0;
        }

        return songs[currentIndex];
    }

    private IEnumerator FadeVolume(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            audioSource.volume = to;
            yield break;
        }

        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);
            audioSource.volume = Mathf.Lerp(from, to, t);
            yield return null;
        }

        audioSource.volume = to;
    }
}