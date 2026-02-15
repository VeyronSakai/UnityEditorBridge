using UnityEditor;

namespace EditorBridge.Editor.Settings
{
    /// <summary>
    /// Persistent user settings for the EditorBridge server.
    ///
    /// Stored as a ScriptableSingleton at the Unity Preferences folder
    /// (e.g. ~/Library/Preferences/Unity/EditorBridge/Settings.asset on macOS).
    /// This means settings survive project switches and are per-user, not per-project.
    /// </summary>
    [FilePath("EditorBridge/Settings.asset", FilePathAttribute.Location.PreferencesFolder)]
    internal sealed class EditorBridgeSettings : ScriptableSingleton<EditorBridgeSettings>
    {
        // The TCP port number the HTTP server listens on.
        // Defaults to 56780 — a high port unlikely to conflict with common services.
        public int Port = 56780;

        // When true, the server starts automatically on editor launch / domain reload.
        // When false, the server must be started manually.
        public bool AutoStart = true;

        /// <summary>
        /// Persists the current settings to disk.
        /// Wraps the inherited <see cref="ScriptableSingleton{T}.Save(bool)"/> with
        /// saveAsText=true so the asset is stored as a human-readable YAML file.
        /// </summary>
        public void Save()
        {
            Save(true);
        }
    }
}
