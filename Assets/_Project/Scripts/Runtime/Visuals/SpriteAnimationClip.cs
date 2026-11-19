using System;
using UnityEngine;

namespace SayHeyDash.Visuals
{
    /// <summary>
    /// A flip-book animation: a list of sprites played at a fixed frame rate. Plain data,
    /// so clips can live in ScriptableObjects and be edited in the Inspector.
    /// </summary>
    [Serializable]
    public sealed class SpriteAnimationClip
    {
        public string name;
        public Sprite[] frames = Array.Empty<Sprite>();
        [Min(0.1f)] public float framesPerSecond = 12f;
        public bool loop = true;

        public SpriteAnimationClip() { }

        public SpriteAnimationClip(string name, Sprite[] frames, float framesPerSecond, bool loop)
        {
            this.name = name;
            this.frames = frames ?? Array.Empty<Sprite>();
            this.framesPerSecond = framesPerSecond;
            this.loop = loop;
        }

        public int FrameCount => frames != null ? frames.Length : 0;

        public bool IsValid => FrameCount > 0;

        /// <summary>Length of one pass through the clip, in seconds.</summary>
        public float Duration => FrameCount / Mathf.Max(0.1f, framesPerSecond);

        /// <summary>
        /// Frame shown <paramref name="time"/> seconds after the clip started. Looping clips
        /// wrap; one-shot clips hold their last frame. Pure function (unit-tested).
        /// </summary>
        public static int FrameAt(int frameCount, float framesPerSecond, float time, bool loop)
        {
            if (frameCount <= 0) return -1;
            if (frameCount == 1 || time <= 0f || framesPerSecond <= 0f) return 0;
            int index = Mathf.FloorToInt(time * framesPerSecond + 1e-4f);
            return loop ? index % frameCount : Mathf.Min(index, frameCount - 1);
        }

        /// <summary>True once a one-shot clip has shown its last frame for a full frame time.</summary>
        public static bool IsFinishedAt(int frameCount, float framesPerSecond, float time, bool loop) =>
            !loop && frameCount > 0 && time >= frameCount / Mathf.Max(0.1f, framesPerSecond);
    }
}
