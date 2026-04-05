
using System;
using UnityEngine;

namespace BirdCafe.Unity.Birds
{
    [Serializable]
    public sealed class BirdAnimationStateClip
    {
        public const float DefaultFps = 9f;

        [Tooltip("Shared visual state key (example: idle_neutral, emo_excited).")]
        public string StateKey;

        [Tooltip("Ordered frames for this visual state.")]
        public Sprite[] Frames;

        [Min(0.01f)]
        [Tooltip("Playback speed in frames per second.")]
        public float FramesPerSecond = DefaultFps;

        [Tooltip("If true, clip plays once and completes. If false, clip loops.")]
        public bool PlayOnce = true;
    }
}