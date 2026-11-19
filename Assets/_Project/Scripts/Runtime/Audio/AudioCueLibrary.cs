using System;
using UnityEngine;

namespace SayHeyDash.Audio
{
    /// <summary>
    /// Clip + volume per <see cref="AudioCue"/> (Docs/01 section 6). Swap a sound or change its
    /// level here; no code changes needed. Final mixing happens in Milestone 4.
    /// </summary>
    [CreateAssetMenu(menuName = "Say Hey Dash/Audio Cue Library", fileName = "AudioCueLibrary")]
    public sealed class AudioCueLibrary : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public AudioCue cue;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
        }

        public Entry[] entries = Array.Empty<Entry>();

        /// <summary>Finds the entry for a cue. Returns false if it is missing or has no clip.</summary>
        public bool TryGet(AudioCue cue, out Entry entry)
        {
            if (entries != null)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    if (entries[i].cue != cue || entries[i].clip == null) continue;
                    entry = entries[i];
                    return true;
                }
            }
            entry = default;
            return false;
        }
    }
}
