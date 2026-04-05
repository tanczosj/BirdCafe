
using System.Collections.Generic;
using BirdCafe.Shared.ViewModels;
using UnityEngine;

namespace BirdCafe.Unity.Birds
{
    public sealed class BirdRosterSpawner : MonoBehaviour
    {
        [SerializeField] private BirdVisualController birdPrefab;
        [SerializeField] private Transform spawnRoot;
        [SerializeField] private bool spawnOnEnable = true;
        [SerializeField] private bool clearExistingBeforeSpawn = true;

        private readonly List<BirdVisualController> _spawned = new List<BirdVisualController>();

        private void OnEnable()
        {
            if (spawnOnEnable)
            {
                SpawnRoster();
            }
        }

        [ContextMenu("Spawn Roster")]
        public void SpawnRoster()
        {
            if (birdPrefab == null)
            {
                Debug.LogWarning("[BirdRosterSpawner] Missing bird prefab reference.", this);
                return;
            }

            if (clearExistingBeforeSpawn)
            {
                ClearSpawned();
            }

            List<BirdAnimationStateViewModel> states = BirdVisualFacade.GetAllBirdAnimationStates();
            if (states == null || states.Count == 0)
            {
                Debug.LogWarning("[BirdRosterSpawner] Shared facade returned no bird animation states.", this);
                return;
            }

            Transform parent = spawnRoot != null ? spawnRoot : transform;

            foreach (BirdAnimationStateViewModel state in states)
            {
                if (state == null || string.IsNullOrWhiteSpace(state.BirdId))
                {
                    continue;
                }

                BirdVisualController instance = Instantiate(birdPrefab, parent);
                instance.BindToViewModel(state, immediateApply: true);
                _spawned.Add(instance);
            }
        }

        [ContextMenu("Clear Spawned")]
        public void ClearSpawned()
        {
            for (int i = _spawned.Count - 1; i >= 0; i--)
            {
                if (_spawned[i] == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(_spawned[i].gameObject);
                }
                else
                {
                    DestroyImmediate(_spawned[i].gameObject);
                }
            }

            _spawned.Clear();
        }
    }
}