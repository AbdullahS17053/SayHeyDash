using System.Collections.Generic;

namespace SayHeyDash.Settings
{
    /// <summary>In-memory store: used in the Editor (unless persistence is switched on) and by tests.</summary>
    public sealed class MemorySettingsStore : ISettingsStore
    {
        private readonly Dictionary<string, int> _values = new Dictionary<string, int>();

        public bool IsPersistent => false;

        public int SaveCount { get; private set; }

        public bool TryGetInt(string key, out int value) => _values.TryGetValue(key, out value);

        public void SetInt(string key, int value) => _values[key] = value;

        public void Save() => SaveCount++;
    }
}
