using System;
using SayHeyDash.Platform;
using UnityEngine;

namespace SayHeyDash.Settings
{
    /// <summary>
    /// <see cref="ISettingsStore"/> over Unity's PlayerPrefs (IndexedDB on WebGL). Never throws:
    /// any storage error is logged once as a warning and the game carries on with in-memory values.
    /// </summary>
    public sealed class PlayerPrefsSettingsStore : ISettingsStore
    {
        private const string Prefix = "shd.";
        private bool _warned;

        public PlayerPrefsSettingsStore()
        {
            IsPersistent = BrowserBridge.StorageAvailable;
        }

        public bool IsPersistent { get; private set; }

        public bool TryGetInt(string key, out int value)
        {
            value = 0;
            if (!IsPersistent) return false;
            try
            {
                if (!PlayerPrefs.HasKey(Prefix + key)) return false;
                value = PlayerPrefs.GetInt(Prefix + key);
                return true;
            }
            catch (Exception e)
            {
                Fail(e);
                return false;
            }
        }

        public void SetInt(string key, int value)
        {
            if (!IsPersistent) return;
            try { PlayerPrefs.SetInt(Prefix + key, value); }
            catch (Exception e) { Fail(e); }
        }

        public void Save()
        {
            if (!IsPersistent) return;
            try { PlayerPrefs.Save(); }
            catch (Exception e) { Fail(e); }
        }

        private void Fail(Exception e)
        {
            IsPersistent = false;
            if (_warned) return;
            _warned = true;
            Debug.LogWarning($"[Settings] Storage unavailable, settings last for this visit only: {e.Message}");
        }
    }
}
