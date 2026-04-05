
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BirdCafe.Unity.Birds
{
    [CreateAssetMenu(menuName = "Bird Cafe/Birds/Bird Appearance Library", fileName = "BirdAppearanceLibrary")]
    public sealed class BirdAppearanceLibrary : ScriptableObject
    {
        [SerializeField] private List<BirdAnimationSet> animationSets = new List<BirdAnimationSet>();
        [SerializeField] private BirdAnimationSet globalFallbackSet;

        private readonly Dictionary<string, BirdAnimationSet> _setByKey =
            new Dictionary<string, BirdAnimationSet>(StringComparer.Ordinal);

        public IReadOnlyList<BirdAnimationSet> AnimationSets => animationSets;

        public BirdAnimationSet ResolveSet(string speciesId, string costumeId)
        {
            RebuildLookup();

            string normalizedSpecies = Normalize(speciesId);
            string normalizedCostume = Normalize(costumeId);
            string exactKey = MakeKey(normalizedSpecies, normalizedCostume);
            string baseKey = MakeKey(normalizedSpecies, string.Empty);

            if (_setByKey.TryGetValue(exactKey, out BirdAnimationSet exact))
            {
                return exact;
            }

            if (!string.IsNullOrEmpty(normalizedCostume) && _setByKey.TryGetValue(baseKey, out BirdAnimationSet speciesBase))
            {
                Debug.LogWarning(
                    $"[BirdAppearanceLibrary] Missing exact set ({speciesId}, '{costumeId}'). Using species base set.",
                    this);
                return speciesBase;
            }

            if (globalFallbackSet != null)
            {
                Debug.LogWarning(
                    $"[BirdAppearanceLibrary] Missing appearance set for ({speciesId}, '{costumeId}'). Using global fallback '{globalFallbackSet.name}'.",
                    this);
                return globalFallbackSet;
            }

            Debug.LogWarning(
                $"[BirdAppearanceLibrary] Missing appearance set for ({speciesId}, '{costumeId}') and no global fallback configured.",
                this);
            return null;
        }

        public void ReplaceSets(List<BirdAnimationSet> sets)
        {
            animationSets = sets ?? new List<BirdAnimationSet>();
            RebuildLookup();
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string MakeKey(string speciesId, string costumeId)
        {
            return $"{speciesId}|{costumeId}";
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
            _setByKey.Clear();

            if (animationSets == null)
            {
                return;
            }

            for (int i = 0; i < animationSets.Count; i++)
            {
                BirdAnimationSet set = animationSets[i];
                if (set == null)
                {
                    continue;
                }

                string key = MakeKey(Normalize(set.SpeciesId), Normalize(set.CostumeId));
                _setByKey[key] = set;
            }
        }
    }
}