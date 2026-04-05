using BirdCafe.Shared.ViewModels;
using UnityEngine;
using UnityEngine.UI;

namespace BirdCafe.Unity.Birds
{
    [DisallowMultipleComponent]
    public sealed class BirdVisualController : MonoBehaviour
    {
        [SerializeField] private string birdId;
        [SerializeField] private BirdAppearanceLibrary appearanceLibrary;
        [SerializeField] private BirdSpriteAnimator spriteAnimator;
        [SerializeField] private Image targetImage;
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private bool initializeOnEnable = true;
        [SerializeField] private bool hideWhenUnbound = true;
        [SerializeField] private bool logVerboseValidation;

        private string _resolvedSpeciesId = string.Empty;
        private string _resolvedCostumeId = string.Empty;
        private BirdAnimationSet _currentSet;
        private bool _isAdvancing;

        public string BirdId => birdId;
        public bool IsBound => !string.IsNullOrWhiteSpace(birdId);

        private void Reset()
        {
            if (spriteAnimator == null)
            {
                spriteAnimator = GetComponent<BirdSpriteAnimator>();
            }

            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }

            if (targetImage == null)
            {
                targetImage = GetComponentInChildren<Image>(true);
            }
        }

        private void OnEnable()
        {
            EnsureReferences();

            if (spriteAnimator != null)
            {
                spriteAnimator.ClipCompleted += HandleClipCompleted;
            }

            if (initializeOnEnable)
            {
                if (IsBound)
                {
                    RefreshVisualFromShared();
                }
                else if (hideWhenUnbound)
                {
                    HideVisual();
                }
            }
        }

        private void OnDisable()
        {
            if (spriteAnimator != null)
            {
                spriteAnimator.ClipCompleted -= HandleClipCompleted;
            }
        }

        [ContextMenu("Refresh Visual From Shared")]
        public void RefreshVisualFromShared()
        {
            if (!IsBound)
            {
                if (hideWhenUnbound)
                {
                    HideVisual();
                }

                return;
            }

            if (!TryGetViewModel(out BirdAnimationStateViewModel viewModel))
            {
                if (hideWhenUnbound)
                {
                    HideVisual();
                }

                return;
            }

            ApplyViewModel(viewModel);
        }

        public void BindToBird(string newBirdId, bool immediateRefresh = true)
        {
            birdId = newBirdId;

            if (immediateRefresh)
            {
                RefreshVisualFromShared();
            }
        }

        public void BindToViewModel(BirdAnimationStateViewModel viewModel, bool immediateApply = true)
        {
            if (viewModel == null || string.IsNullOrWhiteSpace(viewModel.BirdId))
            {
                Unbind(hideVisual: hideWhenUnbound);
                return;
            }

            birdId = viewModel.BirdId;

            if (immediateApply)
            {
                ApplyViewModel(viewModel);
            }
        }

        public void Unbind(bool hideVisual = true)
        {
            birdId = null;
            _resolvedSpeciesId = string.Empty;
            _resolvedCostumeId = string.Empty;
            _currentSet = null;
            _isAdvancing = false;

            if (spriteAnimator != null)
            {
                spriteAnimator.Stop();
            }

            if (hideVisual)
            {
                HideVisual();
            }
        }

        public void ShowVisual()
        {
            EnsureReferences();

            if (visualRoot != null && visualRoot != gameObject)
            {
                visualRoot.SetActive(true);
            }
            else if (targetImage != null)
            {
                targetImage.enabled = true;
            }
        }

        public void HideVisual()
        {
            EnsureReferences();

            if (spriteAnimator != null)
            {
                spriteAnimator.Stop();
            }

            if (targetImage != null)
            {
                targetImage.sprite = null;
            }

            if (visualRoot != null && visualRoot != gameObject)
            {
                visualRoot.SetActive(false);
            }
            else if (targetImage != null)
            {
                targetImage.enabled = false;
            }
        }

        private void HandleClipCompleted(BirdAnimationStateClip completedClip)
        {
            if (_isAdvancing || !IsBound)
            {
                return;
            }

            _isAdvancing = true;

            try
            {
                if (!BirdVisualFacade.TryAdvanceBirdAnimationState(birdId))
                {
                    Debug.LogWarning($"[BirdVisualController] Could not advance visual state for bird '{birdId}'.", this);
                }

                RefreshVisualFromShared();
            }
            finally
            {
                _isAdvancing = false;
            }
        }

        private bool TryGetViewModel(out BirdAnimationStateViewModel viewModel)
        {
            viewModel = null;

            if (!IsBound)
            {
                return false;
            }

            if (!BirdVisualFacade.TryGetBirdAnimationState(birdId, out viewModel))
            {
                Debug.LogWarning($"[BirdVisualController] Shared facade did not return animation state for bird '{birdId}'.", this);
                return false;
            }

            return true;
        }

        private void ApplyViewModel(BirdAnimationStateViewModel viewModel)
        {
            if (viewModel == null || string.IsNullOrWhiteSpace(viewModel.BirdId))
            {
                Unbind(hideVisual: hideWhenUnbound);
                return;
            }

            birdId = viewModel.BirdId;
            EnsureAppearanceSet(viewModel.SpeciesId, viewModel.CostumeId);

            if (_currentSet == null)
            {
                HideVisual();
                return;
            }

            ShowVisual();
            PlayState(viewModel.CurrentVisualStateKey);

            if (logVerboseValidation)
            {
                Debug.Log(
                    $"[BirdVisualController] Applied bird '{birdId}' => ({viewModel.SpeciesId}, '{viewModel.CostumeId ?? "base"}') state '{viewModel.CurrentVisualStateKey}'.",
                    this);
            }
        }

        private void EnsureReferences()
        {
            if (spriteAnimator == null)
            {
                spriteAnimator = GetComponent<BirdSpriteAnimator>();
            }

            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }

            if (targetImage == null)
            {
                targetImage = GetComponentInChildren<Image>(true);
            }

            if (spriteAnimator != null && targetImage != null)
            {
                spriteAnimator.SetTargetImage(targetImage);
            }
        }

        private void EnsureAppearanceSet(string speciesId, string costumeId)
        {
            if (appearanceLibrary == null)
            {
                Debug.LogWarning("[BirdVisualController] Missing BirdAppearanceLibrary reference.", this);
                _currentSet = null;
                return;
            }

            string normalizedSpecies = speciesId ?? string.Empty;
            string normalizedCostume = costumeId ?? string.Empty;

            bool appearanceChanged =
                !string.Equals(_resolvedSpeciesId, normalizedSpecies) ||
                !string.Equals(_resolvedCostumeId, normalizedCostume);

            if (!appearanceChanged && _currentSet != null)
            {
                return;
            }

            _resolvedSpeciesId = normalizedSpecies;
            _resolvedCostumeId = normalizedCostume;
            _currentSet = appearanceLibrary.ResolveSet(speciesId, costumeId);

            if (_currentSet == null)
            {
                Debug.LogWarning(
                    $"[BirdVisualController] No animation set resolved for ({speciesId}, '{costumeId ?? "base"}').",
                    this);
            }
        }

        private void PlayState(string stateKey)
        {
            EnsureReferences();

            if (spriteAnimator == null)
            {
                Debug.LogWarning("[BirdVisualController] Missing BirdSpriteAnimator reference.", this);
                return;
            }

            if (_currentSet == null)
            {
                Debug.LogWarning($"[BirdVisualController] Unable to play '{stateKey}' because no appearance set is resolved.", this);
                return;
            }

            BirdAnimationStateClip clip = _currentSet.GetClipOrFallback(stateKey);
            if (clip == null)
            {
                Debug.LogWarning($"[BirdVisualController] Missing clip '{stateKey}' and fallback for bird '{birdId}'.", this);
                HideVisual();
                return;
            }

            ShowVisual();
            spriteAnimator.Play(clip, restart: true);
        }
    }
}