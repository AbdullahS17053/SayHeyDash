namespace SayHeyDash.Settings
{
    /// <summary>Pure load/save of <see cref="SettingsData"/> through an <see cref="ISettingsStore"/> (unit-tested).</summary>
    public static class SettingsSerializer
    {
        public const string MutedKey = "muted";
        public const string MusicKey = "musicLevel";
        public const string EffectsKey = "effectsLevel";
        public const string ReducedMotionKey = "reducedMotion";
        public const string AssistedKey = "assisted";
        public const string BestScoreKey = "bestScore";

        /// <summary>Missing or out-of-range values fall back to the defaults.</summary>
        public static SettingsData Load(ISettingsStore store, bool reducedMotionDefault)
        {
            SettingsData d = SettingsData.Defaults(reducedMotionDefault);
            if (store == null) return d;
            if (store.TryGetInt(MutedKey, out int v)) d.Muted = v != 0;
            if (store.TryGetInt(MusicKey, out v)) d.MusicLevel = SettingsData.ClampLevel(v);
            if (store.TryGetInt(EffectsKey, out v)) d.EffectsLevel = SettingsData.ClampLevel(v);
            if (store.TryGetInt(ReducedMotionKey, out v)) d.ReducedMotion = v != 0;
            if (store.TryGetInt(AssistedKey, out v)) d.AssistedMode = v != 0;
            if (store.TryGetInt(BestScoreKey, out v)) d.BestScore = v < 0 ? 0 : (v > 1000000 ? 0 : v);
            return d;
        }

        public static void Save(ISettingsStore store, in SettingsData d)
        {
            if (store == null) return;
            store.SetInt(MutedKey, d.Muted ? 1 : 0);
            store.SetInt(MusicKey, SettingsData.ClampLevel(d.MusicLevel));
            store.SetInt(EffectsKey, SettingsData.ClampLevel(d.EffectsLevel));
            store.SetInt(ReducedMotionKey, d.ReducedMotion ? 1 : 0);
            store.SetInt(AssistedKey, d.AssistedMode ? 1 : 0);
            store.SetInt(BestScoreKey, d.BestScore);
            store.Save();
        }
    }
}
