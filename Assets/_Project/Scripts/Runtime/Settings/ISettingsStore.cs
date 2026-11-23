namespace SayHeyDash.Settings
{
    /// <summary>
    /// Where settings are kept between visits. Every call is best-effort: browsers may block
    /// or partition storage inside third-party iframes (Docs/04 F6), so a failed read simply
    /// means "use the default" and a failed write means "this visit only".
    /// </summary>
    public interface ISettingsStore
    {
        /// <summary>False when the browser refuses storage (settings then last for this visit only).</summary>
        bool IsPersistent { get; }

        bool TryGetInt(string key, out int value);

        void SetInt(string key, int value);

        void Save();
    }
}
