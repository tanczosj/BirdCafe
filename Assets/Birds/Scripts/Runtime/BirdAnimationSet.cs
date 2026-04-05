
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BirdCafe.Unity.Birds
{
    [CreateAssetMenu(menuName = "Bird Cafe/Birds/Bird Animation Set", fileName = "BirdAnimationSet")]
    public sealed class BirdAnimationSet : ScriptableObject
    {
        public const string IdleNeutralStateKey = "idle_neutral";

        [SerializeField] private string speciesId;
        [SerializeField] private string costumeId;
        [SerializeField] private List<BirdAnimationStateClip> clips = new List<BirdAnimationStateClip>();

        private readonly Dictionary<string, BirdAnimationStateClip> _clipByState =
            new Dictionary<string, BirdAnimationStateClip>(StringComparer.Ordinal);

        public string SpeciesId => speciesId;
        public string CostumeId => costumeId;
        public IReadOnlyList<BirdAnimationStateClip> Clips => clips;

        public void SetIdentity(string newSpeciesId, string newCostumeId)
        {
            speciesId = newSpeciesId ?? string.Empty;
            costumeId = newCostumeId;
        }

        public void ReplaceClips(List<BirdAnimationStateClip> newClips)
        {
            clips = newClips ?? new List<BirdAnimationStateClip>();
            RebuildLookup();
        }

        public BirdAnimationStateClip GetClipOrFallback(string requestedStateKey)
        {
            RebuildLookup();

            if (TryGetClip(requestedStateKey, out BirdAnimationStateClip exact))
            {
                return exact;
            }

            if (TryGetClip(IdleNeutralStateKey, out BirdAnimationStateClip fallback))
            {
                Debug.LogWarning(
                    $"[BirdAnimationSet] Missing state '{requestedStateKey}' for ({speciesId}, '{costumeId ?? "base"}'). Falling back to '{IdleNeutralStateKey}'.",
                    this);
                return fallback;
            }

            Debug.LogWarning(
                $"[BirdAnimationSet] Missing requested '{requestedStateKey}' and fallback '{IdleNeutralStateKey}' for ({speciesId}, '{costumeId ?? "base"}').",
                this);
            return null;
        }

        public bool TryGetClip(string stateKey, out BirdAnimationStateClip clip)
        {
            RebuildLookup();

            if (string.IsNullOrWhiteSpace(stateKey))
            {
                clip = null;
                return false;
            }

            return _clipByState.TryGetValue(stateKey, out clip);
        }

        private void OnEnable()
        {
            RebuildLookup();
        }

        private void OnValidate()
        {
            RebuildLookup();
        }

        private void RebuildLookup()
        {
            _clipByState.Clear();

            if (clips == null)
            {
                return;
            }

            for (int i = 0; i < clips.Count; i++)
            {
                BirdAnimationStateClip clip = clips[i];
                if (clip == null || string.IsNullOrWhiteSpace(clip.StateKey))
                {
                    continue;
                }

                _clipByState[clip.StateKey] = clip;
            }
        }
    }
}