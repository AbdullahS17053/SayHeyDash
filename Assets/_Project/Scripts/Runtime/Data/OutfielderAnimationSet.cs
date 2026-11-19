using SayHeyDash.Visuals;
using UnityEngine;

namespace SayHeyDash.Data
{
    /// <summary>
    /// Every animation of the playable outfielder (GDD art list: idle, running, catching,
    /// turning and throwing). Frames are drawn facing right; the renderer is flipped to face left.
    /// Swap sprites or frame rates here without touching code.
    /// </summary>
    [CreateAssetMenu(menuName = "Say Hey Dash/Outfielder Animation Set", fileName = "OutfielderAnimations")]
    public sealed class OutfielderAnimationSet : ScriptableObject
    {
        public SpriteAnimationClip idle = new SpriteAnimationClip { name = "idle", framesPerSecond = 4f };
        public SpriteAnimationClip run = new SpriteAnimationClip { name = "run", framesPerSecond = 12f };
        [Tooltip("Running with the glove up, looking back over the shoulder (once the catch window opens).")]
        public SpriteAnimationClip runReach = new SpriteAnimationClip { name = "runReach", framesPerSecond = 12f };
        [Tooltip("Standing still while the catch window is open.")]
        public SpriteAnimationClip reachIdle = new SpriteAnimationClip { name = "reachIdle", framesPerSecond = 1f };
        [Tooltip("Over-the-shoulder catch (one-shot, ball in the glove).")]
        public SpriteAnimationClip catchBall = new SpriteAnimationClip { name = "catch", framesPerSecond = 12f, loop = false };
        [Tooltip("Missed catch (one-shot).")]
        public SpriteAnimationClip whiff = new SpriteAnimationClip { name = "whiff", framesPerSecond = 10f, loop = false };
        [Tooltip("The spin toward the infield (one-shot). Drawn ending facing LEFT.")]
        public SpriteAnimationClip spin = new SpriteAnimationClip { name = "spin", framesPerSecond = 10f, loop = false };
        [Tooltip("Set position held while the throw meter runs.")]
        public SpriteAnimationClip throwReady = new SpriteAnimationClip { name = "throwReady", framesPerSecond = 1f };
        [Tooltip("Throw and follow-through (one-shot).")]
        public SpriteAnimationClip throwRelease = new SpriteAnimationClip { name = "throwRelease", framesPerSecond = 14f, loop = false };
    }
}
