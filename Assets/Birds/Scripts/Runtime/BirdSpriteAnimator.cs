using System;
using UnityEngine;
using UnityEngine.UI;

namespace BirdCafe.Unity.Birds
{
    [DisallowMultipleComponent]
    public sealed class BirdSpriteAnimator : MonoBehaviour
    {
        [SerializeField] private Image targetImage;

        public event Action<BirdAnimationStateClip> ClipCompleted;

        public BirdAnimationStateClip CurrentClip => _currentClip;

        private BirdAnimationStateClip _currentClip;
        private int _frameIndex;
        private float _frameTimer;
        private bool _playing;
        private bool _completionRaised;

        public void SetTargetImage(Image image)
        {
            targetImage = image;
        }

        public void Play(BirdAnimationStateClip clip, bool restart = true)
        {
            if (clip == null)
            {
                Debug.LogWarning("[BirdSpriteAnimator] Cannot play null clip.", this);
                Stop();
                return;
            }

            if (!restart && ReferenceEquals(_currentClip, clip) && _playing)
            {
                return;
            }

            _currentClip = clip;
            _frameIndex = 0;
            _frameTimer = 0f;
            _playing = true;
            _completionRaised = false;

            ApplyFrame();
        }

        public void Stop()
        {
            _playing = false;
            _frameTimer = 0f;
            _frameIndex = 0;
            _completionRaised = false;
        }

        private void Reset()
        {
            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }

            if (targetImage == null)
            {
                targetImage = GetComponentInChildren<Image>(true);
            }
        }

        private void Update()
        {
            if (!_playing || _currentClip == null)
            {
                return;
            }

            Sprite[] frames = _currentClip.Frames;
            if (frames == null || frames.Length == 0)
            {
                Debug.LogWarning($"[BirdSpriteAnimator] Clip '{_currentClip.StateKey}' has no frames.", this);
                _playing = false;
                RaiseCompletedOnce();
                return;
            }

            float fps = Mathf.Max(0.01f, _currentClip.FramesPerSecond);
            float frameDuration = 1f / fps;

            _frameTimer += Time.deltaTime;

            while (_frameTimer >= frameDuration)
            {
                _frameTimer -= frameDuration;
                AdvanceFrame();

                if (!_playing)
                {
                    break;
                }
            }
        }

        private void AdvanceFrame()
        {
            if (_currentClip == null || _currentClip.Frames == null || _currentClip.Frames.Length == 0)
            {
                _playing = false;
                return;
            }

            _frameIndex++;
            bool reachedEnd = _frameIndex >= _currentClip.Frames.Length;

            if (!reachedEnd)
            {
                ApplyFrame();
                return;
            }

            if (_currentClip.PlayOnce)
            {
                _frameIndex = _currentClip.Frames.Length - 1;
                ApplyFrame();
                _playing = false;
                RaiseCompletedOnce();
                return;
            }

            _frameIndex = 0;
            ApplyFrame();
        }

        private void ApplyFrame()
        {
            if (targetImage == null)
            {
                Debug.LogWarning("[BirdSpriteAnimator] Missing target Image reference.", this);
                return;
            }

            if (_currentClip?.Frames == null || _currentClip.Frames.Length == 0)
            {
                return;
            }

            int clampedIndex = Mathf.Clamp(_frameIndex, 0, _currentClip.Frames.Length - 1);
            targetImage.sprite = _currentClip.Frames[clampedIndex];
        }

        private void RaiseCompletedOnce()
        {
            if (_completionRaised)
            {
                return;
            }

            _completionRaised = true;
            ClipCompleted?.Invoke(_currentClip);
        }
    }
}